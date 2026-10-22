using System;
using System.Collections.Generic;
using System.Linq;
using KCViewer.Notifier;
using KCViewer.Utility;
using KCViewer.Window.Settings.Notification.Base;

namespace KCViewer.Window.Settings.Notification.AnchorageRepair;

public class ConfigurationNotificationAnchorageRepairViewModel : ConfigurationNotificationBaseViewModel
{
	protected override NotifierAnchorageRepair NotifierBase { get; }

	public List<AnchorageRepairNotificationLevel> NotificationLevels { get; }

	public AnchorageRepairNotificationLevel NotificationLevel { get; set; }

	public ConfigurationNotificationAnchorageRepairViewModel(
		Configuration.ConfigurationData.ConfigNotifierBase config,
		NotifierAnchorageRepair notifier)
		: base(config, notifier)
	{
		NotificationLevels = Enum.GetValues<AnchorageRepairNotificationLevel>().ToList();

		NotifierBase = notifier;
	}

	public override void Load()
	{
		base.Load();

		NotificationLevel = (AnchorageRepairNotificationLevel)NotifierBase.NotificationLevel;
	}


	public override void Save()
	{
		base.Save();

		NotifierBase.NotificationLevel = (int)NotificationLevel;
	}
}
