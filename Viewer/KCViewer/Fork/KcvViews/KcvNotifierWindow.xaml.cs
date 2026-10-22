using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using KCViewer.Notifier;
using KCViewer.Window;
using Color = System.Drawing.Color;
using SystemColors = System.Drawing.SystemColors;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: the notification window in KanColleViewer's style (see the XAML), shown instead of the viewer's
/// DialogNotifier while the style is on. It follows the notification's settings as the viewer's: the place, closing
/// after the time, the clicks that close it, closing when the mouse moves over it, on top, activated, the image and
/// the border; Enter and Esc close it. The message's colours are the theme's while the settings keep their first
/// values (the system's control colours), else the two set colours (one of them alone could hide the text).
/// </summary>
public partial class KcvNotifierWindow
{
	private NotifierDialogData Data { get; }
	private DispatcherTimer? CloseTimer { get; }

	/// <summary>The notification in this style; false while the style is off (the caller shows the viewer's).</summary>
	public static bool TryShow(NotifierDialogData data, Action closed, System.Windows.Forms.FormClosingEventHandler? closing)
	{
		if (!KcvViewState.Current.Enabled) return false;

		Application.Current.Dispatcher.Invoke(() =>
		{
			KcvNotifierWindow window = new(data);
			window.Closed += (_, _) =>
			{
				closed();
				closing?.Invoke(window, new System.Windows.Forms.FormClosingEventArgs(System.Windows.Forms.CloseReason.UserClosing, false));
			};
			window.Show();
		});
		return true;
	}

	private KcvNotifierWindow(NotifierDialogData data)
	{
		InitializeComponent();

		// a copy, as the viewer's: the notifier changes its message for the next one
		Data = data.Clone();
		Title = Data.Title;
		TitleText.Text = Data.Title;
		Logo.Source = KcvMenuWindows.AppIcon(16);
		Topmost = Data.TopMost;
		ShowActivated = Data.ShowWithActivation;

		Message.Text = Data.Message;
		Message.Visibility = Data.DrawsMessage ? Visibility.Visible : Visibility.Collapsed;
		ShowColours();
		ShowImage();

		// the place once the size is known; off the screen until then (a place the system picks: where it shows it)
		if (Data.Alignment is not NotifierDialogAlignment.NotSet)
		{
			Left = -32000;
			Top = -32000;
			Loaded += (_, _) => Place();
		}

		if (Data.ClosingInterval > 0)
		{
			CloseTimer = new() { Interval = TimeSpan.FromMilliseconds(Data.ClosingInterval) };
			CloseTimer.Tick += (_, _) => Close();
			CloseTimer.Start();
		}

		data.CloseAll += CloseAll;
		Closed += (_, _) =>
		{
			data.CloseAll -= CloseAll;
			CloseTimer?.Stop();
		};

		MouseDown += Clicked;
		MouseMove += (_, _) =>
		{
			if (Data.CloseOnMouseMove) Close();
		};
		KeyDown += (_, e) =>
		{
			if (e.Key is Key.Enter or Key.Escape) Close();
		};
	}

	private void CloseAll(object? sender, EventArgs e) => Dispatcher.BeginInvoke(Close);

	private void CloseClicked(object sender, RoutedEventArgs e) => Close();

	private void ShowColours()
	{
		bool firstValues = Data.ForeColor.ToArgb() == SystemColors.ControlText.ToArgb()
			&& Data.BackColor.ToArgb() == SystemColors.Control.ToArgb();
		if (firstValues) return;

		Frame.Background = Brush(Data.BackColor);
		Message.Foreground = Brush(Data.ForeColor);
	}

	/// <summary>The notification's image at its size with the message over it; without the border the image alone.</summary>
	private void ShowImage()
	{
		if (!Data.DrawsImage || Data.Image is null)
		{
			Frame.Width = 300;
			Body.MinHeight = 42;
			return;
		}

		BitmapSource image = ToBitmapSource(Data.Image);
		Picture.Source = image;
		Picture.Visibility = Visibility.Visible;
		Body.Width = image.Width;
		Body.Height = image.Height;

		if (Data.HasFormBorder) return;

		TitleBar.Visibility = Visibility.Collapsed;
		Frame.BorderThickness = new Thickness(0);
		Frame.Background = Brushes.Transparent;
	}

	private void Clicked(object sender, MouseButtonEventArgs e)
	{
		NotifierDialogClickFlags flags = Data.ClickFlag;
		bool twice = e.ClickCount > 1;

		bool close = e.ChangedButton switch
		{
			MouseButton.Left => flags.HasFlag(NotifierDialogClickFlags.Left) || twice && flags.HasFlag(NotifierDialogClickFlags.LeftDouble),
			MouseButton.Right => flags.HasFlag(NotifierDialogClickFlags.Right) || twice && flags.HasFlag(NotifierDialogClickFlags.RightDouble),
			MouseButton.Middle => flags.HasFlag(NotifierDialogClickFlags.Middle) || twice && flags.HasFlag(NotifierDialogClickFlags.MiddleDouble),
			_ => false,
		};

		if (close) Close();
	}

	/// <summary>The viewer's places (DialogNotifier_Load, NotifierManager.ShowNotifier) in this window's units.</summary>
	private void Place()
	{
		Rect screen = SystemParameters.WorkArea;
		double width = ActualWidth;
		double height = ActualHeight;
		Matrix fromDevice = PresentationSource.FromVisual(this)?.CompositionTarget.TransformFromDevice ?? Matrix.Identity;
		Point location = fromDevice.Transform(new Point(Data.Location.X, Data.Location.Y));

		Point place = Data.Alignment switch
		{
			NotifierDialogAlignment.TopLeft => new(screen.Left, screen.Top),
			NotifierDialogAlignment.TopCenter => new(screen.Left + (screen.Width - width) / 2, screen.Top),
			NotifierDialogAlignment.TopRight => new(screen.Right - width, screen.Top),
			NotifierDialogAlignment.MiddleLeft => new(screen.Left, screen.Top + (screen.Height - height) / 2),
			NotifierDialogAlignment.MiddleCenter => new(screen.Left + (screen.Width - width) / 2, screen.Top + (screen.Height - height) / 2),
			NotifierDialogAlignment.MiddleRight => new(screen.Right - width, screen.Top + (screen.Height - height) / 2),
			NotifierDialogAlignment.BottomLeft => new(screen.Left, screen.Bottom - height),
			NotifierDialogAlignment.BottomCenter => new(screen.Left + (screen.Width - width) / 2, screen.Bottom - height),
			NotifierDialogAlignment.BottomRight => new(screen.Right - width, screen.Bottom - height),
			NotifierDialogAlignment.CustomRelative => FromBrowser(fromDevice, width, height) ?? location,
			_ => location,
		};

		Left = place.X;
		Top = place.Y;
	}

	/// <summary>The set place from the middle of the browser (the window's middle there); null without the browser (the viewer then takes the place as it is).</summary>
	private Point? FromBrowser(Matrix fromDevice, double width, double height)
	{
		if (Application.Current.MainWindow is not FormMainWpf { ViewModel.FormBrowserHost.WindowsFormsHost.Child: FormBrowserHost host }) return null;

		System.Drawing.Point middle = host.PointToScreen(new System.Drawing.Point(host.ClientSize.Width / 2, host.ClientSize.Height / 2));
		Point centre = fromDevice.Transform(new Point(middle.X, middle.Y));
		Vector offset = fromDevice.Transform(new Vector(Data.Location.X, Data.Location.Y));

		return new Point(centre.X - width / 2, centre.Y - height / 2) + offset;
	}

	private static SolidColorBrush Brush(Color colour)
	{
		SolidColorBrush brush = new(System.Windows.Media.Color.FromArgb(colour.A, colour.R, colour.G, colour.B));
		brush.Freeze();
		return brush;
	}

	// through a PNG, which keeps the image's transparency
	private static BitmapSource ToBitmapSource(System.Drawing.Bitmap bitmap)
	{
		using MemoryStream stream = new();
		lock (bitmap) bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
		stream.Position = 0;

		BitmapImage image = new();
		image.BeginInit();
		image.CacheOption = BitmapCacheOption.OnLoad;
		image.StreamSource = stream;
		image.EndInit();
		image.Freeze();
		return image;
	}
}
