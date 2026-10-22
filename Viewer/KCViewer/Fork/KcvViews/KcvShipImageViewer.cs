using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using KCViewer.Core.Types;
using KCViewer.Data;
using KCViewer.Resource;
using KCViewer.Resource.Record;

namespace KCViewer.Fork.KcvViews;

/// <summary>A picture of 이미지 뷰어: its file, the list's group, the kind from its folder and the ship from its name.</summary>
public sealed class KcvShipImageItem : ObservableObject
{
	public string Path { get; }
	public string Group { get; }
	public int Index { get; }
	public IShipDataMaster? Ship { get; }
	public string ShipName => Ship?.NameWithClass ?? "???";
	public string Kind { get; }
	public bool Damaged { get; }
	public string DamagedText => Damaged ? $" · {ForkStrings.ImageViewerDamaged}" : "";
	public string Label => Kind + DamagedText;
	public string FileName => System.IO.Path.GetFileName(Path);
	public ImageSource? Thumbnail { get; set; }

	public KcvShipImageItem(string path, string group, int index)
	{
		Path = path;
		Group = group;
		Index = index;
		Ship = ShipOf(path);

		// kcs2/resources/ship/card_dmg/0009_….png: 카드 · 중파
		string folder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path)) ?? "";
		Damaged = folder.EndsWith("_dmg", StringComparison.Ordinal);
		Kind = ForkStrings.ImageViewerKind(Damaged ? folder[..^4] : folder) ?? System.IO.Path.GetFileNameWithoutExtension(path);
	}

	/// <summary>The ship of a file named by its ID (「0009_…」), as the viewer's.</summary>
	public static IShipDataMaster? ShipOf(string path)
	{
		string name = System.IO.Path.GetFileNameWithoutExtension(path);

		return name.Length >= 4 && int.TryParse(name[..4], out int id) ? KCDatabase.Instance.MasterShips[id] : null;
	}
}

/// <summary>
/// 이미지 뷰어's model: the pictures of a ship and its costume ships (the viewer's order, the missing ones left out), or
/// files opened or dropped, the selected one and its image. The small pictures of the list are read in the background.
/// </summary>
public sealed class KcvShipImageViewerViewModel : ObservableObject
{
	// the viewer's order (DialogShipGraphicViewer.Open): each kind, then its damaged one
	private static readonly (string Type, bool Damaged)[] Kinds =
	[
		(KCResourceHelper.ResourceTypeShipBanner, false), (KCResourceHelper.ResourceTypeShipBanner, true),
		(KCResourceHelper.ResourceTypeShipCard, false), (KCResourceHelper.ResourceTypeShipCard, true),
		(KCResourceHelper.ResourceTypeShipAlbumZoom, false), (KCResourceHelper.ResourceTypeShipAlbumZoom, true),
		(KCResourceHelper.ResourceTypeShipAlbumFull, false), (KCResourceHelper.ResourceTypeShipAlbumFull, true),
		(KCResourceHelper.ResourceTypeShipFull, false), (KCResourceHelper.ResourceTypeShipFull, true),
		(KCResourceHelper.ResourceTypeShipCutin, false), (KCResourceHelper.ResourceTypeShipCutin, true),
		(KCResourceHelper.ResourceTypeShipName, false),
		(KCResourceHelper.ResourceTypeShipSupply, false), (KCResourceHelper.ResourceTypeShipSupply, true),
	];

	public string Title { get; private set; } = ShipGraphicViewerResources.Title;
	public IReadOnlyList<KcvShipImageItem> Items { get; private set; } = [];
	public BitmapSource? Image { get; private set; }

	private KcvShipImageItem? selected;

	public KcvShipImageItem? Selected
	{
		get => selected;
		set
		{
			if (ReferenceEquals(selected, value)) return;

			selected = value;
			Image = value is null ? null : LoadImage(value.Path);
			OnPropertyChanged();
			OnPropertyChanged(nameof(StatusText));
		}
	}

	public string StatusText => Selected is { } item
		? $"{item.Index} / {Items.Count} · {item.ShipName} · {item.Kind}{item.DamagedText} · {item.FileName}"
		: "";

	/// <summary>The pictures of the ship and its costume ships; false when there are none.</summary>
	public bool OpenShip(int shipId)
	{
		if (KCDatabase.Instance.MasterShips[shipId] is not { } ship) return true;

		List<(string Path, string Group)> pictures = [.. ShipPictures(shipId, ship.NameWithClass)];
		foreach (ShipParameterRecord.ShipParameterElement costume in RecordManager.Instance.ShipParameter.Record.Values.Where(r => r.OriginalCostumeShipID == shipId))
		{
			pictures.AddRange(ShipPictures(costume.ShipID, ForkStrings.ImageViewerCostume(costume.ShipID)));
		}

		if (pictures.Count == 0) return false;

		Show(pictures, $"{ShipGraphicViewerResources.Title} - {ship.NameWithClass}");
		return true;
	}

	/// <summary>Files opened or dropped, grouped by their ship; false when none of them is there.</summary>
	public bool OpenFiles(IEnumerable<string> paths)
	{
		List<string> files = paths.Where(File.Exists).ToList();
		if (files.Count == 0) return false;

		List<(string Path, string Group)> pictures = files
			.GroupBy(path => KcvShipImageItem.ShipOf(path)?.NameWithClass ?? "???")
			.SelectMany(group => group.Select(path => (path, ForkStrings.ImageViewerGroup(group.Key, group.Count()))))
			.ToList();

		Show(pictures, ShipGraphicViewerResources.Title);
		return true;
	}

	/// <summary>The next (1) or previous (-1) picture, round the list as the viewer's.</summary>
	public void Move(int step)
	{
		if (Items.Count == 0) return;

		int index = Selected is null ? 0 : Selected.Index - 1 + step;
		Selected = Items[((index % Items.Count) + Items.Count) % Items.Count];
	}

	private static IEnumerable<(string Path, string Group)> ShipPictures(int shipId, string name)
	{
		List<string> paths = Kinds
			.Select(kind => KCResourceHelper.GetShipImagePath(shipId, kind.Damaged, kind.Type))
			.OfType<string>()
			.ToList();

		return paths.Select(path => (path, ForkStrings.ImageViewerGroup(name, paths.Count)));
	}

	private int ShowCount { get; set; }

	private void Show(IReadOnlyList<(string Path, string Group)> pictures, string title)
	{
		Items = pictures.Select((picture, i) => new KcvShipImageItem(picture.Path, picture.Group, i + 1)).ToList();
		Title = title;
		Selected = Items[0];

		// the list's small pictures, one by one; a later list stops the earlier one
		int show = ++ShowCount;
		IReadOnlyList<KcvShipImageItem> items = Items;
		Task.Run(() =>
		{
			foreach (KcvShipImageItem item in items)
			{
				if (show != ShowCount) return;

				item.Thumbnail = LoadImage(item.Path, 60);
			}
		});
	}

	/// <summary>Stops reading the small pictures (the window was closed).</summary>
	public void Stop() => ShowCount++;

	private static BitmapSource? LoadImage(string path, int decodeHeight = 0)
	{
		try
		{
			using FileStream stream = new(path, FileMode.Open, FileAccess.Read);

			BitmapImage image = new();
			image.BeginInit();
			image.CacheOption = BitmapCacheOption.OnLoad;
			if (decodeHeight > 0) image.DecodePixelHeight = decodeHeight;
			image.StreamSource = stream;
			image.EndInit();
			image.Freeze();
			return image;
		}
		catch (Exception ex)
		{
			Utility.Logger.Add(3, $"{ShipGraphicViewerResources.ErrorLoadingImage} {ex.Message}");
			return null;
		}
	}
}

/// <summary>
/// The picture of 이미지 뷰어, drawn as the viewer's panel: centred at the zoom (or fitted to the panel), moved by
/// dragging, sharp or smooth, over the checkered background when it is on, and the viewer's message window with 「@」.
/// </summary>
public sealed class KcvShipImageStage : FrameworkElement
{
	private static readonly Brush CheckerBrush = MakeChecker();

	private BitmapSource? image;
	private double zoom = 1;
	private bool fit;
	private bool smooth;
	private bool checker;
	private bool advMode;
	private bool restricted;
	private Vector offset;
	private Point dragFrom;

	/// <summary>The zoom, the shown picture or the panel's size changed.</summary>
	public event EventHandler? ViewChanged;

	public KcvShipImageStage()
	{
		RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
	}

	/// <summary>A new picture starts at 100% in the middle, as the viewer's.</summary>
	public BitmapSource? Image
	{
		get => image;
		set
		{
			image = value;
			offset = default;
			zoom = 1;
			Changed();
		}
	}

	public double Zoom
	{
		get => zoom;
		set
		{
			zoom = Math.Clamp(value, 0.1, 16);
			Changed();
		}
	}

	public bool Fit
	{
		get => fit;
		set
		{
			fit = value;
			Changed();
		}
	}

	public bool Smooth
	{
		get => smooth;
		set
		{
			smooth = value;
			RenderOptions.SetBitmapScalingMode(this, value ? BitmapScalingMode.HighQuality : BitmapScalingMode.NearestNeighbor);
			Changed();
		}
	}

	public bool Checker
	{
		get => checker;
		set
		{
			checker = value;
			Changed();
		}
	}

	/// <summary>The message window (「@」); with Shift its restricted patch.</summary>
	public void ToggleAdvMode(bool shift)
	{
		advMode = !advMode;
		restricted = advMode && shift;
		Changed();
	}

	/// <summary>The ship of the picture, for the message window.</summary>
	public IShipDataMaster? Ship { get; set; }

	/// <summary>The zoom shown: the fitted one while 맞춤 is on.</summary>
	public double ShownZoom => fit && image is { PixelWidth: > 0, PixelHeight: > 0 } && ActualWidth > 0 && ActualHeight > 0
		? Math.Min(ActualWidth / image.PixelWidth, ActualHeight / image.PixelHeight)
		: zoom;

	private void Changed()
	{
		InvalidateVisual();
		ViewChanged?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
	{
		base.OnRenderSizeChanged(sizeInfo);
		ViewChanged?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
	{
		dragFrom = e.GetPosition(this);
		CaptureMouse();
		e.Handled = true;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (!IsMouseCaptured || e.LeftButton is not MouseButtonState.Pressed) return;

		Point at = e.GetPosition(this);
		offset += at - dragFrom;
		dragFrom = at;
		InvalidateVisual();
	}

	protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
	{
		ReleaseMouseCapture();
	}

	protected override void OnRender(DrawingContext dc)
	{
		Rect bounds = new(RenderSize);

		// a transparent background still takes the mouse
		dc.DrawRectangle(checker ? CheckerBrush : Brushes.Transparent, null, bounds);

		if (image is null) return;

		double shown = ShownZoom;
		Size size = new(image.PixelWidth * shown, image.PixelHeight * shown);
		Point at = new((bounds.Width - size.Width) / 2 + offset.X, (bounds.Height - size.Height) / 2 + offset.Y);
		dc.DrawImage(image, new Rect(at, size));

		if (advMode) DrawAdvMode(dc);
	}

	// かん☆これ!! (DialogShipGraphicViewer.DrawAdvMode)
	private void DrawAdvMode(DrawingContext dc)
	{
		if (Ship is not { } ship) return;

		double width = RenderSize.Width;
		double height = RenderSize.Height;

		const int mainHeight = 200;
		const int nameHeight = 64;
		Rect mainBound = new(16, height - mainHeight - 16, Math.Max(0, width - 32), mainHeight);
		Rect innerBound = new(mainBound.X + 16, mainBound.Y + 16, Math.Max(0, mainBound.Width - 32), mainBound.Height - 32);
		Rect nameBound = new(16, mainBound.Y - nameHeight - 16, 256, nameHeight);
		Rect nameInnerBound = new(nameBound.X + 16, nameBound.Y + 16, nameBound.Width - 32, nameBound.Height - 32);

		Random rand = new(401168);      // for fixed drawing

		// window shadow
		Pen shadow = new(Brushes.RoyalBlue, 8) { LineJoin = PenLineJoin.Round };
		dc.DrawRectangle(null, shadow, new Rect(mainBound.X + 2, mainBound.Y + 2, mainBound.Width, mainBound.Height));
		dc.DrawRectangle(null, shadow, new Rect(nameBound.X + 2, nameBound.Y + 2, nameBound.Width, nameBound.Height));

		// window base
		LinearGradientBrush back = new(Color.FromArgb(0xcc, 0x44, 0xcc, 0xff), Color.FromArgb(0xcc, 0x44, 0x44, 0xff), 90);
		dc.DrawRectangle(back, null, mainBound);
		dc.DrawRectangle(back, null, nameBound);

		// decorations
		dc.PushClip(new RectangleGeometry(mainBound));

		Pen pen = new(new SolidColorBrush(Color.FromArgb(0x44, 0xcc, 0xee, 0xff)), 4);
		Point center = new(mainBound.X + mainHeight * 3 / 4, mainBound.Y + mainHeight * 3 / 4);

		// compass
		dc.DrawEllipse(null, pen, center, 150, 150);
		dc.DrawEllipse(null, pen, center, 140, 140);
		dc.DrawEllipse(null, pen, center, 16, 16);

		int direct = (int)(120 * Math.Sin(Math.PI / 4));
		StreamGeometry needle = new();
		using (StreamGeometryContext g = needle.Open())
		{
			g.BeginFigure(new Point(center.X + direct, center.Y - direct), false, true);
			g.LineTo(new Point(center.X + 16, center.Y + 16), true, false);
			g.LineTo(new Point(center.X - direct, center.Y + direct), true, false);
			g.LineTo(new Point(center.X - 16, center.Y - 16), true, false);
		}
		dc.DrawGeometry(null, pen, needle);

		// bubbles
		for (int i = 0; i < 16; i++)
		{
			int r = (int)(8 + rand.NextDouble() * 24 * i / 8);
			double x = mainBound.Right - 96 + (int)((rand.NextDouble() * 128 - 64) * (i + 1) / 16);
			double y = mainBound.Bottom - 16 - (i * mainBound.Height) / 16;
			dc.DrawEllipse(null, pen, new Point(x + r / 2.0, y + r / 2.0), r / 2.0, r / 2.0);
		}

		dc.Pop();

		// window frame
		Pen frame = new(Brushes.White, 8) { LineJoin = PenLineJoin.Round };
		dc.DrawRectangle(null, frame, mainBound);
		dc.DrawRectangle(null, frame, nameBound);

		// text
		// don't think (about env that font is not installed), feel
		string message = Message(ship);
		DrawText(dc, message, innerBound);
		DrawText(dc, ship.NameWithClass, nameInnerBound);
	}

	private string Message(IShipDataMaster ship)
	{
		if (RecordManager.Instance.ShipParameter[ship.ShipID] is null) return "……";

		List<IShipDataMaster> processedShips = [ship];

		while (processedShips[^1].RemodelBeforeShip is { } before && processedShips.All(s => processedShips[^1].RemodelBeforeShipID != s.ShipID))
		{
			processedShips.Add(before);
		}

		string? message = processedShips.Select(s => (s.MessageGet ?? "").Replace("<br>", "\r\n")).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
			?? processedShips.Select(s => (s.MessageAlbum ?? "").Replace("<br>", "\r\n")).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
			?? "……";

		if (restricted)
		{
			message = message
				.Replace("。", "♥")
				.Replace(".\r\n", "♥\r\n")
				.Replace("……", "…")
				.Replace("…", "…♥")
				.Replace("！", "！♥")
				.Replace("？", "？♥")
				.Replace("!", "!♥")
				.Replace("?", "?♥");
		}

		return message;
	}

	// wrapped to the bound and cut at its edges, as GDI+'s DrawString in a rectangle (a line taller than the name's
	// bound would be left out by MaxTextHeight)
	private void DrawText(DrawingContext dc, string text, Rect bound)
	{
		if (bound.Width <= 0 || bound.Height <= 0) return;

		void Draw(Brush brush, Rect at)
		{
			FormattedText formatted = new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
				new Typeface("にゃしぃフォント改二"), 32, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip)
			{
				MaxTextWidth = at.Width,
			};

			dc.PushClip(new RectangleGeometry(at));
			dc.DrawText(formatted, at.Location);
			dc.Pop();
		}

		Draw(Brushes.RoyalBlue, new Rect(bound.X + 2, bound.Y + 2, bound.Width, bound.Height));
		Draw(Brushes.White, bound);
	}

	// the viewer's 16 px tile: white with light grey squares
	private static Brush MakeChecker()
	{
		DrawingGroup tile = new();
		tile.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
		tile.Children.Add(new GeometryDrawing(Brushes.LightGray, null, new RectangleGeometry(new Rect(0, 0, 8, 8))));
		tile.Children.Add(new GeometryDrawing(Brushes.LightGray, null, new RectangleGeometry(new Rect(8, 8, 8, 8))));

		DrawingBrush brush = new(tile)
		{
			TileMode = TileMode.Tile,
			Viewport = new Rect(0, 0, 16, 16),
			ViewportUnits = BrushMappingMode.Absolute,
		};
		brush.Freeze();
		return brush;
	}
}
