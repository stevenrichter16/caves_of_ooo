using System;
using UnityEngine;

namespace fsm;

public class TimerTransition : Transition
{
	public float duration = 1f;

	public Action<float> onTick;

	private float timer;

	public TimerTransition()
	{
		onStart = (Action)Delegate.Combine(onStart, (Action)delegate
		{
			timer = 0f;
		});
		onTransition = delegate
		{
			timer += Time.deltaTime;
			if (onTick != null)
			{
				onTick(timer / duration);
			}
			return (timer >= duration) ? true : false;
		};
	}
}
