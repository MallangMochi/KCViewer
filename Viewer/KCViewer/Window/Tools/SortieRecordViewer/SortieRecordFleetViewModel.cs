using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Avalonia.Services;
using KCViewer.Core.Types;

namespace KCViewer.Window.Tools.SortieRecordViewer;

public class SortieRecordFleetViewModel(IFleetData fleet, ImageLoadService imageLoadService) : ObservableObject
{
	public List<SortieRecordShipViewModel> Ships { get; } = fleet.MembersInstance
		.OfType<IShipData>()
		.Select(s => new SortieRecordShipViewModel(s, imageLoadService))
		.ToList();
}
