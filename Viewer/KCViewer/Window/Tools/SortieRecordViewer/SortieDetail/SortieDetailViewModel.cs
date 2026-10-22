using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Common;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Mocks;
using KCViewer.Data;
using KCViewer.Database;
using KCViewer.Database.Sortie;
using KCViewer.KancolleApi.Types.ApiGetMember.ShipDeck;
using KCViewer.KancolleApi.Types.ApiReqBattleMidnight.Battle;
using KCViewer.KancolleApi.Types.ApiReqBattleMidnight.SpMidnight;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.Battle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.Battleresult;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.BattleWater;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.EachBattle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.EachBattleWater;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.EcBattle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.EcMidnightBattle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.LdAirbattle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.LdShooting;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.MidnightBattle;
using KCViewer.KancolleApi.Types.ApiReqCombinedBattle.SpMidnight;
using KCViewer.KancolleApi.Types.ApiReqMap.Models;
using KCViewer.KancolleApi.Types.ApiReqMap.Next;
using KCViewer.KancolleApi.Types.ApiReqMap.Start;
using KCViewer.KancolleApi.Types.ApiReqMap.StartAirBase;
using KCViewer.KancolleApi.Types.ApiReqSortie.Airbattle;
using KCViewer.KancolleApi.Types.ApiReqSortie.Battle;
using KCViewer.KancolleApi.Types.ApiReqSortie.Battleresult;
using KCViewer.KancolleApi.Types.ApiReqSortie.LdAirbattle;
using KCViewer.KancolleApi.Types.ApiReqSortie.LdShooting;
using KCViewer.KancolleApi.Types.Interfaces;
using KCViewer.KancolleApi.Types.Legacy.OpeningTorpedoRework;
using KCViewer.Services;
using KCViewer.Window.Dialog;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Node;
using KCViewer.Window.Wpf;

namespace KCViewer.Window.Tools.SortieRecordViewer.SortieDetail;

public partial class SortieDetailViewModel : WindowViewModelBase
{
	public SortieDetailTranslationViewModel SortieDetail { get; }
	private BattleFactory BattleFactory { get; }
	private ToolService ToolService { get; }

	private KCViewerContext Db { get; }
	private SortieRecord Sortie { get; }

	public DateTime? StartTime { get; set; }
	public int World { get; }
	public int Map { get; }
	public BattleFleets Fleets { get; set; }
	public BattleFleets FleetsBeforeSortie { get; private set; }
	private BattleFleets? FleetsAfterSortie { get; }
	public List<List<int>?> StrikePoints { get; } = [];

	public ObservableCollection<SortieNode> Nodes { get; } = [];

	public bool IsDebug =>
#if DEBUG
		true;
#else
		false;
#endif

	public SortieDetailViewModel(KCViewerContext db, SortieRecord sortie,
		BattleFleets fleets, BattleFleets? fleetsAfterSortie)
	{
		SortieDetail = Ioc.Default.GetRequiredService<SortieDetailTranslationViewModel>();
		BattleFactory = Ioc.Default.GetRequiredService<BattleFactory>();
		ToolService = Ioc.Default.GetRequiredService<ToolService>();

		Db = db;
		Sortie = sortie;
		World = sortie.World;
		Map = sortie.Map;
		Fleets = fleets;
		FleetsBeforeSortie = fleets.Clone();
		FleetsAfterSortie = fleetsAfterSortie;
	}

	private List<(object ApiData, DateTime Time)> ApiDataCache { get; } = [];

	public void AddApiFile(object apiData, DateTime time)
	{
		if (apiData is ApiReqMapStartResponse start)
		{
			ProcessApiDataCache();

			ApiDataCache.Add((start, time));

			return;
		}

		if (apiData is ApiReqMapNextResponse next)
		{
			ProcessApiDataCache();

			ApiDataCache.Add((next, time));

			if (next.ApiDestructionBattle is not null)
			{
				ApiDataCache.Add((next.ApiDestructionBattle, time));
			}

			return;
		}

		if (apiData is ApiReqSortieBattleresultResponse or ApiReqCombinedBattleBattleresultResponse)
		{
			ApiDataCache.Add((apiData, time));

			return;
		}

		if (apiData is ApiGetMemberShipDeckResponse deck)
		{
			ApiDataCache.Add((deck, time));

			return;
		}

		ApiDataCache.Add((apiData, time));
	}

	private void ProcessApiDataCache()
	{
		if (!ApiDataCache.Any()) return;

		SortieNode? node = null;
		BattleBaseAirRaid? abRaid = null;
		ApiGetMemberShipDeckResponse? deckResponse = null;
		int cell = 0;
		CellType colorNo = CellType.Unknown;
		int eventId = 0;
		int eventKind = 0;
		ApiHappening? happening = null;
		List<ApiItemget?>? items = null;
		ApiOffshoreSupply? offshoreSupply = null;

		foreach ((object apiData, DateTime time) in ApiDataCache)
		{
			if (apiData is ApiReqMapStartAirBaseRequest ab)
			{
				StrikePoints.Add(ParseStrikePoints(ab.ApiStrikePoint1));
				StrikePoints.Add(ParseStrikePoints(ab.ApiStrikePoint2));
				StrikePoints.Add(ParseStrikePoints(ab.ApiStrikePoint3));

				continue;

				static List<int>? ParseStrikePoints(string? apiStrikePoint) => apiStrikePoint switch
				{
					null => null,
					_ => apiStrikePoint
						.Split(",")
						.Select(p => int.TryParse(p, out int point) switch
						{
							true => point,
							_ => -1,
						}).ToList(),
				};
			}

			if (apiData is IMapProgressApi progress)
			{
				cell = progress.ApiNo;
				colorNo = progress.ApiColorNo;
				eventId = progress.ApiEventId;
				eventKind = progress.ApiEventKind;
				happening = progress.ApiHappening;

				if (apiData is ApiReqMapNextResponse next)
				{
					items = next.ApiItemget switch
					{
						JsonElement { ValueKind: JsonValueKind.Array } i
							=> i.Deserialize<List<ApiItemget?>>(),

						JsonElement i => [i.Deserialize<ApiItemget?>()],

						_ => [],
					};
				}
			}

			offshoreSupply = apiData switch
			{
				ApiReqMapNextResponse n => n.ApiOffshoreSupply,
				_ => offshoreSupply,
			};

			BattleData? battle = GetBattle(apiData, node);

			if (battle is not null)
			{
				battle.TimeStamp = time;
			}

			if (battle is BattleBaseAirRaid raid)
			{
				abRaid = raid;
				continue;
			}

			if (battle is not null)
			{
				if (node is BattleNode battleNode)
				{
					battleNode.SecondBattle = battle;
				}
				else
				{
					node = new BattleNode(KCDatabase.Instance, World, Map, cell, battle, colorNo, eventId, eventKind);
				}
			}

			if (apiData is ISortieBattleResultApi result)
			{
				if (node is not BattleNode battleNode) continue;

				battleNode.AddResult(result);
			}

			// comes before next, so this should always be the last response
			if (apiData is ApiGetMemberShipDeckResponse deck)
			{
				deckResponse = deck;
			}
		}

		node ??= new EmptyNode(KCDatabase.Instance, World, Map, cell, colorNo, eventId, eventKind);

		if (abRaid is not null)
		{
			node.AddAirBaseRaid(abRaid);
		}

		node.Happening = happening;
		node.Items = items;
		node.ApiOffshoreSupply = offshoreSupply;

		if (node is BattleNode b)
		{
			if (deckResponse is not null)
			{
				b.UpdateState(deckResponse);
			}
			else if (FleetsAfterSortie is not null)
			{
				b.UpdateState(FleetsAfterSortie);
			}

			Fleets = b.LastBattle.FleetsAfterBattle.Clone();

			CleanFleet(Fleets.Fleet);
			CleanFleet(Fleets.EscortFleet);
			ApplyBattleResult(b.BattleResult, Fleets);
		}

		Nodes.Add(node);

		ApiDataCache.Clear();
	}

	// todo: remove used damecon from fleet, this needs UsedDamecon to be exposed from attacks
	// blocked by https://github.com/ElectronicObserverEN/ElectronicObserver/pull/484
	private static void CleanFleet(IFleetData? fleetData)
	{
		if (fleetData is not FleetDataMock fleet) return;
		if (fleet.MembersInstance is null) return;

		fleet.MembersInstance = fleet.MembersInstance
			.Where(s => s?.HPCurrent > 0)
			.ToList()
			.ToReadOnlyCollection();
	}

	private static void ApplyBattleResult(BattleResult? battleResult, BattleFleets fleets)
	{
		if (battleResult is null) return;

		ApplyExpGain(fleets.Fleet, battleResult.ExpList, battleResult.LevelUpList);

		if (fleets.EscortFleet is null) return;
		if (battleResult.LevelUpListCombined is null) return;

		ApplyExpGain(fleets.EscortFleet, battleResult.ExpList, battleResult.LevelUpListCombined);
	}

	private static void ApplyExpGain(IFleetData fleet, List<int> expList, List<List<int>> levelUpLists)
	{
		static IEnumerable<(IShipData? Ship, int Exp, List<int> LevelUpList)> GetExpData(
			IFleetData fleet, List<int> expList, List<List<int>> levelUpList)
			=> fleet.MembersInstance
				.Zip(expList.Skip(1))
				.Zip(levelUpList, (t, l) => (t.First, t.Second, l));

		foreach ((IShipData? ship, int exp, List<int> levelUpList) in GetExpData(fleet, expList, levelUpLists))
		{
			if (ship is null) continue;
			if (exp is -1) continue;

			switch (levelUpList.Count)
			{
				// level capped ship
				case 1: break;

				// ship hit level cap
				case 2 when levelUpList[0] + exp > levelUpList[1]:
					((ShipDataMock)ship).Level++;
					break;

				default:
					((ShipDataMock)ship).Level += levelUpList.Count - 2;
					break;
			}
		}
	}

	public void EnsureApiFilesProcessed()
	{
		ProcessApiDataCache();
	}

	private BattleData? GetBattle(object api, SortieNode? node) => node switch
	{
		BattleNode b => GetBattle(api, b.FirstBattle.FleetsAfterBattle),
		_ => GetBattle(api),
	};

	/// <summary>
	/// Used to initialize first battles.
	/// </summary>
	/// <param name="api"></param>
	/// <returns></returns>
	private BattleData? GetBattle(object api) => api switch
	{
		ApiReqSortieBattleResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqBattleMidnightSpMidnightResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqSortieAirbattleResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqSortieLdAirbattleResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqSortieLdShootingResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqCombinedBattleBattleResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqCombinedBattleSpMidnightResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqCombinedBattleBattleWaterResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqCombinedBattleLdAirbattleResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqCombinedBattleEcBattleResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqCombinedBattleEachBattleResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqCombinedBattleEachBattleWaterResponse a => BattleFactory.CreateBattle(a, Fleets),
		ApiReqCombinedBattleLdShootingResponse a => BattleFactory.CreateBattle(a, Fleets),

		ApiDestructionBattle a => BattleFactory.CreateBattle(a, Fleets),

		OpeningTorpedoRework_ApiReqSortieBattleResponse a => BattleFactory.CreateBattle(a, Fleets),
		OpeningTorpedoRework_ApiReqCombinedBattleBattleResponse a => BattleFactory.CreateBattle(a, Fleets),
		OpeningTorpedoRework_ApiReqCombinedBattleBattleWaterResponse a => BattleFactory.CreateBattle(a, Fleets),
		OpeningTorpedoRework_ApiReqCombinedBattleEcBattleResponse a => BattleFactory.CreateBattle(a, Fleets),
		OpeningTorpedoRework_ApiReqCombinedBattleEachBattleResponse a => BattleFactory.CreateBattle(a, Fleets),
		OpeningTorpedoRework_ApiReqCombinedBattleEachBattleWaterResponse a => BattleFactory.CreateBattle(a, Fleets),

		_ => null,
	};

	/// <summary>
	/// Used to initialize second battles.
	/// </summary>
	/// <param name="api"></param>
	/// <param name="fleets"></param>
	/// <returns></returns>
	private BattleData? GetBattle(object api, BattleFleets fleets) => api switch
	{
		ApiReqBattleMidnightBattleResponse a => BattleFactory.CreateBattle(a, fleets),
		ApiReqCombinedBattleMidnightBattleResponse a => BattleFactory.CreateBattle(a, fleets),
		ApiReqCombinedBattleEcMidnightBattleResponse a => BattleFactory.CreateBattle(a, fleets),

		_ => null,
	};

	[RelayCommand]
	private async Task CopySortieData()
	{
		await ToolService.CopySortieDataToClipboard(Db, Sortie);
	}

	[RelayCommand]
	private void LoadSortieData()
	{
		ToolService.LoadSortieDataFromClipboard(Db);
	}

	[RelayCommand]
	private void CopyAirControlSimulatorLink()
	{
		ToolService.CopyAirControlSimulatorLink(Sortie, this);
	}

	[RelayCommand]
	private void OpenAirControlSimulator()
	{
		ToolService.AirControlSimulator(Sortie);
	}

	[RelayCommand]
	private void CopyOperationRoomLink()
	{
		ToolService.CopyOperationRoomLink(Sortie);
	}

	[RelayCommand]
	private void OpenOperationRoom()
	{
		ToolService.OperationRoom(Sortie);
	}

	[RelayCommand]
	private async Task OpenLocalApiLoader()
	{
		if (!IsDebug) return;

		new DialogLocalAPILoader2(Sortie).Show();
	}
}
