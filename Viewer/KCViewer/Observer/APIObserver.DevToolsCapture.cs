using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using BrowserLibCore;
using KCViewer.Data;
using KCViewer.Utility;

namespace KCViewer.Observer;

/// <summary>
/// Fork: entry point for traffic captured through DevTools in the browser process.
/// Does what <see cref="ProxyOnBeforeResponse"/> does for the proxy, without the Titanium parts.
/// </summary>
public sealed partial class APIObserver
{
	/// <summary>
	/// Replaces <see cref="Start"/> when DevTools capture is on. Sets only the GUI object that
	/// <see cref="ProcessApiDataAsync"/> dispatches to; <see cref="Start"/> would also start the Titanium proxy.
	/// </summary>
	public void StartDevToolsCapture(Control uiControl)
	{
		UIControl = uiControl;
	}

	public async Task ProcessCapturedApi(string url, string requestBody, string responseBody)
	{
		// The browser already blanks api_token; blank it again so no other path can pass a token on.
		requestBody = DevToolsCapturePayload.BlankToken(requestBody);
		Configuration.ConfigurationData.ConfigConnection c = Configuration.Config.Connection;

		Fork.Plugins.ApiEvents.Raise(url, requestBody, responseBody);

		await ApiFileService.Value.Add(url.Split("/kcsapi/").Last(), requestBody, responseBody);

		if (c.SaveReceivedData && c.SaveRequest)
		{
			_ = Task.Run(() => SaveRequest(url, requestBody));
		}

		await ApiProcessingChannel.Writer.WriteAsync(() => LoadRequest(url, requestBody));

		if (c.SaveReceivedData && c.SaveResponse)
		{
			Directory.CreateDirectory(c.SaveDataPath);
			_ = Task.Run(() => SaveResponse(url, responseBody));
		}

		await ApiProcessingChannel.Writer.WriteAsync(() => LoadResponse(url, responseBody));

		if (ServerAddress == null)
		{
			// Same value the proxy used (the Host header): host plus port when it is not the default.
			ServerAddress = new Uri(url).Authority;
			KCDatabase.Instance.ServerManager.LoadCurrentServer(ServerAddress);
		}
	}
}
