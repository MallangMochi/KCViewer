using System.Windows;

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
}
