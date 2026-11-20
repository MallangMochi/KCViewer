using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Window.Tools.EquipmentUpgradePlanner;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the planned upgrades panel in KanColleViewer's style (see the XAML).</summary>
public partial class KcvEquipmentUpgradePlanView
{
	public KcvEquipmentUpgradePlanView()
	{
		InitializeComponent();
	}

	/// <summary>
	/// 개수 계획 관리: the tools menu's command, so the window opens as from the menu (in this style, or the viewer's
	/// message without equipment data).
	/// </summary>
	private void OpenPlanner(object sender, RoutedEventArgs e)
	{
		if (Application.Current.MainWindow is not FormMainWpf { ViewModel: { } main }) return;

		main.OpenEquipmentUpgradePlannerCommand.Execute(null);
	}

	/// <summary>
	/// 편집 of a plan row's menu: that plan in the plan editor and saved on [OK], as the planner's [편집]
	/// (<see cref="EquipmentUpgradePlannerViewModel"/>'s OpenEditDialog); the planner window does not open.
	/// </summary>
	private void EditPlan(object sender, RoutedEventArgs e)
	{
		if (sender is not FrameworkElement { DataContext: EquipmentUpgradePlanItemViewModel plan }) return;
		if (!plan.OpenPlanDialog()) return;

		Ioc.Default.GetRequiredService<EquipmentUpgradePlanManager>().Save();
	}
}
