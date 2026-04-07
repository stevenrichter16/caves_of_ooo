using System;

namespace XRL.World.Parts;

[Serializable]
public class ImmobileFollower : IPart
{
	public bool JoinRandomInZone = true;

	public bool JoinAdjacentHostile;

	public override bool WantEvent(int ID, int cascade)
	{
		if (!base.WantEvent(ID, cascade))
		{
			return ID == PooledEvent<JoinPartyLeaderPossibleEvent>.ID;
		}
		return true;
	}

	public override bool HandleEvent(JoinPartyLeaderPossibleEvent E)
	{
		if (!E.IsMobile && !E.Result)
		{
			Zone parentZone = E.TargetCell.ParentZone;
			Cell cell = null;
			if (JoinAdjacentHostile)
			{
				cell = parentZone.GetObjects(ParentObject.IsHostileTowards)?.GetRandomElement()?.CurrentCell;
			}
			if (JoinRandomInZone && cell == null)
			{
				cell = parentZone.GetSpawnCell();
			}
			E.TargetCell = cell ?? E.TargetCell;
			E.Result = true;
		}
		return base.HandleEvent(E);
	}
}
