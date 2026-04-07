using System;
using XRL.UI;
using XRL.World.Capabilities;
using XRL.World.Parts;

namespace XRL.World.Effects;

[Serializable]
public class SenseRobotEffect : Effect
{
	public bool Identified;

	public int Level = 1;

	public GameObject Listener;

	public GameObject Device;

	public SenseRobotEffect()
	{
		Duration = 1;
	}

	public SenseRobotEffect(int Level = 1, GameObject Listener = null, GameObject Device = null)
		: this()
	{
		this.Level = Level;
		this.Listener = Listener;
		this.Device = Device;
	}

	public override int GetEffectType()
	{
		return 16777280;
	}

	public override bool SameAs(Effect e)
	{
		return false;
	}

	public override string GetDescription()
	{
		return null;
	}

	private bool InvalidListen()
	{
		Listener = null;
		base.Object?.RemoveEffect(this);
		return true;
	}

	public bool CheckListen()
	{
		if (!GameObject.Validate(base.Object) || !GameObject.Validate(ref Listener) || !Listener.IsPlayer() || Listener.IsNowhere())
		{
			return InvalidListen();
		}
		int num = base.Object.DistanceTo(Listener);
		if (num > Level)
		{
			return InvalidListen();
		}
		if (!CyberneticsElectromagneticSensor.WillSense(base.Object))
		{
			return InvalidListen();
		}
		if (!GameObject.Validate(ref Device) || Device.Implantee != Listener)
		{
			return InvalidListen();
		}
		if (Identified)
		{
			return true;
		}
		if (base.Object.CurrentCell == null)
		{
			return true;
		}
		if (((int)((double)(100 + 10 * Level) / Math.Pow(num + 9, 2.0) * 100.0)).in100())
		{
			Identified = true;
			if (Listener.IsPlayer())
			{
				AutoAct.CheckHostileInterrupt();
			}
		}
		return false;
	}

	public override bool Apply(GameObject Object)
	{
		CheckListen();
		return true;
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
		CheckListen();
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(EndTurnEvent E)
	{
		CheckListen();
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(EnteringCellEvent E)
	{
		CheckListen();
		return base.HandleEvent(E);
	}

	public override bool FinalRender(RenderEvent E, bool bAlt)
	{
		if (base.Object == null || base.Object.IsPlayer())
		{
			return true;
		}
		Cell cell = base.Object.CurrentCell;
		if (cell != null && (!cell.IsLit() || !cell.IsExplored() || !cell.IsVisible()))
		{
			if (Identified)
			{
				E.HighestLayer = 0;
				E.NoWake = true;
				base.Object.ComponentRender(E);
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
			E.ColorString = "&C";
			E.DetailColor = "C";
			E.CustomDraw = true;
			return false;
		}
		return true;
	}
}
