using System;
using System.Text;
using XRL.Rules;

namespace XRL.World.Effects;

[Serializable]
public class Confused : Effect, ITierInitialized
{
	public const string DEFAULT_CHECK_VS = "Confusion";

	public int Level;

	public int AppliedPlayerConfusion;

	public int MentalPenalty;

	public string NameForChecks = "Confusion";

	public Confused()
	{
		DisplayName = "{{R|confused}}";
	}

	public Confused(int Duration = 1, int Level = 0, int MentalPenalty = 0, string NameForChecks = "Confusion")
		: this()
	{
		base.Duration = Duration;
		this.Level = Level;
		this.MentalPenalty = MentalPenalty;
		this.NameForChecks = NameForChecks;
	}

	public void Initialize(int Tier)
	{
		Tier = Stat.Random(Tier - 2, Tier + 2);
		if (Tier < 1)
		{
			Tier = 1;
		}
		if (Tier > 8)
		{
			Tier = 8;
		}
		Duration = Stat.Random(15, 60);
		Level = Stat.Random(1, 10);
		MentalPenalty = Level;
	}

	public override int GetEffectType()
	{
		return 117440514;
	}

	public override bool SameAs(Effect e)
	{
		return false;
	}

	public override string GetDetails()
	{
		StringBuilder stringBuilder = Event.NewStringBuilder();
		stringBuilder.Append("Acts semi-randomly.\n-").Append(Level).Append(" DV\n-")
			.Append(Level)
			.Append(" MA");
		if (MentalPenalty > 0)
		{
			stringBuilder.Append("\n-").Append(MentalPenalty).Append(" to all mental attributes");
		}
		return stringBuilder.ToString();
	}

	public override bool Apply(GameObject Object)
	{
		if (Object.Brain == null)
		{
			return false;
		}
		if (!Object.FireEvent("CanApplyConfusion"))
		{
			return false;
		}
		if (!Object.FireEvent("ApplyConfusion"))
		{
			return false;
		}
		if (!ApplyEffectEvent.Check(Object, NameForChecks, this))
		{
			return false;
		}
		if (Object.HasEffect<Confused>())
		{
			return false;
		}
		Object?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_bewilderment");
		ApplyChanges();
		DidX("become", "confused", "!", null, null, null, Object);
		return base.Apply(Object);
	}

	public override void Remove(GameObject Object)
	{
		if (!Object.IsPlayer())
		{
			Object.Brain.Goals.Clear();
			Object.Brain.Target = null;
		}
		UnapplyChanges();
		base.Remove(Object);
	}

	private void ApplyChanges()
	{
		base.StatShifter.SetStatShift(base.Object, "DV", -Level);
		base.StatShifter.SetStatShift(base.Object, "MA", -Level);
		base.StatShifter.SetStatShift(base.Object, "Willpower", -MentalPenalty);
		base.StatShifter.SetStatShift(base.Object, "Intelligence", -MentalPenalty);
		base.StatShifter.SetStatShift(base.Object, "Ego", -MentalPenalty);
	}

	private void UnapplyChanges()
	{
		base.StatShifter.RemoveStatShifts();
	}

	public override bool Render(RenderEvent E)
	{
		if (Duration == 0)
		{
			return true;
		}
		E.RenderEffectIndicator("?", null, "&R", "R", 35, 25);
		return true;
	}

	public override bool WantEvent(int ID, int cascade)
	{
		if (!base.WantEvent(ID, cascade) && ID != PooledEvent<IsConversationallyResponsiveEvent>.ID && ID != PooledEvent<GetCompanionStatusEvent>.ID)
		{
			return ID == GetLostChanceEvent.ID;
		}
		return true;
	}

	public override bool HandleEvent(GetLostChanceEvent E)
	{
		if (E.Actor == base.Object)
		{
			E.PercentageBonus -= Level * 100;
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(GetCompanionStatusEvent E)
	{
		if (E.Object == base.Object)
		{
			E.AddStatus("confused", 80);
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(IsConversationallyResponsiveEvent E)
	{
		if (E.Speaker == base.Object)
		{
			if (E.Mental && !E.Physical)
			{
				E.Message = base.Object.Poss("mind") + " is in disarray.";
			}
			else
			{
				E.Message = base.Object.Does("don't") + " seem to understand you.";
			}
			return false;
		}
		return base.HandleEvent(E);
	}

	public override void Register(GameObject Object, IEventRegistrar Registrar)
	{
		Registrar.Register("AfterDeepCopyWithoutEffects");
		Registrar.Register("BeforeDeepCopyWithoutEffects");
		Registrar.Register("BeginTakeAction");
		Registrar.Register("CanApplyConfusion");
		base.Register(Object, Registrar);
	}

	public override bool FireEvent(Event E)
	{
		if (E.ID == "BeginTakeAction")
		{
			if (Duration > 0)
			{
				Duration--;
			}
			if (Duration > 0 && !base.Object.IsPlayer())
			{
				int num = Stat.Random(1, 100);
				if (num <= 50)
				{
					base.Object.Move(Directions.GetRandomDirection());
					base.Object.UseEnergy(base.Object.Energy.Value);
					return false;
				}
				if (num <= 60)
				{
					base.Object.Target = base.currentCell?.GetLocalAdjacentCells(5)?.GetRandomElement()?.GetObjectsInCell()?.GetRandomElement();
					base.Object.UseEnergy(base.Object.Energy.Value);
					return false;
				}
			}
		}
		else
		{
			if (E.ID == "CanApplyConfusion")
			{
				return false;
			}
			if (E.ID == "BeforeDeepCopyWithoutEffects")
			{
				UnapplyChanges();
			}
			else if (E.ID == "AfterDeepCopyWithoutEffects")
			{
				ApplyChanges();
			}
		}
		return base.FireEvent(E);
	}
}
