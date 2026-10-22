using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Avalonia.ShipGroup;
using HanumanInstitute.MvvmDialogs;

namespace KCViewer.Dialogs.DataGridSettings;

public partial class DataGridSettingsViewModel(DataGridSettingsModel settings)
	: ObservableObject, IModalDialogViewModel, ICloseable
{
	public event EventHandler? RequestClose;
	public bool? DialogResult { get; private set; }

	public DataGridSettingsModel Settings { get; } = settings;

	private int ColumnHeaderHeight { get; } = settings.ColumnHeaderHeight;
	private int RowHeight { get; } = settings.RowHeight;

	[RelayCommand]
	private void Ok()
	{
		DialogResult = true;
		RequestClose?.Invoke(this, EventArgs.Empty);
	}

	[RelayCommand]
	private void Cancel()
	{
		UndoChanges();

		DialogResult = false;
		RequestClose?.Invoke(this, EventArgs.Empty);
	}

	[RelayCommand]
	private void Closing()
	{
		if (DialogResult is null)
		{
			UndoChanges();
		}
	}

	private void UndoChanges()
	{
		Settings.ColumnHeaderHeight = ColumnHeaderHeight;
		Settings.RowHeight = RowHeight;
	}
}
