using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using KCViewer.Fork.Update;

namespace KCUpdater;

/// <summary>
/// Fork: started by the viewer from <c>Update\KCUpdater.exe</c> (a copy, so the folder's own KCUpdater.exe can be
/// replaced too) with <see cref="UpdaterArguments"/>. Writes <c>Update\last-result.json</c> for the viewer's next start,
/// shows a message box when the update did not happen, and starts the viewer again unless told not to.
/// Exit code: 0 updated, 1 not updated, 2 bad arguments, 3 another update of this folder is running.
/// </summary>
internal static class Program
{
	private static int Main(string[] args)
	{
		Options options;
		try
		{
			options = Options.Parse(args);
		}
		catch (ArgumentException)
		{
			// the viewer's language if it was given, so the message reads as the viewer does
			if (args.LastOrDefault(arg => arg.StartsWith(UpdaterArguments.Language, StringComparison.Ordinal)) is { } language)
			{
				Strings.Language = language[UpdaterArguments.Language.Length..];
			}

			Dialogs.Error(Strings.BadArguments);
			return 2;
		}

		Strings.Language = options.Language;
		Dialogs.Enabled = options.Dialogs;

		using Mutex mutex = new(false, MutexName(options.Folder));
		try
		{
			if (!mutex.WaitOne(0)) return 3;
		}
		catch (AbandonedMutexException)
		{
			// the last updater of this folder ended without letting go: this one owns it now
		}

		string updateFolder = Path.Combine(options.Folder, UpdateFormat.UpdateFolderName);
		using Log log = new(Path.Combine(updateFolder, UpdateFormat.UpdaterLogFileName));
		log.Write($"KCUpdater: folder {options.Folder}, staging {options.Staging}, viewer process {options.ProcessId?.ToString() ?? "-"}");

		Dialogs.Logged = log.Write;
		Updater updater = new(options, log);
		UpdateResult result = updater.Run();

		try
		{
			UpdateFormat.WriteResult(Path.Combine(updateFolder, UpdateFormat.ResultFileName), result);
		}
		catch (Exception e)
		{
			log.Write($"could not write the result: {e.Message}");
		}

		log.Write($"result: {result.Outcome} {result.From} -> {result.To}, {result.Replaced} replaced, {result.Removed} removed. {result.Message}");

		switch (result.Outcome)
		{
			case UpdateOutcome.NotStarted when !updater.Cancelled:
				Dialogs.Warning(result.Message);
				break;
			case UpdateOutcome.RolledBack:
			case UpdateOutcome.RollbackFailed:
				Dialogs.Error(result.Message);
				break;
		}

		if (options.Restart) StartViewer(options, log);

		return result.Outcome == UpdateOutcome.Updated ? 0 : 1;
	}

	/// <summary>One updater per portable folder; hashed because mutex names are limited in length.</summary>
	private static string MutexName(string folder) =>
		"KCUpdater-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(folder.ToUpperInvariant())))[..32];

	/// <summary>The viewer as it was started, whatever happened: the new version, or the old one put back.</summary>
	private static void StartViewer(Options options, Log log)
	{
		string viewer = Path.Combine(options.Folder, UpdateFormat.ViewerFileName);
		try
		{
			ProcessStartInfo start = new(viewer) { WorkingDirectory = options.Folder, UseShellExecute = false };
			foreach (string argument in options.RestartArguments) start.ArgumentList.Add(argument);

			Process.Start(start)?.Dispose();
			log.Write($"started {viewer} {string.Join(' ', options.RestartArguments)}");
		}
		catch (Exception e)
		{
			log.Write($"could not start {viewer}: {e.Message}");
			Dialogs.Error(Strings.RestartFailed(viewer));
		}
	}
}

/// <summary>The arguments (<see cref="UpdaterArguments"/>).</summary>
internal sealed class Options
{
	/// <summary>The portable folder, full path without a trailing separator.</summary>
	public required string Folder { get; init; }

	/// <summary>The checked new files, inside <c>Update\staging</c> of <see cref="Folder"/>.</summary>
	public required string Staging { get; init; }

	public int? ProcessId { get; init; }

	public string Language { get; init; } = "ko-KR";

	public bool Restart { get; init; } = true;

	public bool Dialogs { get; init; } = true;

	public IReadOnlyList<string> RestartArguments { get; init; } = [];

	public static Options Parse(string[] args)
	{
		int split = Array.IndexOf(args, UpdaterArguments.RestartArguments);
		string[] own = split < 0 ? args : args[..split];
		string[] restart = split < 0 ? [] : args[(split + 1)..];

		string? Value(string prefix) => own.LastOrDefault(arg => arg.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];

		string folder = FullPath(Value(UpdaterArguments.Folder) ?? throw new ArgumentException(UpdaterArguments.Folder));
		string staging = FullPath(Value(UpdaterArguments.Staging) ?? throw new ArgumentException(UpdaterArguments.Staging));
		if (!Directory.Exists(folder)) throw new ArgumentException(folder);

		// the new files are only ever taken from this folder's own staging area
		string stagingRoot = Path.Combine(folder, UpdateFormat.UpdateFolderName, UpdateFormat.StagingFolderName) + Path.DirectorySeparatorChar;
		if (!staging.StartsWith(stagingRoot, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(staging)) throw new ArgumentException(staging);

		int? processId = null;
		if (Value(UpdaterArguments.ProcessId) is { } id)
		{
			processId = int.TryParse(id, out int value) ? value : throw new ArgumentException(UpdaterArguments.ProcessId + id);
		}

		return new Options
		{
			Folder = folder,
			Staging = staging,
			ProcessId = processId,
			Language = Value(UpdaterArguments.Language) ?? "ko-KR",
			Restart = !own.Contains(UpdaterArguments.NoRestart),
			Dialogs = !own.Contains(UpdaterArguments.NoDialogs),
			RestartArguments = restart,
		};
	}

	private static string FullPath(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}

/// <summary>Update\updater.log: every step, for when something went wrong. Started over once it passes 1 MB.</summary>
internal sealed class Log : IDisposable
{
	private readonly StreamWriter? writer;

	public Log(string file)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(file)!);
			bool append = !File.Exists(file) || new FileInfo(file).Length < 1 << 20;
			writer = new StreamWriter(file, append, new UTF8Encoding(false)) { AutoFlush = true };
		}
		catch
		{
			// no log rather than no update
		}
	}

	public void Write(string line) => writer?.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {line}");

	public void Dispose() => writer?.Dispose();
}
