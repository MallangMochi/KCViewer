using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KCViewer.Fork.Update;

/// <summary>
/// Fork: the program update's files and rules (UPDATER-DESIGN.md, sections 3 and 4), shared by the viewer and KCUpdater.
/// KCUpdater links this file (KCUpdater/KCUpdater.csproj) and is published with NativeAOT: no reflection, so the json
/// goes through <see cref="UpdateJson"/> and nothing here may use the viewer's other code.
/// </summary>
public static class UpdateFormat
{
	/// <summary>The release's small file the viewer checks (section 4, user's decision 2026-10-19: the compromise, as a release file).</summary>
	public const string RemoteFileName = "kcviewer-update.json";

	/// <summary>The base64 of an ECDSA P-256 / SHA-256 signature (IEEE P1363) of <see cref="RemoteFileName"/>'s bytes (8-2).</summary>
	public const string SignatureFileName = RemoteFileName + ".sig";

	/// <summary>The program files' list: at the top of the zip and, once installed, of the portable folder.</summary>
	public const string InstallManifestFileName = "install-manifest.json";

	public const string UpdaterFileName = "KCUpdater.exe";

	/// <summary>The viewer and its browser process (renamed from KCViewer.exe and EOBrowser.exe on 2026-10-22).</summary>
	public const string ViewerFileName = "KCViewer.exe";
	public const string BrowserFileName = "KCBrowser.exe";

	/// <summary>The update's working folder inside the portable folder (principle 1): downloads, staging and backups.</summary>
	public const string UpdateFolderName = "Update";
	public const string DownloadFolderName = "download";
	public const string StagingFolderName = "staging";
	public const string BackupFolderName = "backup";
	public const string ResultFileName = "last-result.json";
	public const string UpdaterLogFileName = "updater.log";

	/// <summary>
	/// How this viewer starts KCUpdater (its arguments, <see cref="UpdaterArguments"/>). A release whose
	/// <see cref="RemoteRelease.MinUpdater"/> is higher is installed by hand: this viewer could not start its updater.
	/// </summary>
	public const int UpdaterProtocol = 1;

	/// <summary>The user's folders at the top of the portable folder: in no list and never touched (principle 2).</summary>
	public static IReadOnlyList<string> UserFolders { get; } = ["Data", "Plugins", "Skins", "GameAssets", UpdateFolderName];

	/// <summary>
	/// Whether a list's path is a program file's: relative with forward slashes, no empty, "." or ".." part, not the list
	/// itself and not in a user folder.
	/// </summary>
	public static bool IsProgramPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || path.Contains('\\') || path.Contains(':') || path.StartsWith('/')) return false;

		string[] parts = path.Split('/');
		if (parts.Any(part => part.Length == 0 || part is "." or ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)) return false;
		if (parts.Length == 1 && parts[0].Equals(InstallManifestFileName, StringComparison.OrdinalIgnoreCase)) return false;

		return parts.Length == 1 || !UserFolders.Contains(parts[0], StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>A list's path in a folder.</summary>
	public static string LocalPath(string folder, string path) => Path.Combine(folder, path.Replace('/', Path.DirectorySeparatorChar));

	/// <summary>A folder's file as a list's path (forward slashes).</summary>
	public static string ListPath(string folder, string file) => Path.GetRelativePath(folder, file).Replace(Path.DirectorySeparatorChar, '/');

	/// <summary>The lowercase hex SHA-256 of a file.</summary>
	public static string Sha256(string file)
	{
		using FileStream stream = new(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20);
		return Convert.ToHexStringLower(SHA256.HashData(stream));
	}

	/// <summary>
	/// Compares two release versions, "1.2.3" or "1.2.3-rc1" (section 4, user's decision 2026-10-22): the three numbers in
	/// turn, then a version without a suffix above one with it, then the suffixes as text. Null when either is not a
	/// version.
	/// </summary>
	public static int? CompareVersions(string? a, string? b)
	{
		if (ParseVersion(a) is not { } x || ParseVersion(b) is not { } y) return null;

		for (int i = 0; i < 3; i++)
		{
			if (x.Numbers[i] != y.Numbers[i]) return x.Numbers[i].CompareTo(y.Numbers[i]);
		}

		return (x.Suffix, y.Suffix) switch
		{
			(null, null) => 0,
			(null, _) => 1,
			(_, null) => -1,
			_ => Math.Sign(string.CompareOrdinal(x.Suffix, y.Suffix)),
		};
	}

	private static (int[] Numbers, string? Suffix)? ParseVersion(string? version)
	{
		if (string.IsNullOrWhiteSpace(version)) return null;

		int dash = version.IndexOf('-');
		string core = dash < 0 ? version : version[..dash];
		string? suffix = dash < 0 ? null : version[(dash + 1)..];
		string[] parts = core.Split('.');
		if (parts.Length != 3 || suffix is "") return null;

		int[] numbers = new int[3];
		for (int i = 0; i < 3; i++)
		{
			if (!int.TryParse(parts[i], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out numbers[i])) return null;
		}

		return (numbers, suffix);
	}

	public static InstallManifest ReadInstallManifest(string file) =>
		JsonSerializer.Deserialize(File.ReadAllBytes(file), UpdateJson.Default.InstallManifest)
		?? throw new InvalidDataException(file);

	public static void WriteResult(string file, UpdateResult result)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(file)!);
		File.WriteAllBytes(file, JsonSerializer.SerializeToUtf8Bytes(result, UpdateJson.Default.UpdateResult));
	}

	/// <summary>
	/// The first thing wrong with a list (bad or repeated paths, a hash that is not SHA-256), or null. A list without the
	/// viewer or the updater cannot be an update.
	/// </summary>
	public static UpdateProblem? CheckList(InstallManifest manifest)
	{
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
		foreach (InstallFile file in manifest.Files)
		{
			if (!IsProgramPath(file.Path) || !seen.Add(file.Path)) return new(UpdateProblemKind.BadPath, file.Path);
			if (file.Size < 0 || file.Sha256.Length != 64 || !file.Sha256.All(Uri.IsHexDigit)) return new(UpdateProblemKind.BadEntry, file.Path);
		}

		foreach (string required in new[] { ViewerFileName, UpdaterFileName })
		{
			if (!seen.Contains(required)) return new(UpdateProblemKind.Missing, required);
		}

		return null;
	}

	/// <summary>
	/// The first difference between a folder of new files and its list, or null: every listed file there with its size
	/// and hash, nothing else apart from the list itself.
	/// </summary>
	public static UpdateProblem? CheckFolder(string folder, InstallManifest manifest)
	{
		if (CheckList(manifest) is { } problem) return problem;

		HashSet<string> listed = new(manifest.Files.Select(file => file.Path), StringComparer.OrdinalIgnoreCase) { InstallManifestFileName };
		foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
		{
			string path = ListPath(folder, file);
			if (!listed.Contains(path)) return new(UpdateProblemKind.Unlisted, path);
		}

		foreach (InstallFile file in manifest.Files)
		{
			FileInfo info = new(LocalPath(folder, file.Path));
			if (!info.Exists) return new(UpdateProblemKind.Missing, file.Path);
			if (info.Length != file.Size) return new(UpdateProblemKind.SizeMismatch, file.Path);
			if (!Sha256(info.FullName).Equals(file.Sha256, StringComparison.OrdinalIgnoreCase)) return new(UpdateProblemKind.HashMismatch, file.Path);
		}

		return null;
	}
}

/// <summary>The release's small file (<see cref="UpdateFormat.RemoteFileName"/>).</summary>
public sealed class RemoteRelease
{
	/// <summary>The release's version, "1.0.0" (section 4).</summary>
	public string Version { get; set; } = "";

	/// <summary>When it was built (ISO 8601, UTC).</summary>
	public string Built { get; set; } = "";

	public string Commit { get; set; } = "";

	/// <summary>What changed, shown with the update.</summary>
	public string Note { get; set; } = "";

	/// <summary>The lowest <see cref="UpdateFormat.UpdaterProtocol"/> that can install it.</summary>
	public int MinUpdater { get; set; }

	public ReleasePackage Package { get; set; } = new();
}

/// <summary>The release's zip: the whole portable folder with its list at the top (section 3).</summary>
public sealed class ReleasePackage
{
	public string File { get; set; } = "";

	/// <summary>The zip at the release's own tag, not at "latest": a newer release mid-download changes nothing.</summary>
	public string Url { get; set; } = "";

	public long Size { get; set; }

	public string Sha256 { get; set; } = "";
}

/// <summary>The program files of one version (<see cref="UpdateFormat.InstallManifestFileName"/>).</summary>
public sealed class InstallManifest
{
	public string Version { get; set; } = "";

	public string Commit { get; set; } = "";

	public List<InstallFile> Files { get; set; } = [];
}

public sealed class InstallFile
{
	/// <summary>Relative to the portable folder, with forward slashes (<see cref="UpdateFormat.IsProgramPath"/>).</summary>
	public string Path { get; set; } = "";

	public long Size { get; set; }

	public string Sha256 { get; set; } = "";
}

/// <summary>What KCUpdater did (<see cref="UpdateFormat.ResultFileName"/>), logged by the viewer at its next start.</summary>
public sealed class UpdateResult
{
	public UpdateOutcome Outcome { get; set; }

	public string From { get; set; } = "";

	public string To { get; set; } = "";

	public int Replaced { get; set; }

	public int Removed { get; set; }

	/// <summary>Why it stopped, in the viewer's language; empty when it succeeded.</summary>
	public string Message { get; set; } = "";

	public DateTimeOffset Time { get; set; }
}

public enum UpdateOutcome
{
	/// <summary>The new version is in place.</summary>
	Updated,

	/// <summary>Stopped before any file changed (the window was not closed, the folder has no list, a file was wrong).</summary>
	NotStarted,

	/// <summary>A file could not be replaced and every file was put back.</summary>
	RolledBack,

	/// <summary>A file could not be replaced and putting the old files back failed too: some are in Update\backup.</summary>
	RollbackFailed,
}

public enum UpdateProblemKind
{
	BadPath,
	BadEntry,
	Missing,
	Unlisted,
	SizeMismatch,
	HashMismatch,
}

public sealed record UpdateProblem(UpdateProblemKind Kind, string Path);

/// <summary>
/// KCUpdater's arguments (<see cref="UpdateFormat.UpdaterProtocol"/> 1): <c>--folder=</c> the portable folder,
/// <c>--staging=</c> the checked new files, <c>--pid=</c> the viewer to wait for, <c>--lang=</c> the viewer's language,
/// <c>--no-restart</c> when the viewer was being closed anyway, and after <c>--</c> the viewer's own arguments for the
/// restart.
/// </summary>
public static class UpdaterArguments
{
	public const string Folder = "--folder=";
	public const string Staging = "--staging=";
	public const string ProcessId = "--pid=";
	public const string Language = "--lang=";
	public const string NoRestart = "--no-restart";

	/// <summary>Messages are logged and every question answered "no": for scripted tests.</summary>
	public const string NoDialogs = "--no-dialogs";

	public const string RestartArguments = "--";
}

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	WriteIndented = true,
	UseStringEnumConverter = true)]
[JsonSerializable(typeof(RemoteRelease))]
[JsonSerializable(typeof(InstallManifest))]
[JsonSerializable(typeof(UpdateResult))]
public partial class UpdateJson : JsonSerializerContext
{
}
