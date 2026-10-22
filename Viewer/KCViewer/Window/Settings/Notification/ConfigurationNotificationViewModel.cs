using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Notifier;
using KCViewer.Utility;
using KCViewer.ViewModels;
using KCViewer.Window.Settings.Notification.AnchorageRepair;
using KCViewer.Window.Settings.Notification.Base;
using KCViewer.Window.Settings.Notification.BaseAirCorps;
using KCViewer.Window.Settings.Notification.BattleEnd;
using KCViewer.Window.Settings.Notification.Damage;
using KCViewer.Window.Settings.Notification.HomePortSupply;

namespace KCViewer.Window.Settings.Notification
{
	public partial class ConfigurationNotificationViewModel : ConfigurationViewModelBase
	{
		public ConfigurationNotificationTranslationViewModel Translation { get; }

		private Configuration.ConfigurationData Config { get; }

		public ConfigurationNotificationBaseViewModel Expedition { get; }
		public ConfigurationNotificationBaseViewModel Construction { get; }
		public ConfigurationNotificationBaseViewModel Repair { get; }
		public ConfigurationNotificationBaseViewModel Condition { get; }
		public ConfigurationNotificationDamageViewModel Damage { get; }
		public ConfigurationNotificationAnchorageRepairViewModel AnchorageRepair { get; }
		public ConfigurationNotificationHomePortSupplyViewModel HomePortSupply { get; }
		public ConfigurationNotificationBaseAirCorpsViewModel BaseAirCorps { get; }
		public ConfigurationNotificationBattleEndViewModel BattleEnd { get; }
		public ConfigurationNotificationBaseViewModel RemodelLevel { get; }
		public ConfigurationNotificationBaseViewModel Training { get; }

		public ConfigurationNotificationViewModel(Configuration.ConfigurationData config)
		{
			Translation = Ioc.Default.GetRequiredService<ConfigurationNotificationTranslationViewModel>();

			Config = config;

			Expedition = new(Config.NotifierExpedition, NotifierManager.Instance.Expedition);
			Construction = new(Config.NotifierConstruction, NotifierManager.Instance.Construction);
			Repair = new(Config.NotifierRepair, NotifierManager.Instance.Repair);
			Condition = new(Config.NotifierCondition, NotifierManager.Instance.Condition);
			Damage = new(Config.NotifierDamage, NotifierManager.Instance.Damage);
			AnchorageRepair = new(Config.NotifierAnchorageRepair, NotifierManager.Instance.AnchorageRepair);
			// Fork: its own configuration (upstream passed 정박지 수리's, which only its sound and image checks read)
			HomePortSupply = new(Config.NotifierHomePortSupply, NotifierManager.Instance.HomePortSupply);
			BaseAirCorps = new(Config.NotifierBaseAirCorps, NotifierManager.Instance.BaseAirCorps);
			BattleEnd = new(Config.NotifierBattleEnd, NotifierManager.Instance.BattleEnd);
			RemodelLevel = new(Config.NotifierRemodelLevel, NotifierManager.Instance.RemodelLevel);
			Training = new(Config.NotifierTrainingPlan, NotifierManager.Instance.TrainingPlan);
		}

		public override void Save()
		{
			// no need to save here because it's already saved in the dialog
		}

		[RelayCommand]
		private void OpenNotificationConfigDialog(Base.ConfigurationNotificationBaseViewModel? config)
		{
			if (config is null) return;

			new ConfigurationNotificationDialog(config).ShowDialog(App.Current!.MainWindow!);
		}
	}
}
