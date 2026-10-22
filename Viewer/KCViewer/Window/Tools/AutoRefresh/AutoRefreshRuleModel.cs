using System.Collections.Generic;
using KCViewer.Window.Dialog.QuestTrackerManager.Models;

namespace KCViewer.Window.Tools.AutoRefresh;

public record AutoRefreshRuleModel(bool IsEnabled, MapInfoModel Map, List<CellModel> AllowedCells);