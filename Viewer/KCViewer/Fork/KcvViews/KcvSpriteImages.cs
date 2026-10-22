using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: a sprite of the game (its png and the json of its frames, found by <see cref="ForkGameAssets"/>).</summary>
public sealed class KcvSprite
{
	private KcvSprite(BitmapSource sheet, Dictionary<string, Int32Rect> frames)
	{
		Sheet = sheet;
		Frames = frames;
	}

	public BitmapSource Sheet { get; }

	public IReadOnlyDictionary<string, Int32Rect> Frames { get; }

	/// <summary>The frame's part of the sheet, or null when the sprite has no such frame.</summary>
	public BitmapSource? Crop(string frame)
	{
		if (!Frames.TryGetValue(frame, out Int32Rect rect)) return null;

		CroppedBitmap bitmap = new(Sheet, rect);
		bitmap.Freeze();
		return bitmap;
	}

	/// <summary>Reads a sprite whose files are there; null when it cannot be read (logged).</summary>
	public static KcvSprite? TryLoad(string sprite)
	{
		try
		{
			BitmapImage sheet = new();
			sheet.BeginInit();
			sheet.UriSource = new Uri(ForkGameAssets.Find(sprite + ".png")!);
			sheet.CacheOption = BitmapCacheOption.OnLoad;
			sheet.EndInit();
			sheet.Freeze();

			using FileStream json = File.OpenRead(ForkGameAssets.Find(sprite + ".json")!);
			using JsonDocument document = JsonDocument.Parse(json);

			Dictionary<string, Int32Rect> frames = new();
			foreach (JsonProperty frame in document.RootElement.GetProperty("frames").EnumerateObject())
			{
				JsonElement rect = frame.Value.GetProperty("frame");
				frames[frame.Name] = new(rect.GetProperty("x").GetInt32(), rect.GetProperty("y").GetInt32(),
					rect.GetProperty("w").GetInt32(), rect.GetProperty("h").GetInt32());
			}

			return new(sheet, frames);
		}
		catch (Exception e)
		{
			Utility.Logger.Add(2, ForkStrings.KcvSpriteFailed(sprite, e.Message));
			return null;
		}
	}
}

/// <summary>
/// Fork: images drawn from the game's sprites (<see cref="KcvSprite"/>) that can be handed out before a sprite is there.
/// On a first start the sprites are still being fetched (<see cref="ForkGameAssets.PreloadIcons"/>), so such an image
/// draws what it is given meanwhile and is filled when its sprite arrives; the panels keep the same image. Every call
/// runs on the UI thread, which owns the images that wait.
/// </summary>
public static class KcvSpriteImages
{
	/// <summary>Sprites read so far; null for one that is there but could not be read.</summary>
	private static readonly Dictionary<string, KcvSprite?> Sprites = new();

	private static readonly List<(string Sprite, DrawingGroup Group, Func<KcvSprite, Drawing?> Draw)> Waiting = [];

	private static bool Listening;

	/// <summary>Runs on the UI thread (a converter or a view model may ask from another).</summary>
	public static T OnUiThread<T>(Func<T> function) =>
		Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess() ? dispatcher.Invoke(function) : function();

	/// <summary>The sprite when its files are there (read once), else null.</summary>
	public static KcvSprite? Sprite(string sprite) => OnUiThread<KcvSprite?>(() =>
	{
		if (Sprites.TryGetValue(sprite, out KcvSprite? read)) return read;
		if (!ForkGameAssets.HasSprite(sprite)) return null;

		return Sprites[sprite] = KcvSprite.TryLoad(sprite);
	});

	/// <summary>
	/// An image of a sprite: drawn now when the sprite is there (null when <paramref name="draw"/> has nothing for it),
	/// else an image that draws <paramref name="meanwhile"/> and is filled when the sprite arrives.
	/// </summary>
	public static ImageSource? Image(string sprite, Func<KcvSprite, Drawing?> draw, Drawing? meanwhile = null) => OnUiThread<ImageSource?>(() =>
	{
		if (Sprite(sprite) is { } loaded)
		{
			if (draw(loaded) is not { } drawing) return null;

			DrawingImage image = new(drawing);
			image.Freeze();
			return image;
		}

		if (!ForkGameAssets.HasSprite(sprite) && !Listening)
		{
			ForkGameAssets.SpriteAvailable += Fill;
			Listening = true;
		}

		DrawingGroup group = new();
		if (meanwhile is not null) group.Children.Add(meanwhile);
		Waiting.Add((sprite, group, draw));

		return new DrawingImage(group);
	});

	/// <summary>A frame at its own size in pixels, or scaled down to a square of <paramref name="size"/> pixels in good quality.</summary>
	public static Drawing? Frame(KcvSprite sprite, string frame, int? size = null)
	{
		if (sprite.Crop(frame) is not { } bitmap) return null;

		if (size is { } side)
		{
			// The use item frames are 75 px and shown at 22: made 44 px (twice that) here, which keeps them sharp.
			DrawingVisual visual = new();
			RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
			using (DrawingContext context = visual.RenderOpen())
			{
				context.DrawImage(bitmap, new Rect(0, 0, side, side));
			}

			RenderTargetBitmap scaled = new(side, side, 96, 96, PixelFormats.Pbgra32);
			scaled.Render(visual);
			scaled.Freeze();
			bitmap = scaled;
		}

		ImageDrawing drawing = new(bitmap, new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
		drawing.Freeze();
		return drawing;
	}

	private static void Fill(string sprite)
	{
		if (Sprite(sprite) is not { } loaded) return;

		foreach ((string Sprite, DrawingGroup Group, Func<KcvSprite, Drawing?> Draw) waiting in Waiting.Where(w => w.Sprite == sprite).ToList())
		{
			Waiting.Remove(waiting);

			// nothing for it: it keeps what it drew meanwhile
			if (waiting.Draw(loaded) is not { } drawing) continue;

			waiting.Group.Children.Clear();
			waiting.Group.Children.Add(drawing);
		}
	}
}
