using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Data;
using KCViewer.Database;
using KCViewer.Database.KancolleApi;
using KCViewer.Database.Sortie;
using KCViewer.Resource.Record;
using KCViewer.Utility;
using KCViewer.Window.Tools.SortieRecordViewer;
using Microsoft.EntityFrameworkCore;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the search conditions of a record window as they were when it opened, for its 초기화 button ("초기화를 누르면
/// 검색 조건을 초기상태도 되돌리도록 함", 2026-10-04). Properties are set back in the given order, so a condition that
/// resets another (the sea area resets the expedition list) comes first. A name with a dot is a nested property.
/// </summary>
public sealed class KcvConditionSnapshot(object target, params string[] names)
{
	private object?[] Values { get; set; } = [];

	public void Capture() => Values = names
		.Select(n =>
		{
			object owner = Resolve(n, out PropertyInfo property);
			return property.GetValue(owner);
		})
		.ToArray();

	public void Restore()
	{
		for (int i = 0; i < names.Length && i < Values.Length; i++)
		{
			object owner = Resolve(names[i], out PropertyInfo property);
			property.SetValue(owner, Values[i]);
		}
	}

	private object Resolve(string name, out PropertyInfo property)
	{
		object owner = target;
		string[] parts = name.Split('.');
		for (int i = 0; i < parts.Length - 1; i++)
		{
			owner = owner.GetType().GetProperty(parts[i])!.GetValue(owner)!;
		}

		property = owner.GetType().GetProperty(parts[^1])
			?? throw new ArgumentException($"{owner.GetType().Name} has no property {parts[^1]}");
		return owner;
	}
}

/// <summary>A ship of a recorded fleet: the name and a grey level, as the record tables show it.</summary>
public sealed record KcvRecordShip(string Name, int Level);

public static class KcvRecordFleets
{
	/// <summary>The sortie's fleet, as the viewer's sortie record picks it (SortieRecordViewModel).</summary>
	public static SortieFleet? SortieFleet(SortieRecord sortie)
	{
		SortieFleetData data = sortie.FleetData;
		return data.Fleets.Count >= data.FleetId && data.FleetId > 0
			? data.Fleets[data.FleetId - 1]
			: data.Fleets.FirstOrDefault();
	}

	public static List<KcvRecordShip> Ships(SortieFleet? fleet) => fleet?.Ships
		.Select(s => new KcvRecordShip(KCDatabase.Instance.MasterShips[(int)s.Id]?.NameEN ?? $"#{(int)s.Id}", s.Level))
		.ToList() ?? [];
}

/// <summary>
/// The kinds of map nodes the sortie route colours (mockup v5: 통상 전투 red, 보스 dark red, ...); the compass uses them
/// too (mockup 20261005-compass-route-v4: night battles purple).
/// </summary>
public enum KcvNodeKind
{
	None,
	Battle,
	Night,
	Boss,
	Air,
	Resource,
	Escort,
	Imagination,
	Maelstrom,
	Landing,
}

/// <summary>
/// A node of a sortie route: its letter, kind and the tooltip 「G · 공습전」 with the items got or lost there and the
/// rank of its battle; the nodes after the first have an arrow before them. The compass outlines the last one, the
/// cell the fleet is at.
/// </summary>
public sealed record KcvRouteNode(string Letter, KcvNodeKind Kind, string ToolTip, bool IsFirst, bool IsLast = false);

/// <summary>
/// Fork: what the sortie record's route and the compass's route say about a node (mockup 20261005-compass-route-v4):
/// 「G · 공습전」, the items got (resource, escort success) or lost (maelstrom) in the viewer's compass words
/// 「연료 x 40」, and 「랭크 A」.
/// </summary>
public static class KcvRouteText
{
	/// <summary>「G · 통상 전투」, 「G · 공습전」 (the viewer's names, as the compass shows them); the items and the rank under it.</summary>
	public static string ToolTip(string letter, int eventId, int eventKind, string? items, string? rank)
	{
		List<string> lines = [$"{letter} · {Kind(eventId, eventKind)}"];
		if (!string.IsNullOrEmpty(items)) lines.Add(items);
		if (!string.IsNullOrEmpty(rank)) lines.Add($"{ForkStrings.KcvRecordRank} {rank}");
		return string.Join("\n", lines);
	}

	/// <summary>The kind of a cell in the viewer's names 「통상 전투」, 「공습전」, 「통상 전투/야전」 (also the battle detail's cells).</summary>
	public static string Kind(int eventId, int eventKind) => eventId switch
	{
		4 when eventKind is 4 or 6 => Constants.GetMapEventKind(eventKind),
		4 or 5 when eventKind >= 2 => $"{Constants.GetMapEventID(eventId)}/{Constants.GetMapEventKind(eventKind)}",
		_ => Constants.GetMapEventID(eventId),
	};

	/// <summary>
	/// The items of a map/start or map/next answer as the viewer's compass writes them (GetMaterialInfo): api_itemget
	/// (one or a list) or api_itemget_eo_comment, 「연료 x 40, 탄약 x 20」; a maelstrom's loss (api_happening) 「탄약 x 20」.
	/// </summary>
	public static string? Items(JsonElement data)
	{
		List<string> items = [];

		foreach (string name in new[] { "api_itemget", "api_itemget_eo_comment" })
		{
			if (!data.TryGetProperty(name, out JsonElement got)) continue;

			IEnumerable<JsonElement> list = got.ValueKind switch
			{
				JsonValueKind.Array => got.EnumerateArray(),
				JsonValueKind.Object => [got],
				_ => [],
			};
			items.AddRange(list.Select(i => Item(Int(i, "api_usemst"), Int(i, "api_id"), Int(i, "api_getcount"))));
			break;
		}

		if (data.TryGetProperty("api_happening", out JsonElement lost) && lost.ValueKind is JsonValueKind.Object)
		{
			items.Add($"{Constants.GetMaterialName(Int(lost, "api_mst_id"))} x {Int(lost, "api_count")}");
		}

		return items.Count > 0 ? string.Join(", ", items) : null;
	}

	/// <summary>The compass's items (CompassData.GetItems), as <see cref="Items(JsonElement)"/>.</summary>
	public static string? Items(IEnumerable<CompassData.GetItemData> got)
	{
		List<string> items = got.Select(i => Item(i.ItemID, i.Metadata, i.Amount)).ToList();
		return items.Count > 0 ? string.Join(", ", items) : null;
	}

	// api_usemst 4 is a material (1 fuel ... 8 improvement material), else a use item
	private static string Item(int usemst, int id, int count)
	{
		string name = usemst is 4
			? Constants.GetMaterialName(id)
			: KCDatabase.Instance.MasterUseItems[id]?.NameTranslated ?? CompassResources.UnknownItem;
		return $"{name} x {count}";
	}

	private static int Int(JsonElement data, string name) =>
		data.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.Number ? value.GetInt32() : -1;
}

/// <summary>
/// The routes of the sorties in the sortie record table, read from the record database in the background: the
/// map/start and map/next responses (node, event) and the battle results (rank), newest sorties first. The table
/// cells ask by sortie ID and are refreshed through <see cref="Version"/> after each batch.
/// </summary>
public sealed class KcvSortieRoutes : ObservableObject
{
	private const int BatchSize = 100;

	private ConcurrentDictionary<int, IReadOnlyList<KcvRouteNode>> Routes { get; } = new();
	private CancellationTokenSource? Loading { get; set; }

	public int Version { get; private set; }

	public IReadOnlyList<KcvRouteNode>? Get(int sortieId) => Routes.TryGetValue(sortieId, out IReadOnlyList<KcvRouteNode>? route) ? route : null;

	public void Load(IReadOnlyList<SortieRecordViewModel> sorties)
	{
		Cancel();

		List<(int Id, int World, int Map)> missing = sorties
			.Where(s => !Routes.ContainsKey(s.Id))
			.Select(s => (s.Id, s.World, s.Map))
			.ToList();
		if (missing.Count is 0) return;

		CancellationTokenSource cts = new();
		Loading = cts;
		Task.Run(() => LoadBatches(missing, cts.Token));
	}

	public void Cancel()
	{
		Loading?.Cancel();
		Loading = null;
	}

	private async Task LoadBatches(List<(int Id, int World, int Map)> sorties, CancellationToken ct)
	{
		try
		{
			foreach ((int Id, int World, int Map)[] batch in sorties.Chunk(BatchSize))
			{
				ct.ThrowIfCancellationRequested();

				List<int> ids = batch.Select(s => s.Id).ToList();
				await using KCViewerContext db = new();
				var files = await db.ApiFiles
					.AsNoTracking()
					.Where(f => f.SortieRecordId != null && ids.Contains(f.SortieRecordId.Value))
					.Where(f => f.ApiFileType == ApiFileType.Response)
					.Where(f => f.Name == "api_req_map/start" || f.Name == "api_req_map/next" ||
						f.Name == "api_req_sortie/battleresult" || f.Name == "api_req_combined_battle/battleresult")
					.Select(f => new { f.Id, SortieId = f.SortieRecordId!.Value, f.Name, f.Content })
					.ToListAsync(ct);

				foreach ((int id, int world, int map) in batch)
				{
					Routes[id] = Route(world, map, files.Where(f => f.SortieId == id).OrderBy(f => f.Id).Select(f => (f.Name, f.Content)));
				}

				await Application.Current.Dispatcher.BeginInvoke(() => Version++);
			}
		}
		catch (OperationCanceledException)
		{
			// a new search or the window closed
		}
		catch (Exception e)
		{
			Logger.Add(2, $"Fork: sortie routes could not be read: {e.Message}");
		}
	}

	private sealed class Node
	{
		public required string Letter { get; init; }
		public required int EventId { get; init; }
		public required int EventKind { get; init; }
		public string? Items { get; init; }
		public string? Rank { get; set; }
	}

	private static IReadOnlyList<KcvRouteNode> Route(int world, int map, IEnumerable<(string Name, string Content)> files)
	{
		List<Node> nodes = [];
		foreach ((string name, string content) in files)
		{
			try
			{
				using JsonDocument json = JsonDocument.Parse(content);
				if (!json.RootElement.TryGetProperty("api_data", out JsonElement data) || data.ValueKind is not JsonValueKind.Object) continue;

				if (name.StartsWith("api_req_map/", StringComparison.Ordinal))
				{
					int cell = Int(data, "api_no");
					nodes.Add(new Node
					{
						Letter = KCDatabase.Instance.Translation.Destination.CellDisplay(world, map, cell),
						EventId = Int(data, "api_event_id"),
						EventKind = Int(data, "api_event_kind"),
						Items = KcvRouteText.Items(data),
					});
				}
				else if (nodes.Count > 0 && data.TryGetProperty("api_win_rank", out JsonElement rank))
				{
					nodes[^1].Rank = rank.GetString();
				}
			}
			catch (JsonException)
			{
				// a damaged file: the node is left out
			}
		}

		return nodes
			.Select((n, i) => new KcvRouteNode(n.Letter, Kind(n.EventId, n.EventKind), KcvRouteText.ToolTip(n.Letter, n.EventId, n.EventKind, n.Items, n.Rank), i == 0))
			.ToList();
	}

	private static int Int(JsonElement data, string name) =>
		data.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.Number ? value.GetInt32() : -1;

	/// <summary>
	/// api_event_id 0 初期位置, 1 イベントなし, 2 資源, 3 渦潮, 4 通常戦闘, 5 ボス, 6 気のせい, 7 航空戦, 8 船団護衛成功,
	/// 9 揚陸地点, 10 泊地; api_event_kind 2 夜戦, 3 夜昼戦, 4 航空戦, 6 長距離空襲戦, 7 夜昼戦(対連合). A boss stays a boss
	/// at night.
	/// </summary>
	public static KcvNodeKind Kind(int eventId, int eventKind) => eventId switch
	{
		5 => KcvNodeKind.Boss,
		4 when eventKind is 4 or 6 => KcvNodeKind.Air,
		4 when eventKind is 2 or 3 or 7 => KcvNodeKind.Night,
		4 => KcvNodeKind.Battle,
		7 => KcvNodeKind.Air,
		2 => KcvNodeKind.Resource,
		8 => KcvNodeKind.Escort,
		6 => KcvNodeKind.Imagination,
		3 => KcvNodeKind.Maelstrom,
		9 or 10 => KcvNodeKind.Landing,
		_ => KcvNodeKind.None,
	};
}

/// <summary>
/// The names the record files keep in Japanese (ShipDropRecord 「(なし)」「(満員)」, DevelopmentRecord 「(失敗)」), shown
/// with the viewer's own words for them (user's choice 2026-10-04). The files keep the Japanese.
/// </summary>
public static class KcvRecordNames
{
	public static string Translate(string name) => name
		.Replace("(なし)", DropRecordViewerResources.NameNotExist)
		.Replace("(満員)", DropRecordViewerResources.NameFullPort)
		.Replace("(失敗)", DevelopmentRecordViewerResources.NameNotExist);

	/// <summary>
	/// The names of the drop, development and construction records anew from their IDs with the current master data
	/// and translations, before a record window lists them (user's choice 2026-10-05, "제안된 2번항목으로 진행"). Names
	/// saved as 「?」 (the old Shift-JIS files) or before a translation came keep their ID, so they show right, and one
	/// ship or piece of equipment is one name when the tables group by name. An ID the master data does not know
	/// keeps its saved name. Only the records in memory change; the record files keep what they have (new lines are
	/// written with the current names, as before).
	/// </summary>
	public static void RefreshFromIds()
	{
		KCDatabase db = KCDatabase.Instance;
		if (db.MasterShips.Count is 0) return;

		string? Ship(int id) => id > 0 ? db.MasterShips[id]?.NameWithClass : null;

		foreach (ShipDropRecord.ShipDropElement drop in RecordManager.Instance.ShipDrop.Record)
		{
			drop.ShipName = Ship(drop.ShipID) ?? drop.ShipName;
			drop.ItemName = (drop.ItemID > 0 ? db.MasterUseItems[drop.ItemID]?.NameTranslated : null) ?? drop.ItemName;
			drop.EquipmentName = (drop.EquipmentID > 0 ? db.MasterEquipments[drop.EquipmentID]?.NameEN : null) ?? drop.EquipmentName;
		}

		foreach (DevelopmentRecord.DevelopmentElement development in RecordManager.Instance.Development.Record)
		{
			development.EquipmentName = (development.EquipmentID > 0 ? db.MasterEquipments[development.EquipmentID]?.NameEN : null)
				?? development.EquipmentName;
			development.FlagshipName = Ship(development.FlagshipID) ?? development.FlagshipName;
		}

		foreach (ConstructionRecord.ConstructionElement construction in RecordManager.Instance.Construction.Record)
		{
			construction.ShipName = Ship(construction.ShipID) ?? construction.ShipName;
			construction.FlagshipName = Ship(construction.FlagshipID) ?? construction.FlagshipName;
		}
	}
}

/// <summary>A drop's place as the drop table shows it: 「1-5-11」 or 「61-3[甲]-12」, the node letter and the enemy fleet.</summary>
public sealed record KcvDropMap(string Numbers, string? Letter, bool IsBoss, string? EnemyFleet)
{
	/// <summary>The drop record has no node kinds: a battle, or the boss's in dark red.</summary>
	public KcvNodeKind Kind => IsBoss ? KcvNodeKind.Boss : KcvNodeKind.Battle;
}

public static class KcvDropMaps
{
	/// <summary>
	/// The enemy fleet by the drop's fleet ID; the enemy fleet record works its IDs out again when it loads, so an older
	/// ID may not be there, and then the node's fleet as the viewer's table finds it (GetMapString).
	/// </summary>
	public static KcvDropMap From(ShipDropRecord.ShipDropElement drop) => Make(drop.MapAreaID, drop.MapInfoID, drop.CellID,
		drop.IsBossNode, drop.Difficulty,
		RecordManager.Instance.EnemyFleet.Record.TryGetValue(drop.EnemyFleetID, out var fleet)
			? fleet.FleetName
			: NodeFleet(drop.MapAreaID, drop.MapInfoID, drop.CellID, drop.Difficulty));

	/// <summary>The merged table's key of a node (DropRecordViewerViewModel.GetMapSerialId).</summary>
	public static KcvDropMap FromSerialId(int serialId)
	{
		int world = serialId >> 24 & 0xFF;
		int map = serialId >> 16 & 0xFF;
		int cell = serialId >> 8 & 0xFF;
		int difficulty = (sbyte)((serialId >> 1 & 0x7F) << 1) >> 1;
		bool boss = (serialId & 1) != 0;

		return Make(world, map, cell, boss, difficulty, NodeFleet(world, map, cell, difficulty));
	}

	private static Dictionary<(int World, int Map, int Cell, int Difficulty), string>? NodeFleets { get; set; }
	private static int NodeFleetsCount { get; set; } = -1;

	/// <summary>The first recorded fleet of a node (made again when the enemy fleet record grows).</summary>
	private static string? NodeFleet(int world, int map, int cell, int difficulty)
	{
		Dictionary<ulong, EnemyFleetRecord.EnemyFleetElement> records = RecordManager.Instance.EnemyFleet.Record;
		if (NodeFleets is null || NodeFleetsCount != records.Count)
		{
			NodeFleets = new();
			foreach (EnemyFleetRecord.EnemyFleetElement r in records.Values)
			{
				NodeFleets.TryAdd((r.MapAreaID, r.MapInfoID, r.CellID, r.Difficulty), r.FleetName);
			}
			NodeFleetsCount = records.Count;
		}

		return NodeFleets.GetValueOrDefault((world, map, cell, difficulty));
	}

	private static KcvDropMap Make(int world, int map, int cell, bool boss, int difficulty, string? enemy)
	{
		string numbers = difficulty > 0
			? $"{world}-{map}[{Constants.GetDifficulty(difficulty)}]-{cell}"
			: $"{world}-{map}-{cell}";
		string letter = KCDatabase.Instance.Translation.Destination.CellDisplay(world, map, cell);

		// without a known letter the display gives the number again, which the numbers already show
		return new(numbers, letter == cell.ToString() ? null : letter, boss, string.IsNullOrEmpty(enemy) ? null : enemy);
	}
}

/// <summary>A part of a recipe as the development and construction tables show it: a coloured square and a number.</summary>
public sealed record KcvRecipePart(Brush Color, string Value);

public static class KcvRecipes
{
	/// <summary>Fuel, ammo, steel, bauxite, development material (the mockups' colours, the same as the resource chart's).</summary>
	public static readonly Brush[] Colors =
	[
		Frozen(0x4CAF50),
		Frozen(0xC9A43A),
		Frozen(0xA0A0A8),
		Frozen(0xD9822B),
		Frozen(0x4FC3D9),
	];

	public static List<KcvRecipePart>? Parts(string? recipe)
	{
		if (string.IsNullOrEmpty(recipe)) return null;

		string[] values = recipe.Split('/');
		if (values.Length is < 4 or > 5 || !values.All(v => int.TryParse(v, out _))) return null;

		return values.Select((v, i) => new KcvRecipePart(Colors[i], v)).ToList();
	}

	/// <summary>Large construction takes 1500 fuel at least, normal construction 999 at most.</summary>
	public static bool IsLarge(string? recipe) =>
		recipe?.Split('/') is [string fuel, ..] && int.TryParse(fuel, out int value) && value >= 1000;

	private static SolidColorBrush Frozen(int rgb)
	{
		SolidColorBrush brush = new(KcvPanels.Rgb(rgb));
		brush.Freeze();
		return brush;
	}
}
