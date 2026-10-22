using System.Collections.Generic;
using System.Threading.Tasks;
using BrowserLibCore;
using KCViewer.Fork.Plugins;
using KCViewer.Observer;

namespace KCViewer.Window;

/// <summary>
/// Fork: receives the traffic that the browser process captured through DevTools: API calls and game files for plugins.
/// </summary>
public partial class BrowserHostHub
{
	public Task ApiCaptured(string url, string requestBody, byte[] responseBodyGzip) =>
		APIObserver.Instance.ProcessCapturedApi(url, requestBody, DevToolsCapturePayload.Decompress(responseBodyGzip));

	public Task<bool> GameFileReceived(long id, string url, int status, string mimeType, Dictionary<string, string> headers) =>
		Task.FromResult(GameFileEvents.Offer(id, url, status, mimeType, headers));

	public Task GameFileBodyPart(long id, byte[] part, bool isLast)
	{
		GameFileEvents.AddPart(id, part, isLast);
		return Task.CompletedTask;
	}

	public Task GameFileFailed(long id)
	{
		GameFileEvents.Fail(id);
		return Task.CompletedTask;
	}
}
