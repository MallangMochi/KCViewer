using System.Windows.Media;
using KCViewer.Common;
using KCViewer.Resource;

namespace KCViewer.ViewModels;

public class AnchorableViewModel : UserControlViewModelBase
{
	// Fork: panels of the test instance get a "(test)" suffix.
	private string _title = "";
	public string Title { get => _title; set => _title = Fork.ForkProfile.DecorateTitle(value); }
	public virtual string ContentId { get; }
	public IconContent? Icon { get; set; }

	/// <summary>
	/// This is needed for the window capture feature.
	/// For known icons, the <see cref="Icon"/> property should be used.
	/// </summary>
	public ImageSource? IconSource { get; set; }

	public bool CanFloat { get; set; }

	protected AnchorableViewModel(string title, string contentId, IconContent? icon = null)
	{
		Title = title;
		ContentId = contentId;
		Icon = icon;
	}
}
