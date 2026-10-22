using System.Collections.Generic;

namespace KCViewer.Core.Types;

public interface IApiWrapper : IResponseWrapper
{
	void LoadFromRequest(string apiname, Dictionary<string, string> data);
}
