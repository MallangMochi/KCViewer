using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ModernWpf.Controls.Primitives;

namespace KCViewer.Fork;

/// <summary>
/// Fork: the settings' box for the screenshot key (mockup 20261009-screenshot-capture-v2). Focused, it reads
/// 「키를 누르세요」 and takes the next key pressed that <see cref="ForkHotkey.IsAllowed"/> lets through (others are
/// ignored); Esc leaves it as it was and moves on, Tab moves on. Without a key it reads 「없음」. Both texts are the box's
/// hint (the style's Tag, ModernWpf's placeholder), dimmed. The viewer's screenshot key is off while the box waits, so
/// the key pressed reaches the box.
/// A text box (read only) so it looks as the page's other text boxes.
/// </summary>
public class ForkHotkeyBox : TextBox
{
	public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
		nameof(Hotkey), typeof(string), typeof(ForkHotkeyBox),
		new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((ForkHotkeyBox)d).ShowKey()));

	/// <summary>The key as the menus show it ("F2", "Ctrl+Shift+S"), "" for none.</summary>
	public string Hotkey
	{
		get => (string)GetValue(HotkeyProperty);
		set => SetValue(HotkeyProperty, value);
	}

	private bool Waiting { get; set; }

	static ForkHotkeyBox()
	{
		DefaultStyleKeyProperty.OverrideMetadata(typeof(ForkHotkeyBox), new FrameworkPropertyMetadata(typeof(TextBox)));
	}

	public ForkHotkeyBox()
	{
		// the page's implicit text box style (the viewer's own settings page), which a subclass does not get by itself;
		// a style set on the box (the KanColleViewer style's page) replaces it
		SetResourceReference(StyleProperty, typeof(TextBox));
		IsReadOnly = true;
		IsUndoEnabled = false;
		InputMethod.SetIsInputMethodEnabled(this, false);
		Unloaded += (_, _) => StopWaiting();
		ShowKey();
	}

	protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
	{
		base.OnGotKeyboardFocus(e);
		Waiting = true;
		ForkScreenshot.Suspend(true);
		ShowKey();
	}

	protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
	{
		base.OnLostKeyboardFocus(e);
		StopWaiting();
	}

	protected override void OnPreviewKeyDown(KeyEventArgs e)
	{
		Key key = e.Key == Key.System ? e.SystemKey : e.Key;

		if (key == Key.Tab)
		{
			base.OnPreviewKeyDown(e);
			return;
		}

		e.Handled = true;

		if (key == Key.Escape)
		{
			MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
			return;
		}

		if (!ForkHotkey.IsAllowed(Keyboard.Modifiers, key)) return;

		Waiting = false;
		Hotkey = new ForkHotkey(Keyboard.Modifiers, key).ToString();
		ShowKey();
	}

	private void StopWaiting()
	{
		Waiting = false;
		ForkScreenshot.Suspend(false);
		ShowKey();
	}

	private void ShowKey()
	{
		string hint = Waiting ? ForkStrings.ScreenshotHotkeyWaiting : ForkStrings.ScreenshotHotkeyNone;
		Text = Waiting || string.IsNullOrEmpty(Hotkey) ? "" : Hotkey;
		Tag = hint;
		ControlHelper.SetPlaceholderText(this, hint);
	}
}
