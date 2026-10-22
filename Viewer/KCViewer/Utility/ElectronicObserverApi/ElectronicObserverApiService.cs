using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using KCViewer.Data;

namespace KCViewer.Utility.ElectronicObserverApi;

public class ElectronicObserverApiService(ElectronicObserverApiTranslationViewModel translations)
{
	private HttpClient Client { get; } = new()
	{
		DefaultRequestHeaders =
		{
			UserAgent = { new("ElectronicObserverEN", SoftwareInformation.VersionEnglish) },
		},
	};

	private string Url => Configuration.Config.Debug.ElectronicObserverApiUrl switch
	{
		{ Length: > 0 } => Configuration.Config.Debug.ElectronicObserverApiUrl,
		_ => SoftwareUpdater.CurrentDataVersion.AppApiServerUrl,
	};

	private ElectronicObserverApiTranslationViewModel Translations { get; } = translations;

	// Fork: off. These reports (fit bonus, upgrade list and cost mismatches, unhandled exceptions) go to 74식's
	// server with no setting to stop them (user's decision 2026-10-22 "끔"); every reporter and PostJson check this.
	// Upstream: !string.IsNullOrEmpty(Url) && KCDatabase.Instance.ServerManager.CurrentServer is not null
	public bool IsServerAvailable => false;

	public async Task PostJson<T>(string route, T data)
	{
		if (!IsServerAvailable) return;

		try
		{
			HttpResponseMessage response = await Client.PostAsJsonAsync(Path.Combine(Url, route), data);

			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			Utility.Logger.Add(1, string.Format($"{Translations.ElectronicObserverApi} {ex.Message}"));
		}
	}
}
