using System.ComponentModel.DataAnnotations;

namespace KCViewer.Window.Settings.SubWindow.Group;

public enum ShipNameSortMethod
{
	[Display(ResourceType = typeof(ConfigRes), Name = "ShipNameSortMethod_Number")]
	Library,

	[Display(ResourceType = typeof(ConfigRes), Name = "ShipNameSortMethod_Alphabet")]
	Alphabet,

	[Display(ResourceType = typeof(ConfigurationResources), Name = "ShipNameSortMethod_GameSort")]
	GameSort,
}
