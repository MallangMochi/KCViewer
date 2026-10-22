using System;
using System.Linq;
using KCViewer.Plugins;

namespace KCViewer.Fork.Plugins;

/// <summary>
/// Fork: every API call from <see cref="Observer.APIObserver.ProcessCapturedApi"/>, for plugins.
/// Raised on the thread that delivered the capture.
/// </summary>
public static class ApiEvents
{
	public static event Action<ApiExchange>? Received;

	public static void Raise(string url, string request, string response)
	{
		Received?.Invoke(new ApiExchange(DateTime.Now, url, url.Split("/kcsapi/").Last(), request, response));
	}
}
