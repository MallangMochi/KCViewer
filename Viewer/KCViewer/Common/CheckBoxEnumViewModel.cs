using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KCViewer.Common;

public class CheckBoxEnumViewModel : ObservableObject
{
	public Enum Value { get; }
	public bool IsChecked { get; set; }
	public string? Tooltip { get; set; }

	public CheckBoxEnumViewModel(Enum value)
	{
		Value = value;
	}
}
