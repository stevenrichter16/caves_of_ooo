using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;
using XRL.Core;

public class LeaderboardManager : MonoBehaviour
{
	private class LeaderboardRecordRequest
	{
		public string type;

		public string id;

		public int requestTop;

		public int numRecords;

		public int score;

		public bool friendsonly;

		public Action<LeaderboardScoresDownloaded_t> scoreCallback;
	}

	private const ELeaderboardUploadScoreMethod s_leaderboardMethod = ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest;

	private static CallResult<LeaderboardFindResult_t> m_findResult = new CallResult<LeaderboardFindResult_t>();

	private static CallResult<LeaderboardScoreUploaded_t> m_uploadResult = new CallResult<LeaderboardScoreUploaded_t>();

	private static CallResult<LeaderboardScoresDownloaded_t> m_downloadResult = new CallResult<LeaderboardScoresDownloaded_t>();

	private static Callback<PersonaStateChange_t> m_personaStateChange;

	private static SteamLeaderboard_t currentLeaderboard;

	private static Queue<LeaderboardRecordRequest> requests = new Queue<LeaderboardRecordRequest>();

	private static LeaderboardRecordRequest currentRequest = null;

	public static Dictionary<string, SteamLeaderboard_t> leaderboardID = new Dictionary<string, SteamLeaderboard_t>();

	private static Action<LeaderboardScoresDownloaded_t> leaderboardEntriesCallback = null;

	public static Dictionary<string, string> leaderboardresults = new Dictionary<string, string>();

	private static Dictionary<ulong, TaskCompletionSource<string>> nameCache = new Dictionary<ulong, TaskCompletionSource<string>>();

	public static bool isConnected => PlatformManager.SteamInitialized;

	public void Update()
	{
		if (requests.Count > 0 && currentRequest == null)
		{
			lock (requests)
			{
				currentRequest = requests.Dequeue();
				leaderboardEntriesCallback = currentRequest.scoreCallback;
				FindOrCreateLeaderboard(currentRequest.id);
			}
		}
	}

	public void OnEnable()
	{
	}

	private static void OnLeaderboardFindResult(LeaderboardFindResult_t pCallback, bool failure)
	{
		try
		{
			Debug.Log("STEAM LEADERBOARDS: Found - " + pCallback.m_bLeaderboardFound + " leaderboardID - " + pCallback.m_hSteamLeaderboard.m_SteamLeaderboard);
			currentLeaderboard = pCallback.m_hSteamLeaderboard;
			if (pCallback.m_bLeaderboardFound != 0)
			{
				if (leaderboardID.ContainsKey(currentRequest.id))
				{
					leaderboardID[currentRequest.id] = pCallback.m_hSteamLeaderboard;
				}
				else
				{
					leaderboardID.Add(currentRequest.id, pCallback.m_hSteamLeaderboard);
				}
				if (currentRequest.type == "getleaderboard")
				{
					ELeaderboardDataRequest eLeaderboardDataRequest = (currentRequest.friendsonly ? ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends : ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal);
					SteamAPICall_t hAPICall = SteamUserStats.DownloadLeaderboardEntries(currentLeaderboard, eLeaderboardDataRequest, currentRequest.requestTop, currentRequest.requestTop + currentRequest.numRecords);
					m_downloadResult = new CallResult<LeaderboardScoresDownloaded_t>();
					m_downloadResult.Set(hAPICall, OnDownloadLeaderboardEntries);
				}
				else if (currentRequest.type == "submitleaderboardscore")
				{
					SteamAPICall_t hAPICall2 = SteamUserStats.UploadLeaderboardScore(currentLeaderboard, ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, currentRequest.score, null, 0);
					m_uploadResult.Set(hAPICall2, OnLeaderboardUploadResult);
				}
				else if (currentRequest.type == "getleaderboardrank")
				{
					SteamAPICall_t hAPICall3 = SteamUserStats.DownloadLeaderboardEntries(currentLeaderboard, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, -4, 5);
					m_downloadResult = new CallResult<LeaderboardScoresDownloaded_t>();
					m_downloadResult.Set(hAPICall3, OnDownloadLeaderboardEntries);
				}
			}
			else
			{
				Debug.LogError("Couldn't find leaderboard");
				currentRequest = null;
			}
		}
		catch (Exception ex)
		{
			currentRequest = null;
			XRLCore.LogError("OnLeaderboardFindResult", ex);
		}
	}

	private static void OnDownloadLeaderboardEntries(LeaderboardScoresDownloaded_t pCallback, bool failure)
	{
		if (!failure && leaderboardEntriesCallback != null)
		{
			try
			{
				leaderboardEntriesCallback(pCallback);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
		leaderboardEntriesCallback = null;
		currentRequest = null;
	}

	private static void OnLeaderboardUploadResult(LeaderboardScoreUploaded_t pCallback, bool failure)
	{
		try
		{
			Debug.Log("STEAM LEADERBOARDS: failure - " + failure + " Completed - " + pCallback.m_bSuccess + " NewScore: " + pCallback.m_nGlobalRankNew + " Score " + pCallback.m_nScore + " HasChanged - " + pCallback.m_bScoreChanged);
			SteamAPICall_t hAPICall = SteamUserStats.DownloadLeaderboardEntries(currentLeaderboard, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser, -4, 5);
			m_downloadResult = new CallResult<LeaderboardScoresDownloaded_t>();
			m_downloadResult.Set(hAPICall, OnDownloadLeaderboardEntries);
		}
		catch (Exception ex)
		{
			currentRequest = null;
			XRLCore.LogError("OnLeaderboardUploadResult", ex);
		}
	}

	public static void FindOrCreateLeaderboard(string id)
	{
		try
		{
			SteamAPICall_t hAPICall = SteamUserStats.FindOrCreateLeaderboard(id, ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
			m_findResult = new CallResult<LeaderboardFindResult_t>();
			m_findResult.Set(hAPICall, OnLeaderboardFindResult);
		}
		catch (Exception ex)
		{
			currentRequest = null;
			XRLCore.LogError("FindOrCreateLeaderboard", ex);
		}
	}

	public static string SubmitResult(string id, int score, Action<LeaderboardScoresDownloaded_t> callback)
	{
		if (!PlatformManager.SteamInitialized)
		{
			Debug.Log("Can't upload to the leaderboard because Steam isn't connected.");
			return null;
		}
		lock (requests)
		{
			string[] array = id.Split(':');
			string text = "unknown " + id;
			if (array[0] == "weekly")
			{
				text = array[0] + " for week " + array[2] + " of " + array[1];
			}
			if (array[0] == "daily")
			{
				text = array[0] + " for day " + array[2] + " of " + array[1];
			}
			Debug.Log("uploading score(" + score + ") to steam leaderboard(" + text + ")");
			LeaderboardRecordRequest leaderboardRecordRequest = new LeaderboardRecordRequest();
			leaderboardRecordRequest.type = "submitleaderboardscore";
			leaderboardRecordRequest.id = text;
			leaderboardRecordRequest.score = score;
			leaderboardRecordRequest.scoreCallback = callback;
			requests.Enqueue(leaderboardRecordRequest);
			return text;
		}
	}

	public static string GetLeaderboardSteamId(string id)
	{
		string[] array = id.Split(':');
		string result = "unknown " + id;
		if (array[0] == "weekly")
		{
			result = array[0] + " for week " + array[2] + " of " + array[1];
		}
		if (array[0] == "daily")
		{
			result = array[0] + " for day " + array[2] + " of " + array[1];
		}
		return result;
	}

	public static string GetDailyID(int daysAgo)
	{
		CultureInfo cultureInfo = new CultureInfo("en-US");
		DateTime dateTime = DateTime.Now - new TimeSpan(daysAgo, 0, 0, 0, 0);
		if (dateTime > DateTime.Now)
		{
			dateTime = DateTime.Now;
		}
		if (dateTime < new DateTime(2016, 11, 1))
		{
			dateTime = new DateTime(2016, 11, 1);
		}
		int dayOfYear = cultureInfo.Calendar.GetDayOfYear(dateTime);
		return "daily:" + dateTime.Year + ":" + dayOfYear;
	}

	public static string GetLeaderboardName(string id)
	{
		return GetLeaderboardSteamId(id);
	}

	public static Task<LeaderboardScoresDownloaded_t> GetLeaderboardAsync(string id, int requestTop, int numRecords, bool friendsOnly)
	{
		TaskCompletionSource<LeaderboardScoresDownloaded_t> result = new TaskCompletionSource<LeaderboardScoresDownloaded_t>();
		if (!PlatformManager.SteamInitialized)
		{
			result.TrySetException(new Exception("Not connected to Steam API"));
		}
		else
		{
			GetLeaderboard(id, requestTop, numRecords, friendsOnly, delegate(LeaderboardScoresDownloaded_t r)
			{
				result.TrySetResult(r);
			});
		}
		return result.Task;
	}

	public static async Task<List<(int rank, ulong steamID, int score)>> GetLeaderboardListAsync(string id, int requestTop, int numRecords, bool friendsOnly, CancellationToken? ct = null)
	{
		ct?.ThrowIfCancellationRequested();
		LeaderboardScoresDownloaded_t leaderboardScoresDownloaded_t = await GetLeaderboardAsync(id, requestTop, numRecords, friendsOnly);
		ct?.ThrowIfCancellationRequested();
		List<(int, ulong, int)> list = new List<(int, ulong, int)>(leaderboardScoresDownloaded_t.m_cEntryCount);
		LeaderboardEntry_t pLeaderboardEntry = default(LeaderboardEntry_t);
		for (int i = 0; i < leaderboardScoresDownloaded_t.m_cEntryCount; i++)
		{
			SteamUserStats.GetDownloadedLeaderboardEntry(leaderboardScoresDownloaded_t.m_hSteamLeaderboardEntries, i, out pLeaderboardEntry, null, 0);
			list.Add((pLeaderboardEntry.m_nGlobalRank, (ulong)pLeaderboardEntry.m_steamIDUser, pLeaderboardEntry.m_nScore));
		}
		return list;
	}

	public static void OnPersonaStateChange(PersonaStateChange_t e)
	{
		TaskCompletionSource<string> value = null;
		if (nameCache.TryGetValue(e.m_ulSteamID, out value))
		{
			value.TrySetResult(SteamFriends.GetFriendPersonaName((CSteamID)e.m_ulSteamID));
		}
	}

	public static async Task<string> FriendNameAsync(ulong id)
	{
		TaskCompletionSource<string> value = null;
		if (!nameCache.TryGetValue(id, out value))
		{
			CSteamID cSteamID = (CSteamID)id;
			value = new TaskCompletionSource<string>();
			nameCache.Add(id, value);
			if (!SteamFriends.RequestUserInformation(cSteamID, bRequireNameOnly: true))
			{
				value.TrySetResult(SteamFriends.GetFriendPersonaName(cSteamID));
			}
		}
		return await value.Task;
	}

	public static string GetLeaderboard(string id, int requestTop, int numRecords, bool bFriendsOnly, Action<LeaderboardScoresDownloaded_t> callback)
	{
		if (!PlatformManager.SteamInitialized)
		{
			Debug.Log("Can't upload to the leaderboard because Steam isn't connected.");
			return null;
		}
		lock (requests)
		{
			Debug.Log("getting steam leaderboard(" + leaderboardID?.ToString() + ")");
			LeaderboardRecordRequest leaderboardRecordRequest = new LeaderboardRecordRequest();
			leaderboardRecordRequest.type = "getleaderboard";
			leaderboardRecordRequest.id = GetLeaderboardSteamId(id);
			leaderboardRecordRequest.scoreCallback = callback;
			leaderboardRecordRequest.requestTop = requestTop;
			leaderboardRecordRequest.numRecords = numRecords;
			leaderboardRecordRequest.friendsonly = bFriendsOnly;
			requests.Enqueue(leaderboardRecordRequest);
			return leaderboardRecordRequest.id;
		}
	}

	public static string GetLeaderboardRank(string leaderboardID, Action<LeaderboardScoresDownloaded_t> callback)
	{
		if (!PlatformManager.SteamInitialized)
		{
			Debug.Log("Can't upload to the leaderboard because Steam isn't connected.");
			return null;
		}
		lock (requests)
		{
			LeaderboardRecordRequest leaderboardRecordRequest = new LeaderboardRecordRequest();
			leaderboardRecordRequest.type = "getleaderboardrank";
			leaderboardRecordRequest.id = leaderboardID;
			leaderboardRecordRequest.scoreCallback = callback;
			requests.Enqueue(leaderboardRecordRequest);
			return leaderboardID;
		}
	}
}
