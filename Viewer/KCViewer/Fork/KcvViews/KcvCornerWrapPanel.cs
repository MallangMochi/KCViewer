using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: a wrap panel with its last child at the top right (the planned upgrades panel's 개수 계획 관리 button, mockup
/// 20261028-upgrade-plan-button-v1, approved "구현"). The other children wrap from the left: on the first line in the
/// room left of it, below it in the full width, so a narrow pane does not squeeze every line beside the button (a
/// dock panel wrapped the day filters one to a line there). A child that does not fit beside it even alone starts
/// the second line.
/// </summary>
public class KcvCornerWrapPanel : Panel
{
	/// <summary>Between the first line's children and the corner, as the fleet panel's 함대 상세 button's margin.</summary>
	private const double Gap = 14;

	protected override Size MeasureOverride(Size availableSize)
	{
		foreach (UIElement child in InternalChildren)
		{
			child.Measure(new(availableSize.Width, double.PositiveInfinity));
		}

		return Layout(availableSize.Width, false);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		Layout(finalSize.Width, true);
		return finalSize;
	}

	/// <summary>The lines from the children's measured sizes; the size they take, and with <paramref name="arrange"/> placed.</summary>
	private Size Layout(double width, bool arrange)
	{
		int count = InternalChildren.Count;
		if (count is 0) return new();

		UIElement corner = InternalChildren[count - 1];
		Size end = corner.DesiredSize;
		if (arrange) corner.Arrange(new(Math.Max(0, width - end.Width), 0, end.Width, end.Height));

		List<(UIElement Child, double X)> line = [];
		bool first = true;
		double x = 0;
		double y = 0;
		double lineHeight = end.Height;
		double widest = end.Width;

		void EndLine()
		{
			widest = Math.Max(widest, first && line.Count > 0 ? x + Gap + end.Width : x);
			foreach ((UIElement child, double left) in line)
			{
				if (arrange) child.Arrange(new(left, y, child.DesiredSize.Width, lineHeight));
			}

			y += lineHeight;
			line.Clear();
			first = false;
			x = 0;
			lineHeight = 0;
		}

		for (int i = 0; i < count - 1; i++)
		{
			UIElement child = InternalChildren[i];
			Size size = child.DesiredSize;
			double limit = first ? width - Gap - end.Width : width;

			if ((x > 0 || first) && x + size.Width > limit) EndLine();

			line.Add((child, x));
			x += size.Width;
			lineHeight = Math.Max(lineHeight, size.Height);
		}

		if (line.Count > 0 || first) EndLine();

		return new(widest, y);
	}
}
