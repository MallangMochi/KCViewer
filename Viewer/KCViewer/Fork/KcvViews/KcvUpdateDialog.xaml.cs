using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the program update's small windows in KanColleViewer's style (see the XAML).</summary>
public partial class KcvUpdateDialog
{
	/// <summary>The button the user chose (its index), or -1 when the window was closed another way.</summary>
	public int Choice { get; private set; } = -1;

	private KcvUpdateDialog(string title, string message, string? note, IReadOnlyList<string> buttons, double width)
	{
		InitializeComponent();

		Title = title;
		Width = width;
		MessageText.Text = message;

		if (!string.IsNullOrWhiteSpace(note))
		{
			NoteText.Text = note.Trim();
			NoteBox.Visibility = Visibility.Visible;
		}

		for (int i = 0; i < buttons.Count; i++)
		{
			int index = i;
			Button button = new()
			{
				Content = buttons[i],
				MinWidth = 72,
				Margin = new Thickness(i == 0 ? 0 : 8, 0, 0, 0),
				IsDefault = i == 0,
				IsCancel = i == buttons.Count - 1,
				Style = (Style)FindResource(i == 0 ? "KcvRec.PrimaryButton" : "KcvRec.Button"),
			};
			button.Click += (_, _) =>
			{
				Choice = index;
				Close();
			};
			Buttons.Children.Add(button);
		}
	}

	/// <summary>Shows the window over <paramref name="owner"/> and returns the chosen button's index (-1: closed).</summary>
	public static int Ask(System.Windows.Window? owner, string title, string message, string? note, IReadOnlyList<string> buttons, double width = 430)
	{
		KcvUpdateDialog dialog = new(title, message, note, buttons, width);
		if (owner is { IsVisible: true }) dialog.Owner = owner;
		else dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

		dialog.ShowDialog();
		return dialog.Choice;
	}
}
