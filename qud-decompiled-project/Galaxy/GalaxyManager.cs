using System;
using Galaxy.Api;
using UnityEngine;

namespace Galaxy;

public class GalaxyManager
{
	private class AuthenticationListener : IAuthListener
	{
		public override void OnAuthSuccess()
		{
			Debug.Log("GALAXY - Authentication succeeded, requesting user stats.");
			UserStatsRetrieveListener.Register();
			Manager.Stats.RequestUserStatsAndAchievements();
		}

		public override void OnAuthFailure(FailureReason FailureReason)
		{
			Debug.Log($"GALAXY - Authentication failed ({FailureReason}).");
			Manager.Shutdown();
		}

		public override void OnAuthLost()
		{
			Debug.Log("GALAXY - Connection lost.");
		}
	}

	private class OperationalStateListener : IOperationalStateChangeListener
	{
		private const uint SIGNED_OUT = 0u;

		private const uint SIGNED_IN = 1u;

		private const uint LOGGED_ON = 2u;

		public static OperationalStateListener Listener;

		public static void Register()
		{
			if (Manager.Initialized)
			{
				if (Listener == null)
				{
					Listener = new OperationalStateListener();
				}
				GalaxyInstance.ListenerRegistrar().Register(ListenerType.OPERATIONAL_STATE_CHANGE, Listener);
			}
		}

		public static void Unregister()
		{
			Listener?.Dispose();
			Listener = null;
		}

		public override void OnOperationalStateChanged(uint State)
		{
			if (State == 0)
			{
				Manager.Shutdown();
			}
			else
			{
				Manager.Active &= State.HasBit(1u);
			}
		}
	}

	private class UserStatsRetrieveListener : IUserStatsAndAchievementsRetrieveListener
	{
		public static UserStatsRetrieveListener Listener;

		public static void Register()
		{
			if (Manager.Initialized)
			{
				if (Listener == null)
				{
					Listener = new UserStatsRetrieveListener();
				}
				GalaxyInstance.ListenerRegistrar().Register(ListenerType.USER_STATS_AND_ACHIEVEMENTS_RETRIEVE, Listener);
			}
		}

		public static void Unregister()
		{
			Listener?.Dispose();
			Listener = null;
		}

		public override void OnUserStatsAndAchievementsRetrieveSuccess(GalaxyID UserID)
		{
			Debug.Log("GALAXY - User stats retrieved.");
			Manager.Active = true;
			Manager.Synchronize();
		}

		public override void OnUserStatsAndAchievementsRetrieveFailure(GalaxyID UserID, FailureReason FailureReason)
		{
			Debug.Log($"GALAXY - Error to retrieve user stats ({FailureReason}).");
			Manager.Active = false;
		}
	}

	private class AchievementChangeListener : IAchievementChangeListener
	{
		public static AchievementChangeListener Listener;

		public static void Register()
		{
			if (Manager.Initialized)
			{
				if (Listener == null)
				{
					Listener = new AchievementChangeListener();
				}
				GalaxyInstance.ListenerRegistrar().Register(ListenerType.ACHIEVEMENT_CHANGE, Listener);
			}
		}

		public static void Unregister()
		{
			Listener?.Dispose();
			Listener = null;
		}

		public override void OnAchievementUnlocked(string Name)
		{
			Debug.Log("GALAXY - Unlocked achievement: " + Name + ".");
		}
	}

	private const string CLIENT_ID = "50877364037825147";

	private const string CLIENT_SECRET = "1d281dffae098f06a520daeff1fb1f9e9ebdb346b5c397b4f98e8ddb8d978f4e";

	private static GalaxyManager Manager;

	public bool Active;

	public bool Initialized;

	public bool Synchronized;

	private bool Store;

	private IStats Stats;

	internal GalaxyManager()
	{
		if (Manager != null)
		{
			throw new InvalidOperationException();
		}
		Manager = this;
	}

	public void Awake()
	{
		Active = false;
		Initialized = false;
		Synchronized = false;
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		int i = 0;
		for (int num = commandLineArgs.Length; i < num; i++)
		{
			if (commandLineArgs[i].EqualsNoCase("GALAXY:NO"))
			{
				Debug.Log("GALAXY - Disabled via command line argument.");
				return;
			}
		}
		try
		{
			Debug.Log("GALAXY - Initializing.");
			Initialized = true;
			GalaxyInstance.Init(new InitParams("50877364037825147", "1d281dffae098f06a520daeff1fb1f9e9ebdb346b5c397b4f98e8ddb8d978f4e"));
			Stats = GalaxyInstance.Stats();
			Debug.Log("GALAXY - Authenticating user.");
			OperationalStateListener.Register();
			AchievementChangeListener.Register();
			GalaxyInstance.User().SignInGalaxy(requireOnline: false, new AuthenticationListener());
		}
		catch (Exception ex)
		{
			Debug.LogError("GALAXY - Error initializing: " + ex);
			Shutdown();
		}
	}

	public void Shutdown()
	{
		if (!Initialized)
		{
			return;
		}
		try
		{
			Debug.Log("GALAXY - Shutting down.");
			Initialized = false;
			Active = false;
			Stats = null;
			OperationalStateListener.Unregister();
			UserStatsRetrieveListener.Unregister();
			AchievementChangeListener.Unregister();
			GalaxyInstance.Shutdown(unloadModule: true);
		}
		catch (Exception x)
		{
			MetricsManager.LogException("GalaxyManager::Shutdown", x);
		}
	}

	public void Update()
	{
		GalaxyInstance.ProcessData();
		if (Store)
		{
			Stats.StoreStatsAndAchievements();
			Store = false;
		}
	}

	public bool UpdateAchievement(string ID, ref bool Achieved, ref DateTime TimeStamp)
	{
		bool unlocked = false;
		uint unlockTime = 0u;
		try
		{
			Stats.GetAchievement(ID, ref unlocked, ref unlockTime);
		}
		catch (Exception arg)
		{
			Debug.Log($"GALAXY - Error getting achievement '{ID}':\n{arg}");
			return false;
		}
		if (unlockTime != 0 && TimeStamp == DateTime.MinValue)
		{
			TimeStamp = DateTimeOffset.FromUnixTimeSeconds(unlockTime).DateTime;
		}
		if (Achieved && !unlocked)
		{
			SetAchievement(ID);
			return false;
		}
		if (!Achieved && unlocked)
		{
			Achieved = true;
			return true;
		}
		return false;
	}

	public bool GetAchievement(string ID)
	{
		bool unlocked = false;
		uint unlockTime = 0u;
		try
		{
			Stats.GetAchievement(ID, ref unlocked, ref unlockTime);
			return unlocked;
		}
		catch (Exception arg)
		{
			Debug.Log($"GALAXY - Error getting achievement '{ID}':\n{arg}");
			return false;
		}
	}

	public void SetAchievement(string ID)
	{
		try
		{
			Stats.SetAchievement(ID);
			Store = true;
		}
		catch (Exception arg)
		{
			Debug.Log($"GALAXY - Error setting achievement '{ID}':\n{arg}");
		}
	}

	public int GetStat(string ID)
	{
		try
		{
			return Stats.GetStatInt(ID);
		}
		catch (Exception arg)
		{
			Debug.Log($"GALAXY - Error getting stat '{ID}':\n{arg}");
			return 0;
		}
	}

	public void SetStat(string ID, int Value)
	{
		try
		{
			Stats.SetStatInt(ID, Value);
			Store = true;
		}
		catch (Exception arg)
		{
			Debug.Log($"GALAXY - Error setting stat '{ID}':\n{arg}");
		}
	}

	public bool UpdateStat(string ID, ref int Value)
	{
		if (!Active)
		{
			return false;
		}
		int stat = GetStat(ID);
		if (Value > stat)
		{
			SetStat(ID, Value);
			return false;
		}
		if (Value < stat)
		{
			Value = stat;
			return true;
		}
		return false;
	}

	public void Synchronize()
	{
		if (!AchievementManager.Active || Synchronized)
		{
			return;
		}
		Synchronized = true;
		AchievementState state = AchievementManager.State;
		bool flag = false;
		foreach (AchievementInfo value in state.Achievements.Values)
		{
			if (!value.ID.IsNullOrEmpty())
			{
				flag |= UpdateAchievement(value.ID, ref value.Achieved, ref value.TimeStamp);
			}
		}
		foreach (StatInfo value2 in state.Stats.Values)
		{
			if (!value2.ID.IsNullOrEmpty())
			{
				flag |= UpdateStat(value2.ID, ref value2.Value);
			}
		}
		AchievementManager.Write |= flag;
	}

	public void ResetAchievements()
	{
		Stats.ResetStatsAndAchievements();
	}
}
