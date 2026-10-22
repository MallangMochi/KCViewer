using System.Windows;
using KCViewer.Resource;

namespace KCViewer.Common;

/// <summary>
/// Interaction logic for IconContentIcon.xaml
/// </summary>
public partial class IconContentIcon
{
	public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
		nameof(Type), typeof(IconContent?), typeof(IconContentIcon), new PropertyMetadata(default(IconContent?)));

	public IconContent? Type
	{
		get => (IconContent?)GetValue(TypeProperty);
		set => SetValue(TypeProperty, value);
	}

	public IconContentIcon()
	{
		InitializeComponent();
	}
}
