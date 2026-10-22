using KCViewer.Core.Types.Quests;
using KCViewer.Data;
using KCViewer.Window.Dialog.QuestTrackerManager.Enums;
using MessagePack;

namespace KCViewer.Window.Dialog.QuestTrackerManager.Models;

[MessagePackObject]
public record QuestModel([property: Key(0)] int Id)
{
	[IgnoreMember] public string Name => TryGetQuest()?.Name ?? QuestTrackerManagerResources.UnknownQuest;
	[IgnoreMember] public string? Description => TryGetQuest()?.Description;
	[IgnoreMember] public string? Code => TryGetQuest()?.Code;
	[IgnoreMember] public QuestCategory Category => (QuestCategory)(TryGetQuest()?.Category ?? 0);
	[IgnoreMember] public int State => TryGetQuest()?.State ?? 0;
	[IgnoreMember]
	public QuestResetType ResetType => TryGetQuest()?.GetProgressResetType() ?? QuestResetType.Unknown;

	[IgnoreMember] public string Display => $"{Code}: {Name} (ID: {Id})";

	private QuestData? TryGetQuest()
	{
		if (!KCDatabase.Instance.Quest.Quests.ContainsKey(Id)) return null;

		return KCDatabase.Instance.Quest[Id];
	}
}
