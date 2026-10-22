using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using KCViewer.Core.Types;
using KCViewer.Window.Control;
using KCViewer.Window.Wpf;
using static KCViewer.Fork.KcvViews.KcvPanels;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fixed colours of KanColleViewer's gauges and marks, the same in the light and dark themes.</summary>
public static class KcvBrushes
{
	// Gauges by rate, as the HP gauge: above 75% (no damage), above 50% (小破), above 25% (中破), else (大破).
	public static SolidColorBrush GaugeFull { get; } = Frozen(0x43C328);
	public static SolidColorBrush GaugeShouha { get; } = Frozen(0xEDE625);
	public static SolidColorBrush GaugeChuuha { get; } = Frozen(0xFF8C1A);
	public static SolidColorBrush GaugeTaiha { get; } = Frozen(0xE53030);

	// Map gauges (MapHP window): kill and HP gauges in red, transport gauges in GaugeFull's green (user's choice).
	public static SolidColorBrush MapGaugeRed { get; } = Frozen(0xF02A2A);

	// Condition marks: キラ (50+), normal (40-49), then tired in three steps.
	public static SolidColorBrush ConditionSparkle { get; } = Frozen(0xFFFD40);
	public static SolidColorBrush ConditionNormal { get; } = Frozen(0xFFFFFF);
	public static SolidColorBrush ConditionLittleTired { get; } = Frozen(0xFFB347);
	public static SolidColorBrush ConditionTired { get; } = Frozen(0xFF7F00);
	public static SolidColorBrush ConditionVeryTired { get; } = Frozen(0xFF3030);

	// Fleet state: the status bar under the fleet name, and the brighter mark before the name.
	public static SolidColorBrush ReadyBar { get; } = Frozen(0x296412);
	public static SolidColorBrush ReadyMark { get; } = Frozen(0x46C446);
	public static SolidColorBrush ExpeditionBar { get; } = Frozen(0x1B4F8A);
	public static SolidColorBrush ExpeditionMark { get; } = Frozen(0x0060FF);
	public static SolidColorBrush SortieBar { get; } = Frozen(0x6E1E1E);
	public static SolidColorBrush SortieMark { get; } = Frozen(0xC83232);
	public static SolidColorBrush DangerBar { get; } = Frozen(0x9A1414);
	public static SolidColorBrush DangerMark { get; } = Frozen(0xFF3030);
	public static SolidColorBrush WaitingBar { get; } = Frozen(0x7A5410);
	public static SolidColorBrush WaitingMark { get; } = Frozen(0xFF9A20);
	public static SolidColorBrush NoShipBar { get; } = Frozen(0x3F3F46);
	public static SolidColorBrush NoShipMark { get; } = Frozen(0x8C8C90);

	public static SolidColorBrush White { get; } = Frozen(0xFFFFFF);

	// Improvement level in the equipment list: ★+0 dimmed, improved ones in teal.
	public static SolidColorBrush StarZero { get; } = Frozen(0x6A6A70);
	public static SolidColorBrush StarImproved { get; } = Frozen(0x45B5AE);

	public static SolidColorBrush Gauge(double rate) => rate switch
	{
		> 0.75 => GaugeFull,
		> 0.50 => GaugeShouha,
		> 0.25 => GaugeChuuha,
		_ => GaugeTaiha,
	};

	public static SolidColorBrush Condition(int condition) => condition switch
	{
		>= 50 => ConditionSparkle,
		>= 40 => ConditionNormal,
		>= 30 => ConditionLittleTired,
		>= 20 => ConditionTired,
		_ => ConditionVeryTired,
	};

	public static (SolidColorBrush Bar, SolidColorBrush Mark) State(FleetState state) => state switch
	{
		FleetState.Ready or FleetState.Sparkled => (ReadyBar, ReadyMark),
		FleetState.Expedition => (ExpeditionBar, ExpeditionMark),
		FleetState.Sortie => (SortieBar, SortieMark),
		FleetState.SortieDamaged or FleetState.Damaged => (DangerBar, DangerMark),
		FleetState.NoShip => (NoShipBar, NoShipMark),
		_ => (WaitingBar, WaitingMark),
	};

	private static SolidColorBrush Frozen(int rgb)
	{
		SolidColorBrush brush = new(Rgb(rgb));
		brush.Freeze();
		return brush;
	}
}

/// <summary>
/// (BackColor, Value) of a battle HP bar → "Escaped", "Sunk", "MVP", "Boss" or "", read from the colours the viewer's
/// BattleViewModel gives the bar (the same checks as HealthBarViewModel). The KanColleViewer battle panel shows them
/// as a small mark or dimmed text instead of the viewer's row colours.
/// </summary>
public sealed class KcvBattleStateConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values is not [System.Drawing.Color back, int value, ..]) return "";

		var ui = Utility.Configuration.Config.UI;
		System.Drawing.Color escaped = ui.Battle_ColorHPBarsEscaped;
		System.Drawing.Color mvp = ui.Battle_ColorHPBarsMVP;
		System.Drawing.Color bossDamaged = ui.Battle_ColorHPBarsBossDamaged;

		if (back == escaped) return "Escaped";
		if (value <= 0) return "Sunk";
		if (back == mvp) return "MVP";
		if (back == bossDamaged) return "Boss";
		return "";
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>(value, maximum) → gauge colour.</summary>
public sealed class KcvGaugeBrushConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		values is [int value, int maximum, ..] && maximum > 0
			? KcvBrushes.Gauge((double)value / maximum)
			: KcvBrushes.GaugeFull;

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// (a text colour the viewer's view model gives, KcvViewState.Dark) → the colour to show. The viewer's colours are
/// made for its dark theme (white text, bright green and red), so in the light theme they become the light
/// palette's text, green and red (mockup 20261013-battle-hp-damage-v5); any other colour stays as the viewer has it.
/// Fixed colours, as KcvPanels.Apply sets Dark before it swaps the palette. Besides the configured colours, those of
/// the viewer's light and dark themes (Theme.cs) count too: a battle shown before the theme changed keeps the brushes
/// of the old theme. So in the dark theme the viewer's light text colour (black, from a battle shown in the light
/// theme) becomes the dark palette's text (user's choice, 2026-10-14); its other colours stay.
/// </summary>
public sealed class KcvViewerBrushConverter : IMultiValueConverter
{
	private static SolidColorBrush DarkForeground { get; } = Frozen(0xFFFFFF);
	private static SolidColorBrush LightForeground { get; } = Frozen(0x1E1E1E);
	private static SolidColorBrush LightGreen { get; } = Frozen(0x2E7A18);
	private static SolidColorBrush LightRed { get; } = Frozen(0xC62828);

	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values is not [SolidColorBrush brush, bool dark, ..])
		{
			return values.Length > 0 ? values[0] : DependencyProperty.UnsetValue;
		}

		if (dark) return brush.Color == Rgb(0x000000) ? DarkForeground : brush;

		var ui = Utility.Configuration.Config.UI;
		Color color = brush.Color;

		if (color == ui.ForeColor.ToWpfColor() || color == Rgb(0xFFFFFF) || color == Rgb(0x000000)) return LightForeground;
		if (color == ui.Color_Green.ToWpfColor() || color == Rgb(0x00FF00)) return LightGreen;
		if (color == ui.Color_Red.ToWpfColor() || color == Rgb(0xFF0000)) return LightRed;
		return brush;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();

	private static SolidColorBrush Frozen(int rgb)
	{
		SolidColorBrush brush = new(Rgb(rgb));
		brush.Freeze();
		return brush;
	}
}

/// <summary>
/// A radio button of a choice kept as a number: checked when the value equals the parameter; checking it sets the value.
/// </summary>
public sealed class KcvRadioConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value?.ToString() == parameter?.ToString();

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is true && int.TryParse(parameter?.ToString(), out int choice) ? choice : Binding.DoNothing;
}

/// <summary>Improvement level → ★ colour (★+0 dimmed).</summary>
public sealed class KcvStarBrushConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
		value is int level and > 0 ? KcvBrushes.StarImproved : KcvBrushes.StarZero;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>Condition value → mark colour.</summary>
public sealed class KcvConditionBrushConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
		value is int condition ? KcvBrushes.Condition(condition) : KcvBrushes.ConditionNormal;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>Fleet state → status bar colour, or the mark colour with the parameter "Mark".</summary>
public sealed class KcvFleetStateBrushConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not FleetState state) return DependencyProperty.UnsetValue;

		(SolidColorBrush bar, SolidColorBrush mark) = KcvBrushes.State(state);
		return parameter as string == "Mark" ? mark : bar;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// The admiral's name or rank (parameter "Rank"). The viewer joins both into one text (AdmiralName.Text); that text
/// is bound so the parts update with it, and the parts are read from the database.
/// </summary>
public sealed class KcvAdmiralPartConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		Data.AdmiralData admiral = Data.KCDatabase.Instance.Admiral;
		if (admiral is not { IsAvailable: true }) return value ?? "";

		return parameter as string == "Rank" ? Data.Constants.GetAdmiralRank(admiral.Rank) : admiral.AdmiralName;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// Row index (ItemsControl.AlternationIndex, with an AlternationCount above the row count) → row number from 1,
/// or with the parameter "Odd" whether the row gets the alternate background.
/// </summary>
public sealed class KcvRowIndexConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		int index = value is int i ? i : 0;
		return parameter as string == "Odd" ? index % 2 == 1 : (index + 1).ToString(CultureInfo.InvariantCulture);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>"next:" → "Next:".</summary>
public sealed class KcvCapitalizeConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
		value is string { Length: > 0 } text ? char.ToUpperInvariant(text[0]) + text[1..] : "";

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// Quest category → the category colour of KanColleViewer's quest list (measured on its screen). The number of the
/// game's category, or an enum of it (the quest tracker manager's QuestCategory).
/// </summary>
public sealed class KcvQuestCategoryBrushConverter : IValueConverter
{
	private static readonly SolidColorBrush Composition = Frozen(0x4DA35F);
	private static readonly SolidColorBrush Sortie = Frozen(0xB33C32);
	private static readonly SolidColorBrush Practice = Frozen(0x7CA83A);
	private static readonly SolidColorBrush Expedition = Frozen(0x3E9E9D);
	private static readonly SolidColorBrush Supply = Frozen(0xB39231);
	private static readonly SolidColorBrush Arsenal = Frozen(0x8C6446);
	private static readonly SolidColorBrush Modernization = Frozen(0x9B5CB0);

	// The quest filter 그외 (KcvQuestFilter.ColorCategory -1), which takes several categories
	private static readonly SolidColorBrush Other = Frozen(0x6A6A70);

	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) => (value is Enum ? System.Convert.ToInt32(value, culture) : value) switch
	{
		-1 => Other,
		1 => Composition,
		2 or 8 or 9 or 10 => Sortie,
		3 => Practice,
		4 => Expedition,
		5 => Supply,
		6 or 11 => Arsenal,
		7 => Modernization,
		_ => Brushes.Transparent,
	};

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();

	private static SolidColorBrush Frozen(int rgb)
	{
		SolidColorBrush brush = new(Rgb(rgb));
		brush.Freeze();
		return brush;
	}
}

/// <summary>
/// The quest description, the lines after the first of the name tooltip ("name (ID: n)\r\ndescription"); the row view
/// model keeps the quest itself private, and placeholder rows have none.
/// </summary>
public sealed class KcvQuestDescriptionConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not string tooltip) return "";

		int lineBreak = tooltip.IndexOf('\n');
		return lineBreak < 0 ? "" : tooltip[(lineBreak + 1)..].Replace("\r", "").Replace('\n', ' ');
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>
/// (ship, anything that changes with it) → ship type name. The ship object stays the same when it is remodelled,
/// so a second binding (the ship name) makes the type follow.
/// </summary>
public sealed class KcvShipTypeConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		values is [IShipData ship, ..] ? ship.MasterShip.ShipTypeName : "";

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
