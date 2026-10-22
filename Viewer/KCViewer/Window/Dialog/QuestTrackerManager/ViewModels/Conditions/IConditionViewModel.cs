using System.ComponentModel;
using KCViewer.Core.Types;

namespace KCViewer.Window.Dialog.QuestTrackerManager.ViewModels.Conditions;

public interface IConditionViewModel : INotifyPropertyChanged
{
	string Display { get; }
	bool ConditionMet(IFleetData fleet);
}
