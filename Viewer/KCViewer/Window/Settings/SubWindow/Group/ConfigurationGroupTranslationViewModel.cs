using KCViewer.ViewModels.Translations;

namespace KCViewer.Window.Settings.SubWindow.Group;

public class ConfigurationGroupTranslationViewModel : TranslationBaseViewModel
{
	public string AutoUpdate => ConfigRes.AutoUpdate;
	public string AutoUpdateHint => ConfigRes.AutoUpdateHint;

	public string FormShipGroup_ShowStatusBar => ConfigurationResources.FormShipGroup_ShowStatusBar;
	public string ShowStatusbarHint => ConfigRes.ShowStatusbarHint;

	public string ShipNameSortMethod => ConfigRes.ShipNameSortMethod;
}
