using System.Collections.Generic;
using System.Windows;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the ship album's 수치 편집 in KanColleViewer's style (see the XAML), opened through
/// <see cref="KcvShipAlbumTools.TryShowParameterEditor"/>. OK waits while a number field holds something else.
/// </summary>
public partial class KcvShipParameterWindow
{
	private KcvShipParameterViewModel ViewModel { get; }
	private HashSet<KcvNumberBox> Invalid { get; } = [];

	public KcvShipParameterWindow(KcvShipParameterViewModel viewModel)
	{
		InitializeComponent();

		ViewModel = viewModel;
		DataContext = viewModel;

		AddHandler(KcvNumberBox.InvalidChangedEvent, new RoutedEventHandler(InvalidChanged));
	}

	private void InvalidChanged(object sender, RoutedEventArgs e)
	{
		if (e.OriginalSource is not KcvNumberBox box) return;

		if (box.IsInvalid) Invalid.Add(box);
		else Invalid.Remove(box);

		OkButton.IsEnabled = Invalid.Count == 0;
	}

	private void Save(object sender, RoutedEventArgs e)
	{
		if (Invalid.Count > 0) return;

		ViewModel.Save();
		DialogResult = true;
	}
}
