using System.Drawing;
using System.IO;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the viewer's own icon, kcviewer.ico (user's choice 2026-10-22: candidate A of
/// viewer-design/mockups/20261022-icon-language-v1.html, made by viewer-design/icon/make-icons.py), in place of 74식's
/// Assets/AppIcon.ico and AppIcon_16.png from Assets.zip (ResourceManager): the tray, the notifier and the version and
/// alarm windows. The exe files' icons are ApplicationIcon in the project files, the menu's picture Icons.xaml.
/// </summary>
public static class ForkAppIcon
{
	/// <summary>LogicalName of kcviewer.ico in KCViewer.csproj.</summary>
	private const string ResourceName = "KCViewer.AppIcon.ico";

	/// <summary>The icon with all its sizes, or null when the resource is missing.</summary>
	public static Icon? Load()
	{
		using Stream? stream = typeof(ForkAppIcon).Assembly.GetManifestResourceStream(ResourceName);
		if (stream is null) return null;

		// as ResourceManager.LoadIconFromArchive: from a copy in memory, not from the resource stream itself
		using MemoryStream copy = new();
		stream.CopyTo(copy);
		copy.Position = 0;
		return new Icon(copy);
	}

	/// <summary>The icon's 16 px image (the image list's "AppIcon"), or null.</summary>
	public static Bitmap? Small()
	{
		using Icon? icon = Load();
		if (icon is null) return null;

		using Icon small = new(icon, 16, 16);
		return small.ToBitmap();
	}
}
