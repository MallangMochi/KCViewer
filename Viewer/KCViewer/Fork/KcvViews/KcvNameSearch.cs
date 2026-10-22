using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Core.Services;
using KCViewer.Core.Types;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the name boxes of the fork's windows (ship list, ship picker, ship and equipment encyclopedia, equipment list;
/// mockup 20261005-list-search-v2, user decision 2026-10-05): the viewer's own matching (translated name, reading,
/// romaji; <see cref="TransliterationService"/>) and also the game's Japanese name (長門改, 61cm三連装魚雷), which the
/// viewer does not look at while names are translated. The viewer's own windows keep their matching.
/// </summary>
public static class KcvNameSearch
{
	private static TransliterationService Transliteration => field ??= Ioc.Default.GetRequiredService<TransliterationService>();

	public static bool Matches(IShipDataMaster ship, string filter) =>
		ship.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) || Transliteration.Matches(ship, filter);

	public static bool Matches(IEquipmentDataMaster equipment, string filter) =>
		equipment.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) || Transliteration.Matches(equipment, filter);
}
