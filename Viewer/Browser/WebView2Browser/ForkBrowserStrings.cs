using System;
using System.Globalization;

namespace Browser.WebView2Browser;

/// <summary>
/// Fork: log text of the browser process, Korean when the UI culture is Korean (the host passes it, BrowserViewModel)
/// and the original English otherwise. The fork's own lines (DevTools capture, local skin) and the upstream ones that
/// are often seen (VIEWER-PLAN.md, stage 6 (3)).
/// </summary>
public static class ForkBrowserStrings
{
	private static bool IsKorean => CultureInfo.CurrentUICulture.Name.StartsWith("ko", StringComparison.Ordinal);

	private static string K(string korean, string original) => IsKorean ? korean : original;

	// LocalSkin.cs, WebView2ViewModel.Fork.cs
	public static string LocalSkinOn(int count, string folder) =>
		K($"로컬 스킨: 켜짐, {folder}에 파일 {count}개", $"Local skin: on, {count} files in {folder}");

	public static string LocalSkinOff => K("로컬 스킨: 꺼짐", "Local skin: off");

	public static string LocalSkinAnswered(int total, string path) =>
		K($"로컬 스킨: 지금까지 {total}개 파일로 응답했습니다 (마지막 {path})", $"Local skin: {total} files answered so far (latest {path})");

	public static string LocalSkinReadFailed(string file, string message) =>
		K($"로컬 스킨: {file}을(를) 읽지 못했습니다: {message}", $"Local skin: could not read {file}: {message}");

	public static string LocalSkinError(string message) => K($"로컬 스킨: {message}", $"Local skin: {message}");

	public static string KcvStyleError(string message) => K($"제독업무 스타일: {message}", $"KanColleViewer style: {message}");

	public static string ScreenshotHotkeyError(string message) => K($"스크린샷 단축키: {message}", $"Screenshot key: {message}");

	// The tool strip's tooltips in KanColleViewer's style (BrowserView.xaml): the login item's menu text without its
	// access key, and the other menu (new text, remaining panels mockup 1)
	public static string LoginPageTip =>
		System.Text.RegularExpressions.Regex.Replace(Resources.ToolMenu_Other_NavigateToLogInPage, @"\(&.\)|&", "");

	public static string OtherMenuTip => K("그 밖의 기능", "Other");

	// DevToolsApiCapture.cs, DevToolsApiCapture.GameFiles.cs
	public static string CaptureEventFailed(string eventName, string message) =>
		K($"DevTools 수집({eventName}): {message}", $"DevTools capture ({eventName}): {message}");

	public static string CaptureChildFrameFailed(string? type, string message) =>
		K($"DevTools 수집: 하위 프레임({type})을 켜지 못했습니다: {message}", $"DevTools capture: could not enable a child frame ({type}): {message}");

	public static string CaptureApiFrom(string origin, bool mainFrame) => K(
		$"DevTools 수집: {origin}의 API ({(mainFrame ? "메인 프레임" : "하위 프레임")})",
		$"DevTools capture: API from {origin} ({(mainFrame ? "main frame" : "child frame")})");

	public static string CaptureRequestFailed(string path, string? error) =>
		K($"DevTools 수집: {path} 실패 ({error})", $"DevTools capture: {path} failed ({error})");

	public static string CaptureReadFailed(string path, string message) =>
		K($"DevTools 수집: {path}을(를) 읽지 못했습니다: {message}", $"DevTools capture: could not read {path}: {message}");

	public static string CapturePassFailed(string message) =>
		K($"DevTools 수집: 본체에 데이터를 넘기지 못했습니다: {message}", $"DevTools capture: could not pass data to the host: {message}");

	public static string CaptureOfferFailed(string message) =>
		K($"DevTools 수집: 본체에 게임 파일을 알리지 못했습니다: {message}", $"DevTools capture: could not offer a game file to the host: {message}");

	public static string CapturePassFileFailed(string path, string message) =>
		K($"DevTools 수집: {path}을(를) 본체에 넘기지 못했습니다: {message}", $"DevTools capture: could not pass {path} to the host: {message}");

	// Upstream WebView2ViewModel.cs and BrowserView.xaml
	public static string BrowserProcessExited => K("브라우저 프로세스가 종료되었습니다", "Browser Process Exited");
	public static string GpuProcessExited => K("GPU 프로세스가 종료되었습니다", "GPU Process Exited");
	public static string RenderProcessUnresponsive => K("렌더러 프로세스가 응답하지 않습니다", "Render Process Unresponsive");
	public static string UnknownProcessExited => K("알 수 없는 프로세스가 종료되었습니다", "Unknown Process Exited");
	public static string ProcessFailed => K("프로세스 오류", "Proccess Failed");

	/// <summary>Zoom menu: the game's size before the HTML5 client (800x480).</summary>
	public static string ZoomClassic => K("67% (예전 크기)", "67% (Classic)");
}
