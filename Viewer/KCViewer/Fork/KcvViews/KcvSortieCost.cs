using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Common;
using KCViewer.Window.Tools.SortieRecordViewer;
using KCViewer.Window.Tools.SortieRecordViewer.SortieCostViewer;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: 출격 코스트 in KanColleViewer's style (mockup 20261017-sortie-cost-v3, approved 2026-10-17): the viewer's
/// calculation (<see cref="SortieCostViewerViewModel"/>, started when the window opens and cancelled when it closes) with
/// a row per sortie that also has the sortie's route and fleet, and the status text (calculating, then the total).
/// </summary>
public sealed class KcvSortieCostViewModel : WindowViewModelBase
{
	public SortieCostViewerViewModel Cost { get; }
	public IReadOnlyList<SortieRecordViewModel> Sorties { get; }
	public ObservableCollection<KcvSortieCostRow> Rows { get; } = [];
	public string StatusText { get; private set; } = "";

	public KcvSortieCostViewModel(SortieCostViewerViewModel cost, IReadOnlyList<SortieRecordViewModel> sorties)
	{
		Cost = cost;
		Sorties = sorties;

		Cost.SortieCosts.CollectionChanged += AddRows;
		Cost.PropertyChanged += CostChanged;
		Cost.Configuration.PropertyChanged += Recount;
		ShowStatus();
	}

	public override void Loaded()
	{
		base.Loaded();
		Cost.Loaded();
	}

	public override void Closed()
	{
		base.Closed();
		Cost.Closed();

		// the bucket choice is the sortie record window's and outlives this one
		Cost.Configuration.PropertyChanged -= Recount;
	}

	private void AddRows(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.NewItems is null) return;

		foreach (SortieCostViewModel cost in e.NewItems.OfType<SortieCostViewModel>())
		{
			// the viewer adds the costs in the order of the sorties
			int index = Cost.SortieCosts.IndexOf(cost);
			SortieRecordViewModel? sortie = index >= 0 && index < Sorties.Count ? Sorties[index] : null;
			Rows.Add(new(cost, sortie, Cost));
		}

		ShowStatus();
	}

	private void CostChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(SortieCostViewerViewModel.Progress)) ShowStatus();
	}

	private void Recount(object? sender, PropertyChangedEventArgs e)
	{
		foreach (KcvSortieCostRow row in Rows) row.Recount();
	}

	private void ShowStatus() => StatusText = Cost.Progress is null && Rows.Count == Sorties.Count
		? ForkStrings.SortieCostTotal(Rows.Count)
		: ForkStrings.SortieCostCalculating(Rows.Count, Sorties.Count);
}

/// <summary>
/// A sortie's cost: the viewer's numbers, the sortie for its route and fleet, and the lines of its detail that have a
/// value (gains as negative numbers), the repair line with the ships counted for buckets.
/// </summary>
public sealed class KcvSortieCostRow : ObservableObject
{
	public SortieCostViewModel Cost { get; }
	public SortieRecordViewModel? Sortie { get; }

	public int SortieId => Sortie?.Id ?? 0;
	public DateTime Time => Cost.Time;
	public string MapText => $"{Cost.World}-{Cost.Map}";
	public int MapKey => Cost.World * 10 + Cost.Map;
	public int Fuel => Cost.TotalCost.Fuel;
	public int Ammo => Cost.TotalCost.Ammo;
	public int Steel => Cost.TotalCost.Steel;
	public int Bauxite => Cost.TotalCost.Bauxite;
	public int Buckets => Cost.Buckets;

	public IReadOnlyList<KcvSortieCostLine> Lines { get; }
	private IReadOnlyList<KcvSortieCostDamage> Damage { get; }

	public KcvSortieCostRow(SortieCostViewModel cost, SortieRecordViewModel? sortie, SortieCostViewerViewModel viewer)
	{
		Cost = cost;
		Sortie = sortie;

		SortieCostViewerTranslationViewModel words = viewer.Translation;
		SortieCostConfigurationViewModel choice = viewer.Configuration;

		Damage = new (string Name, int Count, Func<bool> Counted)[]
			{
				(ForkStrings.SortieCostScratch, cost.NormalDamage, () => choice.IsNormalDamageBucket),
				(words.LightDamage, cost.LightDamage, () => choice.IsShouhaBucket),
				(words.MediumDamage, cost.MediumDamage, () => choice.IsChuuhaBucket),
				(words.HeavyDamage, cost.HeavyDamage, () => choice.IsTaihaBucket),
			}
			.Where(d => d.Count > 0)
			.Select((d, i) => new KcvSortieCostDamage($"{d.Name} {d.Count}", i == 0, d.Counted))
			.ToList();

		Lines = new[]
			{
				new KcvSortieCostLine(words.SortieSupply, cost.SortieFleetSupplyCost),
				new KcvSortieCostLine(words.SortieRepair, cost.SortieFleetRepairCost) { Damage = Damage },
				new KcvSortieCostLine(ForkStrings.SortieCostJet, cost.SortieFleetJetCost),
				new KcvSortieCostLine(words.NodeSupport, cost.NodeSupportSupplyCost),
				new KcvSortieCostLine(words.BossSupport, cost.BossSupportSupplyCost),
				new KcvSortieCostLine(words.AirBaseSortie, cost.TotalAirBaseSortieCost),
				new KcvSortieCostLine(words.AirBaseSupply, cost.TotalAirBaseSupplyCost),
				new KcvSortieCostLine(ForkStrings.SortieCostAirBaseJet, cost.AirBaseJetCost),
				new KcvSortieCostLine(ForkStrings.SortieCostResourceNodes, SortieCostModel.Zero - cost.ResourceGain),
				new KcvSortieCostLine(ForkStrings.SortieCostSinking, SortieCostModel.Zero - cost.SinkingResourceGain),
			}
			.Where(l => l.HasValue)
			.ToList();

		cost.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName is nameof(SortieCostViewModel.Buckets)) OnPropertyChanged(nameof(Buckets));
		};
	}

	/// <summary>The bucket choice changed: which damage is counted (the viewer counts the buckets again itself).</summary>
	public void Recount()
	{
		foreach (KcvSortieCostDamage damage in Damage) damage.Recount();
	}
}

/// <summary>A line of a sortie's detail: a cost, or a gain as negative numbers.</summary>
public sealed class KcvSortieCostLine(string name, SortieCostModel cost)
{
	public string Name { get; } = name;
	public int Fuel { get; } = cost.Fuel;
	public int Ammo { get; } = cost.Ammo;
	public int Steel { get; } = cost.Steel;
	public int Bauxite { get; } = cost.Bauxite;

	/// <summary>The ships newly damaged in the sortie (the repair line).</summary>
	public IReadOnlyList<KcvSortieCostDamage> Damage { get; init; } = [];

	public bool HasValue => Fuel != 0 || Ammo != 0 || Steel != 0 || Bauxite != 0 || Damage.Count > 0;
}

/// <summary>「소파 2」 after the repair line, grey when the bucket choice does not count that damage.</summary>
public sealed class KcvSortieCostDamage : ObservableObject
{
	private Func<bool> Counted { get; }

	public string Text { get; }
	public bool IsFirst { get; }
	public bool IsCounted { get; private set; }

	public KcvSortieCostDamage(string text, bool isFirst, Func<bool> counted)
	{
		Text = text;
		IsFirst = isFirst;
		Counted = counted;
		IsCounted = counted();
	}

	public void Recount() => IsCounted = Counted();
}

/// <summary>A sortie cost's number: 「−15」 for a gain, and its kind (Zero, Gain, Cost) for the colour.</summary>
public sealed class KcvCostAmountConverter : KcvOneWayConverter
{
	public static KcvCostAmountConverter Text { get; } = new() { Kind = false };
	public static KcvCostAmountConverter KindOf { get; } = new() { Kind = true };

	private bool Kind { get; init; }

	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not int amount) return Kind ? "" : value?.ToString() ?? "";

		if (Kind) return amount switch
		{
			0 => "Zero",
			< 0 => "Gain",
			_ => "Cost",
		};

		return amount < 0 ? "−" + (-amount).ToString(CultureInfo.InvariantCulture) : amount.ToString(CultureInfo.InvariantCulture);
	}
}

/// <summary>
/// A grid whose first <c>Count</c> columns are as wide as the data grid's first columns, the rest of it after them: a
/// row's detail lined up with the row's cells (the columns are not reordered in that table).
/// </summary>
public static class KcvFollowColumns
{
	public static readonly DependencyProperty CountProperty = DependencyProperty.RegisterAttached(
		"Count", typeof(int), typeof(KcvFollowColumns), new PropertyMetadata(0, OnCountChanged));

	public static int GetCount(Grid grid) => (int)grid.GetValue(CountProperty);
	public static void SetCount(Grid grid, int value) => grid.SetValue(CountProperty, value);

	private static PixelLength Pixels { get; } = new();

	private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not Grid grid) return;

		grid.Loaded -= Follow;
		grid.Loaded += Follow;
	}

	private static void Follow(object sender, RoutedEventArgs e)
	{
		Grid grid = (Grid)sender;
		DataGrid? table = Ancestor<DataGrid>(grid);
		if (table is null) return;

		grid.ColumnDefinitions.Clear();
		foreach (DataGridColumn column in table.Columns.Take(GetCount(grid)))
		{
			ColumnDefinition definition = new();
			BindingOperations.SetBinding(definition, ColumnDefinition.WidthProperty,
				new Binding(nameof(DataGridColumn.ActualWidth)) { Source = column, Converter = Pixels });
			grid.ColumnDefinitions.Add(definition);
		}
		grid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
	}

	private static T? Ancestor<T>(DependencyObject element) where T : DependencyObject
	{
		for (DependencyObject? parent = VisualTreeHelper.GetParent(element); parent is not null; parent = VisualTreeHelper.GetParent(parent))
		{
			if (parent is T found) return found;
		}

		return null;
	}

	private sealed class PixelLength : KcvOneWayConverter
	{
		public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
			value is double width ? new GridLength(width) : new GridLength(0);
	}
}

/// <summary>Where the sortie record opens 출격 코스트; true when the window in KanColleViewer's style opened instead.</summary>
public static class KcvSortieCostWindows
{
	public static bool TryShow(SortieCostViewerViewModel cost, IReadOnlyList<SortieRecordViewModel> sorties)
	{
		if (!KcvViewState.Current.Enabled) return false;

		new KcvSortieCostWindow(new KcvSortieCostViewModel(cost, sorties)).Show();
		return true;
	}
}
