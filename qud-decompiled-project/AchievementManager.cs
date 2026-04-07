using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using XRL;

public static class AchievementManager
{
	public static bool Write = false;

	public static bool Enabled = true;

	public static bool Active;

	public static AchievementState State = new AchievementState(100);

	public static void Awake()
	{
		if (Achievement.DIE != null)
		{
			State.AssignParents();
			Load();
			Active = true;
			PlatformManager.Synchronize();
		}
	}

	public static void Update()
	{
		if (Write)
		{
			Write = false;
			Task.Run((Action)Save);
		}
	}

	public static void Load()
	{
		string text = DataManager.SyncedPath("Achievements.json");
		if (!File.Exists(text))
		{
			return;
		}
		try
		{
			AchievementState achievementState = new JsonSerializer
			{
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
			}.Deserialize<AchievementState>(text);
			foreach (KeyValuePair<string, AchievementInfo> achievement in achievementState.Achievements)
			{
				if (State.Achievements.TryGetValue(achievement.Key, out var value))
				{
					value.Achieved = achievement.Value.Achieved;
					value.TimeStamp = achievement.Value.TimeStamp;
				}
			}
			foreach (KeyValuePair<string, StatInfo> stat in achievementState.Stats)
			{
				if (State.Stats.TryGetValue(stat.Key, out var value2))
				{
					value2.Value = stat.Value.Value;
				}
			}
		}
		catch (Exception x)
		{
			MetricsManager.LogError("Error reading local achievement data", x);
		}
	}

	public static void Save()
	{
		string file = DataManager.SyncedPath("Achievements.json");
		lock (State)
		{
			try
			{
				new JsonSerializer
				{
					Formatting = Formatting.Indented,
					DefaultValueHandling = DefaultValueHandling.Ignore
				}.Serialize(file, State);
			}
			catch (Exception x)
			{
				MetricsManager.LogError("Error writing local achievement data", x);
			}
		}
	}

	public static void Reset()
	{
		foreach (AchievementInfo value in State.Achievements.Values)
		{
			value.Achieved = false;
		}
		foreach (StatInfo value2 in State.Stats.Values)
		{
			value2.Value = 0;
		}
		PlatformManager.ResetAchievements();
	}

	public static bool GetAchievement(string ID)
	{
		if (State.Achievements.TryGetValue(ID, out var value))
		{
			return value.Achieved;
		}
		return false;
	}

	public static void SetAchievement(string ID)
	{
		if (Enabled)
		{
			if (!State.Achievements.TryGetValue(ID, out var value))
			{
				Debug.LogWarning("Unknown achievement ID: " + ID);
			}
			else
			{
				value.Unlock();
			}
		}
	}

	public static void IncrementAchievement(string ID, int Value = 1)
	{
		if (Enabled)
		{
			if (!State.Achievements.TryGetValue(ID, out var value))
			{
				Debug.LogWarning("Unknown stat ID: " + ID);
			}
			else if (value.Progress == null)
			{
				value.Unlock();
			}
			else
			{
				value.Progress.Increment(Value);
			}
		}
	}

	public static int GetStat(string ID)
	{
		if (!State.Stats.TryGetValue(ID, out var value))
		{
			Debug.LogWarning("Unknown stat ID: " + ID);
			return 0;
		}
		return value.Value;
	}

	public static void SetStat(string ID, int Value)
	{
		if (Enabled)
		{
			if (!State.Stats.TryGetValue(ID, out var value))
			{
				Debug.LogWarning("Unknown stat ID: " + ID);
			}
			else
			{
				value.SetValue(Value);
			}
		}
	}

	public static void IncrementStat(string ID)
	{
		if (Enabled)
		{
			if (!State.Stats.TryGetValue(ID, out var value))
			{
				Debug.LogWarning("Unknown stat ID: " + ID);
			}
			else
			{
				value.Increment();
			}
		}
	}
}
