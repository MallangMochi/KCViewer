using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Utility;

namespace KCViewer.Window.Settings.SubWindow.Group;

public class ConfigurationGroupViewModel : ConfigurationViewModelBase
{
	public ConfigurationGroupTranslationViewModel Translation { get; }
	private Configuration.ConfigurationData.ConfigFormShipGroup Config { get; }

	public bool AutoUpdate { get; set; }

	public bool ShowStatusBar { get; set; }

	public ConfigurationGroupViewModel(Configuration.ConfigurationData.ConfigFormShipGroup config)
	{
		Translation = Ioc.Default.GetRequiredService<ConfigurationGroupTranslationViewModel>();

		Config = config;
		Load();
	}

	private void Load()
	{
		AutoUpdate = Config.AutoUpdate;
		ShowStatusBar = Config.ShowStatusBar;
	}

	public override void Save()
	{
		Config.AutoUpdate = AutoUpdate;
		Config.ShowStatusBar = ShowStatusBar;
	}
}
