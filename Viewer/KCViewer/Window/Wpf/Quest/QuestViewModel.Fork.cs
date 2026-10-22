using System.Collections.Generic;
using System.Linq;
using KCViewer.Avalonia.Translation.Quest;
using KCViewer.Data;
using KCViewer.Fork;
using KCViewer.Fork.KcvViews;
using KCViewer.Observer;
using KCViewer.Window.Dialog.QuestTrackerManager.ViewModels;

namespace KCViewer.Window.Wpf.Quest;

/// <summary>
/// Fork: what the KanColleViewer quest panel (Fork/KcvViews/KcvQuestView.xaml, mockup 20261005-quest-filter-v2, user
/// decisions 2026-10-05) adds: while it shows the quests in progress, the header 「수행 중인 임무 (4/5)」 — the quests
/// taken (in progress or done, the viewer's "in progress only" filter; the game's quest slots count them too) and how
/// many can be taken at once (api_port/port's api_parallel_quest_count, which the viewer does not keep) — and the six
/// category filters, one at a time, not saved. The filter goes with the viewer's own filters in <see cref="Updated"/>.
/// </summary>
public partial class QuestViewModel
{
	public IReadOnlyList<KcvQuestFilter> ForkFilters { get; private set; } = [];
	public KcvQuestFilterKind ForkFilter { get; private set; }

	public int ForkRunningCount { get; private set; }

	/// <summary>0 until the first port answer.</summary>
	public int ForkParallelMax { get; private set; }

	public string ForkHeaderText => !MenuMain_ShowRunningOnly
		? ForkStrings.KcvQuests
		: ForkParallelMax > 0
			? ForkStrings.KcvQuestsInProgressCount(ForkRunningCount, ForkParallelMax)
			: ForkStrings.KcvQuestsInProgressCount(ForkRunningCount);

	/// <summary>Called at the end of the constructor.</summary>
	private void ForkInitialize()
	{
		ForkFilters =
		[
			new(KcvQuestFilterKind.Composition, ForkStrings.KcvQuestFilterComposition, null, 1, ForkToggleFilter),
			new(KcvQuestFilterKind.Battle, ForkStrings.KcvQuestFilterBattle, ForkStrings.KcvQuestFilterBattleToolTip, 2, ForkToggleFilter),
			new(KcvQuestFilterKind.Practice, ForkStrings.KcvQuestFilterPractice, null, 3, ForkToggleFilter),
			new(KcvQuestFilterKind.Expedition, ForkStrings.KcvQuestFilterExpedition, null, 4, ForkToggleFilter),
			new(KcvQuestFilterKind.Arsenal, ForkStrings.KcvQuestFilterArsenal, null, 6, ForkToggleFilter),
			new(KcvQuestFilterKind.Other, ForkStrings.KcvQuestFilterOther, ForkStrings.KcvQuestFilterOtherToolTip, -1, ForkToggleFilter),
		];

		APIObserver.Instance.ApiPort_Port.ResponseReceived += ForkPort;

		// The style turned off or on: the list again, with or without the chosen category
		KcvViewState.Current.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName is nameof(KcvViewState.Enabled)) Updated();
		};
	}

	/// <summary>
	/// In <see cref="Updated"/>: a quest outside the chosen category is left out, only while the style is on (the
	/// viewer's own quest panel, shown with the style off, has no chips to undo it).
	/// </summary>
	private bool ForkFilteredOut(QuestData quest) =>
		KcvViewState.Current.Enabled
		&& ForkFilter is not KcvQuestFilterKind.None
		&& KcvQuestFilter.KindOf(quest.Category) != ForkFilter;

	/// <summary>At the end of <see cref="Updated"/>: the taken quests, whatever the filters show.</summary>
	private void ForkUpdated()
	{
		ForkRunningCount = KCDatabase.Instance.Quest.Quests.Values.Count(q => q.State is 2 or 3);
	}

	/// <summary>A chip: that category only; the chosen one again shows every category.</summary>
	private void ForkToggleFilter(KcvQuestFilterKind kind)
	{
		ForkFilter = ForkFilter == kind ? KcvQuestFilterKind.None : kind;
		foreach (KcvQuestFilter filter in ForkFilters)
		{
			filter.IsSelected = filter.Kind == ForkFilter;
		}

		Updated();
	}

	private void ForkPort(string apiname, dynamic data)
	{
		if (!data.api_parallel_quest_count()) return;

		ForkParallelMax = (int)data.api_parallel_quest_count;
	}

	/// <summary>
	/// A green row (mockup 20261007-quest-complete-rows-v1, approved 2026-10-07 "구현"): the game's 達成 (state 3), or
	/// the progress the panel shows is full, in the same order as its progress column: the user's tracker, the system
	/// tracker, the viewer's old progress (dailies, weeklies). A tracker is full when every task reached its count (not
	/// the average of the tasks' rates, which a task past its count would push to 100% with another at 0).
	/// </summary>
	private static bool ForkIsDone(QuestData quest)
	{
		if (quest.State is 3) return true;

		KCDatabase db = KCDatabase.Instance;
		TrackerViewModel? tracker = db.QuestTrackerManagers.GetTrackerById(quest.QuestID);
		TrackerViewModel? systemTracker = db.SystemQuestTrackerManager.GetTrackerById(quest.QuestID);

		if (tracker is { Tasks.Count: > 0 }) return ForkTasksDone(tracker);
		if (systemTracker is { Tasks.Count: > 0 }) return ForkTasksDone(systemTracker);
		if (db.QuestProgress.Progresses.ContainsKey(quest.QuestID)) return db.QuestProgress[quest.QuestID]?.IsCleared is true;

		return false;
	}

	private static bool ForkTasksDone(TrackerViewModel tracker) =>
		tracker.Model.Tasks.All(task => task is not null && task.Progress >= task.Count);

	/// <summary>
	/// The rewards (mockup 20261007-quest-rewards-v1, user's decision 2026-10-07 "둘다 넣음"): a line under the
	/// description and lines in the name's tooltip. The four resources come from the game with the quest
	/// (api_get_material), the rest from the viewer's translation data (QuestDataService.Reward: KC3's memos); with the
	/// other translations off their names are the memo's Japanese.
	/// </summary>
	private static void ForkSetRewards(QuestItemViewModel row, QuestData quest)
	{
		int[] resources = [];
		if (quest.RawData.api_get_material())
		{
			resources = (int[])quest.RawData.api_get_material;
		}

		QuestReward? reward = KCDatabase.Instance.Translation.Quest.Reward(quest.QuestID);
		List<QuestRewardItem> get = reward?.Get ?? [];
		List<List<QuestRewardItem>> choices = reward?.Choices ?? [];
		bool japanese = Utility.Configuration.Config.UI.DisableOtherTranslations;

		string Resources(string[] names) => string.Join(" · ", resources
			.Select((count, i) => count > 0 && i < names.Length ? $"{names[i]} {count:N0}" : null)
			.OfType<string>());

		string Item(QuestRewardItem item)
		{
			string name = japanese ? item.NameJp : item.Type is { } type ? $"{type} {item.Name}" : item.Name;
			string stars = item.Stars switch
			{
				0 => "",
				10 => "★max",
				_ => $"★{item.Stars}",
			};
			// one of a ship, a furniture or an unlock goes without its count
			string count = item.Kind is "ship" or "furniture" or "feature" ? "" : $" ×{item.Count}";
			return name + stars + count;
		}

		string resourcesShort = Resources(ForkStrings.KcvQuestRewardResourcesShort);
		List<string> summary = [];
		if (resourcesShort.Length > 0) summary.Add(resourcesShort);
		summary.AddRange(get.Select(Item));
		if (choices.Count > 0) summary.Add(ForkStrings.KcvQuestRewardChoices(choices.Count));
		row.ForkRewardSummary = summary.Count > 0 ? string.Join(" · ", summary) : null;

		if (summary.Count == 0)
		{
			row.ForkRewardLines = null;
			return;
		}

		string resourcesFull = Resources(ForkStrings.KcvQuestRewardResources);
		List<KcvQuestRewardLine> lines = [new(ForkStrings.KcvQuestReward, true)];
		if (resourcesFull.Length > 0) lines.Add(new(resourcesFull, false));
		lines.AddRange(get.Select(item => new KcvQuestRewardLine(Item(item), false)));
		for (int i = 0; i < choices.Count; i++)
		{
			lines.Add(new(choices.Count > 1 ? ForkStrings.KcvQuestRewardChoiceN(i + 1) : ForkStrings.KcvQuestRewardChoice, true));
			lines.Add(new(string.Join(" / ", choices[i].Select(Item)), false));
		}
		row.ForkRewardLines = lines;
	}
}
