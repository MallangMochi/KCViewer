using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using KCViewer.Fork.Update;

// The release files of the program update (UPDATER-DESIGN.md), called by fork-portable.ps1 -Release.
//   key <private key file> <public key file>
//       Makes the signing key once (ECDSA P-256): the private key (PKCS#8) and the public key (SubjectPublicKeyInfo),
//       base64. The private key never goes into a repository; the public key is Viewer/Portable/update-public-key.txt,
//       which the build puts into the viewer.
//   release <folder> <version> <commit> <out> <private key file> <package url base> [note file]
//       install-manifest.json into the built folder, <out>\KCViewer-<version>.zip (the folder under KCViewer/),
//       <out>\kcviewer-update.json (the zip at <package url base><zip name>) and its signature kcviewer-update.json.sig.
//   verify <out> <public key file>
//       Checks the release files the way the viewer does: the signature, the zip's size and hash, the list in the zip
//       (its version and every file's size and hash, nothing outside it).
try
{
	return args.FirstOrDefault() switch
	{
		"key" => MakeKey(args[1], args[2]),
		"release" => MakeRelease(args[1], args[2], args[3], args[4], args[5], args[6], args.Length > 7 ? args[7] : null),
		"verify" => Verify(args[1], args[2]),
		_ => Usage(),
	};
}
catch (Exception e) when (e is IOException or InvalidDataException or CryptographicException or IndexOutOfRangeException)
{
	Console.Error.WriteLine(e.Message);
	return 1;
}

static int Usage()
{
	Console.Error.WriteLine("key <private key file> <public key file> | release <folder> <version> <commit> <out> <private key file> <package url base> [note file] | verify <out> <public key file>");
	return 2;
}

static int MakeKey(string privateFile, string publicFile)
{
	if (File.Exists(privateFile)) throw new IOException($"{privateFile} is already there: a new key would not sign for the viewers that have the old one");
	Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(privateFile))!);
	Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(publicFile))!);
	using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
	File.WriteAllText(privateFile, Convert.ToBase64String(key.ExportPkcs8PrivateKey()));
	File.WriteAllText(publicFile, Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()));
	Console.WriteLine($"private key {privateFile}, public key {publicFile}");
	return 0;
}

static int MakeRelease(string folder, string version, string commit, string output, string privateFile, string urlBase, string? noteFile)
{
	InstallManifest manifest = new()
	{
		Version = version,
		Commit = commit,
		Files = ProgramFiles(folder).Select(file => new InstallFile
		{
			Path = UpdateFormat.ListPath(folder, file),
			Size = new FileInfo(file).Length,
			Sha256 = UpdateFormat.Sha256(file),
		}).ToList(),
	};
	if (UpdateFormat.CheckList(manifest) is { } problem) throw new InvalidDataException($"install list: {problem}");
	File.WriteAllBytes(Path.Combine(folder, UpdateFormat.InstallManifestFileName), JsonSerializer.SerializeToUtf8Bytes(manifest, UpdateJson.Default.InstallManifest));

	Directory.CreateDirectory(output);
	string zip = Path.Combine(output, $"KCViewer-{version}.zip");
	File.Delete(zip);
	using (ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Create))
	{
		foreach (string file in ProgramFiles(folder).Append(Path.Combine(folder, UpdateFormat.InstallManifestFileName)))
		{
			archive.CreateEntryFromFile(file, "KCViewer/" + UpdateFormat.ListPath(folder, file), CompressionLevel.Optimal);
		}
	}

	RemoteRelease release = new()
	{
		Version = version,
		Built = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
		Commit = commit,
		Note = noteFile is null ? "" : File.ReadAllText(noteFile).Trim(),
		MinUpdater = UpdateFormat.UpdaterProtocol,
		Package = new() { File = Path.GetFileName(zip), Url = urlBase + Path.GetFileName(zip), Size = new FileInfo(zip).Length, Sha256 = UpdateFormat.Sha256(zip) },
	};
	byte[] json = JsonSerializer.SerializeToUtf8Bytes(release, UpdateJson.Default.RemoteRelease);
	File.WriteAllBytes(Path.Combine(output, UpdateFormat.RemoteFileName), json);

	using ECDsa key = ECDsa.Create();
	key.ImportPkcs8PrivateKey(Convert.FromBase64String(File.ReadAllText(privateFile).Trim()), out _);
	File.WriteAllText(Path.Combine(output, UpdateFormat.SignatureFileName), Convert.ToBase64String(key.SignData(json, HashAlgorithmName.SHA256)));

	Console.WriteLine($"{manifest.Files.Count} files, {zip} ({release.Package.Size:N0} B), {UpdateFormat.RemoteFileName} signed");
	return 0;
}

static int Verify(string output, string publicFile)
{
	byte[] json = File.ReadAllBytes(Path.Combine(output, UpdateFormat.RemoteFileName));
	byte[] signature = Convert.FromBase64String(File.ReadAllText(Path.Combine(output, UpdateFormat.SignatureFileName)).Trim());
	using ECDsa key = ECDsa.Create();
	key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(File.ReadAllText(publicFile).Trim()), out _);
	if (!key.VerifyData(json, signature, HashAlgorithmName.SHA256)) throw new InvalidDataException("the signature does not match the public key");

	RemoteRelease release = JsonSerializer.Deserialize(json, UpdateJson.Default.RemoteRelease) ?? throw new InvalidDataException("empty remote json");
	string zip = Path.Combine(output, release.Package.File);
	if (new FileInfo(zip).Length != release.Package.Size || UpdateFormat.Sha256(zip) != release.Package.Sha256)
		throw new InvalidDataException("the zip's size or hash differs from the remote json");

	using ZipArchive archive = ZipFile.OpenRead(zip);
	ZipArchiveEntry listEntry = archive.GetEntry("KCViewer/" + UpdateFormat.InstallManifestFileName) ?? throw new InvalidDataException("no install list in the zip");
	InstallManifest manifest;
	using (Stream stream = listEntry.Open()) manifest = JsonSerializer.Deserialize(stream, UpdateJson.Default.InstallManifest) ?? throw new InvalidDataException("empty install list");
	if (manifest.Version != release.Version) throw new InvalidDataException($"install list {manifest.Version}, remote json {release.Version}");
	if (UpdateFormat.CheckList(manifest) is { } problem) throw new InvalidDataException($"install list: {problem}");

	Dictionary<string, InstallFile> listed = manifest.Files.ToDictionary(f => f.Path, StringComparer.OrdinalIgnoreCase);
	int checkedFiles = 0;
	foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.Length > 0 || !e.FullName.EndsWith('/')))
	{
		if (!entry.FullName.StartsWith("KCViewer/", StringComparison.Ordinal)) throw new InvalidDataException($"outside KCViewer/: {entry.FullName}");
		string path = entry.FullName["KCViewer/".Length..];
		if (path == UpdateFormat.InstallManifestFileName) continue;
		if (!listed.TryGetValue(path, out InstallFile? file)) throw new InvalidDataException($"not in the install list: {path}");
		using Stream stream = entry.Open();
		string hash = Convert.ToHexStringLower(SHA256.HashData(stream));
		if (entry.Length != file.Size || !hash.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"size or hash differs: {path}");
		checkedFiles++;
	}
	if (checkedFiles != manifest.Files.Count) throw new InvalidDataException($"the zip has {checkedFiles} of the list's {manifest.Files.Count} files");

	Console.WriteLine($"verified {release.Version} ({release.Commit}): signature, zip {release.Package.Size:N0} B, {checkedFiles} files");
	return 0;
}

// every file but the user folders, the update folder and the list itself (as the viewer and KCUpdater see them)
static string[] ProgramFiles(string folder) => Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
	.Where(file => UpdateFormat.IsProgramPath(UpdateFormat.ListPath(folder, file)))
	.OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
	.ToArray();
