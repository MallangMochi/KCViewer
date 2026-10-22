using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using KCViewer.Avalonia.ShipGroup;
using KCViewer.Data;
using KCViewer.Window.Dialog;
using Microsoft.Win32;

namespace KCViewer.Fork.KcvViews;

/// <summary>What the CSV output was asked: 취소, or the file, the format, the ships (null: the viewer's) and 열람 용's header (null: the viewer's).</summary>
public sealed record KcvShipGroupCsvChoice(
	bool Cancelled,
	string OutputPath,
	DialogShipGroupCSVOutput.OutputFormatConstants OutputFormat,
	IEnumerable<ShipData?>? Ships,
	string? UserHeader);

/// <summary>
/// Fork: the ship group's CSV output in KanColleViewer's style (see the XAML), opened through <see cref="TryAsk"/>, modal
/// over the window in front. The viewer writes the file as before.
/// </summary>
public partial class KcvShipGroupCsvWindow
{
	/// <summary>
	/// Null while the style is off (the caller opens the viewer's dialog), else the choice. The ships are the selected
	/// rows as the viewer's, or with none selected the group's in the table's order (the table keeps the model's
	/// collection view sorted as itself; the user's answer 2026-10-19 "1.나"); with no group, the viewer's (every ship).
	/// </summary>
	public static KcvShipGroupCsvChoice? TryAsk(bool groupSelected, ShipGroupViewModel table)
	{
		if (!KcvViewState.Current.Enabled) return null;

		KcvShipGroupCsvWindow window = new();
		if (KcvCalcWindows.FrontWindow() is { } owner) window.Owner = owner;

		if (window.ShowDialog() is not true)
		{
			return new KcvShipGroupCsvChoice(true, "", DialogShipGroupCSVOutput.OutputFormatConstants.User, null, null);
		}

		IEnumerable<ShipData?>? ships = groupSelected && table.SelectedShips.Count == 0
			? table.CollectionView.OfType<ShipGroupItemViewModel>().Select(s => KCDatabase.Instance.Ships[s.MasterId]).ToList()
			: null;

		return new KcvShipGroupCsvChoice(
			false,
			window.PathBox.Text.Trim(),
			window.UserFormat.IsChecked is true
				? DialogShipGroupCSVOutput.OutputFormatConstants.User
				: DialogShipGroupCSVOutput.OutputFormatConstants.Data,
			ships,
			ForkStrings.ShipCsvUserHeader);
	}

	private KcvShipGroupCsvWindow()
	{
		InitializeComponent();

		Loaded += (_, _) => PathBox.Focus();
	}

	/// <summary>
	/// The viewer's save dialog: 「CSV를 저장」, CSV or every file, first in the folder of the saved data; again in the
	/// folder of the file chosen (the viewer's dialog remembered it).
	/// </summary>
	private void BrowseButton_Click(object sender, RoutedEventArgs e)
	{
		SaveFileDialog dialog = new()
		{
			Title = ShipGroupCSVOutputResources.DialogSaveCSV,
			Filter = "CSV|*.csv|File|*",
		};

		string typed = PathBox.Text.Trim();
		if (FullPath(typed) is { } file && Path.GetDirectoryName(file) is { } folder && Directory.Exists(folder))
		{
			dialog.InitialDirectory = folder;
			dialog.FileName = Path.GetFileName(file);
		}
		else if (FullPath(Utility.Configuration.Config.Connection.SaveDataPath) is { } saved && Directory.Exists(saved))
		{
			dialog.InitialDirectory = saved;
		}

		if (dialog.ShowDialog(this) is true) PathBox.Text = dialog.FileName;
	}

	/// <summary>The full path, or null for none or one that is not a path.</summary>
	private static string? FullPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path)) return null;

		try
		{
			return Path.GetFullPath(path);
		}
		catch
		{
			return null;
		}
	}

	private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
