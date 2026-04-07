using System;
using System.Collections.Generic;
using System.Text;
using XRL.World;

namespace ConsoleLib.Console;

public class MarkupControlNode : IMarkupNode
{
	public string Action;

	public List<IMarkupNode> Children = new List<IMarkupNode>(64);

	private static Queue<MarkupControlNode> markupControlNodePool = new Queue<MarkupControlNode>();

	[NonSerialized]
	public static Dictionary<char?, string> _charToString = new Dictionary<char?, string>();

	private static StringBuilder _markupBuilderSB = new StringBuilder(2048);

	private static HashSet<string> WarnedAbout = new HashSet<string>();

	public MarkupControlNode()
	{
	}

	public static MarkupControlNode next(string text = null)
	{
		MarkupControlNode markupControlNode;
		lock (markupControlNodePool)
		{
			markupControlNode = ((markupControlNodePool.Count > 0) ? markupControlNodePool.Dequeue() : null) ?? new MarkupControlNode();
		}
		markupControlNode.set(text);
		return markupControlNode;
	}

	public override void release()
	{
		foreach (IMarkupNode child in Children)
		{
			child.release();
		}
		Action = null;
		Children.Clear();
		lock (markupControlNodePool)
		{
			markupControlNodePool.Enqueue(this);
		}
	}

	public void set(string Text)
	{
		Action = Text;
	}

	public MarkupControlNode(string Action)
	{
		set(Action);
		this.Action = Action;
	}

	public MarkupControlNode(string Action, List<IMarkupNode> Children)
		: this(Action)
	{
		this.Children = Children;
	}

	public void Add(string text)
	{
		Children.Add(MarkupTextNode.next(text));
	}

	public void Add(MarkupControlNode node)
	{
		Children.Add(node);
	}

	public void ClearChildText()
	{
		int i = 0;
		for (int count = Children.Count; i < count; i++)
		{
			if (Children[i] is MarkupTextNode markupTextNode)
			{
				markupTextNode.Text = "";
			}
			if (Children[i] is MarkupControlNode markupControlNode)
			{
				markupControlNode.ClearChildText();
			}
		}
	}

	public static string charToString(char? c)
	{
		if (!c.HasValue)
		{
			return null;
		}
		string value = null;
		if (_charToString.TryGetValue(c, out value))
		{
			return value;
		}
		string text = c.ToString();
		_charToString.Add(c, text);
		return text;
	}

	private void AssembleCode(StringBuilder SB, char? foreground = null, char? background = null)
	{
		if (foreground.HasValue)
		{
			SB.Append(charToString('&')).Append(charToString(foreground));
		}
		if (background.HasValue)
		{
			SB.Append(charToString('^')).Append(charToString(background));
		}
	}

	private string AssembleCode(char? foreground = null, char? background = null)
	{
		lock (_markupBuilderSB)
		{
			_markupBuilderSB.Length = 0;
			AssembleCode(_markupBuilderSB, foreground, background);
			return _markupBuilderSB.ToString();
		}
	}

	public override string ToString()
	{
		return ToString(RefreshAtNewline: false);
	}

	public override string ToString(bool RefreshAtNewline)
	{
		lock (_markupBuilderSB)
		{
			_markupBuilderSB.Length = 0;
			ToStringBuilder(_markupBuilderSB, RefreshAtNewline);
			return _markupBuilderSB.ToString();
		}
	}

	public override void ToStringBuilder(StringBuilder SB, bool RefreshAtNewline, ref char? lastForeground, ref char? lastBackground)
	{
		IMarkupShader markupShader = null;
		if (!WarnedAbout.Contains(Action))
		{
			try
			{
				if (!string.IsNullOrEmpty(Action))
				{
					if (Action == "?KB")
					{
						if (ControlManager.activeControllerType == ControlManager.InputDeviceType.Gamepad)
						{
							ClearChildText();
						}
						markupShader = MarkupShaders.Find("y");
					}
					else if (Action == "?Gamepad")
					{
						if (ControlManager.activeControllerType != ControlManager.InputDeviceType.Gamepad)
						{
							ClearChildText();
						}
						markupShader = MarkupShaders.Find("y");
					}
					else
					{
						markupShader = MarkupShaders.Find(Action);
					}
				}
			}
			catch (Exception innerException)
			{
				MetricsManager.LogError(new Exception("Error finding Markup Shader \"" + Action + "\"", innerException));
				WarnedAbout.Add(Action);
			}
		}
		int num = 0;
		int num2 = 0;
		if (markupShader != null)
		{
			int i = 0;
			for (int count = Children.Count; i < count; i++)
			{
				if (Children[i] is MarkupTextNode markupTextNode)
				{
					num2 += markupTextNode.TextLength;
				}
			}
		}
		bool flag = false;
		char? c = lastForeground;
		char? c2 = lastBackground;
		int j = 0;
		for (int count2 = Children.Count; j < count2; j++)
		{
			if (Children[j] is MarkupTextNode markupTextNode2)
			{
				if (flag)
				{
					if (markupShader == null && !string.IsNullOrEmpty(Action))
					{
						SB.Append(Action);
					}
					flag = false;
				}
				if (markupShader == null)
				{
					markupTextNode2.ToStringBuilder(SB, RefreshAtNewline, ref lastForeground, ref lastBackground);
					c = lastForeground;
					c2 = lastBackground;
					continue;
				}
				int num3 = 0;
				int textLength = markupTextNode2.TextLength;
				char? c3 = null;
				int k = 0;
				for (int length = markupTextNode2.Text.Length; k < length; k++)
				{
					char c4 = markupTextNode2.Text[k];
					if (c4 == '&' && k < length - 1)
					{
						k++;
						c4 = markupTextNode2.Text[k];
						if (c4 != '&')
						{
							SB.Append('&').Append(c4);
							lastForeground = c4;
							if (num3 == 0)
							{
								c = markupShader.GetForegroundColor(c4, num3, textLength, num, num2);
							}
							continue;
						}
						c3 = '&';
					}
					else if (c4 == '^' && k < length - 1)
					{
						k++;
						c4 = markupTextNode2.Text[k];
						if (c4 != '^')
						{
							SB.Append('^').Append(c4);
							lastBackground = c4;
							if (num3 == 0)
							{
								c2 = markupShader.GetBackgroundColor(c4, num3, textLength, num, num2);
							}
							continue;
						}
						c3 = '^';
					}
					char? foregroundColor = markupShader.GetForegroundColor(c4, num3, textLength, num, num2);
					char? backgroundColor = markupShader.GetBackgroundColor(c4, num3, textLength, num, num2);
					if (foregroundColor.HasValue && foregroundColor != c)
					{
						if (backgroundColor.HasValue && backgroundColor != c2)
						{
							AssembleCode(SB, foregroundColor, backgroundColor);
							c = (lastForeground = foregroundColor);
							c2 = (lastBackground = backgroundColor);
						}
						else
						{
							AssembleCode(SB, foregroundColor);
							c = (lastForeground = foregroundColor);
						}
					}
					else if (backgroundColor.HasValue && backgroundColor != lastBackground)
					{
						char? background = backgroundColor;
						AssembleCode(SB, null, background);
						c2 = (lastBackground = backgroundColor);
					}
					SB.Append(c4);
					if (c3.HasValue)
					{
						SB.Append(c3);
						c3 = null;
					}
					if (RefreshAtNewline && c4 == '\n')
					{
						AssembleCode(SB, (foregroundColor == 'y') ? ((char?)null) : foregroundColor, (backgroundColor == 'k') ? ((char?)null) : backgroundColor);
					}
					num3++;
					num++;
				}
				continue;
			}
			flag = Children[j] is MarkupControlNode;
			Children[j].ToStringBuilder(SB, RefreshAtNewline, ref lastForeground, ref lastBackground);
			if (markupShader == null && string.IsNullOrEmpty(Action))
			{
				if (c != lastForeground)
				{
					SB.Append('&').Append(c ?? 'y');
					lastForeground = c;
				}
				if (c2 != lastBackground)
				{
					SB.Append('^').Append(c2 ?? 'k');
					lastBackground = c2;
				}
			}
			else
			{
				c = lastForeground;
				c2 = lastBackground;
			}
		}
	}

	public override void ToStringBuilder(StringBuilder SB, bool RefreshAtNewline)
	{
		char? lastForeground = null;
		char? lastBackground = 'k';
		ToStringBuilder(SB, RefreshAtNewline, ref lastForeground, ref lastBackground);
	}

	public override string DebugDump(int depth = 0)
	{
		StringBuilder stringBuilder = Event.NewStringBuilder();
		DebugDump(stringBuilder, depth);
		return stringBuilder.ToString();
	}

	public override void DebugDump(StringBuilder SB, int depth)
	{
		int i = 0;
		for (int num = depth * 2; i < num; i++)
		{
			SB.Append('_');
		}
		SB.Append("control node, ").Append(Children.Count).Append((Children.Count == 1) ? " child: " : " children: ")
			.Append((Action == null) ? "null" : Action.Replace("&", "&&").Replace("^", "^^"))
			.Append('\n');
		int j = 0;
		for (int count = Children.Count; j < count; j++)
		{
			Children[j].DebugDump(SB, depth + 1);
		}
	}
}
