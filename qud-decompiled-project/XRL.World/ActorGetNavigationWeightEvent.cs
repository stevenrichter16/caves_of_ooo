using System.Collections.Generic;

namespace XRL.World;

[GameEvent(Cache = Cache.Pool)]
public class ActorGetNavigationWeightEvent : INavigationWeightEvent
{
	public new static readonly int ID = MinEvent.RegisterEvent(typeof(ActorGetNavigationWeightEvent), null, CountPool, ResetPool);

	private static List<ActorGetNavigationWeightEvent> Pool;

	private static int PoolCounter;

	public ActorGetNavigationWeightEvent()
	{
		base.ID = ID;
	}

	public static int CountPool()
	{
		if (Pool != null)
		{
			return Pool.Count;
		}
		return 0;
	}

	public static void ResetPool()
	{
		while (PoolCounter > 0)
		{
			Pool[--PoolCounter].Reset();
		}
	}

	public static void ResetTo(ref ActorGetNavigationWeightEvent E)
	{
		MinEvent.ResetTo(E, Pool, ref PoolCounter);
		E = null;
	}

	public static ActorGetNavigationWeightEvent FromPool()
	{
		return MinEvent.FromPool(ref Pool, ref PoolCounter);
	}

	public override bool Dispatch(IEventHandler Handler)
	{
		if (!base.Dispatch(Handler))
		{
			return false;
		}
		return Handler.HandleEvent(this);
	}

	public static int GetFor(Cell Cell, GameObject Actor, ref bool Uncacheable, int Weight = 0, int Nav = 0)
	{
		if (Weight >= 100)
		{
			return Weight;
		}
		if (Cell != null && GameObject.Validate(ref Actor) && Actor.WantEvent(ID, MinEvent.CascadeLevel))
		{
			Zone.UnpackNav(Nav, out var Smart, out var Burrower, out var Autoexploring, out var Flying, out var WallWalker, out var IgnoresWalls, out var Swimming, out var Slimewalking, out var Aquatic, out var Polypwalking, out var Strutwalking, out var Juggernaut, out var Reefer, out var IgnoreCreatures, out var IgnoreGases, out var Unbreathing, out var FilthAffinity, out var OutOfPhase, out var Omniphase, out var Nullphase, out var FlexPhase);
			ActorGetNavigationWeightEvent actorGetNavigationWeightEvent = FromPool();
			actorGetNavigationWeightEvent.Cell = Cell;
			actorGetNavigationWeightEvent.Actor = Actor;
			actorGetNavigationWeightEvent.Object = null;
			actorGetNavigationWeightEvent.Uncacheable = Uncacheable;
			actorGetNavigationWeightEvent.Weight = Weight;
			actorGetNavigationWeightEvent.PriorWeight = Weight;
			actorGetNavigationWeightEvent.Smart = Smart;
			actorGetNavigationWeightEvent.Burrower = Burrower;
			actorGetNavigationWeightEvent.Autoexploring = Autoexploring;
			actorGetNavigationWeightEvent.Flying = Flying;
			actorGetNavigationWeightEvent.WallWalker = WallWalker;
			actorGetNavigationWeightEvent.IgnoresWalls = IgnoresWalls;
			actorGetNavigationWeightEvent.Swimming = Swimming;
			actorGetNavigationWeightEvent.Slimewalking = Slimewalking;
			actorGetNavigationWeightEvent.Aquatic = Aquatic;
			actorGetNavigationWeightEvent.Polypwalking = Polypwalking;
			actorGetNavigationWeightEvent.Strutwalking = Strutwalking;
			actorGetNavigationWeightEvent.Juggernaut = Juggernaut;
			actorGetNavigationWeightEvent.Reefer = Reefer;
			actorGetNavigationWeightEvent.IgnoreCreatures = IgnoreCreatures;
			actorGetNavigationWeightEvent.IgnoreGases = IgnoreGases;
			actorGetNavigationWeightEvent.Unbreathing = Unbreathing;
			actorGetNavigationWeightEvent.FilthAffinity = FilthAffinity;
			actorGetNavigationWeightEvent.OutOfPhase = OutOfPhase;
			actorGetNavigationWeightEvent.Omniphase = Omniphase;
			actorGetNavigationWeightEvent.Nullphase = Nullphase;
			actorGetNavigationWeightEvent.FlexPhase = FlexPhase;
			Actor.HandleEvent(actorGetNavigationWeightEvent);
			Weight = actorGetNavigationWeightEvent.Weight;
			Uncacheable = actorGetNavigationWeightEvent.Uncacheable;
		}
		return Weight;
	}
}
