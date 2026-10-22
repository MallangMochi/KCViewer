using System.Diagnostics.CodeAnalysis;
using KCViewer.Avalonia.Services;
using KCViewer.Core.Services;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Data;
using KCViewer.Core.Types.Mocks;

namespace KCViewer.Avalonia.Dialogs.ShipSelector;

/// <summary>
/// Needed because the ships get loaded after the trackers get initialized.
/// </summary>
public class ShipSelectorFactory(IKCDatabase db, TransliterationService transliterationService,
	ImageLoadService imageLoadService)
{
	private IKCDatabase Db { get; } = db;
	private TransliterationService TransliterationService { get; } = transliterationService;
	private ImageLoadService ImageLoadService { get; } = imageLoadService;

	[field: MaybeNull]
	public ShipSelectorViewModel QuestTrackerManager => field ??= CreateQuestTrackerManagerShipSelectorViewModel();

	private ShipSelectorViewModel CreateQuestTrackerManagerShipSelectorViewModel()
	{
		List<IShipData> ships = Db.MasterShips.Values
			.Select(s => new ShipDataMock(s))
			.OfType<IShipData>()
			.ToList();

		return new(TransliterationService, ImageLoadService, ships)
		{
			ShipFilter = { FinalRemodel = false },
		};
	}

	[field: MaybeNull]
	public ShipSelectorViewModel ConfigurationBehavior => field ??= CreateConfigurationBehaviorShipSelectorViewModel();

	private ShipSelectorViewModel CreateConfigurationBehaviorShipSelectorViewModel()
	{
		List<IShipData> ships = Db.MasterShips.Values
			.Select(s => new ShipDataMock(s))
			.OfType<IShipData>()
			.ToList();

		return new(TransliterationService, ImageLoadService, ships);
	}
}
