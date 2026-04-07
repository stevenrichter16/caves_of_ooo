using XRL.World.Anatomy;

namespace XRL.World;

[GameEvent(Cascade = 17, Cache = Cache.Pool)]
public class AfterDismemberEvent : PooledEvent<AfterDismemberEvent>
{
	public new static readonly int CascadeLevel = 17;

	public GameObject Actor;

	public GameObject Object;

	public GameObject Limb;

	public BodyPart Part;

	public IInventory Where;

	public bool Silent;

	public bool Obliterate;

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
		Object = null;
		Limb = null;
		Part = null;
		Where = null;
		Silent = false;
		Obliterate = false;
	}

	public static void Send(GameObject Actor, GameObject Object, GameObject Limb, BodyPart Part, IInventory Where = null, bool Silent = false, bool Obliterate = false)
	{
		AfterDismemberEvent E = PooledEvent<AfterDismemberEvent>.FromPool();
		E.Actor = Actor;
		E.Object = Object;
		E.Limb = Limb;
		E.Part = Part;
		E.Where = Where;
		E.Silent = Silent;
		E.Obliterate = Obliterate;
		if (Actor != null && Actor != Object)
		{
			Actor.HandleEvent(E);
		}
		Object?.HandleEvent(E);
		Limb?.HandleEvent(E);
		PooledEvent<AfterDismemberEvent>.ResetTo(ref E);
	}
}
