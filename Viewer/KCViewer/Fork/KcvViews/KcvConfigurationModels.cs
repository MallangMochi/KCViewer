using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Notifier;
using KCViewer.Window.Settings.Notification.Base;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: an entry of the settings window's list (<see cref="KcvConfigurationWindow"/>): a page, or a group (보조 윈도우,
/// 알람) whose pages show under it while one of them is chosen.
/// </summary>
public sealed class KcvConfigEntry : ObservableObject
{
	public string Name { get; }

	/// <summary>The page's view model, shown through the page templates; null for a group.</summary>
	public object? Content { get; }

	public KcvConfigEntry? Group { get; }

	public List<KcvConfigEntry> Children { get; } = [];

	public bool IsGroup => Content is null;

	public bool IsChild => Group is not null;

	/// <summary>「보조 윈도우 › 함대」 for a page of a group.</summary>
	public string Title => Group is null ? Name : $"{Group.Name} › {Name}";

	/// <summary>A group's pages show while one of them is chosen.</summary>
	public bool IsShown { get; set; } = true;

	public bool IsOpen { get; set; }

	public KcvConfigEntry(string name, object? content, KcvConfigEntry? group = null)
	{
		Name = name;
		Content = content;
		Group = group;
		IsShown = group is null;
	}
}

/// <summary>
/// Fork: a page of 알람 (settings window mockup 20261007-settings-v2): a notification's settings in the window instead of
/// the viewer's dialog, with 「이 알람만 개별 설정」 on 알람 활성화 and on each of the three boxes; or 기본 설정, the common
/// settings (<see cref="ForkNotifierDefaults"/>), whose parts are always its own.
/// </summary>
public sealed partial class KcvNotifierPage : ObservableObject
{
	/// <summary>NotifierManager's property name; empty for 기본 설정.</summary>
	public string Key { get; }

	public string Name { get; }

	/// <summary>This page's settings (for 기본 설정, the common settings').</summary>
	public ConfigurationNotificationBaseViewModel Notifier { get; }

	/// <summary>The common settings, shown dimmed in a part that follows them.</summary>
	public ConfigurationNotificationBaseViewModel Default { get; }

	private NotifierBase NotifierObject { get; }

	public bool IsDefault => Key.Length == 0;

	public bool OwnEnabled { get; set; }
	public bool OwnSound { get; set; }
	public bool OwnImage { get; set; }
	public bool OwnDialog { get; set; }

	/// <summary>For 기본 설정: the notifications, for its table of which parts are their own.</summary>
	public IReadOnlyList<KcvNotifierPage> Pages { get; set; } = [];

	public KcvNotifierPage(string key, string name, ConfigurationNotificationBaseViewModel notifier,
		ConfigurationNotificationBaseViewModel defaults, NotifierBase notifierObject)
	{
		Key = key;
		Name = name;
		Notifier = notifier;
		Default = defaults;
		NotifierObject = notifierObject;

		if (IsDefault)
		{
			OwnEnabled = OwnSound = OwnImage = OwnDialog = true;
			return;
		}

		ForkSettings.NotifierOverride parts = ForkNotifierDefaults.Overrides(key);
		OwnEnabled = parts.Enabled;
		OwnSound = parts.Sound;
		OwnImage = parts.Image;
		OwnDialog = parts.Dialog;
	}

	private ForkSettings.NotifierOverride Parts => new()
	{
		Enabled = OwnEnabled,
		Sound = OwnSound,
		Image = OwnImage,
		Dialog = OwnDialog,
	};

	/// <summary>Keeps which parts are the notification's own (the window's OK).</summary>
	public void StoreParts()
	{
		if (IsDefault) return;

		ForkSettings.NotifierOverride parts = ForkNotifierDefaults.Overrides(Key);
		parts.Enabled = OwnEnabled;
		parts.Sound = OwnSound;
		parts.Image = OwnImage;
		parts.Dialog = OwnDialog;
	}

	/// <summary>
	/// The page's settings as they are now: the common settings and this notification's are taken, the parts that
	/// follow take the common settings, then a test notification (as the viewer's test, this applies them).
	/// </summary>
	[RelayCommand]
	private void Test()
	{
		if (!Default.TrySaveConfiguration()) return;
		if (!IsDefault && !Notifier.TrySaveConfiguration()) return;

		if (!IsDefault)
		{
			ForkNotifierDefaults.ApplyTo(NotifierObject, Parts);
		}

		NotifierObject.DialogData.Message = Notifier.Translation.TestNotification;
		NotifierObject.Notify();
	}
}

/// <summary>
/// A label of the viewer's without its colon, restart mark or colour mark (「테마 : 」, 「하드웨어 가속 켜기 *」,
/// 「■전경색:」; the page draws the colour's swatch).
/// </summary>
public sealed class KcvConfigLabelConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is string text ? text.Trim().TrimStart('■').TrimEnd(':', '：', '*').Trim() : value;
}

/// <summary>A part of a notification in 기본 설정's table: 「개별」 when it is the notification's own, else 「기본」.</summary>
public sealed class KcvNotifyPartConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is true ? ForkStrings.KcvNotifyOwnShort : ForkStrings.KcvNotifyFollowsShort;
}

/// <summary>Collapsed when true (the dimmed common settings of a part that is the notification's own).</summary>
public sealed class KcvCollapsedWhenTrueConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is true ? Visibility.Collapsed : Visibility.Visible;
}
