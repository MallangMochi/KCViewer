using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Data;
using KCViewer.Data.ShipGroup;
using KCViewer.Utility;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the model of the ship group's filter in KanColleViewer's style (KcvShipGroupFilterWindow, mockup
/// 20261019-group-filter-v6). The 필터 tab shows the blocks as lines of the same height (a block's head, its
/// conditions and blocks, its 「＋ 조건」 「＋ 묶음」) beside a gutter of brackets, each with its AND/OR in the middle of
/// its left; the outermost bracket is the whole filter. The 포함/제외 목록 tab is the viewer's.
/// </summary>
public sealed class KcvShipGroupFilterViewModel : ObservableObject
{
	public const double LineHeight = 32;
	public const double BracketStep = 56;

	/// <summary>The bracket's place in its part of the gutter, after the AND/OR button.</summary>
	public const double BracketOffset = 46;

	private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

	public KcvFilterBlock Root { get; private set; }
	public List<int> Inclusion { get; private set; }
	public List<int> Exclusion { get; private set; }

	/// <summary>
	/// Made anew on each change of the blocks: a list emptied and filled again left UI Automation with the lines after
	/// the first two empty.
	/// </summary>
	public IReadOnlyList<KcvFilterLine> Lines { get; private set; } = [];
	public IReadOnlyList<KcvFilterBracket> Brackets { get; private set; } = [];
	public double GutterWidth { get; private set; }
	public double EditorHeight { get; private set; }

	public string ResultText { get; private set; } = "";
	public bool ShowsCode { get; private set; }

	/// <summary>No value is wrong (OK is enabled).</summary>
	public bool IsValid { get; private set; } = true;

	public IReadOnlyList<string> ListNames { get; } =
	[
		ShipGroupFilterResources.ConstFilterSelector_InclusionList,
		ShipGroupFilterResources.ConstFilterSelector_ExclusionList,
	];

	private int _listIndex;

	/// <summary>0 for the 포함 목록, 1 for the 제외 목록.</summary>
	public int ListIndex
	{
		get => _listIndex;
		set
		{
			_listIndex = value;
			UpdateListRows();
		}
	}

	public IReadOnlyList<KcvFilterListRow> ListRows { get; private set; } = [];

	public ICommand ToggleCodeCommand { get; }
	public ICommand OptimizeListCommand { get; }
	public ICommand ConvertListCommand { get; }
	public ICommand ClearListCommand { get; }
	public ICommand ImportCommand { get; }
	public ICommand ExportCommand { get; }

	/// <summary>Asks yes or no (the text, the title, a warning with 아니요 first); the window shows it.</summary>
	public Func<string, string, bool, bool> Confirm { get; set; } = (_, _, _) => false;

	/// <summary>Tells something (the text, the title, the icon); the window shows it.</summary>
	public Action<string, string, MessageBoxImage> Inform { get; set; } = (_, _, _) => { };

	public KcvShipGroupFilterViewModel(ShipGroupData group)
	{
		KcvFilterFields.Refresh();

		ExpressionManager expressions = group.Expressions ?? new ExpressionManager();
		Root = expressions.ForkBlocks?.CloneBlock() ?? KcvFilterBlock.FromLists(expressions);
		Inclusion = [.. group.InclusionFilter];
		Exclusion = [.. group.ExclusionFilter];

		ToggleCodeCommand = new RelayCommand(() =>
		{
			ShowsCode = !ShowsCode;
			Refresh();
		});
		OptimizeListCommand = new RelayCommand(OptimizeList);
		ConvertListCommand = new RelayCommand(ConvertList);
		ClearListCommand = new RelayCommand(ClearList);
		ImportCommand = new RelayCommand(Import);
		ExportCommand = new RelayCommand(Export);

		Rebuild();
		UpdateListRows();
	}

	/// <summary>The group with the blocks (the viewer's lists left empty) and the lists of ships.</summary>
	public ShipGroupData WriteTo(ShipGroupData group)
	{
		group.Expressions = new ExpressionManager { ForkBlocks = Root };
		group.InclusionFilter = Inclusion;
		group.ExclusionFilter = Exclusion;

		return group;
	}

	#region the blocks

	/// <summary>Makes the lines and brackets again after a change of the blocks.</summary>
	public void Rebuild()
	{
		List<KcvFilterLine> lines = [];
		List<KcvFilterBracket> brackets = [];
		int depthMax = 0;

		AddLines(Root, null, 0, "", [], lines, brackets, ref depthMax);

		Lines = lines;
		Brackets = brackets;
		GutterWidth = BracketStep * depthMax + BracketOffset + 16;
		EditorHeight = Lines.Count * LineHeight;

		Refresh();
	}

	private void AddLines(KcvFilterBlock block, KcvFilterBlock? parent, int depth, string name, List<KcvFilterBlock> path,
		List<KcvFilterLine> lines, List<KcvFilterBracket> brackets, ref int depthMax)
	{
		int first = lines.Count;
		List<KcvFilterBlock> blocks = [.. path, block];

		if (parent is not null) lines.Add(new KcvFilterHeaderLine(this, block, parent, name, blocks));

		int number = 0;
		foreach (KcvFilterItem item in block.Children)
		{
			switch (item)
			{
				case KcvFilterBlock child:
					number++;
					AddLines(child, block, depth + 1, name.Length == 0 ? $"{number}" : $"{name}-{number}", blocks, lines, brackets, ref depthMax);
					break;

				case KcvFilterCondition condition:
					lines.Add(new KcvFilterConditionLine(this, condition, block, blocks));
					break;
			}
		}

		lines.Add(new KcvFilterAddLine(this, block, blocks));
		brackets.Add(new KcvFilterBracket(this, block, blocks, depth, first, lines.Count - first));
		depthMax = Math.Max(depthMax, depth);
	}

	/// <summary>The dimmed lines and brackets, the joins, the expression and whether the values are right.</summary>
	public void Refresh()
	{
		foreach (KcvFilterLine line in Lines) line.Update();
		foreach (KcvFilterBracket bracket in Brackets) bracket.Update();

		IsValid = Lines.OfType<KcvFilterConditionLine>().All(l => l.IsValid);
		ResultText = ShowsCode ? CodeText() : Root.InnerText();
	}

	/// <summary>The viewer's expression in code, as its result box shows it when clicked.</summary>
	private string CodeText()
	{
		try
		{
			ExpressionManager manager = new() { ForkBlocks = Root };
			manager.Compile();

			return manager.ToExpressionString();
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	public void AddCondition(KcvFilterBlock block)
	{
		KcvFilterField field = KcvFilterFields.All[0];
		block.Children.Add(new KcvFilterCondition
		{
			Data = new ExpressionData(field.Path, field.Operators[0].Value, field.DefaultValue),
		});

		Rebuild();
	}

	public void AddBlock(KcvFilterBlock block)
	{
		block.Children.Add(new KcvFilterBlock());

		Rebuild();
	}

	public void Remove(KcvFilterItem item, KcvFilterBlock parent)
	{
		parent.Children.Remove(item);

		Rebuild();
	}

	public void Move(KcvFilterItem item, KcvFilterBlock parent, int step)
	{
		int index = parent.Children.IndexOf(item);
		int to = index + step;
		if (index < 0 || to < 0 || to >= parent.Children.Count) return;

		parent.Children.RemoveAt(index);
		parent.Children.Insert(to, item);

		Rebuild();
	}

	/// <summary>
	/// Another item for a condition: the item's first comparison and its default value, as the viewer's. The lines are
	/// made again after the box's change (a line's comparisons and value box are its item's).
	/// </summary>
	public void ChangeField(KcvFilterConditionLine line, KcvFilterField field)
	{
		ExpressionData data = line.Condition.Data;
		data.LeftOperand = field.Path;
		data.Operator = field.Operators[0].Value;
		data.RightOperand = field.DefaultValue;

		_dispatcher.BeginInvoke(Rebuild, DispatcherPriority.Background);
	}

	#endregion

	#region 포함/제외 목록

	private List<int> SelectedList => ListIndex == 0 ? Inclusion : Exclusion;

	private void UpdateListRows() => ListRows = SelectedList
		.Select(id => new KcvFilterListRow(this, id, KCDatabase.Instance.Ships[id]?.NameWithLevel ?? ForkStrings.FilterValueNotEnrolled))
		.ToList();

	public void RemoveFromList(int id)
	{
		SelectedList.Remove(id);
		UpdateListRows();
	}

	private void OptimizeList()
	{
		List<int> ships = SelectedList.Intersect(KCDatabase.Instance.Ships.Keys).ToList();
		if (ListIndex == 0) Inclusion = ships;
		else Exclusion = ships;

		UpdateListRows();
	}

	private void ClearList()
	{
		if (!Confirm(string.Format(ShipGroupFilterResources.ClearFilterWarning, ListNames[ListIndex]),
			ShipGroupFilterResources.ClearFilterWarningTitle, true))
		{
			return;
		}

		SelectedList.Clear();
		UpdateListRows();
	}

	/// <summary>
	/// The viewer's 식으로 변환: the 포함 목록 joined to the filter by OR (a block of 「함선 고유 ID = n」 by OR), then
	/// the 제외 목록 by AND (the same block with NOT). A join other than the filter's own closes the filter in a block.
	/// </summary>
	private void ConvertList()
	{
		if (!Confirm(ShipGroupFilterResources.ConvertToExpressionWarning, ForkStrings.FilterConfirmTitle, false)) return;

		if (Inclusion.Count > 0)
		{
			Join(IdBlock(Inclusion, false), false);
			Inclusion.Clear();
		}

		if (Exclusion.Count > 0)
		{
			Join(IdBlock(Exclusion, true), true);
			Exclusion.Clear();
		}

		Rebuild();
		UpdateListRows();
	}

	private static KcvFilterBlock IdBlock(IEnumerable<int> ids, bool not) => new()
	{
		And = false,
		Not = not,
		Children = ids
			.Select(id => (KcvFilterItem)new KcvFilterCondition
			{
				Data = new ExpressionData(".MasterID", ExpressionData.ExpressionOperator.Equal, id),
			})
			.ToList(),
	};

	private void Join(KcvFilterBlock block, bool and)
	{
		if (Root.Children.Count(c => c.IsEnabled) <= 1 || Root.And == and)
		{
			Root.And = and;
			Root.Children.Add(block);
		}
		else
		{
			Root = new KcvFilterBlock { And = and, Children = [Root, block] };
		}
	}

	#endregion

	#region 메뉴

	private void Import()
	{
		if (!Confirm(ShipGroupFilterResources.ImportFilterFromClipboard, ShipGroupFilterResources.ImportFilterFromClipboardTitle, false)) return;

		string data = Clipboard.GetText();

		if (string.IsNullOrEmpty(data))
		{
			Inform(ShipGroupFilterResources.ClipboardIsEmpty, ShipGroupFilterResources.CanNotImport, MessageBoxImage.Information);
			return;
		}

		try
		{
			using StringReader reader = new(data);

			if (new ExpressionManager().Load(reader) is not ExpressionManager imported)
			{
				throw new ArgumentException(ShipGroupFilterResources.FormatCanNotBeImported);
			}

			Root = imported.ForkBlocks ?? KcvFilterBlock.FromLists(imported);
			Rebuild();
		}
		catch (Exception ex)
		{
			Inform(ShipGroupFilterResources.FilterImportFailed + ex.Message, ShipGroupFilterResources.CanNotImport, MessageBoxImage.Error);
		}
	}

	private void Export()
	{
		try
		{
			StringBuilder text = new();
			new ExpressionManager { ForkBlocks = Root.CloneBlock() }.Save(text);

			new ClipboardService().SetTextAndLogErrors(text.ToString());

			Inform(ShipGroupFilterResources.FilterWasExported, ShipGroupFilterResources.ExportingFilterTitle, MessageBoxImage.Information);
		}
		catch (Exception ex)
		{
			Inform(ShipGroupFilterResources.FilterExportFailed + ex.Message, ShipGroupFilterResources.CanNotExport, MessageBoxImage.Error);
		}
	}

	#endregion
}

/// <summary>A line of the 필터 tab, dimmed in a disabled block (or a disabled condition).</summary>
public abstract class KcvFilterLine : ObservableObject
{
	public KcvShipGroupFilterViewModel Editor { get; }

	/// <summary>The line's block and the blocks around it, the outermost first.</summary>
	public IReadOnlyList<KcvFilterBlock> Blocks { get; }

	public bool Dimmed { get; private set; }

	protected KcvFilterLine(KcvShipGroupFilterViewModel editor, IReadOnlyList<KcvFilterBlock> blocks)
	{
		Editor = editor;
		Blocks = blocks;
	}

	public virtual void Update() => Dimmed = Blocks.Any(b => !b.Enabled);

	protected void SetDimmed(bool dimmed) => Dimmed = dimmed;
}

/// <summary>A block's head: 사용, its name (묶음 1, 묶음 1-1), NOT, ▲▼ among the items of its bracket, ✕.</summary>
public sealed class KcvFilterHeaderLine : KcvFilterLine
{
	public KcvFilterBlock Block { get; }
	public KcvFilterBlock Parent { get; }
	public string Name { get; }

	public bool Enabled
	{
		get => Block.Enabled;
		set
		{
			Block.Enabled = value;
			Editor.Refresh();
		}
	}

	public bool Not
	{
		get => Block.Not;
		set
		{
			Block.Not = value;
			Editor.Refresh();
		}
	}

	public bool CanMoveUp { get; }
	public bool CanMoveDown { get; }

	public ICommand MoveUpCommand { get; }
	public ICommand MoveDownCommand { get; }
	public ICommand RemoveCommand { get; }

	public KcvFilterHeaderLine(KcvShipGroupFilterViewModel editor, KcvFilterBlock block, KcvFilterBlock parent, string name,
		IReadOnlyList<KcvFilterBlock> blocks) : base(editor, blocks)
	{
		Block = block;
		Parent = parent;
		Name = string.Format(ForkStrings.FilterBlockName, name);

		int index = parent.Children.IndexOf(block);
		CanMoveUp = index > 0;
		CanMoveDown = index < parent.Children.Count - 1;

		MoveUpCommand = new RelayCommand(() => Editor.Move(Block, Parent, -1));
		MoveDownCommand = new RelayCommand(() => Editor.Move(Block, Parent, 1));
		RemoveCommand = new RelayCommand(() => Editor.Remove(Block, Parent));
	}
}

/// <summary>
/// A condition: 사용, 항목, 비교, the value (a number, a choice, a ship's name, a text or a time by the item) with the
/// viewer's note beside it, ✕. A wrong value (not a number, out of the item's range) is shown red and keeps OK off.
/// </summary>
public sealed class KcvFilterConditionLine : KcvFilterLine
{
	public KcvFilterCondition Condition { get; }
	public KcvFilterBlock Parent { get; }

	public IReadOnlyList<KcvFilterField> Fields { get; }
	private readonly KcvFilterField _field;

	public KcvFilterField Field
	{
		get => _field;
		set
		{
			if (value is null || ReferenceEquals(value, _field)) return;

			Editor.ChangeField(this, value);
		}
	}

	public IReadOnlyList<KcvFilterOperator> Operators => _field.Operators;

	private KcvFilterOperator? _operator;

	public KcvFilterOperator? Operator
	{
		get => _operator;
		set
		{
			if (value is null) return;

			_operator = value;
			Condition.Data.Operator = value.Value;
			Editor.Refresh();
		}
	}

	public IReadOnlyList<KcvFilterChoice> Choices { get; }

	private KcvFilterChoice? _choice;

	public KcvFilterChoice? Choice
	{
		get => _choice;
		set
		{
			if (value is null) return;

			_choice = value;
			Condition.Data.RightOperand = value.Value;
			Editor.Refresh();
		}
	}

	private string _valueText = "";

	public string ValueText
	{
		get => _valueText;
		set
		{
			_valueText = value ?? "";
			ReadValue();
			Editor.Refresh();
		}
	}

	public bool IsValid { get; private set; } = true;
	public string Description { get; private set; } = "";

	public bool Enabled
	{
		get => Condition.Data.Enabled;
		set
		{
			Condition.Data.Enabled = value;
			Editor.Refresh();
		}
	}

	public bool IsTextValue => _field.Kind is KcvFilterValueKind.Number or KcvFilterValueKind.Decimal
		or KcvFilterValueKind.Text or KcvFilterValueKind.Time or KcvFilterValueKind.Unknown;

	public bool IsChoiceValue => _field.Kind is KcvFilterValueKind.Choice;
	public bool IsNameValue => _field.Kind is KcvFilterValueKind.Name;
	public bool IsEditable => _field.Kind is not KcvFilterValueKind.Unknown;

	/// <summary>A symbol (=, ≤ …) or a word (접두사 아님 …).</summary>
	public double OperatorWidth => Operators.Any(o => o.Symbol.Length > 1) ? 100 : 56;

	public double ValueWidth => _field.Kind is KcvFilterValueKind.Unknown ? 120 : _field.ValueWidth;

	public ICommand RemoveCommand { get; }

	public KcvFilterConditionLine(KcvShipGroupFilterViewModel editor, KcvFilterCondition condition, KcvFilterBlock parent,
		IReadOnlyList<KcvFilterBlock> blocks) : base(editor, blocks)
	{
		Condition = condition;
		Parent = parent;

		ExpressionData data = condition.Data;
		_field = KcvFilterFields.Get(data.LeftOperand);
		Fields = KcvFilterFields.All.Contains(_field) ? KcvFilterFields.All : [.. KcvFilterFields.All, _field];
		_operator = Operators.FirstOrDefault(o => o.Value == data.Operator);

		Choices = _field.Choices;
		if (IsChoiceValue)
		{
			_choice = Choices.FirstOrDefault(c => Equals(c.Value, data.RightOperand));

			// a value not in the list (an equipment not in the master data) stays as it is
			if (_choice is null && data.RightOperand is not null)
			{
				_choice = new KcvFilterChoice(data.RightOperand, KcvFilterFields.ValueText(data.LeftOperand, data.RightOperand));
				Choices = [.. Choices, _choice];
			}
		}

		_valueText = ValueToText(data.RightOperand);
		Description = KcvFilterFields.Description(data.LeftOperand, data.RightOperand);

		RemoveCommand = new RelayCommand(() => Editor.Remove(Condition, Parent));
	}

	public override void Update()
	{
		base.Update();
		if (!Condition.Data.Enabled) SetDimmed(true);
	}

	private string ValueToText(object? value) => _field.Kind switch
	{
		KcvFilterValueKind.Decimal when value is double number => number.ToString("F" + _field.Decimals, CultureInfo.InvariantCulture),
		KcvFilterValueKind.Time when value is int milliseconds => KcvFilterFields.TimeText(milliseconds),
		_ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
	};

	/// <summary>The value of the text in the box, as the item's type; a wrong one leaves the condition's value.</summary>
	private void ReadValue()
	{
		ExpressionData data = Condition.Data;
		string text = _valueText.Trim();
		object? value = null;

		switch (_field.Kind)
		{
			case KcvFilterValueKind.Number:
				if (int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number)
					&& number >= _field.Minimum && number <= _field.Maximum)
				{
					value = number;
				}
				break;

			case KcvFilterValueKind.Decimal:
				if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double real)
					&& real >= _field.Minimum && real <= _field.Maximum)
				{
					value = Math.Round(real, _field.Decimals);
				}
				break;

			case KcvFilterValueKind.Time:
				value = KcvFilterFields.ParseTime(text);
				break;

			case KcvFilterValueKind.Text:
			case KcvFilterValueKind.Name:
				value = _valueText;
				break;

			default:
				return;
		}

		IsValid = value is not null;
		if (value is not null) data.RightOperand = value;

		Description = IsValid ? KcvFilterFields.Description(data.LeftOperand, data.RightOperand) : "";
	}
}

/// <summary>「＋ 조건」 「＋ 묶음」 at the end of a block (the outermost one's at the bottom).</summary>
public sealed class KcvFilterAddLine : KcvFilterLine
{
	public ICommand AddConditionCommand { get; }
	public ICommand AddBlockCommand { get; }

	public KcvFilterAddLine(KcvShipGroupFilterViewModel editor, KcvFilterBlock block, IReadOnlyList<KcvFilterBlock> blocks)
		: base(editor, blocks)
	{
		AddConditionCommand = new RelayCommand(() => Editor.AddCondition(block));
		AddBlockCommand = new RelayCommand(() => Editor.AddBlock(block));
	}
}

/// <summary>
/// A block's bracket in the gutter, from its head to its 「＋」 line (the outermost from the first line to the last), and
/// its AND/OR in the middle of its left: a click changes it. Faint when the block has fewer than two enabled items (the
/// join is not used), dim when it or a block around it is disabled.
/// </summary>
public sealed class KcvFilterBracket : ObservableObject
{
	private readonly KcvShipGroupFilterViewModel _editor;
	private readonly KcvFilterBlock _block;
	private readonly IReadOnlyList<KcvFilterBlock> _blocks;

	public double Left { get; }
	public double Top { get; }
	public double Height { get; }

	public bool And { get; private set; }
	public bool Single { get; private set; }
	public bool Off { get; private set; }

	public ICommand ToggleCommand { get; }

	public KcvFilterBracket(KcvShipGroupFilterViewModel editor, KcvFilterBlock block, IReadOnlyList<KcvFilterBlock> blocks,
		int depth, int firstLine, int lines)
	{
		_editor = editor;
		_block = block;
		_blocks = blocks;

		Left = KcvShipGroupFilterViewModel.BracketStep * depth;
		Top = firstLine * KcvShipGroupFilterViewModel.LineHeight + 4;
		Height = lines * KcvShipGroupFilterViewModel.LineHeight - 8;

		ToggleCommand = new RelayCommand(() =>
		{
			_block.And = !_block.And;
			_editor.Refresh();
		});
	}

	public void Update()
	{
		And = _block.And;
		Single = _block.Children.Count(c => c.IsEnabled) < 2;
		Off = _blocks.Any(b => !b.Enabled);
	}
}

/// <summary>A ship of the 포함/제외 목록: its ID and 「이름 Lv.n」 (「(재적 없음)」 when it is gone), ✕.</summary>
public sealed class KcvFilterListRow
{
	public int Id { get; }
	public string Name { get; }
	public ICommand RemoveCommand { get; }

	public KcvFilterListRow(KcvShipGroupFilterViewModel editor, int id, string name)
	{
		Id = id;
		Name = name;
		RemoveCommand = new RelayCommand(() => editor.RemoveFromList(Id));
	}
}
