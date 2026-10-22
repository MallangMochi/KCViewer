using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace KCViewer.Fork;

/// <summary>
/// Fork: used by the embedded test instance (<c>--embed-into=HWND</c>). Like EO's browser process, the window makes
/// itself a child of the host's window before it is shown. WPF rewrites the window style and position from its own
/// properties whenever something changes, so a hook keeps the child style and keeps the window filling the panel.
/// </summary>
public static class EmbeddedWindow
{
	private const int WM_STYLECHANGING = 0x007C;
	private const int WM_WINDOWPOSCHANGING = 0x0046;
	private const int GWL_STYLE = -16;
	private const int WS_CHILD = 0x40000000;
	private const int WS_CLIPSIBLINGS = 0x04000000;
	private const int WS_CLIPCHILDREN = 0x02000000;
	private const int WS_POPUP = unchecked((int)0x80000000);
	private const int WS_CAPTION = 0x00C00000;
	private const int WS_THICKFRAME = 0x00040000;
	private const int WS_SYSMENU = 0x00080000;
	private const int WS_MINIMIZEBOX = 0x00020000;
	private const int WS_MAXIMIZEBOX = 0x00010000;
	private const uint SWP_NOSIZE = 0x0001;
	private const uint SWP_NOMOVE = 0x0002;

	[StructLayout(LayoutKind.Sequential)]
	private struct STYLESTRUCT
	{
		public int StyleOld;
		public int StyleNew;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct WINDOWPOS
	{
		public IntPtr Hwnd;
		public IntPtr HwndInsertAfter;
		public int X;
		public int Y;
		public int Cx;
		public int Cy;
		public uint Flags;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int Left, Top, Right, Bottom;
	}

	[DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
	[DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
	[DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr hWnd);
	[DllImport("user32.dll")] private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
	[DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
	[DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
	[DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

	private static DispatcherTimer? HostWatch { get; set; }

	/// <summary>Call from <see cref="System.Windows.Window.SourceInitialized"/>, before the window is shown.</summary>
	public static void Install(System.Windows.Window window, IntPtr parent, int hostProcessId)
	{
		if (PresentationSource.FromVisual(window) is not HwndSource source) return;

		source.AddHook(Hook);

		if (IsWindow(parent))
		{
			IntPtr hwnd = source.Handle;
			SetWindowLong(hwnd, GWL_STYLE, WS_CHILD | WS_CLIPSIBLINGS | WS_CLIPCHILDREN);
			SetParent(hwnd, parent);
			if (GetClientRect(parent, out RECT client))
			{
				MoveWindow(hwnd, 0, 0, client.Right - client.Left, client.Bottom - client.Top, true);
			}
		}

		if (hostProcessId != 0)
		{
			// Close with the live instance even if it exits without closing this one (for example when it crashes).
			HostWatch = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
			HostWatch.Tick += (_, _) =>
			{
				if (IsHostAlive(hostProcessId)) return;

				HostWatch.Stop();
				window.Close();
			};
			HostWatch.Start();
		}
	}

	private static bool IsHostAlive(int processId)
	{
		try
		{
			using Process host = Process.GetProcessById(processId);
			return !host.HasExited;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private static IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == WM_STYLECHANGING && wParam.ToInt32() == GWL_STYLE)
		{
			STYLESTRUCT style = Marshal.PtrToStructure<STYLESTRUCT>(lParam);
			if ((style.StyleOld & WS_CHILD) != 0)
			{
				style.StyleNew = (style.StyleNew | WS_CHILD)
					& ~(WS_POPUP | WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
				Marshal.StructureToPtr(style, lParam, false);
			}
		}
		else if (msg == WM_WINDOWPOSCHANGING && (GetWindowLong(hwnd, GWL_STYLE) & WS_CHILD) != 0)
		{
			IntPtr parent = GetParent(hwnd);
			if (parent != IntPtr.Zero && GetClientRect(parent, out RECT client))
			{
				WINDOWPOS pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
				pos.X = 0;
				pos.Y = 0;
				pos.Cx = client.Right - client.Left;
				pos.Cy = client.Bottom - client.Top;
				pos.Flags &= ~(SWP_NOMOVE | SWP_NOSIZE);
				Marshal.StructureToPtr(pos, lParam, false);
			}
		}

		return IntPtr.Zero;
	}
}
