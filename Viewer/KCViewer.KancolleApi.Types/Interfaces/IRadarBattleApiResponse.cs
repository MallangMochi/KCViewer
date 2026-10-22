using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.Interfaces;

public interface IRadarBattleApiResponse : IFirstBattleApiResponse, IAirBaseBattle
{
	ApiHougeki1? ApiHougeki1 { get; set; }
}
