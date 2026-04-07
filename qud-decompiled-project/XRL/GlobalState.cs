using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace XRL;

[Serializable]
public class GlobalState
{
	public GlobalStateBag<string> stringState = new GlobalStateBag<string>();

	public GlobalStateBag<int> intState = new GlobalStateBag<int>();

	public GlobalStateBag<bool> boolState = new GlobalStateBag<bool>();

	public GlobalStateBag<float> floatState = new GlobalStateBag<float>();

	public GlobalStateBag<object> objectState = new GlobalStateBag<object>();

	private static GlobalState _instance;

	public static GlobalState instance
	{
		get
		{
			if (_instance == null)
			{
				string path = DataManager.SyncedPath("GlobalState.json");
				if (!File.Exists(path))
				{
					_instance = new GlobalState();
				}
				else
				{
					try
					{
						_instance = JsonConvert.DeserializeObject<GlobalState>(File.ReadAllText(path));
						if (_instance == null)
						{
							_instance = new GlobalState();
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						_instance = new GlobalState();
					}
				}
			}
			return _instance;
		}
	}

	public void save()
	{
		File.WriteAllText(DataManager.SyncedPath("GlobalState.json"), JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.Auto
		}));
	}
}
