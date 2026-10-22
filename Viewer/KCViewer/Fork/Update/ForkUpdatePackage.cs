using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KCViewer.Fork.Update;

/// <summary>
/// Fork: step 2 of UPDATER-DESIGN.md without the viewer's screens: the release's small file read and its signature
/// checked, the zip fetched (resumed when a download broke off) and checked against it, then unpacked into the staging
/// folder and every file checked against the zip's own list. Uses nothing but <see cref="UpdateFormat"/>, so it can be
/// tried on its own.
/// </summary>
public static class ForkUpdatePackage
{
	/// <summary>
	/// The release's small file from <paramref name="baseUrl"/> (ending with "/"), when its signature is right for
	/// <paramref name="publicKey"/> (the base64 of a SubjectPublicKeyInfo, 8-2).
	/// </summary>
	public static async Task<RemoteRelease> ReadReleaseAsync(HttpClient client, string baseUrl, string publicKey, CancellationToken token = default)
	{
		byte[] data = await client.GetByteArrayAsync(baseUrl + UpdateFormat.RemoteFileName, token);
		string signature = await client.GetStringAsync(baseUrl + UpdateFormat.SignatureFileName, token);
		if (!SignatureIsRight(data, signature, publicKey)) throw new UpdatePackageException(UpdatePackageError.Signature);

		RemoteRelease release = JsonSerializer.Deserialize(data, UpdateJson.Default.RemoteRelease)
			?? throw new UpdatePackageException(UpdatePackageError.BadRelease);
		if (release.Package is not { File.Length: > 0, Url.Length: > 0, Sha256.Length: 64, Size: > 0 } package
			|| package.File.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
		{
			throw new UpdatePackageException(UpdatePackageError.BadRelease);
		}

		return release;
	}

	/// <summary>Whether <paramref name="signature"/> (base64, ECDSA P-256 / SHA-256, IEEE P1363) signs <paramref name="data"/>.</summary>
	public static bool SignatureIsRight(byte[] data, string signature, string publicKey)
	{
		try
		{
			using ECDsa key = ECDsa.Create();
			key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
			return key.VerifyData(data, Convert.FromBase64String(signature.Trim()), HashAlgorithmName.SHA256);
		}
		catch (Exception e) when (e is FormatException or CryptographicException)
		{
			return false;
		}
	}

	/// <summary>
	/// The release's zip in <paramref name="folder"/> (Update\download), its size and hash checked: kept when an earlier
	/// download already finished, continued from its <c>.part</c> file when one broke off (a range request; started over
	/// when the server sends the whole file). <paramref name="progress"/> gets the bytes there so far.
	/// </summary>
	public static async Task<string> DownloadAsync(HttpClient client, ReleasePackage package, string folder,
		IProgress<long>? progress = null, CancellationToken token = default)
	{
		Directory.CreateDirectory(folder);
		string target = Path.Combine(folder, package.File);
		string part = target + ".part";

		if (File.Exists(target))
		{
			if (IsPackage(target, package)) return target;
			File.Delete(target);
		}

		long have = File.Exists(part) ? new FileInfo(part).Length : 0;
		if (have >= package.Size)
		{
			File.Delete(part);
			have = 0;
		}

		using HttpRequestMessage request = new(HttpMethod.Get, package.Url);
		if (have > 0) request.Headers.Range = new RangeHeaderValue(have, null);

		using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
		response.EnsureSuccessStatusCode();
		if (have > 0 && response.StatusCode != HttpStatusCode.PartialContent) have = 0;

		await using (FileStream file = new(part, have > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16))
		await using (Stream body = await response.Content.ReadAsStreamAsync(token))
		{
			byte[] buffer = new byte[1 << 16];
			progress?.Report(have);
			int read;
			while ((read = await body.ReadAsync(buffer, token)) > 0)
			{
				have += read;
				if (have > package.Size) break;

				await file.WriteAsync(buffer.AsMemory(0, read), token);
				progress?.Report(have);
			}
		}

		if (!IsPackage(part, package))
		{
			File.Delete(part);
			throw new UpdatePackageException(UpdatePackageError.Package);
		}

		File.Move(part, target, overwrite: true);
		return target;
	}

	private static bool IsPackage(string file, ReleasePackage package) =>
		new FileInfo(file).Length == package.Size && UpdateFormat.Sha256(file).Equals(package.Sha256, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// The zip unpacked into <paramref name="staging"/> (emptied first) without its top folder (KCViewer/), then checked
	/// (step 2-3): its list is there and is for <paramref name="version"/>, and every file matches it with nothing extra.
	/// </summary>
	public static InstallManifest Unpack(string zip, string staging, string version)
	{
		if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
		Directory.CreateDirectory(staging);
		string stagingRoot = Path.GetFullPath(staging).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

		using (ZipArchive archive = ZipFile.OpenRead(zip))
		{
			ZipArchiveEntry[] files = archive.Entries.Where(entry => entry.Name.Length > 0).ToArray();
			string? top = TopFolder(files);

			foreach (ZipArchiveEntry entry in files)
			{
				string path = entry.FullName.Replace('\\', '/');
				if (top is not null) path = path[(top.Length + 1)..];

				// only program files and the list: nothing outside the folder, nothing in a user folder
				if (path != UpdateFormat.InstallManifestFileName && !UpdateFormat.IsProgramPath(path))
				{
					throw new UpdatePackageException(UpdatePackageError.File, new(UpdateProblemKind.BadPath, entry.FullName));
				}

				string target = Path.GetFullPath(UpdateFormat.LocalPath(staging, path));
				if (!target.StartsWith(stagingRoot, StringComparison.OrdinalIgnoreCase))
				{
					throw new UpdatePackageException(UpdatePackageError.File, new(UpdateProblemKind.BadPath, entry.FullName));
				}

				Directory.CreateDirectory(Path.GetDirectoryName(target)!);
				entry.ExtractToFile(target);
			}
		}

		string list = Path.Combine(staging, UpdateFormat.InstallManifestFileName);
		if (!File.Exists(list)) throw new UpdatePackageException(UpdatePackageError.File, new(UpdateProblemKind.Missing, UpdateFormat.InstallManifestFileName));

		InstallManifest manifest = UpdateFormat.ReadInstallManifest(list);
		if (manifest.Version != version) throw new UpdatePackageException(UpdatePackageError.Version);
		if (UpdateFormat.CheckFolder(staging, manifest) is { } problem) throw new UpdatePackageException(UpdatePackageError.File, problem);

		return manifest;
	}

	/// <summary>The one folder every file of the zip is in (the portable's KCViewer/), or null.</summary>
	private static string? TopFolder(ZipArchiveEntry[] files)
	{
		string[] tops = files.Select(entry => entry.FullName.Replace('\\', '/')).Select(name => name.Contains('/') ? name[..name.IndexOf('/')] : "").Distinct().ToArray();
		return tops is [{ Length: > 0 } top] ? top : null;
	}
}

public enum UpdatePackageError
{
	/// <summary>The release's small file is not signed with our key.</summary>
	Signature,

	/// <summary>The release's small file cannot be read as one.</summary>
	BadRelease,

	/// <summary>The zip's size or hash differs from the release's small file.</summary>
	Package,

	/// <summary>The zip's list is for another version.</summary>
	Version,

	/// <summary>A file of the zip is wrong (<see cref="UpdatePackageException.Problem"/>).</summary>
	File,
}

public sealed class UpdatePackageException(UpdatePackageError error, UpdateProblem? problem = null)
	: Exception($"{error} {problem}")
{
	public UpdatePackageError Error { get; } = error;

	public UpdateProblem? Problem { get; } = problem;
}
