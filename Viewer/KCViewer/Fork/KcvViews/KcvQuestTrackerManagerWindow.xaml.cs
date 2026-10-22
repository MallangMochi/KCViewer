using KCViewer.Window.Dialog.QuestTrackerManager;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: 퀘스트 추적 매니저 in KanColleViewer's style (see the XAML), opened through <see cref="KcvCalcWindows"/> while the style is on.</summary>
public partial class KcvQuestTrackerManagerWindow
{
	public KcvQuestTrackerManagerWindow(QuestTrackerManagerViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}
