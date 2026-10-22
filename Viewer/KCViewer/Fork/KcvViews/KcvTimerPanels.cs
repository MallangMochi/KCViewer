using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: how the dock, arsenal or expedition panel lays out its docks (mockup 20261006-timer-panels-v2, approved
/// 2026-10-06 "구현"): one under another as a table (default), or side by side as cells that wrap, chosen from the
/// panel's context menu as the fleet overview's 가로 배치 / 세로 배치 and kept in Settings\Fork.json per panel.
/// </summary>
public sealed class KcvPanelLayout : ObservableObject
{
	public static KcvPanelLayout Dock { get; } = new(s => s.DockHorizontal, (s, value) => s.DockHorizontal = value);
	public static KcvPanelLayout Arsenal { get; } = new(s => s.ArsenalHorizontal, (s, value) => s.ArsenalHorizontal = value);
	public static KcvPanelLayout Expedition { get; } = new(s => s.ExpeditionHorizontal, (s, value) => s.ExpeditionHorizontal = value);

	private Action<ForkSettings.KcvStyleSettings, bool> Store { get; }

	public bool Horizontal { get; private set; }

	// The two checkable items of the context menu (as the fleet overview's): checking one picks it
	public bool LayoutHorizontal { get => Horizontal; set => SetHorizontal(true, value); }
	public bool LayoutVertical { get => !Horizontal; set => SetHorizontal(false, value); }

	private KcvPanelLayout(Func<ForkSettings.KcvStyleSettings, bool> load, Action<ForkSettings.KcvStyleSettings, bool> store)
	{
		Store = store;
		Horizontal = load(ForkSettings.Current.KcvStyle);
	}

	private void SetHorizontal(bool horizontal, bool value)
	{
		if (value && Horizontal != horizontal)
		{
			Horizontal = horizontal;
			Store(ForkSettings.Current.KcvStyle, horizontal);
			ForkSettings.Current.Save();
		}

		// A click on the checked item unchecks it in the menu: check it again
		OnPropertyChanged(nameof(LayoutHorizontal));
		OnPropertyChanged(nameof(LayoutVertical));
	}
}

/// <summary>
/// The side by side layout's cells (mockup 20261006-timer-panels-v4, approved 2026-10-06 "구현"): as many columns as fit
/// at <see cref="MinCellWidth"/>, the rows balanced (4 docks: 4, 2 × 2, 1 × 4; 3 fleets: 3, 2 + 1, 1 × 3), the cells as
/// wide as the panel allows ("칸 폭은 고정하지 말고 패널 크기에 유동적으로"), never narrower than the minimum ("칸 당 최소
/// 폭은 유지"): in a pane narrower than one cell they keep it and the pane scrolls sideways. In a scroll viewer that
/// scrolls sideways the panel gets no width, so it takes <see cref="AvailableWidth"/> (the viewport) less <see cref="Offset"/>.
/// </summary>
public sealed class KcvCellGrid : Panel
{
	public static readonly DependencyProperty MinCellWidthProperty = DependencyProperty.Register(
		nameof(MinCellWidth), typeof(double), typeof(KcvCellGrid), new FrameworkPropertyMetadata(160.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
		nameof(Gap), typeof(double), typeof(KcvCellGrid), new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	public static readonly DependencyProperty AvailableWidthProperty = DependencyProperty.Register(
		nameof(AvailableWidth), typeof(double), typeof(KcvCellGrid), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure));

	public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
		nameof(Offset), typeof(double), typeof(KcvCellGrid), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	public double MinCellWidth
	{
		get => (double)GetValue(MinCellWidthProperty);
		set => SetValue(MinCellWidthProperty, value);
	}

	public double Gap
	{
		get => (double)GetValue(GapProperty);
		set => SetValue(GapProperty, value);
	}

	/// <summary>The scroll viewer's viewport width, for when the panel is given no width.</summary>
	public double AvailableWidth
	{
		get => (double)GetValue(AvailableWidthProperty);
		set => SetValue(AvailableWidthProperty, value);
	}

	/// <summary>What lies between the viewport's left and right edges and the panel (margins, the vertical label).</summary>
	public double Offset
	{
		get => (double)GetValue(OffsetProperty);
		set => SetValue(OffsetProperty, value);
	}

	private int Columns(double width)
	{
		int count = InternalChildren.Count;
		int fit = Math.Clamp((int)Math.Floor((width + Gap) / (MinCellWidth + Gap)), 1, Math.Max(1, count));
		int rows = (int)Math.Ceiling(count / (double)fit);

		return Math.Max(1, (int)Math.Ceiling(count / (double)Math.Max(1, rows)));
	}

	private (int Columns, double CellWidth) Layout(double width)
	{
		if (double.IsInfinity(width) || double.IsNaN(width))
		{
			width = double.IsNaN(AvailableWidth) ? MinCellWidth : AvailableWidth - Offset;
		}

		int columns = Columns(width);
		double cell = Math.Floor((width - Gap * (columns - 1)) / columns);

		return (columns, Math.Max(MinCellWidth, cell));
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		int count = InternalChildren.Count;
		if (count is 0) return default;

		(int columns, double cell) = Layout(availableSize.Width);
		double rowHeight = 0;

		foreach (UIElement child in InternalChildren)
		{
			child.Measure(new Size(cell, double.PositiveInfinity));
			rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
		}

		int rows = (int)Math.Ceiling(count / (double)columns);

		return new Size(cell * columns + Gap * (columns - 1), rowHeight * rows + Gap * (rows - 1));
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		int count = InternalChildren.Count;
		if (count is 0) return finalSize;

		(int columns, double cell) = Layout(finalSize.Width);
		double rowHeight = 0;

		foreach (UIElement child in InternalChildren)
		{
			rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
		}

		for (int i = 0; i < count; i++)
		{
			int column = i % columns;
			int row = i / columns;
			InternalChildren[i].Arrange(new Rect(column * (cell + Gap), row * (rowHeight + Gap), cell, rowHeight));
		}

		return finalSize;
	}
}

/// <summary>
/// A dock's time left: 「완료!」 once it reaches 00:00:00 (the viewer keeps 00:00:00 until the next answer), and for the
/// arsenal's finished construction, which the viewer writes 「완성!」 (GeneralRes.Complete) (mockup 20261006-timer-panels-v2).
/// </summary>
public sealed class KcvTimeLeftConverter : KcvOneWayConverter
{
	private const string Over = "00:00:00";

	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		Over => ForkStrings.KcvComplete,
		string text when text == GeneralRes.Complete + "!" => ForkStrings.KcvComplete,
		_ => value,
	};
}
