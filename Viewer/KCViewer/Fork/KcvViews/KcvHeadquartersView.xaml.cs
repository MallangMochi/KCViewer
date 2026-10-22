using System;
using System.Windows.Controls;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Window.Wpf.Headquarters;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: KanColleViewer's header design for the headquarters panel (see the XAML). Its context menu is built each time
/// it opens (mockup 20261005-headquarters-icons-v4): the viewer's 18 items in the panel's groups, each checked while
/// shown, and the three 임의 아이템 as submenus with 표시 and the items to choose from.
/// </summary>
public partial class KcvHeadquartersView : UserControl
{
	// The viewer's visibility indices (HeadquartersItem) in the menu's groups; -1, -2, -3 are 임의 아이템 1, 2, 3
	private static readonly int[][] Groups =
	[
		[0, 1, 2, 3, 4],
		[10, 11, 12, 13],
		[5, 6, 7, 8],
		[9, -1, -2, -3],
		[15, 16, 17],
	];

	public KcvHeadquartersView()
	{
		InitializeComponent();

		ContextMenu = new ContextMenu();
	}

	private static string ItemName(int index) => index switch
	{
		0 => HeadquartersResources.ItemNameName,
		1 => HeadquartersResources.ItemNameComment,
		2 => HeadquartersResources.ItemNameHQLevel,
		3 => HeadquartersResources.ItemNameShipSlots,
		4 => HeadquartersResources.ItemNameEquipmentSlots,
		5 => HeadquartersResources.ItemNameInstantRepair,
		6 => HeadquartersResources.ItemNameInstantConstruction,
		7 => HeadquartersResources.ItemNameDevelopmentMaterial,
		8 => HeadquartersResources.ItemNameImproveMaterial,
		9 => HeadquartersResources.ItemNameFurnitureCoin,
		10 => HeadquartersResources.ItemNameFuel,
		11 => HeadquartersResources.ItemNameAmmo,
		12 => HeadquartersResources.ItemNameSteel,
		13 => HeadquartersResources.ItemNameBauxite,
		15 => HeadquartersResources.SenkaSession,
		16 => HeadquartersResources.SenkaDay,
		17 => HeadquartersResources.SenkaMonth,
		_ => throw new ArgumentOutOfRangeException(nameof(index)),
	};

	private void BuildMenu(object sender, ContextMenuEventArgs e)
	{
		if (DataContext is not HeadquartersViewModel viewModel || ContextMenu is null)
		{
			e.Handled = true;
			return;
		}

		ContextMenu.Items.Clear();
		foreach (int[] group in Groups)
		{
			if (ContextMenu.Items.Count > 0) ContextMenu.Items.Add(new Separator());

			foreach (int index in group)
			{
				ContextMenu.Items.Add(index >= 0 ? Toggle(viewModel, index) : OtherItem(viewModel, -index - 1));
			}
		}
	}

	private static MenuItem Toggle(HeadquartersViewModel viewModel, int index)
	{
		bool shown = viewModel.ForkIsShown(index);
		// Checkable, so the check mark is drawn (the menu is built anew each time, so its own toggling does not matter)
		MenuItem item = new() { Header = ItemName(index), IsCheckable = true, IsChecked = shown };
		item.Click += (_, _) => viewModel.ForkSetShown(index, !shown);
		return item;
	}

	// 임의 아이템 n (the chosen item): 표시, then the items, the chosen one checked; choosing one shows it
	private static MenuItem OtherItem(HeadquartersViewModel viewModel, int slot)
	{
		int id = viewModel.ForkOtherItemId(slot);
		bool shown = viewModel.ForkOtherItemShown(slot);
		// The header says the chosen item and, while it is off, 숨김: a check mark as an icon stood out of line with the
		// others, and a checkable item does not open its submenu
		string chosen = id > 0 && KCDatabase.Instance.MasterUseItems[id] is { } master
			? shown ? master.NameTranslated : $"{master.NameTranslated}, {ForkStrings.KcvHqHidden}"
			: ForkStrings.KcvHqNoItem;

		MenuItem parent = new() { Header = $"{HeadquartersViewModel.ForkOtherItemName(slot)} ({chosen})" };

		MenuItem show = new() { Header = ForkStrings.KcvShow, IsCheckable = true, IsChecked = shown, IsEnabled = id > 0 };
		show.Click += (_, _) => viewModel.ForkSetOtherItemShown(slot, !shown);
		parent.Items.Add(show);
		parent.Items.Add(new Separator());

		foreach (IUseItemMaster choice in HeadquartersViewModel.ForkItemChoices)
		{
			int choiceId = choice.ID;
			MenuItem item = new() { Header = choice.NameTranslated, IsCheckable = true, IsChecked = choiceId == id };
			item.Click += (_, _) => viewModel.ForkChooseOtherItem(slot, choiceId);
			parent.Items.Add(item);
		}

		return parent;
	}
}
