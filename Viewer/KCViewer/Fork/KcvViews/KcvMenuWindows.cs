using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KCViewer.Data;
using KCViewer.Resource;
using KCViewer.Window.Dialog.QuestTrackerManager.Models;
using KCViewer.Window.Dialog.UiBlocker;
using KCViewer.Window.Dialog.UiBlocker.Taiha;
using KCViewer.Window.Tools.AutoRefresh;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the windows the menus open directly, in KanColleViewer's style (mockup 20261018-menu-windows-v2, approved
/// 2026-10-18 "구현"): 도구 › 자동 재로딩, 도구 › UI 차단기 and 도움말 › 버전 정보. Each returns false while the style
/// is off, so the viewer opens its own. 자동 재로딩 and UI 차단기 are one window each (the open one comes to the front).
/// </summary>
public static class KcvMenuWindows
{
	private static KcvAutoRefreshWindow? AutoRefresh { get; set; }
	private static KcvUiBlockerWindow? UiBlocker { get; set; }

	public static bool TryShowAutoRefresh(AutoRefreshViewModel viewModel, System.Windows.Window owner)
	{
		if (!KcvViewState.Current.Enabled) return false;

		if (AutoRefresh is { } open)
		{
			open.Activate();
			return true;
		}

		AutoRefresh = new(viewModel) { Owner = owner };
		AutoRefresh.Closed += (_, _) => AutoRefresh = null;
		AutoRefresh.Show();
		return true;
	}

	public static bool TryShowUiBlocker(UiBlockerManagerViewModel viewModel, System.Windows.Window owner)
	{
		if (!KcvViewState.Current.Enabled) return false;

		if (UiBlocker is { } open)
		{
			open.Activate();
			return true;
		}

		UiBlocker = new(viewModel) { Owner = owner };
		UiBlocker.Closed += (_, _) => UiBlocker = null;
		UiBlocker.Show();
		return true;
	}

	/// <summary>버전 정보, modal as the viewer's.</summary>
	public static bool TryShowVersion(System.Windows.Window owner)
	{
		if (!KcvViewState.Current.Enabled) return false;

		new KcvVersionWindow { Owner = owner }.ShowDialog();
		return true;
	}

	/// <summary>The viewer's icon (the windows' title bars and the tray show it) at a size; null without one.</summary>
	public static ImageSource? AppIcon(int size)
	{
		if (ResourceManager.Instance.AppIcon is not { } icon) return null;

		using System.Drawing.Icon sized = new(icon, size, size);
		BitmapSource image = Imaging.CreateBitmapSourceFromHIcon(sized.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		image.Freeze();
		return image;
	}
}

/// <summary>A map of 자동 재로딩 → 「1-1 진수부 정면 해역」, or with the parameter "Name" its name only.</summary>
public sealed class KcvMapNameConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not MapInfoModel map) return null;

		string? name = KCDatabase.Instance.MapInfo[map.AreaId * 10 + map.InfoId]?.NameEN;

		return parameter is "Name" ? name : name is null ? map.Display : $"{map.Display} {name}";
	}
}

/// <summary>
/// (cell ID, map) → an allowed cell as the viewer's cell list writes it: 「A (1)」, the ID alone when the cell's letter
/// is unknown or the letters are off (설정 「노드 지정에 알파벳을 사용」).
/// </summary>
public sealed class KcvCellDisplayConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values is not [int id, MapInfoModel map, ..]) return "";
		if (Utility.Configuration.Config.UI.UseOriginalNodeId) return id.ToString(CultureInfo.InvariantCulture);

		return KCDatabase.Instance.Translation.Destination.CellDisplayWithId(map.AreaId, map.InfoId, id);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>The UI blocker's positions (double) in the whole number boxes.</summary>
public sealed class KcvDoubleIntConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		value is double number ? (int)Math.Round(number) : 0;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		value is int number ? (double)number : 0d;
}

/// <summary>A blocker's box title: 「대파 진격 차단」 for the viewer's 「대파」 one.</summary>
public sealed class KcvUiBlockerTitleConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		TaihaBlockerViewModel => ForkStrings.UiBlockerTaiha,
		UiBlockerViewModel blocker => blocker.Header,
		_ => null,
	};
}
