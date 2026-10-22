using System.Collections.Generic;
using KCViewer.Data;
using KCViewer.ViewModels;

namespace KCViewer.Observer.kcsapi.api_req_member;

public class updatecomment : APIBase
{

	public override bool IsRequestSupported => true;
	public override bool IsResponseSupported => false;

	public override void OnRequestReceived(Dictionary<string, string> data)
	{

		// Fork: the viewer's Halloween dialog for the comment 「jackolantern」 (DialogHalloween) is removed (user's decision 2026-10-20)

		KCDatabase.Instance.Admiral.LoadFromRequest(APIName, data);

		base.OnRequestReceived(data);
	}

	public override string APIName => "api_req_member/updatecomment";
}
