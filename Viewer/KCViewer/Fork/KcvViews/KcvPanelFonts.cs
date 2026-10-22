using System.Windows;
using System.Windows.Media;
using KCViewer.Common;
using KCViewer.Utility;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: viewer panels that keep their own layout but take KanColleViewer's font (Kcv.FontFamily) while its style is
/// on, for panels whose view binds the view model's <see cref="UserControlViewModelBase.Font"/> (the ranking and the
/// expedition check, user's choice 2026-10-02; the information and log panels bind the window's font instead, see
/// <see cref="KcvFontConverter"/>). The base class sets the viewer's main font at every configuration change; this
/// runs after it and when the style is switched, so the original view files stay as they are.
/// </summary>
public static class KcvPanelFonts
{
	public static void Follow(UserControlViewModelBase viewModel)
	{
		void Apply()
		{
			if (KcvViewState.Current.Enabled && Application.Current?.TryFindResource("Kcv.FontFamily") is FontFamily kcvFont)
			{
				viewModel.Font = kcvFont;
			}
			// the viewer's main font, as the base class sets it
			else if (Configuration.Config.UI.MainFont.FontData?.FontFamily.Name is { } name)
			{
				viewModel.Font = new FontFamily(name);
			}
		}

		Configuration.Instance.ConfigurationChanged += Apply;
		KcvViewState.Current.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName is nameof(KcvViewState.Enabled)) Apply();
		};
		Apply();
	}
}
