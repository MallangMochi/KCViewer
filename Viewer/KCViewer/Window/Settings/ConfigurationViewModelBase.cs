using CommunityToolkit.Mvvm.ComponentModel;

namespace KCViewer.Window.Settings;

public abstract class ConfigurationViewModelBase : ObservableValidator
{
	public abstract void Save();
}
