using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using KCViewer.Avalonia.Dialogs.EquipmentSelector;
using KCViewer.Avalonia.Dialogs.ShipSelector;
using KCViewer.Avalonia.ExpeditionCalculator;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.ViewModels;
using KCViewer.Window.Dialog.QuestTrackerManager;
using KCViewer.Window.Settings.BGM;
using KCViewer.Window.Tools.AirDefense;
using KCViewer.Window.Tools.EventLockPlanner;
using KCViewer.Window.Tools.EquipmentUpgradePlanner;
using KCViewer.Window.Tools.EquipmentUpgradePlanner.UpgradeTree;
using KCViewer.Window.Tools.ExpChecker;
using KCViewer.Window.Tools.FleetImageGenerator;
using KCViewer.Window.Wpf.ShipTrainingPlanner;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the tool menu's calculators in KanColleViewer's style (mockup 20261006-calc-windows-v2, approved 2026-10-06
/// "구현"): 대공 포화 상세 and 필요 경험치 계산 open as these windows while the style is on, from every place that opens
/// them (the tool menu, the fleet panel's menus), 원정 계산기 (mockup 20261006-expedition-calculator-v2, approved
/// "1번 수정 반영해서 구현"), 퀘스트 추적 매니저 (mockup 20261006-quest-tracker-v2, approved "구현") and 칸무스 수집 진행도
/// (mockup 20261006-kancolle-progress-v3, approved "구현"), 편성 이미지 출력 (mockup 20261007-fleet-image-v2, approved
/// "구현") and 이벤트 딱지 플래너 (mockup 20261007-event-lock-v2, approved "구현"), the ship training plan editor
/// (mockup 20261008-ship-training-edit-v2, approved "구현"), the ship selector (mockup 20261008-ship-selector-v2,
/// approved "구현") and 개수 계획 관리's plan editor, upgrade tree and equipment selector (mockup
/// 20261009-upgrade-plan-windows-v2, approved "구현"). With the style off the viewer's windows open.
/// </summary>
public static class KcvCalcWindows
{
	private static KcvEventLockPlannerWindow? EventLockPlanner { get; set; }

	/// <summary>Called where the viewer opens its anti-air defense window; true when this one opened instead.</summary>
	public static bool TryShow(AirDefenseViewModel viewModel) => TryShow(() => new KcvAirDefenseWindow(viewModel));

	/// <summary>Called where the viewer opens its exp checker; true when this one opened instead.</summary>
	public static bool TryShow(ExpCheckerViewModel viewModel) => TryShow(() => new KcvExpCheckerWindow(viewModel));

	/// <summary>
	/// Called where the viewer opens its expedition calculator (an Avalonia window, which the style does not reach);
	/// true when this one opened instead (mockup 20261006-expedition-calculator-v2).
	/// </summary>
	public static bool TryShow(ExpeditionCalculatorViewModel viewModel) => TryShow(() => new KcvExpeditionCalculatorWindow(viewModel));

	/// <summary>
	/// Called where the viewer opens its quest tracker manager (the tool menu, the quest panel's 추적기 관리); true when
	/// this one opened instead (mockup 20261006-quest-tracker-v2).
	/// </summary>
	public static bool TryShow(QuestTrackerManagerViewModel viewModel) => TryShow(() => new KcvQuestTrackerManagerWindow(viewModel));

	/// <summary>
	/// Called where the viewer opens 칸무스 수집 진행도; true when this one opened instead (mockup
	/// 20261006-kancolle-progress-v3). Its view model reads the ships when it is made, so only when the window opens.
	/// </summary>
	public static bool TryShowKancolleProgress() => TryShow(() => new KcvKancolleProgressWindow(new KcvKancolleProgressViewModel()));

	/// <summary>
	/// Called where the viewer opens 편성 이미지 출력 (the tool menu, the fleet panel's menu, the sortie and expedition
	/// records all go through ToolService); true when this one opened instead (mockup 20261007-fleet-image-v2).
	/// </summary>
	public static bool TryShow(FleetImageGeneratorImageDataModel model) => TryShow(() => new KcvFleetImageGeneratorWindow(model));

	/// <summary>
	/// Called where the viewer opens its event lock planner; true when this one opened instead or is open already
	/// (mockup 20261007-event-lock-v2). Each window saves the plan when it closes, so one planner is open at a time
	/// (the viewer keeps its own window single the same way).
	/// </summary>
	public static bool TryShowEventLockPlanner(Func<EventLockPlannerViewModel> createViewModel)
	{
		if (EventLockPlanner is not null) return true;

		return TryShow(() =>
		{
			EventLockPlanner = new(createViewModel());
			EventLockPlanner.Closed += (_, _) => EventLockPlanner = null;
			return EventLockPlanner;
		});
	}

	/// <summary>
	/// Called where the viewer opens 파일 › 설정; true when this one opened instead (mockup 20261007-settings-v2,
	/// approved "구현"). Modal, as the viewer's.
	/// </summary>
	public static bool TryShowConfiguration(System.Windows.Window owner)
	{
		if (!KcvViewState.Current.Enabled) return false;

		new KcvConfigurationWindow(new()).ShowDialog(owner);
		return true;
	}

	/// <summary>Called where the BGM page opens a scene's editor; true when this one opened instead, over the settings window.</summary>
	public static bool TryShowSoundHandleEdit(SoundHandleViewModel handle)
	{
		if (!KcvViewState.Current.Enabled) return false;

		System.Windows.Window? owner = Application.Current?.Windows.OfType<KcvConfigurationWindow>().FirstOrDefault()
			?? Application.Current?.MainWindow;

		if (owner is null) return false;

		new KcvSoundHandleEditDialog(handle).ShowDialog(owner);
		return true;
	}

	/// <summary>
	/// Called where the viewer opens its ship training plan editor (the panel's 추가 after the ship picker, 계획 편집);
	/// null while the style is off, else whether [OK] closed this one (mockup 20261008-ship-training-edit-v2, approved
	/// "구현"). Modal, as the viewer's.
	/// </summary>
	public static bool? TryShowShipTrainingPlan(ShipTrainingPlanViewModel plan, bool adding)
	{
		if (!KcvViewState.Current.Enabled) return null;

		KcvShipTrainingPlanWindow window = new(plan, adding);
		if (Application.Current?.MainWindow is { IsLoaded: true } owner) window.Owner = owner;

		return window.ShowDialog() is true;
	}

	/// <summary>
	/// Called where the viewer opens its ship selector (칸무스 레벨링's 추가, the quest tracker's ship conditions, the
	/// Discord ship of the settings); true when this one opened instead (mockup 20261008-ship-selector-v2, approved
	/// "구현"). Modal over the window in front, as the selector is opened from the settings and tool windows too.
	/// </summary>
	public static bool TryShowShipSelector(ShipSelectorViewModel selector)
	{
		if (!KcvViewState.Current.Enabled) return false;

		System.Windows.Window? owner = Application.Current?.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive)
			?? Application.Current?.MainWindow;

		KcvShipSelectorWindow window = new(new(selector));
		if (owner is { IsLoaded: true }) window.Owner = owner;

		window.ShowDialog();
		return true;
	}

	/// <summary>
	/// Called where the viewer opens its equipment selector (the upgrade planner's two adds, the plan editor's
	/// equipment, the upgrade tree's assignments, the quest tracker's scrap condition); true when this one opened
	/// instead (mockup 20261009-upgrade-plan-windows-v2, approved "구현"). Modal over the window in front.
	/// </summary>
	public static bool TryShowEquipmentSelector(EquipmentSelectorViewModel selector)
	{
		if (!KcvViewState.Current.Enabled) return false;

		KcvEquipmentSelectorWindow window = new(new(selector));
		if (FrontWindow() is { } owner) window.Owner = owner;

		window.ShowDialog();
		return true;
	}

	/// <summary>
	/// Called where the viewer opens a plan's upgrade tree; true when this one opened instead (mockup
	/// 20261009-upgrade-plan-windows-v2). Modal, as the viewer's.
	/// </summary>
	public static bool TryShowUpgradeTree(UpgradeTreeViewModel tree)
	{
		if (!KcvViewState.Current.Enabled) return false;

		KcvUpgradeTreeWindow window = new(tree);
		if (FrontWindow() is { } owner) window.Owner = owner;

		window.ShowDialog();
		return true;
	}

	/// <summary>
	/// Called where the viewer opens its upgrade plan editor (<see cref="EquipmentUpgradePlanItemViewModel.OpenPlanDialog"/>);
	/// null while the style is off, else whether [OK] closed this one (mockup 20261009-upgrade-plan-windows-v2). The
	/// editor writes each change into the plan at once (the viewer's), so a cancel puts the plan back as it was (user's
	/// decision 2026-10-09 "남지 않게 함").
	/// </summary>
	public static bool? TryShowUpgradePlanEditor(EquipmentUpgradePlanItemViewModel editor)
	{
		if (!KcvViewState.Current.Enabled) return null;

		EquipmentUpgradePlanItemModel plan = editor.Plan;
		EquipmentUpgradePlanItemModel before = new()
		{
			Parent = plan.Parent,
			ShouldBeConvertedInto = plan.ShouldBeConvertedInto,
			EquipmentMasterId = plan.EquipmentMasterId,
			EquipmentId = plan.EquipmentId,
			DesiredUpgradeLevel = plan.DesiredUpgradeLevel,
			Finished = plan.Finished,
			Priority = plan.Priority,
			SliderLevel = plan.SliderLevel,
			SelectedHelper = plan.SelectedHelper,
		};

		// a plan not in the database yet is being added (the planner's adds, the upgrade tree's)
		KcvEquipmentUpgradePlanWindow window = new(editor, adding: plan.Id == 0);
		if (FrontWindow() is { } owner) window.Owner = owner;

		if (window.ShowDialog() is true) return true;

		plan.Parent = before.Parent;
		plan.ShouldBeConvertedInto = before.ShouldBeConvertedInto;
		plan.EquipmentMasterId = before.EquipmentMasterId;
		plan.EquipmentId = before.EquipmentId;
		plan.DesiredUpgradeLevel = before.DesiredUpgradeLevel;
		plan.Finished = before.Finished;
		plan.Priority = before.Priority;
		plan.SliderLevel = before.SliderLevel;
		plan.SelectedHelper = before.SelectedHelper;
		editor.UnsubscribeFromApis();

		return false;
	}

	/// <summary>The window in front (a tool window or dialog the window opens over), else the main window.</summary>
	internal static System.Windows.Window? FrontWindow() =>
		(Application.Current?.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive)
			?? Application.Current?.MainWindow) is { IsLoaded: true } window
			? window
			: null;

	private static bool TryShow(Func<System.Windows.Window> create)
	{
		if (!KcvViewState.Current.Enabled || Application.Current?.MainWindow is not { } owner) return false;

		create().Show(owner);
		return true;
	}
}

/// <summary>A label of the viewer's without its colon (「함대 방공 : 」 → 「함대 방공」), for the result cards.</summary>
public sealed class KcvLabelTextConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is string text ? text.Trim().TrimEnd(':', '：').TrimEnd() : value;
}

/// <summary>
/// The anti-air table's shoot-down cell (values: the count, the enemy slot's planes): the failure colour when every
/// plane of the slot is shot down, as the viewer marks it red.
/// </summary>
public sealed class KcvWipeBrushConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		values is [int shotDown, int planes] && shotDown >= planes
			? Application.Current.TryFindResource("Kcv.Failure") as Brush ?? Brushes.Transparent
			: Brushes.Transparent;

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>An expedition's name from its display number (「02」 → 「장거리연습항해」), as the viewer translates it.</summary>
public sealed class KcvMissionNameConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is string id ? KCDatabase.Instance.Mission.Values.FirstOrDefault(m => m.DisplayID == id)?.NameEN ?? "" : "";
}

/// <summary>A use item's icon: the headquarters panel's resource and material icons, else the game's item icon.</summary>
public sealed class KcvUseItemIconConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		UseItemId.Fuel => KcvHqIcons.Fuel,
		UseItemId.Ammo => KcvHqIcons.Ammo,
		UseItemId.Steel => KcvHqIcons.Steel,
		UseItemId.Bauxite => KcvHqIcons.Bauxite,
		UseItemId.InstantRepair => KcvHqIcons.InstantRepair,
		UseItemId.InstantConstruction => KcvHqIcons.InstantConstruction,
		UseItemId.DevelopmentMaterial => KcvHqIcons.DevelopmentMaterial,
		UseItemId.ImproveMaterial => KcvHqIcons.ImprovementMaterial,
		UseItemId id => KcvHqIcons.UseItem((int)id),
		_ => null,
	};
}

/// <summary>A reward's name as the viewer's expedition calculator writes it (its UseItemIdConverter, an Avalonia one).</summary>
public sealed class KcvUseItemNameConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		UseItemId.Fuel => ExpeditionCalculatorResources.Fuel,
		UseItemId.Ammo => ExpeditionCalculatorResources.Ammo,
		UseItemId.Steel => ExpeditionCalculatorResources.Steel,
		UseItemId.Bauxite => ExpeditionCalculatorResources.Bauxite,
		UseItemId.InstantRepair => ExpeditionCalculatorResources.InstantRepair,
		UseItemId.InstantConstruction => ExpeditionCalculatorResources.InstantConstruction,
		UseItemId.DevelopmentMaterial => ExpeditionCalculatorResources.DevelopmentMaterial,
		UseItemId.ImproveMaterial => ExpeditionCalculatorResources.ImproveMaterial,
		UseItemId.FurnitureBoxSmall => ExpeditionCalculatorResources.FurnitureBoxSmall,
		UseItemId.FurnitureBoxMedium => ExpeditionCalculatorResources.FurnitureBoxMedium,
		UseItemId.FurnitureBoxLarge => ExpeditionCalculatorResources.FurnitureBoxLarge,
		UseItemId.MoraleFoodIrako => ExpeditionCalculatorResources.MoraleFoodIrako,
		_ => "",
	};
}

/// <summary>One equipment of an opening anti-submarine set: its icon, name and how many (「×2」 when more than one).</summary>
public sealed record KcvAswEquipment(EquipmentIconType? Icon, string Name, int Count)
{
	public string CountText => Count > 1 ? $"×{Count}" : "";
}

/// <summary>
/// The exp checker's equipment text 「[4식 수중청음기x2, 시제 15cm 9연장 대잠분진포], [...]」 as sets of equipment with
/// their icons. The viewer writes the sets from the equipment's names (ExpCheckerViewModel), so the names are found
/// again in the master data. 「-」 (no set) gives none.
/// </summary>
public sealed partial class KcvAswEquipmentConverter : KcvOneWayConverter
{
	[GeneratedRegex(@"\[([^\]]*)\]")]
	private static partial Regex SetPattern();

	[GeneratedRegex(@"^(.+?)x(\d+)$")]
	private static partial Regex CountPattern();

	private static Dictionary<string, EquipmentIconType>? Icons { get; set; }

	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not string text) return Array.Empty<IReadOnlyList<KcvAswEquipment>>();

		Icons ??= KCDatabase.Instance.MasterEquipments.Values
			.Where(eq => !eq.IsAbyssalEquipment)
			.GroupBy(eq => eq.NameEN)
			.ToDictionary(names => names.Key, names => names.First().IconTypeTyped);

		return SetPattern().Matches(text)
			.Select(set => (IReadOnlyList<KcvAswEquipment>)set.Groups[1].Value
				.Split(", ", StringSplitOptions.RemoveEmptyEntries)
				.Select(Equipment)
				.ToList())
			.ToList();
	}

	private static KcvAswEquipment Equipment(string item)
	{
		Match counted = CountPattern().Match(item);
		string name = counted.Success && Icons!.ContainsKey(counted.Groups[1].Value) ? counted.Groups[1].Value : item;
		int count = name == item ? 1 : int.Parse(counted.Groups[2].Value, CultureInfo.InvariantCulture);

		return new(Icons!.TryGetValue(name, out EquipmentIconType icon) ? icon : null, name, count);
	}
}
