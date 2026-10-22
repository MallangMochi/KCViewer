using KCViewer.Fork;

namespace KCViewer.Window.Settings.Behavior;

/// <summary>
/// Fork: the translations' own address (<see cref="ForkSettings.TranslationSettings"/>) beside EO's data address, in the
/// fork's own settings (mockup 20261007-translation-url-v2).
/// </summary>
public partial class ConfigurationBehaviorViewModel
{
	public string ForkTranslationUrl { get; set; } = ForkSettings.Current.Translation.Url;

	// the start-up update checks (ForkUpdateWatch, mockup 20261007-update-watch-v1)
	public bool ForkWatchGame { get; set; } = ForkSettings.Current.UpdateWatch.Game;
	public bool ForkWatchKc3 { get; set; } = ForkSettings.Current.UpdateWatch.Kc3;
	public string ForkWatchGameLabel => ForkStrings.UpdateWatchGame;
	public string ForkWatchGameToolTip => ForkStrings.UpdateWatchGameToolTip;
	public string ForkWatchKc3Label => ForkStrings.UpdateWatchKc3;
	public string ForkWatchKc3ToolTip => ForkStrings.UpdateWatchKc3ToolTip;

	public string ForkTranslationUrlLabel => ForkStrings.KcvConfigTranslationUrl;
	public string ForkTranslationUrlToolTip => ForkStrings.KcvConfigTranslationUrlToolTip;
	public string ForkDataUrlLabel => ForkStrings.KcvConfigDataUrl;
	public string ForkDataUrlToolTip => ForkStrings.KcvConfigDataUrlToolTip;

	private void ForkSave()
	{
		string url = ForkTranslationUrl.Trim();
		ForkSettings.UpdateWatchSettings watch = ForkSettings.Current.UpdateWatch;
		if (ForkSettings.Current.Translation.Url == url && watch.Game == ForkWatchGame && watch.Kc3 == ForkWatchKc3) return;

		ForkSettings.Current.Translation.Url = url;
		watch.Game = ForkWatchGame;
		watch.Kc3 = ForkWatchKc3;
		ForkSettings.Current.Save();
	}
}
