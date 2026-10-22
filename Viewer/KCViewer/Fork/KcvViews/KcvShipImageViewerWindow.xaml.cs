using System;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the ship album's 이미지 뷰어 in KanColleViewer's style (see the XAML), opened through
/// <see cref="KcvShipAlbumTools.TryShowImageViewer"/>. The keys, menus and zoom work as the viewer's
/// (DialogShipGraphicViewer); a zoom while 맞춤 is on leaves 맞춤 and goes on from the fitted zoom.
/// </summary>
public partial class KcvShipImageViewerWindow
{
	// the viewer's menu names, its 「&」 as WPF's access key
	public static string FileHeader => MenuText(ShipGraphicViewerResources.TopMenu_File);
	public static string OpenHeader => MenuText(ShipGraphicViewerResources.TopMenu_File_Open);
	public static string CopyHeader => MenuText(ShipGraphicViewerResources.TopMenu_File_CopyToClipboard);
	public static string ViewHeader => MenuText(ShipGraphicViewerResources.TopMenu_View);
	public static string InterpolationHeader => MenuText(ShipGraphicViewerResources.TopMenu_View_InterpolationMode);
	public static string SharpHeader => MenuText(ShipGraphicViewerResources.TopMenu_View_InterpolationMode_Sharp);
	public static string SmoothHeader => MenuText(ShipGraphicViewerResources.TopMenu_View_InterpolationMode_Smooth);
	public static string ZoomHeader => MenuText(ShipGraphicViewerResources.TopMenu_View_Zoom);
	public static string ZoomInHeader => MenuText(ShipGraphicViewerResources.TopMenu_View_Zoom_In);
	public static string ZoomOutHeader => MenuText(ShipGraphicViewerResources.TopMenu_View_Zoom_Out);
	public static string Zoom100Header => MenuText(ShipGraphicViewerResources.TopMenu_View_Zoom_100);
	public static string FitHeader => MenuText(ShipGraphicViewerResources.TopMenu_View_Zoom_Fit);

	private static string MenuText(string text) => text.Replace("_", "__").Replace("&", "_");

	public KcvShipImageViewerWindow(KcvShipImageViewerViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		ViewModel.PropertyChanged += ViewModelChanged;
		Stage.ViewChanged += (_, _) => ShowZoom();
		Stage.MouseWheel += (_, e) => ZoomBy(e.Delta / 120.0 * 0.1);
		PreviewKeyDown += KeyPressed;
		DragOver += (_, e) =>
		{
			e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
			e.Handled = true;
		};
		Drop += (_, e) =>
		{
			if (e.Data.GetData(DataFormats.FileDrop) is string[] files) Open(files);
		};
		Closed += (_, _) => ViewModel.Stop();

		ShowZoom();
	}

	/// <summary>The pictures of the ship and its costume ships; the viewer's 「이미지를 찾을 수 없습니다.」 when there are none.</summary>
	public void OpenShip(int shipId)
	{
		if (!ViewModel.OpenShip(shipId)) NotFound();
	}

	private void Open(string[] files)
	{
		try
		{
			if (!ViewModel.OpenFiles(files)) NotFound();
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, string.Format(ShipGraphicViewerResources.ImageCanNotBeOpened, string.Join("\r\n", files), ex.GetType().Name, ex.Message));
		}
	}

	private void NotFound() =>
		MessageBox.Show(this, ShipGraphicViewerResources.ImageNotFound, "", MessageBoxButton.OK, MessageBoxImage.Exclamation);

	private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not nameof(KcvShipImageViewerViewModel.Image)) return;

		Stage.Ship = ViewModel.Selected?.Ship;
		Stage.Image = ViewModel.Image;
	}

	private void ScrollToSelected(object sender, SelectionChangedEventArgs e)
	{
		if (List.SelectedItem is { } item) List.ScrollIntoView(item);
	}

	private void ShowZoom() => ZoomText.Text = $"{Math.Round(Stage.ShownZoom * 100):0}%";

	private void KeyPressed(object sender, KeyEventArgs e)
	{
		// the menus keep their own keys
		if (TopMenu.IsKeyboardFocusWithin || Keyboard.FocusedElement is MenuItem) return;

		Key key = e.Key is Key.System ? e.SystemKey : e.Key;

		if (Keyboard.Modifiers is ModifierKeys.Control)
		{
			switch (key)
			{
				case Key.O: OpenFiles(this, e); break;
				case Key.C: CopyToClipboard(this, e); break;
				case Key.OemPlus or Key.Add: ZoomBy(0.1); break;
				case Key.OemMinus or Key.Subtract: ZoomBy(-0.1); break;
				case Key.D0 or Key.NumPad0: Zoom100(this, e); break;
				default: return;
			}

			e.Handled = true;
			return;
		}

		if (ViewModel.Items.Count == 0) return;

		switch (key)
		{
			case Key.Left: ViewModel.Move(-1); break;
			case Key.Right: ViewModel.Move(1); break;
			case Key.Up: ZoomBy(0.1); break;
			case Key.Down: ZoomBy(-0.1); break;
			case Key.Oem3: Stage.ToggleAdvMode(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)); break;      // 「@」
			default: return;
		}

		e.Handled = true;
	}

	private void ZoomBy(double step)
	{
		double from = Stage.ShownZoom;
		SetFit(false);
		Stage.Zoom = from + step;
	}

	private void SetFit(bool fit)
	{
		Stage.Fit = fit;
		FitMenu.IsChecked = fit;
		FitButton.IsChecked = fit;
	}

	private void OpenFiles(object sender, RoutedEventArgs e)
	{
		OpenFileDialog dialog = new()
		{
			Title = ForkStrings.ImageViewerOpenTitle,
			Filter = "Image|*.jpg;*.png|JPEG|*.jpg|PNG|*.png|File|*",
			Multiselect = true,
			// Fork: the local server's assets folder instead of the cache (Fork/ForkAssets.cs)
			InitialDirectory = ForkAssets.ShipImageFolder,
		};

		if (dialog.ShowDialog(this) is true) Open(dialog.FileNames);
	}

	private void CopyToClipboard(object sender, RoutedEventArgs e)
	{
		if (ViewModel.Image is null)
		{
			SystemSounds.Exclamation.Play();
			return;
		}

		try
		{
			Clipboard.SetImage(ViewModel.Image);
		}
		catch (Exception)
		{
			SystemSounds.Exclamation.Play();
		}
	}

	private void DrawSharp(object sender, RoutedEventArgs e) => SetSmooth(false);

	private void DrawSmooth(object sender, RoutedEventArgs e) => SetSmooth(true);

	private void SetSmooth(bool smooth)
	{
		Stage.Smooth = smooth;
		SharpMenu.IsChecked = !smooth;
		SmoothMenu.IsChecked = smooth;
	}

	private void ZoomIn(object sender, RoutedEventArgs e) => ZoomBy(0.1);

	private void ZoomOut(object sender, RoutedEventArgs e) => ZoomBy(-0.1);

	private void Zoom100(object sender, RoutedEventArgs e)
	{
		SetFit(false);
		Stage.Zoom = 1;
	}

	private void ToggleFit(object sender, RoutedEventArgs e) => SetFit(!Stage.Fit);

	private void FitButtonChanged(object sender, RoutedEventArgs e) => SetFit(FitButton.IsChecked is true);

	private void ToggleChecker(object sender, RoutedEventArgs e)
	{
		Stage.Checker = !Stage.Checker;
		CheckerMenu.IsChecked = Stage.Checker;
	}
}
