using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: a line of search conditions with the buttons at its end (전과 뷰어, 자원 차트). The buttons stay on the right
/// of the conditions while the conditions fit beside them (wrapping as they do), otherwise they go to the next line
/// on the right, so a narrow window does not cover the conditions (mockup v6, 2026-10-05). The first child is the
/// conditions, the second the buttons.
/// </summary>
public class KcvTrailingPanel : Panel
{
	private const double Gap = 3;

	private bool Stacked { get; set; }

	protected override Size MeasureOverride(Size availableSize)
	{
		if (InternalChildren.Count < 2)
		{
			foreach (UIElement child in InternalChildren)
			{
				child.Measure(availableSize);
			}

			return InternalChildren.Count is 0 ? new() : InternalChildren[0].DesiredSize;
		}

		UIElement conditions = InternalChildren[0];
		UIElement buttons = InternalChildren[1];

		buttons.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
		Size end = buttons.DesiredSize;

		// a measured size never goes over the space given, so whether the conditions fit beside the buttons comes from
		// the widest piece they cannot wrap
		double beside = Math.Max(0, availableSize.Width - end.Width);
		Stacked = !double.IsInfinity(availableSize.Width) && NarrowestWidth(conditions) > beside;

		if (!Stacked)
		{
			conditions.Measure(new(beside, availableSize.Height));
			return new(conditions.DesiredSize.Width + end.Width, Math.Max(conditions.DesiredSize.Height, end.Height));
		}

		conditions.Measure(availableSize);
		return new(Math.Max(conditions.DesiredSize.Width, end.Width), conditions.DesiredSize.Height + Gap + end.Height);
	}

	/// <summary>
	/// The narrowest an element can wrap to: for a wrap panel its widest child, for a user control or decorator what
	/// it holds, for anything else its own width on one line (with the margins).
	/// </summary>
	private static double NarrowestWidth(UIElement element)
	{
		Thickness margin = element is FrameworkElement framework ? framework.Margin : new();

		switch (element)
		{
			case WrapPanel wrap:
				return margin.Left + margin.Right + wrap.Children.OfType<UIElement>()
					.Where(child => child.Visibility is not Visibility.Collapsed)
					.Select(NarrowestWidth)
					.DefaultIfEmpty(0)
					.Max();

			case ContentControl { Content: UIElement content }:
				return margin.Left + margin.Right + NarrowestWidth(content);

			case Decorator { Child: { } child }:
				return margin.Left + margin.Right + NarrowestWidth(child);

			default:
				element.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
				return element.DesiredSize.Width;
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		if (InternalChildren.Count < 2)
		{
			foreach (UIElement child in InternalChildren)
			{
				child.Arrange(new(finalSize));
			}

			return finalSize;
		}

		UIElement conditions = InternalChildren[0];
		UIElement buttons = InternalChildren[1];
		Size end = buttons.DesiredSize;
		double right = Math.Max(0, finalSize.Width - end.Width);

		if (Stacked)
		{
			double height = conditions.DesiredSize.Height;
			conditions.Arrange(new(0, 0, finalSize.Width, height));
			buttons.Arrange(new(right, height + Gap, end.Width, end.Height));
		}
		else
		{
			// beside the conditions' last line, as the dock panel put them before (bottom aligned)
			conditions.Arrange(new(0, 0, right, finalSize.Height));
			buttons.Arrange(new(right, finalSize.Height - end.Height, end.Width, end.Height));
		}

		return finalSize;
	}
}
