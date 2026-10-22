using System.Collections.Generic;
using KCViewer.Data;

namespace KCViewer.Observer.kcsapi.api_req_quest;

public class clearitemget : APIBase
{

	public override void OnRequestReceived(Dictionary<string, string> data)
	{

		KCDatabase.Instance.Quest.LoadFromRequest(APIName, data);

		base.OnRequestReceived(data);
	}


	public override bool IsRequestSupported => true;
	public override bool IsResponseSupported => true;


	public override string APIName => "api_req_quest/clearitemget";
}
