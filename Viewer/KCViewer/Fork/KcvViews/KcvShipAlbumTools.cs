using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Resource.Record;
using static KCViewer.Resource.Record.ShipParameterRecord;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the ship album's 수치 편집 and 이미지 뷰어 in KanColleViewer's style (mockup 20261017-album-ship-tools-v2,
/// approved 2026-10-18 "구현"), opened from the album's menus while the style is on; otherwise the viewer's WinForms ones.
/// </summary>
public static class KcvShipAlbumTools
{
	/// <summary>수치 편집, modal over the album; false while the style is off. <paramref name="edited"/> runs after OK.</summary>
	public static bool TryShowParameterEditor(int shipId, Action edited)
	{
		if (!KcvViewState.Current.Enabled) return false;

		KcvShipParameterWindow window = new(new KcvShipParameterViewModel(shipId));
		if (KcvCalcWindows.FrontWindow() is { } owner) window.Owner = owner;

		if (window.ShowDialog() is true) edited();
		return true;
	}

	/// <summary>이미지 뷰어 of the ship and its costume ships; false while the style is off.</summary>
	public static bool TryShowImageViewer(int shipId)
	{
		if (!KcvViewState.Current.Enabled) return false;

		KcvShipImageViewerWindow window = new(new KcvShipImageViewerViewModel());
		window.Show();
		window.OpenShip(shipId);
		return true;
	}
}

/// <summary>A stat of 수치 편집: initial and max, the ASW, evasion and LoS ones with the initial's upper bound too.</summary>
public sealed class KcvShipParameterStat : ObservableObject
{
	public string Name { get; }
	public bool HasRange { get; }
	public int Initial { get; set; }
	public int InitialMax { get; set; }
	public int Maximum { get; set; }

	public KcvShipParameterStat(string name, int initial, int maximum)
	{
		Name = name;
		Initial = initial;
		Maximum = maximum;
	}

	public KcvShipParameterStat(string name, Parameter parameter) : this(name, parameter.MinimumEstMin, parameter.Maximum)
	{
		HasRange = true;
		InitialMax = parameter.MinimumEstMax;
	}

	public void WriteTo(Parameter parameter)
	{
		parameter.MinimumEstMin = Initial;
		parameter.MinimumEstMax = InitialMax;
		parameter.Maximum = Maximum;
	}
}

/// <summary>An equipment slot of 수치 편집: the equipment's ID with its name, and the aircraft.</summary>
public sealed class KcvShipParameterSlot : ObservableObject
{
	public int Number { get; }
	public int Equipment { get; set; }
	public int Aircraft { get; set; }

	public string EquipmentName => Equipment switch
	{
		-1 => ForkStrings.ShipParamNone,
		_ => KCDatabase.Instance.MasterEquipments[Equipment]?.NameEN ?? ForkStrings.ShipParamUnknown,
	};

	public bool IsEmpty => Equipment == -1;

	public KcvShipParameterSlot(int number, int equipment, int aircraft)
	{
		Number = number;
		Equipment = equipment;
		Aircraft = aircraft;
	}
}

/// <summary>A choice of 사거리: the value and the viewer's name of it.</summary>
public sealed record KcvShipParameterRange(int Value, string Name);

/// <summary>
/// 수치 편집's model: the ship's record (ShipParameterRecord) in fields, read and written one by one instead of the
/// viewer's one line cut at the commas. The ship's ID and name are not edited. OK puts a new record in the record
/// manager (which saves it as it does every record); 취소 changes nothing, a missing record too.
/// </summary>
public sealed class KcvShipParameterViewModel : ObservableObject
{
	public int ShipId { get; }
	public IShipDataMaster? Ship { get; }
	private ShipParameterElement Original { get; }

	public string ShipName => Ship?.NameWithClass ?? Original.ShipName ?? "???";
	public string ShipTypeName => Ship?.ShipTypeName ?? "";
	public string IdText => $"ID {ShipId}";

	public IReadOnlyList<KcvShipParameterStat> Stats { get; }
	public IReadOnlyList<KcvShipParameterRange> Ranges { get; }
	public KcvShipParameterRange Range { get; set; }
	public IReadOnlyList<KcvShipParameterSlot> Slots { get; }

	public string MessageGet { get; set; }
	public string MessageAlbum { get; set; }
	public string ResourceName { get; set; }
	public int GraphicVersion { get; set; }
	public int VoiceVersion { get; set; }
	public int PortVoiceVersion { get; set; }
	public int OriginalCostumeShipId { get; set; }

	public string CostumeNote => OriginalCostumeShipId == -1
		? ForkStrings.ShipParamNoCostume
		: KCDatabase.Instance.MasterShips[OriginalCostumeShipId]?.NameWithClass ?? ForkStrings.ShipParamUnknown;

	public KcvShipParameterViewModel(int shipId)
	{
		ShipId = shipId;
		Ship = KCDatabase.Instance.MasterShips[shipId];
		Original = RecordManager.Instance.ShipParameter[shipId] ?? new ShipParameterElement { ShipID = shipId };

		ShipParameterElement r = Original;
		Stats =
		[
			new(AlbumMasterShipResources.TitleHP, r.HPMin, r.HPMax),
			new(AlbumMasterShipResources.Firepower, r.FirepowerMin, r.FirepowerMax),
			new(AlbumMasterShipResources.Torpedo, r.TorpedoMin, r.TorpedoMax),
			new(AlbumMasterShipResources.AA, r.AAMin, r.AAMax),
			new(AlbumMasterShipResources.Armor, r.ArmorMin, r.ArmorMax),
			new(AlbumMasterShipResources.ASW, r.ASW),
			new(AlbumMasterShipResources.Evasion, r.Evasion),
			new(AlbumMasterShipResources.LOS, r.LOS),
			new(AlbumMasterShipResources.Luck, r.LuckMin, r.LuckMax),
		];

		List<KcvShipParameterRange> ranges = Enumerable.Range(0, 6).Select(v => new KcvShipParameterRange(v, Constants.GetRange(v))).ToList();
		if (ranges.All(o => o.Value != r.Range)) ranges.Add(new(r.Range, $"{Constants.GetRange(r.Range)} ({r.Range})"));
		Ranges = ranges;
		Range = ranges.First(o => o.Value == r.Range);

		Slots = Enumerable.Range(0, 5)
			.Select(i => new KcvShipParameterSlot(i + 1, r.DefaultSlot?[i] ?? -1, r.Aircraft?[i] ?? 0))
			.ToList();

		MessageGet = MessageText(r.MessageGet);
		MessageAlbum = MessageText(r.MessageAlbum);
		ResourceName = r.ResourceName ?? "";
		GraphicVersion = VersionNumber(r.ResourceGraphicVersion);
		VoiceVersion = VersionNumber(r.ResourceVoiceVersion);
		PortVoiceVersion = VersionNumber(r.ResourcePortVoiceVersion);
		OriginalCostumeShipId = r.OriginalCostumeShipID;
	}

	/// <summary>Puts the fields in the record manager as a new record of the ship.</summary>
	public void Save()
	{
		ShipParameterElement record = new()
		{
			ShipID = ShipId,
			HPMin = Stats[0].Initial,
			HPMax = Stats[0].Maximum,
			FirepowerMin = Stats[1].Initial,
			FirepowerMax = Stats[1].Maximum,
			TorpedoMin = Stats[2].Initial,
			TorpedoMax = Stats[2].Maximum,
			AAMin = Stats[3].Initial,
			AAMax = Stats[3].Maximum,
			ArmorMin = Stats[4].Initial,
			ArmorMax = Stats[4].Maximum,
			LuckMin = Stats[8].Initial,
			LuckMax = Stats[8].Maximum,
			Range = Range.Value,
		};
		Stats[5].WriteTo(record.ASW);
		Stats[6].WriteTo(record.Evasion);
		Stats[7].WriteTo(record.LOS);

		// a record without equipment or aircraft keeps none while the fields are as shown (-1, 0); null is the record's
		// 「null」 (ShipParameterElement.LoadLine), though its properties are not marked so
		int[] equipment = Slots.Select(s => s.Equipment).ToArray();
		int[] aircraft = Slots.Select(s => s.Aircraft).ToArray();
		record.DefaultSlot = (Original.DefaultSlot is null && equipment.All(id => id == -1) ? null : equipment)!;
		record.Aircraft = (Original.Aircraft is null && aircraft.All(count => count == 0) ? null : aircraft)!;

		record.MessageGet = RecordText(MessageGet, Original.MessageGet)!;
		record.MessageAlbum = RecordText(MessageAlbum, Original.MessageAlbum)!;
		record.ResourceName = (ResourceName == (Original.ResourceName ?? "") ? Original.ResourceName : NullWhenEmpty(ResourceName))!;
		record.ResourceGraphicVersion = RecordVersion(GraphicVersion, Original.ResourceGraphicVersion)!;
		record.ResourceVoiceVersion = RecordVersion(VoiceVersion, Original.ResourceVoiceVersion)!;
		record.ResourcePortVoiceVersion = RecordVersion(PortVoiceVersion, Original.ResourcePortVoiceVersion)!;
		record.OriginalCostumeShipID = OriginalCostumeShipId;

		RecordManager.Instance.ShipParameter[ShipId] = record;
	}

	// the record writes a line break as the game, 「<br>」
	private static string MessageText(string? message) => message?.Replace("<br>", "\r\n") ?? "";

	private static string? RecordText(string text, string? original)
	{
		string message = text.Replace("\r\n", "<br>").Replace("\n", "<br>").Replace("\r", "<br>");
		return message == (original ?? "") ? original : NullWhenEmpty(message);
	}

	private static string? NullWhenEmpty(string text) => text.Length == 0 ? null : text;

	// a version is a number in the record's text; none (or one that is not a number) shows 0 and stays while it is 0
	private static int VersionNumber(string? version) =>
		int.TryParse(version, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ? number : 0;

	private static string? RecordVersion(int number, string? original) =>
		number == VersionNumber(original) ? original : number.ToString(CultureInfo.InvariantCulture);
}
