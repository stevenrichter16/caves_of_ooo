using System;
using UnityEngine;

namespace XRL.UI.Framework;

public class FrameworkContext : MonoBehaviour
{
	public virtual NavigationContext context { get; set; }

	public T RequireContext<T>() where T : NavigationContext
	{
		if (context != null)
		{
			T obj = context as T;
			if (obj == null)
			{
				MetricsManager.LogEditorError("Why is this the wrong type???");
			}
			return obj;
		}
		context = Activator.CreateInstance<T>();
		return context as T;
	}
}
