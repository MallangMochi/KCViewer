using System;
using System.Collections.Generic;
using KCViewer.Fork.Update;

namespace KCUpdater;

/// <summary>
/// Fork: the updater's messages in the viewer's language (<c>--lang=</c>: Korean, Japanese, else English), as the
/// viewer's ForkStrings has them. The Korean wording is the user's (mockup 20261022-program-update-v5, approved
/// 2026-10-22 "5판대로 release/Viewer에서 구현해"); the reasons behind a message (the file, the error) go to the log.
/// </summary>
internal static class Strings
{
	public static string Language { get; set; } = "ko-KR";

	private static string T(string korean, string japanese, string english) =>
		Language.StartsWith("ko", StringComparison.OrdinalIgnoreCase) ? korean
		: Language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? japanese
		: english;

	/// <summary>The viewer's name in Korean (ForkStrings.SoftwareNameKorean), KCViewer in the other languages.</summary>
	public static string Title => T("시제 74식 제독업무도 바빠 改 업데이트", "KCViewer アップデート", "KCViewer update");

	/// <summary>Not started by the viewer (no folder, no staging area): the updater only runs from the viewer.</summary>
	public static string BadArguments => T(
		"업데이트 실패.\n자동 업데이트 프로그램의 실행 위치가 잘못되었습니다.",
		"アップデートに失敗しました。\n自動アップデートプログラムの実行場所が正しくありません。",
		"Update failed.\nThe updater was started from the wrong place.");

	public static string ViewerStillOpen => T(
		"접속기가 아직 실행 중입니다.\n\n접속기를 종료한 뒤 [다시 시도]를 누르세요. 업데이트를 중단하려면 [취소]를 누르세요.",
		"ビューアーがまだ実行中です。\n\nビューアーを終了してから［再試行］を押してください。アップデートを中止するには［キャンセル］を押してください。",
		"The viewer is still running.\n\nClose it, then press Retry. To stop the update, press Cancel.");

	/// <summary><paramref name="open"/>: "KCViewer.exe(1234) title" for each, one a line.</summary>
	public static string ProcessesStillOpen(IReadOnlyList<string> open) => T(
		$"접속기가 아직 실행 중입니다.\n{string.Join('\n', open)}\n\n접속기를 종료한 뒤 [다시 시도]를 누르세요. 업데이트를 중단하려면 [취소]를 누르세요.",
		$"ビューアーがまだ実行中です。\n{string.Join('\n', open)}\n\nビューアーを終了してから［再試行］を押してください。アップデートを中止するには［キャンセル］を押してください。",
		$"The viewer is still running.\n{string.Join('\n', open)}\n\nClose it, then press Retry. To stop the update, press Cancel.");

	/// <summary>The result's reason when the user cancelled (logged by the viewer, not shown again).</summary>
	public static string WindowsLeftOpen => T(
		"접속기가 닫히지 않아 업데이트하지 않았습니다.",
		"ビューアーが閉じなかったため、アップデートしませんでした。",
		"Not updated: the viewer was not closed.");

	public static string NoInstallList => T(
		$"자동 설치 목록({UpdateFormat.InstallManifestFileName})이 없어 자동으로 업데이트할 수 없습니다.\n\n릴리스 페이지에서 zip 파일을 받아 새 폴더에 풀고, Data, Plugins, Skins, GameAssets 폴더를 옮겨 다시 설치해 주십시오.",
		$"自動インストール一覧（{UpdateFormat.InstallManifestFileName}）がないため、自動でアップデートできません。\n\nリリースページから zip ファイルを取得して新しいフォルダーに展開し、Data、Plugins、Skins、GameAssets フォルダーを移して再インストールしてください。",
		$"There is no install list ({UpdateFormat.InstallManifestFileName}), so the viewer cannot be updated automatically.\n\nGet the zip file from the release page, unpack it into a new folder and move the Data, Plugins, Skins and GameAssets folders there.");

	public static string BadNewFile => T(
		"다운로드한 파일의 손상이 감지되었습니다. 다시 다운로드를 시도하거나 수동으로 설치해 주십시오.",
		"ダウンロードしたファイルの破損が見つかりました。もう一度ダウンロードするか、手動でインストールしてください。",
		"The downloaded files are damaged. Download them again or install the viewer by hand.");

	public static string CheckFailed => T(
		"업데이트 실패. 업데이트가 준비되지 않았습니다.",
		"アップデートに失敗しました。アップデートの準備ができていません。",
		"Update failed. The update is not ready.");

	public static string RolledBack(string message) => T(
		$"업데이트 실패. ({message})",
		$"アップデートに失敗しました。（{message}）",
		$"Update failed. ({message})");

	public static string RollbackFailed(string backup) => T(
		$"업데이트 실패. 자동 복구 실패\n백업 폴더 {backup}의 백업된 파일을 수동으로 복구하거나 릴리스 페이지에서 수동으로 설치해 주십시오.",
		$"アップデートに失敗しました。自動復元にも失敗しました。\nバックアップフォルダー {backup} のファイルを手動で戻すか、リリースページから手動でインストールしてください。",
		$"Update failed. Restoring the old files failed too.\nPut back the files in the backup folder {backup} by hand, or install the viewer by hand from the release page.");

	public static string RestartFailed(string viewer) => T(
		$"접속기 재실행에 실패했습니다.\n{viewer}를 직접 실행해 주십시오.",
		$"ビューアーを再起動できませんでした。\n{viewer} を直接起動してください。",
		$"The viewer could not be started again.\nStart {viewer} yourself.");
}
