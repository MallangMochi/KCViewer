using System.Windows;
using System.Windows.Controls;
using KCViewer.Window.Wpf.Quest;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: KanColleViewer's 任務 design for the quest panel (see the XAML).</summary>
public partial class KcvQuestView : UserControl
{
	private bool FiltersInline { get; set; } = true;

	public KcvQuestView()
	{
		InitializeComponent();

		// The title changes with the count and the button with the filter: their room changes too
		HeaderTitle.SizeChanged += PlaceFilters;
		ToggleButton.SizeChanged += PlaceFilters;
	}

	// The header button turns the viewer's "in progress only" filter (the context menu item) on and off
	private void ShowRunningOnly_Click(object sender, RoutedEventArgs e)
	{
		if (DataContext is QuestViewModel quests)
		{
			quests.MenuMain_ShowRunningOnly = !quests.MenuMain_ShowRunningOnly;
		}
	}

	/// <summary>
	/// The filters on the header line between the title and the button while they fit there on one line (the panel
	/// about 560 wide), else on a line of their own under it, where they wrap (mockup 20261005-quest-filter-v2, ④).
	/// </summary>
	private void PlaceFilters(object sender, SizeChangedEventArgs e)
	{
		Filters.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		double needed = HeaderTitle.ActualWidth + InlineFilters.Margin.Left + Filters.DesiredSize.Width + 10 + ToggleButton.ActualWidth;
		bool inline = Header.ActualWidth >= needed;
		if (inline == FiltersInline) return;

		FiltersInline = inline;
		(inline ? FiltersBelow : InlineFilters).Child = null;
		(inline ? InlineFilters : FiltersBelow).Child = Filters;
		FiltersBelow.Visibility = inline ? Visibility.Collapsed : Visibility.Visible;
	}
}
