using System;

namespace XRL.World.Parts;

[Serializable]
public class AIShootRound : AIBehaviorPart
{
	public int X;

	public int Y;

	public int Cooldown;

	public void SetTarget(Cell Cell)
	{
		X = Cell.X;
		Y = Cell.Y;
	}

	public override bool WantTurnTick()
	{
		return true;
	}

	public override void TurnTick(long TimeTick, int Amount)
	{
		if (Cooldown > 0)
		{
			Cooldown = Math.Max(Cooldown - Amount, 0);
			return;
		}
		GameObject gameObject = ParentObject.GetMissileWeapons()?.GetRandomElement();
		Cell cell = GetAnyBasisZone()?.GetCell(X, Y);
		if (gameObject != null && cell != null)
		{
			Event obj = Event.New("CommandFireMissile");
			obj.SetParameter("Owner", ParentObject);
			obj.SetParameter("TargetCell", cell);
			obj.SetParameter("EnergyMultiplier", 0f);
			gameObject.FireEvent(obj);
		}
	}
}
