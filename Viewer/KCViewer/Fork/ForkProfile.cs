using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using KCViewer.Plugins;

namespace KCViewer.Fork;

/// <summary>
/// Fork: one exe, several working folders ("profiles").
/// <c>--profile=NAME</c> runs in <c>&lt;exe folder&gt;\Data\NAME</c>. EO keeps Settings\, Record\, the API database and
/// (in this fork) the WebView2 profile and the translation cache relative to the working folder, so each profile
/// has its own settings, records, layout and login. Without the option it runs in <c>Data\live</c> (the live
/// instance), so the exe folder holds only the program and a new build can replace it without touching any data.
/// The test area plugin shows the test instance (profile "test") inside a panel of the live instance.
/// </summary>
public static partial class ForkProfile
{
	public const string TestProfileName = ViewerCommandLine.TestProfile;

	/// <summary>The working folder without <c>--profile</c>; <see cref="Name"/> stays null for it, which plugins read as live.</summary>
	public const string LiveFolderName = "live";

	public static string? Name { get; } = ForkOptions.Argument(ViewerCommandLine.ProfilePrefix);

	public static bool IsTest => Name == TestProfileName;

	/// <summary>
	/// <c>--embed-into=HWND</c>: started by the live instance; the main window makes itself a child of that window
	/// (the test area panel) before it is shown.
	/// </summary>
	public static IntPtr EmbedParent { get; } =
		long.TryParse(ForkOptions.Argument(ViewerCommandLine.EmbedIntoPrefix), out long handle) ? new IntPtr(handle) : IntPtr.Zero;

	/// <summary><c>--host-pid=PID</c>: the live instance; the embedded instance closes itself when it is gone.</summary>
	public static int HostProcessId { get; } =
		int.TryParse(ForkOptions.Argument(ViewerCommandLine.HostProcessIdPrefix), out int pid) ? pid : 0;

	public static bool IsEmbedded => EmbedParent != IntPtr.Zero;

	/// <summary>Must run before anything reads a relative path (first line of the App constructor).</summary>
	public static void Apply()
	{
		if (Name is not null && !ProfileNamePattern().IsMatch(Name))
		{
			throw new ArgumentException($"Invalid profile name: {Name}. Use letters, digits, '-' and '_'.");
		}

		string folder = Folder(Name ?? LiveFolderName);
		Directory.CreateDirectory(folder);
		Directory.SetCurrentDirectory(folder);
	}

	public static string Folder(string name) => Path.Combine(AppContext.BaseDirectory, "Data", name);

	/// <summary>
	/// Name for the single-instance mutex: one per exe and working folder, so the live and test instances can run
	/// together but the same profile cannot be opened twice. Hashed because mutex names are limited in length.
	/// </summary>
	public static string MutexName
	{
		get
		{
			string key = $"{Environment.ProcessPath}|{Directory.GetCurrentDirectory()}".ToUpperInvariant();
			return "KCViewer-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..32];
		}
	}

	public static string TitleSuffix => IsTest ? $" ({ForkStrings.Test})" : "";

	public static string DecorateTitle(string title) =>
		IsTest && !title.EndsWith(TitleSuffix, StringComparison.Ordinal) ? title + TitleSuffix : title;

	[GeneratedRegex("^[A-Za-z0-9_-]+$")]
	private static partial Regex ProfileNamePattern();
}
