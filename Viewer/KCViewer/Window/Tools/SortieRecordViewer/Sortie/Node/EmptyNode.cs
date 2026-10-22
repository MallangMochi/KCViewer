using KCViewer.Core.Types;
using KCViewer.Core.Types.Data;

namespace KCViewer.Window.Tools.SortieRecordViewer.Sortie.Node;

public class EmptyNode(IKCDatabase kcDatabase, int world, int map, int cell, CellType colorNo, int eventId, int eventKind)
	: SortieNode(kcDatabase, world, map, cell, colorNo, eventId, eventKind);
