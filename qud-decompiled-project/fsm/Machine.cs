using System;
using UnityEngine;

namespace fsm;

public class Machine : State
{
	public enum MachineState
	{
		Running,
		Paused,
		Stopping,
		Stopped
	}

	public bool showDebugInfo = true;

	public bool logDebugInfo;

	public Action onStart;

	public Action onStop;

	protected MachineState machineState = MachineState.Stopped;

	protected State startState = new State("fsm_start");

	protected bool isUpdating;

	public Machine()
		: base("fsm_state_machine")
	{
	}

	public void Restart()
	{
		Stop();
		Start();
	}

	public void Start()
	{
		if (machineState == MachineState.Running || machineState == MachineState.Paused)
		{
			return;
		}
		machineState = MachineState.Running;
		if (onStart != null)
		{
			onStart();
		}
		if (mode == Mode.Exclusive)
		{
			if (base.initState != null)
			{
				EnterStates(base.initState, startState);
			}
			else
			{
				Debug.LogError("FSM error: can't find initial state in " + name);
			}
		}
		else
		{
			for (int i = 0; i < children.Count; i++)
			{
				EnterStates(children[i], startState);
			}
		}
	}

	public void Stop()
	{
		if (machineState != MachineState.Stopped)
		{
			if (isUpdating)
			{
				machineState = MachineState.Stopping;
			}
			else
			{
				ProcessStop();
			}
		}
	}

	public void Pause()
	{
		machineState = MachineState.Paused;
	}

	public void Resume()
	{
		machineState = MachineState.Running;
	}

	protected void ProcessStop()
	{
		ClearCurrentStatesRecursively();
		if (onStop != null)
		{
			onStop();
		}
		machineState = MachineState.Stopped;
	}

	public void Tick()
	{
		if (machineState == MachineState.Paused || machineState == MachineState.Stopped)
		{
			return;
		}
		isUpdating = true;
		if (machineState != MachineState.Stopping)
		{
			CheckConditions();
			OnAction();
			UpdateTransitions();
			if (false)
			{
				Stop();
			}
		}
		isUpdating = false;
		if (machineState == MachineState.Stopping)
		{
			ProcessStop();
		}
	}
}
