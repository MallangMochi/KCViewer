using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Database.Sortie;
using KCViewer.Window.Dialog.QuestTrackerManager.Enums;
using KCViewer.Window.Tools.DropRecordViewer;
using KCViewer.Window.Tools.ExpeditionRecordViewer;
using KCViewer.Window.Tools.SenkaViewer;
using KCViewer.Window.Tools.SortieRecordViewer;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: converters of the record windows in KanColleViewer's style (KcvRecordStyles.xaml).</summary>
public abstract class KcvOneWayConverter : IValueConverter
{
	public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>Every record window writes times as 2026-10-03 02:28:26, 24-hour (user's choice 2026-10-04).</summary>
public sealed class KcvRecordTimeConverter : KcvOneWayConverter
{
	public static string Format(DateTime time) =>
		(time.Kind is DateTimeKind.Utc ? time.ToLocalTime() : time).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		DateTime time => Format(time),
		// the development and construction rows only give their date as text, 2026/10/01 09:31:46
		string text => text.Replace('/', '-'),
		_ => "",
	};
}

/// <summary>A sortie's or expedition's fleet as ship chips (name and level).</summary>
public sealed class KcvRecordFleetConverter : KcvOneWayConverter
{
	private static readonly ConditionalWeakTable<object, List<KcvRecordShip>> Cache = new();

	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		SortieFleet? fleet = value switch
		{
			SortieRecordViewModel sortie => KcvRecordFleets.SortieFleet(sortie.Model),
			ExpeditionRecordViewModel expedition => expedition.Model.Fleet,
			SortieFleet f => f,
			_ => null,
		};
		if (fleet is null) return null;

		return Cache.GetValue(fleet, f => KcvRecordFleets.Ships((SortieFleet)f));
	}
}

/// <summary>The route of a sortie (sortie ID, <see cref="KcvSortieRoutes.Version"/>) once it has been read.</summary>
public sealed class KcvRouteConverter : IMultiValueConverter
{
	public KcvSortieRoutes? Routes { get; set; }

	public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture) =>
		values.FirstOrDefault() is int id ? Routes?.Get(id) : null;

	public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}

/// <summary>An expedition ID → its (translated) name, shown grey after the number.</summary>
public sealed class KcvExpeditionNameConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is int id ? KCDatabase.Instance.Mission.Values.FirstOrDefault(m => m.MissionID == id)?.NameEN : null;
}

/// <summary>The viewer's result text → Great, Success or Failure for the badge colours.</summary>
public sealed class KcvExpeditionResultConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not string result) return "";
		if (result == Constants.GetExpeditionResult(2)) return "Great";
		if (result == Constants.GetExpeditionResult(1)) return "Success";
		if (result == Constants.GetExpeditionResult(0)) return "Failure";
		return "";
	}
}

/// <summary>
/// The viewer's count texts 「47 (35.3%)」 and 「1/2 (50.0%)」 in two parts, the number (parameter main) and the grey
/// rate (parameter rate).
/// </summary>
public sealed class KcvCountPartConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not string text) return "";

		int split = text.IndexOf(" (", StringComparison.Ordinal);
		if (split < 0) return parameter as string == "rate" ? "" : text;

		return parameter as string == "rate" ? text[(split + 1)..] : text[..split];
	}
}

/// <summary>True for a count text that starts with 0 (「0 (0.0%)」, 「0/0 (0.0%)」), which is shown grey.</summary>
public sealed class KcvZeroCountConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is string text && (text.StartsWith("0 ", StringComparison.Ordinal) || text.StartsWith("0/", StringComparison.Ordinal));
}

/// <summary>Shown when there is a value (the expedition record's sums of the selected rows).</summary>
public sealed class KcvNotNullConverter : KcvOneWayConverter
{
	public static KcvNotNullConverter Visible { get; } = new();

	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is null ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
}

/// <summary>True for a number that is 0 (faded in the tables).</summary>
public sealed class KcvIsZeroConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		int i => i == 0,
		double d => Math.Abs(d) < 0.005,
		_ => false,
	};
}

/// <summary>+8.19, -8.19 and 0 (the viewer's +0.## wrote -+8.19 for a loss).</summary>
public sealed class KcvSignedConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		double d => d.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture),
		int i => i.ToString("+0;-0;0", CultureInfo.InvariantCulture),
		_ => "",
	};
}

/// <summary>A total of the senka cards: 179.33, -8.19, 0.</summary>
public sealed class KcvNumberConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		double d => d.ToString("0.##", CultureInfo.InvariantCulture),
		_ => "",
	};
}

/// <summary>A recipe 「30/30/30/30/1」 → its parts (null when the text is not a recipe).</summary>
public sealed class KcvRecipeConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		KcvRecipes.Parts(value as string);
}

/// <summary>True for a large construction recipe (the 「대형」 tag).</summary>
public sealed class KcvLargeRecipeConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		KcvRecipes.IsLarge(value as string);
}

/// <summary>A drop table row → where the drop was (raw rows; merged rows grouped by node when a ship is chosen).</summary>
public sealed class KcvDropMapConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		DropRecordRow { ForkElement: { } drop } => KcvDropMaps.From(drop),
		MergedDropRecordRow { ForkMapSerialId: not 0 } row => KcvDropMaps.FromSerialId(row.ForkMapSerialId),
		_ => null,
	};
}

/// <summary>A ship → its type, shown grey after the name (nothing for 「(없음)」 and 「(만원)」).</summary>
public sealed class KcvShipTypeNameConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		ShipId id when (int)id > 0 => KCDatabase.Instance.MasterShips[(int)id]?.ShipTypeName,
		_ => null,
	};
}

/// <summary>A battle rank → S, A or Loss for its colour (B has the normal colour).</summary>
public sealed class KcvRankKindConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
	{
		BattleRank.SS or BattleRank.S => "S",
		BattleRank.A => "A",
		BattleRank.C or BattleRank.D or BattleRank.E => "Loss",
		_ => "",
	};
}

/// <summary>
/// The development table's equipment (its sort key: equipment ID + 1000 × category) → the game's icon; nothing for
/// a failure or a recipe.
/// </summary>
public sealed class KcvDevelopmentIconConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not int key || key % 1000 <= 0) return null;

		IEquipmentDataMaster? equipment = KCDatabase.Instance.MasterEquipments[key % 1000];
		return equipment is null ? null : KcvGameIcons.Get(equipment.IconTypeTyped);
	}
}

/// <summary>A senka session → 「2026-10-02 14:00:00 ~ 2026-10-03 02:00:00」 for the tooltip of its end time.</summary>
public sealed class KcvSenkaIntervalConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is SenkaRecord record ? $"{KcvRecordTimeConverter.Format(record.Start)} ~ {KcvRecordTimeConverter.Format(record.End)}" : null;
}

/// <summary>True for the session that has not ended yet (the 「진행 중」 tag).</summary>
public sealed class KcvSenkaInProgressConverter : KcvOneWayConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is SenkaRecord record && record.Start <= DateTime.UtcNow && DateTime.UtcNow < record.End;
}
