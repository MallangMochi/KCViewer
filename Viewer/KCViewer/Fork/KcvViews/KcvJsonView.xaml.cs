using System;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.ViewModels.Translations;
using Microsoft.Win32;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the JSON panel in KanColleViewer's style (see the XAML and KcvJson.cs).</summary>
public partial class KcvJsonView
{
	public KcvJsonModel Model => KcvJsonModel.Instance;
	public FormJsonTranslationViewModel Translation { get; } = Ioc.Default.GetRequiredService<FormJsonTranslationViewModel>();

	private KcvJsonNode? Selected => Tree.SelectedItem as KcvJsonNode;

	public KcvJsonView()
	{
		InitializeComponent();
	}

	// A right click selects the node too, as in the viewer (the innermost item under the mouse comes last).
	private void OnNodeRightButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is TreeViewItem item) item.IsSelected = true;
	}

	private void OnTreeMenuOpened(object sender, RoutedEventArgs e) => CsvItem.IsEnabled = Selected?.CanWriteCsv ?? false;

	private void OnExpandAll(object sender, RoutedEventArgs e) => Selected?.ExpandAll();

	private void OnCollapse(object sender, RoutedEventArgs e)
	{
		if (Selected is { } node) node.IsExpanded = false;
	}

	private void OnCollapseParent(object sender, RoutedEventArgs e)
	{
		if (Selected?.Parent is { } parent) parent.IsExpanded = false;
	}

	private void OnWriteCsv(object sender, RoutedEventArgs e)
	{
		if (Selected is not { CanWriteCsv: true } node) return;

		SaveFileDialog dialog = new()
		{
			Title = Translation.CSVSaver,
			Filter = "CSV|*.csv|File|*",
		};
		if (dialog.ShowDialog(System.Windows.Window.GetWindow(this)) != true) return;

		try
		{
			KcvJsonModel.WriteCsv(node, dialog.FileName);
		}
		catch (Exception ex)
		{
			// the viewer's messages
			Utility.ErrorReporter.SendErrorReport(ex, "JSON: CSV 出力に失敗しました。");
			MessageBox.Show("CSV 出力に失敗しました。\r\n" + ex.Message, "出力エラー", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void OnCopy(object sender, RoutedEventArgs e) => Copy(Selected?.Data is { } data ? data.ToString() : null);

	private void OnCopyDocument(object sender, RoutedEventArgs e) => Copy(Selected?.Data is { } data ? KcvJsonModel.Document(data) : null);

	private static void Copy(string? text)
	{
		if (text is null)
		{
			SystemSounds.Exclamation.Play();
			return;
		}

		Clipboard.SetText(text);
	}

	private void OnFilterLostFocus(object sender, KeyboardFocusChangedEventArgs e) => Model.ApplyFilter();

	private void OnDragEnter(object sender, DragEventArgs e)
	{
		e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		e.Handled = true;
	}

	private void OnDrop(object sender, DragEventArgs e)
	{
		if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths) return;

		foreach (string path in paths.OrderBy(s => s)) Model.LoadFromFile(path);
	}
}
