using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using KCViewer.Avalonia.Translation;
using KCViewer.Utility;

namespace KCViewer.Fork;

/// <summary>
/// Fork: our changes to the viewer's data, as the translations are ours (user's decision 2026-10-17 "가 로 진행하고
/// 번역처럼 우리가 데이터를 필요에 따라 변경한 내용이 적용되었으면 함"). EO's data files still come from the data address;
/// the entries the viewer's own repository keeps in Override/ (next to its translations) replace EO's entries with the
/// same key or are added. Only our entries are published, never EO's files (which carry no licence).
/// <para>
/// EO's file is kept as fetched in Data/Upstream, so a change of ours alone is merged again without fetching it;
/// Override/update.json numbers our files ({"QuestTrackers.json": 2, ...}), as the translations' update.json does.
/// Our file that cannot be applied leaves EO's file as it is (logged).
/// </para>
/// </summary>
public static class ForkDataOverrides
{
	/// <summary>
	/// The files that can be overridden and what tells two entries apart (EO's formats, 2026-10-17): a quest tracker by
	/// its quest, quest metadata by id, an equipment upgrade by eq_id, a fit bonus by the equipment it is for (ids or
	/// types), a lock by Id and a phase by Name, a map's routes by the map.
	/// </summary>
	private static readonly Dictionary<string, Func<JsonNode, JsonNode, (int Replaced, int Added)>> Mergers = new()
	{
		["QuestTrackers.json"] = (eo, ours) => MergeArray(eo, ours, n => n is JsonArray { Count: > 0 } a && a[0] is JsonArray { Count: > 0 } head ? head[0]?.ToJsonString() : null),
		["QuestsMetadata.json"] = (eo, ours) => MergeArray(eo, ours, n => n?["id"]?.ToJsonString()),
		["EquipmentUpgrades.json"] = (eo, ours) => MergeArray(eo, ours, n => n?["eq_id"]?.ToJsonString()),
		["FitBonuses.json"] = (eo, ours) => MergeArray(eo, ours, FitBonusTarget),
		["Locks.json"] = MergeLocks,
		["destination.json"] = MergeMaps,
	};

	public static IEnumerable<string> Files => Mergers.Keys;

	public static bool Handles(string name) => Mergers.ContainsKey(name);

	private static string OverrideFolder => Path.Join(DataConstants.DataFolder, "Override");
	private static string UpstreamFolder => Path.Join(DataConstants.DataFolder, "Upstream");
	private static string LocalVersionFile => Path.Join(OverrideFolder, "update.json");

	private static readonly JsonSerializerOptions Output = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

	/// <summary>The last reason our versions could not be read, logged once instead of at every hourly check.</summary>
	private static string? FailureLogged { get; set; }

	/// <summary>Our files' versions at the address: empty when it has none (404), null when they could not be read.</summary>
	public static async Task<Dictionary<string, int>?> ReadRemoteVersions(HttpClient client, string? repoUrl)
	{
		if (repoUrl is null) return null;

		try
		{
			using HttpResponseMessage response = await client.GetAsync(repoUrl + "Override/update.json");
			if (response.StatusCode is HttpStatusCode.NotFound) return [];
			response.EnsureSuccessStatusCode();

			Dictionary<string, int> versions = ParseVersions(await response.Content.ReadAsStringAsync());
			FailureLogged = null;
			return versions;
		}
		catch (Exception e)
		{
			string message = ForkStrings.DataOverrideVersionsFailed(e.Message.TrimEnd('.'));
			if (FailureLogged != message)
			{
				FailureLogged = message;
				Logger.Add(2, message);
			}

			return null;
		}
	}

	/// <summary>Our files whose version at the address differs from the one here, or that the address no longer has.</summary>
	public static List<string> ChangedFiles(Dictionary<string, int>? remote)
	{
		if (remote is null) return [];

		Dictionary<string, int> local = ReadLocalVersions();
		return remote.Keys.Concat(local.Keys).Distinct()
			.Where(Handles)
			.Where(name => !remote.TryGetValue(name, out int r) || !local.TryGetValue(name, out int l) || r != l)
			.ToList();
	}

	public static void WriteLocalVersions(Dictionary<string, int> versions)
	{
		Directory.CreateDirectory(OverrideFolder);
		File.WriteAllText(LocalVersionFile, JsonSerializer.Serialize(versions));
	}

	/// <summary>Our file fetched, or removed when the address no longer has it; false when it could not be fetched.</summary>
	public static async Task<bool> TryDownload(string repoUrl, string name, bool present)
	{
		string path = Path.Join(OverrideFolder, name);

		try
		{
			if (!present)
			{
				if (File.Exists(path)) File.Delete(path);
				return true;
			}

			using HttpClient client = new();
			using HttpResponseMessage response = await client.GetAsync(repoUrl + "Override/" + name);
			response.EnsureSuccessStatusCode();

			Directory.CreateDirectory(OverrideFolder);
			string temporary = path + ".download";
			await File.WriteAllTextAsync(temporary, await response.Content.ReadAsStringAsync());
			File.Move(temporary, path, overwrite: true);
			Logger.Add(1, ForkStrings.DataOverrideFetched(name));
			return true;
		}
		catch (Exception e)
		{
			Logger.Add(3, ForkStrings.DataOverrideFetchFailed(name, e.Message.TrimEnd('.')));
			return false;
		}
	}

	/// <summary>EO's file as fetched, kept to merge it again when only ours changes.</summary>
	public static void KeepUpstream(string name, string text)
	{
		Directory.CreateDirectory(UpstreamFolder);
		string path = Path.Join(UpstreamFolder, name);
		string temporary = path + ".download";
		File.WriteAllText(temporary, text);
		File.Move(temporary, path, overwrite: true);
	}

	/// <summary>
	/// The data file written again from the kept EO file with our entries; false when there is no kept file (EO's file
	/// has to be fetched, which applies them).
	/// </summary>
	public static bool Remerge(string name)
	{
		string upstream = Path.Join(UpstreamFolder, name);
		if (!File.Exists(upstream)) return false;

		string path = Path.Join(DataConstants.DataFolder, name);
		string temporary = path + ".download";
		File.WriteAllText(temporary, Apply(name, File.ReadAllText(upstream)));
		File.Move(temporary, path, overwrite: true);
		return true;
	}

	/// <summary>EO's text with our entries applied; EO's text itself when there are none or they cannot be applied.</summary>
	public static string Apply(string name, string eoText)
	{
		string path = Path.Join(OverrideFolder, name);
		if (!Mergers.TryGetValue(name, out var merge) || !File.Exists(path)) return eoText;

		try
		{
			JsonNode eo = Parse(eoText);
			JsonNode ours = Parse(File.ReadAllText(path));
			(int replaced, int added) = merge(eo, ours);
			Logger.Add(2, ForkStrings.DataOverrideApplied(name, replaced, added));
			return eo.ToJsonString(Output);
		}
		catch (Exception e)
		{
			Logger.Add(3, ForkStrings.DataOverrideFailed(name, e.Message.TrimEnd('.')));
			return eoText;
		}
	}

	private static JsonNode Parse(string text) =>
		JsonNode.Parse(text.TrimStart('﻿'), documentOptions: new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip })
		?? throw new InvalidDataException("empty");

	private static Dictionary<string, int> ReadLocalVersions() =>
		File.Exists(LocalVersionFile) ? ParseVersions(File.ReadAllText(LocalVersionFile)) : [];

	private static Dictionary<string, int> ParseVersions(string text) =>
		Parse(text).AsObject()
			.Where(p => p.Value is JsonValue v && v.TryGetValue(out int _))
			.ToDictionary(p => p.Key, p => p.Value!.GetValue<int>());

	/// <summary>Ours replace EO's entries with the same key in place, the others are added at the end.</summary>
	private static (int Replaced, int Added) MergeArray(JsonNode eo, JsonNode ours, Func<JsonNode?, string?> key)
	{
		JsonArray target = eo.AsArray();
		Dictionary<string, int> at = [];
		for (int i = 0; i < target.Count; i++)
		{
			if (key(target[i]) is { } k) at.TryAdd(k, i);
		}

		int replaced = 0, added = 0;
		foreach (JsonNode? item in ours.AsArray())
		{
			JsonNode? copy = item?.DeepClone();
			string? k = key(item);
			if (k is not null && at.TryGetValue(k, out int index))
			{
				target[index] = copy;
				replaced++;
				continue;
			}

			target.Add(copy);
			if (k is not null) at[k] = target.Count - 1;
			added++;
		}

		return (replaced, added);
	}

	/// <summary>The equipment a fit bonus is for (ids or types, numbers in any order).</summary>
	private static string? FitBonusTarget(JsonNode? node) => node is JsonObject o
		? string.Join(";", o.Where(p => p.Key != "bonuses").OrderBy(p => p.Key, StringComparer.Ordinal)
			.Select(p => p.Key + "=" + (p.Value is JsonArray a
				? string.Join(",", a.Select(v => v?.ToJsonString()).OrderBy(v => v, StringComparer.Ordinal))
				: p.Value?.ToJsonString())))
		: null;

	private static (int Replaced, int Added) MergeLocks(JsonNode eo, JsonNode ours)
	{
		(int r1, int a1) = ours["Locks"] is { } locks ? MergeArray(eo["Locks"]!, locks, n => n?["Id"]?.ToJsonString()) : (0, 0);
		(int r2, int a2) = ours["Phases"] is { } phases ? MergeArray(eo["Phases"]!, phases, n => n?["Name"]?.ToJsonString()) : (0, 0);
		return (r1 + r2, a1 + a2);
	}

	/// <summary>A map's routes replaced or added whole; the file's version stays EO's.</summary>
	private static (int Replaced, int Added) MergeMaps(JsonNode eo, JsonNode ours)
	{
		JsonObject target = eo.AsObject();
		int replaced = 0, added = 0;
		foreach ((string map, JsonNode? routes) in ours.AsObject())
		{
			if (map == "version") continue;
			if (target.ContainsKey(map)) replaced++;
			else added++;
			target[map] = routes?.DeepClone();
		}

		return (replaced, added);
	}
}
