using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using KCViewer.Core;
using KCViewer.Data;
using KCViewer.Data.ShipGroup;
using KCViewer.Utility.Data;
using KCViewer.Utility.Mathematics;
using Operator = KCViewer.Data.ShipGroup.ExpressionData.ExpressionOperator;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: a part of the ship group's filter in KanColleViewer's style (mockup 20261019-group-filter-v6, approved
/// 2026-10-19 "구현"): a block (괄호) of blocks and conditions, or a condition. The blocks go in the viewer's
/// <see cref="ExpressionManager"/> (<see cref="ExpressionManager.ForkBlocks"/>) and replace its lists while they are set,
/// so a group's copy and the filter's import and export (the clipboard's XML) carry them too.
/// </summary>
[DataContract(Name = "KcvFilterItem", Namespace = Namespace)]
[KnownType(typeof(KcvFilterBlock))]
[KnownType(typeof(KcvFilterCondition))]
public abstract class KcvFilterItem
{
	/// <summary>Fixed, so the saved groups still read if the classes move.</summary>
	public const string Namespace = "http://schemas.datacontract.org/2004/07/KCViewer.Fork.KcvViews";

	public abstract bool IsEnabled { get; }

	public abstract Expression Compile(ParameterExpression ship);

	/// <summary>The expression in words (「(레벨 ≥ 99 AND 잠금 = ○)」).</summary>
	public abstract string ToText();

	public abstract KcvFilterItem CloneItem();
}

/// <summary>
/// A block: its enabled items joined by AND or OR (true when it has none), NOT before it when <see cref="Not"/>, as the
/// viewer's list of conditions but with blocks inside blocks. The outermost block is the whole filter.
/// </summary>
[DataContract(Name = "KcvFilterBlock", Namespace = Namespace)]
public sealed class KcvFilterBlock : KcvFilterItem
{
	[DataMember(Order = 0)]
	public bool Enabled { get; set; } = true;

	[DataMember(Order = 1)]
	public bool And { get; set; } = true;

	[DataMember(Order = 2)]
	public bool Not { get; set; }

	[DataMember(Order = 3)]
	public List<KcvFilterItem> Children { get; set; } = [];

	public override bool IsEnabled => Enabled;

	// the serializer makes the object without its constructor
	[OnDeserializing]
	private void Defaults(StreamingContext context)
	{
		Enabled = true;
		And = true;
		Children = [];
	}

	public override Expression Compile(ParameterExpression ship)
	{
		Expression? expression = null;

		foreach (KcvFilterItem item in Children.Where(i => i.IsEnabled))
		{
			Expression next = item.Compile(ship);
			expression = expression is null ? next
				: And ? Expression.AndAlso(expression, next)
				: Expression.OrElse(expression, next);
		}

		expression ??= Expression.Constant(true);

		return Not ? Expression.Not(expression) : expression;
	}

	public override string ToText() => Children.Any(i => i.IsEnabled)
		? (Not ? "NOT (" : "(") + InnerText() + ")"
		: (Not ? "NOT " : "") + ForkStrings.FilterNone;

	/// <summary>The items in words without the block's own brackets: the whole filter's text.</summary>
	public string InnerText()
	{
		List<string> texts = Children.Where(i => i.IsEnabled).Select(i => i.ToText()).ToList();

		return texts.Count == 0 ? ForkStrings.FilterNone : string.Join(And ? " AND " : " OR ", texts);
	}

	public override KcvFilterItem CloneItem() => CloneBlock();

	public KcvFilterBlock CloneBlock() => new()
	{
		Enabled = Enabled,
		And = And,
		Not = Not,
		Children = Children.Select(i => i.CloneItem()).ToList(),
	};

	/// <summary>
	/// The blocks of a filter saved only in the viewer's lists: a block for each list (its conditions, AND or OR, NOT
	/// when inverted) inside the whole filter. The viewer joins the enabled lists from the first on, each with the
	/// previous result by its own AND or OR (the first one's is not used); a change between them closes the lists so
	/// far in a block of their own, as the viewer's order of joining. Disabled lists go at the end, still disabled.
	/// </summary>
	public static KcvFilterBlock FromLists(ExpressionManager manager)
	{
		List<ExpressionList> lists = manager.Expressions ?? [];

		KcvFilterBlock root = new();

		foreach (ExpressionList list in lists.Where(l => l.Enabled))
		{
			KcvFilterBlock block = FromList(list);

			if (root.Children.Count == 0)
			{
				root.Children.Add(block);
			}
			else if (root.Children.Count == 1 || root.And == list.ExternalAnd)
			{
				root.And = list.ExternalAnd;
				root.Children.Add(block);
			}
			else
			{
				root = new KcvFilterBlock { And = list.ExternalAnd, Children = [root, block] };
			}
		}

		foreach (ExpressionList list in lists.Where(l => !l.Enabled))
		{
			KcvFilterBlock block = FromList(list);
			block.Enabled = false;
			root.Children.Add(block);
		}

		return root;
	}

	private static KcvFilterBlock FromList(ExpressionList list) => new()
	{
		And = list.InternalAnd,
		Not = list.Inverse,
		Children = (list.Expressions ?? [])
			.Select(e => (KcvFilterItem)new KcvFilterCondition { Data = e.Clone() })
			.ToList(),
	};
}

/// <summary>A condition: the viewer's own (item, comparison, value, enabled).</summary>
[DataContract(Name = "KcvFilterCondition", Namespace = Namespace)]
public sealed class KcvFilterCondition : KcvFilterItem
{
	[DataMember(Order = 0)]
	public ExpressionData Data { get; set; } = new(".MasterID", Operator.Equal, 0);

	public override bool IsEnabled => Data.Enabled;

	/// <summary>
	/// The viewer's expression, but 「HP1당」 (a time) against its value in milliseconds as the viewer stores times: the
	/// viewer's own compared a time with a number and failed (user's answer 2026-10-19 "2.고침").
	/// </summary>
	public override Expression Compile(ParameterExpression ship)
	{
		if (Data.RightOperand is int milliseconds && KcvFilterFields.StaticType(Data.LeftOperand) == typeof(TimeSpan))
		{
			Expression time = Expression.Property(KcvFilterFields.Member(ship, Data.LeftOperand), nameof(TimeSpan.TotalMilliseconds));
			Expression value = Expression.Constant((double)milliseconds);

			return Data.Operator switch
			{
				Operator.Equal => Expression.Equal(time, value),
				Operator.NotEqual => Expression.NotEqual(time, value),
				Operator.LessThan => Expression.LessThan(time, value),
				Operator.LessEqual => Expression.LessThanOrEqual(time, value),
				Operator.GreaterThan => Expression.GreaterThan(time, value),
				Operator.GreaterEqual => Expression.GreaterThanOrEqual(time, value),
				_ => Data.Compile(ship),
			};
		}

		return Data.Compile(ship);
	}

	public override string ToText()
	{
		string item = KcvFilterFields.Get(Data.LeftOperand).Name;
		string value = KcvFilterFields.ValueText(Data.LeftOperand, Data.RightOperand);

		return Data.Operator switch
		{
			Operator.Contains => string.Format(ForkStrings.FilterTextContains, item, value),
			Operator.NotContains => string.Format(ForkStrings.FilterTextNotContains, item, value),
			Operator.BeginWith => string.Format(ForkStrings.FilterTextBeginWith, item, value),
			Operator.NotBeginWith => string.Format(ForkStrings.FilterTextNotBeginWith, item, value),
			Operator.EndWith => string.Format(ForkStrings.FilterTextEndWith, item, value),
			Operator.NotEndWith => string.Format(ForkStrings.FilterTextNotEndWith, item, value),
			Operator.ArrayContains => string.Format(ForkStrings.FilterTextHas, item, value),
			Operator.ArrayNotContains => string.Format(ForkStrings.FilterTextHasNot, item, value),
			// a text compared whole in brackets too, as the comparisons of a part
			_ when Data.RightOperand is string => $"{item} {KcvFilterOperators.Get(Data.Operator).Symbol} 「{value}」",
			_ => $"{item} {KcvFilterOperators.Get(Data.Operator).Symbol} {value}",
		};
	}

	public override KcvFilterItem CloneItem() => new KcvFilterCondition { Data = Data.Clone() };
}

/// <summary>A comparison: its symbol (shown in the box and the expression) and the name in the box's list.</summary>
public sealed class KcvFilterOperator
{
	public Operator Value { get; }
	public string Symbol { get; }
	public string Name { get; }

	public KcvFilterOperator(Operator value, string symbol, string name)
	{
		Value = value;
		Symbol = symbol;
		Name = name;
	}

	public override string ToString() => Symbol;
}

/// <summary>
/// The comparisons in the user's words (2026-10-19): = 같음, ≠ 같지 않음, &lt; 미만, ≤ 이하, &gt; 초과, ≥ 이상, 포함 / 포함 안
/// 함, 접두사 / 접두사 아님, 접미사 / 접미사 아님, and 있음 / 없음 for a list (장비). The sets are the viewer's.
/// </summary>
public static class KcvFilterOperators
{
	private static KcvFilterOperator Symbol(Operator value, string symbol, string name) => new(value, symbol, $"{symbol} {name}");
	private static KcvFilterOperator Word(Operator value, string name) => new(value, name, name);

	public static IReadOnlyList<KcvFilterOperator> All { get; } =
	[
		Symbol(Operator.Equal, "=", ForkStrings.FilterOpEqual),
		Symbol(Operator.NotEqual, "≠", ForkStrings.FilterOpNotEqual),
		Symbol(Operator.LessThan, "<", ForkStrings.FilterOpLessThan),
		Symbol(Operator.LessEqual, "≤", ForkStrings.FilterOpLessEqual),
		Symbol(Operator.GreaterThan, ">", ForkStrings.FilterOpGreaterThan),
		Symbol(Operator.GreaterEqual, "≥", ForkStrings.FilterOpGreaterEqual),
		Word(Operator.Contains, ForkStrings.FilterOpContains),
		Word(Operator.NotContains, ForkStrings.FilterOpNotContains),
		Word(Operator.BeginWith, ForkStrings.FilterOpBeginWith),
		Word(Operator.NotBeginWith, ForkStrings.FilterOpNotBeginWith),
		Word(Operator.EndWith, ForkStrings.FilterOpEndWith),
		Word(Operator.NotEndWith, ForkStrings.FilterOpNotEndWith),
		Word(Operator.ArrayContains, ForkStrings.FilterOpHas),
		Word(Operator.ArrayNotContains, ForkStrings.FilterOpHasNot),
	];

	public static KcvFilterOperator Get(Operator value) => All.First(o => o.Value == value);

	private static IReadOnlyList<KcvFilterOperator> Of(params Operator[] values) => values.Select(Get).ToList();

	public static IReadOnlyList<KcvFilterOperator> Bool { get; } = Of(Operator.Equal, Operator.NotEqual);

	public static IReadOnlyList<KcvFilterOperator> Number { get; } = Of(Operator.Equal, Operator.NotEqual,
		Operator.LessThan, Operator.LessEqual, Operator.GreaterThan, Operator.GreaterEqual);

	public static IReadOnlyList<KcvFilterOperator> Text { get; } = Of(Operator.Equal, Operator.NotEqual,
		Operator.Contains, Operator.NotContains, Operator.BeginWith, Operator.NotBeginWith, Operator.EndWith, Operator.NotEndWith);

	public static IReadOnlyList<KcvFilterOperator> Array { get; } = Of(Operator.ArrayContains, Operator.ArrayNotContains);
}

/// <summary>How an item's value is put in: a whole number, a number with decimals, a choice, a ship's name (a list or
/// typed), a text, a time (분:초), or nothing (an item the viewer does not know, kept as it is).</summary>
public enum KcvFilterValueKind
{
	Number,
	Decimal,
	Choice,
	Name,
	Text,
	Time,
	Unknown,
}

/// <summary>A choice of a value: the value saved and its name.</summary>
public sealed record KcvFilterChoice(object Value, string Name)
{
	public override string ToString() => Name;
}

/// <summary>An item of the filter (항목): the viewer's item with how its value is put in and compared.</summary>
public sealed class KcvFilterField
{
	public string Path { get; init; } = "";
	public string Name { get; init; } = "";
	public KcvFilterValueKind Kind { get; init; }
	public IReadOnlyList<KcvFilterOperator> Operators { get; init; } = KcvFilterOperators.Bool;
	public IReadOnlyList<KcvFilterChoice> Choices { get; init; } = [];
	public double Minimum { get; init; }
	public double Maximum { get; init; }
	public int Decimals { get; init; }
	public object DefaultValue { get; init; } = 0;

	/// <summary>The width of the value's box (a number, or a choice of long names).</summary>
	public double ValueWidth { get; init; } = 70;

	public override string ToString() => Name;
}

/// <summary>
/// The items in the viewer's order (<see cref="ExpressionData.LeftOperandNameTable"/>), with the viewer's special cases
/// (함명, 함 종류, 장비, 장비 가능 목록, 사거리, 속력, 희귀도) and number ranges. The type of a value comes from the
/// item's definition (the properties of <see cref="ShipData"/>) instead of the viewer's first ship, so a condition can be
/// added with no ships (user's answer 2026-10-19 "2.고침"); 「HP1당」 is put in as a time.
/// </summary>
public static class KcvFilterFields
{
	private static readonly Regex PathPattern = new(@"\.(?<name>\w+)(\[(?<index>\d+?)\])?", RegexOptions.Compiled);

	private static IReadOnlyList<KcvFilterField>? _all;
	private static Dictionary<string, KcvFilterField>? _byPath;

	/// <summary>Made again on the next use, with the lists of ships and equipment of the master data now.</summary>
	public static void Refresh()
	{
		_all = null;
		_byPath = null;
	}

	/// <summary>Made when first used: the lists of ships and equipment need the master data.</summary>
	public static IReadOnlyList<KcvFilterField> All
	{
		get
		{
			if (_all is null)
			{
				_all = ExpressionData.LeftOperandNameTable.Select(p => Make(p.Key, p.Value)).ToList();
				_byPath = _all.ToDictionary(f => f.Path);
			}

			return _all;
		}
	}

	/// <summary>The item of a path; one the viewer does not know is kept with its path as its name.</summary>
	public static KcvFilterField Get(string path)
	{
		_ = All;

		return _byPath!.TryGetValue(path, out KcvFilterField? field) ? field : new KcvFilterField
		{
			Path = path,
			Name = path,
			Kind = KcvFilterValueKind.Unknown,
			Operators = KcvFilterOperators.All,
		};
	}

	/// <summary>The type of an item's value from the properties of <see cref="ShipData"/>; null when it has none.</summary>
	public static Type? StaticType(string path)
	{
		Type? type = typeof(ShipData);

		foreach (Match match in PathPattern.Matches(path))
		{
			if (type is null) return null;

			type = FindProperty(type, match.Groups["name"].Value)?.PropertyType;

			if (type is not null && match.Groups["index"].Success)
			{
				type = type.IsArray ? type.GetElementType() : FindProperty(type, "Item")?.PropertyType;
			}
		}

		return type == typeof(ShipData) ? null : type;
	}

	/// <summary>The element of a list (장비, 장비 가능 목록), else null.</summary>
	private static Type? ElementType(Type type)
	{
		if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type)) return null;
		if (type.IsArray) return type.GetElementType();

		return type.GetInterfaces().Append(type)
			.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			?.GetGenericArguments()[0];
	}

	private static PropertyInfo? FindProperty(Type type, string name)
	{
		PropertyInfo? property = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.FirstOrDefault(p => p.Name == name && (name != "Item" || p.GetIndexParameters().Length == 1));

		return property ?? type.GetInterfaces()
			.SelectMany(i => i.GetProperties())
			.FirstOrDefault(p => p.Name == name && (name != "Item" || p.GetIndexParameters().Length == 1));
	}

	/// <summary>The item's value of a ship, as the viewer's expression reads it.</summary>
	public static Expression Member(ParameterExpression ship, string path)
	{
		Expression member = ship;

		foreach (Match match in PathPattern.Matches(path))
		{
			member = Expression.PropertyOrField(member, match.Groups["name"].Value);

			if (int.TryParse(match.Groups["index"].Value, out int index))
			{
				member = Expression.Property(member, "Item", Expression.Constant(index, typeof(int)));
			}
		}

		return member;
	}

	private static KcvFilterField Make(string path, string name)
	{
		Type? type = StaticType(path);
		Type? element = type is null ? null : ElementType(type);
		bool isList = element is not null;
		if (isList) type = element;

		IReadOnlyList<KcvFilterChoice> names = path == ".MasterShip.NameWithClass" ? ShipNames() : [];

		KcvFilterField field = path switch
		{
			".MasterShip.NameWithClass" => new()
			{
				Kind = KcvFilterValueKind.Name,
				Operators = KcvFilterOperators.Text,
				Choices = names,
				DefaultValue = names.FirstOrDefault()?.Name ?? "",
				ValueWidth = 180,
			},
			".MasterShip.ShipType" => Choice(KcvFilterOperators.Bool, ShipTypes(), 2, 140),
			_ when path.Contains("SlotMaster") => Choice(KcvFilterOperators.Bool, Equipment(), 1, 220),
			".MasterShip.EquippableCategories" => Choice(KcvFilterOperators.Array, EquipmentCategories(), 1, 220),
			".Range" => Choice(KcvFilterOperators.Number, Enumerable.Range(0, 5).Select(i => new KcvFilterChoice(i, Constants.GetRange(i))).ToList(), 1, 96),
			".Speed" or ".MasterShip.Speed" => Choice(KcvFilterOperators.Number,
				new[] { 0, 5, 10, 15, 20 }.Select(i => new KcvFilterChoice(i, Constants.GetSpeed(i))).ToList(), 10, 96),
			".MasterShip.Rarity" => Choice(KcvFilterOperators.Number,
				Enumerable.Range(1, 8).Select(i => new KcvFilterChoice(i, Constants.GetShipRarity(i))).ToList(), 1, 96),
			".RepairTimeUnit" => new() { Kind = KcvFilterValueKind.Time, Operators = KcvFilterOperators.Number, DefaultValue = 0 },
			_ when type == typeof(int) => Number(path),
			_ when type == typeof(double) => Rate(path),
			_ when type == typeof(bool) => Choice(KcvFilterOperators.Bool, [new(true, "○"), new(false, "×")], true, 56),
			_ when type is { IsEnum: true } => Choice(KcvFilterOperators.Bool, type.GetEnumNames()
				.Select(n => new KcvFilterChoice(Convert.ToInt32(Enum.Parse(type, n), CultureInfo.InvariantCulture), n)).ToList(), 0, 140),
			_ when type is null => new() { Kind = KcvFilterValueKind.Unknown, Operators = KcvFilterOperators.All },
			_ => new() { Kind = KcvFilterValueKind.Text, Operators = KcvFilterOperators.Text, DefaultValue = "", ValueWidth = 160 },
		};

		return new KcvFilterField
		{
			Path = path,
			Name = name,
			Kind = field.Kind,
			Operators = isList ? KcvFilterOperators.Array : field.Operators,
			Choices = field.Choices,
			Minimum = field.Minimum,
			Maximum = field.Maximum,
			Decimals = field.Decimals,
			DefaultValue = field.DefaultValue,
			ValueWidth = field.ValueWidth,
		};
	}

	private static KcvFilterField Choice(IReadOnlyList<KcvFilterOperator> operators, IReadOnlyList<KcvFilterChoice> choices,
		object defaultValue, double width) => new()
	{
		Kind = KcvFilterValueKind.Choice,
		Operators = operators,
		Choices = choices,
		DefaultValue = defaultValue,
		ValueWidth = width,
	};

	/// <summary>The viewer's ranges of whole numbers.</summary>
	private static KcvFilterField Number(string path)
	{
		(int minimum, int maximum) = path switch
		{
			".MasterID" => (0, 999999),
			".Level" => (1, ExpTable.ShipMaximumLevel),
			".ExpTotal" or ".ExpNextRemodel" => (0, 4360000),
			".ExpNext" => (0, 195000),
			".HPCurrent" or ".HPMax" => (0, 999),
			".Condition" => (0, 100),
			".RepairingDockID" => (-1, 4),
			".RepairTime" => (0, int.MaxValue),
			".SlotSize" => (0, 5),
			".MasterShip.SortID" => (0, 99999),
			_ => (0, 9999),
		};

		return new()
		{
			Kind = KcvFilterValueKind.Number,
			Operators = KcvFilterOperators.Number,
			Minimum = minimum,
			Maximum = maximum,
			DefaultValue = minimum,
			ValueWidth = path == ".RepairTime" ? 96 : 70,
		};
	}

	/// <summary>The viewer's numbers with decimals: the rates from 0 to 1 by 0.01, the powers whole.</summary>
	private static KcvFilterField Rate(string path)
	{
		bool rate = path.Contains("Rate");

		return new()
		{
			Kind = KcvFilterValueKind.Decimal,
			Operators = KcvFilterOperators.Number,
			Minimum = rate ? 0 : int.MinValue,
			Maximum = rate ? 1 : int.MaxValue,
			Decimals = rate ? 2 : 0,
			DefaultValue = 0.0,
		};
	}

	private static IReadOnlyList<KcvFilterChoice> ShipNames() => KCDatabase.Instance.MasterShips.Values
		.Where(s => !s.IsAbyssalShip)
		.OrderBy(s => s.NameWithClass)
		.OrderBy(s => s.NameReading)
		.Select(s => new KcvFilterChoice(s.NameEN, s.NameEN))
		.ToList();

	private static IReadOnlyList<KcvFilterChoice> ShipTypes() => KCDatabase.Instance.MasterShips.Values
		.Where(s => !s.IsAbyssalShip)
		.Select(s => (int)s.ShipType)
		.Distinct()
		.OrderBy(i => i)
		.Select(i => KCDatabase.Instance.ShipTypes[i])
		.Where(t => t is not null)
		.Select(t => new KcvFilterChoice(t.TypeID, t.NameEN))
		.ToList();

	private static IReadOnlyList<KcvFilterChoice> Equipment() =>
	[
		new(-1, ForkStrings.FilterValueNone),
		.. KCDatabase.Instance.MasterEquipments.Values
			.Where(e => !e.IsAbyssalEquipment)
			.OrderBy(e => e.CategoryType)
			.Select(e => new KcvFilterChoice(e.EquipmentID, e.NameEN)),
		new(0, ForkStrings.FilterValueLocked),
	];

	private static IReadOnlyList<KcvFilterChoice> EquipmentCategories() => KCDatabase.Instance.MasterEquipments.Values
		.Select(e => (int)e.CategoryType)
		.Distinct()
		.OrderBy(i => i)
		.Select(i => KCDatabase.Instance.EquipmentTypes[i])
		.Where(t => t is not null)
		.Select(t => new KcvFilterChoice(t.TypeID, t.NameEN))
		.ToList();

	/// <summary>A rate as the viewer shows it in Korean (「100%」, the invariant culture puts a space before %).</summary>
	private static string RateText(double rate) => (rate * 100).ToString("0", CultureInfo.InvariantCulture) + "%";

	/// <summary>A time of 「HP1당」 as typed (분:초) from milliseconds.</summary>
	public static string TimeText(int milliseconds)
	{
		TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);

		return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
	}

	/// <summary>Milliseconds of 「분:초」 or 「시:분:초」.</summary>
	public static int? ParseTime(string text)
	{
		string[] parts = text.Trim().Split(':');
		if (parts.Length is < 2 or > 3) return null;

		int[] numbers = new int[parts.Length];
		for (int i = 0; i < parts.Length; i++)
		{
			if (!int.TryParse(parts[i].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i])) return null;
		}

		// the seconds, and the minutes after hours, under 60
		if (numbers[^1] >= 60 || (numbers.Length == 3 && numbers[1] >= 60)) return null;

		double seconds = numbers.Length == 3
			? numbers[0] * 3600.0 + numbers[1] * 60 + numbers[2]
			: numbers[0] * 60.0 + numbers[1];

		return seconds * 1000 > int.MaxValue ? null : (int)(seconds * 1000);
	}

	/// <summary>A value in words for the expression, the viewer's in Korean (재적 없음, 없음, 미개방 …).</summary>
	public static string ValueText(string path, object? value)
	{
		if (value is null) return "";

		KcvFilterField field = Get(path);

		if (field.Kind is KcvFilterValueKind.Choice && field.Choices.FirstOrDefault(c => Equals(c.Value, value)) is { } choice)
		{
			return choice.Name;
		}

		switch (path)
		{
			case ".MasterID" when value is int id:
				return KCDatabase.Instance.Ships[id] is { } ship ? $"{id} ({ship.NameWithLevel})" : $"{id} {ForkStrings.FilterValueNotEnrolled}";

			case ".ShipID" when value is int id:
				return KCDatabase.Instance.MasterShips[id] is { } master ? $"{id} ({master.NameWithClass})" : $"{id} {ForkStrings.FilterValueNone}";

			case ".MasterShip.ShipType" when value is int id:
				return KCDatabase.Instance.ShipTypes[id]?.NameEN ?? $"{id} {ForkStrings.FilterValueNone}";

			case ".MasterShip.EquippableCategories" when value is int id:
				return KCDatabase.Instance.EquipmentTypes[id]?.NameEN ?? $"{id} {ForkStrings.FilterValueNone}";

			case ".MasterShip.AlbumNo" when value is int number:
				return KCDatabase.Instance.MasterShips.Values.FirstOrDefault(s => s.AlbumNo == number) is { } album
					? $"{number} ({album.NameWithClass})"
					: $"{number} {ForkStrings.FilterValueNone}";

			case ".MasterShip.RemodelAfterShipID" or ".MasterShip.RemodelBeforeShipID" when value is int id:
				if (id == 0) return path.Contains("After") ? ForkStrings.FilterValueLastRemodel : ForkStrings.FilterValueNoRemodelBefore;

				return KCDatabase.Instance.MasterShips[id] is { } remodel ? $"{id} ({remodel.NameWithClass})" : $"{id} {ForkStrings.FilterValueNone}";

			case ".RepairTime" when value is int milliseconds:
				return DateTimeHelper.ToTimeRemainString(DateTimeHelper.FromAPITimeSpan(milliseconds));

			case ".RepairTimeUnit" when value is int milliseconds:
				return TimeText(milliseconds);
		}

		if (path.Contains("SlotMaster") && value is int equipment)
		{
			return equipment switch
			{
				-1 => ForkStrings.FilterValueNone,
				0 => ForkStrings.FilterValueLocked,
				_ => KCDatabase.Instance.MasterEquipments[equipment]?.NameEN ?? $"{equipment} {ForkStrings.FilterValueNone}",
			};
		}

		if (path.Contains("Rate") && value is double rate) return RateText(rate);
		if (value is bool flag) return flag ? "○" : "×";

		return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
	}

	/// <summary>The viewer's note beside a value (in Korean), or empty.</summary>
	public static string Description(string path, object? value)
	{
		if (path.Contains("Rate") && value is double rate) return RateText(rate);
		if (value is not int number) return "";

		switch (path)
		{
			case ".MasterID":
				return KCDatabase.Instance.Ships[number]?.NameWithLevel ?? ForkStrings.FilterValueNotEnrolled;

			case ".ShipID":
				return KCDatabase.Instance.MasterShips[number] is { } master ? $"{master.ShipTypeName} {master.NameWithClass}" : ForkStrings.FilterValueNone;

			case ".MasterShip.AlbumNo":
				return KCDatabase.Instance.MasterShips.Values.FirstOrDefault(s => s.AlbumNo == number) is { } album
					? $"{album.ShipTypeName} {album.NameWithClass}"
					: ForkStrings.FilterValueNone;

			case ".RepairingDockID":
				return ForkStrings.FilterDockNote;

			case ".RepairTime":
				return string.Format(ForkStrings.FilterMillisecondsNote, DateTimeHelper.ToTimeRemainString(DateTimeHelper.FromAPITimeSpan(number)));

			case ".MasterShip.RemodelBeforeShipID":
			{
				if (number == 0) return ForkStrings.FilterValueNoRemodelBefore;
				if (KCDatabase.Instance.MasterShips[number] is not { } before) return ForkStrings.FilterValueNone;

				return $"{before.NameWithClass} ← {before.RemodelBeforeShip?.NameWithClass ?? "×"}";
			}

			case ".MasterShip.RemodelAfterShipID":
			{
				if (number == 0) return ForkStrings.FilterValueLastRemodel;
				if (KCDatabase.Instance.MasterShips[number] is not { } after) return ForkStrings.FilterValueNone;

				return $"{after.NameWithClass} → {after.RemodelAfterShip?.NameWithClass ?? "×"}";
			}
		}

		return "";
	}
}
