using System.Collections.Generic;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the default equipment data's log lines and 자동 데이터 업데이트 수동 병합 (<see cref="ForkShipDefaultEquipment"/>,
/// mockup 20261125-default-equipment-v6; the window's Korean text is the user's, 2026-11-25).
/// </summary>
public static partial class ForkStrings
{
	public static string DefaultEquipmentFetched(int version) => T(
		$"기본 장비 자료 버전 {version}{ObjectParticle(version)} 받았습니다.",
		$"初期装備データ バージョン {version} を取得しました。",
		$"Fetched the default equipment data, version {version}.");

	public static string DefaultEquipmentFetchFailed(string message) => T(
		$"기본 장비 자료를 받지 못했습니다({message}).",
		$"初期装備データを取得できませんでした（{message}）。",
		$"Could not fetch the default equipment data ({message}).");

	public static string DefaultEquipmentVersionFailed(string message) => T(
		$"기본 장비 자료: 버전 정보를 받지 못해 지금 있는 기록을 그대로 씁니다({message}).",
		$"初期装備データ: バージョン情報を取得できず、現在の記録をそのまま使います（{message}）。",
		$"Default equipment data: could not read its version, the record here stays as it is ({message}).");

	public static string DefaultEquipmentFailed(string message) => T(
		$"기본 장비 자료를 처리하지 못했습니다({message}).",
		$"初期装備データを処理できませんでした（{message}）。",
		$"Could not process the default equipment data ({message}).");

	public static string DefaultEquipmentProfileUnreadable(string message) => T(
		$"기본 장비 자료: 대조 기록을 읽지 못해 처음 대조처럼 다시 대조합니다({message}).",
		$"初期装備データ: 照合記録を読めないため、初回と同じく照合し直します（{message}）。",
		$"Default equipment data: could not read the comparison record, it is compared again as the first time ({message}).");

	public static string DefaultEquipmentCompared(int version, int updated, int matched, int conflicts) => T(
		$"기본 장비 자료 버전 {version} 대조: 갱신 {updated}척, 일치 {matched}척, 불일치 {conflicts}척" + (conflicts > 0 ? "(안내 창)" : ""),
		$"初期装備データ バージョン {version} 照合: 更新 {updated}隻、一致 {matched}隻、不一致 {conflicts}隻" + (conflicts > 0 ? "（案内ウィンドウ）" : ""),
		$"Default equipment data version {version} compared: {updated} updated, {matched} matching, {conflicts} conflicting" + (conflicts > 0 ? " (notice window)" : ""));

	public static string DefaultEquipmentUpdatedShips(IEnumerable<string> names) => T("갱신: ", "更新: ", "Updated: ") + string.Join(", ", names);

	public static string DefaultEquipmentAnswered(int version, IReadOnlyCollection<string> applied, IReadOnlyCollection<string> kept) => T(
		$"기본 장비 자료 버전 {version}: 신규 데이터 적용 {applied.Count}건{Names(applied)}, 기존 데이터 유지 {kept.Count}건{Names(kept)}",
		$"初期装備データ バージョン {version}: 新規データ適用 {applied.Count}件{Names(applied)}、既存データ維持 {kept.Count}件{Names(kept)}",
		$"Default equipment data version {version}: new data applied to {applied.Count}{Names(applied)}, kept {kept.Count}{Names(kept)}");

	public static string DefaultEquipmentObservedDiffers(int version, string ship) => T(
		$"기본 장비 자료(버전 {version})와 다른 초기 장비를 기록했습니다: {ship}. 관측 값을 적용합니다.",
		$"初期装備データ（バージョン {version}）と異なる初期装備を記録しました: {ship}。観測値を適用します。",
		$"Recorded default equipment that differs from the default equipment data (version {version}): {ship}. The observed one is used.");

	public static string DefaultEquipmentNoConflicts => T(
		"기본 장비 자료와 불일치한 함선이 없습니다.",
		"初期装備データと一致しない艦船はありません。",
		"No ship differs from the default equipment data.");

	public static string DefaultEquipmentNoData => T(
		"받아 둔 기본 장비 자료가 없습니다. 번역 주소에서 자료를 받은 뒤에 대조할 수 있습니다.",
		"取得済みの初期装備データがありません。翻訳アドレスからデータを取得した後に照合できます。",
		"No default equipment data has been fetched; it can be compared once it is fetched from the translations' address.");

	public static string DefaultEquipmentNeedsMaster => T(
		"기본 장비 자료 대조는 게임에 접속한 뒤에 할 수 있습니다.",
		"初期装備データの照合はゲームに接続した後に行えます。",
		"The default equipment data can be compared once the game has started.");

	// 자동 데이터 업데이트 수동 병합 (mockup 20261125-default-equipment-v6, the user's words)
	public static string DefaultEquipmentMergeTitle => T("자동 데이터 업데이트 수동 병합", "自動データ更新 手動マージ", "Manual merge of an automatic data update");

	/// <summary>The window's first line; <paramref name="previous"/> is null when the profile never compared.</summary>
	public static string DefaultEquipmentMergeHeader(int version, int? previous) => T(
		$"자동 데이터 업데이트 충돌 감지 신규 : 버전{version} / 기존 : " + (previous is { } p ? $"버전{p}" : "없음") + " (함선 기본 장비)",
		$"自動データ更新の競合を検出 新規 : バージョン{version} / 既存 : " + (previous is { } q ? $"バージョン{q}" : "なし") + " (艦船初期装備)",
		$"Automatic data update conflict detected  New: version {version} / Current: " + (previous is { } r ? $"version {r}" : "none") + " (ship default equipment)");

	public static string DefaultEquipmentMergeUpdated(int count) => T($"갱신 {count}건", $"更新 {count}件", $"Updated {count}");
	public static string DefaultEquipmentMergeMatched(int count) => T($"일치 {count}건", $"一致 {count}件", $"Matching {count}");
	public static string DefaultEquipmentMergeConflicts(int count) => T($"불일치 {count}건", $"不一致 {count}件", $"Conflicting {count}");

	public static string DefaultEquipmentMergeDescription => T(
		"아래 데이터가 자동으로 업데이트 되지 않았습니다. 확인 후 데이터 적용 여부를 선택 해주십시오.",
		"以下のデータは自動で更新されませんでした。確認の上、データを適用するか選択してください。",
		"The data below was not updated automatically. Check it and choose whether to apply the new data.");

	public static string DefaultEquipmentMergeShip => T("함선", "艦船", "Ship");
	public static string DefaultEquipmentMergeProfile => T("프로필 기록", "プロファイルの記録", "Profile record");
	public static string DefaultEquipmentMergeNew(int version) => T($"새 자료(버전 {version})", $"新しいデータ（バージョン {version}）", $"New data (version {version})");
	public static string DefaultEquipmentMergeShipDetail(int id, string type) => $"ID {id} · {type}";
	public static string DefaultEquipmentMergeEmptySlot => T("(빈 칸)", "(空き)", "(empty)");
	public static string DefaultEquipmentMergeApply(int count) => T($"신규 데이터 적용 ({count}건)", $"新規データ適用 ({count}件)", $"Apply new data ({count})");
	public static string DefaultEquipmentMergeKeep => T("기존 데이터 유지", "既存データ維持", "Keep current data");

	// the ship album's 파일 menu, in place of 디폴트 레코드로부터 갱신
	public static string AlbumCompareDefaultEquipment => T("기본 장비 자료와 대조(_M)", "初期装備データと照合(_M)", "Compare with default equipment data (_M)");

	private static string Names(IReadOnlyCollection<string> names) => names.Count > 0 ? T($"({string.Join(", ", names)})", $"（{string.Join("、", names)}）", $" ({string.Join(", ", names)})") : "";

	/// <summary>을 or 를 after a number as it is read in Korean (1 일, 2 이, …, 10 십).</summary>
	private static string ObjectParticle(int number) => (System.Math.Abs(number) % 10) switch
	{
		2 or 4 or 5 or 9 => "를",
		_ => "을",
	};
}
