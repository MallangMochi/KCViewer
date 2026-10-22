using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Avalonia.Dialogs.ShipSelector;
using KCViewer.Avalonia.Services;
using KCViewer.Core.Services;
using KCViewer.Core.Types;
using KCViewer.Window.Tools.DropRecordViewer;

namespace KCViewer.Window.Dialog.ShipSelector;

public partial class DropRecordShipSelectorViewModel(TransliterationService transliterationService,
	ImageLoadService imageLoadService, List<IShipData> ships)
	: ShipSelectorViewModel(transliterationService, imageLoadService, ships)
{
	public List<DropRecordOption> DropRecordOptions { get; } = [.. Enum.GetValues<DropRecordOption>()];

	public DropRecordOption? SelectedOption { get; set; }

	[RelayCommand]
	private void SelectDropRecordOption(DropRecordOption? pickedOption)
	{
		SelectedOption = pickedOption;
		DialogResult = pickedOption is not null;

		Close();
	}
}
