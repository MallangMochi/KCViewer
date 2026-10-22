using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using KCViewer.Fork.Update;

namespace KCUpdater;

/// <summary>
/// Fork: step 4 of UPDATER-DESIGN.md. Waits for the viewer, checks the new files again, moves the files that change
/// to <c>Update\backup</c>, moves the new ones in and the new list last. When a move fails, every file goes back to
/// where it was, so the folder is never left half updated (principle 4). Only listed program files are touched
/// (principle 2): a file of the old list that the new one drops is removed, nothing else.
/// </summary>
internal sealed class Updater(Options options, Log log)
{
	/// <summary>How long the viewer gets to close before the user is asked.</summary>
	private static readonly TimeSpan ViewerWait = TimeSpan.FromSeconds(60);

	/// <summary>How long the folder's other viewer processes (the test instance, the browser) get.</summary>
	private static readonly TimeSpan OthersWait = TimeSpan.FromSeconds(30);

	private string Folder => options.Folder;
	private string Staging => options.Staging;
	private string Backup { get; } = Path.Combine(options.Folder, UpdateFormat.UpdateFolderName, UpdateFormat.BackupFolderName);
	private string FolderList => Path.Combine(Folder, UpdateFormat.InstallManifestFileName);
	private string StagingList => Path.Combine(Staging, UpdateFormat.InstallManifestFileName);
	private string BackupList => Path.Combine(Backup, UpdateFormat.InstallManifestFileName);

	private UpdateResult Result { get; } = new() { Time = DateTimeOffset.Now };

	/// <summary>The user chose not to update while windows were open: nothing more to tell them.</summary>
	public bool Cancelled { get; private set; }

	public UpdateResult Run()
	{
		InstallManifest next;
		List<string> replace;
		List<string> remove;

		try
		{
			if (!WaitForViewer())
			{
				Cancelled = true;
				return Stop(UpdateOutcome.NotStarted, Strings.WindowsLeftOpen);
			}

			// a folder without its list cannot be updated safely: installed by hand again (user's decision 2026-10-22
			// "목록 누락으로 수동 재설치 권장 알림 띄우고 중단")
			if (!File.Exists(FolderList)) return Stop(UpdateOutcome.NotStarted, Strings.NoInstallList);

			InstallManifest current = UpdateFormat.ReadInstallManifest(FolderList);
			if (UpdateFormat.CheckList(current) is { } listProblem)
			{
				log.Write($"the folder's list: {listProblem}");
				return Stop(UpdateOutcome.NotStarted, Strings.NoInstallList);
			}

			next = UpdateFormat.ReadInstallManifest(StagingList);
			Result.From = current.Version;
			Result.To = next.Version;

			log.Write($"checking the new files of {next.Version}");
			if (UpdateFormat.CheckFolder(Staging, next) is { } problem)
			{
				log.Write($"the new files: {problem}");
				return Stop(UpdateOutcome.NotStarted, Strings.BadNewFile);
			}

			// a file is replaced when it is missing or differs from the new one, and removed when only the old list has it
			replace = next.Files
				.Where(file => !SameFile(UpdateFormat.LocalPath(Folder, file.Path), file))
				.Select(file => file.Path)
				.ToList();
			HashSet<string> kept = new(next.Files.Select(file => file.Path), StringComparer.OrdinalIgnoreCase);
			remove = current.Files
				.Select(file => file.Path)
				.Where(path => !kept.Contains(path) && File.Exists(UpdateFormat.LocalPath(Folder, path)))
				.ToList();
			log.Write($"{replace.Count} to replace, {remove.Count} to remove: {string.Join(", ", replace.Concat(remove))}");
		}
		catch (Exception e)
		{
			log.Write($"stopped before any change: {e}");
			return Stop(UpdateOutcome.NotStarted, Strings.CheckFailed);
		}

		return Replace(replace, remove);
	}

	private UpdateResult Stop(UpdateOutcome outcome, string message)
	{
		Result.Outcome = outcome;
		Result.Message = message;
		return Result;
	}

	private static bool SameFile(string file, InstallFile listed)
	{
		FileInfo info = new(file);
		return info.Exists && info.Length == listed.Size && UpdateFormat.Sha256(file).Equals(listed.Sha256, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Moves, and puts everything back when a move fails.</summary>
	private UpdateResult Replace(List<string> replace, List<string> remove)
	{
		// what was moved, in order, to be undone backwards
		List<(string From, string To)> moved = [];

		try
		{
			if (Directory.Exists(Backup)) Directory.Delete(Backup, recursive: true);
			Directory.CreateDirectory(Backup);

			foreach (string path in replace.Concat(remove))
			{
				string file = UpdateFormat.LocalPath(Folder, path);
				if (!File.Exists(file)) continue;

				Move(file, UpdateFormat.LocalPath(Backup, path));
				moved.Add((file, UpdateFormat.LocalPath(Backup, path)));
			}

			foreach (string path in replace)
			{
				Move(UpdateFormat.LocalPath(Staging, path), UpdateFormat.LocalPath(Folder, path));
				moved.Add((UpdateFormat.LocalPath(Staging, path), UpdateFormat.LocalPath(Folder, path)));
			}

			// the list last: until it is in place the folder still says it is the old version
			Move(FolderList, BackupList);
			moved.Add((FolderList, BackupList));
			Move(StagingList, FolderList);
			moved.Add((StagingList, FolderList));
		}
		catch (Exception e)
		{
			log.Write($"replacing failed, putting the files back: {e}");
			bool restored = Undo(moved);
			return Stop(
				restored ? UpdateOutcome.RolledBack : UpdateOutcome.RollbackFailed,
				restored ? Strings.RolledBack(e.Message) : Strings.RollbackFailed(Backup));
		}

		Result.Outcome = UpdateOutcome.Updated;
		Result.Replaced = replace.Count;
		Result.Removed = remove.Count;

		RemoveEmptyFolders(remove);
		TryDelete(Backup);
		TryDelete(Staging);
		return Result;
	}

	/// <summary>Every move undone, last first; false when one could not be.</summary>
	private bool Undo(List<(string From, string To)> moved)
	{
		bool restored = true;
		for (int i = moved.Count - 1; i >= 0; i--)
		{
			(string from, string to) = moved[i];
			try
			{
				Move(to, from);
			}
			catch (Exception e)
			{
				restored = false;
				log.Write($"could not put back {from}: {e.Message}");
			}
		}

		return restored;
	}

	/// <summary>A move on the same drive (a rename), tried a few times: a virus scanner may hold a new file for a moment.</summary>
	private void Move(string from, string to)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(to)!);
		for (int attempt = 1; ; attempt++)
		{
			try
			{
				File.Move(from, to);
				return;
			}
			catch (IOException) when (attempt < 5 && File.Exists(from) && !File.Exists(to))
			{
				Thread.Sleep(200 * attempt);
			}
			catch (UnauthorizedAccessException) when (attempt < 5)
			{
				Thread.Sleep(200 * attempt);
			}
		}
	}

	/// <summary>The folders a removed file leaves empty, up to the portable folder.</summary>
	private void RemoveEmptyFolders(List<string> removed)
	{
		foreach (string path in removed)
		{
			string? folder = Path.GetDirectoryName(UpdateFormat.LocalPath(Folder, path));
			while (folder is not null && folder.Length > Folder.Length && Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
			{
				try
				{
					Directory.Delete(folder);
				}
				catch (IOException)
				{
					break;
				}

				folder = Path.GetDirectoryName(folder);
			}
		}
	}

	private void TryDelete(string folder)
	{
		try
		{
			if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
		}
		catch (Exception e)
		{
			log.Write($"could not delete {folder}: {e.Message}");
		}
	}

	/// <summary>
	/// The viewer that started the update, then the folder's other viewer processes (the test instance, the browsers):
	/// the program files are locked while they run. Processes of other folders are left alone. After a while the user is
	/// told which are still open: closed, the update goes on; cancelled, it does not happen.
	/// </summary>
	private bool WaitForViewer()
	{
		if (options.ProcessId is { } id && Running(id) is { } viewer)
		{
			log.Write($"waiting for the viewer ({id})");
			using (viewer)
			{
				while (!viewer.WaitForExit(ViewerWait))
				{
					if (!Dialogs.AskRetry(Strings.ViewerStillOpen)) return false;
				}
			}
		}

		while (true)
		{
			DateTime until = DateTime.Now + OthersWait;
			List<string> open;
			while ((open = FolderProcesses()).Count > 0 && DateTime.Now < until)
			{
				Thread.Sleep(500);
			}

			if (open.Count == 0) return true;

			log.Write($"still open: {string.Join(", ", open)}");
			if (!Dialogs.AskRetry(Strings.ProcessesStillOpen(open))) return false;
		}
	}

	private static Process? Running(int id)
	{
		try
		{
			return Process.GetProcessById(id);
		}
		catch (ArgumentException)
		{
			// already gone
			return null;
		}
	}

	/// <summary>"KCViewer.exe (1234) 「title」" for each viewer or browser process started from this folder.</summary>
	private List<string> FolderProcesses()
	{
		List<string> open = [];
		foreach (string exe in new[] { UpdateFormat.ViewerFileName, UpdateFormat.BrowserFileName })
		{
			string path = Path.Combine(Folder, exe);
			foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exe)))
			{
				using (process)
				{
					try
					{
						if (process.HasExited || !string.Equals(process.MainModule?.FileName, path, StringComparison.OrdinalIgnoreCase)) continue;

						string title = process.MainWindowTitle;
						// 「실행파일명(pid) 프로그램명」 (2026-10-22), the window's title; a browser has none
						open.Add(title.Length > 0 ? $"{exe}({process.Id}) {title}" : $"{exe}({process.Id})");
					}
					catch (Exception)
					{
						// gone meanwhile, or not ours to read (another user's)
					}
				}
			}
		}

		return open;
	}
}
