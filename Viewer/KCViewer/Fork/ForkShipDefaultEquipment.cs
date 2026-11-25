using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using KCViewer.Avalonia.Translation;
using KCViewer.Data;
using KCViewer.Fork.KcvViews;
using KCViewer.Observer;
using KCViewer.Resource;
using KCViewer.Resource.Record;
using KCViewer.Utility;
using static KCViewer.Resource.Record.ShipParameterRecord;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the ships' default equipment as data of the viewer's repository, next to its translations (Data/KCViewer/,
/// user's decision 2026-11-25, handoff ship-default-equipment-data-20261125.md section 5, mockup
/// 20261125-default-equipment-v6 approved "시안 6대로 구현"). The default record in Assets.zip only starts a new
/// profile's record; after that the data is what fills and corrects the record.
/// <list type="bullet">
/// <item>Fetching: KCViewer/update.json ({"ShipDefaultEquipment.json": 2}) at every update check, the data itself only
/// when its version there differs from the one fetched before (user's decision "update 파일만 받다가 update의 버전이
/// 변경된게 확인되면 그때 받아오도록").</item>
/// <item>Comparing: when the data's version is above the one this profile last compared (Record/ShipDefaultEquipment.json,
/// which keeps that version's data too), once the game's master is in. A ship whose record is unknown, or still the
/// previous version's data (the default record at the first comparison), is updated and logged (갱신); a ship that
/// differs otherwise, as when it was recorded in play, goes to 자동 데이터 업데이트 수동 병합 (불일치), whose answer
/// ends the comparison of that version. Closing the viewer without answering asks again at the next start.</item>
/// <item>In play the record is used as before; a ship received or remodelled with other equipment than the data is
/// recorded as it is and logged (user's decision "관측 데이터를 적용하고 로그기록").</item>
/// <item>The ship album's 기본 장비 자료와 대조 compares the data fetched with the record again
/// (<see cref="CompareFromMenu"/>), in place of the old merge from the default record.</item>
/// </list>
/// Without the data (no translations' address, offline) the record is used as it is.
/// </summary>
public static class ForkShipDefaultEquipment
{
	public const string FileName = "ShipDefaultEquipment.json";

	private const int SlotCount = 5;

	private static string Folder => Path.Join(DataConstants.WorkingFolder, "KCViewer");
	private static string DataPath => Path.Join(Folder, FileName);
	private static string LocalVersionPath => Path.Join(Folder, "update.json");
	private static string ProfilePath => Path.Join(RecordManager.Instance.MasterPath, FileName);

	private static readonly JsonSerializerOptions Reading = new()
	{
		AllowTrailingCommas = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
	};

	/// <summary>The last reason the version could not be read, logged once instead of at every hourly check.</summary>
	private static string? FailureLogged { get; set; }

	/// <summary>The window of a comparison waiting for the user's answer.</summary>
	private static KcvDefaultEquipmentMergeWindow? OpenWindow { get; set; }

	/// <summary>The data this profile last compared, read once; null when it never compared.</summary>
	private static DefaultEquipmentData? Profile { get; set; }
	private static bool ProfileRead { get; set; }

	/// <summary>Compares again once the game's master is in (start, and each time the game is opened again).</summary>
	public static void Register()
	{
		APIObserver.Instance.ApiStart2_GetData.ResponseReceived += (_, _) =>
			Application.Current?.Dispatcher.BeginInvoke(Reconcile);
	}

	/// <summary>
	/// The data's version at the address and the data when the version changed; then the comparison. Called by the
	/// update check (start and every hour), in the background.
	/// </summary>
	public static async Task CheckAsync(string? repoUrl)
	{
		if (repoUrl is null) return;

		try
		{
			using HttpClient client = new();
			if (await ReadRemoteVersion(client, repoUrl) is { } version && (ReadLocalVersion() != version || !File.Exists(DataPath)))
			{
				using HttpResponseMessage response = await client.GetAsync(repoUrl + "KCViewer/" + FileName);
				response.EnsureSuccessStatusCode();
				string text = await response.Content.ReadAsStringAsync();

				// a file that cannot be read is not kept; the old one stays in use
				Parse(text, version);

				Directory.CreateDirectory(Folder);
				WriteReplacing(DataPath, text);
				WriteReplacing(LocalVersionPath, JsonSerializer.Serialize(new Dictionary<string, int> { [FileName] = version }));
				Logger.Add(2, ForkStrings.DefaultEquipmentFetched(version));
			}
		}
		catch (Exception e)
		{
			Logger.Add(3, ForkStrings.DefaultEquipmentFetchFailed(e.Message.TrimEnd('.')));
		}

		if (Application.Current is { } app)
		{
			await app.Dispatcher.InvokeAsync(Reconcile);
		}
	}

	/// <summary>The comparison of a version this profile has not compared yet, once the master is in.</summary>
	public static void Reconcile() => Compare(fromMenu: false);

	/// <summary>The ship album's 기본 장비 자료와 대조: the data fetched against the record, whatever was compared before.</summary>
	public static void CompareFromMenu() => Compare(fromMenu: true);

	/// <summary>
	/// A ship's default equipment recorded in play (received, built, remodelled): logged when the record changed to
	/// something other than the data this profile compared last (the record keeps it).
	/// </summary>
	public static void OnObserved(int shipId, int[]? before, IEnumerable<int> slots)
	{
		try
		{
			if (ReadProfile() is not { } profile) return;
			if (!profile.Ships.TryGetValue(shipId, out int[]? data)) return;
			if (!ObservedDiffers(before, slots.ToArray(), data)) return;

			string name = KCDatabase.Instance.MasterShips[shipId]?.NameWithClass ?? shipId.ToString();
			Logger.Add(2, ForkStrings.DefaultEquipmentObservedDiffers(profile.Version, name));
		}
		catch (Exception e)
		{
			Logger.Add(3, ForkStrings.DefaultEquipmentFailed(e.Message.TrimEnd('.')));
		}
	}

	/// <summary>Equipment seen in play is logged when it is not the data's and the record did not already hold it.</summary>
	public static bool ObservedDiffers(int[]? before, int[] observed, int[] data)
	{
		int[] seen = Normalize(observed)!;
		if (Same(seen, data)) return false;

		return Normalize(before) is not { } recorded || !Same(recorded, seen);
	}

	private static void Compare(bool fromMenu)
	{
		try
		{
			if (OpenWindow is { } open)
			{
				if (fromMenu) open.Activate();
				return;
			}

			if (KCDatabase.Instance.MasterShips.Count == 0)
			{
				if (fromMenu) Logger.Add(2, ForkStrings.DefaultEquipmentNeedsMaster);
				return;
			}

			if (ReadData() is not { } data)
			{
				if (fromMenu) Logger.Add(2, ForkStrings.DefaultEquipmentNoData);
				return;
			}

			DefaultEquipmentData? profile = ReadProfile();
			if (!fromMenu && profile is not null && profile.Version >= data.Version) return;

			IReadOnlyDictionary<int, int[]> previous = profile?.Ships ?? ReadDefaultRecord();
			ShipParameterRecord record = RecordManager.Instance.ShipParameter;

			// the names are the master's; a ship it does not have waits for a master that has it
			(List<int> updated, int matched, List<int> conflicts) = Classify(data.Ships, previous,
				id => record[id]?.DefaultSlot, KCDatabase.Instance.MasterShips.ContainsKey);

			foreach (int id in updated)
			{
				SetDefaultSlot(record, id, data.Ships[id]);
			}

			Logger.Add(2, ForkStrings.DefaultEquipmentCompared(data.Version, updated.Count, matched, conflicts.Count));
			if (updated.Count > 0)
			{
				Logger.Add(2, ForkStrings.DefaultEquipmentUpdatedShips(updated.Select(ShipName)));
				SaveRecord();
			}

			if (conflicts.Count == 0)
			{
				WriteProfile(data);
				if (fromMenu) Logger.Add(2, ForkStrings.DefaultEquipmentNoConflicts);
				return;
			}

			KcvDefaultEquipmentMergeViewModel viewModel = new(data.Version, profile?.Version, updated.Count, matched,
				conflicts.Select(id => new KcvDefaultEquipmentMergeRow(id, Normalize(record[id]?.DefaultSlot)!, data.Ships[id])));
			KcvDefaultEquipmentMergeWindow window = new(viewModel);
			window.Answered += applied => Resolve(data, conflicts, applied);
			window.Closed += (_, _) => OpenWindow = null;
			OpenWindow = window;
			window.ShowNotice();
		}
		catch (Exception e)
		{
			Logger.Add(3, ForkStrings.DefaultEquipmentFailed(e.Message.TrimEnd('.')));
		}
	}

	/// <summary>
	/// The comparison of a version (handoff ship-default-equipment-data-20261125.md section 5 ②, the user's 2026-11-25
	/// "등록과 갱신을 구분하지 말고 갱신 으로 통합"): a ship of <paramref name="data"/> whose record equals it matches (일치);
	/// one whose record is unknown, or equals <paramref name="previous"/> (the data compared before, or the default
	/// record), is updated (갱신); any other goes to the user (불일치). Ships <paramref name="known"/> rejects are left out.
	/// </summary>
	public static (List<int> Updated, int Matched, List<int> Conflicts) Classify(
		IReadOnlyDictionary<int, int[]> data,
		IReadOnlyDictionary<int, int[]> previous,
		Func<int, int[]?> recorded,
		Func<int, bool> known)
	{
		List<int> updated = [];
		List<int> conflicts = [];
		int matched = 0;
		foreach ((int id, int[] slots) in data.OrderBy(p => p.Key))
		{
			if (!known(id)) continue;

			int[]? current = Normalize(recorded(id));
			if (current is not null && Same(current, slots)) matched++;
			else if (current is null || previous.TryGetValue(id, out int[]? before) && Same(current, before)) updated.Add(id);
			else conflicts.Add(id);
		}

		return (updated, matched, conflicts);
	}

	/// <summary>The user's answer: the ships chosen get the data, the others keep the record; the version is compared.</summary>
	private static void Resolve(DefaultEquipmentData data, IReadOnlyList<int> conflicts, IReadOnlyCollection<int> applied)
	{
		try
		{
			ShipParameterRecord record = RecordManager.Instance.ShipParameter;
			foreach (int id in applied)
			{
				SetDefaultSlot(record, id, data.Ships[id]);
			}

			if (applied.Count > 0) SaveRecord();
			WriteProfile(data);

			Logger.Add(2, ForkStrings.DefaultEquipmentAnswered(data.Version,
				conflicts.Where(applied.Contains).Select(ShipName).ToList(),
				conflicts.Where(id => !applied.Contains(id)).Select(ShipName).ToList()));
		}
		catch (Exception e)
		{
			Logger.Add(3, ForkStrings.DefaultEquipmentFailed(e.Message.TrimEnd('.')));
		}

		// a version fetched while the window was open is compared now
		Application.Current?.Dispatcher.BeginInvoke(Reconcile);
	}

	private static string ShipName(int id) => KCDatabase.Instance.MasterShips[id]?.NameWithClass ?? id.ToString();

	private static void SetDefaultSlot(ShipParameterRecord record, int id, int[] slots)
	{
		ShipParameterElement element = record[id] ?? new ShipParameterElement { ShipID = id };
		element.DefaultSlot = slots.ToArray();
		record.Update(element);
	}

	/// <summary>
	/// The ship record written now, with the version compared, so the two never disagree (a record reloaded from its
	/// file, or a viewer that stops, would otherwise lose what the data put in while the version stays compared).
	/// </summary>
	private static void SaveRecord()
	{
		RecordManager.Instance.ShipParameter.SaveAll(RecordManager.Instance.MasterPath);
	}

	private static void WriteProfile(DefaultEquipmentData data)
	{
		Directory.CreateDirectory(RecordManager.Instance.MasterPath);
		WriteReplacing(ProfilePath, Serialize(data));
		Profile = data;
		ProfileRead = true;
	}

	private static DefaultEquipmentData? ReadProfile()
	{
		if (ProfileRead) return Profile;

		ProfileRead = true;
		try
		{
			if (File.Exists(ProfilePath)) Profile = Parse(File.ReadAllText(ProfilePath), null);
		}
		catch (Exception e)
		{
			// compared again as at the first time; the file is written anew then
			Logger.Add(3, ForkStrings.DefaultEquipmentProfileUnreadable(e.Message.TrimEnd('.')));
		}

		return Profile;
	}

	/// <summary>The data fetched, null when there is none or it cannot be read.</summary>
	private static DefaultEquipmentData? ReadData()
	{
		if (ReadLocalVersion() is not { } version || !File.Exists(DataPath)) return null;

		try
		{
			return Parse(File.ReadAllText(DataPath), version);
		}
		catch (Exception e)
		{
			Logger.Add(3, ForkStrings.DefaultEquipmentFailed(e.Message.TrimEnd('.')));
			return null;
		}
	}

	/// <summary>
	/// The default record's equipment (Assets.zip, Shift-JIS), what a profile's record was started with: the
	/// "previous version" of a profile that never compared.
	/// </summary>
	private static IReadOnlyDictionary<int, int[]> ReadDefaultRecord()
	{
		Dictionary<int, int[]> ships = [];

		try
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			using MemoryStream? stream = ResourceManager.GetStreamFromArchive("Record/" + RecordManager.Instance.ShipParameter.FileName);
			if (stream is null) return ships;

			stream.Position = 0;
			using StreamReader reader = new(stream, Encoding.GetEncoding(932));
			reader.ReadLine();
			while (reader.ReadLine() is { } line)
			{
				try
				{
					ShipParameterElement element = new(line);
					if (Normalize(element.DefaultSlot) is { } slots) ships[element.ShipID] = slots;
				}
				catch (Exception)
				{
					// a line that is not a ship
				}
			}
		}
		catch (Exception e)
		{
			Logger.Add(3, ForkStrings.DefaultEquipmentFailed(e.Message.TrimEnd('.')));
		}

		return ships;
	}

	/// <summary>The version at the address; null when it has none (404, no entry) or it could not be read.</summary>
	private static async Task<int?> ReadRemoteVersion(HttpClient client, string repoUrl)
	{
		try
		{
			using HttpResponseMessage response = await client.GetAsync(repoUrl + "KCViewer/update.json");
			if (response.StatusCode is HttpStatusCode.NotFound) return null;
			response.EnsureSuccessStatusCode();

			int? version = ParseVersion(await response.Content.ReadAsStringAsync());
			FailureLogged = null;
			return version;
		}
		catch (Exception e)
		{
			string message = ForkStrings.DefaultEquipmentVersionFailed(e.Message.TrimEnd('.'));
			if (FailureLogged != message)
			{
				FailureLogged = message;
				Logger.Add(2, message);
			}

			return null;
		}
	}

	private static int? ReadLocalVersion()
	{
		try
		{
			return File.Exists(LocalVersionPath) ? ParseVersion(File.ReadAllText(LocalVersionPath)) : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static int? ParseVersion(string text)
	{
		Dictionary<string, JsonElement>? versions = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(text.TrimStart('﻿'), Reading);
		return versions is not null && versions.TryGetValue(FileName, out JsonElement value) && value.TryGetInt32(out int version)
			? version
			: null;
	}

	/// <summary>A data file (fetched, <paramref name="version"/> from update.json) or this profile's copy (its own version).</summary>
	private static DefaultEquipmentData Parse(string text, int? version)
	{
		DataFile file = JsonSerializer.Deserialize<DataFile>(text.TrimStart('﻿'), Reading)
			?? throw new InvalidDataException("empty");

		Dictionary<int, int[]> ships = [];
		Dictionary<int, string> names = [];
		foreach (DataShip ship in file.Ships ?? [])
		{
			if (ship.Slots is not { Length: SlotCount } slots) continue;

			ships[ship.Id] = slots.ToArray();
			names[ship.Id] = ship.Name ?? "";
		}

		if (ships.Count == 0) throw new InvalidDataException("no ships");

		return new(version ?? file.Version ?? throw new InvalidDataException("no version"), file.Master, ships, names);
	}

	/// <summary>One ship a line, as the repository's file.</summary>
	private static string Serialize(DefaultEquipmentData data)
	{
		StringBuilder text = new();
		text.Append("{\n\t\"version\": ").Append(data.Version).Append(",\n");
		text.Append("\t\"master\": ").Append(JsonSerializer.Serialize(data.Master ?? "", Writing)).Append(",\n");
		text.Append("\t\"ships\": [\n");
		text.AppendJoin(",\n", data.Ships.OrderBy(p => p.Key).Select(p =>
			$"\t\t{{\"id\": {p.Key}, \"name\": {JsonSerializer.Serialize(data.Names.GetValueOrDefault(p.Key, ""), Writing)}, \"slots\": [{string.Join(", ", p.Value)}]}}"));
		text.Append("\n\t]\n}\n");
		return text.ToString();
	}

	private static readonly JsonSerializerOptions Writing = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

	private static void WriteReplacing(string path, string text)
	{
		string temporary = path + ".download";
		File.WriteAllText(temporary, text);
		File.Move(temporary, path, overwrite: true);
	}

	/// <summary>Five slots, -1 for none; null for unknown.</summary>
	private static int[]? Normalize(int[]? slots) => slots switch
	{
		null => null,
		{ Length: SlotCount } => slots,
		_ => Enumerable.Range(0, SlotCount).Select(i => i < slots.Length ? slots[i] : -1).ToArray(),
	};

	private static bool Same(int[] a, int[] b) => a.AsSpan().SequenceEqual(b);

	private sealed class DataFile
	{
		[JsonPropertyName("version")]
		public int? Version { get; set; }

		[JsonPropertyName("master")]
		public string? Master { get; set; }

		[JsonPropertyName("ships")]
		public List<DataShip>? Ships { get; set; }
	}

	private sealed class DataShip
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("slots")]
		public int[]? Slots { get; set; }
	}
}

/// <summary>A version of the default equipment data: each ship's five slots (-1 for none) and its name as written.</summary>
public sealed record DefaultEquipmentData(
	int Version,
	string? Master,
	IReadOnlyDictionary<int, int[]> Ships,
	IReadOnlyDictionary<int, string> Names);
