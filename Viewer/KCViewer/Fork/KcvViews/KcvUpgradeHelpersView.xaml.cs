using System.Windows;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the helper groups of a planned upgrade in KanColleViewer's style (see the XAML).</summary>
public partial class KcvUpgradeHelpersView
{
	public static readonly DependencyProperty TodayOnlyProperty = DependencyProperty.Register(
		nameof(TodayOnly), typeof(bool), typeof(KcvUpgradeHelpersView), new PropertyMetadata(false));

	/// <summary>Only the groups that can help today.</summary>
	public bool TodayOnly
	{
		get => (bool)GetValue(TodayOnlyProperty);
		set => SetValue(TodayOnlyProperty, value);
	}

	public KcvUpgradeHelpersView()
	{
		InitializeComponent();
	}
}
