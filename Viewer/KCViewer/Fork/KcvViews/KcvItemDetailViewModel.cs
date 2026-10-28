using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using KCViewer.Common;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Utility;

namespace KCViewer.Fork.KcvViews;

/// <summary>An item of the list: the game's icon, the name and how many there are.</summary>
public sealed record KcvItemDetailRow(int Id, ImageSource? Icon, string Name, int Count);

/// <summary>
/// Fork: the items the admiral has (mockup 20261024-item-detail-v2, approved "구현"), in item ID order, those with one or
/// more only. The use items of the last useitem / require_info answer, and the materials and furniture coins, which the
/// game gives with the port and material answers rather than in the item list but shows on its item screen too; not the
/// four resources (the headquarters panel has them). Names as the headquarters panel's, else the game's.
/// </summary>
public sealed class KcvItemDetailViewModel : WindowViewModelBase
{
	public string Title => ForkStrings.KcvItemDetail;

	public List<KcvItemDetailRow> Rows { get; private set; } = [];

	public KcvItemDetailViewModel()
	{
		Update();
	}

	public override void Loaded()
	{
		base.Loaded();

		// items and materials change with many answers (port, quests, item use, development, repairs); the viewer's
		// 1 s timer covers them all, and the list is made again only when a count changed
		SystemEvents.UpdateTimerTick += Update;
	}

	public override void Closed()
	{
		base.Closed();

		SystemEvents.UpdateTimerTick -= Update;
	}

	private void Update()
	{
		KCDatabase db = KCDatabase.Instance;

		List<(int Id, int Count)> materials =
		[
			((int)UseItemId.InstantRepair, db.Material.InstantRepair),
			((int)UseItemId.InstantConstruction, db.Material.InstantConstruction),
			((int)UseItemId.DevelopmentMaterial, db.Material.DevelopmentMaterial),
			((int)UseItemId.ImproveMaterial, db.Material.ModdingMaterial),
			((int)UseItemId.FurnitureCoin, db.Admiral.IsAvailable ? db.Admiral.FurnitureCoin : 0),
		];
		IEnumerable<(int Id, int Count)> items = db.UseItems.Values
			.Where(item => materials.All(m => m.Id != item.ItemID))
			.Select(item => (item.ItemID, item.Count));

		List<(int Id, int Count)> owned = materials.Concat(items).Where(c => c.Count > 0).OrderBy(c => c.Id).ToList();
		if (owned.SequenceEqual(Rows.Select(r => (r.Id, r.Count)))) return;

		Rows = owned.Select(c => new KcvItemDetailRow(c.Id, KcvHqIcons.UseItem(c.Id), NameOf(c.Id), c.Count)).ToList();
	}

	private static string NameOf(int id)
	{
		if (KCDatabase.Instance.MasterUseItems[id] is not { } master) return ((UseItemId)id).NameTranslated();

		// an item the viewer knows by ID but has no name for comes back as ConstantsRes.Unknown
		string name = master.NameTranslated;
		return name == ConstantsRes.Unknown ? master.Name : name;
	}
}
