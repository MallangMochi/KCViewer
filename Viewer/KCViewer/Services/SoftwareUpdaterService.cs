using System.Threading.Tasks;
using KCViewer.Avalonia.Translation;
using KCViewer.Utility;

namespace KCViewer.Services;

public class SoftwareUpdaterService : ISoftwareUpdaterService
{
	public Task DownloadData(string filename, DataType type) => SoftwareUpdater.DownloadData(filename, type);
}
