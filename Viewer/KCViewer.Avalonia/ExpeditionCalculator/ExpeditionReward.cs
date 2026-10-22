using KCViewer.Core.Types;

namespace KCViewer.Avalonia.ExpeditionCalculator;

public class ExpeditionReward
{
	public required UseItemId Type { get; init; }
	public required int Amount { get; init; }
}
