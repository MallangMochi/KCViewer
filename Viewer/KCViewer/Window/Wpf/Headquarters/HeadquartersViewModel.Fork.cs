using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Fork;
using KCViewer.Fork.KcvViews;
using KCViewer.Utility;

namespace KCViewer.Window.Wpf.Headquarters;

/// <summary>
/// Fork: what the KanColleViewer headquarters panel (Fork/KcvViews/KcvHeadquartersView.xaml, mockup
/// 20261005-headquarters-icons-v4, user decisions 2026-10-05) shows: the viewer's items as icons with their values,
/// the names in the tooltips, in groups that wrap as one (ships and equipment, resources, materials, coins and three
/// 임의 아이템), and its context menu turning each item on or off. The items' visibility is the viewer's own setting
/// (settings window, headquarters), so either place changes both; 임의 아이템 1 is the viewer's DisplayUseItemID, 2 and 3
/// are kept in Settings\Fork.json.
/// </summary>
public partial class HeadquartersViewModel
{
	// The use items the viewer's settings window offers as 임의 아이템 (ConfigurationHeadquartersViewModel.IgnoredItems)
	private static readonly UseItemId[] ForkIgnoredItems =
	[
		UseItemId.InstantRepair,
		UseItemId.InstantConstruction,
		UseItemId.DevelopmentMaterial,
		UseItemId.ImproveMaterial,
		UseItemId.RepairTeam,
		UseItemId.RepairGoddess,
		UseItemId.CombatRation,
		UseItemId.UnderwayReplenishment,
		UseItemId.CannedSaury,
	];

	/// <summary>The viewer's visibility index of 임의 아이템 1 (DisplayUseItem).</summary>
	private const int ForkOtherItemIndex = 14;

	public KcvHqItem ForkShipCount { get; } = new() { IconSize = 16, Width = 112 };
	public KcvHqItem ForkEquipmentCount { get; } = new() { IconSize = 16, Width = 112 };
	public KcvHqItem ForkFuel { get; } = new() { Icon = KcvHqIcons.Fuel };
	public KcvHqItem ForkAmmo { get; } = new() { Icon = KcvHqIcons.Ammo };
	public KcvHqItem ForkSteel { get; } = new() { Icon = KcvHqIcons.Steel };
	public KcvHqItem ForkBauxite { get; } = new() { Icon = KcvHqIcons.Bauxite };
	public KcvHqItem ForkInstantRepair { get; } = new() { Icon = KcvHqIcons.InstantRepair };
	public KcvHqItem ForkInstantConstruction { get; } = new() { Icon = KcvHqIcons.InstantConstruction };
	public KcvHqItem ForkDevelopmentMaterial { get; } = new() { Icon = KcvHqIcons.DevelopmentMaterial };
	public KcvHqItem ForkImprovementMaterial { get; } = new() { Icon = KcvHqIcons.ImprovementMaterial };
	public KcvHqItem ForkFurnitureCoin { get; } = new() { Icon = KcvHqIcons.UseItem((int)UseItemId.FurnitureCoin) };
	public IReadOnlyList<KcvHqItem> ForkOtherItems { get; } = [new(), new(), new()];

	// Set in ForkInitialize (the groups hold the items above)
	public KcvHqGroup ForkCounts { get; private set; } = null!;
	public KcvHqGroup ForkResources { get; private set; } = null!;
	public KcvHqGroup ForkMaterials { get; private set; } = null!;
	public KcvHqGroup ForkOwned { get; private set; } = null!;

	/// <summary>The admiral's group (name and comment, headquarters level), and its name column.</summary>
	public bool ForkShowAdmiral { get; private set; }
	public bool ForkShowNameColumn { get; private set; }
	public bool ForkShowSenka { get; private set; }

	/// <summary>The use items offered as 임의 아이템, by ID, as the viewer's settings window offers them.</summary>
	public static IReadOnlyList<IUseItemMaster> ForkItemChoices => KCDatabase.Instance.MasterUseItems.Values
		.Where(i => i.NameTranslated.Length > 0 && i.Description.Length > 0 && !ForkIgnoredItems.Contains(i.ItemID))
		.OrderBy(i => i.ID)
		.ToList();

	/// <summary>Called at the end of the constructor.</summary>
	private void ForkInitialize()
	{
		ForkShipCount.Icon = Application.Current.TryFindResource("Headquarters.Ship") as ImageSource;
		ForkEquipmentCount.Icon = Application.Current.TryFindResource("Headquarters.Equipment") as ImageSource;

		ForkCounts = new(ForkShipCount, ForkEquipmentCount);
		ForkResources = new(ForkFuel, ForkAmmo, ForkSteel, ForkBauxite);
		ForkMaterials = new(ForkInstantRepair, ForkInstantConstruction, ForkDevelopmentMaterial, ForkImprovementMaterial);
		ForkOwned = new([ForkFurnitureCoin, .. ForkOtherItems]);

		// After the viewer's handler (subscribed first), which sets the items' visibility
		Configuration.Instance.ConfigurationChanged += ForkSync;
		ForkSync();
	}

	/// <summary>Copies the viewer's items (end of Updated, after a configuration change and a menu click).</summary>
	private void ForkSync()
	{
		Copy(ForkShipCount, ShipCount, HeadquartersResources.ItemNameShipSlots);
		Copy(ForkEquipmentCount, EquipmentCount, HeadquartersResources.ItemNameEquipmentSlots);
		Copy(ForkFuel, Fuel, HeadquartersResources.ItemNameFuel);
		Copy(ForkAmmo, Ammo, HeadquartersResources.ItemNameAmmo);
		Copy(ForkSteel, Steel, HeadquartersResources.ItemNameSteel);
		Copy(ForkBauxite, Bauxite, HeadquartersResources.ItemNameBauxite);
		Copy(ForkInstantRepair, InstantRepair, HeadquartersResources.ItemNameInstantRepair);
		Copy(ForkInstantConstruction, InstantConstruction, HeadquartersResources.ItemNameInstantConstruction);
		Copy(ForkDevelopmentMaterial, DevelopmentMaterial, HeadquartersResources.ItemNameDevelopmentMaterial);
		Copy(ForkImprovementMaterial, ModdingMaterial, HeadquartersResources.ItemNameImproveMaterial);
		Copy(ForkFurnitureCoin, FurnitureCoin, HeadquartersResources.ItemNameFurnitureCoin);

		for (int slot = 0; slot < ForkOtherItems.Count; slot++)
		{
			ForkSetOtherItem(slot);
		}

		ForkCounts.Refresh();
		ForkResources.Refresh();
		ForkMaterials.Refresh();
		ForkOwned.Refresh();

		ForkShowNameColumn = AdmiralName.Visible || AdmiralComment.Visible;
		ForkShowAdmiral = ForkShowNameColumn || HQLevel.Visible;
		ForkShowSenka = SenkaSession.Visible || SenkaDay.Visible || SenkaMonth.Visible;
	}

	private static void Copy(KcvHqItem to, HeadquarterItemViewModel from, string name)
	{
		to.Text = from.Text;
		to.Visible = from.Visible;
		to.IsLimit = from.Tag;
		to.ToolTip = Named(name, from.ToolTip);
	}

	private static string Named(string name, string? toolTip) =>
		string.IsNullOrWhiteSpace(toolTip) ? name : name + "\n" + toolTip.Trim();

	/// <summary>임의 아이템 1, 2, 3 (as the menu and the tooltips call them).</summary>
	public static string ForkOtherItemName(int slot) => $"{HeadquartersResources.ItemNameOtherItem} {slot + 1}";

	public int ForkOtherItemId(int slot) => slot switch
	{
		0 => Configuration.Config.FormHeadquarters.DisplayUseItemID,
		1 => ForkSettings.Current.KcvStyle.HeadquartersItem2,
		_ => ForkSettings.Current.KcvStyle.HeadquartersItem3,
	};

	public bool ForkOtherItemShown(int slot) => slot switch
	{
		0 => ForkIsShown(ForkOtherItemIndex),
		1 => ForkSettings.Current.KcvStyle.HeadquartersShowItem2 && ForkOtherItemId(1) > 0,
		_ => ForkSettings.Current.KcvStyle.HeadquartersShowItem3 && ForkOtherItemId(2) > 0,
	};

	public bool ForkIsShown(int index)
	{
		CheckVisibilityConfiguration();
		return Configuration.Config.FormHeadquarters.Visibility.List[index];
	}

	/// <summary>A menu click on an item: the viewer's visibility setting, then the viewer's own handler.</summary>
	public void ForkSetShown(int index, bool shown)
	{
		CheckVisibilityConfiguration();
		Configuration.Config.FormHeadquarters.Visibility.List[index] = shown;
		ConfigurationChanged();
		ForkSync();
	}

	public void ForkSetOtherItemShown(int slot, bool shown)
	{
		if (slot is 0)
		{
			ForkSetShown(ForkOtherItemIndex, shown);
			return;
		}

		if (slot is 1) ForkSettings.Current.KcvStyle.HeadquartersShowItem2 = shown;
		else ForkSettings.Current.KcvStyle.HeadquartersShowItem3 = shown;
		ForkSettings.Current.Save();
		ForkSync();
	}

	/// <summary>A menu click on an item of a 임의 아이템's list: that item, shown.</summary>
	public void ForkChooseOtherItem(int slot, int id)
	{
		if (slot is 0)
		{
			Configuration.Config.FormHeadquarters.DisplayUseItemID = id;
			ForkSetShown(ForkOtherItemIndex, true);
			return;
		}

		if (slot is 1)
		{
			ForkSettings.Current.KcvStyle.HeadquartersItem2 = id;
			ForkSettings.Current.KcvStyle.HeadquartersShowItem2 = true;
		}
		else
		{
			ForkSettings.Current.KcvStyle.HeadquartersItem3 = id;
			ForkSettings.Current.KcvStyle.HeadquartersShowItem3 = true;
		}

		ForkSettings.Current.Save();
		ForkSync();
	}

	// As the viewer's UpdateDisplayUseItem: the count, and the tooltip with the rice set or the saury and sardine
	// together; by ID, and with the menu as the place to change the item
	private void ForkSetOtherItem(int slot)
	{
		KcvHqItem item = ForkOtherItems[slot];
		KCDatabase db = KCDatabase.Instance;
		int id = ForkOtherItemId(slot);
		IUseItemMaster? master = id > 0 ? db.MasterUseItems[id] : null;

		item.Visible = ForkOtherItemShown(slot);
		item.Icon = KcvHqIcons.UseItem(id);

		int Count(int itemId) => db.UseItems[itemId]?.Count ?? 0;

		string body = master switch
		{
			null => string.Format(FormHeadquarters.UnknownItem, id),
			_ => id switch
			{
				85 or 86 or 87 or 88 => $"{FormHeadquarters.Rice}: {Count(85)}\n{FormHeadquarters.Umeboshi}: {Count(86)}\n" +
					$"{FormHeadquarters.Nori}: {Count(87)}\n{FormHeadquarters.Tea}: {Count(88)}",
				68 or 93 => $"{FormHeadquarters.Sanma}: {Count(68)}\n{FormHeadquarters.Iwashi}: {Count(93)}",
				_ => master.NameTranslated,
			},
		};

		item.Text = master is null ? "???" : Count(id).ToString();
		item.ToolTip = $"{ForkOtherItemName(slot)}\n{body}\n{ForkStrings.KcvHqItemHint}";
	}
}
