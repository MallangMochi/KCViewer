using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace BrowserLibCore;

/// <summary>
/// Fork: helpers shared by the browser process (DevTools capture) and the host.
/// </summary>
public static class DevToolsCapturePayload
{
	/// <summary>
	/// Response bodies go to the host gzip-compressed. api_start2 is about 2 MB of JSON;
	/// compressed it stays far below the 4 MB gRPC message limit.
	/// </summary>
	public static byte[] Compress(string text)
	{
		using MemoryStream output = new();
		using (GZipStream gzip = new(output, CompressionLevel.Fastest))
		{
			gzip.Write(Encoding.UTF8.GetBytes(text));
		}
		return output.ToArray();
	}

	public static string Decompress(byte[] data)
	{
		using GZipStream gzip = new(new MemoryStream(data), CompressionMode.Decompress);
		using StreamReader reader = new(gzip, Encoding.UTF8);
		return reader.ReadToEnd();
	}

	/// <summary>
	/// Blanks the api_token value in a form-urlencoded body (the game sends the key as <c>api%5Ftoken</c>).
	/// The token is the login credential, so it must never leave the browser process.
	/// The key stays, with an empty value, so the body keeps the shape EO's request parser expects.
	/// </summary>
	public static string BlankToken(string formBody) =>
		string.Join("&", formBody.Split('&').Select(pair =>
		{
			int separator = pair.IndexOf('=');
			string key = separator < 0 ? pair : pair[..separator];
			return Uri.UnescapeDataString(key) == "api_token" ? key + "=" : pair;
		}));
}
