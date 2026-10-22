using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Common.ContentDialogs;
using KCViewer.Common.ContentDialogs.ExportFilter;
using KCViewer.Common.ContentDialogs.ExportProgress;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the sortie record's CSV export dialogs in KanColleViewer's style (mockup 20261020-csv-export-dialogs-v2,
/// approved 2026-10-20 "구현"): small windows over the sortie record window instead of the viewer's dialogs inside it.
/// 추출 필터 can be cancelled (null: the export stops); 데이터 처리... closes itself when the work is done; a failure
/// shows in a window of its own.
/// </summary>
public sealed class KcvContentDialogService : ContentDialogService
{
	private readonly System.Windows.Window _owner;

	public KcvContentDialogService(System.Windows.Window owner)
	{
		_owner = owner;
	}

	public override Task<ExportFilterViewModel?> ShowExportFilterAsync(ExportFilterViewModel exportFilter)
	{
		KcvExportFilterWindow window = new(new KcvExportFilterViewModel(exportFilter)) { Owner = _owner };

		return Task.FromResult(window.ShowDialog() is true ? exportFilter : null);
	}

	/// <summary>Modal while the work goes on; true when it closed (done or 취소), as the viewer's.</summary>
	public override Task<bool> ShowExportProgressAsync(ExportProgressViewModel exportProgress)
	{
		KcvExportProgressWindow window = new(exportProgress) { Owner = _owner };
		window.ShowDialog();

		return Task.FromResult(true);
	}

	/// <summary>The viewer gives no title (its only text is 「CSV 저장에 실패했습니다: …」), so that is the title.</summary>
	public override Task<bool> ShowNotificationAsync(string? text, string? title = null)
	{
		KcvExportNotificationWindow window = new(text ?? "", title ?? CsvExportResources.FailedToSaveCsv) { Owner = _owner };
		window.ShowDialog();

		return Task.FromResult(true);
	}
}

/// <summary>
/// 추출 필터's model: the viewer's (IgnoreMisses, IgnoreCellFilters, its cells «1-1-A») with the cells in a line per map,
/// the maps in number order and the cells in name order (user's answer 2026-10-20 "2.가"). 「모두」 is the viewer's: all on
/// if any is off, else all off.
/// </summary>
public sealed class KcvExportFilterViewModel : ObservableObject
{
	public ExportFilterViewModel Filter { get; }
	public IReadOnlyList<KcvExportMap> Maps { get; }
	public ICommand ToggleAllCommand { get; }

	public KcvExportFilterViewModel(ExportFilterViewModel filter)
	{
		Filter = filter;

		Maps = filter.Destinations
			.Select(d => (Destination: d, Parts: d.Display.Split('-', 3)))
			.GroupBy(d => d.Parts.Length == 3 ? $"{d.Parts[0]}-{d.Parts[1]}" : d.Destination.Display)
			.Select(g => new KcvExportMap(g.Key, g
				.Select(d => new KcvExportCell(d.Destination, d.Parts.Length == 3 ? d.Parts[2] : d.Destination.Display))
				.OrderBy(c => c.Name.Length)
				.ThenBy(c => c.Name, StringComparer.Ordinal)
				.ToList()))
			.OrderBy(m => MapNumber(m.Name, 0))
			.ThenBy(m => MapNumber(m.Name, 1))
			.ToList();

		ToggleAllCommand = new RelayCommand(() =>
		{
			List<KcvExportCell> cells = Maps.SelectMany(m => m.Cells).ToList();
			bool on = cells.Any(c => !c.IsChecked);

			foreach (KcvExportCell cell in cells) cell.IsChecked = on;
		});
	}

	private static int MapNumber(string name, int part) =>
		name.Split('-') is { Length: 2 } parts && int.TryParse(parts[part], out int number) ? number : int.MaxValue;
}

/// <summary>A map's line: 「1-1」 and its cells.</summary>
public sealed record KcvExportMap(string Name, IReadOnlyList<KcvExportCell> Cells);

/// <summary>A cell's check: the viewer's destination (all its cell numbers) under the cell's name.</summary>
public sealed class KcvExportCell : ObservableObject
{
	private readonly DestinationItemViewModel _destination;

	public string Name { get; }

	public bool IsChecked
	{
		get => _destination.IsChecked;
		set
		{
			_destination.IsChecked = value;
			OnPropertyChanged(nameof(IsChecked));
		}
	}

	public KcvExportCell(DestinationItemViewModel destination, string name)
	{
		_destination = destination;
		Name = name;
	}
}

/// <summary>데이터 처리...'s model: 「41 / 82」 and the bar, from the viewer's progress (which changes on another thread).</summary>
public sealed class KcvExportProgressViewModel : ObservableObject
{
	private readonly ExportProgressViewModel _progress;

	public int Progress => _progress.Progress;
	public int Total => Math.Max(_progress.Total, 1);
	public string Text => $"{_progress.Progress} / {_progress.Total}";

	/// <summary>The work is done: the viewer's condition (progress at the total, on a change of the progress).</summary>
	public event EventHandler? Done;

	public KcvExportProgressViewModel(ExportProgressViewModel progress)
	{
		_progress = progress;
		_progress.PropertyChanged += ProgressChanged;
	}

	public void Detach() => _progress.PropertyChanged -= ProgressChanged;

	private void ProgressChanged(object? sender, PropertyChangedEventArgs e) => Application.Current?.Dispatcher.BeginInvoke(() =>
	{
		OnPropertyChanged(nameof(Progress));
		OnPropertyChanged(nameof(Total));
		OnPropertyChanged(nameof(Text));

		if (e.PropertyName is nameof(ExportProgressViewModel.Progress) && _progress.Progress >= _progress.Total)
		{
			Done?.Invoke(this, EventArgs.Empty);
		}
	});
}
