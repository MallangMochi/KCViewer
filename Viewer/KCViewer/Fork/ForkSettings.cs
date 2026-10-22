using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using KCViewer.Utility;

namespace KCViewer.Fork;

/// <summary>
/// Fork: settings of the fork's own features, in <c>Settings\Fork.json</c> of the working folder, so each profile
/// (live, test) has its own. Kept apart from EO's configuration so upstream merges stay simple.
/// </summary>
public sealed class ForkSettings
{
	private const string FilePath = @"Settings\Fork.json";

	public static ForkSettings Current { get; } = Load();

	public LocalSkinSettings LocalSkin { get; set; } = new();

	public KcvStyleSettings KcvStyle { get; set; } = new();

	public AssetsSettings Assets { get; set; } = new();

	public LogSettings Log { get; set; } = new();

	public TranslationSettings Translation { get; set; } = new();

	public ProgramUpdateSettings ProgramUpdate { get; set; } = new();

	public UpdateWatchSettings UpdateWatch { get; set; } = new();

	public FleetPanelSettings FleetPanel { get; set; } = new();

	public ScreenshotSettings Screenshot { get; set; } = new();

	/// <summary>
	/// The screenshot's shortcut key (<see cref="ForkScreenshot"/>, mockup 20261009-screenshot-capture-v2): taken in any
	/// window of the viewer while the viewer is in front. Written as the menus show it ("F2", "Ctrl+Shift+S"), "" for none;
	/// F2 by default, the key the browser had.
	/// </summary>
	public sealed class ScreenshotSettings
	{
		public string Hotkey { get; set; } = "F2";
	}

	/// <summary>
	/// The fleet panels (mockup 20261009-fleet-dock-badge-v5): a docked ship's row in the docking colour (KanColleViewer's
	/// fleet panel only; off: the badge over the HP gauge only), and a docked fleet panel brought to the front of its pane
	/// when its fleet's composition changes (<see cref="ForkFleetPanelSwitch"/>, whatever the style). Both on by default.
	/// </summary>
	public sealed class FleetPanelSettings
	{
		public bool DockHighlight { get; set; } = true;
		public bool SwitchOnComposition { get; set; } = true;
	}

	/// <summary>
	/// The start-up checks for a game or KC3 update (<see cref="ForkUpdateWatch"/>), both off by default (mockup
	/// 20261007-update-watch-v1), and the values the last check saw.
	/// </summary>
	public sealed class UpdateWatchSettings
	{
		public bool Game { get; set; }

		public bool Kc3 { get; set; }

		/// <summary>The game version (kcs_const.js VersionInfo.scriptVesion) the last check saw; empty before the first.</summary>
		public string GameVersion { get; set; } = "";

		/// <summary>The newest commit of KC3's translations (data/) the last check saw, in full; empty before the first.</summary>
		public string Kc3Commit { get; set; } = "";
	}

	/// <summary>
	/// The Korean translations (ship, equipment, quest, expedition, operation and lock names) come from the viewer's own
	/// repository, apart from EO's data (<c>UpdateRepoURL</c>, which keeps quest trackers, locks, fit bonuses and
	/// upgrades), user's decision 2026-10-07 (mockup 20261007-translation-url-v2, <see cref="Utility.SoftwareUpdater"/>).
	/// </summary>
	public sealed class TranslationSettings
	{
		public const string DefaultUrl = "https://raw.githubusercontent.com/MallangMochi/KCViewer/main/Data/";

		/// <summary>The folder above <c>Translations\&lt;language&gt;\</c>. Empty: no translations are fetched.</summary>
		public string Url { get; set; } = DefaultUrl;
	}

	/// <summary>
	/// The viewer's own program updates (UPDATER-DESIGN.md, <see cref="Update.ForkProgramUpdate"/>), from the releases of
	/// our public repository (user's decision 2026-10-22 "우리 저장소 사용하도록 함").
	/// </summary>
	public sealed class ProgramUpdateSettings
	{
		public const string DefaultUrl = "https://github.com/MallangMochi/KCViewer/releases/latest/download/";

		/// <summary>Where the latest release's files are (the small file, its signature). Empty: no program update.</summary>
		public string Url { get; set; } = DefaultUrl;

		/// <summary>
		/// The release whose manual install notice was shown: it shows once a version (mockup 20261022-program-update-v5 ⑨),
		/// after that only the menu bar item says so.
		/// </summary>
		public string ManualNoticeVersion { get; set; } = "";
	}

	/// <summary>The common notification settings, 알람 › 기본 설정 (<see cref="ForkNotifierDefaults"/>).</summary>
	public NotificationSettings Notification { get; set; } = new();

	public sealed class NotificationSettings
	{
		/// <summary>The notifications' boxes were sorted into 기본 and 개별 once, when the feature came.</summary>
		public bool Migrated { get; set; }

		public NotifierValues Default { get; set; } = new();

		/// <summary>By notifier (NotifierManager's property name): which parts are the notifier's own (개별).</summary>
		public Dictionary<string, NotifierOverride> Overrides { get; set; } = [];
	}

	/// <summary>A notifier's settings, by default the viewer's first values (<c>ConfigNotifierBase</c>'s constructor).</summary>
	public sealed class NotifierValues
	{
		public bool IsEnabled { get; set; } = true;

		public bool PlaysSound { get; set; }
		public string SoundPath { get; set; } = "";
		public int SoundVolume { get; set; } = 100;
		public bool LoopsSound { get; set; }

		public bool DrawsImage { get; set; }
		public string ImagePath { get; set; } = "";

		public bool ShowsDialog { get; set; } = true;
		public bool DrawsMessage { get; set; } = true;
		public int ClosingInterval { get; set; } = 10000;
		public int AccelInterval { get; set; }
		public bool CloseOnMouseMove { get; set; }
		public int ClickFlag { get; set; } = (int)Notifier.NotifierDialogClickFlags.Left;
		public int Alignment { get; set; } = (int)Notifier.NotifierDialogAlignment.BottomRight;
		public int LocationX { get; set; }
		public int LocationY { get; set; }
		public bool HasFormBorder { get; set; } = true;
		public bool TopMost { get; set; } = true;
		public bool ShowWithActivation { get; set; } = true;
		public int ForeColor { get; set; } = System.Drawing.SystemColors.ControlText.ToArgb();
		public int BackColor { get; set; } = System.Drawing.SystemColors.Control.ToArgb();
	}

	/// <summary>True: that part is the notifier's own (「이 알람만 개별 설정」), false: it follows 기본 설정.</summary>
	public sealed class NotifierOverride
	{
		public bool Enabled { get; set; }
		public bool Sound { get; set; }
		public bool Image { get; set; }
		public bool Dialog { get; set; }
	}

	/// <summary>The daily cleanup of the viewer's log (<see cref="ForkLogCleanup"/>).</summary>
	public sealed class LogSettings
	{
		/// <summary>The hour (0-23, local time) of the daily cleanup. 5 by default: 05:00, when the game's day resets.</summary>
		public int CleanupHour { get; set; } = 5;

		[JsonIgnore]
		public int CleanupHourValue => Math.Clamp(CleanupHour, 0, 23);
	}

	/// <summary>
	/// The local server's game files (<c>assets\</c>, laid out like the game's paths), where the viewer looks for game
	/// images instead of its own cache, which stays empty as the fork does not run the proxy (<see cref="ForkAssets"/>).
	/// </summary>
	public sealed class AssetsSettings
	{
		/// <summary>Empty: the <c>assets</c> folder of the first folder above the viewer with <c>server\index.js</c>, as the plugins find it.</summary>
		public string Folder { get; set; } = "";

		/// <summary>
		/// The official server game images missing from both folders are fetched from (<see cref="ForkGameAssets"/>), the
		/// one the local assets came from (KCCacheProxy, scripts/fetch-missing-assets.js). Empty: nothing is fetched.
		/// </summary>
		public string DownloadServer { get; set; } = "https://w01y.kancolle-server.com";

		/// <summary>Null when there is no such folder.</summary>
		[JsonIgnore]
		public string? ResolvedFolder
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(Folder))
				{
					string folder = Path.GetFullPath(Folder.Trim());
					return Directory.Exists(folder) ? folder : null;
				}

				for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
				{
					if (File.Exists(Path.Combine(dir.FullName, "server", "index.js")))
					{
						string assets = Path.Combine(dir.FullName, "assets");
						return Directory.Exists(assets) ? assets : null;
					}
				}

				return null;
			}
		}
	}

	/// <summary>KanColleViewer's look (<see cref="Fork.KcvStyle"/>). On by default, with its default purple accent.</summary>
	public sealed class KcvStyleSettings
	{
		public bool Enabled { get; set; } = true;

		/// <summary>Purple, Blue or Orange, the accents KanColleViewer offers.</summary>
		public string Accent { get; set; } = nameof(KcvAccent.Purple);

		public KcvAccent AccentValue => Enum.TryParse(Accent, out KcvAccent accent) ? accent : KcvAccent.Purple;

		/// <summary>The fleet overview's cards one under another (true) or side by side, wrapping (false, default).</summary>
		public bool FleetOverviewVertical { get; set; }

		/// <summary>
		/// The dock, arsenal and expedition panels' docks side by side, wrapping (true), or one under another (false,
		/// default) (mockup 20261006-timer-panels-v2); and the expedition panel's names (off: the number only).
		/// </summary>
		public bool DockHorizontal { get; set; }
		public bool ArsenalHorizontal { get; set; }
		public bool ExpeditionHorizontal { get; set; }
		public bool ExpeditionShowName { get; set; } = true;

		/// <summary>
		/// The headquarters' 임의 아이템 2 and 3 (mockup 20261005-headquarters-icons-v4): a use item ID (0: none) and
		/// whether it shows. 임의 아이템 1 is the viewer's own setting (DisplayUseItemID).
		/// </summary>
		public int HeadquartersItem2 { get; set; }
		public int HeadquartersItem3 { get; set; }
		public bool HeadquartersShowItem2 { get; set; }
		public bool HeadquartersShowItem3 { get; set; }

		/// <summary>The ship list's and the equipment list's filters unfolded (mockup 20261005-list-search-v2); open at first.</summary>
		public bool ShipListFiltersExpanded { get; set; } = true;
		public bool EquipmentListFiltersExpanded { get; set; } = true;
	}

	public sealed class LocalSkinSettings
	{
		/// <summary>Off by default (requirement 8, item 6).</summary>
		public bool Enabled { get; set; }

		/// <summary>Folder laid out like the game's paths (kcs2\..., kcs\...). Empty: <c>&lt;exe folder&gt;\Skins</c>.</summary>
		public string Folder { get; set; } = "";

		public string ResolvedFolder => string.IsNullOrWhiteSpace(Folder)
			? Path.Combine(AppContext.BaseDirectory, "Skins")
			: Path.GetFullPath(Folder.Trim());
	}

	private static ForkSettings Load()
	{
		try
		{
			if (File.Exists(FilePath)) return JsonSerializer.Deserialize<ForkSettings>(File.ReadAllText(FilePath)) ?? new();
		}
		catch (Exception ex)
		{
			Logger.Add(3, $"{FilePath}: {ex.Message}");
		}

		return new();
	}

	public void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
			File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
		}
		catch (Exception ex)
		{
			Logger.Add(3, $"{FilePath}: {ex.Message}");
		}
	}
}
