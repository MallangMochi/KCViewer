using System.ComponentModel;
using System.Windows;
using KCViewer.Core.Types;
using KCViewer.Window.Tools.ExpChecker;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: 필요 경험치 계산 in KanColleViewer's style (see the XAML), opened through <see cref="KcvCalcWindows"/> while the style is on.</summary>
public partial class KcvExpCheckerWindow
{
	public KcvExpCheckerWindow(ExpCheckerViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		ViewModel.PropertyChanged += ShowChanges;
		ShowSummary();
		ShowWarning();
	}

	private void ShowChanges(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			// the viewer writes its one summary line when it has the table of a ship
			case nameof(ViewModel.GroupExpText):
				ShowSummary();
				break;

			case nameof(ViewModel.WarningText):
				ShowWarning();
				break;
		}
	}

	/// <summary>
	/// The viewer's 「유다치 改 2 Lv. 82: Exp. 411500, 대잠 61 (현재 개수+0)」 as the name and 「Exp. 411500 · 대잠 61 (현재
	/// 개수 +0)」, from the ship of the table. Empty before a ship is chosen.
	/// </summary>
	private void ShowSummary()
	{
		if (string.IsNullOrEmpty(ViewModel.GroupExpText) || ViewModel.SelectedShip is not IShipData ship)
		{
			SummaryName.Text = "";
			SummaryValues.Text = "";
			return;
		}

		ExpCheckerTranslationViewModel text = ViewModel.ExpChecker;
		SummaryName.Text = ship.NameWithLevel;
		SummaryValues.Text = $"Exp. {ship.ExpTotal} · {text.ASW} {ship.ASWBase} ({text.Modernization} +{ship.ASWModernized})";
	}

	private void ShowWarning()
	{
		Warning.Visibility = string.IsNullOrEmpty(ViewModel.WarningText) ? Visibility.Collapsed : Visibility.Visible;
	}
}
