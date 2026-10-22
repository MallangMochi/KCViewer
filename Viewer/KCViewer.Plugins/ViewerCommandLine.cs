using System;

namespace KCViewer.Plugins;

/// <summary>
/// Command line options of the viewer, shared by the viewer and plugins that start another instance
/// (the test area plugin starts the test instance with them).
/// </summary>
public static class ViewerCommandLine
{
	/// <summary>Profile of the test instance.</summary>
	public const string TestProfile = "test";

	/// <summary><c>--profile=NAME</c>: run in <c>&lt;exe folder&gt;\Data\NAME</c>.</summary>
	public const string ProfilePrefix = "--profile=";

	/// <summary><c>--embed-into=HWND</c>: make the main window a child of that window before showing it.</summary>
	public const string EmbedIntoPrefix = "--embed-into=";

	/// <summary><c>--host-pid=PID</c>: close when that process (the live instance) is gone.</summary>
	public const string HostProcessIdPrefix = "--host-pid=";

	/// <summary><c>--start-url=URL</c>: the browser's start page for this run only; not saved.</summary>
	public const string StartUrlPrefix = "--start-url=";

	public static string Profile(string name) => ProfilePrefix + name;
	public static string EmbedInto(IntPtr parent) => EmbedIntoPrefix + parent.ToInt64();
	public static string HostProcessId(int processId) => HostProcessIdPrefix + processId;
	public static string StartUrl(string url) => StartUrlPrefix + url;
}
