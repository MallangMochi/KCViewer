using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using KCViewer.Fork.Plugins;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the top menus as decided with the user (VIEWER-PLAN "상단 메뉴 개편"). The order of the panel menu (the
/// viewer's 表示, panel menu mockup 8 with the answers of 2026-10-02) and of the tool menu (tool menu mockup 4 of
/// 2026-10-03); the file and help menus of the menu check of 2026-10-05 (access keys, the update check's name, the
/// help items that lead to the viewer's own project left out); the plugin menu. FormMainWpf.xaml keeps the viewer's
/// items as the viewer has them; they are put in this order once the window is made, so the upstream file changes
/// little. An item is known by the binding path of its header, or of its command when the header is plain text.
/// Items not named here (anything the viewer adds later) follow at the end in their own order.
/// </summary>
public static class ForkMenuLayout
{
	private const string View = "ForkPanelsMenuHeader";
	private const string Tools = "FormMain.StripMenu_Tool";
	private const string File = "FormMain.StripMenu_File";
	private const string Help = "FormMain.StripMenu_Help";
	private const string Plugins = "ForkPluginsMenuHeader";

	/// <summary>
	/// Help items left out. Until 2026-10-22 every one that went to 74식's project (its wiki, issues, Discord, release
	/// notes: "다 빼고", 2026-10-05); since then the help, issue and release items lead to our repository
	/// (<see cref="ForkHelpLinks"/>, mockup 20261022-program-update-v5 ①) and only Discord, which we have none of, stays out.
	/// </summary>
	private static readonly string[] HelpLeftOut = ["JoinDiscordCommand"];

	/// <summary>The help items' names (mockup 20261022-program-update-v5 ①), by their command.</summary>
	private static readonly (string Command, Func<string> Header)[] HelpHeaders =
	[
		("ViewHelpCommand", () => ForkStrings.HelpOnline),
		("ReportIssueCommand", () => ForkStrings.HelpReportIssue),
		("CheckForUpdateCommand", () => ForkStrings.CheckUpdates),
		("OpenReleaseNotesCommand", () => ForkStrings.OpenReleasePage),
	];

	// Top to bottom; null is a separator.
	private static readonly string?[] PanelOrder =
	[
		"FormMain.StripMenu_View_Fleet",
		"FormMain.StripMenu_View_FleetOverview",
		"FormMain.StripMenu_View_Battle",
		"FormMain.StripMenu_View_Compass",
		"FormMain.StripMenu_View_BaseAirCorps",
		"FormMain.StripMenu_View_ShipGroup",
		"FormMain.StripMenu_View_FleetPreset",
		null,
		"ForkExpeditionMenuHeader",
		"FormMain.StripMenu_Tool_ExpeditionCheck",
		"FormMain.StripMenu_View_Dock",
		"FormMain.StripMenu_View_Arsenal",
		"FormMain.EquipmentUpgradePlanViewerTitle",
		null,
		"FormMain.StripMenu_View_Headquarters",
		"FormMain.StripMenu_View_Quest",
		"FormMain.StripMenu_View_ShipTrainingPlanViewer",
		"FormMain.SenkaLeaderboard",
		"FormMain.StripMenu_View_Information",
		null,
		"FormMain.StripMenu_View_Browser",
		// the screenshot panel under the browser (mockup 20261009-screenshot-capture-v2)
		"ForkScreenshotMenuHeader",
		"FormMain.StripMenu_WindowCapture",
		"FormMain.StripMenu_View_Log",
		"FormMain.StripMenu_View_Json",
	];

	// Tool menu mockup 4: the fork's windows, records, encyclopedias, calculators and planners, web pages, the rest.
	private static readonly string?[] ToolOrder =
	[
		"ForkShipListMenuHeader",
		"FormMain.StripMenu_Tool_EquipmentList",
		"ForkFleetDetailMenuHeader",
		"ForkAirBaseDetailMenuHeader",
		"ForkMapHpMenuHeader",
		null,
		"FormMain.SortieRecordViewer",
		"FormMain.ExpeditionRecordViewer",
		"FormMain.StripMenu_Tool_DropRecord",
		"FormMain.StripMenu_Tool_DevelopmentRecord",
		"FormMain.StripMenu_Tool_ConstructionRecord",
		"FormMain.StripMenu_Tool_ResourceChart",
		"FormMain.SenkaViewer",
		null,
		"FormMain.StripMenu_Tool_AlbumMasterShip",
		"FormMain.StripMenu_Tool_AlbumMasterEquipment",
		null,
		"FormMain.StripMenu_Tool_AntiAirDefense",
		"FormMain.StripMenu_Tool_ExpChecker",
		"FormMain.ExpeditionCalculatorTitle",
		"FormMain.EquipmentUpgradePlannerTitle",
		"FormMain.EventLockPlannerTitle",
		"FormMain.StripMenu_Tool_QuestTrackerManager",
		"FormMain.StripMenu_Tool_KancolleProgress",
		"FormMain.StripMenu_Tool_FleetImageGenerator",
		null,
		"FormMain.AirControlSimulator",
		"FormMain.OperationRoom",
		"FormMain.CompassPrediction",
		"FormMain.StripMenu_Tool_ExtraBrowser",
		null,
		"FormMain.AutoRefreshTitle",
		"FormMain.UiBlocker",
		// Db explorer (development builds only): its header is plain text
		"OpenDatabaseExplorerCommand",
	];

	// Korean screens only (user decision): these headers are titles of panels and windows, or the fork's own strings,
	// which have no access key, so the menu adds one. The other Korean keys are in the viewer's resources (원정 조건 체크
	// K: MainResources.ko-KR.resx). The web pages of the tool menu take digits, as letters ran short (tool menu mockup 4).
	private static readonly Dictionary<string, char> KoreanAccessKeys = new()
	{
		["FormMain.StripMenu_View_ShipTrainingPlanViewer"] = 'T',
		["FormMain.EquipmentUpgradePlanViewerTitle"] = 'U',
		["FormMain.SenkaLeaderboard"] = 'R',

		["ForkShipListMenuHeader"] = 'L',
		["ForkFleetDetailMenuHeader"] = 'T',
		["ForkAirBaseDetailMenuHeader"] = 'B',
		["ForkMapHpMenuHeader"] = 'M',
		["FormMain.SortieRecordViewer"] = 'O',
		["FormMain.ExpeditionRecordViewer"] = 'P',
		["FormMain.SenkaViewer"] = 'K',
		["FormMain.ExpeditionCalculatorTitle"] = 'J',
		["FormMain.EquipmentUpgradePlannerTitle"] = 'U',
		["FormMain.EventLockPlannerTitle"] = 'N',
		["FormMain.StripMenu_Tool_QuestTrackerManager"] = 'Y',
		["FormMain.StripMenu_Tool_KancolleProgress"] = 'G',
		["FormMain.AirControlSimulator"] = '1',
		["FormMain.OperationRoom"] = '2',
		["FormMain.CompassPrediction"] = '3',
		["FormMain.StripMenu_Tool_ExtraBrowser"] = '4',
		["FormMain.AutoRefreshTitle"] = 'H',
		["FormMain.UiBlocker"] = 'I',

		// file menu (menu check of 2026-10-05)
		["FormMain.ResetFloatingWindows"] = 'R',
		["FormMain.Adjustment"] = 'A',
	};

	public static void Install(System.Windows.Window window)
	{
		Arrange(window, View, PanelOrder);
		Arrange(window, Tools, ToolOrder);
		if (FindMenu(window, File) is { } file) AddKoreanKeys(file);
		TidyHelp(window);
	}

	/// <summary>The panels of the plugins, a separator and 플러그인 폴더 열기; only the folder item without plugins.</summary>
	public static void FillPluginMenu(System.Windows.Window window, IList<PluginPanelViewModel> panels, ICommand openFolder)
	{
		if (FindMenu(window, Plugins) is not { } menu) return;

		menu.Items.Clear();
		foreach (PluginPanelViewModel panel in panels)
		{
			MenuItem item = new() { DataContext = panel };
			item.SetBinding(HeaderedItemsControl.HeaderProperty, nameof(PluginPanelViewModel.Title));
			item.SetBinding(MenuItem.CommandProperty, nameof(PluginPanelViewModel.ShowCommand));
			menu.Items.Add(item);
		}

		if (menu.Items.Count > 0) menu.Items.Add(new Separator());
		menu.Items.Add(new MenuItem { Header = ForkStrings.OpenPluginFolder, Command = openFolder });
	}

	private static void TidyHelp(System.Windows.Window window)
	{
		if (FindMenu(window, Help) is not { } menu) return;

		foreach (MenuItem item in menu.Items.OfType<MenuItem>().Where(m => HelpLeftOut.Contains(CommandPath(m))).ToList())
		{
			menu.Items.Remove(item);
		}

		// no separator left at the top, the end or twice
		List<object> items = menu.Items.Cast<object>().ToList();
		for (int i = items.Count - 1; i >= 0; i--)
		{
			if (items[i] is Separator && (i == 0 || i == items.Count - 1 || items[i - 1] is Separator))
			{
				menu.Items.Remove(items[i]);
				items.RemoveAt(i);
			}
		}

		// our names: the update check also looks for the viewer's own release (Fork/Update), the links go to our repository
		foreach (MenuItem item in menu.Items.OfType<MenuItem>())
		{
			Func<string>? header = HelpHeaders.Where(h => h.Command == CommandPath(item)).Select(h => h.Header).FirstOrDefault();
			if (header is null) continue;

			BindingOperations.ClearBinding(item, HeaderedItemsControl.HeaderProperty);
			item.Header = header();
		}
	}

	/// <summary>The Korean screen's access keys of a menu's items at every depth (the file menu's 조정, 플로팅 윈도우를 리셋).</summary>
	private static void AddKoreanKeys(MenuItem menu)
	{
		if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "ko") return;

		foreach (MenuItem item in menu.Items.OfType<MenuItem>())
		{
			if (HeaderPath(item) is { } path && KoreanAccessKeys.TryGetValue(path, out char key))
			{
				item.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding(path) { Converter = new AccessKeyConverter(key) });
			}

			AddKoreanKeys(item);
		}
	}

	private static void Arrange(System.Windows.Window window, string menuPath, string?[] order)
	{
		if (FindMenu(window, menuPath) is not { } menu) return;

		List<object> items = menu.Items.Cast<object>().ToList();
		List<MenuItem> menuItems = items.OfType<MenuItem>().ToList();
		List<object> ordered = [];

		foreach (string? path in order)
		{
			if (path is null)
			{
				ordered.Add(new Separator());
				continue;
			}

			// every item with this header (the viewer has a second, hidden 함선 그룹)
			List<MenuItem> matches = menuItems.Where(m => KeyOf(m) == path).ToList();
			ordered.AddRange(matches);
			menuItems.RemoveAll(matches.Contains);
		}

		if (menuItems.Count > 0)
		{
			ordered.Add(new Separator());
			ordered.AddRange(menuItems);
		}

		menu.Items.Clear();
		foreach (object item in ordered) menu.Items.Add(item);

		if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ko")
		{
			foreach (MenuItem item in ordered.OfType<MenuItem>())
			{
				if (HeaderPath(item) is { } path && KoreanAccessKeys.TryGetValue(path, out char key))
				{
					item.SetBinding(HeaderedItemsControl.HeaderProperty, new Binding(path) { Converter = new AccessKeyConverter(key) });
				}
			}
		}
	}

	private static string? HeaderPath(MenuItem item) => BindingOperations.GetBinding(item, HeaderedItemsControl.HeaderProperty)?.Path?.Path;

	private static string? CommandPath(MenuItem item) => BindingOperations.GetBinding(item, MenuItem.CommandProperty)?.Path?.Path;

	private static string? KeyOf(MenuItem item) => HeaderPath(item) ?? CommandPath(item);

	/// <summary>A menu item by the binding path of its header, at any depth (also Update/ForkProgramUpdateUi).</summary>
	internal static MenuItem? FindMenu(DependencyObject root, string headerPath)
	{
		foreach (object child in LogicalTreeHelper.GetChildren(root))
		{
			if (child is not DependencyObject d) continue;
			if (d is MenuItem m && HeaderPath(m) == headerPath) return m;
			if (FindMenu(d, headerPath) is { } found) return found;
		}

		return null;
	}

	/// <summary>"개수 계획" → "개수 계획(_U)"; a header that already has an access key is left as it is.</summary>
	private sealed class AccessKeyConverter(char key) : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
			value is string s && !s.Contains('_') ? $"{s}(_{key})" : value;

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}
