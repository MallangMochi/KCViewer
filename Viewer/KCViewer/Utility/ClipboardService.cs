using System;
using System.Windows.Forms;
using KCViewer.Core.Services;

namespace KCViewer.Utility;

public class ClipboardService : IClipboardService
{
	public void SetTextAndLogErrors(string text)
	{
		try
		{
			Clipboard.SetText(text);
		}
		catch (Exception ex)
		{
			ErrorReporter.SendErrorReport(ex, LoggerRes.FailedToCopyToClipboard);
		}
	}
}
