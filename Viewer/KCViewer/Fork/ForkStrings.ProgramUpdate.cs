using KCViewer.Fork.Update;

namespace KCViewer.Fork;

/// <summary>Fork: the log lines of the viewer's own program updates (<see cref="ForkProgramUpdate"/>).</summary>
public static partial class ForkStrings
{
	public static string ProgramUpdateOff => T(
		"프로그램 업데이트: 이 빌드에는 업데이트 서명 키가 없어 새 판을 확인하지 않습니다.",
		"プログラム更新: このビルドには更新の署名鍵がないため、新しい版を確認しません。",
		"Program update: this build has no update signing key, so no new version is looked for.");

	public static string ProgramUpdateUpToDate(string current) => T(
		$"프로그램 업데이트: 최신 판을 쓰고 있습니다({current}).",
		$"プログラム更新: 最新版を使用しています（{current}）。",
		$"Program update: this is the latest version ({current}).");

	public static string ProgramUpdateDevelopmentBuild(string latest) => T(
		$"프로그램 업데이트: 개발 버전이라 업데이트하지 않습니다. 최신 정식 판은 {latest}입니다.",
		$"プログラム更新: 開発版のため更新しません。最新の正式版は {latest} です。",
		$"Program update: a development build is not updated. The latest release is {latest}.");

	public static string ProgramUpdateAvailable(string latest, string current) => T(
		$"프로그램 업데이트: 새 판이 있습니다. {current} → {latest}",
		$"プログラム更新: 新しい版があります。{current} → {latest}",
		$"Program update: a new version is out. {current} → {latest}");

	public static string ProgramUpdateManualInstall(string latest, ManualInstallReason reason) => reason switch
	{
		ManualInstallReason.NoInstallList => T(
			$"프로그램 업데이트: 새 판 {latest}이 있지만, 이 폴더에는 설치 목록({UpdateFormat.InstallManifestFileName})이 없어 자동으로 업데이트할 수 없습니다. 릴리스 페이지에서 받아 다시 설치해 주십시오.",
			$"プログラム更新: 新しい版 {latest} がありますが、このフォルダーにはインストール一覧（{UpdateFormat.InstallManifestFileName}）がないため自動で更新できません。リリースページから取得して再インストールしてください。",
			$"Program update: {latest} is out, but this folder has no install list ({UpdateFormat.InstallManifestFileName}), so it cannot be updated automatically. Get it from the release page and install it again."),
		_ => T(
			$"프로그램 업데이트: 새 판 {latest}은 이 판에서 자동으로 설치할 수 없습니다(업데이트 방식이 바뀜). 릴리스 페이지에서 받아 다시 설치해 주십시오.",
			$"プログラム更新: 新しい版 {latest} はこの版から自動でインストールできません（更新方式の変更）。リリースページから取得して再インストールしてください。",
			$"Program update: {latest} cannot be installed automatically from this version (the update method changed). Get it from the release page and install it again."),
	};

	public static string ProgramUpdateDownloading(string latest, long size) => T(
		$"프로그램 업데이트: {latest}을 받는 중입니다({size / 1048576.0:N0} MB).",
		$"プログラム更新: {latest} を取得しています（{size / 1048576.0:N0} MB）。",
		$"Program update: downloading {latest} ({size / 1048576.0:N0} MB).");

	public static string ProgramUpdateReady(string latest) => T(
		$"프로그램 업데이트: {latest}을 받아 확인했습니다. 다시 시작하면 업데이트합니다.",
		$"プログラム更新: {latest} を取得して確認しました。再起動すると更新します。",
		$"Program update: {latest} is downloaded and checked. Restart to update.");

	public static string ProgramUpdateCheckFailed(string message) => T(
		$"프로그램 업데이트: 새 판을 확인하지 못했습니다({message}).",
		$"プログラム更新: 新しい版を確認できませんでした（{message}）。",
		$"Program update: could not look for a new version ({message}).");

	public static string ProgramUpdateDownloadFailed(string latest, string message) => T(
		$"프로그램 업데이트: {latest}을 받지 못했습니다({message}). 다음에 확인할 때 다시 받습니다.",
		$"プログラム更新: {latest} を取得できませんでした（{message}）。次の確認で再取得します。",
		$"Program update: could not download {latest} ({message}). The next check tries again.");

	public static string ProgramUpdateBadVersion(string latest) => T(
		$"프로그램 업데이트: 릴리스의 판 번호를 읽지 못했습니다({latest}).",
		$"プログラム更新: リリースの版番号を読めませんでした（{latest}）。",
		$"Program update: the release's version cannot be read ({latest}).");

	public static string ProgramUpdateUpdaterStarted(string latest) => T(
		$"프로그램 업데이트: 업데이트 프로그램을 시작했습니다. 접속기가 닫히면 {latest}로 바꿉니다.",
		$"プログラム更新: 更新プログラムを起動しました。ビューアーが閉じると {latest} に置き換えます。",
		$"Program update: the updater has started. Once the viewer closes it is replaced with {latest}.");

	public static string ProgramUpdateUpdaterFailed(string message) => T(
		$"프로그램 업데이트: 업데이트 프로그램을 시작하지 못했습니다({message}).",
		$"プログラム更新: 更新プログラムを起動できませんでした（{message}）。",
		$"Program update: the updater could not be started ({message}).");

	public static string ProgramUpdateResult(UpdateResult result) => result.Outcome switch
	{
		UpdateOutcome.Updated => T(
			$"프로그램 업데이트: {result.From} → {result.To} 업데이트했습니다(바꾼 파일 {result.Replaced}, 지운 파일 {result.Removed}).",
			$"プログラム更新: {result.From} → {result.To} に更新しました（置換 {result.Replaced}、削除 {result.Removed}）。",
			$"Program update: updated {result.From} → {result.To} ({result.Replaced} replaced, {result.Removed} removed)."),
		UpdateOutcome.NotStarted => T(
			$"프로그램 업데이트: 업데이트하지 않았습니다. {result.Message}",
			$"プログラム更新: 更新しませんでした。{result.Message}",
			$"Program update: not updated. {result.Message}"),
		_ => T(
			$"프로그램 업데이트: 업데이트하지 못했습니다. {result.Message}",
			$"プログラム更新: 更新できませんでした。{result.Message}",
			$"Program update: the update failed. {result.Message}"),
	};

	public static string ProgramUpdateResultUnreadable(string message) => T(
		$"프로그램 업데이트: 지난 업데이트 결과를 읽지 못했습니다({message}).",
		$"プログラム更新: 前回の更新結果を読めませんでした（{message}）。",
		$"Program update: the last update's result cannot be read ({message}).");

	public static string ProgramUpdatePackageError(UpdatePackageException e) => e.Error switch
	{
		UpdatePackageError.Signature => T("서명이 맞지 않음", "署名が一致しない", "the signature is wrong"),
		UpdatePackageError.BadRelease => T("릴리스 정보를 읽을 수 없음", "リリース情報を読めない", "the release's file cannot be read"),
		UpdatePackageError.Package => T("받은 zip의 크기나 내용이 다름", "取得した zip のサイズか内容が違う", "the zip's size or contents differ"),
		UpdatePackageError.Version => T("zip 안의 판이 다름", "zip 内の版が違う", "the zip is for another version"),
		_ => e.Problem is { } problem ? $"{problem.Path}: {UpdateProblemText(problem.Kind)}" : T("zip 안의 파일이 맞지 않음", "zip 内のファイルが正しくない", "a file in the zip is wrong"),
	};

	private static string UpdateProblemText(UpdateProblemKind kind) => kind switch
	{
		UpdateProblemKind.Missing => T("없음", "なし", "missing"),
		UpdateProblemKind.Unlisted => T("목록에 없는 파일", "一覧にないファイル", "not in the list"),
		UpdateProblemKind.SizeMismatch => T("크기가 다름", "サイズが違う", "wrong size"),
		UpdateProblemKind.HashMismatch => T("내용이 다름", "内容が違う", "wrong contents"),
		UpdateProblemKind.BadPath => T("잘못된 경로", "不正なパス", "bad path"),
		_ => T("잘못된 항목", "不正な項目", "bad entry"),
	};
}
