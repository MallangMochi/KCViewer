using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Extensions;
using KCViewer.Services;
using KCViewer.Window.Tools.EquipmentUpgradePlanner;
using KCViewer.Window.Tools.EquipmentUpgradePlanner.Helpers;

namespace KCViewer.Fork.KcvViews;

/*
 * Fork: converters of the planned upgrades panel and the upgrade planner window in KanColleViewer's style
 * (KcvEquipmentUpgradePlanView, KcvEquipmentUpgradePlannerWindow; mockups v1-v2, user's approval 2026-10-02).
 */

/// <summary>
/// Improvement level → "★+n" or "★max" as the equipment list writes it (conversion keeps the viewer's text), or with
/// the parameter "Brush" its colour: ★+0 dimmed, improved ones in teal. Conversion has none here: a style setter bound
/// to it would fall to the default black, so the views set the text colour for it with a trigger.
/// </summary>
public sealed class KcvUpgradeLevelConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not UpgradeLevel level) return parameter as string == "Brush" ? DependencyProperty.UnsetValue : "";

		return parameter as string == "Brush" ? Brush(level) : Text(level);
	}

	public static string Text(UpgradeLevel level) => level switch
	{
		UpgradeLevel.Max => "★max",
		UpgradeLevel.Conversion => level.Display(),
		_ => $"★+{(int)level}",
	};

	public static object Brush(UpgradeLevel level) => level switch
	{
		UpgradeLevel.Conversion => DependencyProperty.UnsetValue,
		UpgradeLevel.Zero => KcvBrushes.StarZero,
		_ => KcvBrushes.StarImproved,
	};

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// (CurrentLevelDisplay, plan) → the current level of the assigned equipment as ★+n, or the viewer's text
/// (未割当) when none is assigned; with the parameter "Brush" its colour. The first value only makes it update: the
/// plan's equipment is read anew each time and has no change notification of its own.
/// </summary>
public sealed class KcvUpgradeCurrentLevelConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
	{
		bool brush = parameter as string == "Brush";
		if (values is not [var display, EquipmentUpgradePlanItemViewModel plan, ..]) return brush ? DependencyProperty.UnsetValue : "";

		if (plan.Equipment is not { MasterID: > 0 } equipment)
		{
			return brush ? DependencyProperty.UnsetValue : display as string ?? "";
		}

		return brush ? KcvUpgradeLevelConverter.Brush(equipment.UpgradeLevel) : KcvUpgradeLevelConverter.Text(equipment.UpgradeLevel);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>(EquipmentName, plan) → the game's icon of the plan's equipment; the name only makes it update.</summary>
public sealed class KcvUpgradePlanIconConverter : IMultiValueConverter
{
	public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		values is [_, EquipmentUpgradePlanItemViewModel { Equipment: { } equipment }, ..]
			? KcvGameIcons.Get(equipment.MasterEquipment.IconTypeTyped)
			: null;

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// (enough resources, a helper owned on some day, the "today" filter, a helper owned today) → whether the panel shows
/// the plan as one that cannot be done: the conditions of the viewer's red rows.
/// </summary>
public sealed class KcvUpgradePlanBlockedConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		values is [bool enoughResources, bool helperSomeDay, bool selectToday, bool helperToday, ..]
		&& (!enoughResources || !helperSomeDay || (selectToday && !helperToday));

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// (helper groups, today only) → the groups to show: with "today only" (the panel's 今日 filter, as the viewer's
/// tooltip) only those that can help today, so the first shown group is also the first row.
/// </summary>
public sealed class KcvUpgradeHelpersTodayConverter : IMultiValueConverter
{
	public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => values switch
	{
		[IEnumerable<EquipmentUpgradeHelpersViewModel> helpers, true, ..] => helpers.Where(h => h.CanHelpToday).ToList(),
		[IEnumerable<EquipmentUpgradeHelpersViewModel> helpers, ..] => helpers,
		_ => null,
	};

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// (item, the list it is in) → whether it is the last one: the helper names put "·" after each but the last, so a
/// wrapped line ends with it instead of starting with it.
/// </summary>
public sealed class KcvIsLastItemConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		values is [var item, System.Collections.IEnumerable list, ..] && Equals(list.Cast<object>().LastOrDefault(), item);

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// A day of a helper group (or of all helpers, compact mode) → "On" (a helper can do it that day), "Today" (and that
/// day is today, JST as the game) or "Off", for the day boxes' colours (Kcv.UpgradeDayTemplate).
/// </summary>
public sealed class KcvUpgradeDayStateConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value switch
	{
		EquipmentUpgradeHelpersDayViewModel day => State(day.IsHelperDay, day.DayValue),
		EquipmentUpgradeDayViewModel day => State(day.Helpers.Count > 0, day.DayValue),
		_ => "Off",
	};

	private static string State(bool helperDay, DayOfWeek day)
	{
		if (!helperDay) return "Off";

		DayOfWeek? today = Ioc.Default.GetService<TimeChangeService>()?.CurrentDayOfWeekJST;
		return day == today ? "Today" : "On";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// Day of the week → its shortest name in the viewer's language (월, 金, Fr) for the day boxes, or with the parameter
/// "Helpers" the title of a day box's tooltip (금요일 보조함).
/// </summary>
public sealed class KcvUpgradeDayTextConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not DayOfWeek day) return "";

		// the culture the viewer's own day names use (EquipmentUpgradePlanner/Extensions.cs)
		DateTimeFormatInfo names = (CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.CurrentCulture).DateTimeFormat;
		return parameter as string == "Helpers"
			? ForkStrings.KcvUpgradeDayHelpers(names.GetDayName(day))
			: names.GetShortestDayName(day);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
