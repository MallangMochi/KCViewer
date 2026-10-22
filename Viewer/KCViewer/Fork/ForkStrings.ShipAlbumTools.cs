namespace KCViewer.Fork;

/// <summary>
/// Fork: the ship album's 수치 편집 and 이미지 뷰어 in KanColleViewer's style (KcvShipAlbumTools, mockup
/// 20261017-album-ship-tools-v2). The labels the album already has (능력치, 초기, 최대, 사거리, 장비, 탑재, 도감 설명) come
/// from ForkStrings.Albums, the viewer's menus and messages from its own resources.
/// </summary>
public static partial class ForkStrings
{
	public static string ShipParamInitialMax => T("초기 상한", "初期上限", "Base max");
	public static string ShipParamStatsNote => T(
		"대잠·회피·색적은 Lv1 값을 하한~상한으로 적습니다(74식 추정 범위).",
		"対潜・回避・索敵はLv1の値を下限～上限で書きます(74式の推定範囲)。",
		"ASW, evasion and LoS hold the Lv1 value as a lower and upper bound (the viewer's estimate).");
	public static string ShipParamSlotsNote => T(
		"장비 칸은 도감 ID, -1은 없음. 이름은 고른 ID의 장비 이름.",
		"装備欄は図鑑ID、-1はなし。名前は入力したIDの装備名。",
		"An equipment is its album ID, -1 for none; the name is that of the ID.");
	public static string ShipParamNone => T("(없음)", "(なし)", "(none)");
	public static string ShipParamUnknown => T("(알 수 없음)", "(不明)", "(unknown)");
	public static string ShipParamMessages => T("대사 · 설명", "台詞・説明", "Messages");
	public static string ShipParamMessageGet => T("입수 대사", "入手時の台詞", "Get message");
	public static string ShipParamMessagesNote => T(
		"줄바꿈은 기록에 게임처럼 <br>로 저장합니다.",
		"改行は記録にゲームと同じく<br>で保存します。",
		"Line breaks are saved as <br>, as the game writes them.");
	public static string ShipParamResources => T("자원", "リソース", "Resources");
	public static string ShipParamResourceName => T("리소스 이름", "リソース名", "Resource name");
	public static string ShipParamGraphicVersion => T("이미지 판", "画像ver", "Image ver.");
	public static string ShipParamVoiceVersion => T("음성 판", "ボイスver", "Voice ver.");
	public static string ShipParamPortVoiceVersion => T("모항 음성 판", "母港ボイスver", "Port voice ver.");
	public static string ShipParamCostume => T("원래 의상 ID", "元衣装ID", "Original costume ID");
	public static string ShipParamNoCostume => T("없음(의상 함선이면 원래 함선의 ID)", "なし(衣装艦なら元の艦のID)", "None (a costume ship's original ship ID)");

	public static string ImageViewerChecker => T("바둑판 배경", "市松模様の背景", "Checkered background");
	public static string ImageViewerOpenTitle => T("이미지 열기", "画像を開く", "Open Image File");
	public static string ImageViewerFit => T("맞춤", "ぴったり", "Fit");
	public static string ImageViewerHint => T("←/→ 넘기기 · 휠 확대 · 끌어서 이동", "←/→ 切り替え・ホイール 拡大・ドラッグ 移動", "←/→ switch · wheel zoom · drag move");
	public static string ImageViewerDamaged => T("중파", "中破", "damaged");
	public static string ImageViewerGroup(string name, int count) => T($"{name} · {count}장", $"{name} · {count}枚", $"{name} · {count}");
	public static string ImageViewerCostume(int shipId) => T($"의상 {shipId}", $"衣装 {shipId}", $"Costume {shipId}");

	/// <summary>A ship image's kind by its folder (kcs2/resources/ship/…), null for another folder.</summary>
	public static string? ImageViewerKind(string folder) => folder switch
	{
		"banner" => T("배너", "バナー", "Banner"),
		"card" => T("카드", "カード", "Card"),
		"character_up" => T("상반신", "上半身", "Upper body"),
		"character_full" => T("전신", "全身", "Full body"),
		"full" => T("큰 그림", "立ち絵", "Full"),
		"remodel" => T("개장 컷인", "改装カットイン", "Remodel cut-in"),
		"album_status" => T("도감 이름", "図鑑名", "Album name"),
		"supply_character" => T("보급", "補給", "Supply"),
		_ => null,
	};
}
