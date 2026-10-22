using System.Windows;
using KCViewer.Data;

namespace KCViewer.Window.Dialog;

/// <summary>
/// Interaction logic for DialogKancolleProgressWpf.xaml
/// </summary>
public partial class DialogKancolleProgressWpf : System.Windows.Window
{
	public DialogKancolleProgressWpf()
	{
		InitializeComponent();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		KancolleProgress.AllShips = KCDatabase.Instance.MasterShips.Values;
		KancolleProgress.UserShips = KCDatabase.Instance.Ships.Values;
		KancolleProgress.UserEquipment = KCDatabase.Instance.Equipments.Values;
	}
}
