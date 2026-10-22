using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Window.Wpf;

namespace KCViewer.ViewModels;

public class StripStatusViewModel : ObservableObject
{
	public string Information { get; set; } = "";
	public string Clock { get; set; } = "";
	public string? ClockToolTip { get; set; }
	public bool Visible { get; set; }

	public Visibility Visibility => Visible.ToVisibility();

}
