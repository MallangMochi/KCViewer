using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using AvalonDock.Themes;
using AvalonTheme = AvalonDock.Themes.Theme;
using DockKeys = AvalonDock.Themes.VS2013.Themes.ResourceKeys;
using WpfWindow = System.Windows.Window;
using ModernWpf.Controls;

namespace KCViewer.Fork;

public enum KcvAccent
{
	Purple,
	Blue,
	Orange,
}

/// <summary>
/// Fork: KanColleViewer's look, from its theme library MetroRadiance (Themes/Dark.xaml, Light.xaml, Accents/*.xaml):
/// the accent colour on the status bar, the window border, the active docking tab and tool window title and
/// ModernWpf's accent, with the caption in the theme colour. Light and dark follow the viewer's own theme setting;
/// the panel colours of the viewer's light and dark themes are already the same Visual Studio colours
/// KanColleViewer uses, so they are kept.
/// </summary>
public static class KcvStyle
{
	/// <summary>MetroRadiance AccentColorKey, AccentHighlightColorKey, AccentActiveColorKey.</summary>
	public sealed record AccentColors(Color Accent, Color Highlight, Color Active);

	public static AccentColors ColorsOf(KcvAccent accent) => accent switch
	{
		KcvAccent.Blue => new(Rgb(0x007ACC), Rgb(0x28A0F0), Rgb(0x005AAA)),
		KcvAccent.Orange => new(Rgb(0xCA5100), Rgb(0xF07828), Rgb(0xB43C00)),
		_ => new(Rgb(0x68217A), Rgb(0x8C46A0), Rgb(0x5A1464)),
	};

	/// <summary>MetroRadiance ThemeColorKey: the window and its caption.</summary>
	public static Color ThemeColor(bool dark) => dark ? Rgb(0x2D2D30) : Rgb(0xEFEFF2);

	/// <summary>MetroRadiance BorderColorKey: the window border while another window is active.</summary>
	public static Color BorderColor(bool dark) => dark ? Rgb(0x3F3F46) : Rgb(0xB9B9C0);

	private static readonly Dictionary<(bool Dark, KcvAccent Accent), AvalonTheme> DockThemes = new();

	/// <summary>
	/// The viewer's Visual Studio 2013 docking theme with its blue replaced by the accent. Kept per combination, so
	/// setting it again after an unrelated configuration change does not make the docking manager reload its theme.
	/// </summary>
	public static AvalonTheme DockTheme(bool dark, KcvAccent accent)
	{
		if (DockThemes.TryGetValue((dark, accent), out AvalonTheme? cached)) return cached;

		AccentColors colors = ColorsOf(accent);
		ResourceDictionary original = new()
		{
			Source = new Uri($"/AvalonDock.Themes.VS2013;component/{(dark ? "Dark" : "Light")}Brushs.xaml", UriKind.Relative),
		};

		// The accent brushes (hovered auto-hide tab, drag preview box, docking guide) take their colour from
		// ControlAccentColorKey through DynamicResource, which a loose dictionary leaves unresolved (transparent);
		// read through an element holding the dictionary, they resolve.
		FrameworkElement resolver = new();
		resolver.Resources.MergedDictionaries.Add(original);

		ResourceDictionary brushes = new();
		foreach (DictionaryEntry entry in original)
		{
			brushes[entry.Key] = resolver.FindResource(entry.Key) switch
			{
				SolidColorBrush brush => Frozen(new SolidColorBrush(Recolor(brush.Color, colors)) { Opacity = brush.Opacity }),
				Color color => Recolor(color, colors),
				var value => value,
			};
		}

		// The hovered auto-hide tab (mockup 20261007-autohide-tab-hover-v2 ①, approved 2026-10-07 "구현"): the text in
		// white (black in the light theme) instead of the accent, which is hard to read on the tab; the bar keeps the accent.
		brushes[DockKeys.AutoHideTabHoveredText] = Frozen(new SolidColorBrush(dark ? Colors.White : Colors.Black));

		ResourceDictionary theme = new();
		theme.MergedDictionaries.Add(brushes);
		theme.MergedDictionaries.Add(new ResourceDictionary
		{
			Source = new Uri("/AvalonDock.Themes.VS2013;component/Themes/Generic.xaml", UriKind.Relative),
		});

		return DockThemes[(dark, accent)] = new KcvDockTheme(theme);
	}

	/// <summary>The blues of the VS2013 brushes (DarkBrushs.xaml, LightBrushs.xaml) by role.</summary>
	private static Color Recolor(Color color, AccentColors colors)
	{
		Color accent = (color.R << 16 | color.G << 8 | color.B) switch
		{
			// 0x1BA1E2: ControlAccentColorKey of the light theme
			0x007ACC or 0x1BA1E2 => colors.Accent,
			0x1C97EA or 0x52B0EF or 0x59A8DE or 0x0097FB => colors.Highlight,
			0x0E6198 => colors.Active,
			0xD0E6F5 or 0xC9DEF5 => Blend(colors.Accent, Colors.White, 0.8),
			_ => color,
		};
		accent.A = color.A;
		return accent;
	}

	private sealed class WindowState
	{
		public required Brush Active { get; set; }
		public required Brush Inactive { get; set; }
	}

	private static readonly Dictionary<WpfWindow, WindowState> Windows = new();

	/// <summary>
	/// Caption in the theme colour; a 1 px border in the accent while the window is active, as KanColleViewer has.
	/// While it is inactive, ModernWpf's window template draws its own border colour.
	/// </summary>
	public static void ApplyWindow(WpfWindow window, bool dark, AccentColors colors)
	{
		// Caption and window (the menu row shows the window background) in the one theme colour, as KanColleViewer has.
		SolidColorBrush caption = Frozen(new SolidColorBrush(ThemeColor(dark)));
		TitleBar.SetBackground(window, caption);
		TitleBar.SetInactiveBackground(window, caption);
		window.Background = caption;

		Brush active = Frozen(new SolidColorBrush(colors.Accent));
		Brush inactive = Frozen(new SolidColorBrush(BorderColor(dark)));
		if (Windows.TryGetValue(window, out WindowState? state))
		{
			state.Active = active;
			state.Inactive = inactive;
		}
		else
		{
			state = new WindowState { Active = active, Inactive = inactive };
			Windows[window] = state;
			window.Activated += OnActivationChanged;
			window.Deactivated += OnActivationChanged;
		}

		window.BorderThickness = new Thickness(1);
		window.BorderBrush = window.IsActive ? state.Active : state.Inactive;
	}

	/// <summary>Back to the viewer's own look.</summary>
	public static void RestoreWindow(WpfWindow window)
	{
		if (Windows.Remove(window))
		{
			window.Activated -= OnActivationChanged;
			window.Deactivated -= OnActivationChanged;
		}

		window.ClearValue(TitleBar.BackgroundProperty);
		window.ClearValue(TitleBar.InactiveBackgroundProperty);
		window.ClearValue(WpfWindow.BackgroundProperty);
		window.ClearValue(WpfWindow.BorderBrushProperty);
		window.ClearValue(WpfWindow.BorderThicknessProperty);
	}

	private static void OnActivationChanged(object? sender, EventArgs e)
	{
		if (sender is WpfWindow window && Windows.TryGetValue(window, out WindowState? state))
		{
			window.BorderBrush = window.IsActive ? state.Active : state.Inactive;
		}
	}

	public static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

	private static Color Rgb(int rgb) => Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

	private static Color Blend(Color color, Color with, double amount) => Color.FromRgb(
		(byte)(color.R + (with.R - color.R) * amount),
		(byte)(color.G + (with.G - color.G) * amount),
		(byte)(color.B + (with.B - color.B) * amount));

	private static T Frozen<T>(T freezable) where T : Freezable
	{
		freezable.Freeze();
		return freezable;
	}
}

/// <summary>AvalonDock's DictionaryTheme is abstract; this one only carries the dictionary built by <see cref="KcvStyle.DockTheme"/>.</summary>
internal sealed class KcvDockTheme(ResourceDictionary dictionary) : DictionaryTheme(dictionary);
