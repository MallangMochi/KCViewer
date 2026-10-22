namespace KCViewer.Fork;

/// <summary>
/// Fork: text of the ship and equipment albums in KanColleViewer's style (Fork/KcvViews/KcvAlbum*, mockup v5,
/// VIEWER-PLAN.md "도감 묶음"). The labels the viewer already has keep coming from its own resources; these are the new ones.
/// </summary>
public static partial class ForkStrings
{
	public static string KcvAlbumShipName => T("함명", "艦名", "Name");
	public static string KcvAlbumEquipmentName => T("장비명", "装備名", "Name");
	public static string KcvAlbumCategory => T("분류", "分類", "Category");
	public static string KcvAlbumNumber => T("도감", "図鑑", "No.");
	public static string KcvAlbumStats => T("능력치", "ステータス", "Stats");
	public static string KcvAlbumInitial => T("초기", "初期", "Base");
	public static string KcvAlbumMaximum => T("최대", "最大", "Max");
	public static string KcvAlbumRange => T("사거리", "射程", "Range");
	public static string KcvAlbumSlots => T("장비", "装備", "Equipment");
	public static string KcvAlbumAircraft => T("탑재", "搭載", "Aircraft");
	public static string KcvAlbumDescription => T("도감 설명", "図鑑説明", "Description");
	public static string KcvAlbumSwitchMessage => T("누르면 입수 대사와 바꿈", "クリックで入手時の台詞と切り替え", "Click to switch to the get message");
	public static string KcvAlbumInitialShips => T("초기 장비함", "初期装備艦", "Initially equipped on");
	public static string KcvAlbumRightClickShip => T("우클릭 함선 도감", "右クリックで艦船図鑑", "Right-click: ship album");
	public static string KcvAlbumRightClickEquipment => T("우클릭 장비 도감", "右クリックで装備図鑑", "Right-click: equipment album");
	public static string KcvAlbumUpgrade => T("개수", "改修", "Improvement");
	public static string KcvAlbumUpgradeResources => T("자재", "資材", "Resources");
	public static string KcvAlbumDevelopmentMaterial => T("개발 자재", "開発資材", "Dev. mat.");
	public static string KcvAlbumImprovementMaterial => T("개수 자재", "改修資材", "Imp. mat.");
	public static string KcvAlbumConversion => T("갱신", "更新", "Conversion");
	public static string KcvAlbumOwned => T("소지", "所持", "owned");

	// the status bar: the rows the search shows
	public static string KcvAlbumShipCount(int count) => T($"함선 {count:N0}척", $"艦船 {count:N0}隻", $"{count:N0} ships");
	public static string KcvAlbumEquipmentCount(int count) => T($"장비 {count:N0}개", $"装備 {count:N0}個", $"{count:N0} equipment");
}
