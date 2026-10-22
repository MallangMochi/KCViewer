using Avalonia;
using Avalonia.Media.Imaging;
using KCViewer.Core.Types;

namespace KCViewer.Avalonia.Services;

public class GameResourceHelper(IConfigurationConnection connection)
{
	private IConfigurationConnection Connection { get; } = connection;

	public static Size ShipNameSize { get; } = new(654, 95);
	public static Size ShipBannerSize { get; } = new(240, 60);
	public static Size ShipCardSize { get; } = new(327, 450);
	public static Size ShipAlbumSize { get; } = new(431, 645);
	public static Size ShipCutinSize { get; } = new(998, 182);
	public static Size ShipSupplySize { get; } = new(711, 71);

	public static Size EquipmentCardSize { get; } = new(390, 390);
	public static Size EquipmentCardSmallSize { get; } = new(160, 160);
	public static Size EquipmentAlbumSize { get; } = new(430, 645);
	public static Size EquipmentSpecSize { get; } = new(301, 183);
	public static Size EquipmentNameSize { get; } = new(654, 94);


	public static string ResourceTypeShipName { get; } = "album_status";
	public static string ResourceTypeShipBanner { get; } = "banner";
	public static string ResourceTypeShipCard { get; } = "card";
	public static string ResourceTypeShipAlbumFull { get; } = "character_full";
	public static string ResourceTypeShipAlbumZoom { get; } = "character_up";
	public static string ResourceTypeShipFull { get; } = "full";
	public static string ResourceTypeShipCutin { get; } = "remodel";
	public static string ResourceTypeShipSupply { get; } = "supply_character";

	public static string ResourceTypeEquipmentText { get; } = "btxt_flat";
	public static string ResourceTypeEquipmentCard { get; } = "card";
	public static string ResourceTypeEquipmentCardSmall { get; } = "card_t";
	public static string ResourceTypeEquipmentFairy { get; } = "item_character";
	public static string ResourceTypeEquipmentAlbum { get; } = "item_on";
	public static string ResourceTypeEquipmentWeapon { get; } = "item_up";
	public static string ResourceTypeEquipmentSpec { get; } = "remodel";
	public static string ResourceTypeEquipmentName { get; } = "statustop_item";

	/// <summary>
	/// 艦船画像リソースへのパスを取得します。
	/// </summary>
	/// <param name="shipId">艦船ID。</param>
	/// <param name="isDamaged">中破グラフィックかどうか。</param>
	/// <param name="resourceType">画像種別。["album_status", "banner", "card", "character_full", "character_up", "full", "remodel", "supply_character"]</param>
	/// <returns></returns>
	private static string GetShipResourcePath(ShipId shipId, bool isDamaged, string resourceType)
	{
		if (resourceType is "album_status")
		{
			isDamaged = false;
		}

		if (isDamaged)
		{
			resourceType += "_dmg";
		}

		return $@"kcs2\resources\ship\{resourceType}\{(int)shipId:d4}_";
	}

	/// <summary>
	/// 装備画像リソースへのパスを取得します。
	/// </summary>
	/// <param name="equipmentId">装備ID。</param>
	/// <param name="resourceType">画像種別。["btxt_flat", "card", "card_t", "item_character", "item_on", "item_up", "remodel", "statustop_item"]</param>
	/// <param name="useOldFormat">Old format used 3 digits for equipment id, new one uses 4 digits.</param>
	/// <returns></returns>
	private static string GetEquipmentResourcePath(EquipmentId equipmentId, string resourceType, bool useOldFormat = false)
	{
		return useOldFormat switch
		{
			true => $@"kcs2\resources\slot\{resourceType}\{(int)equipmentId:d3}_",
			_ => $@"kcs2\resources\slot\{resourceType}\{(int)equipmentId:d4}_",
		};
	}

	/// <summary>
	/// 最新のリソースへのパスを取得します。
	/// </summary>
	public string? GetLatestResourcePath(string resourcePath)
		// Fork: the local server's assets folder first, then the viewer's own folder, then the cache
		=> (ForkAssetsFolder is { } assets ? GetLatestResourcePath(assets, resourcePath) : null)
		?? (ForkGameAssetsFolder is { } own ? GetLatestResourcePath(own, resourcePath) : null)
		?? GetLatestResourcePath(Connection.SaveDataPath, resourcePath);

	/// <summary>
	/// Fork: a folder laid out like the game's paths that is searched before SaveDataPath: the local server's assets
	/// folder, set by the viewer (KCViewer/Fork/ForkAssets.cs). The fork does not run the proxy that filled
	/// the cache.
	/// </summary>
	public static string? ForkAssetsFolder { get; set; }

	/// <summary>
	/// Fork: the viewer's own folder for the game's files it fetched (<c>&lt;exe folder&gt;\GameAssets</c>), searched
	/// after <see cref="ForkAssetsFolder"/>; set by the viewer (KCViewer/Fork/ForkGameAssets.cs).
	/// </summary>
	public static string? ForkGameAssetsFolder { get; set; }

	/// <summary>
	/// Fork: fetches a missing game file (a path in the game's layout) from the official server into
	/// <see cref="ForkGameAssetsFolder"/>; true when it is there afterwards. Used by <see cref="GameAssetDownloaderService"/>
	/// instead of the viewer's mirror; set by the viewer (KCViewer/Fork/ForkGameAssets.cs).
	/// </summary>
	public static Func<string, Task<bool>>? ForkFetch { get; set; }

	private static string? GetLatestResourcePath(string rootFolder, string resourcePath)
	{
		string root = Path.Join(rootFolder, Path.GetDirectoryName(resourcePath));

		try
		{
			if (!Directory.Exists(root)) return null;

			return Directory
				.EnumerateFiles(root, Path.GetFileNameWithoutExtension(resourcePath) + "*.png", SearchOption.TopDirectoryOnly)
				.OrderBy(File.GetLastWriteTime)
				.LastOrDefault();
		}
		catch (Exception)
		{
			return null;
		}
	}


	public string? GetShipImagePath(ShipId shipId, bool isDamaged, string resourceType)
		=> GetLatestResourcePath(GetShipResourcePath(shipId, isDamaged, resourceType));

	public string? GetEquipmentImagePath(EquipmentId equipmentId, string resourceType)
		=> GetLatestResourcePath(GetEquipmentResourcePath(equipmentId, resourceType))
		?? GetLatestResourcePath(GetEquipmentResourcePath(equipmentId, resourceType, true));

	public bool HasShipImage(ShipId shipId, bool isDamaged, string resourceType)
		=> GetShipImagePath(shipId, isDamaged, resourceType) != null;

	public bool HasEquipmentImage(EquipmentId equipmentId, string resourceType)
		=> GetEquipmentImagePath(equipmentId, resourceType) != null;

	/// <summary>
	/// 艦船の画像を読み込みます。
	/// </summary>
	/// <param name="shipId">艦船ID。</param>
	/// <param name="isDamaged">中破しているかどうか。</param>
	/// <param name="resourceType">画像種別。同クラスの定数を使用します。</param>
	/// <returns>成功した場合は艦船画像。失敗した場合は null。</returns>
	public Bitmap? LoadShipImage(ShipId shipId, bool isDamaged, string resourceType)
	{
		string resourcePath = GetShipResourcePath(shipId, isDamaged, resourceType);
		string? realpath = GetLatestResourcePath(resourcePath);

		return realpath switch
		{
			null => null,
			_ => new Bitmap(realpath),
		};
	}
}
