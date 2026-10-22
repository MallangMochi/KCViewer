using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KCViewer.Avalonia.Translation;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.ViewModels.Translations;

namespace KCViewer.Utility;

public class SoftwareUpdater
{
	private static bool WaitForRestart { get; set; }

	public static SoftwareUpdateData CurrentDataVersion { get; set; } = new();
	public static SoftwareUpdateData LatestDataVersion { get; set; } = new();

	public static TranslationUpdateData CurrentTranslationVersion { get; set; } = new();
	public static TranslationUpdateData LatestTranslationVersion { get; set; } = new();

	private static string CurrentTranslationLanguage => Configuration.Config.UI.Culture switch
	{
		// Japanese translations don't exist, so fall back to English
		"ja-JP" => "en-US",
		string culture => culture,
	};

	private static Uri DataUpdateURL => new($"{Configuration.Config.Control.UpdateRepoURL}/update.json");

	// Fork: the translations come from the viewer's own repository (ForkSettings.Translation.Url), EO's data still from
	// UpdateRepoURL (user's decision 2026-10-07, mockup 20261007-translation-url-v2). Empty: no translations are fetched.
	private static string? TranslationRepoURL =>
		Fork.ForkSettings.Current.Translation.Url.Trim() is { Length: > 0 } url ? url.TrimEnd('/') + "/" : null;

	private static Uri? TranslationUpdateURL =>
		TranslationRepoURL is { } url ? new($"{url}Translations/{CurrentTranslationLanguage}/update.json") : null;

	/// <summary>Fork: whether this check got the translations' versions; without them no translation is fetched.</summary>
	private static bool TranslationsAvailable { get; set; }

	/// <summary>Fork: the last reason no translation was fetched, logged once instead of at every hourly check.</summary>
	private static string? TranslationSkipLogged { get; set; }

	/// <summary>Fork: our changes to EO's data at the translations' address (ForkDataOverrides); null when unread.</summary>
	private static Dictionary<string, int>? RemoteOverrideVersions { get; set; }

	private static string DataUpdateFile => Path.Join(DataConstants.WorkingFolder, "update.json");
	private static string TranslationUpdateFile => Path.Join(DataConstants.TranslationFolder(CurrentTranslationLanguage), "update.json");

	public static string DownloadProgressString { get; private set; } = "";

	public static SoftwareDownloadTranslationViewModel SoftwareDownload { get; } = new();

	/// <summary>
	/// Perform software update in background 
	/// </summary>
	public static async Task UpdateSoftware()
	{
		// Fork: an upstream release would overwrite the fork's files.
		if (Fork.ForkOptions.DisableSoftwareUpdate)
		{
			Logger.Add(2, Fork.ForkStrings.SoftwareUpdateDisabled);
			return;
		}

		if (WaitForRestart) return;

		if (!Directory.Exists(DataConstants.AppDataFolder))
			Directory.CreateDirectory(DataConstants.AppDataFolder);

		var url = LatestDataVersion.AppDownloadUrl;
		if (url != string.Empty)
		{
			try
			{
				Logger.Add(1, string.Format(SoftwareInformationResources.StartedDownloadingUpdate, url));
				await DownloadUpdate(url);
				Logger.Add(1, SoftwareInformationResources.DownloadFinished);
			}
			catch
			{
				return;
			}
		}

		try
		{
			await DownloadUpdater();
		}
		catch
		{
			return;
		}

		var updaterFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\EOUpdater.exe";
		if (!File.Exists(updaterFile)) return;
		var updater = new Process
		{
			StartInfo =
			{
				FileName = updaterFile,
				UseShellExecute = false,
				CreateNoWindow = false,
				Arguments = "--restart"
			}
		};
		updater.Start();
		Logger.Add(2, SoftwareInformationResources.CloseElectronicObserverToCompleteTheUpdate);
		WaitForRestart = true;
	}

	public static async Task PeriodicUpdateCheckAsync(CancellationToken cancellationToken)
	{
		while (true)
		{
			// Check for update every 1 hour.
			await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
			await CheckUpdateAsync();
		}
	}

	/// <summary>
	/// Check for update data, but only update translation data and game related data (Fit bonuses, equipment upgrades, ...)
	/// </summary>
	public static async Task CheckUpdateAsync()
	{
		// Fork: one check at a time; two at once (start, the settings button) wrote the same files together and failed.
		if (!await UpdateCheckGate.WaitAsync(0)) return;

		Directory.CreateDirectory(Path.GetDirectoryName(TranslationUpdateFile)!);
		Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(DataUpdateFile)!, "Data"));

		try
		{
			await ReadRemoteAndLocalUpdateData();

			List<(string FileName, DataType Type)> downloadList = GetDownloadList();

			// Fork: our changes to EO's data first (fetched, or removed when the address no longer has them); EO's files
			// fetched below are written with them, an EO file not fetched now is merged again from its kept copy (or
			// fetched when there is none)
			List<string> overrides = Fork.ForkDataOverrides.ChangedFiles(RemoteOverrideVersions);
			bool[] oursFetched = await Task.WhenAll(overrides.Select(name =>
				Fork.ForkDataOverrides.TryDownload(TranslationRepoURL!, name, RemoteOverrideVersions!.ContainsKey(name))));
			bool remerged = false;
			foreach (string name in overrides.Where(name => !downloadList.Contains((name, DataType.Data))))
			{
				if (Fork.ForkDataOverrides.Remerge(name)) remerged = true;
				else downloadList.Add((name, DataType.Data));
			}

			// Fork: each file on its own, so one that fails no longer stops the reload of the others
			// (after a language change the new translations stayed unread until the next version).
			bool[] downloaded = await Task.WhenAll(downloadList.Select(file => Task.Run(() => TryDownloadData(file.FileName, file.Type))));
			bool allDownloaded = downloaded.All(ok => ok) && oursFetched.All(ok => ok);

			// it's possible that one of the other files fails to download (exception gets thrown)
			// only update the update files after all files were downloaded successfully
			if (allDownloaded)
			{
				await DownloadData("update.json", DataType.None);

				if (TranslationsAvailable)
				{
					await DownloadData("update.json", DataType.Translation);
				}

				if (overrides.Count > 0)
				{
					Fork.ForkDataOverrides.WriteLocalVersions(RemoteOverrideVersions!);
				}
			}

			if (downloaded.Any(ok => ok) || remerged)
			{
				await KCDatabase.Instance.Translation.Initialize();
				KCDatabase.Instance.SystemQuestTrackerManager.Load();
				Logger.Add(2, SoftwareInformationResources.TranslationFilesUpdated);
			}

			if (allDownloaded)
			{
				CurrentDataVersion = LatestDataVersion;
				CurrentTranslationVersion = LatestTranslationVersion;
			}
		}
		catch (Exception e)
		{
			Logger.Add(3, SoftwareInformationResources.FailedToObtainUpdateData + e);
		}
		finally
		{
			UpdateCheckGate.Release();
		}
	}

	private static SemaphoreSlim UpdateCheckGate { get; } = new(1, 1);

	/// <summary>Fork: <see cref="DownloadData"/> logs a failure; the file is fetched again at the next check.</summary>
	private static async Task<bool> TryDownloadData(string fileName, DataType type)
	{
		try
		{
			await DownloadData(fileName, type);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static List<(string FileName, DataType Type)> GetDownloadList()
	{
		List<(string FileName, DataType Type)> downloadList = [];

		if (CurrentTranslationVersion.Equipment != LatestTranslationVersion.Equipment)
			downloadList.Add(("equipment.json", DataType.Translation));

		if (CurrentTranslationVersion.Expedition != LatestTranslationVersion.Expedition)
			downloadList.Add(("expedition.json", DataType.Translation));

		if (CurrentDataVersion.Destination != LatestDataVersion.Destination)
			downloadList.Add((("destination.json", DataType.Data)));

		if (CurrentTranslationVersion.Operation != LatestTranslationVersion.Operation)
			downloadList.Add(("operation.json", DataType.Translation));

		if (CurrentTranslationVersion.Quest != LatestTranslationVersion.Quest)
			downloadList.Add(("quest.json", DataType.Translation));

		if (CurrentTranslationVersion.Ship != LatestTranslationVersion.Ship)
			downloadList.Add(("ship.json", DataType.Translation));

		if (CurrentDataVersion.QuestTrackers < LatestDataVersion.QuestTrackers)
		{
			downloadList.Add(("QuestTrackers.json", DataType.Data));
		}

		if (CurrentDataVersion.QuestsMetadata < LatestDataVersion.QuestsMetadata)
		{
			downloadList.Add(("QuestsMetadata.json", DataType.Data));
		}

		if (CurrentDataVersion.EventLocks < LatestDataVersion.EventLocks)
		{
			downloadList.Add(("Locks.json", DataType.Data));
		}

		if (CurrentTranslationVersion.LockTranslations < LatestTranslationVersion.LockTranslations)
		{
			downloadList.Add(("Locks.json", DataType.Translation));
		}

		if (CurrentDataVersion.FitBonuses < LatestDataVersion.FitBonuses)
		{
			downloadList.Add(("FitBonuses.json", DataType.Data));
		}

		if (CurrentDataVersion.EquipmentUpgrades < LatestDataVersion.EquipmentUpgrades)
		{
			downloadList.Add(("EquipmentUpgrades.json", DataType.Data));
		}

		return downloadList;
	}

	/// <summary>
	/// Read remote (EO repository) and local update data to compare them later and download the required updates
	/// </summary>
	/// <returns></returns>
	private static async Task ReadRemoteAndLocalUpdateData()
	{
		using HttpClient client = new HttpClient();
		using HttpResponseMessage dataUpdateResponse = await client.GetAsync(DataUpdateURL);

		string dataUpdateData = await dataUpdateResponse.Content.ReadAsStringAsync();

		// Fork: the translations' versions from their own address, null when there are none
		string? translationUpdateData = await ReadTranslationUpdate(client);
		TranslationsAvailable = translationUpdateData is not null;

		// Fork: the versions of our changes to EO's data, from the same address
		RemoteOverrideVersions = await Fork.ForkDataOverrides.ReadRemoteVersions(client, TranslationRepoURL);

		bool updateDataReceived = !string.IsNullOrEmpty(dataUpdateData);

		if (updateDataReceived)
		{
			(LatestDataVersion, LatestTranslationVersion) = ParseUpdate(dataUpdateData, translationUpdateData ?? "{}");
		}

		// Fork: the local update files are no longer written here, before the files they list were downloaded; a
		// file that then failed was never fetched again. CheckUpdateAsync writes them once every file has arrived.
		// Each is read on its own: the translations' one is missing while no translation is fetched, which no longer
		// makes the data be fetched again at every check.
		string dataFileContent = File.Exists(DataUpdateFile) ? File.ReadAllText(DataUpdateFile) : "{}";
		string translationFileContent = File.Exists(TranslationUpdateFile) ? File.ReadAllText(TranslationUpdateFile) : "{}";
		(CurrentDataVersion, CurrentTranslationVersion) = ParseUpdate(dataFileContent, translationFileContent);

		// Fork: without the translations' versions the local ones stand, so no translation is fetched
		if (!TranslationsAvailable)
		{
			LatestTranslationVersion = CurrentTranslationVersion;
		}
	}

	/// <summary>Fork: the translations' update.json, null when the address is empty or it could not be read.</summary>
	private static async Task<string?> ReadTranslationUpdate(HttpClient client)
	{
		if (TranslationUpdateURL is not { } url)
		{
			LogTranslationSkip(Fork.ForkStrings.TranslationUrlEmpty);
			return null;
		}

		try
		{
			using HttpResponseMessage response = await client.GetAsync(url);
			response.EnsureSuccessStatusCode();

			string content = await response.Content.ReadAsStringAsync();
			if (string.IsNullOrWhiteSpace(content)) throw new InvalidDataException(Fork.ForkStrings.TranslationUpdateEmpty);

			TranslationSkipLogged = null;
			return content;
		}
		catch (Exception e)
		{
			LogTranslationSkip(Fork.ForkStrings.TranslationUpdateFailed(url.AbsoluteUri, e.Message.TrimEnd('.')));
			return null;
		}
	}

	private static void LogTranslationSkip(string message)
	{
		if (TranslationSkipLogged == message) return;

		TranslationSkipLogged = message;
		Logger.Add(2, message);
	}

	private static async Task DownloadUpdater()
	{
		try
		{
			using HttpClient client = new();

			var url = @"https://raw.githubusercontent.com/ElectronicObserverEN/Data/master/Data/EOUpdater.exe";
			var updaterFile = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\EOUpdater.exe";

			Progress<float> progress = new();
			progress.ProgressChanged += (_, progress) => DownloadProgressString = string.Format(SoftwareDownload.Update_DownloadingUpdater, progress);

			using FileStream file = new(updaterFile, FileMode.Create);
			await client.DownloadDataAsync(url, file, progress);

			Logger.Add(1, SoftwareInformationResources.UpdaterDownloadFinished);
		}
		catch (Exception e)
		{
			Logger.Add(3, SoftwareInformationResources.FailedToDownloadUpdater + e);
			throw;
		}
		finally
		{
			DownloadProgressString = "";
		}
	}

	private static async Task DownloadUpdate(string url)
	{
		try
		{
			using HttpClient client = new();
			string tempFile = Path.Join(DataConstants.AppDataFolder, "latest.zip");

			Console.WriteLine(SoftwareInformationResources.DownloadingUpdate);

			Progress<float> progress = new();
			progress.ProgressChanged += (_, progress) => DownloadProgressString = string.Format(SoftwareDownload.Update_DownloadingUpdate, progress);

			await using FileStream file = new(tempFile, FileMode.Create);
			await client.DownloadDataAsync(url, file, progress);
		}
		catch (Exception e)
		{
			Logger.Add(3, SoftwareInformationResources.FailedToDownloadElectronicObserver + e.Message);
			throw;
		}
		finally
		{
			DownloadProgressString = "";
		}
	}

	private static (SoftwareUpdateData, TranslationUpdateData) ParseUpdate(string dataJson, string translationJson)
	{
		try
		{
			SoftwareUpdateData? data = JsonSerializer.Deserialize<SoftwareUpdateData>(dataJson);
			TranslationUpdateData? translations = JsonSerializer.Deserialize<TranslationUpdateData>(translationJson);

			if (data is not null && translations is not null)
			{
				return (data, translations);
			}
		}
		catch (Exception e)
		{
			Logger.Add(3, SoftwareInformationResources.FailedToParseUpdateData + e.ToString());
		}

		return (new(), new());
	}
	
	
	private static string GetFullPath(string fileName, DataType type) => type switch
	{
		DataType.Translation => Path.Combine("Translations", CurrentTranslationLanguage, fileName),
		DataType.Data => Path.Combine("Data", fileName),
		DataType.None => fileName,
	};

	public static async Task DownloadData(string filename, DataType type)
	{
		// Fork: no translation address, no translation (a translation service asks for a missing file by itself)
		if (type is DataType.Translation && TranslationRepoURL is null)
		{
			LogTranslationSkip(Fork.ForkStrings.TranslationUrlEmpty);
			return;
		}

		string name = filename;
		filename = GetFullPath(filename, type);

		string path = Path.Combine(DataConstants.WorkingFolder, filename);
		string url = type switch
		{
			// Fork: the translations from their own address
			DataType.Translation => TranslationRepoURL + filename.Replace('\\', '/'),
			_ => Path.Combine(Configuration.Config.Control.UpdateRepoURL.AbsoluteUri, filename),
		};

		try
		{
			using HttpClient client = new();
			using HttpResponseMessage response = await client.GetAsync(url);

			response.EnsureSuccessStatusCode();

			string text = await response.Content.ReadAsStringAsync();

			// Fork: EO's data file kept as fetched and written with our changes applied (ForkDataOverrides)
			if (type is DataType.Data && Fork.ForkDataOverrides.Handles(name))
			{
				Fork.ForkDataOverrides.KeepUpstream(name, text);
				text = Fork.ForkDataOverrides.Apply(name, text);
			}

			// Fork: written next to it and then swapped in, so a failed write never leaves half a file
			string temporary = path + ".download";
			await File.WriteAllTextAsync(temporary, text);
			File.Move(temporary, path, overwrite: true);

			if (filename.Contains("update.json") == false)
			{
				Logger.Add(1, string.Format(SoftwareInformationResources.FileUpdated, filename));
			}
		}
		catch (Exception e)
		{
			Logger.Add(3, string.Format(SoftwareInformationResources.FailedToUpdateFile, filename, e.Message));
			throw;
		}
	}
}
