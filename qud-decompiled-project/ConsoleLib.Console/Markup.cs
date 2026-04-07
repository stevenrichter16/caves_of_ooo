using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ConsoleLib.Console;

public class Markup : MarkupControlNode
{
	public const char BASE_FOREGROUND = 'y';

	public const char BASE_BACKGROUND = 'k';

	public static readonly char[] NORMAL_FOREGROUNDS = new char[15]
	{
		'K', 'b', 'B', 'C', 'c', 'g', 'G', 'r', 'R', 'm',
		'M', 'W', 'w', 'Y', 'y'
	};

	private static string _BaseColorPattern;

	public static bool Enabled = true;

	private static ConcurrentStack<StringBuilder> Builders = new ConcurrentStack<StringBuilder>();

	private static Queue<Markup> markupNodePool = new Queue<Markup>();

	private static string BaseColorPattern
	{
		get
		{
			if (_BaseColorPattern == null)
			{
				_BaseColorPattern = "&y^k";
			}
			return _BaseColorPattern;
		}
	}

	public Markup()
	{
	}

	public Markup(string text)
		: this()
	{
		set(text);
	}

	public new void set(string text)
	{
		Action = BaseColorPattern;
		int pos = 0;
		ParseText(text, ref pos, this);
	}

	private static Markup Parse(string text)
	{
		Markup markup;
		lock (markupNodePool)
		{
			markup = ((markupNodePool.Count > 0) ? markupNodePool.Dequeue() : null) ?? new Markup();
		}
		markup.set(text);
		return markup;
	}

	public static string Transform(string text, bool refreshAtNewline = false)
	{
		if (Enabled && text != null && text.Contains("{{"))
		{
			Markup markup = Parse(text);
			string result = markup.ToString(refreshAtNewline);
			markup.release();
			return result;
		}
		return text;
	}

	public static void Transform(StringBuilder text, bool refreshAtNewline = false)
	{
		if (Enabled && text != null && text.Contains("{{"))
		{
			Markup markup = Parse(text.ToString());
			string value = markup.ToString(refreshAtNewline);
			text.Length = 0;
			text.Append(value);
			markup.release();
		}
	}

	public static List<string> Transform(List<string> text, bool refreshAtNewline = false)
	{
		return new List<string>(Transform(string.Join("\n", text.ToArray())).Split('\n'));
	}

	public static string Strip(string Text)
	{
		if (!Builders.TryPop(out var result))
		{
			result = new StringBuilder(Text.Length);
		}
		result.Append(Text);
		Strip(result);
		string result2 = result.ToString();
		result.Clear();
		Builders.Push(result);
		return result2;
	}

	public static void Strip(StringBuilder Text)
	{
		int i = 0;
		int num = -1;
		int num2 = 0;
		for (int num3 = Text.Length; i < num3; i++)
		{
			switch (Text[i])
			{
			case '{':
				if (i < num3 - 1 && Text[i + 1] == '{')
				{
					num = i++;
				}
				break;
			case '|':
				if (num != -1)
				{
					i++;
					Text.Remove(num, i - num);
					num3 -= i - num;
					i = num - 1;
					num2++;
					num = -1;
				}
				break;
			case '}':
				if (num2 > 0 && i < num3 - 1 && Text[i + 1] == '}')
				{
					Text.Remove(i, 2);
					num3 -= 2;
					num2--;
				}
				break;
			}
		}
	}

	public static string Wrap(string Text)
	{
		if (Text != null && (Text.Contains("&") || Text.Contains("^")) && !Text.StartsWith("{{"))
		{
			Text = "{{|" + Text + "}}";
		}
		return Text;
	}

	public static string Color(string Color, string Text)
	{
		return "{{" + Color + "|" + Text + "}}";
	}

	private static void ParseText(string text, ref int pos, MarkupControlNode parent)
	{
		int num = pos;
		while (pos < text.Length)
		{
			if (text[pos] == '{' && pos < text.Length - 1 && text[pos + 1] == '{')
			{
				if (pos > num)
				{
					parent.Add(text.Substring(num, pos - num));
				}
				pos += 2;
				ParseControl(text, ref pos, parent);
				num = pos;
			}
			else
			{
				pos++;
			}
		}
		if (pos > num)
		{
			parent.Add(text.Substring(num, pos - num));
		}
	}

	private static void ParseControl(string text, ref int pos, MarkupControlNode parent)
	{
		int num = pos;
		MarkupControlNode markupControlNode = MarkupControlNode.next();
		while (pos < text.Length)
		{
			char c = text[pos];
			if (c == '|' && markupControlNode.Action == null)
			{
				markupControlNode.Action = ((pos > num) ? text.Substring(num, pos - num) : "");
				pos++;
				num = pos;
				continue;
			}
			if (c == '}' && pos < text.Length - 1 && text[pos + 1] == '}')
			{
				if (markupControlNode.Action != null)
				{
					markupControlNode.Add(text.Substring(num, pos - num));
				}
				pos += 2;
				num = pos;
				break;
			}
			if (c == '{' && pos < text.Length - 1 && text[pos + 1] == '{')
			{
				if (markupControlNode.Action == null)
				{
					markupControlNode.Action = "";
				}
				if (pos > num)
				{
					markupControlNode.Add(text.Substring(num, pos - num));
				}
				pos += 2;
				ParseControl(text, ref pos, markupControlNode);
				num = pos;
			}
			else
			{
				pos++;
			}
		}
		if (pos > num)
		{
			markupControlNode.Add(text.Substring(num, pos - num));
		}
		if (markupControlNode.Children.Count > 0)
		{
			parent.Add(markupControlNode);
		}
	}

	public new static Markup next(string text = null)
	{
		Markup markup;
		lock (markupNodePool)
		{
			markup = ((markupNodePool.Count > 0) ? markupNodePool.Dequeue() : null) ?? new Markup();
		}
		markup.set(text);
		return markup;
	}

	public override void release()
	{
		foreach (IMarkupNode child in Children)
		{
			child.release();
		}
		Action = null;
		Children.Clear();
		lock (markupNodePool)
		{
			markupNodePool.Enqueue(this);
		}
	}
}
