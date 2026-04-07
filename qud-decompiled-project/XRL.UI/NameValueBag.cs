using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleJSON;

namespace XRL.UI;

public class NameValueBag
{
	private string fileName;

	public Dictionary<string, string> Bag;

	public NameValueBag(string Path)
	{
		fileName = Path;
		Bag = new Dictionary<string, string>();
	}

	public string GetValue(string Name, string Default = null)
	{
		if (Bag == null)
		{
			Load();
		}
		if (Bag.TryGetValue(Name, out var value))
		{
			return value;
		}
		return Default;
	}

	public void SetValue(string Name, string Value, bool FlushToFile = true)
	{
		Bag[Name] = Value;
		if (FlushToFile)
		{
			Flush();
		}
	}

	public void Load()
	{
		Bag = new Dictionary<string, string>();
		string text = "";
		if (File.Exists(fileName))
		{
			using StreamReader streamReader = new StreamReader(fileName);
			text = streamReader.ReadToEnd();
		}
		try
		{
			if (!(text != ""))
			{
				return;
			}
			foreach (KeyValuePair<string, JSONNode> item in JSON.Parse(text) as JSONClass)
			{
				SetValue(item.Key, item.Value);
			}
		}
		catch (Exception x)
		{
			MetricsManager.LogError("Failed to parse " + fileName + "!", x);
		}
	}

	public void Flush()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{\n");
		int num = 0;
		foreach (string key in Bag.Keys)
		{
			if (num != 0)
			{
				stringBuilder.Append(",\n");
			}
			num++;
			stringBuilder.Append("\"" + key + "\":\"" + Bag[key] + "\"");
		}
		stringBuilder.Append("\n}");
		using StreamWriter streamWriter = new StreamWriter(fileName);
		streamWriter.Write(stringBuilder.ToString());
	}
}
