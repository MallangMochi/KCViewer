using System.Linq;
using System.Threading.Tasks;

namespace KCViewer.Observer;

/// <summary>
/// Fork: the API replay (<see cref="Fork.ApiReplay"/>, test profiles only) saves its traffic in the record database as
/// <see cref="ProcessCapturedApi"/> does, so the sortie, expedition and senka windows have records to show.
/// </summary>
public sealed partial class APIObserver
{
	/// <summary>
	/// Saves one replayed request and response, and returns once the files so far are saved: in play the save is
	/// queued before the response changes anything, so a sortie or expedition record takes the fleet of its time.
	/// </summary>
	public async Task SaveReplayedApiFile(string url, string requestBody, string responseBody)
	{
		await ApiFileService.Value.Add(url.Split("/kcsapi/").Last(), requestBody, responseBody);

		while (ApiFileService.Value.HasPendingFiles)
		{
			await Task.Delay(1);
		}
	}
}
