using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ConsoleLib.Console;
using Qud.UI;
using XRL.World;

namespace XRL.UI;

public class TextBlock
{
	public List<string> Lines;

	private int MaxLines;

	private bool ReverseBlocks;

	[NonSerialized]
	private static StringBuilder FormatSB = new StringBuilder(2048);

	[NonSerialized]
	private static StringBuilder Line = new StringBuilder(2048);

	[NonSerialized]
	private static StringBuilder Foreground = new StringBuilder(2048);

	[NonSerialized]
	private static StringBuilder Background = new StringBuilder(2048);

	[NonSerialized]
	private static Dictionary<Thread, TextBlockWord> threadWords = new Dictionary<Thread, TextBlockWord>();

	[NonSerialized]
	private static char[] WordBreaks = new char[2] { ' ', '\n' };

	public int AdjustFirstLine;

	public int _Width = -1;

	private static TextBlockWord WordReturn
	{
		get
		{
			if (!threadWords.TryGetValue(Thread.CurrentThread, out var value))
			{
				value = new TextBlockWord();
				threadWords.Add(Thread.CurrentThread, value);
			}
			return value;
		}
	}

	public int Width
	{
		get
		{
			if (_Width == -1)
			{
				foreach (string line in Lines)
				{
					if (line.Length > _Width)
					{
						_Width = line.Length;
					}
				}
			}
			return _Width;
		}
	}

	public int Height => Lines.Count;

	public void Format(string String)
	{
		if (Lines == null)
		{
			Lines = new List<string>();
		}
		if (String == null || String.Length == 0)
		{
			Lines.Clear();
			return;
		}
		lock (FormatSB)
		{
			Format(FormatSB.Clear().Append(String));
		}
	}

	public void Format(StringBuilder String)
	{
		if (Lines == null)
		{
			Lines = new List<string>();
		}
		Lines.Clear();
		if (String == null || String.Length == 0)
		{
			return;
		}
		Markup.Transform(String);
		lock (Line)
		{
			int num = 0;
			Line.Length = 0;
			Foreground.Length = 0;
			Background.Length = 0;
			int i = 0;
			int num2 = Width + AdjustFirstLine;
			List<string> list = (ReverseBlocks ? new List<string>() : null);
			while (i < String.Length)
			{
				TextBlockWord nextWord = GetNextWord(String, i);
				if (nextWord.Length + num + 2 > num2)
				{
					if (Lines.Count > 0 || Line.Length > 0)
					{
						if (ReverseBlocks)
						{
							list.Add(Line.ToString());
						}
						else
						{
							Lines.Add(Line.ToString());
						}
					}
					Line.Length = 0;
					num = 0;
					if (Foreground.Length > 0)
					{
						Line.Append('&').Append(Foreground);
					}
					if (Background.Length > 0)
					{
						Line.Append('^').Append(Background);
					}
					if (Lines.Count >= MaxLines)
					{
						break;
					}
					num2 = Width;
				}
				i += nextWord.Word.Length;
				Line.Append(nextWord.Word).Append(' ');
				num += nextWord.Length + 1;
				if (nextWord.Foreground.Length > 0)
				{
					Foreground.Length = 0;
					Foreground.Append(nextWord.Foreground);
				}
				if (nextWord.Background.Length > 0)
				{
					Background.Length = 0;
					Background.Append(nextWord.Background);
				}
				for (; i < String.Length && (String[i] == '\n' || String[i] == ' '); i++)
				{
					if (String[i] == '\n')
					{
						if (ReverseBlocks)
						{
							list.Add(Line.ToString());
						}
						else
						{
							Lines.Add(Line.ToString());
						}
						Line.Length = 0;
						num = 0;
						if (Foreground != null && Foreground.Length > 0)
						{
							Line.Append('&').Append(Foreground);
							num = num;
						}
						if (Background != null && Background.Length > 0)
						{
							Line.Append('^').Append(Background);
							num = num;
						}
						if (ReverseBlocks && list.Count > 0)
						{
							list.Reverse();
							Lines.AddRange(list);
							list.Clear();
						}
						if (Lines.Count >= MaxLines)
						{
							break;
						}
					}
				}
			}
			if (ReverseBlocks)
			{
				if (Line.Length > 0)
				{
					list.Add(Line.ToString());
				}
				if (list.Count > 0)
				{
					list.Reverse();
					Lines.AddRange(list);
				}
			}
			else if (Line.Length > 0)
			{
				Lines.Add(Line.ToString());
			}
		}
	}

	public TextBlock(string String, int Width, int MaxLines, bool ReverseBlocks = false, int AdjustFirstLine = 0)
	{
		_Width = Width;
		this.MaxLines = MaxLines;
		this.ReverseBlocks = ReverseBlocks;
		this.AdjustFirstLine = AdjustFirstLine;
		Format(String);
	}

	public TextBlock(StringBuilder String, int Width, int MaxLines, bool ReverseBlocks = false, int AdjustFirstLine = 0)
	{
		_Width = Width;
		this.MaxLines = MaxLines;
		this.ReverseBlocks = ReverseBlocks;
		this.AdjustFirstLine = AdjustFirstLine;
		Format(String);
	}

	private TextBlockWord GetNextWord(StringBuilder Str, int StartPos)
	{
		TextBlockWord wordReturn = WordReturn;
		wordReturn.Clear();
		if (Str[StartPos] == '\n')
		{
			wordReturn.Word.Append('\n');
			wordReturn.Length = 1;
			return wordReturn;
		}
		int num = Str.CleanContainsStartFrom(StartPos, WordBreaks);
		if (num == -1)
		{
			int i = StartPos;
			for (int length = Str.Length; i < length; i++)
			{
				wordReturn.Word.Append(Str[i]);
			}
		}
		else
		{
			Str.Substring(StartPos, num - StartPos, wordReturn.Word);
		}
		GetLastForeground(wordReturn.Word, wordReturn.Foreground);
		GetLastBackground(wordReturn.Word, wordReturn.Background);
		wordReturn.Length = ColorUtility.LengthExceptFormatting(wordReturn.Word);
		return wordReturn;
	}

	private void GetLastForeground(StringBuilder Str, StringBuilder Output)
	{
		Output.Length = 0;
		int num = Str.IndexOf('&');
		if (num == -1)
		{
			return;
		}
		int i = num;
		for (int num2 = Str.Length - 1; i < num2; i++)
		{
			if (Str[i] == '&')
			{
				i++;
				if (Str[i] != '&')
				{
					Output.Length = 0;
					Output.Append(Str[i]);
				}
			}
		}
	}

	private void GetLastBackground(StringBuilder Str, StringBuilder Output)
	{
		Output.Length = 0;
		int num = Str.IndexOf('^');
		if (num == -1)
		{
			return;
		}
		int i = num;
		for (int num2 = Str.Length - 1; i < num2; i++)
		{
			if (Str[i] == '^')
			{
				i++;
				if (Str[i] != '^')
				{
					Output.Length = 0;
					Output.Append(Str[i]);
				}
			}
		}
	}

	public StringBuilder GetStringBuilder()
	{
		StringBuilder stringBuilder = Event.NewStringBuilder();
		for (int i = 0; i < Lines.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append('\n');
			}
			stringBuilder.Append(Lines[i]);
		}
		return stringBuilder;
	}

	public string GetRTFBlock()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < Lines.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append('\n');
			}
			stringBuilder.Append(Lines[i]);
		}
		return RTF.FormatToRTF(stringBuilder.ToString());
	}
}
