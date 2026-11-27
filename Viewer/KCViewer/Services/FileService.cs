using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using KCViewer.Observer;
using KCViewer.Utility;
using Microsoft.Win32;

namespace KCViewer.Services;

public class FileService
{
	private static System.Windows.Window MainWindow => App.Current!.MainWindow!;

	private static string LayoutFilter => "Layout File|*.xml";

	/// <summary>
	/// Opens a file selection dialog to select a layout file.
	/// </summary>
	/// <param name="path">Current layout path.</param>
	/// <returns>Selected file path or null if no path was selected.</returns>
	public static string? OpenLayoutPath(string path)
	{
		OpenFileDialog dialog = new()
		{
			Filter = LayoutFilter,
			Title = MainResources.OpenLayoutCaption,
		};

		PathHelper.InitOpenFileDialog(path, dialog);

		return dialog.ShowDialog(MainWindow) switch
		{
			true => PathHelper.GetPathFromOpenFileDialog(dialog),
			_ => null,
		};
	}

	/// <summary>
	/// Opens a file save dialog to save a layout file.
	/// </summary>
	/// <param name="path">Current layout path.</param>
	/// <returns>Selected file path or null if no path was selected.</returns>
	public static string? SaveLayoutPath(string path)
	{
		SaveFileDialog dialog = new()
		{
			Filter = LayoutFilter,
			Title = MainResources.OpenLayoutCaption,
		};

		PathHelper.InitSaveFileDialog(path, dialog);

		return dialog.ShowDialog(MainWindow) switch
		{
			true => PathHelper.GetPathFromSaveFileDialog(dialog),
			_ => null,
		};
	}

	/// <summary>
	/// Opens a folder browser dialog to select a folder.
	/// </summary>
	/// <param name="path">Current folder path.</param>
	/// <returns>Selected folder path or null if no path was selected.</returns>
	public static string? SelectFolder(string? path = null)
	{
		if (string.IsNullOrEmpty(path))
		{
			// Fork: the exe's folder; Assembly.Location is empty in a single-file build
			path = AppContext.BaseDirectory;
		}

		if (string.IsNullOrEmpty(path)) return null;

		string fullPath = Path.GetFullPath(path);

		OpenFolderDialog dialog = new()
		{
			InitialDirectory = fullPath,
		};

		return dialog.ShowDialog(MainWindow) switch
		{
			true => dialog.FolderName,
			_ => null,
		};
	}

	public static string? OpenApiListPath(string path)
	{
		OpenFileDialog dialog = new()
		{
			Filter = "Text File|*.txt|File|*",
			Title = "API リストを開く",
		};

		PathHelper.InitOpenFileDialog(path, dialog);

		return dialog.ShowDialog(MainWindow) switch
		{
			true => PathHelper.GetPathFromOpenFileDialog(dialog),
			_ => null,
		};
	}

	public static string? OpenSoundPath(string? path)
	{
		OpenFileDialog dialog = new()
		{
			Filter = Fork.ForkStrings.MusicFiles + "|" + string.Join(";", EOMediaPlayer.SupportedExtensions.Select(s => "*." + s)) + "|File|*",
			Title = NotifyRes.OpenSound,
		};

		if (!string.IsNullOrEmpty(path))
		{
			try
			{
				dialog.InitialDirectory = Path.GetDirectoryName(path);

			}
			catch (Exception) 
			{ 
				// do not throw to avoid issues
			}
		}

		return dialog.ShowDialog(MainWindow) switch
		{
			true => dialog.FileName,
			_ => null,
		};
	}

	public static string? OpenImagePath(string? path)
	{
		OpenFileDialog dialog = new()
		{
			Filter = "Image|*.bmp;*.div;*.jpg;*.jpeg;*.jpe;*.jfif;*.gif;*.png;*.tif;*.tiff|BMP|*.bmp;*.div|JPEG|*.jpg;*.jpeg;*.jpe;*.jfif|GIF|*.gif|PNG|*.png|TIFF|*.tif;*.tiff|File|*",
			Title = NotifyRes.OpenImage,
		};

		if (!string.IsNullOrEmpty(path))
		{
			try
			{
				dialog.InitialDirectory = Path.GetDirectoryName(path);

			}
			catch (Exception) 
			{ 
				// do not throw to avoid issues
			}
		}

		return dialog.ShowDialog(MainWindow) switch
		{
			true => dialog.FileName,
			_ => null,
		};
	}

	public string? ExportCsv(string? path)
	{
		if (string.IsNullOrEmpty(path))
		{
			path = Directory.GetCurrentDirectory();
		}

		SaveFileDialog dialog = new()
		{
			Filter = "CSV|*.csv|File|*",
			Title = EquipmentListResources.SaveCSVDialog,
			InitialDirectory = path,
		};

		return dialog.ShowDialog(MainWindow) switch
		{
			true => dialog.FileName,
			_ => null,
		};
	}
}
