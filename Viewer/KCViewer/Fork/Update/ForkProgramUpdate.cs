using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KCViewer.Utility;

namespace KCViewer.Fork.Update;

/// <summary>
/// Fork: the viewer's own program updates (UPDATER-DESIGN.md, steps 1 to 3 and 5), from the releases of our public
/// repository (<see cref="ForkSettings.ProgramUpdateSettings"/>). The screens read <see cref="State"/> and call the
/// steps; nothing here shows a window.
/// <list type="bullet">
/// <item>Checked at start while 「업데이트 확인」 is on, and from the menu whatever it says (user's decision 2026-10-19).</item>
/// <item>Only the live instance looks (2026-10-19, 13번); a build without the release key never does (8-2).</item>
/// <item>A development build only learns the latest release (9번). A release fetches a newer one at once after the
/// automatic check, after the user's choice after the menu's (10번).</item>
/// <item>A folder without its install list, or a release whose updater this one cannot start, is installed by hand
/// (2026-10-22 "목록 누락으로 수동 재설치 권장 알림 띄우고 중단").</item>
/// </list>
/// </summary>
public static class ForkProgramUpdate
{
	public static ProgramUpdateState State { get; private set; } = ProgramUpdateState.Idle;

	/// <summary>The latest release once a check has read it.</summary>
	public static RemoteRelease? Latest { get; private set; }

	/// <summary>Why <see cref="ProgramUpdateState.ManualInstall"/>.</summary>
	public static ManualInstallReason ManualReason { get; private set; }

	/// <summary>The zip's bytes there so far while <see cref="ProgramUpdateState.Downloading"/>.</summary>
	public static long Downloaded { get; private set; }

	/// <summary>What went wrong, while <see cref="ProgramUpdateState.Failed"/>.</summary>
	public static string Error { get; private set; } = "";

	/// <summary>Which step failed, while <see cref="ProgramUpdateState.Failed"/>: the check (no version known) or the download.</summary>
	public static ProgramUpdateFailure FailedDuring { get; private set; }

	/// <summary>Raised on any thread when <see cref="State"/> or <see cref="Downloaded"/> changes.</summary>
	public static event Action? Changed;

	/// <summary>Whether this instance looks for program updates at all: the live one, with a key and an address.</summary>
	public static bool Enabled => IsLiveInstance && PublicKey is not null && BaseUrl is not null;

	private static bool IsLiveInstance => ForkProfile.Name is null && !ForkProfile.IsEmbedded;

	private static string? PublicKey => ForkVersion.UpdatePublicKey;

	private static string? BaseUrl =>
		ForkSettings.Current.ProgramUpdate.Url.Trim() is { Length: > 0 } url ? url.TrimEnd('/') + "/" : null;

	private static string PortableFolder { get; } = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
	private static string UpdateFolder => Path.Combine(PortableFolder, UpdateFormat.UpdateFolderName);
	private static string DownloadFolder => Path.Combine(UpdateFolder, UpdateFormat.DownloadFolderName);
	private static string StagingRoot => Path.Combine(UpdateFolder, UpdateFormat.StagingFolderName);
	private static string BackupFolder => Path.Combine(UpdateFolder, UpdateFormat.BackupFolderName);
	private static string ResultFile => Path.Combine(UpdateFolder, UpdateFormat.ResultFileName);

	/// <summary>The checked new files once <see cref="ProgramUpdateState.Ready"/>.</summary>
	private static string? Staging { get; set; }

	/// <summary>No timeout: the zip is large. A check that hangs only keeps <see cref="State"/> at Checking.</summary>
	private static HttpClient Client { get; } = CreateClient();

	private static SemaphoreSlim Gate { get; } = new(1, 1);

	private static bool NoKeyLogged { get; set; }

	private static HttpClient CreateClient()
	{
		HttpClient client = new() { Timeout = Timeout.InfiniteTimeSpan };
		client.DefaultRequestHeaders.UserAgent.ParseAdd($"{ForkVersion.ProductName}/{ForkVersion.ReleaseVersion ?? ForkVersion.CommitText ?? "dev"}");
		return client;
	}

	/// <summary>
	/// At start, once the main window is up: the last update's result logged and its leftovers removed, then the
	/// automatic check while 「업데이트 확인」 (<c>Life.CheckUpdateInformation</c>) is on.
	/// </summary>
	public static void Start()
	{
		if (!IsLiveInstance) return;

		ReportLastResult();

		// an earlier start's unpacked files: unpacked again from the kept zip when needed
		TryDelete(StagingRoot);

		if (Configuration.Config.Life.CheckUpdateInformation) _ = CheckAsync(manual: false);
	}

	/// <summary>
	/// Reads the latest release and decides (see the class). <paramref name="manual"/>: from the menu, so the result is
	/// logged where the user sees it and a newer release waits for <see cref="DownloadAsync"/>.
	/// </summary>
	public static async Task CheckAsync(bool manual)
	{
		if (!IsLiveInstance) return;

		if (PublicKey is null || BaseUrl is not { } baseUrl)
		{
			if (manual || !NoKeyLogged) Logger.Add(manual ? 2 : 1, ForkStrings.ProgramUpdateOff);
			NoKeyLogged = true;
			return;
		}

		// a download under way or done is not thrown away by a new look
		if (State is ProgramUpdateState.Downloading or ProgramUpdateState.Ready)
		{
			if (manual) Logger.Add(2, State == ProgramUpdateState.Ready ? ForkStrings.ProgramUpdateReady(Latest!.Version) : ForkStrings.ProgramUpdateDownloading(Latest!.Version, Latest.Package.Size));
			return;
		}

		if (!await Gate.WaitAsync(0)) return;

		try
		{
			Set(ProgramUpdateState.Checking);

			// the small file only: a check that hangs gives up instead of staying at Checking
			using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
			RemoteRelease latest = await ForkUpdatePackage.ReadReleaseAsync(Client, baseUrl, PublicKey, timeout.Token);
			Latest = latest;

			if (ForkVersion.ReleaseVersion is not { } current)
			{
				Logger.Add(manual ? 2 : 1, ForkStrings.ProgramUpdateDevelopmentBuild(latest.Version));
				Set(ProgramUpdateState.DevelopmentBuild);
				return;
			}

			switch (UpdateFormat.CompareVersions(latest.Version, current))
			{
				case null:
					Fail(ForkStrings.ProgramUpdateBadVersion(latest.Version), ProgramUpdateFailure.Check);
					return;
				case <= 0:
					Logger.Add(manual ? 2 : 1, ForkStrings.ProgramUpdateUpToDate(current));
					Set(ProgramUpdateState.UpToDate);
					return;
			}

			if (latest.MinUpdater > UpdateFormat.UpdaterProtocol || !File.Exists(Path.Combine(PortableFolder, UpdateFormat.InstallManifestFileName)))
			{
				ManualReason = latest.MinUpdater > UpdateFormat.UpdaterProtocol ? ManualInstallReason.NewerUpdater : ManualInstallReason.NoInstallList;
				Logger.Add(3, ForkStrings.ProgramUpdateManualInstall(latest.Version, ManualReason));
				Set(ProgramUpdateState.ManualInstall);
				return;
			}

			Logger.Add(2, ForkStrings.ProgramUpdateAvailable(latest.Version, current));
			Set(ProgramUpdateState.Available);
		}
		catch (Exception e)
		{
			Fail(ForkStrings.ProgramUpdateCheckFailed(Describe(e)), ProgramUpdateFailure.Check);
			return;
		}
		finally
		{
			Gate.Release();
		}

		if (!manual) await DownloadAsync();
	}

	/// <summary>Fetches and unpacks the newer release (step 2): <see cref="ProgramUpdateState.Ready"/> once every file is checked.</summary>
	public static async Task DownloadAsync()
	{
		if (State != ProgramUpdateState.Available || Latest is not { } release) return;

		Downloaded = 0;
		Set(ProgramUpdateState.Downloading);
		Logger.Add(2, ForkStrings.ProgramUpdateDownloading(release.Version, release.Package.Size));

		try
		{
			RemoveOtherDownloads(release.Package.File);
			string zip = await ForkUpdatePackage.DownloadAsync(Client, release.Package, DownloadFolder, new DownloadProgress(release.Package.Size));

			string staging = Path.Combine(StagingRoot, release.Version);
			await Task.Run(() => ForkUpdatePackage.Unpack(zip, staging, release.Version));
			Staging = staging;

			Logger.Add(2, ForkStrings.ProgramUpdateReady(release.Version));
			Set(ProgramUpdateState.Ready);
		}
		catch (Exception e)
		{
			Fail(ForkStrings.ProgramUpdateDownloadFailed(release.Version, Describe(e)), ProgramUpdateFailure.Download);
		}
	}

	/// <summary>
	/// 「다시 시도」 after a failure: the download again when that failed (the release is known), else the check, as
	/// from the menu.
	/// </summary>
	public static Task RetryAsync()
	{
		if (State != ProgramUpdateState.Failed) return Task.CompletedTask;

		if (FailedDuring == ProgramUpdateFailure.Download && Latest is not null)
		{
			Set(ProgramUpdateState.Available);
			return DownloadAsync();
		}

		return CheckAsync(manual: true);
	}

	/// <summary>
	/// Starts KCUpdater for the checked new version (step 3), from a copy in Update\ so the folder's own one can be
	/// replaced too. True when it runs: the caller then closes the viewer, which the updater waits for.
	/// <paramref name="restart"/>: the viewer starts again afterwards (false when it was being closed anyway).
	/// </summary>
	public static bool StartUpdater(bool restart)
	{
		if (State != ProgramUpdateState.Ready || Staging is not { } staging || Latest is not { } release) return false;

		try
		{
			string updater = Path.Combine(UpdateFolder, UpdateFormat.UpdaterFileName);
			File.Copy(Path.Combine(staging, UpdateFormat.UpdaterFileName), updater, overwrite: true);

			ProcessStartInfo start = new(updater) { UseShellExecute = false, WorkingDirectory = UpdateFolder };
			start.ArgumentList.Add(UpdaterArguments.Folder + PortableFolder);
			start.ArgumentList.Add(UpdaterArguments.Staging + staging);
			start.ArgumentList.Add(UpdaterArguments.ProcessId + Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
			start.ArgumentList.Add(UpdaterArguments.Language + CultureInfo.CurrentUICulture.Name);
			if (!restart) start.ArgumentList.Add(UpdaterArguments.NoRestart);
			start.ArgumentList.Add(UpdaterArguments.RestartArguments);
			foreach (string argument in Environment.GetCommandLineArgs().Skip(1)) start.ArgumentList.Add(argument);

			Process.Start(start)?.Dispose();
			Logger.Add(2, ForkStrings.ProgramUpdateUpdaterStarted(release.Version));
			return true;
		}
		catch (Exception e)
		{
			Logger.Add(3, ForkStrings.ProgramUpdateUpdaterFailed(e.Message));
			return false;
		}
	}

	/// <summary>KCUpdater's result of the last update in the log, then its files removed (step 5).</summary>
	private static void ReportLastResult()
	{
		if (!File.Exists(ResultFile)) return;

		UpdateOutcome? outcome = null;
		try
		{
			UpdateResult result = JsonSerializer.Deserialize(File.ReadAllBytes(ResultFile), UpdateJson.Default.UpdateResult)
				?? throw new InvalidDataException(ResultFile);
			outcome = result.Outcome;
			Logger.Add(result.Outcome == UpdateOutcome.Updated ? 2 : 3, ForkStrings.ProgramUpdateResult(result));
		}
		catch (Exception e)
		{
			Logger.Add(3, ForkStrings.ProgramUpdateResultUnreadable(e.Message));
		}

		TryDelete(ResultFile);
		if (outcome == UpdateOutcome.Updated) TryDelete(DownloadFolder);

		// after a failed undo the old files wait in Update\backup for the user
		if (outcome != UpdateOutcome.RollbackFailed) TryDelete(BackupFolder);
	}

	/// <summary>Zips of other versions (a release that was replaced meanwhile).</summary>
	private static void RemoveOtherDownloads(string keep)
	{
		if (!Directory.Exists(DownloadFolder)) return;

		foreach (string file in Directory.EnumerateFiles(DownloadFolder))
		{
			string name = Path.GetFileName(file);
			if (name != keep && name != keep + ".part") TryDelete(file);
		}
	}

	private static void Fail(string message, ProgramUpdateFailure step)
	{
		Error = message;
		FailedDuring = step;
		Logger.Add(3, message);
		Set(ProgramUpdateState.Failed);
	}

	private static void Set(ProgramUpdateState state)
	{
		State = state;
		Changed?.Invoke();
	}

	private static string Describe(Exception e) => e is UpdatePackageException package ? ForkStrings.ProgramUpdatePackageError(package) : e.Message;

	private static void TryDelete(string path)
	{
		try
		{
			if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
			else if (File.Exists(path)) File.Delete(path);
		}
		catch (Exception e)
		{
			Logger.Add(1, $"{path}: {e.Message}");
		}
	}

	/// <summary><see cref="Downloaded"/> on the download's own thread, <see cref="Changed"/> once per half percent.</summary>
	private sealed class DownloadProgress(long size) : IProgress<long>
	{
		private long reported = -1;

		public void Report(long value)
		{
			Downloaded = value;
			if (reported >= 0 && value - reported < size / 200 && value < size) return;

			reported = value;
			Changed?.Invoke();
		}
	}
}

public enum ProgramUpdateState
{
	/// <summary>Not looked yet, or this instance does not look (<see cref="ForkProgramUpdate.Enabled"/>).</summary>
	Idle,

	Checking,

	/// <summary>The latest release is not newer.</summary>
	UpToDate,

	/// <summary>A development build: the latest release is only made known (<see cref="ForkProgramUpdate.Latest"/>).</summary>
	DevelopmentBuild,

	/// <summary>Newer, but this folder has to be installed by hand (<see cref="ForkProgramUpdate.ManualReason"/>).</summary>
	ManualInstall,

	/// <summary>Newer, waiting for the user to download it (after the menu's check).</summary>
	Available,

	Downloading,

	/// <summary>Downloaded and checked: the update happens when the viewer restarts or closes.</summary>
	Ready,

	/// <summary>The check or the download failed (<see cref="ForkProgramUpdate.Error"/>); the next check tries again.</summary>
	Failed,
}

public enum ProgramUpdateFailure
{
	/// <summary>The release's small file could not be read or checked: no version is known.</summary>
	Check,

	/// <summary>The zip could not be fetched, or a file in it was wrong.</summary>
	Download,
}

public enum ManualInstallReason
{
	/// <summary>The folder has no install-manifest.json.</summary>
	NoInstallList,

	/// <summary>The release needs a newer way of starting its updater than this version knows.</summary>
	NewerUpdater,
}
