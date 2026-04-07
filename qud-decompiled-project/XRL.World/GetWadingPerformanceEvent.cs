namespace XRL.World;

[GameEvent(Cascade = 1, Cache = Cache.Pool)]
public class GetWadingPerformanceEvent : PooledEvent<GetWadingPerformanceEvent>
{
	public new static readonly int CascadeLevel = 1;

	public GameObject Actor;

	public int MoveSpeedPenalty;

	public override int GetCascadeLevel()
	{
		return CascadeLevel;
	}

	public override bool Dispatch(IEventHandler Handler)
	{
		return Handler.HandleEvent(this);
	}

	public override void Reset()
	{
		base.Reset();
		Actor = null;
		MoveSpeedPenalty = 0;
	}

	public static GetWadingPerformanceEvent FromPool(GameObject Actor, int MoveSpeedPenalty)
	{
		GetWadingPerformanceEvent getWadingPerformanceEvent = PooledEvent<GetWadingPerformanceEvent>.FromPool();
		getWadingPerformanceEvent.Actor = Actor;
		getWadingPerformanceEvent.MoveSpeedPenalty = MoveSpeedPenalty;
		return getWadingPerformanceEvent;
	}

	public static bool GetFor(GameObject Actor, ref int MoveSpeedPenalty)
	{
		if (Actor != null)
		{
			if (Actor.HasRegisteredEvent("GetWadingPerformance"))
			{
				Event obj = Event.New("GetWadingPerformance", "Actor", Actor, "MoveSpeedPenalty", MoveSpeedPenalty);
				bool num = Actor.FireEvent(obj);
				MoveSpeedPenalty = obj.GetIntParameter("MoveSpeedPenalty");
				if (!num)
				{
					return false;
				}
			}
			if (Actor.WantEvent(PooledEvent<GetWadingPerformanceEvent>.ID, CascadeLevel))
			{
				GetWadingPerformanceEvent getWadingPerformanceEvent = FromPool(Actor, MoveSpeedPenalty);
				bool num2 = Actor.HandleEvent(getWadingPerformanceEvent);
				MoveSpeedPenalty = getWadingPerformanceEvent.MoveSpeedPenalty;
				if (!num2)
				{
					return false;
				}
			}
		}
		return true;
	}
}
