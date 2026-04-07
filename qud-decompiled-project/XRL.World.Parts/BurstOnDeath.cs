using System;

namespace XRL.World.Parts;

[Serializable]
public class BurstOnDeath : IPart
{
	public string Blueprint;

	public int Radius = 1;

	public int Chance = 100;

	public bool SkipOccluding;

	public bool SkipSolid;

	public override bool SameAs(IPart p)
	{
		return true;
	}

	public override bool WantEvent(int ID, int cascade)
	{
		if (!base.WantEvent(ID, cascade))
		{
			return ID == BeforeDeathRemovalEvent.ID;
		}
		return true;
	}

	public override bool HandleEvent(BeforeDeathRemovalEvent E)
	{
		if (!Blueprint.IsNullOrEmpty() && !ParentObject.IsNowhere())
		{
			Cell.SpiralEnumerator enumerator = ParentObject.CurrentCell.IterateAdjacent(Radius, IncludeSelf: true).GetEnumerator();
			while (enumerator.MoveNext())
			{
				Cell current = enumerator.Current;
				if ((!SkipOccluding || !current.IsOccluding()) && (!SkipSolid || !current.IsSolid()) && Chance.in100())
				{
					current.AddObject(Blueprint.GetRandomSubstring(','));
				}
			}
		}
		return base.HandleEvent(E);
	}
}
