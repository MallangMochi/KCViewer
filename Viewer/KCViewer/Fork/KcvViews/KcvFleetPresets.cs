using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Utility.Data;
using KCViewer.ViewModels.Translations;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// A piece of equipment in a ship's tooltip: the game icon and the viewer's text ("[18/18] name★+n"); equipment in the
/// reinforcement slot comes under a line (mockup 20261010-ex-slot-separator-v1).
/// </summary>
public sealed record KcvPresetEquipment(EquipmentIconType Icon, string Text, bool IsExpansion = false);

/// <summary>
/// A ship of a preset; Header is the viewer's first three tooltip lines (type and name, HP, cond). NameMaxWidth: the
/// fleet panels' fixed name width setting, which the viewer's preset panel follows too.
/// </summary>
public sealed record KcvPresetShip(string Name, string LevelText, string Header, List<KcvPresetEquipment> Equipment, double NameMaxWidth);

/// <summary>A ship cell: a ship, a dash (no ship in that place) or nothing (an empty preset slot).</summary>
public sealed record KcvPresetCell(KcvPresetShip? Ship, bool Dash);

/// <summary>A preset line. An empty slot has the viewer's "----" and blank cells.</summary>
public sealed record KcvPresetRow(string Name, bool Empty, int LowestCondition, string ConditionTip, List<KcvPresetCell> Cells, bool ShowSeventh);

/// <summary>
/// Fork: what the fleet preset panel in KanColleViewer's style shows, from the viewer's preset data
/// (<see cref="FleetPresetManager"/>); the same values and tooltip text as the viewer's own panel.
/// </summary>
public static class KcvFleetPresets
{
	// the viewer's panel keeps a seventh place for 7-ship fleets; it is shown only when a preset has one
	private const int Places = 7;

	/// <summary>Null before the game sent the presets (the viewer's panel is empty then).</summary>
	public static List<KcvPresetRow>? Rows()
	{
		FleetPresetManager? presets = KCDatabase.Instance.FleetPreset;
		if (presets is not { MaximumCount: > 0 }) return null;

		Utility.Configuration.ConfigurationData config = Utility.Configuration.Config;
		double maxWidth = config.FormFleet.FixShipNameWidth ? config.FormFleet.FixedShipNameWidth : double.PositiveInfinity;

		bool seventh = Enumerable.Range(1, presets.MaximumCount)
			.Select(id => presets[id])
			.Any(p => p is not null && p.MembersInstance.Skip(Places - 1).Any(s => s is not null));

		return Enumerable.Range(1, presets.MaximumCount)
			.Select(id => RowOf(presets[id], seventh, maxWidth))
			.ToList();
	}

	private static KcvPresetRow RowOf(FleetPresetData? preset, bool seventh, double maxWidth)
	{
		if (preset is null)
		{
			return new("----", true, 0, "", Enumerable.Repeat(new KcvPresetCell(null, false), Places).ToList(), seventh);
		}

		// the viewer gives null for places without a ship (-1)
		List<ShipData?> members = preset.MembersInstance.Cast<ShipData?>().ToList();

		int lowest = members
			.OfType<ShipData>()
			.Select(s => s.Condition)
			.DefaultIfEmpty(49)
			.Min();

		FormFleetPresetTranslationViewModel strings = Ioc.Default.GetRequiredService<FormFleetPresetTranslationViewModel>();

		List<KcvPresetCell> cells = Enumerable.Range(0, Places)
			.Select(i => i < members.Count && members[i] is { } ship ? new KcvPresetCell(ShipOf(ship, maxWidth), false) : new KcvPresetCell(null, true))
			.ToList();

		return new(preset.Name, false, lowest, $"{strings.LowestCondition}: {lowest}", cells, seventh);
	}

	private static KcvPresetShip ShipOf(ShipData ship, double maxWidth)
	{
		// the viewer's tooltip lines (FleetPresetItemViewModel.Update)
		string header = string.Join("\n",
			$"{ship.MasterShip.ShipTypeName} {ship.NameWithLevel}",
			$"HP: {ship.HPCurrent} / {ship.HPMax} ({ship.HPRate:p1}) [{Constants.GetDamageState(ship.HPRate)}]",
			$"cond: {ship.Condition}");

		IList<IEquipmentData?> slots = ship.AllSlotInstance;
		List<KcvPresetEquipment> equipment = [];

		for (int e = 0; e < slots.Count; e++)
		{
			if (slots[e] is not { } eq) continue;

			string text = e < ship.MasterShip.Aircraft.Count
				? $"[{ship.Aircraft[e]}/{ship.AircraftMax?[e] ?? ship.MasterShip.Aircraft[e]}] {eq.NameWithLevel}"
				: eq.NameWithLevel;

			// the reinforcement slot is the last one (ShipData.AllSlot)
			equipment.Add(new(eq.MasterEquipment.IconTypeTyped, text, e == slots.Count - 1));
		}

		return new(ship.Name, $"Lv.{ship.Level}", header, equipment, maxWidth);
	}
}
