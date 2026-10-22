using System;
using System.Runtime.InteropServices;

namespace KCUpdater;

/// <summary>
/// Fork: the updater's only screens, Windows message boxes (user's decision 2026-10-20 "창없이"). NativeAOT has neither
/// WinForms nor WPF, so <c>MessageBoxW</c> is called directly. Shown on top and in front: the viewer has closed and
/// nothing else of the updater is visible.
/// </summary>
internal static partial class Dialogs
{
	/// <summary>False with <c>--no-dialogs</c>: each message is only logged and each question answered "no".</summary>
	public static bool Enabled { get; set; } = true;

	/// <summary>Where a message goes as well (the updater's log), once there is one.</summary>
	public static Action<string>? Logged { get; set; }

	public static void Error(string message) => Show(message, MB_OK | MB_ICONERROR);

	public static void Warning(string message) => Show(message, MB_OK | MB_ICONWARNING);

	/// <summary>Retry (true) or Cancel (false).</summary>
	public static bool AskRetry(string message) => Show(message, MB_RETRYCANCEL | MB_ICONWARNING) == IDRETRY;

	private static int Show(string message, uint type)
	{
		Logged?.Invoke("message: " + message.ReplaceLineEndings(" "));
		if (!Enabled) return IDCANCEL;

		return MessageBox(IntPtr.Zero, message, Strings.Title, type | MB_SETFOREGROUND | MB_TOPMOST);
	}

	private const uint MB_OK = 0x0;
	private const uint MB_RETRYCANCEL = 0x5;
	private const uint MB_ICONERROR = 0x10;
	private const uint MB_ICONWARNING = 0x30;
	private const uint MB_SETFOREGROUND = 0x10000;
	private const uint MB_TOPMOST = 0x40000;
	private const int IDCANCEL = 2;
	private const int IDRETRY = 4;

	[LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial int MessageBox(IntPtr owner, string text, string caption, uint type);
}
