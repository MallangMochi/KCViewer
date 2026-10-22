namespace KCViewer.Fork;

/// <summary>Fork: the log lines of our changes to the viewer's data (<see cref="ForkDataOverrides"/>).</summary>
public static partial class ForkStrings
{
	public static string DataOverrideApplied(string file, int replaced, int added) => T(
		$"데이터 덮어쓰기: {file} 파일에 우리 항목을 적용했습니다(바꿈 {replaced}, 더함 {added}).",
		$"データ上書き: {file} に独自の項目を適用しました（置換 {replaced}、追加 {added}）。",
		$"Data override: our entries applied to {file} ({replaced} replaced, {added} added).");

	public static string DataOverrideFailed(string file, string message) => T(
		$"데이터 덮어쓰기: {file} 파일에 우리 항목을 적용하지 못해 받은 데이터를 그대로 씁니다({message}).",
		$"データ上書き: {file} に独自の項目を適用できず、取得したデータをそのまま使います（{message}）。",
		$"Data override: could not apply our entries to {file}, the fetched data is used as it is ({message}).");

	public static string DataOverrideFetched(string file) => T(
		$"데이터 덮어쓰기: {file} 파일을 받았습니다.",
		$"データ上書き: {file} を取得しました。",
		$"Data override: fetched {file}.");

	public static string DataOverrideFetchFailed(string file, string message) => T(
		$"데이터 덮어쓰기: {file} 파일을 받지 못했습니다({message}).",
		$"データ上書き: {file} を取得できませんでした（{message}）。",
		$"Data override: could not fetch {file} ({message}).");

	public static string DataOverrideVersionsFailed(string message) => T(
		$"데이터 덮어쓰기: 판 정보를 받지 못해 지금 있는 덮어쓰기를 그대로 씁니다({message}).",
		$"データ上書き: 版情報を取得できず、現在の上書きをそのまま使います（{message}）。",
		$"Data override: could not read the versions, the overrides here stay as they are ({message}).");
}
