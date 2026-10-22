using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KCViewer.Utility;
using KCViewer.Window;
using KCViewer.Window.Dialog;
using KCViewer.Window.Integrate;
using MediaColor = System.Windows.Media.Color;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: KanColleViewer's colours for the viewer's WinForms windows (the dialogs and the WinForms docking panels)
/// while the style is on, like the WPF windows get them from the common control styles. WinForms has no styles, so a
/// window event hook (EVENT_OBJECT_SHOW) finds each WinForms control as it is shown and its colours are set one by
/// one; what was replaced is kept and put back when the style is turned off. Menus and tool strips take a renderer of
/// the same colours (ToolStripManager.Renderer), and the caption of a dialog the window colour (DWM, Windows 11).
/// </summary>
public static class KcvWinForms
{
	/// <summary>The colours of <see cref="KcvPanels"/> (Palette, ThemeBrushes) as WinForms colours.</summary>
	private sealed record Scheme(
		bool Dark,
		Color Background,
		Color Foreground,
		Color Inactive,
		Color Field,
		Color Header,
		Color Border,
		Color Separator,
		Color ButtonBackground,
		Color ButtonForeground,
		Color Hover,
		Color Accent)
	{
		public static Scheme Of(bool dark, Color accent) => dark
			? new(true, Rgb(0x2D2D30), Rgb(0xFFFFFF), Rgb(0x8C8C90), Rgb(0x1B1B1C), Rgb(0x1B1B1C), Rgb(0x3F3F46),
				Rgb(0x55555B), Rgb(0x1B1B1C), Rgb(0xD8D8DC), Rgb(0x3F3F46), accent)
			: new(false, Rgb(0xEFEFF2), Rgb(0x1E1E1E), Rgb(0x96969C), Rgb(0xFFFFFF), Rgb(0xDADAE0), Rgb(0xC6C6CC),
				Rgb(0xC6C6CC), Rgb(0xF7F7F9), Rgb(0x1E1E1E), Rgb(0xE4E4E8), accent);
	}

	/// <summary>
	/// Windows left as they are: the notifier takes the colours set in its own configuration, the capture overlays are
	/// drawn in fixed colours, the browser host only holds the browser process's window.
	/// </summary>
	private static readonly HashSet<Type> Excluded =
	[
		typeof(DialogNotifier), typeof(FormCandidate), typeof(FormCapturing), typeof(FormBrowserHost),
	];

	private static Scheme? Current { get; set; }

	/// <summary>How to put back what was changed on a control.</summary>
	private static readonly ConditionalWeakTable<Control, Action> Restores = new();

	private static readonly List<WeakReference<Form>> Forms = [];

	private static WinEventDelegate? Hook { get; set; }

	public static void Install()
	{
		if (Hook is not null) return;

		// Out of context: the event comes in the message loop of this (the UI) thread, where the WinForms windows live.
		Hook = OnWindowShown;
		SetWinEventHook(EventObjectShow, EventObjectShow, IntPtr.Zero, Hook, (uint)Environment.ProcessId, 0, WinEventOutOfContext);
	}

	/// <summary>Called with every theme change (<see cref="KcvPanels.Apply"/>).</summary>
	public static void Apply(bool enabled, bool dark, MediaColor accent)
	{
		Scheme? scheme = enabled ? Scheme.Of(dark, Color.FromArgb(accent.R, accent.G, accent.B)) : null;
		if (scheme == Current) return;

		Current = scheme;
		ToolStripManager.Renderer = scheme is null ? null : new KcvToolStripRenderer(scheme);

		// Windows opened while the style was off are themed now.
		if (scheme is not null)
		{
			foreach (Form form in System.Windows.Forms.Application.OpenForms.Cast<Form>().ToList())
			{
				if (IsThemed(form)) Track(form);
			}
		}

		Forms.RemoveAll(reference => !reference.TryGetTarget(out Form? form) || form.IsDisposed);
		foreach (WeakReference<Form> reference in Forms)
		{
			if (!reference.TryGetTarget(out Form? form)) continue;

			RestoreTree(form);
			if (scheme is not null) ThemeTree(form, scheme);
		}
	}

	private static bool IsThemed(Form form) =>
		form.GetType().Assembly == typeof(KcvWinForms).Assembly && !Excluded.Contains(form.GetType());

	private static void Track(Form form)
	{
		if (Forms.Any(reference => reference.TryGetTarget(out Form? tracked) && tracked == form)) return;

		Forms.Add(new WeakReference<Form>(form));
	}

	private static void OnWindowShown(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
	{
		if (Current is not { } scheme || idObject != ObjectIdWindow || idChild != 0) return;

		try
		{
			// Controls shown later (a panel made visible, a control added after the window opened) come here too.
			if (Control.FromHandle(hwnd) is not { } control) return;
			if ((control as Form ?? control.FindForm()) is not { } form || !IsThemed(form)) return;

			Track(form);
			ThemeTree(control, scheme);
		}
		catch (Exception ex)
		{
			Logger.Add(3, ForkStrings.KcvWinFormsError(ex.Message));
		}
	}

	private static void ThemeTree(Control control, Scheme scheme)
	{
		if (!Restores.TryGetValue(control, out _))
		{
			Restores.Add(control, Theme(control, scheme));
		}

		foreach (Control child in control.Controls)
		{
			ThemeTree(child, scheme);
		}
	}

	private static void RestoreTree(Control control)
	{
		if (Restores.TryGetValue(control, out Action? restore))
		{
			Restores.Remove(control);
			restore();
		}

		foreach (Control child in control.Controls)
		{
			RestoreTree(child);
		}
	}

	/// <summary>Sets the colours of one control and returns how to put them back.</summary>
	private static Action Theme(Control control, Scheme scheme)
	{
		List<Action> restores = [];

		switch (control)
		{
			case Form form:
				// The source of the ambient colours: labels, panels and check boxes without colours of their own follow it.
				restores.Add(KeepColors(form));
				form.BackColor = scheme.Background;
				form.ForeColor = scheme.Foreground;
				if (form.TopLevel) restores.Add(ThemeCaption(form, scheme));
				// The docked WinForms panels (window capture, a captured window's panel, JSON) in KanColleViewer's font,
				// as the WPF panels
				else restores.Add(ThemeFont(form));
				break;

			case Button button:
				// Restored in reverse order: the colours first, as setting them turns UseVisualStyleBackColor off.
				restores.Add(Keep(button, b => (b.FlatStyle, b.UseVisualStyleBackColor, b.FlatAppearance.BorderColor,
					b.FlatAppearance.MouseOverBackColor, b.FlatAppearance.MouseDownBackColor), (b, v) =>
					(b.FlatStyle, b.UseVisualStyleBackColor, b.FlatAppearance.BorderColor, b.FlatAppearance.MouseOverBackColor,
						b.FlatAppearance.MouseDownBackColor) = v));
				restores.Add(KeepColors(button));
				button.FlatStyle = FlatStyle.Flat;
				button.BackColor = scheme.ButtonBackground;
				button.ForeColor = scheme.ButtonForeground;
				button.FlatAppearance.BorderColor = scheme.Border;
				button.FlatAppearance.MouseOverBackColor = scheme.Hover;
				button.FlatAppearance.MouseDownBackColor = scheme.Accent;
				break;

			case TextBoxBase or ComboBox or UpDownBase:
				restores.Add(KeepColors(control));
				control.BackColor = scheme.Field;
				control.ForeColor = scheme.Foreground;
				if (control is ComboBox combo)
				{
					restores.Add(Keep(combo, c => c.FlatStyle, (c, v) => c.FlatStyle = v));
					combo.FlatStyle = FlatStyle.Flat;
				}
				// The dark edit theme of the file dialogs: a dark border, and dark scroll bars on a multi-line box.
				restores.Add(ThemeNative(control, scheme, "DarkMode_CFD"));
				break;

			case ListBox or TreeView or ListView:
				// Lists stand on the window colour, as KanColleViewer's tables do.
				restores.Add(KeepColors(control));
				control.BackColor = scheme.Background;
				control.ForeColor = scheme.Foreground;
				if (control is TreeView tree)
				{
					restores.Add(Keep(tree, t => t.LineColor, (t, v) => t.LineColor = v));
					tree.LineColor = scheme.Separator;
				}
				restores.Add(ThemeNative(control, scheme, "DarkMode_Explorer"));
				break;

			case DataGridView grid:
				restores.Add(ThemeGrid(grid, scheme));
				break;

			case ScrollBar:
				restores.Add(ThemeNative(control, scheme, "DarkMode_Explorer"));
				break;

			case TabControl tabs:
				restores.Add(TabPainter.Attach(tabs));
				break;

			case TabPage page:
				// A tab page with the visual style background draws the theme's white and ignores BackColor.
				restores.Add(Keep(page, p => p.UseVisualStyleBackColor, (p, v) => p.UseVisualStyleBackColor = v));
				restores.Add(KeepColors(page));
				page.UseVisualStyleBackColor = false;
				page.BackColor = scheme.Background;
				page.ForeColor = scheme.Foreground;
				break;

			case ToolStrip:
				// Drawn by ToolStripManager.Renderer
				break;

			default:
				// Labels, check boxes, panels, group boxes: only colours of their own, the others follow the window.
				restores.Add(KeepColors(control));
				if (HasOwn(control, nameof(Control.BackColor)) && control.BackColor != Color.Transparent)
				{
					control.BackColor = scheme.Background;
				}
				if (HasOwn(control, nameof(Control.ForeColor))) control.ForeColor = scheme.Foreground;
				break;
		}

		return () =>
		{
			for (int i = restores.Count - 1; i >= 0; i--)
			{
				restores[i]();
			}
		};
	}

	/// <summary>Whether the property has a value of its own rather than the ambient one of the parent.</summary>
	private static bool HasOwn(Control control, string property) =>
		TypeDescriptor.GetProperties(control)[property]?.ShouldSerializeValue(control) ?? false;

	private static Action KeepColors(Control control)
	{
		bool ownBack = HasOwn(control, nameof(Control.BackColor));
		bool ownFore = HasOwn(control, nameof(Control.ForeColor));
		Color back = control.BackColor;
		Color fore = control.ForeColor;

		return () =>
		{
			// The panels hosted in the main window take the viewer's theme colours (WinformsHostViewModel), which may
			// have changed since.
			if (control is Form { TopLevel: false })
			{
				back = Configuration.Config.UI.BackColor;
				fore = Configuration.Config.UI.ForeColor;
			}

			if (ownBack) control.BackColor = back;
			else control.ResetBackColor();
			if (ownFore) control.ForeColor = fore;
			else control.ResetForeColor();
		};
	}

	/// <summary>
	/// A family of Kcv.FontFamily in the panel's own size; the controls without a font of their own follow. WinForms
	/// takes one family, and Segoe UI's system fallback mixes several fonts in Korean text, so the family is the one
	/// Kcv.FontFamily draws the screen language with.
	/// </summary>
	private static Action ThemeFont(Form form)
	{
		Font font = form.Font;
		string family = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
		{
			"ko" => "Malgun Gothic",
			"ja" => "Meiryo UI",
			_ => "Segoe UI",
		};
		form.Font = new Font(family, font.Size, font.Style, font.Unit);
		return () => form.Font = font;
	}

	private static Action Keep<TControl, TValue>(TControl control, Func<TControl, TValue> get, Action<TControl, TValue> set)
	{
		TValue value = get(control);
		return () => set(control, value);
	}

	private static Action ThemeGrid(DataGridView grid, Scheme scheme)
	{
		DataGridViewCellStyle cells = new(grid.DefaultCellStyle);
		DataGridViewCellStyle columnHeaders = new(grid.ColumnHeadersDefaultCellStyle);
		DataGridViewCellStyle rowHeaders = new(grid.RowHeadersDefaultCellStyle);
		DataGridViewCellStyle alternating = new(grid.AlternatingRowsDefaultCellStyle);
		(bool headers, Color background, Color gridColor) = (grid.EnableHeadersVisualStyles, grid.BackgroundColor, grid.GridColor);
		(DataGridViewHeaderBorderStyle columnBorder, DataGridViewHeaderBorderStyle rowBorder) = (grid.ColumnHeadersBorderStyle, grid.RowHeadersBorderStyle);

		grid.EnableHeadersVisualStyles = false;
		grid.BackgroundColor = scheme.Background;
		grid.GridColor = scheme.Separator;
		// The raised header border draws light lines of the system colours
		grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
		grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
		SetStyle(grid.DefaultCellStyle, scheme.Background, scheme.Foreground, scheme.Accent, Color.White);
		SetStyle(grid.ColumnHeadersDefaultCellStyle, scheme.Header, scheme.Foreground, scheme.Header, scheme.Foreground);
		SetStyle(grid.RowHeadersDefaultCellStyle, scheme.Header, scheme.Foreground, scheme.Accent, Color.White);
		if (!alternating.BackColor.IsEmpty) grid.AlternatingRowsDefaultCellStyle.BackColor = scheme.Dark ? Rgb(0x333337) : Rgb(0xE6E6EA);

		Action scrollBars = ThemeNative(grid, scheme, "DarkMode_Explorer");

		return () =>
		{
			scrollBars();
			grid.DefaultCellStyle = cells;
			grid.ColumnHeadersDefaultCellStyle = columnHeaders;
			grid.RowHeadersDefaultCellStyle = rowHeaders;
			grid.AlternatingRowsDefaultCellStyle = alternating;
			(grid.EnableHeadersVisualStyles, grid.BackgroundColor, grid.GridColor) = (headers, background, gridColor);
			(grid.ColumnHeadersBorderStyle, grid.RowHeadersBorderStyle) = (columnBorder, rowBorder);
		};

		static void SetStyle(DataGridViewCellStyle style, Color back, Color fore, Color selectionBack, Color selectionFore)
		{
			style.BackColor = back;
			style.ForeColor = fore;
			style.SelectionBackColor = selectionBack;
			style.SelectionForeColor = selectionFore;
		}
	}

	/// <summary>
	/// A dark theme of Windows 10 1809 and later for what WinForms leaves to Windows (scroll bars, borders); the light
	/// theme keeps the default one.
	/// </summary>
	private static Action ThemeNative(Control control, Scheme scheme, string theme)
	{
		if (!scheme.Dark) return () => { };

		void Set(string? name)
		{
			if (control.IsHandleCreated) SetWindowTheme(control.Handle, name, null);
		}

		Set(theme);
		control.HandleCreated += OnHandleCreated;
		return () =>
		{
			control.HandleCreated -= OnHandleCreated;
			Set(null);
		};

		void OnHandleCreated(object? sender, EventArgs e) => Set(theme);
	}

	/// <summary>
	/// The caption in the window colour and the border in the accent while the window is active, as the WPF windows
	/// have them (KcvStyle.ApplyWindow). DWM caption colours need Windows 11; earlier versions keep their caption.
	/// </summary>
	private static Action ThemeCaption(Form form, Scheme scheme)
	{
		void SetColors()
		{
			if (!form.IsHandleCreated) return;

			SetAttribute(form.Handle, DwmUseImmersiveDarkMode, scheme.Dark ? 1 : 0);
			SetAttribute(form.Handle, DwmCaptionColor, ColorRef(scheme.Background));
			SetAttribute(form.Handle, DwmTextColor, ColorRef(scheme.Foreground));
			SetAttribute(form.Handle, DwmBorderColor, ColorRef(Form.ActiveForm == form ? scheme.Accent : scheme.Border));
		}

		void OnActivation(object? sender, EventArgs e) => SetAttribute(form.Handle, DwmBorderColor,
			ColorRef(Form.ActiveForm == form ? scheme.Accent : scheme.Border));

		void OnHandleCreated(object? sender, EventArgs e) => SetColors();

		int darkBefore = form.IsHandleCreated ? GetAttribute(form.Handle, DwmUseImmersiveDarkMode) : 0;
		SetColors();
		form.Activated += OnActivation;
		form.Deactivate += OnActivation;
		form.HandleCreated += OnHandleCreated;

		return () =>
		{
			form.Activated -= OnActivation;
			form.Deactivate -= OnActivation;
			form.HandleCreated -= OnHandleCreated;
			if (!form.IsHandleCreated) return;

			SetAttribute(form.Handle, DwmUseImmersiveDarkMode, darkBefore);
			SetAttribute(form.Handle, DwmCaptionColor, DwmColorDefault);
			SetAttribute(form.Handle, DwmTextColor, DwmColorDefault);
			SetAttribute(form.Handle, DwmBorderColor, DwmColorDefault);
		};
	}

	/// <summary>
	/// Tabs as text, the selected one in the text colour and the others dimmer (Kcv.TabItemStyle). The tab control
	/// draws its strip with the visual style whatever its colours, so the strip is painted over after it.
	/// </summary>
	private sealed class TabPainter : NativeWindow
	{
		private TabControl Tabs { get; }

		private TabPainter(TabControl tabs)
		{
			Tabs = tabs;
			if (tabs.IsHandleCreated) AssignHandle(tabs.Handle);
			tabs.HandleCreated += OnHandleCreated;
			tabs.SelectedIndexChanged += OnSelectedIndexChanged;
			tabs.Invalidate();
		}

		public static Action Attach(TabControl tabs)
		{
			TabPainter painter = new(tabs);
			return () =>
			{
				tabs.HandleCreated -= painter.OnHandleCreated;
				tabs.SelectedIndexChanged -= painter.OnSelectedIndexChanged;
				painter.ReleaseHandle();
				tabs.Invalidate();
			};
		}

		private void OnHandleCreated(object? sender, EventArgs e) => AssignHandle(Tabs.Handle);

		private void OnSelectedIndexChanged(object? sender, EventArgs e) => Tabs.Invalidate();

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			if (m.Msg == WmPaint && Current is { } scheme && Tabs.TabCount > 0) Paint(scheme);
		}

		private void Paint(Scheme scheme)
		{
			using Graphics graphics = Graphics.FromHwnd(Tabs.Handle);
			Rectangle pages = Tabs.DisplayRectangle;

			using (Region strip = new(Tabs.ClientRectangle))
			using (SolidBrush background = new(scheme.Background))
			{
				strip.Exclude(pages);
				graphics.FillRegion(background, strip);
			}

			using (Pen line = new(scheme.Separator))
			{
				graphics.DrawLine(line, pages.Left - 1, pages.Top - 1, pages.Right, pages.Top - 1);
			}

			for (int i = 0; i < Tabs.TabCount; i++)
			{
				TextRenderer.DrawText(graphics, Tabs.TabPages[i].Text, Tabs.Font, Tabs.GetTabRect(i),
					i == Tabs.SelectedIndex ? scheme.Foreground : scheme.Inactive,
					TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
			}
		}
	}

	/// <summary>Menus, context menus and tool strips in the window colours; items and arrows in the text colour.</summary>
	private sealed class KcvToolStripRenderer(Scheme scheme) : ToolStripProfessionalRenderer(new KcvColorTable(scheme))
	{
		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
		{
			e.TextColor = e.Item.Enabled ? scheme.Foreground : scheme.Inactive;
			base.OnRenderItemText(e);
		}

		protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
		{
			e.ArrowColor = e.Item?.Enabled != false ? scheme.Foreground : scheme.Inactive;
			base.OnRenderArrow(e);
		}
	}

	private sealed class KcvColorTable(Scheme scheme) : ProfessionalColorTable
	{
		public override Color MenuStripGradientBegin => scheme.Background;
		public override Color MenuStripGradientEnd => scheme.Background;
		public override Color ToolStripGradientBegin => scheme.Background;
		public override Color ToolStripGradientMiddle => scheme.Background;
		public override Color ToolStripGradientEnd => scheme.Background;
		public override Color ToolStripDropDownBackground => scheme.Background;
		public override Color ToolStripContentPanelGradientBegin => scheme.Background;
		public override Color ToolStripContentPanelGradientEnd => scheme.Background;
		public override Color ToolStripPanelGradientBegin => scheme.Background;
		public override Color ToolStripPanelGradientEnd => scheme.Background;
		public override Color StatusStripGradientBegin => scheme.Background;
		public override Color StatusStripGradientEnd => scheme.Background;
		public override Color ImageMarginGradientBegin => scheme.Background;
		public override Color ImageMarginGradientMiddle => scheme.Background;
		public override Color ImageMarginGradientEnd => scheme.Background;
		public override Color OverflowButtonGradientBegin => scheme.Background;
		public override Color OverflowButtonGradientMiddle => scheme.Background;
		public override Color OverflowButtonGradientEnd => scheme.Background;
		public override Color MenuBorder => scheme.Border;
		public override Color ToolStripBorder => scheme.Background;
		public override Color MenuItemBorder => scheme.Hover;
		public override Color MenuItemSelected => scheme.Hover;
		public override Color MenuItemSelectedGradientBegin => scheme.Hover;
		public override Color MenuItemSelectedGradientEnd => scheme.Hover;
		public override Color MenuItemPressedGradientBegin => scheme.Hover;
		public override Color MenuItemPressedGradientMiddle => scheme.Hover;
		public override Color MenuItemPressedGradientEnd => scheme.Hover;
		public override Color ButtonSelectedHighlight => scheme.Hover;
		public override Color ButtonSelectedGradientBegin => scheme.Hover;
		public override Color ButtonSelectedGradientMiddle => scheme.Hover;
		public override Color ButtonSelectedGradientEnd => scheme.Hover;
		public override Color ButtonSelectedBorder => scheme.Hover;
		public override Color ButtonPressedGradientBegin => scheme.Accent;
		public override Color ButtonPressedGradientMiddle => scheme.Accent;
		public override Color ButtonPressedGradientEnd => scheme.Accent;
		public override Color ButtonCheckedGradientBegin => scheme.Hover;
		public override Color ButtonCheckedGradientMiddle => scheme.Hover;
		public override Color ButtonCheckedGradientEnd => scheme.Hover;
		public override Color CheckBackground => scheme.Hover;
		public override Color CheckSelectedBackground => scheme.Hover;
		public override Color CheckPressedBackground => scheme.Hover;
		public override Color SeparatorDark => scheme.Separator;
		public override Color SeparatorLight => scheme.Background;
		public override Color GripDark => scheme.Separator;
		public override Color GripLight => scheme.Background;
	}

	private static Color Rgb(int rgb) => Color.FromArgb(rgb >> 16 & 0xFF, rgb >> 8 & 0xFF, rgb & 0xFF);

	private static int ColorRef(Color color) => color.R | color.G << 8 | color.B << 16;

	private static void SetAttribute(IntPtr hwnd, int attribute, int value) =>
		DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));

	private static int GetAttribute(IntPtr hwnd, int attribute) =>
		DwmGetWindowAttribute(hwnd, attribute, out int value, sizeof(int)) == 0 ? value : 0;

	private const uint EventObjectShow = 0x8002;
	private const uint WinEventOutOfContext = 0;
	private const int ObjectIdWindow = 0;
	private const int WmPaint = 0x000F;
	private const int DwmUseImmersiveDarkMode = 20;
	private const int DwmBorderColor = 34;
	private const int DwmCaptionColor = 35;
	private const int DwmTextColor = 36;
	private const int DwmColorDefault = unchecked((int)0xFFFFFFFF);

	private delegate void WinEventDelegate(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time);

	[DllImport("user32.dll")]
	private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventDelegate callback, uint processId, uint threadId, uint flags);

	[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
	private static extern int SetWindowTheme(IntPtr hwnd, string? subAppName, string? subIdList);

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);
}
