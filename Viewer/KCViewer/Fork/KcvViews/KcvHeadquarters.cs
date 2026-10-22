using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the game's icons of the headquarters panel (mockup 20261005-headquarters-icons-v4), cut from the game's sprites
/// (read by <see cref="ForkGameAssets"/>, not kept in the build): the port's resource icons (port_skin_1 frames 18-24),
/// the arsenal's burner (arsenal_main 90) and the use item icons (common_itemicons, 75 px frames made 44 px, twice the
/// 22 px they are shown at). On a first start they are empty until their sprites have been fetched.
/// </summary>
public static class KcvHqIcons
{
	private const string Port = @"kcs2\img\port\port_skin_1";
	private const string Arsenal = @"kcs2\img\arsenal\arsenal_main";
	private const string Items = @"kcs2\img\common\common_itemicons";

	private static Dictionary<string, ImageSource?> Icons { get; } = new();

	public static ImageSource? Fuel => Frame(Port, "port_skin_1_18");
	public static ImageSource? Ammo => Frame(Port, "port_skin_1_19");
	public static ImageSource? Steel => Frame(Port, "port_skin_1_20");
	public static ImageSource? Bauxite => Frame(Port, "port_skin_1_21");
	public static ImageSource? InstantRepair => Frame(Port, "port_skin_1_22");
	public static ImageSource? InstantConstruction => Frame(Arsenal, "arsenal_main_90");
	public static ImageSource? DevelopmentMaterial => Frame(Port, "port_skin_1_23");
	public static ImageSource? ImprovementMaterial => Frame(Port, "port_skin_1_24");

	/// <summary>The icon of a use item by its ID (the frame number), or null when the sprite has none.</summary>
	public static ImageSource? UseItem(int id) => Frame(Items, $"common_itemicons_id_{id}", 44);

	private static ImageSource? Frame(string sprite, string frame, int? size = null) => KcvSpriteImages.OnUiThread(() =>
	{
		if (Icons.TryGetValue(frame, out ImageSource? icon)) return icon;

		return Icons[frame] = KcvSpriteImages.Image(sprite, loaded => KcvSpriteImages.Frame(loaded, frame, size));
	});
}

/// <summary>
/// Fork: one item of the headquarters panel: its icon and value, its name and the viewer's tooltip in the tooltip, red
/// at the limit (ships, equipment). Copied from the viewer's item, or made by HeadquartersViewModel.Fork.cs for the
/// three 임의 아이템.
/// </summary>
public sealed class KcvHqItem : ObservableObject
{
	public ImageSource? Icon { get; set; }

	/// <summary>22 for the game's icons; 16 for the viewer's ship and equipment icons, which blur when enlarged.</summary>
	public double IconSize { get; init; } = 22;

	/// <summary>92; 112 for the ship and equipment counts, which run to 「2057/2057」.</summary>
	public double Width { get; init; } = 92;

	public string? Text { get; set; }
	public string? ToolTip { get; set; }
	public bool Visible { get; set; }
	public bool IsLimit { get; set; }
}

/// <summary>Fork: items that keep together on one line of the headquarters panel and wrap as one; hidden when all are.</summary>
public sealed class KcvHqGroup(params KcvHqItem[] items) : ObservableObject
{
	public IReadOnlyList<KcvHqItem> Items { get; } = items;
	public bool Visible { get; private set; }

	public void Refresh() => Visible = Items.Any(i => i.Visible);
}
