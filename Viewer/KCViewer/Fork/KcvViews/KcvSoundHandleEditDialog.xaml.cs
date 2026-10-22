using System.ComponentModel;
using KCViewer.Window.Settings.BGM;

namespace KCViewer.Fork.KcvViews;

/// <summary>Fork: the BGM scene editor in KanColleViewer's style (see the XAML), opened through <see cref="KcvCalcWindows"/>.</summary>
public partial class KcvSoundHandleEditDialog
{
	public KcvSoundHandleEditDialog(SoundHandleViewModel soundHandle) : base(new SoundHandleEditViewModel(soundHandle))
	{
		InitializeComponent();

		ViewModel.PropertyChanged += CloseOnResult;
		Closed += (_, _) => ViewModel.PropertyChanged -= CloseOnResult;
	}

	private void CloseOnResult(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not nameof(ViewModel.DialogResult)) return;

		Close();
	}
}
