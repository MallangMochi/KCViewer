using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using KCViewer.Window.Tools.AutoRefresh;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: 자동 재로딩 in KanColleViewer's style (see the XAML), opened through
/// <see cref="KcvMenuWindows.TryShowAutoRefresh"/>. The status bar follows the rules and 싱글 맵 모드.
/// </summary>
public partial class KcvAutoRefreshWindow
{
	private static KcvMapNameConverter MapName { get; } = new();

	public KcvAutoRefreshWindow(AutoRefreshViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		ViewModel.PropertyChanged += ViewModelChanged;
		ViewModel.Rules.CollectionChanged += RulesChanged;
		foreach (AutoRefreshRuleViewModel rule in ViewModel.Rules) rule.PropertyChanged += RuleChanged;

		Closed += (_, _) =>
		{
			ViewModel.PropertyChanged -= ViewModelChanged;
			ViewModel.Rules.CollectionChanged -= RulesChanged;
			foreach (AutoRefreshRuleViewModel rule in ViewModel.Rules) rule.PropertyChanged -= RuleChanged;
		};

		ShowStatus();
	}

	private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(AutoRefreshViewModel.IsSingleMapMode) or nameof(AutoRefreshViewModel.SelectedSingleMapModeMap)) ShowStatus();
	}

	// the view model loads the saved rules again when the window opens (Clear, then each rule)
	private void RulesChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.OldItems is not null) foreach (AutoRefreshRuleViewModel rule in e.OldItems) rule.PropertyChanged -= RuleChanged;
		if (e.NewItems is not null) foreach (AutoRefreshRuleViewModel rule in e.NewItems) rule.PropertyChanged += RuleChanged;

		ShowStatus();
	}

	private void RuleChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(AutoRefreshRuleViewModel.IsEnabled)) ShowStatus();
	}

	private void ShowStatus()
	{
		string? singleMap = ViewModel is { IsSingleMapMode: true, SelectedSingleMapModeMap: { } map }
			? MapName.Convert(map, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture) as string
			: null;

		Status.Text = ForkStrings.AutoRefreshStatus(ViewModel.Rules.Count, ViewModel.Rules.Count(r => r.IsEnabled), singleMap);
	}
}
