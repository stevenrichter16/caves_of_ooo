using System.Text;

namespace XRL.UI;

public class TextBlockWord
{
	public StringBuilder Foreground = new StringBuilder();

	public StringBuilder Background = new StringBuilder();

	public StringBuilder Word = new StringBuilder();

	public int Length;

	public void Clear()
	{
		Foreground.Length = 0;
		Background.Length = 0;
		Word.Length = 0;
		Length = 0;
	}
}
