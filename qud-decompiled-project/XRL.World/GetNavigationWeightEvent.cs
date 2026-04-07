using System.Collections.Generic;

namespace XRL.World;

[GameEvent(Cache = Cache.Pool)]
public class GetNavigationWeightEvent : INavigationWeightEvent
{
	public new static readonly int ID = MinEvent.RegisterEvent(typeof(GetNavigationWeightEvent), null, CountPool, ResetPool);

	private static List<GetNavigationWeightEvent> Pool;

	private static int PoolCounter;

	public GetNavigationWeightEvent()
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

	public static void ResetTo(ref GetNavigationWeightEvent E)
	{
		MinEvent.ResetTo(E, Pool, ref PoolCounter);
		E = null;
	}

	public static GetNavigationWeightEvent FromPool()
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
		if (Cell != null)
		{
			GetNavigationWeightEvent getNavigationWeightEvent = null;
			int i = 0;
			for (int count = Cell.Objects.Count; i < count; i++)
			{
				int priorWeight = Weight;
				GameObject gameObject = Cell.Objects[i];
				int intProperty = gameObject.GetIntProperty("NavigationWeight");
				if (intProperty > Weight)
				{
					Weight = intProperty;
				}
				if (gameObject.WantEvent(ID, MinEvent.CascadeLevel))
				{
					if (getNavigationWeightEvent == null)
					{
						Zone.UnpackNav(Nav, out var Smart, out var Burrower, out var Autoexploring, out var Flying, out var WallWalker, out var IgnoresWalls, out var Swimming, out var Slimewalking, out var Aquatic, out var Polypwalking, out var Strutwalking, out var Juggernaut, out var Reefer, out var IgnoreCreatures, out var IgnoreGases, out var Unbreathing, out var FilthAffinity, out var OutOfPhase, out var Omniphase, out var Nullphase, out var FlexPhase);
						getNavigationWeightEvent = FromPool();
						getNavigationWeightEvent.Cell = Cell;
						getNavigationWeightEvent.Actor = Actor;
						getNavigationWeightEvent.Uncacheable = Uncacheable;
						getNavigationWeightEvent.Weight = 0;
						getNavigationWeightEvent.PriorWeight = 0;
						getNavigationWeightEvent.Smart = Smart;
						getNavigationWeightEvent.Burrower = Burrower;
						getNavigationWeightEvent.Autoexploring = Autoexploring;
						getNavigationWeightEvent.Flying = Flying;
						getNavigationWeightEvent.WallWalker = WallWalker;
						getNavigationWeightEvent.IgnoresWalls = IgnoresWalls;
						getNavigationWeightEvent.Swimming = Swimming;
						getNavigationWeightEvent.Slimewalking = Slimewalking;
						getNavigationWeightEvent.Aquatic = Aquatic;
						getNavigationWeightEvent.Polypwalking = Polypwalking;
						getNavigationWeightEvent.Strutwalking = Strutwalking;
						getNavigationWeightEvent.Juggernaut = Juggernaut;
						getNavigationWeightEvent.Reefer = Reefer;
						getNavigationWeightEvent.IgnoreCreatures = IgnoreCreatures;
						getNavigationWeightEvent.IgnoreGases = IgnoreGases;
						getNavigationWeightEvent.Unbreathing = Unbreathing;
						getNavigationWeightEvent.FilthAffinity = FilthAffinity;
						getNavigationWeightEvent.OutOfPhase = OutOfPhase;
						getNavigationWeightEvent.Omniphase = Omniphase;
						getNavigationWeightEvent.Nullphase = Nullphase;
						getNavigationWeightEvent.FlexPhase = FlexPhase;
					}
					getNavigationWeightEvent.Object = gameObject;
					getNavigationWeightEvent.Weight = Weight;
					getNavigationWeightEvent.PriorWeight = priorWeight;
					gameObject.HandleEvent(getNavigationWeightEvent);
					Weight = getNavigationWeightEvent.Weight;
					Uncacheable = getNavigationWeightEvent.Uncacheable;
				}
				if (Weight >= 100)
				{
					return Weight;
				}
			}
		}
		return Weight;
	}
}
