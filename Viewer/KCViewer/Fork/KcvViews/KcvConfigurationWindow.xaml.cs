using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using KCViewer.Notifier;
using KCViewer.Translations;
using KCViewer.Window.Settings;
using KCViewer.Window.Settings.Notification;
using KCViewer.Window.Settings.Notification.Base;
using KCViewer.Window.Settings.SubWindow;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: 파일 › 설정 in KanColleViewer's style (see the XAML), opened through <see cref="KcvCalcWindows"/> while the
/// style is on. The notifications are saved with the window's [OK] (the viewer saves each in its own dialog), with
/// the common settings and which parts are each notification's own (<see cref="ForkNotifierDefaults"/>).
/// </summary>
public partial class KcvConfigurationWindow
{
	/// <summary>The chosen page's name, the title of the box of a page with one box (보조 윈도우 › 공창 and so on).</summary>
	public static readonly DependencyProperty PageNameProperty = DependencyProperty.Register(
		nameof(PageName), typeof(string), typeof(KcvConfigurationWindow), new PropertyMetadata(""));

	public string PageName
	{
		get => (string)GetValue(PageNameProperty);
		set => SetValue(PageNameProperty, value);
	}

	private List<KcvConfigEntry> Entries { get; } = [];

	/// <summary>기본 설정 first, then the notifications.</summary>
	private List<KcvNotifierPage> NotifierPages { get; } = [];

	public KcvConfigurationWindow(ConfigurationViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		BuildEntries();
		Nav.ItemsSource = Entries;
		Nav.SelectedItem = Entries[0];

		ViewModel.PropertyChanged += CloseOnResult;
		Closed += (_, _) =>
		{
			ViewModel.PropertyChanged -= CloseOnResult;

			foreach (KcvNotifierPage page in NotifierPages)
			{
				page.Notifier.StopSound();
			}

			// a test applied the page's settings for a moment; the parts that follow the common settings take them again
			ForkNotifierDefaults.Apply();
		};
	}

	private void CloseOnResult(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not nameof(ViewModel.DialogResult)) return;

		Close();
	}

	private void BuildEntries()
	{
		ConfigurationTranslationViewModel t = ViewModel.Translation;
		ConfigurationSubWindowViewModel sub = ViewModel.SubWindow;
		ConfigurationSubWindowTranslationViewModel st = sub.Translation;

		Add(t.Communication, ViewModel.Connection);
		Add(t.UI, ViewModel.UI);
		Add(t.Log, ViewModel.Log);
		Add(t.Behavior, ViewModel.Behavior);
		Add(t.Window, ViewModel.Window);

		KcvConfigEntry subWindows = Add(t.SubWindow, null);
		AddTo(subWindows, st.Fleet, sub.Fleet);
		AddTo(subWindows, st.Arsenal, sub.Arsenal);
		AddTo(subWindows, st.Dock, sub.Dock);
		AddTo(subWindows, st.HQ, sub.Headquarters);
		AddTo(subWindows, st.Compass, sub.Compass);
		AddTo(subWindows, st.Quests, sub.Quest);
		AddTo(subWindows, st.ShipTraining, sub.ShipTraining);
		AddTo(subWindows, st.Group, sub.Group);
		AddTo(subWindows, st.Combat, sub.Combat);
		AddTo(subWindows, st.Browser, sub.Browser);
		AddTo(subWindows, st.AB, sub.AirBase);
		AddTo(subWindows, st.JSON, sub.Json);

		KcvConfigEntry notifications = Add(t.Notification, null);
		foreach (KcvNotifierPage page in BuildNotifierPages())
		{
			AddTo(notifications, page.Name, page);
		}

		Add(t.BGM, ViewModel.BGM);
		Add(t.Data, ViewModel.DataSubmission);
		// rarely used, so at the end of the list (the viewer has it fifth)
		Add(t.Debug, ViewModel.Debug);
	}

	private KcvConfigEntry Add(string name, object? content)
	{
		KcvConfigEntry entry = new(name, content);
		Entries.Add(entry);
		return entry;
	}

	private void AddTo(KcvConfigEntry group, string name, object content)
	{
		KcvConfigEntry entry = new(name, content, group);
		group.Children.Add(entry);
		Entries.Add(entry);
	}

	private List<KcvNotifierPage> BuildNotifierPages()
	{
		ConfigurationNotificationViewModel n = ViewModel.Notification;

		ConfigurationNotificationBaseViewModel defaults = new(ForkNotifierDefaults.Config, ForkNotifierDefaults.Notifier);
		defaults.Load();
		KcvNotifierPage defaultPage = new("", ForkStrings.KcvNotifyDefault, defaults, defaults, ForkNotifierDefaults.Notifier);

		(string Key, string Name, ConfigurationNotificationBaseViewModel ViewModel)[] notifiers =
		[
			("Expedition", ConfigRes.ExpedReturn, n.Expedition),
			("Construction", ConfigRes.ConstructEnd, n.Construction),
			("Repair", ConfigRes.DockEnd, n.Repair),
			("Condition", ConfigRes.FatigueRestore, n.Condition),
			("Damage", ConfigRes.TaihaAdvance, n.Damage),
			("AnchorageRepair", ConfigRes.AnchorageRepairFinish, n.AnchorageRepair),
			("HomePortSupply", NotifierRes.HomePortSupply, n.HomePortSupply),
			("BaseAirCorps", ForkStrings.KcvNotifyBaseAirCorps, n.BaseAirCorps),
			("BattleEnd", ForkStrings.KcvNotifyBattleEnd, n.BattleEnd),
			("RemodelLevel", ConfigurationResources.Notification_RemodelLevel, n.RemodelLevel),
			("TrainingPlan", ConfigurationResources.Notification_Training, n.Training),
		];

		List<KcvNotifierPage> pages = notifiers
			.Select(x =>
			{
				x.ViewModel.Load();
				NotifierBase notifier = ForkNotifierDefaults.Notifiers.First(p => p.Key == x.Key).Notifier();
				return new KcvNotifierPage(x.Key, x.Name, x.ViewModel, defaults, notifier);
			})
			.ToList();

		defaultPage.Pages = pages;

		NotifierPages.Add(defaultPage);
		NotifierPages.AddRange(pages);

		return NotifierPages;
	}

	/// <summary>A group opens its first page; a page shows under its open group, the other groups fold.</summary>
	private void ShowEntry(object sender, SelectionChangedEventArgs e)
	{
		if (Nav.SelectedItem is not KcvConfigEntry entry) return;

		if (entry.IsGroup)
		{
			Nav.SelectedItem = entry.Children[0];
			return;
		}

		foreach (KcvConfigEntry group in Entries.Where(g => g.IsGroup))
		{
			bool open = group == entry.Group;
			group.IsOpen = open;

			foreach (KcvConfigEntry child in group.Children)
			{
				child.IsShown = open;
			}
		}

		PageTitle.Text = entry.Title;
		PageName = entry.Name;
		PageContent.Content = entry.Content;
		Page.ScrollToTop();
	}

	/// <summary>
	/// The notifications first (each page's values, the common settings, which parts are whose), then the viewer's
	/// own saving of the other pages, which closes the window. A notification that does not validate shows its page.
	/// </summary>
	private void Confirm(object sender, RoutedEventArgs e)
	{
		foreach (KcvNotifierPage page in NotifierPages)
		{
			if (page.Notifier.TrySaveConfiguration()) continue;

			Nav.SelectedItem = Entries.First(x => x.Content == page);
			return;
		}

		foreach (KcvNotifierPage page in NotifierPages)
		{
			page.StoreParts();
		}

		ForkNotifierDefaults.Save();
		ForkNotifierDefaults.Apply();

		ViewModel.ConfirmCommand.Execute(null);
	}
}
