using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the quest panel's category filters (mockup 20261005-quest-filter-v2, user decisions 2026-10-05): 편성, 전투, 연습,
/// 원정, 공창 and 그외, one at a time, not saved.
/// </summary>
public enum KcvQuestFilterKind
{
	None,
	Composition,
	Battle,
	Practice,
	Expedition,
	Arsenal,
	Other,
}

/// <summary>Fork: one filter chip of the quest panel; its colour is the list's category colour (KcvQuestCategoryBrushConverter).</summary>
public sealed class KcvQuestFilter : ObservableObject
{
	public KcvQuestFilterKind Kind { get; }
	public string Name { get; }
	public string? ToolTip { get; }

	/// <summary>A category the colour is taken from (KcvQuestCategoryBrushConverter); -1 for 그외's grey.</summary>
	public int ColorCategory { get; }

	public bool IsSelected { get; set; }

	public IRelayCommand ToggleCommand { get; }

	public KcvQuestFilter(KcvQuestFilterKind kind, string name, string? toolTip, int colorCategory, Action<KcvQuestFilterKind> toggle)
	{
		Kind = kind;
		Name = name;
		ToolTip = toolTip;
		ColorCategory = colorCategory;
		ToggleCommand = new RelayCommand(() => toggle(kind));
	}

	/// <summary>
	/// The game's quest categories (api_category, Constants.GetQuestCategory): 1 編成, 2·8·9·10 出撃, 3 演習, 4 遠征,
	/// 6·11 工廠; 그외 is the rest (5 補給・入渠, 7 改装 and any other).
	/// </summary>
	public static KcvQuestFilterKind KindOf(int category) => category switch
	{
		1 => KcvQuestFilterKind.Composition,
		2 or 8 or 9 or 10 => KcvQuestFilterKind.Battle,
		3 => KcvQuestFilterKind.Practice,
		4 => KcvQuestFilterKind.Expedition,
		6 or 11 => KcvQuestFilterKind.Arsenal,
		_ => KcvQuestFilterKind.Other,
	};
}

/// <summary>
/// A line of the rewards in a quest's tooltip (mockup 20261007-quest-rewards-v1): a heading (보상, 선택 보상 1)
/// in the lighter color, or what is under it, indented.
/// </summary>
public sealed record KcvQuestRewardLine(string Text, bool IsHeading);
