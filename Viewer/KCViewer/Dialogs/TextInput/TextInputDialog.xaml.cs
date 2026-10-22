using System.Windows.Threading;

namespace KCViewer.Dialogs.TextInput;

public partial class TextInputDialog
{
	public TextInputDialog()
	{
		InitializeComponent();

		// https://github.com/Kinnara/ModernWpf/issues/378
		SourceInitialized += (_, _) =>
		{
			Dispatcher.Invoke(InvalidateVisual, DispatcherPriority.Input);
		};
	}
}
