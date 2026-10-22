using System.Runtime.InteropServices;

namespace KCViewer.Window.Dialog.UiBlocker;

[StructLayout(LayoutKind.Sequential)]
public struct RawPoint
{
	public int X;
	public int Y;
}
