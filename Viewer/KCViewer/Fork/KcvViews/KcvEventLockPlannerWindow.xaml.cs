using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KCViewer.Avalonia.Controls.ShipFilter;
using KCViewer.Window.Tools.EventLockPlanner;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: 이벤트 딱지 플래너 in KanColleViewer's style (see the XAML), opened through <see cref="KcvCalcWindows"/> while
/// the style is on.
/// </summary>
public partial class KcvEventLockPlannerWindow
{
	/// <summary>What one notch of the wheel moves the body: a scroll viewer's three lines of 16.</summary>
	private const double WheelStep = 48;

	/// <summary>The least heights of 「딱지 없는 함선」 and of the body as the XAML gives them, with the filter folded.</summary>
	private double ShipsLeast { get; }
	private double BodyLeast { get; }

	public KcvEventLockPlannerWindow(EventLockPlannerViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		ShipsLeast = ShipsRow.MinHeight;
		BodyLeast = BodyContent.MinHeight;
		Filter.SizeChanged += (_, _) => KeepTableRoom();
		Filter.IsVisibleChanged += (_, _) => KeepTableRoom();
	}

	/// <summary>
	/// The open filter adds its height to the least heights, so a low window scrolls instead of leaving the table a
	/// row (the least heights of the mockup are with the filter folded).
	/// </summary>
	private void KeepTableRoom()
	{
		double filter = Filter.IsVisible ? Filter.ActualHeight + Filter.Margin.Top + Filter.Margin.Bottom : 0;

		ShipsRow.MinHeight = ShipsLeast + filter;
		BodyContent.MinHeight = BodyLeast + filter;
	}

	/// <summary>
	/// The wheel moves the innermost list under the mouse that can still move that way. A card row (it only scrolls
	/// sideways) or a list at its end would otherwise keep the wheel, and a body lower than the boxes could not be
	/// scrolled over the cards.
	/// </summary>
	private void PassWheel(object sender, MouseWheelEventArgs e)
	{
		ScrollViewer? innermost = null;
		DependencyObject? element = e.OriginalSource as DependencyObject;

		for (; element is not null && element != Body; element = ParentOf(element))
		{
			// the number boxes step with the wheel
			if (element is KcvNumberBox) return;
			if (element is not ScrollViewer scroller) continue;

			innermost ??= scroller;
			if (!CanScroll(scroller, e.Delta)) continue;

			// the list under the mouse moves by itself
			if (scroller == innermost) return;

			e.Handled = true;
			Scroll(scroller, e.Delta);
			return;
		}

		// over the body itself, or in a popup (the colour picker's), whose tree does not reach the body: as it is
		if (element != Body || innermost is null) return;

		e.Handled = true;
		if (CanScroll(Body, e.Delta))
		{
			Scroll(Body, e.Delta);
		}
	}

	private static bool CanScroll(ScrollViewer scroller, int delta) => delta < 0
		? scroller.VerticalOffset < scroller.ScrollableHeight
		: scroller.VerticalOffset > 0;

	private static void Scroll(ScrollViewer scroller, int delta) =>
		scroller.ScrollToVerticalOffset(scroller.VerticalOffset - delta / 120.0 * WheelStep);

	/// <summary>A part of a text (a ship's name and level are <see cref="System.Windows.Documents.Run"/>s) is no visual.</summary>
	private static DependencyObject? ParentOf(DependencyObject element) => element switch
	{
		Visual => VisualTreeHelper.GetParent(element),
		FrameworkContentElement content => content.Parent,
		_ => null,
	};
}

/// <summary>「전체 선택」 of the ship filter: checked when every ship type is.</summary>
public sealed class KcvAllTypesCheckedConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is IEnumerable<ShipTypeGroupFilterViewModel> filters && filters.All(f => f.IsChecked);
}

/// <summary>The lock a ship has in the game, before its name (「[2] 」), as the viewer's planner writes it.</summary>
public sealed class KcvLockPrefixConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is int lockId and > 0 ? $"[{lockId}] " : "";
}

/// <summary>A yes / no column of the planner's table: ✓ or nothing.</summary>
public sealed class KcvCheckMarkConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is true ? "✓" : "";
}
