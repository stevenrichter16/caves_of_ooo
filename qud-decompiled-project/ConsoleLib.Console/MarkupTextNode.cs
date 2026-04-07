using System.Collections.Generic;
using System.Text;
using XRL.World;

namespace ConsoleLib.Console;

public class MarkupTextNode : IMarkupNode
{
	private string _Text;

	private int _TextLength = -1;

	private static Queue<MarkupTextNode> markupTextNodePool = new Queue<MarkupTextNode>();

	public string Text
	{
		get
		{
			return _Text;
		}
		set
		{
			_Text = value;
			_TextLength = -1;
		}
	}

	public int TextLength
	{
		get
		{
			if (_TextLength == -1)
			{
				_TextLength = ColorUtility.LengthExceptFormatting(_Text);
			}
			return _TextLength;
		}
	}

	public static MarkupTextNode next(string text = null)
	{
		MarkupTextNode markupTextNode;
		lock (markupTextNodePool)
		{
			markupTextNode = ((markupTextNodePool.Count > 0) ? markupTextNodePool.Dequeue() : null) ?? new MarkupTextNode();
		}
		markupTextNode.set(text);
		return markupTextNode;
	}

	public override void release()
	{
		_Text = null;
		_TextLength = -1;
		lock (markupTextNodePool)
		{
			markupTextNodePool.Enqueue(this);
		}
	}

	public void set(string text)
	{
		Text = text;
	}

	public override string ToString()
	{
		return ToString(RefreshAtNewline: false);
	}

	public override string ToString(bool RefreshAtNewline)
	{
		return Text;
	}

	public override void ToStringBuilder(StringBuilder SB, bool RefreshAtNewline, ref char? lastForeground, ref char? lastBackground)
	{
		SB.Append(Text);
		ColorUtility.FindLastForegroundAndBackground(Text, ref lastForeground, ref lastBackground);
	}

	public override void ToStringBuilder(StringBuilder SB, bool RefreshAtNewline)
	{
		SB.Append(Text);
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
		SB.Append("text: ").Append(Text.Replace("\n", "\\n")).Append('\n');
	}
}
