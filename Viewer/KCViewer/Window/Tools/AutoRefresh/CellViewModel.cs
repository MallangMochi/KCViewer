using CommunityToolkit.Mvvm.ComponentModel;

namespace KCViewer.Window.Tools.AutoRefresh;

public class CellViewModel : ObservableObject
{
	public int Id { get; }

	public CellViewModel(int id)
	{
		Id = id;
	}
}