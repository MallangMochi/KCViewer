using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using ModernWpf;

namespace Browser.WebView2Browser;

/// <summary>
/// Fork: the local skin (LocalSkin.cs) and the KanColleViewer style's accent, kept out of WebView2ViewModel.cs so
/// upstream merges stay simple.
/// </summary>
public partial class WebView2ViewModel
{
	private LocalSkin? LocalSkin { get; set; }

	private Color? OriginalAccentColor { get; set; }
	private bool OriginalAccentColorSaved { get; set; }

	/// <summary>Zoom menu item (BrowserView.xaml); bound, as the window is built before the culture is set.</summary>
	public string ForkZoomClassicHeader => ForkBrowserStrings.ZoomClassic;

	/// <summary>Whether the host's KanColleViewer style is on (it sends an accent colour then).</summary>
	public bool ForkKcvStyle { get; private set; }

	/// <summary>The tool strip's background in the style: the window colour of the style's panels (Kcv.Background).</summary>
	public Brush ForkKcvBackground { get; private set; } = Brushes.Transparent;

	/// <summary>The zoom in use, next to the zoom icon in the style, as KanColleViewer shows it by its zoom button.</summary>
	public string ForkZoomText { get; private set; } = "";
	public Visibility ForkZoomTextVisibility => ForkKcvStyle ? Visibility.Visible : Visibility.Collapsed;

	// The tool strip's tooltips in the style: the viewer has the texts but no tooltips (null leaves none)
	public string? ForkTipScreenshot => ForkKcvStyle ? FormBrowser.Strip_ScreenShot : null;
	public string? ForkTipZoom => ForkKcvStyle ? FormBrowser.Strip_Zoom : null;
	public string? ForkTipMute => ForkKcvStyle ? FormBrowser.Strip_Mute : null;
	public string? ForkTipRefresh => ForkKcvStyle ? FormBrowser.Strip_Refresh : null;
	public string? ForkTipLogin => ForkKcvStyle ? ForkBrowserStrings.LoginPageTip : null;
	public string? ForkTipOther => ForkKcvStyle ? ForkBrowserStrings.OtherMenuTip : null;

	/// <summary>Called by ApplyZoom with the zoom it set.</summary>
	private void OnForkZoomApplied(double zoomFactor) => ForkZoomText = $"{Math.Round(zoomFactor * 100)}%";

	/// <summary>Before the first navigation, so the game's first requests can already be answered.</summary>
	private async Task StartForkFeaturesAsync(CoreWebView2Environment environment)
	{
		LocalSkin = new LocalSkin(WebView2!.CoreWebView2, environment, AddLog);
		if (ApiCapture is not null) ApiCapture.IsAnsweredLocally = LocalSkin.TakeAnswered;
		await OnForkConfigurationChangedAsync();
	}

	/// <summary>
	/// The host calls ConfigurationChanged when its configuration changes, when the skin is turned on or off or
	/// reloaded, and when the KanColleViewer style changes.
	/// </summary>
	protected override async Task OnForkConfigurationChangedAsync()
	{
		await ApplyLocalSkinAsync();
		await ApplyKcvAccentAsync();
		await ApplyScreenshotHotkeyAsync();
	}

	private async Task ApplyScreenshotHotkeyAsync()
	{
		try
		{
			await UpdateForkScreenshotHotkeyAsync();
		}
		catch (Exception ex)
		{
			AddLog(3, ForkBrowserStrings.ScreenshotHotkeyError(ex.Message));
		}
	}

	private async Task ApplyLocalSkinAsync()
	{
		if (LocalSkin is not { } skin) return;

		try
		{
			string? folder = await BrowserHost.LocalSkinFolder();
			// The WebView2 filters must be changed on the UI thread.
			await App.Current.Dispatcher.InvokeAsync(() => skin.ApplyAsync(folder)).Task.Unwrap();
		}
		catch (Exception ex)
		{
			AddLog(3, ForkBrowserStrings.LocalSkinError(ex.Message));
		}
	}

	private async Task ApplyKcvAccentAsync()
	{
		try
		{
			string? accent = await BrowserHost.KcvAccentColor();
			// asked here: the base class sets the theme in a later dispatcher call
			bool dark = (ApplicationTheme)await BrowserHost.GetTheme() == ApplicationTheme.Dark;
			await App.Current.Dispatcher.InvokeAsync(() =>
			{
				ForkKcvStyle = accent is not null;
				SolidColorBrush background = new(dark ? Color.FromRgb(0x2D, 0x2D, 0x30) : Color.FromRgb(0xEF, 0xEF, 0xF2));
				background.Freeze();
				ForkKcvBackground = background;

				if (!OriginalAccentColorSaved)
				{
					OriginalAccentColor = ThemeManager.Current.AccentColor;
					OriginalAccentColorSaved = true;
				}

				ThemeManager.Current.AccentColor = accent is null ? OriginalAccentColor : (Color)ColorConverter.ConvertFromString(accent);
			});
		}
		catch (Exception ex)
		{
			AddLog(3, ForkBrowserStrings.KcvStyleError(ex.Message));
		}
	}
}
