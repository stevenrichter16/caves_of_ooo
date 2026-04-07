using System;
using XRL.UI;
using XRL.World.Capabilities;
using XRL.World.Parts.Mutation;

namespace XRL.World.Effects;

[Serializable]
public class HeightenedSmellEffect : Effect
{
	public bool Identified;

	public int Level = 1;

	public GameObject Smeller;

	public HeightenedSmellEffect()
	{
		Duration = 1;
	}

	public HeightenedSmellEffect(int Level, GameObject Smeller)
		: this()
	{
		this.Level = Level;
		this.Smeller = Smeller;
	}

	public override int GetEffectType()
	{
		return 512;
	}

	public override bool SameAs(Effect e)
	{
		return false;
	}

	public override string GetDescription()
	{
		return null;
	}

	public override void RegisterActive(GameObject Object, IEventRegistrar Registrar)
	{
		Registrar.Register(The.Game, this, PooledEvent<AfterPlayerBodyChangeEvent>.ID);
	}

	public override bool WantEvent(int ID, int cascade)
	{
		if (!base.WantEvent(ID, cascade) && ID != SingletonEvent<EndTurnEvent>.ID)
		{
			return ID == EnteringCellEvent.ID;
		}
		return true;
	}

	public override bool HandleEvent(AfterPlayerBodyChangeEvent E)
	{
		CheckSmell();
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(EndTurnEvent E)
	{
		CheckSmell();
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(EnteringCellEvent E)
	{
		CheckSmell();
		return base.HandleEvent(E);
	}

	private bool BadSmeller()
	{
		Smeller = null;
		base.Object?.RemoveEffect(this);
		return true;
	}

	public bool CheckSmell()
	{
		if (!GameObject.Validate(base.Object) || !GameObject.Validate(ref Smeller) || !Smeller.IsPlayer())
		{
			return BadSmeller();
		}
		HeightenedSmell part = Smeller.GetPart<HeightenedSmell>();
		if (part == null || part.Level <= 0)
		{
			return BadSmeller();
		}
		int num = base.Object.DistanceTo(Smeller);
		if (num > part.GetRadius())
		{
			return BadSmeller();
		}
		if (!base.Object.IsSmellable(Smeller))
		{
			return BadSmeller();
		}
		if (Identified)
		{
			return true;
		}
		if (base.Object.CurrentCell == null)
		{
			return true;
		}
		if (((int)((double)(100 + 20 * Level) / Math.Pow(num + 9, 2.0) * 100.0)).in100())
		{
			Identified = true;
			if (Smeller.IsPlayer())
			{
				AutoAct.CheckHostileInterrupt();
			}
		}
		return false;
	}

	public override bool Apply(GameObject Object)
	{
		CheckSmell();
		return true;
	}

	public bool NotSeen(GameObject obj)
	{
		if (obj == null || obj.IsPlayer())
		{
			return false;
		}
		if (!obj.IsVisible())
		{
			return true;
		}
		Cell cell = obj.CurrentCell;
		if (cell != null && (!cell.IsLit() || !cell.IsExplored()))
		{
			return true;
		}
		return false;
	}

	public override bool FinalRender(RenderEvent E, bool bAlt)
	{
		if (NotSeen(base.Object) && base.Object.CanHypersensesDetect())
		{
			if (Identified)
			{
				E.HighestLayer = 0;
				E.NoWake = true;
				base.Object.ComponentRender(E);
				E.RenderString = base.Object.Render.RenderString;
				if (Options.UseTiles)
				{
					E.HFlip = base.Object.Render.getHFlip();
					E.VFlip = base.Object.Render.getVFlip();
					E.Tile = base.Object.Render.Tile;
				}
				else
				{
					E.Tile = null;
				}
			}
			else
			{
				E.Tile = null;
				E.RenderString = "?";
			}
			E.ColorString = "&w";
			E.DetailColor = "w";
			E.CustomDraw = true;
			return false;
		}
		return true;
	}
}
