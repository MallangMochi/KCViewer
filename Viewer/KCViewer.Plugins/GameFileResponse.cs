using System;
using System.Collections.Generic;

namespace KCViewer.Plugins;

/// <summary>
/// A game file that this instance's browser received: a GET response under /kcs2/, /kcs/ or /gadget_html5/
/// (images, sounds, scripts, JSON). Pages (such as /kcs2/index.php) and API calls are not game files.
/// </summary>
/// <param name="Time">When the response finished.</param>
/// <param name="Url">Full URL. An api_token value in the query is blanked.</param>
/// <param name="Host">Host name, for example "w13b.kancolle-server.com".</param>
/// <param name="Path">URL path as sent (not unescaped), for example "/kcs2/img/title/title_main.png".</param>
/// <param name="Query">Query with its leading "?", for example "?version=5", or "" when there is none.</param>
/// <param name="Status">HTTP status.</param>
/// <param name="MimeType">MIME type as the browser saw it.</param>
/// <param name="Headers">Response headers; names are compared without case. Set-Cookie is left out.</param>
public sealed record GameFileResponse(
	DateTime Time,
	string Url,
	string Host,
	string Path,
	string Query,
	int Status,
	string MimeType,
	IReadOnlyDictionary<string, string> Headers);
