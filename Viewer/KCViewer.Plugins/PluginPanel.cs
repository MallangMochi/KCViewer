using System;
using System.Windows;

namespace KCViewer.Plugins;

/// <summary>A dockable panel to add with <see cref="IPluginHost.AddPanel"/>.</summary>
public sealed class PluginPanelOptions
{
	/// <summary>Unique within the plugin; the saved layout finds the panel by it.</summary>
	public required string Id { get; init; }

	/// <summary>Panel and menu title. The test instance adds "(test)" to it.</summary>
	public required string Title { get; init; }

	/// <summary>
	/// Creates the panel's content once. WPF content is used as is; to show WinForms controls, wrap them in a
	/// <c>WindowsFormsHost</c>.
	/// </summary>
	public required Func<FrameworkElement> CreateContent { get; init; }

	/// <summary>Name of one of the viewer's icons, for example "FormJson" or "FormBrowser". Optional.</summary>
	public string? Icon { get; init; }
}

/// <summary>A panel the plugin added.</summary>
public interface IPluginPanel
{
	/// <summary>The panel is open (it may still be behind another tab).</summary>
	bool IsVisible { get; }

	/// <summary>Raised on the UI thread when the panel is opened or closed, including by the saved layout.</summary>
	event EventHandler? VisibilityChanged;

	/// <summary>Opens the panel and brings it to the front.</summary>
	void Show();
}
