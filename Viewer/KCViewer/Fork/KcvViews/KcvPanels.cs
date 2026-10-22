using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Window.Wpf;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: KanColleViewer's panel designs (VIEWER-PLAN.md, stage 6). The panels keep the viewer's view models; only
/// the XAML inside each docking panel is drawn anew (Fork/KcvViews/*.xaml). Where the panels go is still up to the
/// docking layout. Each replaced panel switches between its original template and the KanColleViewer one with
/// <see cref="Enabled"/>, so the style can be turned off at run time.
/// </summary>
public sealed class KcvViewState : ObservableObject
{
	public static KcvViewState Current { get; } = new();

	public bool Enabled { get; set; }

	public bool Dark { get; set; }
}

public static class KcvPanels
{
	/// <summary>
	/// Replaces the templates of the selector (a keyed resource of the main window, so a merged dictionary cannot
	/// override them). Runs before the layout is loaded, so the panels are created with the new templates.
	/// </summary>
	public static void Install(ViewTemplateSelector selector)
	{
		ResourceDictionary views = new()
		{
			Source = new Uri("/KCViewer;component/Fork/KcvViews/KcvViews.xaml", UriKind.Relative),
		};
		Application.Current.Resources.MergedDictionaries.Add(views);

		selector.Fleet = Switch(selector.Fleet, (DataTemplate)views["Kcv.FleetTemplate"]);
		selector.Headquarters = Switch(selector.Headquarters, (DataTemplate)views["Kcv.HeadquartersTemplate"]);
		selector.Dock = Switch(selector.Dock, (DataTemplate)views["Kcv.DockTemplate"]);
		selector.Arsenal = Switch(selector.Arsenal, (DataTemplate)views["Kcv.ArsenalTemplate"]);
		selector.Quest = Switch(selector.Quest, (DataTemplate)views["Kcv.QuestTemplate"]);
		selector.FleetOverview = Switch(selector.FleetOverview, (DataTemplate)views["Kcv.FleetOverviewTemplate"]);
		selector.Battle = Switch(selector.Battle, (DataTemplate)views["Kcv.BattleTemplate"]);
		selector.Compass = Switch(selector.Compass, (DataTemplate)views["Kcv.CompassTemplate"]);
		selector.EquipmentUpgradePlanViewer = Switch(selector.EquipmentUpgradePlanViewer, (DataTemplate)views["Kcv.EquipmentUpgradePlanTemplate"]);
		selector.BaseAirCorps = Switch(selector.BaseAirCorps, (DataTemplate)views["Kcv.BaseAirCorpsTemplate"]);
		selector.FleetPreset = Switch(selector.FleetPreset, (DataTemplate)views["Kcv.FleetPresetTemplate"]);
		selector.ShipTrainingPlanViewer = Switch(selector.ShipTrainingPlanViewer, (DataTemplate)views["Kcv.ShipTrainingTemplate"]);
		// The viewer sends every WinForms panel to its WinformsHost template (its Json template is unset), so the JSON
		// panel gets one of its own: the viewer's WinForms panel, or the style's WPF one.
		selector.Json = Switch(selector.WinformsHost, (DataTemplate)views["Kcv.JsonTemplate"]);
		selector.GroupAvalonia = Switch(selector.GroupAvalonia, (DataTemplate)views["Kcv.ShipGroupTemplate"]);

		KcvWinForms.Install();
	}

	/// <summary>
	/// Where the common control styles (KcvControls.xaml) go while enabled: the implicit style of each control type and
	/// ModernWpf's base key that the viewer's own styles use (BasedOn DefaultCheckBoxStyle, ...). They are entries of
	/// the application resources themselves, because an entry there wins over merged dictionaries (App.xaml defines an
	/// implicit check box style of its own).
	/// </summary>
	private static readonly (object Key, string Style)[] ControlStyles =
	[
		(typeof(Button), "Kcv.ButtonStyle"),
		("DefaultButtonStyle", "Kcv.ButtonStyle"),
		(typeof(CheckBox), "Kcv.CheckBoxStyle"),
		("DefaultCheckBoxStyle", "Kcv.CheckBoxStyle"),
		(typeof(RadioButton), "Kcv.RadioButtonStyle"),
		("DefaultRadioButtonStyle", "Kcv.RadioButtonStyle"),
		(typeof(TabItem), "Kcv.TabItemStyle"),
	];

	/// <summary>
	/// ModernWpf's theme brushes that differ from KanColleViewer, measured with its dark and light theme dictionaries:
	/// the window background is pure black (white) there, so every window but the main one looked black; table headers
	/// and dialogs too. Popup menus get the window colour with a grey border instead of black.
	/// </summary>
	private static IEnumerable<(string Key, Color Color)> ThemeBrushes(bool dark)
	{
		Color window = dark ? Rgb(0x2D2D30) : Rgb(0xEFEFF2);
		Color header = dark ? Rgb(0x1B1B1C) : Rgb(0xDADAE0);
		Color border = dark ? Rgb(0x3F3F46) : Rgb(0xC6C6CC);

		return
		[
			("SystemControlPageBackgroundAltHighBrush", window),
			("ApplicationPageBackgroundThemeBrush", window),
			("SystemControlBackgroundAltHighBrush", window),
			("ContentDialogBackground", window),
			("MenuFlyoutPresenterBackground", window),
			("ComboBoxDropDownBackground", window),
			("MenuFlyoutPresenterBorderBrush", border),
			("SystemControlTransientBorderBrush", border),
			("DataGridColumnHeaderBackgroundBrush", header),
			("DataGridRowHeaderBackgroundBrush", header),
			// A focused text box is pure black in ModernWpf's dark theme
			("TextControlBackgroundFocused", dark ? Rgb(0x1B1B1C) : Rgb(0xFFFFFF)),
		];
	}

	private static ResourceDictionary? Controls { get; set; }

	/// <summary>The application's own entries that were replaced (null: there was none), to put back when turned off.</summary>
	private static Dictionary<object, object?>? Replaced { get; set; }

	/// <summary>
	/// Puts the common control styles and the theme brushes into the application resources while enabled (again at
	/// each theme change, as the brushes depend on it), and the original entries back when turned off.
	/// </summary>
	private static void ApplyControls(ResourceDictionary resources, bool enabled, bool dark)
	{
		if (enabled)
		{
			Controls ??= new ResourceDictionary
			{
				Source = new Uri("/KCViewer;component/Fork/KcvViews/KcvControls.xaml", UriKind.Relative),
			};

			IEnumerable<(object Key, object Value)> entries = ControlStyles
				.Select(s => (s.Key, Controls[s.Style]))
				.Concat(ThemeBrushes(dark).Select(b => ((object)b.Key, (object)Frozen(b.Color))));

			Replaced ??= new();
			List<object> ownKeys = resources.Keys.Cast<object>().ToList();
			foreach ((object key, object value) in entries)
			{
				if (!Replaced.ContainsKey(key)) Replaced[key] = ownKeys.Contains(key) ? resources[key] : null;
				resources[key] = value;
			}
		}
		else if (Replaced is not null)
		{
			foreach ((object key, object? original) in Replaced)
			{
				if (original is null) resources.Remove(key);
				else resources[key] = original;
			}

			Replaced = null;
		}
	}

	private static SolidColorBrush Frozen(Color color)
	{
		SolidColorBrush brush = new(color);
		brush.Freeze();
		return brush;
	}

	/// <summary>Called with every theme change (FormMainViewModel.ApplyForkTheme).</summary>
	public static void Apply(bool enabled, bool dark, KcvStyle.AccentColors accent)
	{
		KcvViewState.Current.Enabled = enabled;
		KcvViewState.Current.Dark = dark;

		ResourceDictionary resources = Application.Current.Resources;
		foreach ((string key, Color color) in Palette(dark)
			.Append(("Kcv.Accent", accent.Accent))
			// today's box among the upgrade helpers' days
			.Append(("Kcv.AccentHighlight", accent.Highlight)))
		{
			resources[key] = Frozen(color);
		}

		ApplyControls(resources, enabled, dark);
		KcvWinForms.Apply(enabled, dark, accent.Accent);
	}

	/// <summary>
	/// Panel colours of KanColleViewer (MetroRadiance Dark/Light), measured on its screens where the theme files
	/// give no value (text on dark: white, labels a little dimmer; the empty part of a gauge is white).
	/// </summary>
	private static IEnumerable<(string Key, Color Color)> Palette(bool dark) => dark
		? [
			("Kcv.Background", Rgb(0x2D2D30)),
			("Kcv.Foreground", Rgb(0xFFFFFF)),
			("Kcv.SubForeground", Rgb(0xD0D0D4)),
			("Kcv.InactiveForeground", Rgb(0x8C8C90)),
			("Kcv.Separator", Rgb(0x55555B)),
			("Kcv.GaugeBackground", Rgb(0xFFFFFF)),
			// the battle panel's HP gauge: the part lost in this battle, and the number of it over the gauge
			// (mockup 20261013-battle-hp-damage-v5: light grey; white with a black outline in the dark theme)
			("Kcv.GaugeLost", Rgb(0xB4B4B8)),
			("Kcv.GaugeDamageText", Rgb(0xFFFFFF)),
			("Kcv.GaugeDamageOutline", Rgb(0x000000)),
			// the empty part of the MapHP window's map gauges, darker than the ship gauges' white
			("Kcv.MapGaugeBackground", Rgb(0x48484E)),
			("Kcv.MarkBorder", Colors.Transparent),
			("Kcv.ButtonBackground", Rgb(0x1B1B1C)),
			("Kcv.ButtonBorder", Rgb(0x3F3F46)),
			("Kcv.HeaderBackground", Rgb(0x3F3F46)),
			("Kcv.AlternateRow", Rgb(0x333337)),
			("Kcv.BadgeBackground", Rgb(0x245A40)),
			("Kcv.BadgeForeground", Rgb(0xA8DCC0)),
			// the quest panel's 수행 중 badge (accepted quests), the same tone as the badge above in orange
			("Kcv.InProgressBadgeBackground", Rgb(0x7A4A12)),
			("Kcv.InProgressBadgeForeground", Rgb(0xFFCF8F)),
			("Kcv.ButtonForeground", Rgb(0xD8D8DC)),
			("Kcv.ButtonHover", Rgb(0x3F3F46)),
			("Kcv.CheckFill", Rgb(0x404046)),
			("Kcv.CheckMark", Rgb(0xFFFFFF)),
			("Kcv.TableHeaderBackground", Rgb(0x1B1B1C)),
			// fully modernized parameters in the ship list (火力, 雷装, 対空, 装甲 MAX)
			("Kcv.StatFirepower", Rgb(0x663834)),
			("Kcv.StatTorpedo", Rgb(0x314D65)),
			("Kcv.StatAA", Rgb(0x794B29)),
			("Kcv.StatArmor", Rgb(0x7C6436)),
			// planned upgrades: what is short (resources, equipment, helpers not owned) and the plans that cannot be done
			("Kcv.LackForeground", Rgb(0xF2585B)),
			// 칸무스 수집 진행도's level colours, the viewer's (DeepPink, DeepSkyBlue, LimeGreen, Yellow) made softer on dark boxes
			// (mockup 20261006-kancolle-progress-v3); Lv 1+ is the foreground and Lv 0 the lack colour
			("Kcv.LevelMax", Rgb(0xF25AA8)),
			("Kcv.Level99", Rgb(0x4FC3F7)),
			("Kcv.Level90", Rgb(0x6CCB5F)),
			("Kcv.Level80", Rgb(0xE8D44D)),
			("Kcv.BlockedRow", Color.FromArgb(0x30, 0xF0, 0x2A, 0x2A)),
			("Kcv.BlockedRowHover", Color.FromArgb(0x50, 0xF0, 0x2A, 0x2A)),
			("Kcv.BlockedBar", Rgb(0xF02A2A)),
			// finished plans, in the green of a full gauge (user's choice 2026-10-02)
			("Kcv.FinishedRow", Color.FromArgb(0x30, 0x43, 0xC3, 0x28)),
			("Kcv.FinishedRowHover", Color.FromArgb(0x50, 0x43, 0xC3, 0x28)),
			("Kcv.FinishedBar", Rgb(0x43C328)),
			// record windows (mockups 2026-10-04): status bar and cards, ship chips, faded zeros
			("Kcv.StatusBackground", Rgb(0x252526)),
			("Kcv.ChipBackground", Rgb(0x38383D)),
			("Kcv.FaintForeground", Rgb(0x6A6A70)),
			// the sortie cost's resources gained, and a cost below 0 (mockup 20261017-sortie-cost-v3)
			("Kcv.GainForeground", Rgb(0x6CCB5F)),
			// the sortie route's nodes by kind, and the drop table's nodes; the compass's cell and kind of cell too
			// (mockup 20261005-compass-route-v4: night battles purple, the maelstrom solid as the boss)
			("Kcv.NodeBattle", Color.FromArgb(0x4D, 0xE0, 0x52, 0x52)),
			("Kcv.NodeBattleText", Rgb(0xF6B0B0)),
			("Kcv.NodeNight", Color.FromArgb(0x5C, 0xB0, 0x76, 0xEB)),
			("Kcv.NodeNightText", Rgb(0xE0C8FF)),
			("Kcv.NodeBoss", Rgb(0xB42828)),
			("Kcv.NodeBossText", Rgb(0xFFFFFF)),
			("Kcv.NodeAir", Color.FromArgb(0x52, 0xE8, 0x87, 0x3A)),
			("Kcv.NodeAirText", Rgb(0xF5C08F)),
			("Kcv.NodeResource", Color.FromArgb(0x4D, 0x4C, 0xAF, 0x50)),
			("Kcv.NodeResourceText", Rgb(0xA6E0A8)),
			("Kcv.NodeEscort", Color.FromArgb(0x4D, 0xE8, 0xC5, 0x47)),
			("Kcv.NodeEscortText", Rgb(0xF0DC8A)),
			("Kcv.NodeImagination", Color.FromArgb(0x4D, 0x4F, 0x8F, 0xE0)),
			("Kcv.NodeImaginationText", Rgb(0xA9C8F2)),
			("Kcv.NodeMaelstrom", Rgb(0x8E2BB0)),
			("Kcv.NodeMaelstromText", Rgb(0xFFFFFF)),
			("Kcv.NodeLanding", Color.FromArgb(0x4D, 0x2B, 0xB3, 0xA8)),
			("Kcv.NodeLandingText", Rgb(0x9EE0DA)),
			("Kcv.NodeNone", Rgb(0x44444A)),
			("Kcv.NodeNoneText", Rgb(0xC0C0C6)),
			// battle ranks in the user's colours (2026-10-13 "SS,S 금색 A 적색 B 주황 C 노랑 D 초록 E 하늘색", everywhere
			// a rank shows), criticals of the battle detail in the orange the A rank had, and expedition results
			// (大成功, 成功, 失敗)
			("Kcv.RankS", Rgb(0xE8C547)),
			("Kcv.RankA", Rgb(0xF2585B)),
			("Kcv.RankB", Rgb(0xFF8C1A)),
			("Kcv.RankC", Rgb(0xEDE625)),
			("Kcv.RankD", Rgb(0x43C328)),
			("Kcv.RankE", Rgb(0x4FC3F7)),
			("Kcv.Critical", Rgb(0xE8A13A)),
			("Kcv.GreatSuccess", Color.FromArgb(0x2E, 0xE8, 0xC5, 0x47)),
			("Kcv.GreatSuccessText", Rgb(0xE8C547)),
			("Kcv.Success", Color.FromArgb(0x29, 0x43, 0xC3, 0x28)),
			("Kcv.SuccessText", Rgb(0x7DD35F)),
			("Kcv.Failure", Color.FromArgb(0x2E, 0xE5, 0x30, 0x30)),
			("Kcv.FailureText", Rgb(0xF08080)),
			// the resource chart
			("Kcv.ChartGrid", Rgb(0x3F3F46)),
			("Kcv.ChartZero", Rgb(0x8C8C90)),
			("Kcv.ChartFrame", Rgb(0x55555C)),
			("Kcv.ChartTick", Rgb(0xA0A0A8)),
			// a ship or equipment to go to (the albums' remodel and conversion)
			("Kcv.Link", Rgb(0x9CC8F0)),
		]
		: [
			("Kcv.Background", Rgb(0xEFEFF2)),
			("Kcv.Foreground", Rgb(0x1E1E1E)),
			("Kcv.SubForeground", Rgb(0x505055)),
			("Kcv.InactiveForeground", Rgb(0x96969C)),
			("Kcv.Separator", Rgb(0xC6C6CC)),
			("Kcv.GaugeBackground", Rgb(0xD4D4D8)),
			("Kcv.GaugeLost", Rgb(0xA8A8AE)),
			("Kcv.GaugeDamageText", Rgb(0x000000)),
			("Kcv.GaugeDamageOutline", Rgb(0xFFFFFF)),
			("Kcv.MapGaugeBackground", Rgb(0xD4D4D8)),
			("Kcv.MarkBorder", Rgb(0x9A9AA0)),
			("Kcv.ButtonBackground", Rgb(0xF7F7F9)),
			("Kcv.ButtonBorder", Rgb(0xC6C6CC)),
			("Kcv.HeaderBackground", Rgb(0xDADAE0)),
			("Kcv.AlternateRow", Rgb(0xE6E6EA)),
			("Kcv.BadgeBackground", Rgb(0xCDEBDA)),
			("Kcv.BadgeForeground", Rgb(0x1E5A3A)),
			("Kcv.InProgressBadgeBackground", Rgb(0xF8DEC0)),
			("Kcv.InProgressBadgeForeground", Rgb(0x7A4510)),
			("Kcv.ButtonForeground", Rgb(0x1E1E1E)),
			("Kcv.ButtonHover", Rgb(0xE4E4E8)),
			("Kcv.CheckFill", Rgb(0xD4D4DA)),
			("Kcv.CheckMark", Rgb(0x1E1E1E)),
			("Kcv.TableHeaderBackground", Rgb(0xDADAE0)),
			("Kcv.StatFirepower", Rgb(0xF0C8C2)),
			("Kcv.StatTorpedo", Rgb(0xC6D8EA)),
			("Kcv.StatAA", Rgb(0xF2D5BA)),
			("Kcv.StatArmor", Rgb(0xE6D7B8)),
			("Kcv.LackForeground", Rgb(0xC62828)),
			("Kcv.LevelMax", Rgb(0xC2185B)),
			("Kcv.Level99", Rgb(0x0277BD)),
			("Kcv.Level90", Rgb(0x2E7D32)),
			("Kcv.Level80", Rgb(0x8A6D00)),
			("Kcv.BlockedRow", Color.FromArgb(0x30, 0xE5, 0x30, 0x30)),
			("Kcv.BlockedRowHover", Color.FromArgb(0x50, 0xE5, 0x30, 0x30)),
			("Kcv.BlockedBar", Rgb(0xE02424)),
			("Kcv.FinishedRow", Color.FromArgb(0x38, 0x2E, 0x9E, 0x1E)),
			("Kcv.FinishedRowHover", Color.FromArgb(0x58, 0x2E, 0x9E, 0x1E)),
			("Kcv.FinishedBar", Rgb(0x2E9E1E)),
			("Kcv.StatusBackground", Rgb(0xE4E4E8)),
			("Kcv.ChipBackground", Rgb(0xDEDEE3)),
			("Kcv.FaintForeground", Rgb(0xB0B0B6)),
			("Kcv.GainForeground", Rgb(0x2E7D32)),
			("Kcv.NodeBattle", Rgb(0xF6D2D2)),
			("Kcv.NodeBattleText", Rgb(0x9A2222)),
			("Kcv.NodeNight", Rgb(0xE8DCF4)),
			("Kcv.NodeNightText", Rgb(0x5A2E8A)),
			("Kcv.NodeBoss", Rgb(0xC62828)),
			("Kcv.NodeBossText", Rgb(0xFFFFFF)),
			("Kcv.NodeAir", Rgb(0xF8DEC6)),
			("Kcv.NodeAirText", Rgb(0x8A4A10)),
			("Kcv.NodeResource", Rgb(0xD3EED4)),
			("Kcv.NodeResourceText", Rgb(0x1E6B22)),
			("Kcv.NodeEscort", Rgb(0xF3E6B6)),
			("Kcv.NodeEscortText", Rgb(0x6E5608)),
			("Kcv.NodeImagination", Rgb(0xD6E4F7)),
			("Kcv.NodeImaginationText", Rgb(0x1F4F8F)),
			("Kcv.NodeMaelstrom", Rgb(0x8E2BB0)),
			("Kcv.NodeMaelstromText", Rgb(0xFFFFFF)),
			("Kcv.NodeLanding", Rgb(0xCDEDEA)),
			("Kcv.NodeLandingText", Rgb(0x13665F)),
			("Kcv.NodeNone", Rgb(0xDCDCE0)),
			("Kcv.NodeNoneText", Rgb(0x505055)),
			("Kcv.RankS", Rgb(0x9A7A00)),
			("Kcv.RankA", Rgb(0xC62828)),
			("Kcv.RankB", Rgb(0xC75B00)),
			("Kcv.RankC", Rgb(0x9C9000)),
			("Kcv.RankD", Rgb(0x2E7A18)),
			("Kcv.RankE", Rgb(0x0277BD)),
			("Kcv.Critical", Rgb(0xB06A00)),
			("Kcv.GreatSuccess", Rgb(0xF6E8B0)),
			("Kcv.GreatSuccessText", Rgb(0x7A5E00)),
			("Kcv.Success", Rgb(0xD4EFCB)),
			("Kcv.SuccessText", Rgb(0x2E7A18)),
			("Kcv.Failure", Rgb(0xF6D0D0)),
			("Kcv.FailureText", Rgb(0xA02020)),
			("Kcv.ChartGrid", Rgb(0xD4D4DA)),
			("Kcv.ChartZero", Rgb(0x8C8C90)),
			("Kcv.ChartFrame", Rgb(0xB9B9C0)),
			("Kcv.ChartTick", Rgb(0x505055)),
			("Kcv.Link", Rgb(0x1F5F9F)),
		];

	/// <summary>A template that shows <paramref name="original"/> or <paramref name="kcv"/> by <see cref="KcvViewState.Enabled"/>.</summary>
	private static DataTemplate? Switch(DataTemplate? original, DataTemplate kcv)
	{
		if (original is null) return null;

		Style style = new(typeof(ContentControl));
		style.Setters.Add(new Setter(ContentControl.ContentTemplateProperty, original));
		DataTrigger enabled = new()
		{
			Binding = new Binding(nameof(KcvViewState.Enabled)) { Source = KcvViewState.Current },
			Value = true,
		};
		enabled.Setters.Add(new Setter(ContentControl.ContentTemplateProperty, kcv));
		style.Triggers.Add(enabled);

		FrameworkElementFactory content = new(typeof(ContentControl));
		content.SetBinding(ContentControl.ContentProperty, new Binding());
		content.SetValue(UIElement.FocusableProperty, false);
		content.SetValue(FrameworkElement.StyleProperty, style);

		return new DataTemplate { VisualTree = content };
	}

	internal static Color Rgb(int rgb) => Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
}
