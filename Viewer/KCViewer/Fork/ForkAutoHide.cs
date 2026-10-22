using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using AvalonDock;
using AvalonDock.Controls;
using AvalonDock.Layout;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the tab of an auto-hidden panel (a panel unpinned to a side or the bottom, showing only its name) works as a
/// toggle (request 2026-10-07, option 나): a click opens the panel and a click on the tab of the open panel collapses
/// it again; a click elsewhere still collapses it, as before. The panel no longer opens when the mouse rests on its tab.
/// Applies with the KanColleViewer style on and off. Nothing goes into the layout: the hover opening is skipped at the
/// event instead of turning off the panels' <c>CanShowOnHover</c>, which the layout file keeps.
/// The open panel is also repainted once it is shown or resized, against the part over the browser staying black.
/// </summary>
public static class ForkAutoHide
{
	/// <summary>
	/// What AvalonDock's close timer does when the open panel is left: internal in AvalonDock 4.74.1, so through
	/// reflection. If a later version drops it, the tab of the open panel does nothing, as before.
	/// </summary>
	private static readonly MethodInfo? HideAutoHideWindow = typeof(DockingManager).GetMethod(
		"HideAutoHideWindow", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(LayoutAnchorControl)]);

	public static void Register()
	{
		EventManager.RegisterClassHandler(typeof(LayoutAnchorControl), Mouse.MouseEnterEvent,
			new MouseEventHandler(OnMouseEnter));
		EventManager.RegisterClassHandler(typeof(LayoutAnchorControl), UIElement.PreviewMouseLeftButtonDownEvent,
			new MouseButtonEventHandler(OnPreviewMouseLeftButtonDown));
		EventManager.RegisterClassHandler(typeof(LayoutAutoHideWindowControl), FrameworkElement.LoadedEvent,
			new RoutedEventHandler(OnWindowLoaded));
		EventManager.RegisterClassHandler(typeof(LayoutAutoHideWindowControl), FrameworkElement.SizeChangedEvent,
			new SizeChangedEventHandler(OnWindowSizeChanged));
	}

	/// <summary>
	/// Runs before the tab's own OnMouseEnter (called from the base class's class handler, which skips handled events),
	/// so the timer that opens the panel on hover never starts. The tab's highlight follows IsMouseOver, not this event.
	/// </summary>
	private static void OnMouseEnter(object sender, MouseEventArgs e)
	{
		e.Handled = true;
	}

	/// <summary>The tab of the open panel: collapse it. Handled, so the tab's OnMouseDown does not open it again.</summary>
	private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (HideAutoHideWindow is null) return;
		if (sender is not LayoutAnchorControl { Model: LayoutContent model } anchor) return;
		if (model.Root?.Manager is not DockingManager manager) return;
		if (manager.AutoHideWindow?.Model != model) return;

		HideAutoHideWindow.Invoke(manager, [anchor]);
		e.Handled = true;
	}

	/// <summary>
	/// The open panel is a window of its own (an HwndHost) over the browser, another window. Opened again after its
	/// size was changed, the part over the browser sometimes stayed black until something there changed (the
	/// viewer's own build too, user report 2026-10-07); a repaint of that window from outside cleared it in the test
	/// instance. So the window is repainted once it is shown or resized and the layout is done.
	/// </summary>
	private static void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is not LayoutAutoHideWindowControl window) return;

		// Loaded can come again (moved in the tree); one subscription
		window.IsVisibleChanged -= OnWindowVisibleChanged;
		window.IsVisibleChanged += OnWindowVisibleChanged;
	}

	private static void OnWindowVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is HwndHost window && e.NewValue is true) Repaint(window);
	}

	private static void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (sender is HwndHost window) Repaint(window);
	}

	private static void Repaint(HwndHost window)
	{
		window.Dispatcher.InvokeAsync(() =>
		{
			if (window.Handle == IntPtr.Zero || !window.IsVisible) return;

			RedrawWindow(window.Handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_ALLCHILDREN | RDW_UPDATENOW);
		}, DispatcherPriority.ContextIdle);
	}

	private const uint RDW_INVALIDATE = 0x0001;
	private const uint RDW_ALLCHILDREN = 0x0080;
	private const uint RDW_UPDATENOW = 0x0100;

	[DllImport("user32.dll")] private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
}
