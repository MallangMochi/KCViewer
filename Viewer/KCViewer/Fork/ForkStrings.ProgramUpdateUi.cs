using System.Linq;
using KCViewer.Fork.Update;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the screens of the viewer's own program updates (<see cref="Update.ForkProgramUpdateUi"/>,
/// <see cref="ForkHelpLinks"/>). The Korean wording is the user's (mockup 20261022-program-update-v5, approved
/// 2026-10-22 "5판대로 release/Viewer에서 구현해"); Japanese and English follow it.
/// </summary>
public static partial class ForkStrings
{
	// 도움말 menu (①)
	public static string HelpOnline => T("온라인 도움말(_H)", "オンラインヘルプ(_H)", "Online _help");
	public static string HelpReportIssue => T("문제 보고(_I)", "問題を報告(_I)", "Report an _issue");
	public static string CheckUpdates => T("업데이트 확인(_U)", "更新を確認(_U)", "Check for _updates");
	public static string OpenReleasePage => T("릴리스 페이지 열기(_O)", "リリースページを開く(_O)", "_Open the release page");

	// the question before a help link opens the browser (①-2)
	public static string HelpLinkTitle => T("도움말", "ヘルプ", "Help");
	public static string ReportIssueTitle => T("문제 보고", "問題を報告", "Report an issue");
	public static string OpenInBrowserQuestion => T(
		"외부 브라우저로 이동됩니다. 이동하시겠습니까?",
		"外部ブラウザーで開きます。開きますか？",
		"This opens your web browser. Open it?");
	public static string Yes => T("예", "はい", "Yes");
	public static string No => T("아니요", "いいえ", "No");

	// the menu bar item (② ~ ⑦)
	public static string UpdateDownloading(string version, int percent) => T(
		$"{version} 받는 중 {percent}%", $"{version} 取得中 {percent}%", $"{version} downloading {percent}%");
	public static string UpdateReady(string version) => T($"{version} 준비됨", $"{version} 準備完了", $"{version} ready");
	public static string UpdateManualInstall(string version) => T($"{version} (수동 설치)", $"{version}（手動インストール）", $"{version} (install by hand)");
	public static string UpdateRelease(string version) => T($"정규 버전 {version}", $"正式版 {version}", $"Release {version}");
	public static string UpdateFailed => T("업데이트 실패", "更新失敗", "Update failed");

	public static string UpdateFromTo(string current, string latest) => $"{current} → {latest}";
	public static string UpdateDownloadedOf(string version, long done, long total) =>
		$"{version}: {done / 1048576.0:N0} / {total / 1048576.0:N0} MB";
	public static string UpdateDownload(long size) => T(
		$"받기 ({size / 1048576.0:N0} MB)(_D)", $"ダウンロード ({size / 1048576.0:N0} MB)(_D)", $"_Download ({size / 1048576.0:N0} MB)");
	public static string RestartNow => T("지금 다시 시작(_R)", "今すぐ再起動(_R)", "_Restart now");
	public static string UpdateAtExit => T("종료 시 업데이트 예약(_C)", "終了時に更新(_C)", "Update when the viewer _closes");
	public static string Retry => T("다시 시도(_R)", "再試行(_R)", "_Retry");

	public static string NoInstallListLine => T(
		"이 폴더에는 설치 목록이 없어 자동으로 업데이트할 수 없습니다.",
		"このフォルダーにはインストール一覧がないため、自動で更新できません。",
		"This folder has no install list, so it cannot be updated automatically.");
	public static string NewerUpdaterLine => T(
		"이 버전에서는 자동으로 설치할 수 없습니다. 수동 업데이트가 필요합니다.",
		"このバージョンからは自動でインストールできません。手動での更新が必要です。",
		"This version cannot install it automatically. It has to be updated by hand.");
	public static string DevelopmentBuildLine => T("개발 버전: 업데이트 비활성화", "開発版: 更新無効", "Development build: updates off");
	public static string DownloadFailedLine(string version) => T($"{version} 다운로드 실패", $"{version} のダウンロードに失敗", $"{version}: download failed");
	public static string CheckFailedLine => T("업데이트 확인 실패", "更新の確認に失敗", "Update check failed");

	// the windows (⑧ ⑨)
	public static string ProgramUpdateTitle => T("프로그램 업데이트", "プログラム更新", "Program update");
	public static string ReadyQuestion(string version) => T(
		$"신규 버전 {version} 다운로드 완료. 지금 다시 시작해서 업데이트를 완료하시겠습니까?",
		$"新しいバージョン {version} のダウンロードが完了しました。今すぐ再起動して更新を完了しますか？",
		$"Version {version} is downloaded. Restart now to finish the update?");
	public static string RestartNowButton => T("지금 다시 시작", "今すぐ再起動", "Restart now");
	public static string UpdateAtExitButton => T("종료 시 업데이트 예약", "終了時に更新", "Update when closing");
	public static string NoUpdateButton => T("업데이트 안 함", "更新しない", "Not now");
	public static string OpenReleasePageButton => T("릴리스 페이지 열기", "リリースページを開く", "Open the release page");
	public static string CloseButton => T("닫기", "閉じる", "Close");

	public static string ManualInstallMessage(string version, ManualInstallReason reason) =>
		(reason == ManualInstallReason.NoInstallList
			? T($"신규 버전 {version}{ObjectParticle(version)} 자동 설치하기 위한 설치 목록({UpdateFormat.InstallManifestFileName})이 없어 자동으로 업데이트할 수 없습니다.",
				$"新しいバージョン {version} を自動でインストールするためのインストール一覧（{UpdateFormat.InstallManifestFileName}）がないため、自動で更新できません。",
				$"Version {version} cannot be installed automatically: there is no install list ({UpdateFormat.InstallManifestFileName}).")
			: T("해당 버전은 수동 업데이트가 필요합니다.",
				"このバージョンは手動での更新が必要です。",
				"This version has to be updated by hand."))
		+ "\n\n"
		+ T("릴리스 페이지에서 zip 파일을 받아 새 폴더에 풀고, Data, Plugins, Skins, GameAssets 폴더를 옮겨 다시 설치해 주십시오.",
			"リリースページから zip ファイルを取得して新しいフォルダーに展開し、Data、Plugins、Skins、GameAssets フォルダーを移して再インストールしてください。",
			"Get the zip file from the release page, unpack it into a new folder and move the Data, Plugins, Skins and GameAssets folders there.");

	// the settings' box (⑩)
	public static string StartupUpdateCheck => T("시작할 때 업데이트 확인", "起動時に更新を確認", "Check for updates at startup");

	/// <summary>
	/// 「을」 or 「를」 after a version as it is read (1.0.1 일 → 을, 1.0.2 이 → 를): the last digit's sound; 「을(를)」 when
	/// it does not end with one.
	/// </summary>
	private static string ObjectParticle(string version) => version.LastOrDefault() switch
	{
		'0' or '1' or '3' or '6' or '7' or '8' => "을",
		'2' or '4' or '5' or '9' => "를",
		_ => "을(를)",
	};
}
