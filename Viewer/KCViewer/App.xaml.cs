using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Avalonia.Dialogs.ShipSelector;
using KCViewer.Avalonia.Services;
using KCViewer.Common;
using KCViewer.Core.Services;
using KCViewer.Core.Services.Data;
using KCViewer.Core.Types.Data;
using KCViewer.Data;
using KCViewer.Data.Bonodere;
using KCViewer.Database.DataMigration;
using KCViewer.Dialogs;
using KCViewer.Services;
using KCViewer.Utility;
using KCViewer.Utility.Data;
using KCViewer.Utility.ElectronicObserverApi;
using KCViewer.Utility.ElectronicObserverApi.DataIssueLogs;
using KCViewer.ViewModels;
using KCViewer.ViewModels.Translations;
using KCViewer.Window.Control.ShipFilter;
using KCViewer.Window.Settings;
using KCViewer.Window.Settings.Behavior;
using KCViewer.Window.Settings.BGM;
using KCViewer.Window.Settings.Connection;
using KCViewer.Window.Settings.DataSubmission;
using KCViewer.Window.Settings.Debugging;
using KCViewer.Window.Settings.Log;
using KCViewer.Window.Settings.Notification;
using KCViewer.Window.Settings.Notification.Base;
using KCViewer.Window.Settings.SubWindow;
using KCViewer.Window.Settings.SubWindow.AirBase;
using KCViewer.Window.Settings.SubWindow.Arsenal;
using KCViewer.Window.Settings.SubWindow.Browser;
using KCViewer.Window.Settings.SubWindow.Combat;
using KCViewer.Window.Settings.SubWindow.Compass;
using KCViewer.Window.Settings.SubWindow.Dock;
using KCViewer.Window.Settings.SubWindow.Fleet;
using KCViewer.Window.Settings.SubWindow.Group;
using KCViewer.Window.Settings.SubWindow.Headquarters;
using KCViewer.Window.Settings.SubWindow.Json;
using KCViewer.Window.Settings.SubWindow.Quest;
using KCViewer.Window.Settings.SubWindow.ShipTraining;
using KCViewer.Window.Settings.UI;
using KCViewer.Window.Settings.Window;
using KCViewer.Window.Tools.AirControlSimulator;
using KCViewer.Window.Tools.AirDefense;
using KCViewer.Window.Tools.AutoRefresh;
using KCViewer.Window.Tools.ConstructionRecordViewer;
using KCViewer.Window.Tools.DevelopmentRecordViewer;
using KCViewer.Window.Tools.DialogAlbumMasterEquipment;
using KCViewer.Window.Tools.DialogAlbumMasterEquipment.EquipmentUpgrade;
using KCViewer.Window.Tools.DialogAlbumMasterShip;
using KCViewer.Window.Tools.DropRecordViewer;
using KCViewer.Window.Tools.EquipmentList;
using KCViewer.Window.Tools.EquipmentUpgradePlanner;
using KCViewer.Window.Tools.EventLockPlanner;
using KCViewer.Window.Tools.ExpChecker;
using KCViewer.Window.Tools.ExpeditionRecordViewer;
using KCViewer.Window.Tools.FleetImageGenerator;
using KCViewer.Window.Tools.SenkaViewer;
using KCViewer.Window.Tools.SortieRecordViewer;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle;
using KCViewer.Window.Tools.SortieRecordViewer.Sortie.Battle.Phase;
using KCViewer.Window.Tools.SortieRecordViewer.SortieCostViewer;
using KCViewer.Window.Tools.SortieRecordViewer.SortieDetail;
using KCViewer.Window.Wpf;
using KCViewer.Window.Wpf.EquipmentUpgradePlanViewer;
using KCViewer.Window.Wpf.ExpeditionCheck;
using KCViewer.Window.Wpf.SenkaLeaderboard;
using KCViewer.Window.Wpf.ShipTrainingPlanner;
using Jot;
using Jot.Storage;
using Microsoft.Extensions.DependencyInjection;
using Application = System.Windows.Application;
using ShutdownMode = System.Windows.ShutdownMode;

namespace KCViewer;

public partial class App
{
	public static new App? Current => (App)Application.Current;
	public static FormMainViewModel MainViewModel => (FormMainViewModel)Application.Current!.MainWindow!.DataContext;
	private AppBuilder AvaloniaApp { get; }

	public App()
	{
		// Fork: switch to the profile's working folder before anything reads a relative path.
		Fork.ForkProfile.Apply();

		// Fork: auto-hidden panels open and collapse by clicking their tab, not on hover, and are repainted when shown.
		Fork.ForkAutoHide.Register();

		AvaloniaApp = Avalonia.Program
			.BuildAvaloniaApp()
			.SetupWithoutStarting();

		DispatcherUnhandledException += (sender, args) =>
		{
			if (args.Exception.StackTrace?.Contains("AvalonDock.Controls.TransformExtensions.TransformActualSizeToAncestor") ?? false)
			{
				// hack: ignore the exception when trying to resize the autohide area
				args.Handled = true;
				return;
			}

			if (args.Exception.StackTrace?.Contains("MS.Internal.PointUtil.ScreenToClient(Point pointScreen, PresentationSource presentationSource)") is true)
			{
				// hack: this exception is sometime thrown by WPF after moving elements in layout
				args.Handled = true;
				return;
			}

			// https://stackoverflow.com/questions/12769264/openclipboard-failed-when-copy-pasting-data-from-wpf-datagrid
			const int CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0);

			if (args.Exception is not COMException { ErrorCode: CLIPBRD_E_CANT_OPEN }) return;

			Logger.Add(3, MainResources.CopyingToClipboardFailed);
			args.Handled = true;
		};
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		bool allowMultiInstance = e.Args.Contains("-m") || e.Args.Contains("--multi-instance");


		// Fork: one mutex per working folder, so the live and test instances can run side by side.
		using (var mutex = new Mutex(false, Fork.ForkProfile.MutexName,
			out var created))
		{

			/*
			bool hasHandle = false;

			try
			{
				hasHandle = mutex.WaitOne(0, false);
			}
			catch (AbandonedMutexException)
			{
				hasHandle = true;
			}
			*/

			if (!created && !allowMultiInstance)
			{
				System.Windows.Window temp = new() { Visibility = Visibility.Hidden };
				temp.Show();

				// Fork: the viewer's own name
				string caption = Fork.ForkStrings.ProductDisplayName;

				// 多重起動禁止
				MessageBox.Show
				(
					KCViewer.Translations.Resources.MultiInstanceNotification,
					caption,
					MessageBoxButton.OK,
					MessageBoxImage.Exclamation
				);

				Shutdown();
				return;
			}

			System.Windows.Forms.Application.EnableVisualStyles();
			System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

			try
			{
				Directory.CreateDirectory(@"Settings\Layout");
			}
			catch (UnauthorizedAccessException)
			{
				MessageBox.Show(MainResources.MissingPermissions,
					MainResources.ErrorCaption,
					MessageBoxButton.OK, MessageBoxImage.Error);
				throw;
			}

			Configuration.Instance.Load();

			ConfigureServices();

			ToolTipService.ShowDurationProperty.OverrideMetadata(
				typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));
			ToolTipService.InitialShowDelayProperty.OverrideMetadata(
				typeof(DependencyObject), new FrameworkPropertyMetadata(0));

			UpdateTheme();
			UpdateFont();

			Configuration.Instance.ConfigurationChanged += UpdateTheme;
			Configuration.Instance.ConfigurationChanged += UpdateFont;

			FormMainWpf mainWindow = new();

			MainWindow = mainWindow;
			ShutdownMode = ShutdownMode.OnMainWindowClose;

			mainWindow.ShowDialog();
		}
	}

	private void UpdateTheme()
	{
		if (AvaloniaApp.Instance is not Avalonia.App app) return;

		ThemeVariant themeVariant = Configuration.Config.UI.ThemeMode switch
		{
			0 => ThemeVariant.Light, // light theme => light theme
			1 => ThemeVariant.Dark, // dark theme => dark theme
			_ => ThemeVariant.Dark, // custom theme => dark theme
		};

		app.UpdateTheme(themeVariant);
	}

	private void UpdateFont()
	{
		FontOverrides? overrides = Resources.MergedDictionaries
			.OfType<FontOverrides>()
			.FirstOrDefault();

		if (overrides is null) return;
		if (Configuration.Config.UI.MainFont.FontData is null) return;

		string fontName = Configuration.Config.UI.MainFont.FontData.Name;
		double fontSize = Configuration.Config.UI.MainFont.FontData.ToSize();

		overrides.FontFamily = new FontFamily(fontName);
		overrides.FontSize = fontSize;

		if (AvaloniaApp.Instance is not Avalonia.App app) return;

		app.UpdateFont(fontName, fontSize);
	}

	private void ConfigureServices()
	{
		ServiceProvider services = new ServiceCollection()
			.AddSingleton<IKCDatabase>(KCDatabase.Instance)
			.AddSingleton<IConfigurationConnection>(Configuration.Config.Connection)
			.AddSingleton<IConfigurationUi>(Configuration.Config.UI)
			.AddDialogServices()
			// config translations
			.AddSingleton<ConfigurationTranslationViewModel>()
			.AddSingleton<ConfigurationConnectionTranslationViewModel>()
			.AddSingleton<ConfigurationUITranslationViewModel>()
			.AddSingleton<ConfigurationLogTranslationViewModel>()
			.AddSingleton<ConfigurationBehaviorTranslationViewModel>()
			.AddSingleton<ConfigurationDebugTranslationViewModel>()
			.AddSingleton<ConfigurationWindowTranslationViewModel>()
			.AddSingleton<ConfigurationSubWindowTranslationViewModel>()
			.AddSingleton<ConfigurationNotificationTranslationViewModel>()
			.AddSingleton<ConfigurationNotificationBaseTranslationViewModel>()
			.AddSingleton<ConfigurationBGMTranslationViewModel>()
			.AddSingleton<SoundHandleEditTranslationViewModel>()
			.AddSingleton<ConfigurationFleetTranslationViewModel>()
			.AddSingleton<ConfigurationArsenalTranslationViewModel>()
			.AddSingleton<ConfigurationDockTranslationViewModel>()
			.AddSingleton<ConfigurationHeadquartersTranslationViewModel>()
			.AddSingleton<ConfigurationCompassTranslationViewModel>()
			.AddSingleton<ConfigurationQuestTranslationViewModel>()
			.AddSingleton<ConfigurationGroupTranslationViewModel>()
			.AddSingleton<ConfigurationCombatTranslationViewModel>()
			.AddSingleton<ConfigurationBrowserTranslationViewModel>()
			.AddSingleton<ConfigurationAirBaseTranslationViewModel>()
			.AddSingleton<ConfigurationJsonTranslationViewModel>()
			.AddSingleton<ConfigurationShipTrainingTranslationViewModel>()
			.AddSingleton<ConfigurationDataSubmissionTranslationViewModel>()
			// view translations
			.AddSingleton<FormArsenalTranslationViewModel>()
			.AddSingleton<FormBaseAirCorpsTranslationViewModel>()
			.AddSingleton<FormBattleTranslationViewModel>()
			.AddSingleton<FormBrowserHostTranslationViewModel>()
			.AddSingleton<FormCompassTranslationViewModel>()
			.AddSingleton<FormDockTranslationViewModel>()
			.AddSingleton<FormFleetTranslationViewModel>()
			.AddSingleton<FormFleetOverviewTranslationViewModel>()
			.AddSingleton<FormFleetPresetTranslationViewModel>()
			.AddSingleton<FormHeadquartersTranslationViewModel>()
			.AddSingleton<FormInformationTranslationViewModel>()
			.AddSingleton<FormJsonTranslationViewModel>()
			.AddSingleton<FormLogTranslationViewModel>()
			.AddSingleton<FormMainTranslationViewModel>()
			.AddSingleton<FormQuestTranslationViewModel>()
			.AddSingleton<FormShipGroupTranslationViewModel>()
			.AddSingleton<FormWindowCaptureTranslationViewModel>()
			.AddSingleton<EquipmentUpgradePlanViewerTranslationViewModel>()
			// tool translations
			.AddSingleton<DialogAlbumMasterShipTranslationViewModel>()
			.AddSingleton<DialogAlbumMasterEquipmentTranslationViewModel>()
			.AddSingleton<DialogDevelopmentRecordViewerTranslationViewModel>()
			.AddSingleton<SortieRecordViewerTranslationViewModel>()
			.AddSingleton<SortieCostViewerTranslationViewModel>()
			.AddSingleton<ExpeditionRecordViewerTranslationViewModel>()
			.AddSingleton<DialogDropRecordViewerTranslationViewModel>()
			.AddSingleton<DialogConstructionRecordViewerTranslationViewModel>()
			.AddSingleton<DialogResourceChartTranslationViewModel>()
			.AddSingleton<SenkaViewerTranslationViewModel>()
			.AddSingleton<DialogEquipmentListTranslationViewModel>()
			.AddSingleton<AirDefenseTranslationViewModel>()
			.AddSingleton<QuestTrackerManagerTranslationViewModel>()
			.AddSingleton<EventLockPlannerTranslationViewModel>()
			.AddSingleton<ShipFilterTranslationViewModel>()
			.AddSingleton<AirControlSimulatorTranslationViewModel>()
			.AddSingleton<AutoRefreshTranslationViewModel>()
			.AddSingleton<ExpeditionCheckTranslationViewModel>()
			.AddSingleton<FleetImageGeneratorTranslationViewModel>()
			.AddSingleton<ExpCheckerTranslationViewModel>()
			.AddSingleton<ShipTrainingPlannerTranslationViewModel>()
			.AddSingleton<EquipmentUpgradePlannerTranslationViewModel>()
			.AddSingleton<AlbumMasterEquipmentUpgradeTranslationViewModel>()
			.AddSingleton<SortieDetailTranslationViewModel>()
			.AddSingleton<ElectronicObserverApiTranslationViewModel>()
			.AddSingleton<BonodereSubmissionTranslationViewModel>()
			.AddSingleton<SenkaLeaderboardTranslationViewModel>()
			// tools
			.AddSingleton<AutoRefreshViewModel>()
			.AddSingleton<ShipTrainingPlanViewerViewModel>()
			.AddSingleton<PhaseFactory>()
			.AddSingleton<BattleFactory>()
			// services
			.AddSingleton<DataSerializationService>()
			.AddSingleton<ToolService>()
			.AddSingleton<TransliterationService>()
			.AddSingleton<GameResourceHelper>()
			.AddSingleton<GameAssetDownloaderService>()
			.AddSingleton<ImageLoadService>()
			.AddSingleton<FileService>()
			.AddSingleton<EquipmentUpgradePlanManager>()
			.AddSingleton<TimeChangeService>()
			.AddSingleton<ColorService>()
			.AddSingleton<ElectronicObserverApiService>()
			.AddSingleton<SortieRecordMigrationService>()
			.AddSingleton<SenkaLeaderboardManager>()
			.AddSingleton<BonodereSubmissionService>()
			.AddSingleton<IClipboardService, ClipboardService>()
			.AddSingleton<ShipSelectorFactory>()
			.AddSingleton<ITransportGaugeService, TransportGaugeService>()
			// issue reporter
			.AddSingleton<DataAndTranslationIssueReporter>()
			.AddSingleton<FitBonusIssueReporter>()
			.AddSingleton<WrongUpgradesIssueReporter>()
			.AddSingleton<WrongUpgradesCostIssueReporter>()
			.AddSingleton<SoftwareIssueReporter>()
			// external
			.AddSingleton(JotTracker())

			.BuildServiceProvider();

		Ioc.Default.ConfigureServices(services);

		Ioc.Default.GetRequiredService<DataAndTranslationIssueReporter>();
		Ioc.Default.GetRequiredService<ITransportGaugeService>();
	}

	private static Tracker JotTracker()
	{
		Tracker tracker = new(new JsonFileStore(@"Settings\WindowStates"));

		tracker
			.Configure<System.Windows.Window>()
			.Id(w => w.Name)
			.Property(w => w.Top)
			.Property(w => w.Left)
			.Property(w => w.Height)
			.Property(w => w.Width)
			.Property(w => w.WindowState)
			.PersistOn(nameof(System.Windows.Window.Closed))
			.StopTrackingOn(nameof(System.Windows.Window.Closed));

		// EventLockPlannerWindow extends System.Windows.Window so the config above applies to it
		tracker
			.Configure<EventLockPlannerWindow>()
			.Property(w => w.ViewModel.ShowFinishedPhases)
			.Property(w => w.ViewModel.DisplayToolTips);

		tracker
			.Configure<DialogAlbumMasterShipWpf>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<DialogAlbumMasterEquipmentWpf>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<EquipmentListWindow>()
			.Property(w => w.ViewModel.EquipmentGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.EquipmentGridViewModel.SortDescriptions)
			.Property(w => w.ViewModel.EquipmentGridWidth)
			.Property(w => w.ViewModel.EquipmentDetailGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.EquipmentDetailGridViewModel.SortDescriptions);

		tracker
			.Configure<ExpeditionCheckView>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<EquipmentUpgradePlanViewerViewModel>()
			.Property(w => w.Filters.DisplayFinished)
			.Property(w => w.Filters.SelectAllDay)
			.Property(w => w.Filters.SelectToday)
			.Property(w => w.DataGridViewModel.ColumnProperties)
			.Property(w => w.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<ShipTrainingPlanViewerViewModel>()
			.Property(w => w.DataGridViewModel.ColumnProperties)
			.Property(w => w.DataGridViewModel.SortDescriptions)
			.Property(w => w.DisplayFinished);

		tracker
			.Configure<EquipmentUpgradePlannerWindow>()
			.Property(w => w.ViewModel.Filters.DisplayFinished)
			.Property(w => w.ViewModel.Filters.SelectAllDay)
			.Property(w => w.ViewModel.Filters.SelectToday)
			.Property(w => w.ViewModel.EquipmentUpgradePlanManager.CompactMode)
			.Property(w => w.ViewModel.PlanListWidth)
			.Property(w => w.ViewModel.PlannedUpgradesPager.ItemsPerPage);

		tracker
			.Configure<ExpCheckerWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<BaseAirCorpsSimulationContentDialog>()
			.Property(w => w.ViewModel.MaxAircraftLevelFleet)
			.Property(w => w.ViewModel.MaxAircraftLevelAirBase);

		tracker
			.Configure<AirDefenseWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<ConfigurationWindow>()
			.Property(w => w.ViewModel.BGM.ColumnProperties)
			.Property(w => w.ViewModel.BGM.SortDescriptions);

		tracker
			.Configure<ConstructionRecordViewerWindow>()
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.SortDescriptions)
			.Property(w => w.ViewModel.DataGridMergedRowsAllViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridMergedRowsAllViewModel.SortDescriptions)
			.Property(w => w.ViewModel.DataGridMergedRowsFilteredByShipViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridMergedRowsFilteredByShipViewModel.SortDescriptions);

		tracker
			.Configure<DevelopmentRecordViewerWindow>()
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.SortDescriptions)
			.Property(w => w.ViewModel.DataGridMergedRowsViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridMergedRowsViewModel.SortDescriptions);

		tracker
			.Configure<DropRecordViewerWindow>()
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridRawRowsViewModel.SortDescriptions)
			.Property(w => w.ViewModel.DataGridMergedRowsViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridMergedRowsViewModel.SortDescriptions);

		tracker
			.Configure<EventLockPlannerWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<SenkaViewerWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<SortieRecordViewerWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<ExpeditionRecordViewerWindow>()
			.Property(w => w.ViewModel.DataGridViewModel.ColumnProperties)
			.Property(w => w.ViewModel.DataGridViewModel.SortDescriptions);

		tracker
			.Configure<SenkaLeaderboardManager>()
			.Property(w => w.CurrentCutoffData.DataGridViewModel.ColumnProperties)
			.Property(w => w.CurrentCutoffData.DataGridViewModel.SortDescriptions)
			.Property(w => w.CurrentCutoffData.PagingViewModel.ItemsPerPage);

		// Fork: the column settings of the KanColleViewer style record windows
		Fork.KcvViews.KcvRecordWindows.ConfigureTracker(tracker);

		return tracker;
	}
}
