using KCViewer.Core.Types;
using KCViewer.Fork.KcvViews;

namespace KCViewer.Window.Tools.DialogAlbumMasterShip;

/// <summary>
/// Fork: the KanColleViewer style album (Fork/KcvViews/KcvAlbumShipWindow) also finds ships by their Japanese name
/// (<see cref="KcvNameSearch"/>, mockup 20261005-list-search-v2); the viewer's own album keeps its search.
/// </summary>
public partial class DialogAlbumMasterShipViewModel
{
	/// <summary>Set by the KanColleViewer style window.</summary>
	public bool ForkJapaneseNameSearch { get; set; }

	private bool ForkMatches(IShipDataMaster ship, string filter) => ForkJapaneseNameSearch
		? KcvNameSearch.Matches(ship, filter)
		: TransliterationService.Matches(ship, filter);
}
