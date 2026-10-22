using System;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Plugins;
using KCViewer.Resource;
using KCViewer.ViewModels;

namespace KCViewer.Fork.Plugins;

/// <summary>Fork: a dockable panel added by a plugin (shown by the template in Fork/ForkViews.xaml).</summary>
public sealed partial class PluginPanelViewModel : AnchorableViewModel, IPluginPanel
{
	private readonly Func<FrameworkElement> createContent;
	private FrameworkElement? content;

	/// <summary>Created on first use and then kept, so moving the panel keeps its state.</summary>
	public FrameworkElement Content => content ??= createContent();

	public event EventHandler? VisibilityChanged;

	public bool IsVisible => Visibility == Visibility.Visible;

	public PluginPanelViewModel(string pluginId, PluginPanelOptions options)
		: base(options.Title, $"Plugin.{pluginId}.{options.Id}", ParseIcon(options.Icon))
	{
		createContent = options.CreateContent;

		PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(Visibility)) VisibilityChanged?.Invoke(this, EventArgs.Empty);
		};
	}

	[RelayCommand]
	public void Show()
	{
		Visibility = Visibility.Visible;
		IsSelected = true;
		IsActive = true;
	}

	private static IconContent? ParseIcon(string? name) =>
		Enum.TryParse(name, out IconContent icon) ? icon : null;
}
