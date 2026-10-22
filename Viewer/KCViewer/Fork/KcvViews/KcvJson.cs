using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DynaJson;
using KCViewer.Observer;
using PropertyChanged;

namespace KCViewer.Fork.KcvViews;

/// <summary>
/// A node of the JSON tree, as the viewer's FormJson builds them: "name : value", arrays as [n] and objects as {n}.
/// Children are made one level ahead (so a node shows its expander), the grandchildren when it is first expanded.
/// </summary>
public sealed class KcvJsonNode : ObservableObject
{
	public string Text { get; }
	public object? Data { get; }
	public KcvJsonNode? Parent { get; }
	public ObservableCollection<KcvJsonNode> Children { get; } = [];
	public bool IsExpanded { get; set; }
	public bool IsSelected { get; set; }

	private bool Grown { get; set; }

	public KcvJsonNode(string text, object? data = null, KcvJsonNode? parent = null)
	{
		Text = text;
		Data = data;
		Parent = parent;
	}

	/// <summary>A node of a JSON value (FormJson.CreateNode).</summary>
	public static KcvJsonNode Of(string name, dynamic? data, KcvJsonNode? parent)
	{
		string text = string.IsNullOrEmpty(name) ? "" : name + " : ";
		object? value = data;

		text += value switch
		{
			null => "null",
			string s => "\"" + s + "\"",
			bool or double => value.ToString(),
			JsonObject { IsArray: true } => "[" + Count(value) + "]",
			JsonObject { IsObject: true } => "{" + Count(value) + "}",
			_ => throw new NotImplementedException(),
		};

		return new KcvJsonNode(text, value, parent);
	}

	private static int Count(dynamic json)
	{
		int count = 0;
		foreach (dynamic _ in json) count++;
		return count;
	}

	/// <summary>Makes the children of a JSON array or object (FormJson.CreateChildNode).</summary>
	public void MakeChildren()
	{
		if (Data is not JsonObject json || Children.Count > 0) return;

		if (json.IsArray)
		{
			foreach (dynamic element in (dynamic)json) Children.Add(Of("", element, this));
		}
		else if (json.IsObject)
		{
			foreach (KeyValuePair<string, dynamic> element in (dynamic)json) Children.Add(Of(element.Key, element.Value, this));
		}
	}

	// Fody calls this after IsExpanded is set: the grandchildren on the first expansion (FormJson.JsonTreeView_BeforeExpand)
	private void OnIsExpandedChanged()
	{
		if (!IsExpanded || Grown) return;

		Grown = true;
		foreach (KcvJsonNode child in Children) child.MakeChildren();
	}

	/// <summary>Expands this node and everything under it (the viewer's 모두 펼치기).</summary>
	public void ExpandAll()
	{
		IsExpanded = true;
		foreach (KcvJsonNode child in Children) child.ExpandAll();
	}

	/// <summary>The node's text, also its name for UI Automation.</summary>
	public override string ToString() => Text;

	/// <summary>The viewer's condition for its CSV item: an array whose first element is an array or object.</summary>
	public bool CanWriteCsv =>
		Data is JsonObject { IsArray: true } && Children.Count > 0 && Children[0].Data is JsonObject { IsArray: true } or JsonObject { IsObject: true };
}

/// <summary>
/// Fork: the JSON panel of the KanColleViewer style (KcvJsonView), the viewer's FormJson in WPF: the last request
/// and response as a tree and as raw text, the viewer's settings (Config.FormJson: auto update, tree update, the API
/// filter) and saved API files dropped on it. It follows the API from its first use to the end, as FormJson does while
/// hidden, so the panel shows the latest API whenever it is opened.
/// </summary>
public sealed class KcvJsonModel : ObservableObject
{
	// yyyyMMdd_hhmmssff[S|Q]@api_path.json (FormJson.FileNamePattern)
	private static readonly Regex FileNamePattern = new(@"\d{8}_\d{8}([SQ])@(.*)\.json$", RegexOptions.Compiled);

	private static KcvJsonModel? instance;
	public static KcvJsonModel Instance => instance ??= new KcvJsonModel();

	public ObservableCollection<KcvJsonNode> Nodes { get; } = [];
	public string RawText { get; private set; } = "";
	public bool FilterInvalid { get; private set; }

	[DoNotNotify]
	public bool AutoUpdate
	{
		get => Utility.Configuration.Config.FormJson.AutoUpdate;
		set
		{
			Utility.Configuration.Config.FormJson.AutoUpdate = value;
			OnPropertyChanged();
			ResetTree();
		}
	}

	[DoNotNotify]
	public bool UpdatesTree
	{
		get => Utility.Configuration.Config.FormJson.UpdatesTree;
		set
		{
			Utility.Configuration.Config.FormJson.UpdatesTree = value;
			OnPropertyChanged();
			ResetTree();
		}
	}

	/// <summary>The API filter as typed; applied when the box loses the focus (FormJson.AutoUpdateFilter_Validated).</summary>
	public string Filter { get; set; } = "";

	private Regex? ApiPattern { get; set; }
	private string? CurrentApiPath { get; set; }

	private KcvJsonModel()
	{
		ConfigurationChanged();

		APIObserver.Instance.RequestReceived += (apiName, data) =>
		{
			if (Accepts(apiName)) LoadRequest(apiName, data);
		};
		APIObserver.Instance.ResponseReceived += (apiName, data) =>
		{
			if (Accepts(apiName)) LoadResponse(apiName, data);
		};
		Utility.Configuration.Instance.ConfigurationChanged += ConfigurationChanged;
	}

	private bool Accepts(string apiName) => AutoUpdate && (ApiPattern is null || ApiPattern.Match(apiName).Success);

	private void ConfigurationChanged()
	{
		OnPropertyChanged(nameof(AutoUpdate));
		OnPropertyChanged(nameof(UpdatesTree));
		Filter = Utility.Configuration.Config.FormJson.AutoUpdateFilter;
		ApplyFilterPattern();
		ResetTree();
	}

	/// <summary>Saves the typed filter and makes its pattern; an invalid one leaves every API through, marked red.</summary>
	public void ApplyFilter()
	{
		Utility.Configuration.Config.FormJson.AutoUpdateFilter = Filter;
		ApplyFilterPattern();
	}

	private void ApplyFilterPattern()
	{
		try
		{
			ApiPattern = new Regex(Filter);
			FilterInvalid = false;
		}
		catch (Exception)
		{
			ApiPattern = null;
			FilterInvalid = true;
		}
	}

	private void ResetTree()
	{
		Nodes.Clear();
		if (!AutoUpdate || !UpdatesTree) Nodes.Add(new KcvJsonNode(JsonResources.AutoUpdateDisabledMessage));
	}

	private void LoadRequest(string apiName, Dictionary<string, string> data)
	{
		RawText = apiName + " : Request\r\n" + string.Join("\r\n", data.Select(p => p.Key + "=" + p.Value));
		CurrentApiPath = apiName;

		if (!UpdatesTree) return;

		Nodes.Clear();
		Nodes.Add(new KcvJsonNode(apiName));

		KcvJsonNode request = new("<Request> : {" + data.Count + "}");
		foreach ((string key, string value) in data) request.Children.Add(new KcvJsonNode(key + " : " + value, null, request));
		Nodes.Add(request);
	}

	private void LoadResponse(string apiName, dynamic? data)
	{
		RawText = (CurrentApiPath == apiName ? RawText + "\r\n\r\n" : "") + apiName + " : Response\r\n" + (data == null ? "" : data.ToString());
		CurrentApiPath = apiName;

		if (!UpdatesTree) return;

		if (Nodes.Count == 0 || Nodes[0].Text != apiName)
		{
			Nodes.Clear();
			Nodes.Add(new KcvJsonNode(apiName));
		}

		KcvJsonNode response = KcvJsonNode.Of("<Response>", data, null);
		response.MakeChildren();
		Nodes.Add(response);
	}

	/// <summary>A saved API file (FormJson.LoadFromFile), with the viewer's error report.</summary>
	public void LoadFromFile(string path)
	{
		Match match = FileNamePattern.Match(path);
		if (!match.Success) return;

		try
		{
			string data = File.ReadAllText(path);
			string apiName = match.Groups[2].Value.Replace('@', '/');

			if (match.Groups[1].Value == "Q")
			{
				Dictionary<string, string> parsed = [];
				foreach (string unit in System.Web.HttpUtility.UrlDecode(data).Split('&'))
				{
					string[] pair = unit.Split('=');
					parsed.Add(pair[0], pair[1]);
				}

				LoadRequest(apiName, parsed);
			}
			else
			{
				int head = data.IndexOfAny(['[', '{']);
				if (head == -1) throw new ArgumentException("JSON の開始文字を検出できませんでした。");

				LoadResponse(apiName, JsonObject.Parse(data[head..]));
			}
		}
		catch (Exception ex)
		{
			Utility.ErrorReporter.SendErrorReport(ex, "JSON データの読み込みに失敗しました。");
		}
	}

	/// <summary>The node's array as CSV: a header of the first element's paths, then a line per element (FormJson).</summary>
	public static void WriteCsv(KcvJsonNode node, string path)
	{
		dynamic array = node.Data!;
		using StreamWriter writer = new(path, false, Utility.Configuration.Config.Log.FileEncoding);

		writer.WriteLine(CsvHeader(new StringBuilder(), "", array[0]).ToString());
		foreach (dynamic element in array) writer.WriteLine(CsvContent(new StringBuilder(), element).ToString());
	}

	private static StringBuilder CsvHeader(StringBuilder sb, string currentPath, dynamic data)
	{
		if (data is JsonObject json)
		{
			if (json.IsObject)
			{
				foreach (KeyValuePair<string, dynamic> p in (dynamic)json) CsvHeader(sb, currentPath + "." + p.Key, p.Value);
				return sb;
			}

			if (json.IsArray)
			{
				int index = 0;
				foreach (dynamic element in (dynamic)json) CsvHeader(sb, currentPath + "[" + index++ + "]", element);
				return sb;
			}
		}

		return sb.Append(currentPath).Append(',');
	}

	private static StringBuilder CsvContent(StringBuilder sb, dynamic data)
	{
		if (data is JsonObject json)
		{
			if (json.IsObject)
			{
				foreach (KeyValuePair<string, dynamic> p in (dynamic)json) CsvContent(sb, p.Value);
				return sb;
			}

			if (json.IsArray)
			{
				foreach (dynamic element in (dynamic)json) CsvContent(sb, element);
				return sb;
			}
		}

		return sb.Append((object?)data).Append(',');
	}

	/// <summary>The node as a document: member names, tab-aligned, nested by indentation (FormJson.BuildDocument).</summary>
	public static string Document(object data) => DocumentContent(new StringBuilder(), data, 0).ToString();

	private static StringBuilder DocumentContent(StringBuilder sb, dynamic data, int indentLevel)
	{
		if (data is not JsonObject json) return sb;

		if (json.IsObject)
		{
			foreach (string p in json.GetDynamicMemberNames())
			{
				sb.AppendLine();
				sb.Append('\t', indentLevel);
				sb.Append(p);
				sb.Append('\t', (int)Math.Ceiling((24 - p.Length) / 4.0));
				sb.Append('：');

				DocumentContent(sb, ((dynamic)json)[p], indentLevel + 1);
			}
		}
		else if (json.IsArray)
		{
			sb.Append($"[{((dynamic[])(dynamic)json).Length}]");

			foreach (dynamic element in (dynamic)json)
			{
				if (element is JsonObject { IsObject: true } or JsonObject { IsArray: true })
				{
					DocumentContent(sb, element, indentLevel);
					break;
				}
			}
		}

		return sb;
	}
}
