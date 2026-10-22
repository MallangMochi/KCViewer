using System.Windows;
using System.Windows.Controls;
using KCViewer.Window.Wpf.Arsenal;
using KCViewer.Window.Wpf.BaseAirCorps;
using KCViewer.Window.Wpf.Battle;
using KCViewer.Window.Wpf.Compass;
using KCViewer.Window.Wpf.Dock;
using KCViewer.Window.Wpf.Fleet;
using KCViewer.Window.Wpf.FleetOverview;
using KCViewer.Window.Wpf.FleetPreset;
using KCViewer.Window.Wpf.Headquarters;
using KCViewer.Window.Wpf.Quest;
using KCViewer.Window.Wpf.WinformsHost;
using KCViewer.Window.Wpf.InformationView;
using KCViewer.Window.Wpf.Log;
using KCViewer.Window.Wpf.ExpeditionCheck;
using KCViewer.Window.Wpf.EquipmentUpgradePlanViewer;
using KCViewer.Window.Wpf.SenkaLeaderboard;
using KCViewer.Window.Wpf.ShipGroupAvalonia;
using KCViewer.Window.Wpf.ShipGroupWinforms;
using KCViewer.Window.Wpf.ShipTrainingPlanner;

namespace KCViewer.Window.Wpf;

public class ViewTemplateSelector : DataTemplateSelector
{
	public DataTemplate? Fleet { get; set; }
	public DataTemplate? FleetOverview { get; set; }
	public DataTemplate? Group { get; set; }
	public DataTemplate? GroupWinforms { get; set; }
	public DataTemplate? GroupAvalonia { get; set; }
	public DataTemplate? FleetPreset { get; set; }
	public DataTemplate? ShipTrainingPlanViewer { get; set; }
	public DataTemplate? SenkaLeaderboardViewer { get; set; }

	public DataTemplate? Dock { get; set; }
	public DataTemplate? Arsenal { get; set; }
	public DataTemplate? EquipmentUpgradePlanViewer { get; set; }
	public DataTemplate? BaseAirCorps { get; set; }

	public DataTemplate? Headquarters { get; set; }
	public DataTemplate? Quest { get; set; }
	public DataTemplate? ExpeditionCheck { get; set; }
	public DataTemplate? Information { get; set; }

	public DataTemplate? Compass { get; set; }
	public DataTemplate? Battle { get; set; }

	public DataTemplate? BrowserHost { get; set; }
	public DataTemplate? Log { get; set; }
	public DataTemplate? Json { get; set; }

	public DataTemplate? WinformsHost { get; set; }

	public override DataTemplate? SelectTemplate(object item, DependencyObject container) => item switch
	{
		FleetViewModel => Fleet,
		FleetOverviewViewModel => FleetOverview,
		ShipGroupWinformsViewModel => GroupWinforms,
		ShipGroupAvaloniaViewModel => GroupAvalonia,
		FleetPresetViewModel => FleetPreset,
		ShipTrainingPlanViewerViewModel => ShipTrainingPlanViewer,
		LogViewModel => Log,
		DockViewModel => Dock,
		ArsenalViewModel => Arsenal,
		EquipmentUpgradePlanViewerViewModel => EquipmentUpgradePlanViewer,
		BaseAirCorpsViewModel => BaseAirCorps,
		InformationViewModel => Information,
		HeadquartersViewModel => Headquarters,
		QuestViewModel => Quest,
		ExpeditionCheckViewModel => ExpeditionCheck,
		SenkaLeaderboardViewModel => SenkaLeaderboardViewer,

		CompassViewModel => Compass,
		BattleViewModel => Battle,

		// Fork: the JSON panel's own template when one is set (the KanColleViewer style's, KcvPanels.Install)
		WinformsWrappers.FormJsonViewModel when Json is not null => Json,
		WinformsHostViewModel => WinformsHost,

		_ => base.SelectTemplate(item, container)
	};
}
