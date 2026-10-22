using System.Collections.Generic;
using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Observer;
using KCViewer.Resource;
using KCViewer.ViewModels;
using KCViewer.ViewModels.Translations;

namespace KCViewer.Window.Wpf.Dock;

public class DockViewModel : AnchorableViewModel
{
	public FormDockTranslationViewModel FormDock { get; }
	public List<DockItemViewModel> Docks { get; }

	public DockViewModel() : base("Dock", "Dock", IconContent.FormDock)
	{
		FormDock = Ioc.Default.GetService<FormDockTranslationViewModel>()!;

		Title = FormDock.Title;
		FormDock.PropertyChanged += (_, _) => Title = FormDock.Title;

		Docks = new()
		{
			new(),
			new(),
			new(),
			new(),
		};

		APIObserver o = APIObserver.Instance;

		o.ApiReqNyukyo_Start.RequestReceived += Updated;
		o.ApiReqNyukyo_SpeedChange.RequestReceived += Updated;

		o.ApiPort_Port.ResponseReceived += Updated;
		o.ApiGetMember_NDock.ResponseReceived += Updated;

		Utility.Configuration.Instance.ConfigurationChanged += ConfigurationChanged;
		Utility.SystemEvents.UpdateTimerTick += UpdateTimerTick;
	}

	private void Updated(string apiname, dynamic data)
	{
		for (int i = 0; i < Docks.Count; i++)
		{
			Docks[i].Update(i + 1);
		}
	}

	private void UpdateTimerTick()
	{
		for (int i = 0; i < Docks.Count; i++)
		{
			Docks[i].Refresh(i + 1);
		}
	}

	private void ConfigurationChanged()
	{
		// no idea
	}
}
