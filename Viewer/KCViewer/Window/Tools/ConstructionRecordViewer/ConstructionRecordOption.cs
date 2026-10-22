using System.ComponentModel.DataAnnotations;

namespace KCViewer.Window.Tools.ConstructionRecordViewer;

public enum ConstructionRecordOption
{
	[Display(ResourceType = typeof(ConstructionRecordViewerResources), Name = "NameAny")]
	All,
}
