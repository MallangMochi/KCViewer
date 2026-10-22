using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using KCViewer.Core.Types;

namespace KCViewer.Window.Tools.DevelopmentRecordViewer;

/// <summary>
/// Fork: the [검색] [취소] buttons of the KanColleViewer style window (Fork/KcvViews/KcvDevelopmentRecordWindow). The
/// viewer has one button that asks whether to stop a running search; the style's window has a cancel button that is
/// enabled while a search runs ("검색을 취소하는 취소 버튼을 검색과 초기화 사이에 모두 둠", 2026-10-04).
/// </summary>
public partial class DevelopmentRecordViewerViewModel
{
	public bool ForkIsSearching { get; private set; }
	public bool ForkCanSearch => !ForkIsSearching;
	private bool ForkWatchesSearcher { get; set; }

	[RelayCommand]
	private void ForkSearch()
	{
		if (Searcher.IsBusy) return;

		if (!ForkWatchesSearcher)
		{
			Searcher.RunWorkerCompleted += (_, _) => ForkIsSearching = false;
			ForkWatchesSearcher = true;
		}

		ForkIsSearching = true;
		StartSearchCommand.Execute(null);
	}

	[RelayCommand]
	private void ForkCancelSearch()
	{
		if (Searcher.IsBusy) Searcher.CancelAsync();
	}

	private List<object>? ForkAllEquipment { get; set; }

	/// <summary>
	/// The equipment list follows the category, as the construction record's ship list ("카테고리에 맞춰서 함선과
	/// 같게", 2026-10-05). (모두) (성공) (실패) stay; a chosen equipment that is not in the new list goes back to (모두).
	/// </summary>
	public void ForkFollowCategory() => PropertyChanged += (_, e) =>
	{
		if (e.PropertyName is not nameof(SelectedCategory)) return;

		ForkAllEquipment ??= Equipment;
		Equipment = SelectedCategory switch
		{
			EquipmentTypes category => ForkAllEquipment.Where(o => o is not IEquipmentDataMaster equipment || equipment.CategoryType == category).ToList(),
			_ => ForkAllEquipment,
		};
		if (!Equipment.Contains(SelectedEquipment)) SelectedEquipment = DevelopmentRecordOption.All;
	};
}
