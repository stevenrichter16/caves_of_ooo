using Galaxy;
using Steamworks;

public static class PlatformManager
{
	public static readonly SteamManager Steam = new SteamManager();

	public static readonly GalaxyManager Galaxy = new GalaxyManager();

	public static bool SteamInitialized => Steam.Initialized;

	public static bool Initialized => Steam.Initialized;

	public static void Awake()
	{
		Steam.Awake();
		Galaxy.Awake();
	}

	public static void UpdateAchievement(AchievementInfo Achievement)
	{
		if (Steam.Active)
		{
			Steam.UpdateAchievement(Achievement.ID, ref Achievement.Achieved, ref Achievement.TimeStamp);
		}
		if (Galaxy.Active)
		{
			Galaxy.UpdateAchievement(Achievement.ID, ref Achievement.Achieved, ref Achievement.TimeStamp);
		}
	}

	public static void UpdateStat(StatInfo Stat)
	{
		if (Steam.Active)
		{
			Steam.UpdateStat(Stat.ID, ref Stat.Value);
		}
		if (Galaxy.Active)
		{
			Galaxy.UpdateStat(Stat.ID, ref Stat.Value);
		}
	}

	public static bool GetAchievement(AchievementInfo Achievement)
	{
		if (Steam.Active)
		{
			return Steam.GetAchievement(Achievement.ID);
		}
		if (Galaxy.Active)
		{
			return Galaxy.GetAchievement(Achievement.ID);
		}
		return false;
	}

	public static bool TrySetAchievement(AchievementInfo Achievement)
	{
		if (Steam.Active && Steam.SetAchievement(Achievement.ID))
		{
			return true;
		}
		if (Galaxy.Active)
		{
			Galaxy.SetAchievement(Achievement.ID);
			return true;
		}
		return false;
	}

	public static int GetStat(StatInfo Stat)
	{
		if (Steam.Active)
		{
			return Steam.GetStat(Stat.ID);
		}
		if (Galaxy.Active)
		{
			return Galaxy.GetStat(Stat.ID);
		}
		return 0;
	}

	public static void SetStat(StatInfo Stat)
	{
		if (Steam.Active)
		{
			Steam.SetStat(Stat.ID, Stat.Value);
		}
		if (Galaxy.Active)
		{
			Galaxy.SetStat(Stat.ID, Stat.Value);
		}
	}

	public static void Synchronize()
	{
		if (Steam.Active)
		{
			Steam.Synchronize();
		}
		if (Galaxy.Active)
		{
			Galaxy.Synchronize();
		}
	}

	public static void Update()
	{
		if (Steam.Initialized)
		{
			Steam.Update();
		}
		if (Galaxy.Initialized)
		{
			Galaxy.Update();
		}
	}

	public static void Shutdown()
	{
		MetricsManager.LogInfo("Platform - Shutdown Starting");
		Steam.Shutdown();
		Galaxy.Shutdown();
		MetricsManager.LogInfo("Platform - Shutdown Complete");
	}

	public static void ResetAchievements()
	{
		if (Steam.Active)
		{
			Steam.ResetAchievements();
		}
		if (Galaxy.Active)
		{
			Galaxy.ResetAchievements();
		}
	}

	public static bool TryShowAchievementProgress(AchievementInfo Achievement)
	{
		if (Achievement.Progress == null)
		{
			return false;
		}
		int value = Achievement.Progress.Value;
		int achievedAt = Achievement.AchievedAt;
		if (Steam.Active && Steam.TryShowAchievementProgress(Achievement.ID, value, achievedAt))
		{
			return true;
		}
		return false;
	}

	public static bool IsOverlayEnabled()
	{
		return false;
	}

	public static PlatformControlType GetControlType()
	{
		if (Steam.Initialized)
		{
			return Steam.GetControlType();
		}
		return PlatformControlType.Undefined;
	}

	public static string GetOptionDefault(string OptionID)
	{
		string result = null;
		if (Steam.Initialized)
		{
			result = Steam.GetOptionDefault(OptionID);
		}
		if (OptionID == "OptionDisplayFramerate")
		{
			result = "120";
		}
		return result;
	}

	public static bool TryShowVirtualKeyboard(VirtualKeyboardType Type = VirtualKeyboardType.SingleLine, int X = 0, int Y = 0, int Width = 80, int Height = 1)
	{
		if (Steam.Initialized && Steam.TryShowVirtualKeyboard(Type, X, Y, Width, Height))
		{
			return true;
		}
		return false;
	}

	public static ControlManager.ControllerFontType GetFontType()
	{
		if (Steam.Initialized)
		{
			return Steam.GetFontType();
		}
		return ControlManager.ControllerFontType.Default;
	}
}
