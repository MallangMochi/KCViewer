using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Utility;

namespace KCViewer.Window.Settings.SubWindow.Combat;

public class ConfigurationCombatViewModel : ConfigurationViewModelBase
{
	public ConfigurationCombatTranslationViewModel Translation { get; }
	private Configuration.ConfigurationData.ConfigFormBattle Config { get; }

	public bool IsScrollable { get; set; }

	public bool HideDuringBattle { get; set; }

	public bool Display7thAsSingleLine { get; set; }

	public ConfigurationCombatViewModel(Configuration.ConfigurationData.ConfigFormBattle config)
	{
		Translation = Ioc.Default.GetRequiredService<ConfigurationCombatTranslationViewModel>();

		Config = config;
		Load();
	}

	private void Load()
	{
		IsScrollable = Config.IsScrollable;
		HideDuringBattle = Config.HideDuringBattle;
		Display7thAsSingleLine = Config.Display7thAsSingleLine;
	}

	public override void Save()
	{
		Config.IsScrollable = IsScrollable;
		Config.HideDuringBattle = HideDuringBattle;
		Config.Display7thAsSingleLine = Display7thAsSingleLine;
	}
}
