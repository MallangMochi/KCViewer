using System.Windows;
using System.Windows.Controls;
using KCViewer.ViewModels;
using KCViewer.Window.Tools.FleetImageGenerator;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: 편성 이미지 출력 in KanColleViewer's style (mockup 20261007-fleet-image-v2, approved 2026-10-07 "구현"). It is
/// the viewer's window, so its image templates and its preview stay as they are (the image is saved and copied as
/// before); only what is above the preview is replaced by <see cref="KcvFleetImageHeader"/>: the tabs 카드·컷인·배너 with
/// 복사·불러오기·저장·설정, the box 「출력 조건」 and, with 「설정을 창에 펼치기」, the settings. The preview scrolls both ways
/// when the window is smaller than the image ("창 크기에 따라 가로 세로 스크롤을 붙이도록 함"); the scroll bars, which
/// ModernWpf draws over the content, get room of their own, so they no longer cover the right of 컷인 as the viewer's did
/// ("잘리지 않도록 조치 할 것"). The first size is 1000×800, then the last one is kept (its own WindowStates entry).
/// </summary>
public sealed class KcvFleetImageGeneratorWindow : FleetImageGeneratorWindow
{
	public KcvFleetImageGeneratorWindow(FleetImageGeneratorImageDataModel model) : base(model)
	{
		Width = 1000;
		Height = 800;
		// also the image's background while 커스텀 테마 is off (the viewer's dark window has the same colour)
		SetResourceReference(BackgroundProperty, "Kcv.Background");

		// The viewer's layout has the button row, the quick settings and the 전차 수송량 row above the preview
		ScrollViewer preview = (ScrollViewer)ImageContent.Parent;
		((Panel)preview.Parent).Children.Remove(preview);

		preview.HorizontalAlignment = HorizontalAlignment.Stretch;
		preview.VerticalAlignment = VerticalAlignment.Stretch;
		preview.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
		preview.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		preview.Padding = new Thickness(10, 10, 18, 18);

		// the image keeps its own size: stretched to the preview, it would be saved with the empty space
		ImageContent.HorizontalAlignment = HorizontalAlignment.Left;
		ImageContent.VerticalAlignment = VerticalAlignment.Top;

		Border frame = new()
		{
			Margin = new Thickness(8, 0, 8, 8),
			BorderThickness = new Thickness(1),
			Child = preview,
		};
		frame.SetResourceReference(Border.BorderBrushProperty, "Kcv.HeaderBackground");

		KcvFleetImageHeader header = new();
		DockPanel.SetDock(header, Dock.Top);

		Content = new DockPanel { Children = { header, frame } };
	}

	public void CopyToClipboard() => ForkCopyImage();

	public void LoadFromClipboard() => ForkLoadImage();

	public void SaveToFile() => ForkSaveImage();

	public void OpenSettings() => new KcvFleetImageSettingsWindow(ViewModel).Show(this);
}
