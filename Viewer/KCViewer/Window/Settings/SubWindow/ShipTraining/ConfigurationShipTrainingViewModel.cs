using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Utility;

namespace KCViewer.Window.Settings.SubWindow.ShipTraining;

public class ConfigurationShipTrainingViewModel : ConfigurationViewModelBase
{
	public ConfigurationShipTrainingTranslationViewModel Translation { get; }
	private Configuration.ConfigurationData.ConfigFormShipTraining Config { get; }

	public bool AllowMultiplePlanPerShip { get; set; }

	public ConfigurationShipTrainingViewModel(Configuration.ConfigurationData.ConfigFormShipTraining config)
	{
		Translation = Ioc.Default.GetRequiredService<ConfigurationShipTrainingTranslationViewModel>();

		Config = config;
		Load();
	}

	public void Load()
	{
		AllowMultiplePlanPerShip = Config.AllowMultiplePlanPerShip;
	}

	public override void Save()
	{
		Config.AllowMultiplePlanPerShip = AllowMultiplePlanPerShip;
	}
}
