using System.Collections.Generic;
using System.Threading.Tasks;
using MagicOnion;

namespace BrowserLibCore;

public interface IBrowserHost : IStreamingHub<IBrowserHost, IBrowser>
{
	Task ConnectToBrowser(long handle);
	Task<BrowserConfiguration> Configuration();
	Task SendErrorReport(string exceptionName, string message);
	Task AddLog(int priority, string message);
	Task ConfigurationUpdated(BrowserConfiguration configuration);
	Task SetProxyCompleted();
	Task RequestNavigation(string v);
	Task<byte[][]> GetIconResource();
	Task<bool> IsServerAlive();
	Task<int> GetTheme();
	Task<string> GetFleetData();
	Task<string?> GetFleetAndAirBaseData();
	Task<string> GetShipData(bool allShips);
	Task<string> GetEquipmentData(bool allEquipment);
	Task<Dictionary<int, List<int>>> GetMapList();
	Task<(int?, int?)> GetCurrentMap();

	// Fork: game traffic read through DevTools in the browser process (replaces the MITM proxy).
	// requestBody has the api_token value blanked; responseBodyGzip is DevToolsCapturePayload.Compress output.
	Task ApiCaptured(string url, string requestBody, byte[] responseBodyGzip);

	// Fork: game files (DevToolsGameFiles). The browser offers each response first and reads the body only when the host
	// wants it, then sends it in parts of at most DevToolsGameFiles.PartSize bytes, or reports that it could not read it.
	Task<bool> GameFileReceived(long id, string url, int status, string mimeType, Dictionary<string, string> headers);
	Task GameFileBodyPart(long id, byte[] part, bool isLast);
	Task GameFileFailed(long id);

	// Fork: the local skin folder while the local skin is on, null while it is off (LocalSkin in the browser).
	// Asked at startup and after IBrowser.ConfigurationChanged.
	Task<string?> LocalSkinFolder();

	// Fork: the KanColleViewer style's accent colour ("#RRGGBB") while the style is on, null while it is off.
	Task<string?> KcvAccentColor();

	// Fork: the screenshot's shortcut key as the menus show it ("F2", "Ctrl+Shift+S"), "" when there is none.
	// Asked at startup and after IBrowser.ConfigurationChanged, for the tool menu's 스크린샷 item.
	Task<string> ScreenshotHotkey();
}
