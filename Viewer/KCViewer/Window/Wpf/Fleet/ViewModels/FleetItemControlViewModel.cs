using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Resource;
using KCViewer.ViewModels;
using KCViewer.Window.Tools.DialogAlbumMasterShip;

namespace KCViewer.Window.Wpf.Fleet.ViewModels;

public partial class FleetItemControlViewModel : ObservableObject
{
	public int MaxWidth { get; set; }
	public string? Text { get; set; }
	public int Tag { get; set; }
	public string? ToolTip { get; set; }
	public Color ForeColor { get; set; }
	public Color BackColor { get; set; }
	public bool Visible { get; set; }

	public SolidColorBrush Foreground => new(ForeColor);
	public SolidColorBrush Background => new(BackColor);
	public IconContent? Icon { get; set; }

	public FleetItemControlViewModel()
	{
		Utility.Configuration.Instance.ConfigurationChanged += ConfigurationChanged;

		ConfigurationChanged();
	}

	[RelayCommand]
	private void OpenShipEncyclopedia()
	{
		// Fork: the album in the KanColleViewer style while it is on
		Fork.KcvViews.KcvAlbums.ShowShip(Tag);
	}

	private void ConfigurationChanged()
	{
		ForeColor = Utility.Configuration.Config.UI.ForeColor.ToWpfColor();
		BackColor = Colors.Transparent;
	}
}
