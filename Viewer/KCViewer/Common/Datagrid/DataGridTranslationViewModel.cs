using KCViewer.ViewModels.Translations;

namespace KCViewer.Common.Datagrid;
public class DataGridTranslationViewModel : TranslationBaseViewModel
{
	public string HideColumn => DataGridResources.HideColumn;
	public string OpenColumnSelector => DataGridResources.OpenColumnSelector;
	public string ClearSorting => DataGridResources.ClearSorting;
}
