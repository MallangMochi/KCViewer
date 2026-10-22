using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: a whole number between <see cref="Minimum"/> and <see cref="Maximum"/> (see the XAML).</summary>
public partial class KcvNumberBox : UserControl
{
	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		nameof(Value), typeof(int), typeof(KcvNumberBox),
		new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((KcvNumberBox)d).OnValueChanged()));

	public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
		nameof(Minimum), typeof(int), typeof(KcvNumberBox), new PropertyMetadata(int.MinValue, (d, _) => ((KcvNumberBox)d).KeepInRange()));

	public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
		nameof(Maximum), typeof(int), typeof(KcvNumberBox), new PropertyMetadata(int.MaxValue, (d, _) => ((KcvNumberBox)d).KeepInRange()));

	/// <summary>Writes the value with its sign (「+2」, 「+0」), for a bonus such as the training plan's modernization.</summary>
	public static readonly DependencyProperty PlusProperty = DependencyProperty.Register(
		nameof(Plus), typeof(bool), typeof(KcvNumberBox), new PropertyMetadata(false, (d, _) => ((KcvNumberBox)d).ShowValue()));

	public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
		nameof(TextAlignment), typeof(TextAlignment), typeof(KcvNumberBox), new PropertyMetadata(TextAlignment.Left));

	/// <summary>
	/// Keeps text that is not a whole number (instead of putting the value back when the box is left) and marks the box
	/// red, for a form that blocks OK while one is wrong (the ship album's 수치 편집).
	/// </summary>
	public static readonly DependencyProperty KeepsInvalidTextProperty = DependencyProperty.Register(
		nameof(KeepsInvalidText), typeof(bool), typeof(KcvNumberBox), new PropertyMetadata(false));

	private static readonly DependencyPropertyKey IsInvalidPropertyKey = DependencyProperty.RegisterReadOnly(
		nameof(IsInvalid), typeof(bool), typeof(KcvNumberBox), new PropertyMetadata(false));

	public static readonly DependencyProperty IsInvalidProperty = IsInvalidPropertyKey.DependencyProperty;

	/// <summary>Raised (bubbling) when <see cref="IsInvalid"/> changes.</summary>
	public static readonly RoutedEvent InvalidChangedEvent = EventManager.RegisterRoutedEvent(
		nameof(InvalidChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(KcvNumberBox));

	public event RoutedEventHandler InvalidChanged
	{
		add => AddHandler(InvalidChangedEvent, value);
		remove => RemoveHandler(InvalidChangedEvent, value);
	}

	public bool KeepsInvalidText
	{
		get => (bool)GetValue(KeepsInvalidTextProperty);
		set => SetValue(KeepsInvalidTextProperty, value);
	}

	/// <summary>The text is not a whole number (only with <see cref="KeepsInvalidText"/>).</summary>
	public bool IsInvalid
	{
		get => (bool)GetValue(IsInvalidProperty);
		private set
		{
			if (IsInvalid == value) return;

			SetValue(IsInvalidPropertyKey, value);
			ShowBorder();
			RaiseEvent(new RoutedEventArgs(InvalidChangedEvent, this));
		}
	}

	public int Value
	{
		get => (int)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public bool Plus
	{
		get => (bool)GetValue(PlusProperty);
		set => SetValue(PlusProperty, value);
	}

	public TextAlignment TextAlignment
	{
		get => (TextAlignment)GetValue(TextAlignmentProperty);
		set => SetValue(TextAlignmentProperty, value);
	}

	public int Minimum
	{
		get => (int)GetValue(MinimumProperty);
		set => SetValue(MinimumProperty, value);
	}

	public int Maximum
	{
		get => (int)GetValue(MaximumProperty);
		set => SetValue(MaximumProperty, value);
	}

	/// <summary>While the text is typed: the value follows it, the text is not written back.</summary>
	private bool Typing { get; set; }

	public KcvNumberBox()
	{
		InitializeComponent();

		ShowValue();
		Input.TextChanged += (_, _) => TakeText();
		Input.LostKeyboardFocus += (_, _) =>
		{
			if (!IsInvalid) ShowValue();
		};
		Input.PreviewKeyDown += StepByKey;
		PreviewMouseWheel += StepByWheel;

		// the accent border under the mouse and while typing, as the record windows' text boxes
		MouseEnter += (_, _) => ShowBorder();
		MouseLeave += (_, _) => ShowBorder();
		Input.GotKeyboardFocus += (_, _) => ShowBorder();
		Input.LostKeyboardFocus += (_, _) => ShowBorder();
	}

	private int InRange(int value) => Math.Clamp(value, Minimum, Math.Max(Minimum, Maximum));

	/// <summary>The text is being written from the value, not typed.</summary>
	private bool Showing { get; set; }

	private void ShowValue()
	{
		if (Typing) return;

		Showing = true;
		Input.Text = Plus && Value >= 0
			? "+" + Value.ToString(CultureInfo.InvariantCulture)
			: Value.ToString(CultureInfo.InvariantCulture);
		Showing = false;
		IsInvalid = false;
	}

	private void TakeText()
	{
		if (Showing) return;
		if (!int.TryParse(Input.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int typed))
		{
			IsInvalid = KeepsInvalidText;
			return;
		}

		IsInvalid = false;
		Typing = true;
		Value = InRange(typed);
		Typing = false;
	}

	private void KeepInRange()
	{
		int value = InRange(Value);
		if (value != Value) Value = value;
	}

	/// <summary>A range check is waiting for the bindings (see <see cref="OnValueChanged"/>).</summary>
	private bool KeepPending { get; set; }

	/// <summary>
	/// A value from the source outside the range (a training plan's goal under the level now, or one bound after the
	/// range) is brought into it once the bindings have finished, so the source gets it too, as ModernWpf's number box.
	/// </summary>
	private void OnValueChanged()
	{
		ShowValue();

		if (KeepPending || InRange(Value) == Value) return;

		KeepPending = true;
		Dispatcher.BeginInvoke(() =>
		{
			KeepPending = false;
			KeepInRange();
		}, DispatcherPriority.DataBind);
	}

	private void Step(int delta)
	{
		Value = InRange(Value + delta);
		ShowValue();
	}

	private void StepUp(object sender, RoutedEventArgs e) => Step(1);

	private void StepDown(object sender, RoutedEventArgs e) => Step(-1);

	private void StepByKey(object sender, KeyEventArgs e)
	{
		if (e.Key is not (Key.Up or Key.Down)) return;

		Step(e.Key is Key.Up ? 1 : -1);
		e.Handled = true;
	}

	private void StepByWheel(object sender, MouseWheelEventArgs e)
	{
		if (!IsKeyboardFocusWithin) return;

		Step(e.Delta > 0 ? 1 : -1);
		e.Handled = true;
	}

	private void ShowBorder()
	{
		Box.SetResourceReference(Border.BorderBrushProperty,
			IsInvalid ? "Kcv.LackForeground" : IsMouseOver || Input.IsKeyboardFocused ? "Kcv.Accent" : "Kcv.Separator");
	}
}
