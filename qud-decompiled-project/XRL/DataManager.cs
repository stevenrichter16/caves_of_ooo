using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Mono.Data.Sqlite;
using UnityEngine;
using XRL.Core;

namespace XRL;

[HasModSensitiveStaticCache]
[HasGameBasedStaticCache]
public static class DataManager
{
	public struct CacheOperation : IDisposable
	{
		private SqliteCommand _Command;

		public bool Exclusive;

		public SqliteCommand Command => _Command ?? (_Command = RequireCacheConnection().CreateCommand());

		public CacheOperation(bool Exclusive)
		{
			_Command = null;
			this.Exclusive = Exclusive;
		}

		public void Dispose()
		{
			if (_Command != null)
			{
				_Command.Dispose();
				_Command = null;
			}
			if (Exclusive)
			{
				ConnectionLock.ExitWriteLock();
			}
			else
			{
				ConnectionLock.ExitReadLock();
			}
		}
	}

	public static Dictionary<string, string> contents = new Dictionary<string, string>();

	private static SqliteConnection Connection;

	private static ReaderWriterLockSlim ConnectionLock = new ReaderWriterLockSlim();

	public const string STEAM_PATH_REGEX = "^(.+)[/\\\\]steamapps[/\\\\]";

	private static Dictionary<string, List<DataFile>> XMLFilesByRoot = new Dictionary<string, List<DataFile>>(StringComparer.OrdinalIgnoreCase);

	private static bool XMLCacheInitialized;

	[GameBasedCacheInit]
	public static void ResetCacheConnection()
	{
		if (Connection != null)
		{
			Connection.Dispose();
			Connection = null;
		}
	}

	public static void CloseCacheConnection()
	{
		if (Connection != null)
		{
			Connection.Close();
		}
	}

	public static bool HasMigration(string Migration)
	{
		using SqliteCommand sqliteCommand = Connection.CreateCommand();
		sqliteCommand.CommandText = "SELECT Migration FROM Migrations WHERE Migration = @Migration";
		sqliteCommand.Parameters.AddWithValue("Migration", Migration);
		return sqliteCommand.ExecuteReader().HasRows;
	}

	public static SqliteCommand CreateMigrationUpgradeTransactionCommand(string Migration, string CommandRaw)
	{
		SqliteCommand sqliteCommand = Connection.CreateCommand();
		sqliteCommand.CommandText = "BEGIN; " + CommandRaw + "; INSERT INTO Migrations (Migration) VALUES (@Migration); COMMIT;";
		sqliteCommand.Parameters.AddWithValue("Migration", Migration);
		return sqliteCommand;
	}

	public static void CheckSqlMigrations(bool initialCreate)
	{
		if (initialCreate)
		{
			using (SqliteCommand sqliteCommand = CreateMigrationUpgradeTransactionCommand("FrozenZone_FrozenTick", "\r\n                    CREATE TABLE IF NOT EXISTS 'FrozenZone' (\r\n                        'ID' INTEGER PRIMARY KEY AUTOINCREMENT,\r\n                        'ZoneID' TEXT UNIQUE NOT NULL,\r\n                        'FrozenTick' INTEGER,\r\n                        'Data' BLOB NOT NULL\r\n                    );\r\n                    CREATE UNIQUE INDEX IF NOT EXISTS ZoneIDIndex ON FrozenZone (ZoneID);\r\n                "))
			{
				sqliteCommand.ExecuteNonQuery();
				MetricsManager.LogInfo("Creating Cache.db with FrozenZone_FrozenTick");
				return;
			}
		}
		if (!HasMigration("FrozenZone_FrozenTick"))
		{
			using (SqliteCommand sqliteCommand2 = CreateMigrationUpgradeTransactionCommand("FrozenZone_FrozenTick", "ALTER TABLE 'FrozenZone' ADD 'FrozenTick' INTEGER"))
			{
				sqliteCommand2.ExecuteNonQuery();
				MetricsManager.LogInfo("Upgrading Cache.db with FrozenZone_FrozenTick");
			}
		}
	}

	public static SqliteConnection RequireCacheConnection()
	{
		if (Connection == null)
		{
			XRLGame game = The.Game;
			if (game == null)
			{
				throw new NullReferenceException("Game is not initialized.");
			}
			bool initialCreate = !File.Exists(game.GetCacheDirectory("Cache.db"));
			Connection = new SqliteConnection("Data Source=" + game.GetCacheDirectory("Cache.db"));
			Connection.Open();
			using (SqliteCommand sqliteCommand = Connection.CreateCommand())
			{
				sqliteCommand.CommandText = "BEGIN;CREATE TABLE IF NOT EXISTS 'Migrations' (   'ID' INTEGER PRIMARY KEY AUTOINCREMENT,   'Migration' TEXT UNIQUE NOT NULL );CREATE UNIQUE INDEX IF NOT EXISTS MigrationIndex ON Migrations (Migration); COMMIT;";
				sqliteCommand.ExecuteNonQuery();
			}
			CheckSqlMigrations(initialCreate);
		}
		else if (Connection.State == ConnectionState.Closed)
		{
			Connection.Open();
		}
		return Connection;
	}

	public static CacheOperation StartCacheOperation(bool Exclusive = false)
	{
		if (Exclusive)
		{
			ConnectionLock.EnterWriteLock();
		}
		else
		{
			ConnectionLock.EnterReadLock();
		}
		return new CacheOperation(Exclusive);
	}

	public static void Shutdown()
	{
		ResetCacheConnection();
	}

	public static void preloadContents(string path)
	{
		try
		{
			string key = Path.GetFileName(path).ToLower();
			if (!contents.ContainsKey(key))
			{
				string text = Path.Combine(Path.Combine(Application.streamingAssetsPath, "Base"), path);
				WWW wWW = new WWW(text);
				while (!wWW.isDone)
				{
					Thread.Sleep(0);
				}
				contents.Add(key, wWW.text);
				MetricsManager.LogInfo("loaded " + text + " length " + wWW.text.Length);
			}
		}
		catch (Exception x)
		{
			MetricsManager.LogError("error loading " + path, x);
		}
	}

	public static Stream GenerateStreamFromString(string s)
	{
		MemoryStream memoryStream = new MemoryStream();
		StreamWriter streamWriter = new StreamWriter(memoryStream);
		streamWriter.Write(s);
		streamWriter.Flush();
		memoryStream.Position = 0L;
		return memoryStream;
	}

	public static XmlDataHelper GetXMLStream(string FileName, ModInfo modInfo)
	{
		string key = Path.GetFileName(FileName).ToLower();
		if (contents.ContainsKey(key))
		{
			return new XmlDataHelper(GenerateStreamFromString(contents[key]), modInfo);
		}
		return new XmlDataHelper(FilePath(FileName), modInfo);
	}

	public static XmlTextReader GetStreamingAssetsXMLStream(string FileName)
	{
		string key = Path.GetFileName(FileName).ToLower();
		if (contents.ContainsKey(key))
		{
			return new XmlTextReader(GenerateStreamFromString(contents[key]));
		}
		return new XmlTextReader(FilePath(FileName));
	}

	public static StreamReader GetStreamingAssetsStreamReader(string FileName)
	{
		string key = Path.GetFileName(FileName).ToLower();
		if (contents.ContainsKey(key))
		{
			return new StreamReader(GenerateStreamFromString(contents[key]));
		}
		return new StreamReader(FilePath(FileName));
	}

	public static string EmbeddedModsPath()
	{
		if (string.IsNullOrWhiteSpace(XRLCore.EmbeddedModsPath))
		{
			XRLCore.EmbeddedModsPath = Path.Combine(Application.streamingAssetsPath, "Mods");
		}
		return XRLCore.EmbeddedModsPath;
	}

	public static string OSXDLCPath()
	{
		if (string.IsNullOrWhiteSpace(XRLCore.OSXDLCPath))
		{
			XRLCore.OSXDLCPath = Path.Combine(Application.dataPath, "../../CoQ_Data/StreamingAssets/DLC");
		}
		return XRLCore.OSXDLCPath;
	}

	public static string OSXDLCPath(string file)
	{
		if (string.IsNullOrWhiteSpace(XRLCore.OSXDLCPath))
		{
			XRLCore.OSXDLCPath = Path.Combine(Application.dataPath, "../../CoQ_Data/StreamingAssets/DLC");
		}
		return Path.Combine(XRLCore.OSXDLCPath, file);
	}

	public static string DLCPath()
	{
		if (string.IsNullOrWhiteSpace(XRLCore.DLCPath))
		{
			XRLCore.DLCPath = Path.Combine(Application.streamingAssetsPath, "DLC");
		}
		return XRLCore.DLCPath;
	}

	public static string DLCPath(string file)
	{
		if (string.IsNullOrWhiteSpace(XRLCore.DLCPath))
		{
			XRLCore.DLCPath = Path.Combine(Application.streamingAssetsPath, "DLC");
		}
		return Path.Combine(XRLCore.DLCPath, file);
	}

	public static string FilePath()
	{
		if (string.IsNullOrWhiteSpace(XRLCore.DataPath))
		{
			XRLCore.DataPath = Path.Combine(Application.streamingAssetsPath, "Base");
		}
		return XRLCore.DataPath;
	}

	public static string FilePath(string FileName)
	{
		if (string.IsNullOrWhiteSpace(XRLCore.DataPath))
		{
			XRLCore.DataPath = Path.Combine(Application.streamingAssetsPath, "Base");
		}
		return Path.Combine(XRLCore.DataPath, FileName);
	}

	public static string SavePath(string FileName)
	{
		return Path.Combine(XRLCore.SavePath, FileName);
	}

	public static string LocalPath(string FileName)
	{
		return Path.Combine(XRLCore.LocalPath, FileName);
	}

	public static string SyncedPath(string FileName)
	{
		return Path.Combine(XRLCore.SyncedPath, FileName);
	}

	public static string SanitizePathForDisplay(string Source)
	{
		string oldValue = SavePath("");
		string text = Source.Replace(oldValue, "<...>/CavesOfQud");
		oldValue = SavePath("").Replace("/", "\\");
		return Regex.Replace(text.Replace(oldValue, "<...>"), "^(.+)[/\\\\]steamapps[/\\\\]", "<...>/steamapps/").Replace('\\', '/');
	}

	[ModSensitiveCacheInit]
	private static void ClearXMLCache()
	{
		if (XMLCacheInitialized && GameManager.AwakeComplete)
		{
			XMLFilesByRoot.Clear();
			XMLCacheInitialized = false;
		}
	}

	private static void CheckXMLInitialize()
	{
		if (XMLCacheInitialized)
		{
			return;
		}
		if (XMLFilesByRoot.IsNullOrEmpty())
		{
			FileInfo[] files = new DirectoryInfo(FilePath("")).GetFiles("*.xml", SearchOption.AllDirectories);
			for (int i = 0; i < files.Length; i++)
			{
				CacheFile(files[i].FullName);
			}
			SortXMLRoots();
		}
		if (!ModManager.Initialized)
		{
			return;
		}
		foreach (ModInfo activeMod in ModManager.ActiveMods)
		{
			foreach (ModFile file in activeMod.Files)
			{
				if (file.Type == ModFileType.XML)
				{
					CacheFile(file.OriginalName, activeMod);
				}
			}
		}
		SortXMLRoots();
		PrintXMLRoots();
		XMLCacheInitialized = true;
	}

	private static void SortXMLRoots()
	{
		foreach (KeyValuePair<string, List<DataFile>> item in XMLFilesByRoot)
		{
			item.Value.Sort();
		}
	}

	private static void PrintXMLRoots()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append($"Found {XMLFilesByRoot.Count} XML roots;");
		foreach (KeyValuePair<string, List<DataFile>> item in XMLFilesByRoot)
		{
			item.Deconstruct(out var key, out var value);
			string value2 = key;
			List<DataFile> list = value;
			int num = 0;
			int num2 = 0;
			foreach (DataFile item2 in list)
			{
				if (item2.IsBase)
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
			stringBuilder.Append("\n - ").AppendUpper(value2).Append(": ");
			if (num != 0)
			{
				stringBuilder.AppendThings(num, "base file", "base files");
			}
			if (num2 != 0)
			{
				if (num != 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.AppendThings(num2, "mod file", "mod files");
			}
		}
		MetricsManager.LogInfo(stringBuilder.ToString());
	}

	private static void CacheFile(string Path, ModInfo Mod = null)
	{
		try
		{
			using XmlReader xmlReader = XmlReader.Create(Path);
			while (xmlReader.Read())
			{
				if (!xmlReader.Name.IsNullOrEmpty() && !(xmlReader.Name == "xml"))
				{
					if (!XMLFilesByRoot.TryGetValue(xmlReader.Name, out var value))
					{
						value = (XMLFilesByRoot[xmlReader.Name] = new List<DataFile>());
					}
					int result;
					int priority = (int.TryParse(xmlReader.GetAttribute("LoadPriority"), out result) ? result : 0);
					value.Add(new DataFile
					{
						Path = Path,
						Mod = Mod,
						Priority = priority
					});
					break;
				}
			}
		}
		catch (Exception ex)
		{
			MetricsManager.LogPotentialModError(Mod, Path + ": " + ex.Message);
		}
	}

	public static void DeleteSaveDirectory(string Path)
	{
		if (Path.IsNullOrEmpty())
		{
			return;
		}
		using (StartCacheOperation(Exclusive: true))
		{
			ResetCacheConnection();
			int num = 0;
			while (true)
			{
				try
				{
					Directory.Delete(Path, recursive: true);
					break;
				}
				catch (Exception x)
				{
					if (num++ < 20)
					{
						Thread.Sleep(50);
						continue;
					}
					MetricsManager.LogException("Error deleting saved game", x);
					break;
				}
			}
		}
	}

	/// <summary>Get list of XML files with root element within both base and mod directories.</summary>
	public static List<DataFile> GetXMLFilesWithRoot(string Root)
	{
		CheckXMLInitialize();
		return XMLFilesByRoot.GetValue(Root) ?? new List<DataFile>();
	}

	/// <summary>Yield XmlDataHelper streams for each file in <see cref="M:XRL.DataManager.GetXMLFilesWithRoot(System.String)" />.</summary>
	/// <remarks>Stream is disposed on next enumeration.</remarks>
	public static IEnumerable<XmlDataHelper> YieldXMLStreamsWithRoot(string Root, bool IncludeBase = true, bool IncludeMods = true)
	{
		foreach (DataFile item in GetXMLFilesWithRoot(Root))
		{
			if ((IncludeBase || !item.IsBase) && (IncludeMods || !item.IsMod))
			{
				using XmlDataHelper stream = GetXMLStream(item, item.Mod);
				yield return stream;
			}
		}
	}

	public static void Open(string Path)
	{
		Path = System.IO.Path.GetFullPath(Path);
		try
		{
			if (Process.Start("open", "\"" + Path + "\"") != null)
			{
				return;
			}
		}
		catch (Exception)
		{
		}
		try
		{
			if (Process.Start("xdg-open", "\"" + Path + "\"") != null)
			{
				return;
			}
		}
		catch (Exception)
		{
		}
		try
		{
			Process.Start("explorer", "\"" + Path + "\"");
		}
		catch (Exception)
		{
		}
	}
}
