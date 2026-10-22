using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Data;
using KCViewer.Fork;
using KCViewer.Fork.KcvViews;
using KCViewer.Fork.Plugins;
using KCViewer.Utility;

namespace KCViewer.ViewModels;

/// <summary>
/// Fork: plugins and window hooks of the fork, kept out of FormMainViewModel.cs so upstream merges stay simple.
/// </summary>
public partial class FormMainViewModel
{
	/// <summary>Panels added by plugins, listed in the plugin menu (Fork/ForkMenuLayout.cs).</summary>
	public ObservableCollection<PluginPanelViewModel> ForkPluginPanels { get; } = new();

	// the top menus as decided (VIEWER-PLAN "상단 메뉴 개편"): 패널 (the viewer's 表示), 스킨, 플러그인
	public string ForkPanelsMenuHeader => ForkStrings.PanelsMenu;
	public string ForkSkinMenuHeader => ForkStrings.SkinMenu;
	public string ForkPluginsMenuHeader => ForkStrings.PluginsMenu;

	/// <summary>The plugin menu's last item: Plugins\ next to the exe, made when it is not there yet.</summary>
	[RelayCommand]
	private void ForkOpenPluginFolder()
	{
		string folder = PluginManager.PluginsFolder;
		try
		{
			Directory.CreateDirectory(folder);
			Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			Logger.Add(3, $"{folder}: {ex.Message}");
		}
	}

	public string ForkWindowTitle => FormMain.Title + ForkProfile.TitleSuffix;

	// access keys of the skin menu on the Korean screen (menu check of 2026-10-05)
	public string ForkLocalSkinMenuHeader => ForkStrings.WithKey(ForkStrings.LocalSkin, 'G');
	public string ForkLocalSkinEnabledHeader => ForkStrings.WithKey(ForkStrings.LocalSkinEnabled, 'U');
	public string ForkLocalSkinReloadHeader => ForkStrings.WithKey(ForkStrings.LocalSkinReload, 'R');
	public string ForkLocalSkinOpenFolderHeader => ForkStrings.WithKey(ForkStrings.LocalSkinOpenFolder, 'F');

	/// <summary>Local skin of this instance (requirement 12). Off by default; saved in Settings\Fork.json.</summary>
	public bool ForkLocalSkinEnabled
	{
		get => ForkSettings.Current.LocalSkin.Enabled;
		set
		{
			if (value == ForkSettings.Current.LocalSkin.Enabled) return;

			ForkSettings.Current.LocalSkin.Enabled = value;
			ForkSettings.Current.Save();
			OnPropertyChanged();
			Logger.Add(2, value ? ForkStrings.LocalSkinTurnedOn(ForkSettings.Current.LocalSkin.ResolvedFolder) : ForkStrings.LocalSkinTurnedOff);
			NotifyBrowserOfLocalSkin();
		}
	}

	/// <summary>Reads the skin folder again, after files were added or removed.</summary>
	[RelayCommand]
	private void ForkReloadLocalSkin() => NotifyBrowserOfLocalSkin();

	[RelayCommand]
	private void ForkOpenLocalSkinFolder()
	{
		string folder = ForkSettings.Current.LocalSkin.ResolvedFolder;
		try
		{
			Directory.CreateDirectory(folder);
			Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			Logger.Add(3, $"{folder}: {ex.Message}");
		}
	}

	public string ForkKcvStyleMenuHeader => ForkStrings.WithKey(ForkStrings.KcvStyle, 'K');
	public string ForkKcvStyleEnabledHeader => ForkStrings.WithKey(ForkStrings.KcvStyleEnabled, 'U');
	public string ForkThemeLightHeader => ForkStrings.WithKey(ForkStrings.ThemeLight, 'L');
	public string ForkThemeDarkHeader => ForkStrings.WithKey(ForkStrings.ThemeDark, 'D');
	public string ForkKcvAccentPurpleHeader => ForkStrings.WithKey(ForkStrings.AccentPurple, 'P');
	public string ForkKcvAccentBlueHeader => ForkStrings.WithKey(ForkStrings.AccentBlue, 'B');
	public string ForkKcvAccentOrangeHeader => ForkStrings.WithKey(ForkStrings.AccentOrange, 'O');

	/// <summary>KanColleViewer's look (Fork/KcvStyle.cs). On by default; saved in Settings\Fork.json.</summary>
	public bool ForkKcvStyleEnabled
	{
		get => ForkSettings.Current.KcvStyle.Enabled;
		set
		{
			if (value == ForkSettings.Current.KcvStyle.Enabled) return;

			ForkSettings.Current.KcvStyle.Enabled = value;
			ForkSettings.Current.Save();
			ApplyForkConfiguration();
		}
	}

	// a click on the checked item (false) changes nothing; the menu reads the state back so it stays checked
	public bool ForkKcvAccentPurple { get => IsForkAccent(KcvAccent.Purple); set { SetForkAccent(KcvAccent.Purple, value); OnPropertyChanged(); } }
	public bool ForkKcvAccentBlue { get => IsForkAccent(KcvAccent.Blue); set { SetForkAccent(KcvAccent.Blue, value); OnPropertyChanged(); } }
	public bool ForkKcvAccentOrange { get => IsForkAccent(KcvAccent.Orange); set { SetForkAccent(KcvAccent.Orange, value); OnPropertyChanged(); } }

	/// <summary>The viewer's own light and dark themes (Configuration > UI), here for quick switching.</summary>
	public bool ForkThemeLight { get => Config.UI.ThemeMode == 0; set { SetForkThemeMode(0, value); OnPropertyChanged(); } }
	public bool ForkThemeDark { get => Config.UI.ThemeMode == 1; set { SetForkThemeMode(1, value); OnPropertyChanged(); } }

	/// <summary>The status bar takes the accent colour, as KanColleViewer's does (style trigger in FormMainWpf.xaml).</summary>
	public bool ForkKcvStatusBarActive => ForkSettings.Current.KcvStyle.Enabled;
	public Brush? ForkKcvAccentBrush { get; private set; }

	private Color? ForkOriginalAccentColor { get; set; }
	private bool ForkOriginalAccentColorSaved { get; set; }

	private bool IsForkAccent(KcvAccent accent) => ForkSettings.Current.KcvStyle.AccentValue == accent;

	private void SetForkAccent(KcvAccent accent, bool value)
	{
		if (!value || IsForkAccent(accent)) return;

		ForkSettings.Current.KcvStyle.Accent = accent.ToString();
		ForkSettings.Current.Save();
		ApplyForkConfiguration();
	}

	private void SetForkThemeMode(int mode, bool value)
	{
		if (!value || Config.UI.ThemeMode == mode) return;

		Config.UI.ThemeMode = mode;
		ApplyForkConfiguration();
	}

	/// <summary>Runs the viewer's own configuration change, which calls <see cref="ApplyForkTheme"/> and tells the browser.</summary>
	private void ApplyForkConfiguration()
	{
		Configuration.Instance.OnConfigurationChanged();
		foreach (string name in new[]
		{
			nameof(ForkKcvStyleEnabled), nameof(ForkKcvAccentPurple), nameof(ForkKcvAccentBlue), nameof(ForkKcvAccentOrange),
			nameof(ForkThemeLight), nameof(ForkThemeDark),
		})
		{
			OnPropertyChanged(name);
		}
	}

	/// <summary>Called at the end of SetTheme, after the viewer applied its own theme.</summary>
	private void ApplyForkTheme()
	{
		if (!ForkOriginalAccentColorSaved)
		{
			ForkOriginalAccentColor = ModernWpf.ThemeManager.Current.AccentColor;
			ForkOriginalAccentColorSaved = true;
		}

		ForkSettings.KcvStyleSettings style = ForkSettings.Current.KcvStyle;
		if (style.Enabled)
		{
			bool dark = Config.UI.ThemeMode != 0;
			KcvStyle.AccentColors colors = KcvStyle.ColorsOf(style.AccentValue);
			CurrentTheme = KcvStyle.DockTheme(dark, style.AccentValue);
			ModernWpf.ThemeManager.Current.AccentColor = colors.Accent;
			SolidColorBrush accent = new(colors.Accent);
			accent.Freeze();
			ForkKcvAccentBrush = accent;
			if (Window is not null) KcvStyle.ApplyWindow(Window, dark, colors);
		}
		else
		{
			ModernWpf.ThemeManager.Current.AccentColor = ForkOriginalAccentColor;
			ForkKcvAccentBrush = null;
			if (Window is not null) KcvStyle.RestoreWindow(Window);
		}

		// The panels drawn as KanColleViewer's follow the same switch (Fork/KcvViews).
		KcvPanels.Apply(style.Enabled, Config.UI.ThemeMode != 0, KcvStyle.ColorsOf(style.AccentValue));

		OnPropertyChanged(nameof(ForkKcvStatusBarActive));
		OnPropertyChanged(nameof(ForkKcvAccentBrush));
	}

	public string ForkShipListMenuHeader => ForkStrings.KcvShipList;

	/// <summary>KanColleViewer's 所属艦娘一覧 (Fork/KcvViews); the menu item shows while the style is on.</summary>
	[RelayCommand]
	private void ForkOpenShipList() => new KcvShipListWindow().Show(Window);

	public string ForkExpeditionMenuHeader => ForkStrings.KcvExpeditionMenu;

	/// <summary>The small panel with the time left of the expeditions (Fork/KcvViews/KcvExpeditionViewModel.cs).</summary>
	public KcvExpeditionViewModel? ForkExpedition { get; private set; }

	public string ForkCombinedFleetMenuHeader => ForkStrings.KcvCombinedFleet;

	/// <summary>The combined fleet panel (Fork/KcvViews/KcvCombinedFleetViewModel.cs), a KanColleViewer style panel.</summary>
	public KcvCombinedFleetViewModel? ForkCombinedFleet { get; private set; }

	public string ForkScreenshotMenuHeader => ForkStrings.ScreenshotMenu;

	/// <summary>The screenshot panel, one button to place anywhere (Fork/ForkScreenshotPanelViewModel.cs), whatever the style.</summary>
	public ForkScreenshotPanelViewModel? ForkScreenshotPanel { get; private set; }

	public string ForkFleetDetailMenuHeader => ForkStrings.KcvFleetDetail;

	/// <summary>KanColleViewer's 艦隊詳細 for the first fleet; the fleet panels have a button for their own fleet.</summary>
	[RelayCommand]
	private void ForkOpenFleetDetail() => KcvFleetDetailWindow.Open(1, Window);

	public string ForkAirBaseDetailMenuHeader => ForkStrings.KcvAirBaseDetail;

	/// <summary>The land base details window (Fork/KcvViews), from its first area; the land base panel has the same button.</summary>
	[RelayCommand]
	private void ForkOpenAirBaseDetail() => KcvBaseAirCorpsDetailWindow.Open(0, Window);

	public string ForkMapHpMenuHeader => ForkStrings.KcvMapHp;

	/// <summary>The gauges of the open maps, as KanColleViewer's MapHP (Fork/KcvViews); always in the menu.</summary>
	[RelayCommand]
	private void ForkOpenMapHp() => KcvMapHpWindow.Open(Window);

	/// <summary>With the KanColleViewer style on, the equipment list opens as its 所有装備一覧 (Fork/KcvViews).</summary>
	private bool ForkOpenEquipmentList()
	{
		if (!KcvViewState.Current.Enabled) return false;

		new KcvEquipmentListWindow().Show(Window);
		return true;
	}

	/// <summary>
	/// With the KanColleViewer style on, the upgrade planner opens in its style (Fork/KcvViews), with the viewer's view
	/// model. Without equipment data the viewer's own method shows its message.
	/// </summary>
	private bool ForkOpenEquipmentUpgradePlanner()
	{
		if (!KcvViewState.Current.Enabled) return false;
		if (!KCDatabase.Instance.Equipments.Any()) return false;

		new KcvEquipmentUpgradePlannerWindow(new()).Show(Window);
		return true;
	}

	/// <summary>
	/// With the KanColleViewer style on, the record windows of the tools menu open in its style (Fork/KcvViews/KcvRecord*,
	/// VIEWER-PLAN.md "기록 묶음 구현") with the viewer's view models; the viewer's checks (master data, empty records)
	/// come first.
	/// </summary>
	private bool ForkOpenSortieRecord() => ForkOpenRecordWindow(() => new KcvSortieRecordWindow());

	private bool ForkOpenExpeditionRecord() => ForkOpenRecordWindow(() => new KcvExpeditionRecordWindow());

	private bool ForkOpenDropRecord() => ForkOpenRecordWindow(() => new KcvDropRecordWindow());

	private bool ForkOpenDevelopmentRecord() => ForkOpenRecordWindow(() => new KcvDevelopmentRecordWindow());

	private bool ForkOpenConstructionRecord() => ForkOpenRecordWindow(() => new KcvConstructionRecordWindow());

	private bool ForkOpenResourceChart() => ForkOpenRecordWindow(() => new KcvResourceChartWindow());

	private bool ForkOpenSenkaViewer() => ForkOpenRecordWindow(() => new KcvSenkaViewerWindow());

	/// <summary>
	/// With the KanColleViewer style on, the ship and equipment albums open in its style (Fork/KcvViews/KcvAlbum*,
	/// VIEWER-PLAN.md "도감 묶음") with the viewer's view models; the viewer's master data checks come first.
	/// </summary>
	private bool ForkOpenAlbumMasterShip() => ForkOpenRecordWindow(() => new KcvAlbumShipWindow());

	private bool ForkOpenAlbumMasterEquipment() => ForkOpenRecordWindow(() => new KcvAlbumEquipmentWindow());

	private bool ForkOpenRecordWindow(Func<System.Windows.Window> create)
	{
		if (!KcvViewState.Current.Enabled) return false;

		create().Show(Window);
		return true;
	}

	/// <summary>The browser then asks for its configuration and the skin folder again (BrowserViewModel.ConfigurationChanged).</summary>
	private static void NotifyBrowserOfLocalSkin()
	{
		try
		{
			global::KCViewer.Window.FormBrowserHost.Instance?.ConfigurationChanged();
		}
		catch (Exception ex)
		{
			Logger.Add(3, $"{ForkStrings.LocalSkin}: {ex.Message}");
		}
	}

	private void AddForkViews()
	{
		// Game images from the local server's assets folder before the cache the fork never fills.
		ForkAssets.Install();
		ForkGameAssets.Install();
		ForkGameAssets.PreloadIcons();

		// The log's daily cleanup at the set hour (entries older than the newest 1,000 to the log file).
		ForkLogCleanup.Install();

		// Once the window shows: has the game or KC3's translations changed since the last start (off by default).
		ForkUpdateWatch.Install(Window);

		// Template for plugin panels; ViewTemplateSelector falls back to implicit templates for unknown view models.
		Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
		{
			Source = new Uri("/KCViewer;component/Fork/ForkViews.xaml", UriKind.Relative),
		});

		// KanColleViewer designs of the panels; the selector is used when the layout creates the panels.
		KcvPanels.Install((global::KCViewer.Window.Wpf.ViewTemplateSelector)Window.Resources["ViewTemplateSelector"]);

		// Which maps are open (the last map list), for the MapHP window, which may be opened after that answer.
		KcvMapHpTracker.Install();

		// KanColleViewer's font on these panels while its style is on; their layout stays the viewer's.
		KcvPanelFonts.Follow(ExpeditionCheck);
		KcvPanelFonts.Follow(SenkaLeaderboardViewer);

		// Test profiles only: API traffic from a file instead of the browser (--replay-api=FILE).
		ApiReplay.Install(Window);

		// The panel menu in the order decided with the user; FormMainWpf.xaml keeps the viewer's order.
		ForkMenuLayout.Install(Window);

		// The viewer's own program updates: the menu bar item in place of 74식's, the check once the window shows.
		Fork.Update.ForkProgramUpdateUi.Install(Window);

		// Before the saved layout is restored, so the panel keeps its place (template in Fork/ForkViews.xaml).
		Views.Add(ForkExpedition = new KcvExpeditionViewModel());

		// The combined fleet panel, before the layout is restored too; it brings itself to the front as the fleet panels.
		Views.Add(ForkCombinedFleet = new KcvCombinedFleetViewModel(Fleets[0], Fleets[1], FleetOverview.ForkCombinedCard));
		ForkFleetPanelSwitch.SetCombinedPanel(ForkCombinedFleet);

		// The screenshot panel, before the layout is restored too; and the viewer's screenshot key, taken in any of its
		// windows while it is in front (Fork/ForkScreenshot.cs).
		Views.Add(ForkScreenshotPanel = new ForkScreenshotPanelViewModel());
		ForkScreenshot.Start();

		// Before the saved layout is restored (on Loaded), so plugin panels keep their place.
		PluginManager.LoadAll(panel =>
		{
			Views.Add(panel);
			ForkPluginPanels.Add(panel);
		});
		ForkMenuLayout.FillPluginMenu(Window, ForkPluginPanels, ForkOpenPluginFolderCommand);

		FormMain.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(FormMain.Title)) OnPropertyChanged(nameof(ForkWindowTitle));
		};

		if (ForkProfile.IsEmbedded)
		{
			// Shown inside the live window's test area panel. The window's own properties (WindowStyle,
			// ShowInTaskbar) stay as they are: changing them makes WPF add an owner window and rewrite the style.
			Window.SourceInitialized += (_, _) => EmbeddedWindow.Install(Window, ForkProfile.EmbedParent, ForkProfile.HostProcessId);
		}

		// Closed only fires once closing is confirmed. Plugins stop what they started (the test instance, the local server).
		Window.Closed += (_, _) => PluginManager.ShutDown();
	}
}
