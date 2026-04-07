using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace XRL.Core;

public class Scoreboard2
{
	private SortScoreEntry2 sorter = new SortScoreEntry2();

	public List<ScoreEntry2> Scores = new List<ScoreEntry2>();

	public void Sort()
	{
		Scores.Sort(sorter);
	}

	public void Add(int Score, string Details, long Turn, string GameId, string GameMode, bool ReplaceOnId = false, int Level = 0, string Name = "")
	{
		if (ReplaceOnId && !string.IsNullOrEmpty(GameId))
		{
			if (Scores.Any((ScoreEntry2 e) => e.GameId == GameId))
			{
				if (Scores.Where((ScoreEntry2 e) => e.GameId == GameId).First().Score < Score)
				{
					Scores.RemoveAll((ScoreEntry2 e) => e.GameId == GameId);
					Scores.Add(new ScoreEntry2(Score, Details, Turn, GameId, GameMode, Level, Name));
				}
			}
			else
			{
				Scores.Add(new ScoreEntry2(Score, Details, Turn, GameId, GameMode, Level, Name));
			}
		}
		else
		{
			Scores.Add(new ScoreEntry2(Score, Details, Turn, GameId, GameMode, Level, Name));
		}
		Save();
	}

	public void Save()
	{
		try
		{
			Sort();
			File.WriteAllText(DataManager.SyncedPath("HighScores.json"), JsonConvert.SerializeObject(this));
		}
		catch (Exception x)
		{
			MetricsManager.LogException("Scoreboard Save", x);
		}
	}

	public static Scoreboard2 Load()
	{
		Scoreboard2 scoreboard = null;
		try
		{
			if (File.Exists(DataManager.SyncedPath("HighScores.json")))
			{
				scoreboard = JsonConvert.DeserializeObject<Scoreboard2>(File.ReadAllText(DataManager.SyncedPath("HighScores.json")));
			}
			else if (File.Exists(DataManager.SavePath("HighScores.dat")))
			{
				using Stream stream = File.OpenRead(DataManager.SavePath("HighScores.dat"));
				Scoreboard obj = ((IFormatter)new BinaryFormatter()).Deserialize(stream) as Scoreboard;
				stream.Close();
				Scoreboard2 scoreboard2 = new Scoreboard2();
				foreach (ScoreEntry score in obj.Scores)
				{
					scoreboard2.Scores.Add(new ScoreEntry2(score));
				}
				scoreboard = scoreboard2;
			}
			else
			{
				scoreboard = new Scoreboard2();
			}
		}
		catch (Exception x)
		{
			MetricsManager.LogException("Exception loading high scores", x);
			scoreboard = new Scoreboard2();
		}
		if (scoreboard == null)
		{
			scoreboard = new Scoreboard2();
		}
		if (scoreboard.Scores == null)
		{
			scoreboard.Scores = new List<ScoreEntry2>();
		}
		scoreboard.Scores.RemoveAll((ScoreEntry2 s) => s == null);
		return scoreboard;
	}
}
