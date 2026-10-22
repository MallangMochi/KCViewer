using KCViewer.KancolleApi.Types.Models;

namespace KCViewer.KancolleApi.Types.ApiGetMember.PictureBook;

public class ApiGetMemberPictureBookResponse
{
	[JsonPropertyName("api_list")]
	public List<ApiPictureBookList> ApiList { get; set; } = new();
}
