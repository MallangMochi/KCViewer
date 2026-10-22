using System.Collections.Generic;
using MessagePack;

namespace KCViewer.Window.Dialog.QuestTrackerManager.Models;

[MessagePackObject]
public record QuestTrackerProgressRecord
(
	[property: Key(0)] int QuestId,
	[property: Key(1)] IEnumerable<int> Progresses
);
