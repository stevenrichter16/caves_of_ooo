using System.Collections.Generic;

namespace XRL;

public class GlobalStateBag<T>
{
	public Dictionary<string, T> elements = new Dictionary<string, T>();

	public T get(string key, T defaultValue = default(T))
	{
		if (elements.TryGetValue(key, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public T set(string key, T value)
	{
		if (!elements.ContainsKey(key))
		{
			elements.Add(key, value);
		}
		else
		{
			elements[key] = value;
		}
		GlobalState.instance.save();
		return value;
	}
}
