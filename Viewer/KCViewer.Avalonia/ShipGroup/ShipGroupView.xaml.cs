using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace KCViewer.Avalonia.ShipGroup;

public class ShipGroupView : UserControl
{
	public ShipGroupView()
	{
		InitializeComponent();
	}

	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
	}
}
