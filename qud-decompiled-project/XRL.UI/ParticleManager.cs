using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ConsoleLib.Console;
using XRL.Messages;
using XRL.Rules;

namespace XRL.UI;

public class ParticleManager
{
	public TextConsole _TextConsole;

	public ScreenBuffer _ScreenBuffer;

	public CleanQueue<BannerText> Banners = new CleanQueue<BannerText>();

	public List<TextParticle> Particles = new List<TextParticle>();

	public List<TileParticle> TileParticles = new List<TileParticle>();

	public List<RadialTextParticle> RadialParticles = new List<RadialTextParticle>();

	public List<SinusoidalTextParticle> SinusoidalParticles = new List<SinusoidalTextParticle>();

	[NonSerialized]
	public static CleanQueue<TextParticle> textParticlesQueue = new CleanQueue<TextParticle>();

	[NonSerialized]
	public static CleanQueue<TileParticle> tileParticlesQueue = new CleanQueue<TileParticle>();

	[NonSerialized]
	public static CleanQueue<SinusoidalTextParticle> sinParticlesQueue = new CleanQueue<SinusoidalTextParticle>();

	[NonSerialized]
	public static CleanQueue<RadialTextParticle> radParticlesQueue = new CleanQueue<RadialTextParticle>();

	private StringBuilder SB = new StringBuilder(1024);

	private List<TextParticle> Removed = new List<TextParticle>();

	private List<TileParticle> TileRemoved = new List<TileParticle>();

	private List<RadialTextParticle> RadialRemoved = new List<RadialTextParticle>();

	private List<SinusoidalTextParticle> SinRemoved = new List<SinusoidalTextParticle>();

	private static Stopwatch frameTimer = null;

	private static long lastElapsed = 0L;

	private static long accumulator = 0L;

	public void reset()
	{
		Banners = new CleanQueue<BannerText>();
		Particles = new List<TextParticle>();
		TileParticles = new List<TileParticle>();
		RadialParticles = new List<RadialTextParticle>();
		SinusoidalParticles = new List<SinusoidalTextParticle>();
	}

	public void Init(TextConsole TextConsole_, ScreenBuffer ScreenBuffer_)
	{
		_TextConsole = TextConsole_;
		_ScreenBuffer = ScreenBuffer_;
	}

	public void AddBanner(string Text, BannerTextType Type)
	{
		BannerText bannerText = new BannerText();
		bannerText.Block = new TextBlock(Text, 40, 25);
		bannerText.Type = Type;
		bannerText.x = 40 - bannerText.Block.Width / 2;
		bannerText.y = 23 - bannerText.Block.Height;
		Banners.Enqueue(bannerText);
		MessageQueue.AddPlayerMessage(Text);
	}

	public void AddRadial(string Text, float x, float y, float r, float d, float rdel, float ddel)
	{
		AddRadial(Text, x, y, r, d, rdel, ddel, 999, 0L);
	}

	public void Add(string Text, float x, float y, float xDel, float yDel)
	{
		Add(Text, x, y, xDel, yDel, 999, 0f, 0f, 0L);
	}

	public void Add(string Text, float x, float y, float xDel, float yDel, int Life)
	{
		Add(Text, x, y, xDel, yDel, Life, 0f, 0f, 0L);
	}

	public void Add(string Text, float x, float y, float xDel, float yDel, int Life, float xDelDel, float yDelDel, long delayMS = 0L)
	{
		if (Particles.Count <= 1000)
		{
			if (Text == "@" && Options.GetOption("OptionDisableTextWarpEffects") == "Yes")
			{
				Text = "*";
			}
			TextParticle textParticle = ((textParticlesQueue.Count <= 0) ? new TextParticle() : textParticlesQueue.Dequeue());
			textParticle.x = x;
			textParticle.y = y;
			textParticle.xDel = xDel;
			textParticle.yDel = yDel;
			textParticle.xDelDel = xDelDel;
			textParticle.yDelDel = yDelDel;
			textParticle.Text = Text;
			textParticle.TextBlock = new TextBlock(Text, 12, 8);
			textParticle.Life = Life;
			textParticle.DelayMS = delayMS;
			Particles.Add(textParticle);
		}
	}

	public void AddTile(string Tile, string ColorString, string DetailColor, float x, float y, float xDel, float yDel, int Life, float xDelDel, float yDelDel, bool HFlip = false, bool VFlip = false, long delayMS = 0L)
	{
		if (Particles.Count <= 1000)
		{
			TileParticle tileParticle = ((tileParticlesQueue.Count <= 0) ? new TileParticle() : tileParticlesQueue.Dequeue());
			tileParticle.x = x;
			tileParticle.y = y;
			tileParticle.xDel = xDel;
			tileParticle.yDel = yDel;
			tileParticle.xDelDel = xDelDel;
			tileParticle.yDelDel = yDelDel;
			tileParticle.Tile = Tile;
			tileParticle.ColorString = ColorString;
			tileParticle.DetailColor = DetailColor;
			tileParticle.Life = Life;
			tileParticle.bHFlipped = HFlip;
			tileParticle.bVFlipped = VFlip;
			tileParticle.DelayMS = delayMS;
			TileParticles.Add(tileParticle);
		}
	}

	public void AddSinusoidal(string Text, float x, float y, float amp, float sinstart, float sinincrement, float xamp, float yamp, float xdel, float ydel, int Life, long delayMS = 0L)
	{
		if (SinusoidalParticles.Count <= 100)
		{
			if (Text == "@" && Options.GetOption("OptionDisableTextWarpEffects") == "Yes")
			{
				Text = "*";
			}
			SinusoidalTextParticle sinusoidalTextParticle = ((sinParticlesQueue.Count <= 0) ? new SinusoidalTextParticle() : sinParticlesQueue.Dequeue());
			sinusoidalTextParticle.x = x;
			sinusoidalTextParticle.y = y;
			sinusoidalTextParticle.amp = amp;
			sinusoidalTextParticle.xamp = xamp;
			sinusoidalTextParticle.yamp = yamp;
			sinusoidalTextParticle.sinstart = sinstart;
			sinusoidalTextParticle.sinincrement = sinincrement;
			sinusoidalTextParticle.ydel = ydel;
			sinusoidalTextParticle.xdel = xdel;
			sinusoidalTextParticle.Text = Text;
			sinusoidalTextParticle.TextBlock = new TextBlock(Text, 12, 8);
			sinusoidalTextParticle.Life = Life;
			sinusoidalTextParticle.DelayMS = delayMS;
			SinusoidalParticles.Add(sinusoidalTextParticle);
		}
	}

	public void AddRadial(string Text, float x, float y, float r, float d, float rdel, float ddel, int Life, long delayMS = 0L)
	{
		if (RadialParticles.Count <= 100)
		{
			if (Text == "@" && Options.GetOption("OptionDisableTextWarpEffects") == "Yes")
			{
				Text = "*";
			}
			RadialTextParticle radialTextParticle = ((radParticlesQueue.Count <= 0) ? new RadialTextParticle() : radParticlesQueue.Dequeue());
			radialTextParticle.x = x;
			radialTextParticle.y = y;
			radialTextParticle.r = r;
			radialTextParticle.d = d;
			radialTextParticle.rdel = rdel;
			radialTextParticle.ddel = ddel;
			radialTextParticle.Text = Text;
			radialTextParticle.TextBlock = new TextBlock(Text, 12, 8);
			radialTextParticle.Life = Life;
			radialTextParticle.DelayMS = delayMS;
			RadialParticles.Add(radialTextParticle);
		}
	}

	public void Render(ScreenBuffer Buffer)
	{
		if (!Options.UseTextParticleVFX)
		{
			return;
		}
		if (Removed.Count > 0)
		{
			Removed.Clear();
		}
		if (TileRemoved.Count > 0)
		{
			TileRemoved.Clear();
		}
		if (RadialRemoved.Count > 0)
		{
			RadialRemoved.Clear();
		}
		if (SinRemoved.Count > 0)
		{
			SinRemoved.Clear();
		}
		for (int i = 0; i < Particles.Count && Particles[i].DelayMS <= 0; i++)
		{
			TextParticle textParticle = Particles[i];
			int num = Convert.ToInt32(textParticle.x);
			int num2 = Convert.ToInt32(textParticle.y);
			if (num < 0 || num > 79 || num2 < 0 || num2 > 24)
			{
				Removed.Add(textParticle);
			}
			if (num < 0)
			{
				num = 0;
			}
			if (num > 79)
			{
				num = 79;
			}
			if (num2 < 0)
			{
				num2 = 0;
			}
			if (num2 > 24)
			{
				num2 = 24;
			}
			if (textParticle.Life <= 0)
			{
				Removed.Add(textParticle);
			}
			Buffer.Goto(num, num2);
			if (textParticle.Text == "firefly")
			{
				Buffer.Buffer[num, num2].Char = 'ú';
				if (textParticle.Life < 10)
				{
					Buffer.Buffer[num, num2].SetColorsFromOldCharCode(ColorUtility.MakeColor(TextColor.Blue, TextColor.Black));
				}
				else if (textParticle.Life < 20)
				{
					Buffer.Buffer[num, num2].SetColorsFromOldCharCode(ColorUtility.MakeColor(ColorUtility.Bright(TextColor.Blue), TextColor.Black));
				}
				else if (textParticle.Life < 30)
				{
					Buffer.Buffer[num, num2].SetColorsFromOldCharCode(ColorUtility.MakeColor(TextColor.Cyan, TextColor.Black));
				}
				else if (textParticle.Life < 40)
				{
					Buffer.Buffer[num, num2].SetColorsFromOldCharCode(ColorUtility.MakeColor(ColorUtility.Bright(TextColor.Cyan), TextColor.Black));
				}
				else if (textParticle.Life < 50)
				{
					Buffer.Buffer[num, num2].SetColorsFromOldCharCode(ColorUtility.MakeColor(ColorUtility.Bright(TextColor.Blue), TextColor.Black));
				}
				else if (textParticle.Life < 60)
				{
					Buffer.Buffer[num, num2].SetColorsFromOldCharCode(ColorUtility.MakeColor(TextColor.Blue, TextColor.Black));
				}
			}
			else if (textParticle.Text[0] == '@')
			{
				int num3 = num;
				int num4 = num2;
				if ((float)num < textParticle.x)
				{
					num3--;
				}
				if ((float)num > textParticle.x)
				{
					num3++;
				}
				if ((float)num2 < textParticle.y)
				{
					num4--;
				}
				if ((float)num2 > textParticle.y)
				{
					num4++;
				}
				if (num3 < 0)
				{
					num3 = 0;
				}
				if (num3 > 79)
				{
					num3 = 79;
				}
				if (num4 < 0)
				{
					num4 = 0;
				}
				if (num4 > 24)
				{
					num4 = 24;
				}
				if ((float)num != textParticle.x || (float)num2 != textParticle.y)
				{
					Buffer.Buffer[num, num2].Copy(Buffer.Buffer[num3, num4]);
				}
			}
			else
			{
				Buffer.Write(textParticle.Text);
			}
		}
		for (int j = 0; j < TileParticles.Count && TileParticles[j].DelayMS <= 0; j++)
		{
			TileParticle tileParticle = TileParticles[j];
			int num5 = Convert.ToInt32(tileParticle.x);
			int num6 = Convert.ToInt32(tileParticle.y);
			if (num5 < 0 || num5 > 79 || num6 < 0 || num6 > 24)
			{
				TileRemoved.Add(tileParticle);
				continue;
			}
			if (tileParticle.Life <= 0)
			{
				TileRemoved.Add(tileParticle);
				continue;
			}
			Buffer.Goto(num5, num6);
			if (string.IsNullOrEmpty(tileParticle.Tile))
			{
				continue;
			}
			Buffer.Buffer[num5, num6].Tile = tileParticle.Tile;
			if (!string.IsNullOrEmpty(tileParticle.ColorString))
			{
				ushort foreground = 0;
				int num7 = tileParticle.ColorString.LastIndexOf('&');
				if (num7 != -1)
				{
					foreground = ColorUtility.CharToColorMap[tileParticle.ColorString[num7 + 1]];
				}
				ushort background = 0;
				int num8 = tileParticle.ColorString.LastIndexOf('^');
				if (num8 != -1)
				{
					background = ColorUtility.CharToColorMap[tileParticle.ColorString[num8 + 1]];
				}
				Buffer.Buffer[num5, num6].SetColorsFromOldCharCode(ColorUtility.MakeColor(foreground, background));
			}
			if (!string.IsNullOrEmpty(tileParticle.DetailColor))
			{
				Buffer.Buffer[num5, num6].Detail = ColorUtility.ColorMap[tileParticle.DetailColor[0]];
			}
			Buffer.Buffer[num5, num6].HFlip = tileParticle.bHFlipped;
			Buffer.Buffer[num5, num6].VFlip = tileParticle.bVFlipped;
		}
		for (int k = 0; k < RadialParticles.Count && RadialParticles[k].DelayMS <= 0; k++)
		{
			RadialTextParticle radialTextParticle = RadialParticles[k];
			int num9 = Convert.ToInt32(radialTextParticle.x) + (int)((double)radialTextParticle.d * Math.Cos(radialTextParticle.r));
			int num10 = Convert.ToInt32(radialTextParticle.y) + (int)((double)radialTextParticle.d * Math.Sin(radialTextParticle.r));
			if (radialTextParticle.Life <= 0)
			{
				RadialRemoved.Add(radialTextParticle);
			}
			if (radialTextParticle.d < 1f)
			{
				RadialRemoved.Add(radialTextParticle);
			}
			if (num9 < 0 || num9 > 79 || num10 < 0 || num10 > 24)
			{
				continue;
			}
			Buffer.Goto(num9, num10);
			if (radialTextParticle.Text[0] == '@')
			{
				int num11 = num9;
				int num12 = num10;
				if ((float)num9 < radialTextParticle.x)
				{
					num11--;
				}
				if ((float)num9 > radialTextParticle.x)
				{
					num11++;
				}
				if ((float)num10 < radialTextParticle.y)
				{
					num12--;
				}
				if ((float)num10 > radialTextParticle.y)
				{
					num12++;
				}
				if (num11 < 0)
				{
					num11 = 0;
				}
				if (num11 > 79)
				{
					num11 = 79;
				}
				if (num12 < 0)
				{
					num12 = 0;
				}
				if (num12 > 24)
				{
					num12 = 24;
				}
				if ((float)num9 != radialTextParticle.x || (float)num10 != radialTextParticle.y)
				{
					Buffer.Buffer[num9, num10].Char = Buffer.Buffer[num11, num12].Char;
					Buffer.Buffer[num9, num10].Tile = Buffer.Buffer[num11, num12].Tile;
					Buffer.Buffer[num9, num10].Foreground = Buffer.Buffer[num11, num12].Foreground;
					Buffer.Buffer[num9, num10].Background = Buffer.Buffer[num11, num12].Background;
				}
			}
			else
			{
				Buffer.Write(radialTextParticle.Text);
			}
		}
		for (int l = 0; l < SinusoidalParticles.Count && SinusoidalParticles[l].DelayMS <= 0; l++)
		{
			SinusoidalTextParticle sinusoidalTextParticle = SinusoidalParticles[l];
			int num13 = Convert.ToInt32(sinusoidalTextParticle.x) + (int)(Math.Sin(sinusoidalTextParticle.sinstart) * (double)sinusoidalTextParticle.amp * (double)sinusoidalTextParticle.xamp);
			int num14 = Convert.ToInt32(sinusoidalTextParticle.y) + (int)(Math.Sin(sinusoidalTextParticle.sinstart) * (double)sinusoidalTextParticle.amp * (double)sinusoidalTextParticle.yamp);
			if (sinusoidalTextParticle.Life <= 0)
			{
				SinRemoved.Add(sinusoidalTextParticle);
			}
			if (num13 < 0 || num13 > 79 || num14 < 0 || num14 > 24)
			{
				continue;
			}
			Buffer.Goto(num13, num14);
			if (sinusoidalTextParticle.Text[0] == '@')
			{
				int num15 = num13;
				int num16 = num14;
				if ((float)num13 < sinusoidalTextParticle.x)
				{
					num15--;
				}
				if ((float)num13 > sinusoidalTextParticle.x)
				{
					num15++;
				}
				if ((float)num14 < sinusoidalTextParticle.y)
				{
					num16--;
				}
				if ((float)num14 > sinusoidalTextParticle.y)
				{
					num16++;
				}
				if (num15 < 0)
				{
					num15 = 0;
				}
				if (num15 > 79)
				{
					num15 = 79;
				}
				if (num16 < 0)
				{
					num16 = 0;
				}
				if (num16 > 24)
				{
					num16 = 24;
				}
				if ((float)num13 != sinusoidalTextParticle.x || (float)num14 != sinusoidalTextParticle.y)
				{
					Buffer.Buffer[num13, num14].Char = Buffer.Buffer[num15, num16].Char;
					Buffer.Buffer[num13, num14].Tile = Buffer.Buffer[num15, num16].Tile;
					Buffer.Buffer[num13, num14].Foreground = Buffer.Buffer[num15, num16].Foreground;
					Buffer.Buffer[num13, num14].Background = Buffer.Buffer[num15, num16].Background;
				}
			}
			else
			{
				Buffer.Write(sinusoidalTextParticle.Text);
			}
		}
		if (Banners.Count > 0)
		{
			BannerText bannerText = Banners.Peek();
			string value = "&w";
			string value2 = "&W";
			string value3 = "&Y";
			if (bannerText.Type == BannerTextType.GoldenText)
			{
				value = "&w";
				value2 = "&W";
				value3 = "&Y";
			}
			if (bannerText.Complete)
			{
				bannerText.Life--;
				bannerText.Complete = true;
				int num17 = 0;
				foreach (string line in bannerText.Block.Lines)
				{
					Buffer.Goto(bannerText.x, bannerText.y + num17);
					Buffer.Write(line);
					num17++;
				}
			}
			else
			{
				bannerText.Complete = true;
				int num18 = 0;
				foreach (string line2 in bannerText.Block.Lines)
				{
					string text = ColorUtility.StripFormatting(line2);
					Buffer.Goto(bannerText.x, bannerText.y + num18);
					for (int m = 0; m < text.Length; m++)
					{
						int num19 = m / 4 * 3;
						int num20 = m / 4 * 3 + 1;
						int num21 = m / 4 * 3 + 2;
						if (bannerText.Time < num19)
						{
							bannerText.Complete = false;
							break;
						}
						if (bannerText.Time >= num21)
						{
							SB.Length = 0;
							SB.Append(value3);
							SB.Append(text[m]);
							Buffer.Write(SB.ToString());
						}
						else if (bannerText.Time >= num20)
						{
							SB.Length = 0;
							SB.Append(value2);
							SB.Append(text[m]);
							Buffer.Write(SB.ToString());
						}
						else
						{
							SB.Length = 0;
							SB.Append(value);
							SB.Append("*");
							Buffer.Write(SB.ToString());
							AddRadial("&Y.", Buffer.X, Buffer.Y, Stat.RandomCosmetic(0, 5), Stat.RandomCosmetic(5, 10), 0f, 0.05f * (float)Stat.RandomCosmetic(3, 7), 15, 0L);
						}
					}
					num18++;
				}
				bannerText.Time++;
			}
			if (bannerText.Complete && bannerText.Life <= 0)
			{
				Banners.Dequeue();
			}
		}
		foreach (TextParticle item in Removed)
		{
			textParticlesQueue.Enqueue(item);
			Particles.Remove(item);
		}
		foreach (TileParticle item2 in TileRemoved)
		{
			tileParticlesQueue.Enqueue(item2);
			TileParticles.Remove(item2);
		}
		foreach (RadialTextParticle item3 in RadialRemoved)
		{
			radParticlesQueue.Enqueue(item3);
			RadialParticles.Remove(item3);
		}
		foreach (SinusoidalTextParticle item4 in SinRemoved)
		{
			sinParticlesQueue.Enqueue(item4);
			SinusoidalParticles.Remove(item4);
		}
	}

	public void Frame()
	{
		if (frameTimer == null)
		{
			frameTimer = new Stopwatch();
			frameTimer.Reset();
			frameTimer.Start();
		}
		long num = frameTimer.ElapsedMilliseconds - lastElapsed;
		if (num > 64)
		{
			num = 64L;
		}
		if (num < 1)
		{
			num = 1L;
		}
		lastElapsed = frameTimer.ElapsedMilliseconds;
		accumulator += num;
		if (accumulator > 32)
		{
			accumulator = 32L;
		}
		while (accumulator > 16)
		{
			accumulator -= 16L;
			if (Removed.Count > 0)
			{
				Removed.Clear();
			}
			if (TileRemoved.Count > 0)
			{
				TileRemoved.Clear();
			}
			if (RadialRemoved.Count > 0)
			{
				RadialRemoved.Clear();
			}
			if (SinRemoved.Count > 0)
			{
				SinRemoved.Clear();
			}
			foreach (TextParticle particle in Particles)
			{
				if (particle.DelayMS > 0)
				{
					particle.DelayMS -= num;
					continue;
				}
				particle.Life--;
				particle.x += particle.xDel;
				particle.y += particle.yDel;
				particle.xDel += particle.xDelDel;
				particle.yDel += particle.yDelDel;
				if (particle.x < 0f)
				{
					Removed.Add(particle);
				}
				else if (particle.x > 79f)
				{
					Removed.Add(particle);
				}
				else if (particle.y < 0f)
				{
					Removed.Add(particle);
				}
				else if (particle.y > 24f)
				{
					Removed.Add(particle);
				}
			}
			foreach (TileParticle tileParticle in TileParticles)
			{
				if (tileParticle.DelayMS > 0)
				{
					tileParticle.DelayMS -= num;
					continue;
				}
				tileParticle.Life--;
				tileParticle.x += tileParticle.xDel;
				tileParticle.y += tileParticle.yDel;
				tileParticle.xDel += tileParticle.xDelDel;
				tileParticle.yDel += tileParticle.yDelDel;
				if (tileParticle.x < 0f)
				{
					TileRemoved.Add(tileParticle);
				}
				else if (tileParticle.x > 79f)
				{
					TileRemoved.Add(tileParticle);
				}
				else if (tileParticle.y < 0f)
				{
					TileRemoved.Add(tileParticle);
				}
				else if (tileParticle.y > 24f)
				{
					TileRemoved.Add(tileParticle);
				}
			}
			foreach (RadialTextParticle radialParticle in RadialParticles)
			{
				if (radialParticle.DelayMS > 0)
				{
					radialParticle.DelayMS -= num;
					continue;
				}
				radialParticle.Life--;
				radialParticle.r += radialParticle.rdel;
				radialParticle.d += radialParticle.ddel;
				if (radialParticle.d < 1f)
				{
					RadialRemoved.Add(radialParticle);
				}
				while (radialParticle.r < 0f)
				{
					radialParticle.r += 6.28f;
				}
				while ((double)radialParticle.r > 6.28)
				{
					radialParticle.r -= 6.28f;
				}
			}
			foreach (SinusoidalTextParticle sinusoidalParticle in SinusoidalParticles)
			{
				if (sinusoidalParticle.DelayMS > 0)
				{
					sinusoidalParticle.DelayMS -= num;
					continue;
				}
				sinusoidalParticle.Life--;
				sinusoidalParticle.x += sinusoidalParticle.xdel;
				sinusoidalParticle.y += sinusoidalParticle.ydel;
				sinusoidalParticle.sinstart += sinusoidalParticle.sinincrement;
				if (sinusoidalParticle.x < 0f)
				{
					SinRemoved.Add(sinusoidalParticle);
				}
				else if (sinusoidalParticle.x > 79f)
				{
					SinRemoved.Add(sinusoidalParticle);
				}
				else if (sinusoidalParticle.y < 0f)
				{
					SinRemoved.Add(sinusoidalParticle);
				}
				else if (sinusoidalParticle.y > 24f)
				{
					SinRemoved.Add(sinusoidalParticle);
				}
				while (sinusoidalParticle.sinstart < 0f)
				{
					sinusoidalParticle.sinstart += 6.28f;
				}
				while ((double)sinusoidalParticle.sinstart > 6.28)
				{
					sinusoidalParticle.sinstart -= 6.28f;
				}
			}
			foreach (TextParticle item in Removed)
			{
				Particles.Remove(item);
			}
			foreach (TileParticle item2 in TileRemoved)
			{
				TileParticles.Remove(item2);
			}
			foreach (SinusoidalTextParticle item3 in SinRemoved)
			{
				SinusoidalParticles.Remove(item3);
			}
			foreach (RadialTextParticle item4 in RadialRemoved)
			{
				RadialParticles.Remove(item4);
			}
		}
	}
}
