using System;
using System.Collections.Generic;
using XRL.World.AI.Pathfinding;

namespace XRL.World.AI.GoalHandlers;

[Serializable]
public class MoveToGlobal : IMovementGoal
{
	public const int DEFAULT_MAX_WEIGHT = 95;

	private string dZone;

	private int dCx;

	private int dCy;

	private int Tries;

	private int MaxTurns = -1;

	public bool OverridesCombat;

	public int MaxWeight = 95;

	public MoveToGlobal()
	{
	}

	public MoveToGlobal(string ZoneID, int Cx, int Cy)
		: this()
	{
		dZone = ZoneID;
		dCx = Cx;
		dCy = Cy;
	}

	public MoveToGlobal(string ZoneID, int Cx, int Cy, int Turns)
		: this(ZoneID, Cx, Cy)
	{
		MaxTurns = Turns;
	}

	public MoveToGlobal(GlobalLocation Target)
		: this(Target.ZoneID, Target.CellX, Target.CellY)
	{
	}

	public MoveToGlobal(GlobalLocation Target, int Turns)
		: this(Target.ZoneID, Target.CellX, Target.CellY, Turns)
	{
	}

	public override bool Finished()
	{
		return Tries > 0;
	}

	public override void TakeAction()
	{
		Tries++;
		if (!base.ParentObject.IsMobile())
		{
			FailToParent();
			return;
		}
		if (base.ParentObject.CurrentZone.ZoneID == null)
		{
			Pop();
			return;
		}
		if (dZone == null)
		{
			Pop();
			return;
		}
		if (base.ParentObject.InZone(dZone))
		{
			base.ParentObject.UseEnergy(1000);
			FindPath findPath = new FindPath(base.ParentObject.CurrentZone.ZoneID, base.ParentObject.CurrentCell.X, base.ParentObject.CurrentCell.Y, dZone, dCx, dCy, PathGlobal: true, PathUnlimited: false, base.ParentObject, Juggernaut: false, IgnoreCreatures: false, IgnoreGases: false, FlexPhase: false, MaxWeight);
			if (findPath.Usable)
			{
				findPath.Directions.Reverse();
				int num = 0;
				if (MaxTurns > -1)
				{
					Pop();
				}
				{
					foreach (string direction in findPath.Directions)
					{
						PushGoal(new Step(direction, careful: false, overridesCombat: false, wandering: false, juggernaut: false, null, allowUnbuilt: true));
						num++;
						if (MaxTurns > -1 && num >= MaxTurns)
						{
							break;
						}
					}
					return;
				}
			}
			FailToParent();
			return;
		}
		base.ParentObject.UseEnergy(1000);
		string[] array = dZone.Split('.');
		int num2 = Convert.ToInt32(array[1]) * 3 + Convert.ToInt32(array[3]);
		int num3 = Convert.ToInt32(array[2]) * 3 + Convert.ToInt32(array[4]);
		int num4 = Convert.ToInt32(array[5]);
		List<string> list = new List<string>();
		if (base.ParentObject.CurrentZone.wX * 3 + base.ParentObject.CurrentZone.X < num2)
		{
			list.Add("E");
		}
		if (base.ParentObject.CurrentZone.wX * 3 + base.ParentObject.CurrentZone.X > num2)
		{
			list.Add("W");
		}
		if (base.ParentObject.CurrentZone.wY * 3 + base.ParentObject.CurrentZone.Y < num3)
		{
			list.Add("S");
		}
		if (base.ParentObject.CurrentZone.wY * 3 + base.ParentObject.CurrentZone.Y > num3)
		{
			list.Add("N");
		}
		if (base.ParentObject.CurrentZone.Z < num4)
		{
			GameObject gameObject = base.ParentObject.CurrentZone.FindClosestObjectWithPart(base.ParentObject, "StairsDown");
			if (gameObject != null)
			{
				base.ParentObject.Brain.PushGoal(new Step("D", careful: false, OverridesCombat, wandering: false, juggernaut: false, null, allowUnbuilt: true));
				base.ParentObject.Brain.PushGoal(new MoveTo(gameObject, careful: false, OverridesCombat));
				return;
			}
		}
		if (base.ParentObject.CurrentZone.Z > num4)
		{
			GameObject gameObject2 = base.ParentObject.CurrentZone.FindClosestObjectWithPart(base.ParentObject, "StairsUp");
			if (gameObject2 != null)
			{
				base.ParentObject.Brain.PushGoal(new Step("U", careful: false, OverridesCombat, wandering: false, juggernaut: false, null, allowUnbuilt: true));
				base.ParentObject.Brain.PushGoal(new MoveTo(gameObject2, careful: false, OverridesCombat));
				return;
			}
		}
		if (base.ParentObject.CurrentZone.Z != num4 || list.Count == 0)
		{
			list.Add("N");
			list.Add("S");
			list.Add("E");
			list.Add("W");
		}
		string text = list[new Random().Next(0, list.Count - 1)];
		List<Cell> list2 = new List<Cell>();
		if (text == "N")
		{
			for (int i = 0; i < base.CurrentZone.Width; i++)
			{
				list2.Add(base.CurrentZone.GetCell(i, 0));
			}
		}
		if (text == "S")
		{
			for (int j = 0; j < base.CurrentZone.Width; j++)
			{
				list2.Add(base.CurrentZone.GetCell(j, base.CurrentZone.Height - 1));
			}
		}
		if (text == "W")
		{
			for (int k = 0; k < base.CurrentZone.Height; k++)
			{
				list2.Add(base.CurrentZone.GetCell(0, k));
			}
		}
		if (text == "E")
		{
			for (int l = 0; l < base.CurrentZone.Height; l++)
			{
				list2.Add(base.CurrentZone.GetCell(base.CurrentZone.Width - 1, l));
			}
		}
		list2.ShuffleInPlace();
		for (int m = 0; m < list2.Count; m++)
		{
			if (!list2[m].IsReachable())
			{
				continue;
			}
			FindPath findPath2 = new FindPath(base.ParentObject.CurrentCell, list2[m], PathGlobal: true, PathUnlimited: true, base.ParentObject, MaxWeight);
			if (!findPath2.Usable)
			{
				break;
			}
			findPath2.Directions.Reverse();
			PushChildGoal(new Step(text, careful: false, OverridesCombat, wandering: false, juggernaut: false, null, allowUnbuilt: true));
			{
				foreach (string direction2 in findPath2.Directions)
				{
					PushChildGoal(new Step(direction2, careful: false, OverridesCombat, wandering: false, juggernaut: false, null, allowUnbuilt: true));
				}
				break;
			}
		}
	}
}
