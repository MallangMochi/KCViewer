using KCViewer.Common;
using KCViewer.Window.Tools.DatabaseExplorer.ApiFile;
using KCViewer.Window.Tools.DatabaseExplorer.ApiTypeTester;

namespace KCViewer.Window.Tools.DatabaseExplorer;

public class DatabaseExplorerViewModel : WindowViewModelBase
{
	public ApiFileExplorerViewModel ApiFileExplorer { get; } = new();
	public ApiTypeTesterViewModel ApiTypeTester { get; } = new();
}
