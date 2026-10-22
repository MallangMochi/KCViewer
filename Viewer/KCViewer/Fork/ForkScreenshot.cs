using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using KCViewer.Utility;
using KCViewer.Window;

namespace KCViewer.Fork;

/// <summary>
/// Fork: a screenshot key, modifiers and a key, written as the menus show it ("F2", "Ctrl+Shift+S")
/// (mockup 20261009-screenshot-capture-v2).
/// </summary>
public readonly record struct ForkHotkey(ModifierKeys Modifiers, Key Key)
{
	// keys the browser uses already: refresh, hard refresh, mute, developer tools; and closing a window
	private static readonly HashSet<ForkHotkey> Reserved =
	[
		new(ModifierKeys.None, Key.F5),
		new(ModifierKeys.Control, Key.F5),
		new(ModifierKeys.None, Key.F7),
		new(ModifierKeys.None, Key.F12),
		new(ModifierKeys.Control, Key.M),
		new(ModifierKeys.Alt, Key.F4),
	];

	/// <summary>
	/// F1 to F12, alone or with any of Ctrl, Alt, Shift; a letter or a digit only with Ctrl or Alt (Shift may be added),
	/// so typing never takes a screenshot; not the keys the browser uses.
	/// </summary>
	public static bool IsAllowed(ModifierKeys modifiers, Key key)
	{
		if ((modifiers & ModifierKeys.Windows) != 0) return false;
		if (Reserved.Contains(new(modifiers, key))) return false;
		if (key is >= Key.F1 and <= Key.F12) return true;

		bool letterOrDigit = key is >= Key.A and <= Key.Z or >= Key.D0 and <= Key.D9;
		return letterOrDigit && (modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) != 0;
	}

	public static ForkHotkey? Parse(string? text)
	{
		if (string.IsNullOrWhiteSpace(text)) return null;

		ModifierKeys modifiers = ModifierKeys.None;
		string[] parts = text.Split('+');
		for (int i = 0; i < parts.Length - 1; i++)
		{
			modifiers |= parts[i] switch
			{
				"Ctrl" => ModifierKeys.Control,
				"Alt" => ModifierKeys.Alt,
				"Shift" => ModifierKeys.Shift,
				_ => ModifierKeys.Windows,
			};
		}

		string last = parts[^1];
		string keyName = last.Length == 1 && char.IsDigit(last[0]) ? "D" + last : last;
		if (!Enum.TryParse(keyName, out Key key)) return null;

		return IsAllowed(modifiers, key) ? new ForkHotkey(modifiers, key) : null;
	}

	public override string ToString()
	{
		List<string> parts = [];
		if ((Modifiers & ModifierKeys.Control) != 0) parts.Add("Ctrl");
		if ((Modifiers & ModifierKeys.Alt) != 0) parts.Add("Alt");
		if ((Modifiers & ModifierKeys.Shift) != 0) parts.Add("Shift");
		parts.Add(Key is >= Key.D0 and <= Key.D9 ? ((int)(Key - Key.D0)).ToString() : Key.ToString());
		return string.Join("+", parts);
	}
}

/// <summary>
/// Fork: the screenshots the viewer asks the browser for (mockup 20261009-screenshot-capture-v2): the shortcut key set in
/// the settings (<see cref="ForkSettings.ScreenshotSettings"/>) and the screenshot panel's button. The key is a Windows
/// hot key, so it is taken whichever window of the viewer has the focus, the game included; it is registered only while a
/// window of the viewer (or of its browser) is in front, so another program in front keeps the same key, and not while
/// the settings' key box waits for a key.
/// </summary>
public static class ForkScreenshot
{
	private const int HotkeyId = 0x5353;
	private const int WM_HOTKEY = 0x0312;
	private const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_NOREPEAT = 0x4000;
	private const uint EVENT_SYSTEM_FOREGROUND = 0x3, WINEVENT_OUTOFCONTEXT = 0x0;
	private static readonly IntPtr HWND_MESSAGE = new(-3);

	private static HwndSource? MessageWindow { get; set; }
	// kept here so the collector leaves the callback alone while Windows calls it
	private static WinEventDelegate? ForegroundCallback { get; set; }
	private static IntPtr ForegroundHook { get; set; }

	private static ForkHotkey? Registered { get; set; }
	private static bool Suspended { get; set; }
	/// <summary>The key last found taken by another program, logged once rather than on each change of window.</summary>
	private static ForkHotkey? FailedOnce { get; set; }

	/// <summary>Whether the browser is running and can take a screenshot (the screenshot panel's button).</summary>
	public static bool CanTake => FormBrowserHost.Instance?.ForkBrowserReady == true;

	/// <summary>Called once on the UI thread when the main window starts.</summary>
	public static void Start()
	{
		if (MessageWindow is not null) return;

		MessageWindow = new HwndSource(new HwndSourceParameters("ForkScreenshotHotkey")
		{
			ParentWindow = HWND_MESSAGE,
			WindowStyle = 0,
		});
		MessageWindow.AddHook(WndProc);

		ForegroundCallback = (_, _, _, _, _, _, _) => Update();
		ForegroundHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, ForegroundCallback, 0, 0, WINEVENT_OUTOFCONTEXT);

		Configuration.Instance.ConfigurationChanged += Update;
		if (System.Windows.Application.Current is { } application) application.Exit += (_, _) => Stop();
		Update();
	}

	/// <summary>Takes a screenshot through the browser, or logs why it cannot.</summary>
	public static void Take()
	{
		if (FormBrowserHost.Instance is not { ForkBrowserReady: true } host)
		{
			Logger.Add(2, ForkStrings.ScreenshotNoBrowser);
			return;
		}

		try
		{
			host.Browser.TakeScreenshot();
		}
		catch (Exception ex)
		{
			Logger.Add(3, ForkStrings.ScreenshotFailed(ex.Message));
		}
	}

	/// <summary>While the settings' key box waits for a key: the key goes to the box instead of taking a screenshot.</summary>
	public static void Suspend(bool suspended)
	{
		Suspended = suspended;
		Update();
	}

	private static void Update()
	{
		if (MessageWindow is null) return;

		ForkHotkey? wanted = Suspended || !IsViewerInFront() ? null : ForkHotkey.Parse(ForkSettings.Current.Screenshot.Hotkey);
		if (wanted == Registered) return;

		if (Registered is not null)
		{
			UnregisterHotKey(MessageWindow.Handle, HotkeyId);
			Registered = null;
		}

		if (wanted is not { } hotkey) return;

		uint modifiers = MOD_NOREPEAT
			| ((hotkey.Modifiers & ModifierKeys.Control) != 0 ? MOD_CONTROL : 0)
			| ((hotkey.Modifiers & ModifierKeys.Alt) != 0 ? MOD_ALT : 0)
			| ((hotkey.Modifiers & ModifierKeys.Shift) != 0 ? MOD_SHIFT : 0);

		if (RegisterHotKey(MessageWindow.Handle, HotkeyId, modifiers, (uint)KeyInterop.VirtualKeyFromKey(hotkey.Key)))
		{
			Registered = hotkey;
			FailedOnce = null;
		}
		else if (FailedOnce != hotkey)
		{
			FailedOnce = hotkey;
			Logger.Add(3, ForkStrings.ScreenshotHotkeyTaken(hotkey.ToString()));
		}
	}

	private static bool IsViewerInFront()
	{
		GetWindowThreadProcessId(GetForegroundWindow(), out uint processId);
		return processId == (uint)Environment.ProcessId || processId == FormBrowserHost.Instance?.ForkBrowserProcessId;
	}

	private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == WM_HOTKEY && wParam == HotkeyId)
		{
			handled = true;
			Take();
		}

		return IntPtr.Zero;
	}

	private static void Stop()
	{
		if (MessageWindow is null) return;

		if (Registered is not null) UnregisterHotKey(MessageWindow.Handle, HotkeyId);
		if (ForegroundHook != IntPtr.Zero) UnhookWinEvent(ForegroundHook);
		MessageWindow.Dispose();
		MessageWindow = null;
		Registered = null;
	}

	private delegate void WinEventDelegate(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	[DllImport("user32.dll")]
	private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventDelegate callback, uint processId, uint threadId, uint flags);

	[DllImport("user32.dll")]
	private static extern bool UnhookWinEvent(IntPtr hook);
}
