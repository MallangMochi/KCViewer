using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using KCViewer.Notifier;
using KCViewer.Utility;
using ConfigNotifier = KCViewer.Utility.Configuration.ConfigurationData.ConfigNotifierBase;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the common notification settings, 알람 › 기본 설정 (settings window mockup 20261007-settings-v2, approved
/// 2026-10-07 "구현"). A notifier takes four parts from them, each unless that part is its own (「이 알람만 개별 설정」):
/// 알람 활성화, the sound, the image and the dialog. The settings are kept in <c>Settings\Fork.json</c>; a default
/// notifier holds them, so the settings window edits and tests them as it does a notifier.
/// <para>
/// The notifiers keep the values they use, and the viewer writes those to its configuration when it closes, so a part
/// that follows the common settings always holds their values. A part found different (changed in the viewer's own
/// notification dialog, with the style off) becomes the notifier's own (<see cref="Reconcile"/>).
/// </para>
/// </summary>
public static class ForkNotifierDefaults
{
	/// <summary>The notifiers by their NotifierManager property name, the key of their parts in the settings.</summary>
	public static IReadOnlyList<(string Key, Func<NotifierBase> Notifier)> Notifiers { get; } =
	[
		("Expedition", () => NotifierManager.Instance.Expedition),
		("Construction", () => NotifierManager.Instance.Construction),
		("Repair", () => NotifierManager.Instance.Repair),
		("Condition", () => NotifierManager.Instance.Condition),
		("Damage", () => NotifierManager.Instance.Damage),
		("AnchorageRepair", () => NotifierManager.Instance.AnchorageRepair),
		("HomePortSupply", () => NotifierManager.Instance.HomePortSupply),
		("BaseAirCorps", () => NotifierManager.Instance.BaseAirCorps),
		("BattleEnd", () => NotifierManager.Instance.BattleEnd),
		("RemodelLevel", () => NotifierManager.Instance.RemodelLevel),
		("TrainingPlan", () => NotifierManager.Instance.TrainingPlan),
	];

	private static ForkSettings.NotificationSettings Settings => ForkSettings.Current.Notification;

	private static ConfigNotifier? config;
	private static DefaultNotifier? notifier;

	/// <summary>The configuration object of the default notifier (its sound and image paths live here).</summary>
	public static ConfigNotifier Config => config ??= ToConfig(Settings.Default);

	/// <summary>The default notifier: not one of the game's notifications, it holds and tests the common settings.</summary>
	public static NotifierBase Notifier => notifier ??= new DefaultNotifier(Config);

	public static ForkSettings.NotifierOverride Overrides(string key)
	{
		if (!Settings.Overrides.TryGetValue(key, out ForkSettings.NotifierOverride? parts))
		{
			parts = new();
			Settings.Overrides[key] = parts;
		}

		return parts;
	}

	/// <summary>
	/// At start, after the notifiers are made: the first time, a part equal to the common settings' first values
	/// follows them and a part the user had changed stays the notifier's own, so nothing changes; then the parts that
	/// follow take the common settings.
	/// </summary>
	public static void Initialize()
	{
		if (!Settings.Migrated)
		{
			foreach ((string key, Func<NotifierBase> get) in Notifiers)
			{
				NotifierBase target = get();
				ForkSettings.NotifierOverride parts = Overrides(key);

				parts.Enabled = target.IsEnabled != Notifier.IsEnabled;
				parts.Sound = !SameSound(target, Notifier);
				parts.Image = !SameImage(target, Notifier);
				parts.Dialog = !SameDialog(target, Notifier);
			}

			Settings.Migrated = true;
			ForkSettings.Current.Save();
		}

		Apply();
	}

	/// <summary>The parts that follow the common settings take their values (start, the settings window closing).</summary>
	public static void Apply()
	{
		foreach ((string key, Func<NotifierBase> get) in Notifiers)
		{
			ApplyTo(get(), Overrides(key));
		}
	}

	/// <summary>One notifier's parts that follow the common settings take their values (also for a test).</summary>
	public static void ApplyTo(NotifierBase target, ForkSettings.NotifierOverride parts)
	{
		NotifierBase source = Notifier;

		if (!parts.Enabled) target.IsEnabled = source.IsEnabled;
		if (!parts.Sound) CopySound(source, target);
		if (!parts.Image) CopyImage(source, target);
		if (!parts.Dialog) CopyDialog(source, target);
	}

	/// <summary>
	/// After the viewer's own settings window (style off): a part that follows the common settings but holds other
	/// values was changed in its notification dialog, so it becomes the notifier's own.
	/// </summary>
	public static void Reconcile()
	{
		bool changed = false;

		foreach ((string key, Func<NotifierBase> get) in Notifiers)
		{
			NotifierBase target = get();
			ForkSettings.NotifierOverride parts = Overrides(key);

			changed |= Own(parts, target.IsEnabled == Notifier.IsEnabled, p => p.Enabled, (p, v) => p.Enabled = v);
			changed |= Own(parts, SameSound(target, Notifier), p => p.Sound, (p, v) => p.Sound = v);
			changed |= Own(parts, SameImage(target, Notifier), p => p.Image, (p, v) => p.Image = v);
			changed |= Own(parts, SameDialog(target, Notifier), p => p.Dialog, (p, v) => p.Dialog = v);
		}

		if (changed)
		{
			ForkSettings.Current.Save();
		}
	}

	private static bool Own(ForkSettings.NotifierOverride parts, bool same, Func<ForkSettings.NotifierOverride, bool> get,
		Action<ForkSettings.NotifierOverride, bool> set)
	{
		if (get(parts) || same) return false;

		set(parts, true);
		return true;
	}

	/// <summary>Keeps the common settings (the default notifier's values) and each notifier's parts.</summary>
	public static void Save()
	{
		Settings.Default = FromNotifier(Notifier);
		ForkSettings.Current.Save();
	}

	public static bool SameSound(NotifierBase a, NotifierBase b) =>
		a.PlaysSound == b.PlaysSound &&
		a.SoundPath == b.SoundPath &&
		a.SoundVolume == b.SoundVolume &&
		a.LoopsSound == b.LoopsSound;

	public static bool SameImage(NotifierBase a, NotifierBase b) =>
		a.DialogData.DrawsImage == b.DialogData.DrawsImage &&
		a.DialogData.ImagePath == b.DialogData.ImagePath;

	public static bool SameDialog(NotifierBase a, NotifierBase b) =>
		a.ShowsDialog == b.ShowsDialog &&
		a.AccelInterval == b.AccelInterval &&
		a.DialogData.DrawsMessage == b.DialogData.DrawsMessage &&
		a.DialogData.ClosingInterval == b.DialogData.ClosingInterval &&
		a.DialogData.CloseOnMouseMove == b.DialogData.CloseOnMouseMove &&
		a.DialogData.ClickFlag == b.DialogData.ClickFlag &&
		a.DialogData.Alignment == b.DialogData.Alignment &&
		a.DialogData.Location == b.DialogData.Location &&
		a.DialogData.HasFormBorder == b.DialogData.HasFormBorder &&
		a.DialogData.TopMost == b.DialogData.TopMost &&
		a.DialogData.ShowWithActivation == b.DialogData.ShowWithActivation &&
		a.DialogData.ForeColor.ToArgb() == b.DialogData.ForeColor.ToArgb() &&
		a.DialogData.BackColor.ToArgb() == b.DialogData.BackColor.ToArgb();

	private static void CopySound(NotifierBase source, NotifierBase target)
	{
		target.PlaysSound = source.PlaysSound;
		target.SoundVolume = source.SoundVolume;
		target.LoopsSound = source.LoopsSound;

		if (target.SoundPath == source.SoundPath) return;

		// the path is kept by the notifier's configuration object; loading a sound sets it
		if (string.IsNullOrEmpty(source.SoundPath) || !target.LoadSound(source.SoundPath))
		{
			target.Sound.Close();
			ConfigOf(target).SoundPath = source.SoundPath;
		}
	}

	private static void CopyImage(NotifierBase source, NotifierBase target)
	{
		target.DialogData.DrawsImage = source.DialogData.DrawsImage;

		if (target.DialogData.ImagePath == source.DialogData.ImagePath) return;

		if (string.IsNullOrEmpty(source.DialogData.ImagePath) || !target.DialogData.LoadImage(source.DialogData.ImagePath))
		{
			target.DialogData.DisposeImage();
		}
	}

	private static void CopyDialog(NotifierBase source, NotifierBase target)
	{
		target.ShowsDialog = source.ShowsDialog;
		target.AccelInterval = source.AccelInterval;
		target.DialogData.DrawsMessage = source.DialogData.DrawsMessage;
		target.DialogData.ClosingInterval = source.DialogData.ClosingInterval;
		target.DialogData.CloseOnMouseMove = source.DialogData.CloseOnMouseMove;
		target.DialogData.ClickFlag = source.DialogData.ClickFlag;
		target.DialogData.Alignment = source.DialogData.Alignment;
		target.DialogData.Location = source.DialogData.Location;
		target.DialogData.HasFormBorder = source.DialogData.HasFormBorder;
		target.DialogData.TopMost = source.DialogData.TopMost;
		target.DialogData.ShowWithActivation = source.DialogData.ShowWithActivation;
		target.DialogData.ForeColor = source.DialogData.ForeColor;
		target.DialogData.BackColor = source.DialogData.BackColor;
	}

	/// <summary>The configuration object a notifier was made with (NotifierManager passes the configuration's own).</summary>
	private static ConfigNotifier ConfigOf(NotifierBase target)
	{
		if (target == Notifier) return Config;

		Configuration.ConfigurationData c = Configuration.Config;
		NotifierManager m = NotifierManager.Instance;

		return target switch
		{
			_ when target == m.Expedition => c.NotifierExpedition,
			_ when target == m.Construction => c.NotifierConstruction,
			_ when target == m.Repair => c.NotifierRepair,
			_ when target == m.Condition => c.NotifierCondition,
			_ when target == m.Damage => c.NotifierDamage,
			_ when target == m.AnchorageRepair => c.NotifierAnchorageRepair,
			_ when target == m.HomePortSupply => c.NotifierHomePortSupply,
			_ when target == m.BaseAirCorps => c.NotifierBaseAirCorps,
			_ when target == m.BattleEnd => c.NotifierBattleEnd,
			_ when target == m.RemodelLevel => c.NotifierRemodelLevel,
			_ => c.NotifierTrainingPlan,
		};
	}

	private static ConfigNotifier ToConfig(ForkSettings.NotifierValues v) => new()
	{
		IsEnabled = v.IsEnabled,
		PlaysSound = v.PlaysSound,
		SoundPath = v.SoundPath,
		SoundVolume = v.SoundVolume,
		LoopsSound = v.LoopsSound,
		DrawsImage = v.DrawsImage,
		ImagePath = v.ImagePath,
		ShowsDialog = v.ShowsDialog,
		DrawsMessage = v.DrawsMessage,
		ClosingInterval = v.ClosingInterval,
		AccelInterval = v.AccelInterval,
		CloseOnMouseMove = v.CloseOnMouseMove,
		ClickFlag = (NotifierDialogClickFlags)v.ClickFlag,
		Alignment = (NotifierDialogAlignment)v.Alignment,
		Location = new(v.LocationX, v.LocationY),
		HasFormBorder = v.HasFormBorder,
		TopMost = v.TopMost,
		ShowWithActivation = v.ShowWithActivation,
		ForeColor = Color.FromArgb(v.ForeColor),
		BackColor = Color.FromArgb(v.BackColor),
	};

	private static ForkSettings.NotifierValues FromNotifier(NotifierBase n) => new()
	{
		IsEnabled = n.IsEnabled,
		PlaysSound = n.PlaysSound,
		SoundPath = n.SoundPath,
		SoundVolume = n.SoundVolume,
		LoopsSound = n.LoopsSound,
		DrawsImage = n.DialogData.DrawsImage,
		ImagePath = n.DialogData.ImagePath,
		ShowsDialog = n.ShowsDialog,
		DrawsMessage = n.DialogData.DrawsMessage,
		ClosingInterval = n.DialogData.ClosingInterval,
		AccelInterval = n.AccelInterval,
		CloseOnMouseMove = n.DialogData.CloseOnMouseMove,
		ClickFlag = (int)n.DialogData.ClickFlag,
		Alignment = (int)n.DialogData.Alignment,
		LocationX = n.DialogData.Location.X,
		LocationY = n.DialogData.Location.Y,
		HasFormBorder = n.DialogData.HasFormBorder,
		TopMost = n.DialogData.TopMost,
		ShowWithActivation = n.DialogData.ShowWithActivation,
		ForeColor = n.DialogData.ForeColor.ToArgb(),
		BackColor = n.DialogData.BackColor.ToArgb(),
	};

	/// <summary>The notifier of the common settings; its test dialog is titled 「기본 설정」.</summary>
	private sealed class DefaultNotifier : NotifierBase
	{
		public DefaultNotifier(ConfigNotifier config) : base(config)
		{
			DialogData.Title = ForkStrings.KcvNotifyDefault;
		}
	}
}
