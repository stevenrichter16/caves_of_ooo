namespace XRL.World;

[GameEvent(Cascade = 271, Cache = Cache.Singleton)]
public class EndTurnEvent : SingletonEvent<EndTurnEvent>
{
	public new static readonly int CascadeLevel = 271;

	public static ImmutableEvent registeredInstance = new ImmutableEvent("EndTurn");

	public override int GetCascadeLevel()
	{
		return CascadeLevel;
	}

	public override bool Dispatch(IEventHandler Handler)
	{
		return Handler.HandleEvent(this);
	}

	public static void Send(GameObject Object)
	{
		Object.HandleEvent(SingletonEvent<EndTurnEvent>.Instance);
	}

	public static void Send(XRLGame Game)
	{
		Game.HandleEvent(SingletonEvent<EndTurnEvent>.Instance);
	}

	public static void Send(Zone Zone)
	{
		Zone.HandleEvent(SingletonEvent<EndTurnEvent>.Instance);
	}
}
