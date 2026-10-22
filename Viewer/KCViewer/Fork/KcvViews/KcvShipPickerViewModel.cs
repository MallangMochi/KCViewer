using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Avalonia.Services;
using KCViewer.Core.Types;
using KCViewer.Core.Types.Extensions;
using KCViewer.Data;
using KCViewer.Window.Tools.DropRecordViewer;

namespace KCViewer.Fork.KcvViews;

/// <summary>A ship type group of the picker's 함종 condition (the viewer's ship filter groups).</summary>
public sealed class KcvShipTypeGroupFilter(ShipTypeGroup group) : ObservableObject
{
	public ShipTypeGroup Group { get; } = group;
	public string Name { get; } = group.Display();
	public bool IsChecked { get; set; } = true;
}

/// <summary>A ship of the picker: the banner (loaded when the row is first shown), album number, type and name.</summary>
public sealed class KcvPickerShip(IShipDataMaster ship, ImageLoadService imageLoadService) : ObservableObject
{
	public IShipDataMaster Ship { get; } = ship;
	public int AlbumNo => Ship.AlbumNo;
	public string TypeName => Ship.ShipTypeName;
	public string Name => Ship.NameEN;
	private bool TriedToLoadBanner { get; set; }

	public BitmapSource? Banner
	{
		get
		{
			if (field is null && !TriedToLoadBanner)
			{
				TriedToLoadBanner = true;
				Task.Run(LoadBanner);
			}

			return field;
		}
		private set;
	}

	private async Task LoadBanner()
	{
		Bitmap? bitmap = await imageLoadService.GetShipImage(Ship.ShipId, GameResourceHelper.ResourceTypeShipBanner);
		if (bitmap is null) return;

		await Application.Current.Dispatcher.BeginInvoke(() =>
		{
			Banner = bitmap.ToBitmapImage();
			OnPropertyChanged(nameof(Banner));
		});
	}
}

/// <summary>
/// Fork: the drop record's ship picker in KanColleViewer's style (mockup v5, 2026-10-05). The same choices as the
/// viewer's picker (DropRecordShipSelectorViewModel): (모두) (드랍) (없음) (만원), or one ship of the ships that are not
/// remodels of another; the conditions kept are the ship type groups, the name (also by reading and by the Japanese
/// name, <see cref="KcvNameSearch"/>) and 최종 개조, with the banners ("배너만 남기고 해당 부분들은 제외": the ASW,
/// luck and equipment conditions and the stat columns are left out).
/// </summary>
public sealed class KcvShipPickerViewModel : ObservableObject
{
	public List<KcvShipTypeGroupFilter> TypeFilters { get; }
	public List<DropRecordOptionChoice> Options { get; } =
	[
		new(DropRecordOption.All),
		new(DropRecordOption.Drop),
		new(DropRecordOption.NoDrop),
		new(DropRecordOption.FullPort),
	];

	public List<KcvPickerShip> Ships { get; }
	public ICollectionView View { get; }

	public string NameFilter { get; set; } = "";

	/// <summary>Off at first, as the viewer's drop record picker has it.</summary>
	public bool FinalRemodel { get; set; }

	public int ShownCount { get; private set; }
	public string CountText => ForkStrings.KcvPickerShips(ShownCount);

	/// <summary>A DropRecordOption or the chosen ship's IShipDataMaster.</summary>
	public object? Result { get; set; }

	private bool SettingAllTypes { get; set; }

	public KcvShipPickerViewModel()
	{
		ImageLoadService images = Ioc.Default.GetRequiredService<ImageLoadService>();

		TypeFilters = Enum.GetValues<ShipTypeGroup>().Select(g => new KcvShipTypeGroupFilter(g)).ToList();
		foreach (KcvShipTypeGroupFilter filter in TypeFilters)
		{
			filter.PropertyChanged += (_, _) =>
			{
				if (SettingAllTypes) return;
				OnPropertyChanged(nameof(AllTypes));
				Refresh();
			};
		}

		Ships = KCDatabase.Instance.MasterShips.Values
			.Where(s => !s.IsAbyssalShip && s.RemodelBeforeShipID == 0)
			.OrderBy(s => s.AlbumNo)
			.ThenBy(s => s.ShipID)
			.Select(s => new KcvPickerShip(s, images))
			.ToList();
		View = CollectionViewSource.GetDefaultView(Ships);
		View.Filter = o => o is KcvPickerShip ship && Matches(ship.Ship);

		PropertyChanged += (_, e) =>
		{
			if (e.PropertyName is nameof(NameFilter) or nameof(FinalRemodel)) Refresh();
		};
		Refresh();
	}

	/// <summary>전체 선택: all, none, or some groups.</summary>
	public bool? AllTypes
	{
		get => TypeFilters.All(f => f.IsChecked) ? true : TypeFilters.Any(f => f.IsChecked) ? null : false;
		set
		{
			SettingAllTypes = true;
			foreach (KcvShipTypeGroupFilter filter in TypeFilters)
			{
				filter.IsChecked = value is not false;
			}
			SettingAllTypes = false;

			OnPropertyChanged(nameof(AllTypes));
			Refresh();
		}
	}

	private bool Matches(IShipDataMaster ship)
	{
		if (!TypeFilters.Where(f => f.IsChecked).SelectMany(f => f.Group.ToTypes()).Contains(ship.ShipType)) return false;
		if (FinalRemodel && !ship.IsFinalRemodel()) return false;
		if (!string.IsNullOrEmpty(NameFilter) && !KcvNameSearch.Matches(ship, NameFilter)) return false;

		return true;
	}

	private void Refresh()
	{
		View.Refresh();
		ShownCount = View.Cast<object>().Count();
	}
}

/// <summary>One of the four choices above the ship table.</summary>
public sealed record DropRecordOptionChoice(DropRecordOption Option)
{
	public string Name => Option.Display();
}
