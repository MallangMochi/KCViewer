using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KCViewer.Avalonia.Services;
using KCViewer.Core.Types;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// Fork: a game image for the viewer's WPF windows when no folder has it yet: a ship's banner
/// (ShipToBannerImageConverter: the ship album, the expedition record, the ship training plan) or an equipment's card
/// (EquipmentToImageConverter: the equipment album). It is fetched from the official server when a window asks for it
/// (<see cref="ForkGameAssets"/>, not at start) and the window gets an image of the right size right away that is
/// filled when the file arrives, so it shows without reopening the window (user's decisions 2026-10-07). One that
/// cannot be fetched stays empty. The windows of the viewer's Avalonia part load and refresh by themselves
/// (ImageLoadService, GameAssetDownloaderService → <see cref="GameResourceHelper.ForkFetch"/>).
/// </summary>
public static class KcvGameImages
{
	private static Dictionary<string, ImageSource> Waiting { get; } = new(StringComparer.OrdinalIgnoreCase);

	public static ImageSource ShipImage(int shipId, string type, double width, double height) =>
		Pending(GameAssetDownloaderService.GetShipImagePath((ShipId)shipId, false, type), width, height);

	public static ImageSource EquipmentImage(int equipmentId, string type, double width, double height) =>
		Pending(GameAssetDownloaderService.GetEquipmentImagePath(equipmentId, type), width, height);

	private static ImageSource Pending(string file, double width, double height) => KcvSpriteImages.OnUiThread(() =>
	{
		if (Waiting.TryGetValue(file, out ImageSource? waiting)) return waiting;

		DrawingGroup group = new();
		group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, width, height))));
		ImageSource image = new DrawingImage(group);
		Waiting[file] = image;

		_ = Fill(group, file, width, height);
		return image;
	});

	/// <summary>Started on the UI thread, so it carries on there after the fetch.</summary>
	private static async Task Fill(DrawingGroup group, string file, double width, double height)
	{
		if (!await ForkGameAssets.FetchAsync(file, quiet: true) || ForkGameAssets.Find(file) is not { } path) return;

		try
		{
			BitmapImage bitmap = new();
			bitmap.BeginInit();
			bitmap.UriSource = new Uri(path);
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.EndInit();
			bitmap.Freeze();

			group.Children.Clear();
			group.Children.Add(new ImageDrawing(bitmap, new Rect(0, 0, width, height)));
		}
		catch (Exception e)
		{
			Utility.Logger.Add(2, ForkStrings.KcvSpriteFailed(file, e.Message));
		}
	}
}
