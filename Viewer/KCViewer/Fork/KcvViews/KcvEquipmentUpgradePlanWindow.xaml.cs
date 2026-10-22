using System.Windows;
using KCViewer.Window.Tools.EquipmentUpgradePlanner;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the upgrade plan editor in KanColleViewer's style (see the XAML), opened through
/// <see cref="KcvCalcWindows.TryShowUpgradePlanEditor"/>: 「계획 추가」 for a plan not saved yet, else 「계획 편집」.
/// </summary>
public partial class KcvEquipmentUpgradePlanWindow
{
	public KcvEquipmentUpgradePlanWindow(EquipmentUpgradePlanItemViewModel viewModel, bool adding) : base(viewModel)
	{
		InitializeComponent();

		Title = adding ? ForkStrings.KcvUpgradePlanAdd : ForkStrings.KcvUpgradePlanEdit;
	}

	private void OnConfirmClick(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		DialogResult = false;
	}
}
