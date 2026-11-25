using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: 자동 데이터 업데이트 수동 병합 (see the XAML), opened by <see cref="ForkShipDefaultEquipment"/>. Its two buttons
/// answer, the title bar's ✕ (and Alt+F4) keeps the record as 기존 데이터 유지 does; closed with the viewer it has not
/// answered, so the next start asks again.
/// </summary>
public partial class KcvDefaultEquipmentMergeWindow
{
	private const int WM_SYSCOMMAND = 0x0112;
	private const int SC_CLOSE = 0xF060;

	public KcvDefaultEquipmentMergeViewModel ViewModel { get; }

	/// <summary>The user's answer: the ships to get the new data (none to keep the record).</summary>
	public event Action<IReadOnlyCollection<int>>? Answered;

	private bool IsAnswered { get; set; }

	/// <summary>The user closed the window (✕, Alt+F4), not the viewer.</summary>
	private bool ClosedByUser { get; set; }

	public KcvDefaultEquipmentMergeWindow(KcvDefaultEquipmentMergeViewModel viewModel)
	{
		InitializeComponent();

		ViewModel = viewModel;
		DataContext = viewModel;

		ApplySavedBounds();
		Closing += SaveBounds;
		Closed += OnClosed;
	}

	/// <summary>Over the viewer without taking the focus from it.</summary>
	public void ShowNotice()
	{
		if (Application.Current?.MainWindow is { IsLoaded: true } owner && !ReferenceEquals(owner, this))
		{
			Owner = owner;
		}
		else if (WindowStartupLocation != WindowStartupLocation.Manual)
		{
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}

		Show();
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WatchClose);
	}

	private IntPtr WatchClose(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == WM_SYSCOMMAND && (wParam.ToInt64() & 0xFFF0) == SC_CLOSE)
		{
			ClosedByUser = true;
		}

		return IntPtr.Zero;
	}

	private void OnApplyClick(object sender, RoutedEventArgs e) => Answer(ViewModel.SelectedShips);

	private void OnKeepClick(object sender, RoutedEventArgs e) => Answer([]);

	private void Answer(IReadOnlyCollection<int> applied)
	{
		if (IsAnswered) return;

		IsAnswered = true;
		Answered?.Invoke(applied);
		Close();
	}

	private void OnClosed(object? sender, EventArgs e)
	{
		if (!IsAnswered && ClosedByUser)
		{
			IsAnswered = true;
			Answered?.Invoke([]);
		}
	}

	private void ApplySavedBounds()
	{
		ForkSettings.WindowBounds bounds = ForkSettings.Current.DefaultEquipmentMergeWindow;
		if (bounds.Width <= 0 || bounds.Height <= 0) return;

		Width = Math.Max(bounds.Width, MinWidth);
		Height = Math.Max(bounds.Height, MinHeight);

		// the place only while it is still on a screen (a monitor may have gone)
		Rect screen = new(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
			SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
		if (screen.IntersectsWith(new Rect(bounds.Left, bounds.Top, Width, Height)))
		{
			WindowStartupLocation = WindowStartupLocation.Manual;
			Left = bounds.Left;
			Top = bounds.Top;
		}
	}

	private void SaveBounds(object? sender, CancelEventArgs e)
	{
		Rect rect = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;
		if (rect.Width <= 0 || rect.Height <= 0) return;

		ForkSettings.Current.DefaultEquipmentMergeWindow = new()
		{
			Left = rect.Left,
			Top = rect.Top,
			Width = rect.Width,
			Height = rect.Height,
		};
		ForkSettings.Current.Save();
	}
}
