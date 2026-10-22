using KCViewer.Data;

namespace KCViewer.Window.Dialog.QuestTrackerManager;

/// <summary>
/// Interaction logic for QuestTrackerManagerWindow.xaml
/// </summary>
public partial class QuestTrackerManagerWindow
{
	public QuestTrackerManagerWindow() : base(KCDatabase.Instance.QuestTrackerManagers)
	{
		InitializeComponent();
	}
}
