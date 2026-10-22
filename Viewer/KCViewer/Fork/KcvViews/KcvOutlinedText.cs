using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// A short bold text with a 1 px outline, drawn centred on the element whatever its height and taking no room in the
/// layout, so it can sit over a 6 px gauge without making it taller (the battle panel's HP lost in this battle, mockup
/// 20261013-battle-hp-damage-v5). Drawn as the mockup's text-shadow: the text in the outline colour moved 1 px each
/// way (8 times), then the text over them; on whole pixels (display formatting), as the small digits blur otherwise.
/// </summary>
public sealed class KcvOutlinedText : FrameworkElement
{
	public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
		nameof(Text), typeof(string), typeof(KcvOutlinedText),
		new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
		nameof(Fill), typeof(Brush), typeof(KcvOutlinedText),
		new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty OutlineProperty = DependencyProperty.Register(
		nameof(Outline), typeof(Brush), typeof(KcvOutlinedText),
		new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
		nameof(FontSize), typeof(double), typeof(KcvOutlinedText),
		new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
		typeof(KcvOutlinedText),
		new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily,
			FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

	public string? Text
	{
		get => (string?)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public Brush? Fill
	{
		get => (Brush?)GetValue(FillProperty);
		set => SetValue(FillProperty, value);
	}

	public Brush? Outline
	{
		get => (Brush?)GetValue(OutlineProperty);
		set => SetValue(OutlineProperty, value);
	}

	public double FontSize
	{
		get => (double)GetValue(FontSizeProperty);
		set => SetValue(FontSizeProperty, value);
	}

	public FontFamily FontFamily
	{
		get => (FontFamily)GetValue(FontFamilyProperty);
		set => SetValue(FontFamilyProperty, value);
	}

	// no room of its own: the parent's size is the box the text is centred in
	protected override Size MeasureOverride(Size availableSize) => new(0, 0);

	protected override void OnRender(DrawingContext drawingContext)
	{
		if (string.IsNullOrEmpty(Text)) return;

		DpiScale dpi = VisualTreeHelper.GetDpi(this);
		Typeface typeface = new(FontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
		FormattedText Format(Brush brush) => new(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
			typeface, FontSize, brush, null, TextFormattingMode.Display, dpi.PixelsPerDip);

		FormattedText text = Format(Fill ?? Brushes.White);

		// centre the ink (the digits), not the line box, and keep the glyphs on whole device pixels
		Rect ink = text.BuildGeometry(new Point(0, 0)).Bounds;
		if (ink.IsEmpty) return;

		double x = Snap(RenderSize.Width / 2 - (ink.Left + ink.Width / 2), dpi.DpiScaleX);
		double y = Snap(RenderSize.Height / 2 - (ink.Top + ink.Height / 2), dpi.DpiScaleY);

		if (Outline is not null)
		{
			FormattedText outline = Format(Outline);
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dy = -1; dy <= 1; dy++)
				{
					if (dx == 0 && dy == 0) continue;
					drawingContext.DrawText(outline, new Point(x + dx, y + dy));
				}
			}
		}

		drawingContext.DrawText(text, new Point(x, y));
	}

	private static double Snap(double value, double scale) => Math.Round(value * scale) / scale;
}
