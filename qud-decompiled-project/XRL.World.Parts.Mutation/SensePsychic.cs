using System;
using System.Collections.Generic;
using XRL.World.Effects;

namespace XRL.World.Parts.Mutation;

[Serializable]
public class SensePsychic : BaseMutation
{
	public int BaseRadius = 9;

	public bool Levelable;

	public bool RealityDistortionBased;

	public int Radius
	{
		get
		{
			if (!Levelable)
			{
				return BaseRadius;
			}
			return BaseRadius + base.Level;
		}
	}

	public SensePsychic()
	{
		base.Type = "Mental";
	}

	public override bool CanLevel()
	{
		return Levelable;
	}

	public override bool WantTurnTick()
	{
		return true;
	}

	public override void TurnTick(long TimeTick, int Amount)
	{
		SensePsychics();
	}

	public override string GetDescription()
	{
		return string.Concat("" + "You can sense other mental mutants through the psychic aether.\n\n", "You detect the presence of psychic enemies within a radius of ", Radius.ToString(), ".\nThere's a chance you identify detected enemies.");
	}

	public override string GetLevelText(int Level)
	{
		return "";
	}

	public override bool ChangeLevel(int NewLevel)
	{
		return base.ChangeLevel(NewLevel);
	}

	public override bool Mutate(GameObject GO, int Level)
	{
		return base.Mutate(GO, Level);
	}

	public override bool Unmutate(GameObject GO)
	{
		return base.Unmutate(GO);
	}

	public override bool WantEvent(int ID, int cascade)
	{
		if (!base.WantEvent(ID, cascade))
		{
			return ID == PooledEvent<ExtraHostilePerceptionEvent>.ID;
		}
		return true;
	}

	public override bool HandleEvent(ExtraHostilePerceptionEvent E)
	{
		if (E.Actor == ParentObject && ParentObject.IsPlayer())
		{
			Cell cell = ParentObject.CurrentCell;
			if (cell != null)
			{
				List<GameObject> list = cell.ParentZone.FastSquareSearch(cell.X, cell.Y, Radius, "Combat");
				int i = 0;
				for (int count = list.Count; i < count; i++)
				{
					GameObject gameObject = list[i];
					if (gameObject.GetEffect(typeof(SensePsychicEffect), IsOurs) is SensePsychicEffect sensePsychicEffect && sensePsychicEffect.Listener == ParentObject && sensePsychicEffect.Identified && ParentObject.IsRelevantHostile(gameObject))
					{
						E.Hostile = gameObject;
						E.PerceiveVerb = "sense";
					}
				}
			}
		}
		return base.HandleEvent(E);
	}

	private static bool SensePsychicListenerIsPlayer(Effect FX)
	{
		return (FX as SensePsychicEffect).Listener?.IsPlayer() ?? false;
	}

	public static SensePsychicEffect SensePsychicFromPlayer(GameObject GO)
	{
		return GO.GetEffect(typeof(SensePsychicEffect), SensePsychicListenerIsPlayer) as SensePsychicEffect;
	}

	private bool IsOurs(Effect FX)
	{
		return (FX as SensePsychicEffect).Listener == ParentObject;
	}

	public bool IsOtherPsychic(GameObject obj)
	{
		if (obj == ParentObject)
		{
			return false;
		}
		return IsSensableAsPsychicEvent.Check(obj);
	}

	public bool IsOtherReachablePsychic(GameObject obj)
	{
		if (!IsOtherPsychic(obj))
		{
			return false;
		}
		return IComponent<GameObject>.CheckRealityDistortionAccessibility(obj, null, ParentObject, null, this);
	}

	public void SensePsychics()
	{
		if (!ParentObject.IsPlayer())
		{
			return;
		}
		Cell cell = ParentObject.CurrentCell;
		if (cell == null || cell.OnWorldMap())
		{
			return;
		}
		List<GameObject> list = ((!RealityDistortionBased) ? cell.ParentZone.FastSquareSearch(cell.X, cell.Y, Radius, IsOtherPsychic, CachedOkay: true) : ((!CheckMyRealityDistortionUsability()) ? null : cell.ParentZone.FastSquareSearch(cell.X, cell.Y, Radius, IsOtherReachablePsychic, CachedOkay: true)));
		if (list == null)
		{
			return;
		}
		int i = 0;
		for (int count = list.Count; i < count; i++)
		{
			GameObject gameObject = list[i];
			if (!gameObject.HasEffect(typeof(SensePsychicEffect), IsOurs))
			{
				gameObject.ApplyEffect(new SensePsychicEffect((!Levelable) ? 1 : base.Level, ParentObject));
			}
		}
	}
}
