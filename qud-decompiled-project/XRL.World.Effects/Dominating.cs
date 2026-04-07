using XRL.Rules;
using XRL.World.AI.GoalHandlers;
using XRL.World.Parts.Mutation;

namespace XRL.World.Effects;

public class Dominating : Effect
{
	public GameObject Target;

	public Dominating()
	{
		DisplayName = "{{B|projecting consciousness}}";
		Duration = 1;
	}

	public Dominating(GameObject Target)
		: this()
	{
		this.Target = Target;
	}

	public override string GetDetails()
	{
		return "This creature is unresponsive.\nDV set to 0.";
	}

	public override int GetEffectType()
	{
		return 33587202;
	}

	public override bool Apply(GameObject Object)
	{
		if (Object.HasEffect(typeof(Dominating)) || !ApplyEffectEvent.Check(Object, "Dominating", this))
		{
			return false;
		}
		base.StatShifter.SetStatShift("DV", -Stats.GetCombatDV(Object));
		return base.Apply(Object);
	}

	public override void Remove(GameObject Object)
	{
		base.StatShifter.RemoveStatShifts();
		base.Remove(Object);
	}

	public override bool WantEvent(int ID, int cascade)
	{
		if (!base.WantEvent(ID, cascade) && ID != EffectAppliedEvent.ID && ID != SingletonEvent<BeginTakeActionEvent>.ID && ID != BeforeDeathRemovalEvent.ID && ID != GetZoneFreezabilityEvent.ID)
		{
			return ID == PooledEvent<IsConversationallyResponsiveEvent>.ID;
		}
		return true;
	}

	public override bool HandleEvent(EffectAppliedEvent E)
	{
		if (E.Effect != this && E.Effect.IsOfType(33554432))
		{
			InterruptDomination();
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(BeginTakeActionEvent E)
	{
		if (base.Object.IsPlayer())
		{
			InterruptDomination();
		}
		if (!GameObject.Validate(ref Target) || !Target.HasEffect(typeof(Dominated)))
		{
			base.Object.RemoveEffect(this);
			return true;
		}
		if (!(base.Object.Brain.Goals.Peek() is Dormant))
		{
			base.Object.Brain.Goals.Clear();
			base.Object.Brain.PushGoal(new Dormant(-1));
		}
		return false;
	}

	public override bool HandleEvent(BeforeDeathRemovalEvent E)
	{
		PerformMetempsychosis();
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(GetZoneFreezabilityEvent E)
	{
		E.Freezability = Freezability.FormerPlayerObject;
		return false;
	}

	public override bool HandleEvent(IsConversationallyResponsiveEvent E)
	{
		if (E.Speaker == base.Object && GameObject.Validate(ref Target))
		{
			if (E.Mental && !E.Physical)
			{
				E.Message = base.Object.Poss("mind") + " seems to be elsewhere.";
			}
			else
			{
				E.Message = base.Object.Does("are") + " utterly unresponsive.";
			}
			return false;
		}
		return base.HandleEvent(E);
	}

	public void InterruptDomination()
	{
		if (GameObject.Validate(ref Target))
		{
			Target.FireEvent("InterruptDomination");
		}
	}

	private bool IsOurDominationEffect(Dominated FX)
	{
		return FX.Dominator == base.Object;
	}

	public Dominated GetDominationEffect(GameObject obj = null)
	{
		return (obj ?? Target)?.GetEffect<Dominated>(IsOurDominationEffect);
	}

	public bool PerformMetempsychosis()
	{
		if (!GameObject.Validate(ref Target))
		{
			return false;
		}
		Dominated dominationEffect = GetDominationEffect();
		if (dominationEffect != null && !dominationEffect.BeingRemovedBySource)
		{
			dominationEffect.BeingRemovedBySource = true;
			dominationEffect.Metempsychosis = true;
			Target.RemoveEffect(dominationEffect);
			Domination.Metempsychosis(Target, dominationEffect.FromOriginalPlayerBody);
		}
		Target = null;
		return true;
	}
}
