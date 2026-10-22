namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: KanColleViewer's 所有装備一覧 (see the XAML). Opened instead of the viewer's list while the style is on.</summary>
public partial class KcvEquipmentListWindow
{
	public KcvEquipmentListWindow() : base(new KcvEquipmentListViewModel())
	{
		InitializeComponent();
	}
}
