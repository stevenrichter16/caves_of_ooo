using System;
using System.Text;
using XRL.World.Parts;

namespace XRL.World.AI.GoalHandlers;

[Serializable]
public class Step : IMovementGoal
{
	public string Dir;

	public bool allowUnbuilt;

	public bool careful;

	public bool overridesCombat;

	public bool wandering;

	public bool juggernaut;

	public GameObject Toward;

	[NonSerialized]
	private static StringBuilder detailBuilder = new StringBuilder();

	public Step()
	{
	}

	public Step(string Direction, bool careful = false, bool overridesCombat = false, bool wandering = false, bool juggernaut = false, GameObject Toward = null, bool allowUnbuilt = false)
		: this()
	{
		Dir = Direction;
		this.careful = careful;
		this.overridesCombat = overridesCombat;
		this.wandering = wandering;
		this.juggernaut = juggernaut;
		this.Toward = Toward;
		this.allowUnbuilt = allowUnbuilt;
	}

	public override string GetDetails()
	{
		detailBuilder.Clear().Append(Dir);
		if (careful)
		{
			detailBuilder.Append('.');
		}
		if (allowUnbuilt)
		{
			detailBuilder.Append('+');
		}
		if (overridesCombat)
		{
			detailBuilder.Append('!');
		}
		if (wandering)
		{
			detailBuilder.Append('?');
		}
		if (juggernaut)
		{
			detailBuilder.Append('*');
		}
		return detailBuilder.ToString();
	}

	public override bool Finished()
	{
		return false;
	}

	public override bool CanFight()
	{
		return !overridesCombat;
	}

	private bool CellHasHostile(Cell TargetCell)
	{
		if (TargetCell == null)
		{
			return false;
		}
		if (ParentBrain == null)
		{
			return false;
		}
		int i = 0;
		for (int count = TargetCell.Objects.Count; i < count; i++)
		{
			GameObject gameObject = TargetCell.Objects[i];
			if (gameObject.HasPart<Combat>() && ParentBrain.IsHostileTowards(gameObject) && base.ParentObject.PhaseAndFlightMatches(gameObject))
			{
				return true;
			}
		}
		return false;
	}

	private bool CellHasLedBySame(Cell TargetCell, ref GameObject Skip)
	{
		if (TargetCell == null)
		{
			return false;
		}
		GameObject gameObject = ParentBrain?.GetFinalLeader();
		if (gameObject == null)
		{
			return false;
		}
		int i = 0;
		for (int count = TargetCell.Objects.Count; i < count; i++)
		{
			GameObject gameObject2 = TargetCell.Objects[i];
			if (gameObject2 != Skip && gameObject2.IsLedBy(gameObject))
			{
				if (base.ParentObject.PhaseAndFlightMatches(gameObject2) && gameObject2.IsPotentiallyMobile())
				{
					return true;
				}
				Skip = gameObject2;
			}
		}
		return false;
	}

	private bool CellHasSomeoneMobileWithSameTarget(Cell TargetCell, ref GameObject Skip)
	{
		if (TargetCell == null)
		{
			return false;
		}
		GameObject gameObject = base.ParentObject?.Target;
		if (gameObject == null)
		{
			return false;
		}
		int i = 0;
		for (int count = TargetCell.Objects.Count; i < count; i++)
		{
			GameObject gameObject2 = TargetCell.Objects[i];
			if (gameObject2 != Skip && gameObject2.Target == gameObject)
			{
				if (base.ParentObject.PhaseAndFlightMatches(gameObject2) && gameObject2.IsPotentiallyMobile())
				{
					return true;
				}
				Skip = gameObject2;
			}
		}
		return false;
	}

	private bool CellHasMobileAlly(Cell TargetCell, ref GameObject Skip)
	{
		if (TargetCell == null)
		{
			return false;
		}
		int i = 0;
		for (int count = TargetCell.Objects.Count; i < count; i++)
		{
			GameObject gameObject = TargetCell.Objects[i];
			if (gameObject != Skip && gameObject.IsAlliedTowards(base.ParentObject))
			{
				if (base.ParentObject.PhaseAndFlightMatches(gameObject) && gameObject.IsPotentiallyMobile())
				{
					return true;
				}
				Skip = gameObject;
			}
		}
		return false;
	}

	private bool CellHasMobileNonEnemy(Cell TargetCell, ref GameObject Skip)
	{
		if (TargetCell == null || base.ParentObject == null)
		{
			return false;
		}
		int i = 0;
		for (int count = TargetCell.Objects.Count; i < count; i++)
		{
			GameObject gameObject = TargetCell.Objects[i];
			if (gameObject != Skip && !ParentBrain.IsHostileTowards(gameObject))
			{
				if (base.ParentObject.PhaseAndFlightMatches(gameObject) && gameObject.IsPotentiallyMobile())
				{
					return true;
				}
				Skip = gameObject;
			}
		}
		return false;
	}

	public override void TakeAction()
	{
		if (!base.ParentObject.IsMobile())
		{
			Think("I can't move!");
			FailToParent();
			return;
		}
		if (Toward != null && (!Toward.IsValid() || Toward.IsNowhere()))
		{
			Think("I was trying to go toward someone, but they're gone!");
			FailToParent();
			return;
		}
		Cell cellFromDirection = base.ParentObject.CurrentCell.GetCellFromDirection(Dir, !allowUnbuilt);
		if (cellFromDirection == null)
		{
			Think("I can't move there!");
			FailToParent();
			return;
		}
		if (wandering && cellFromDirection.HasObjectWithTagOrProperty("WanderStopper"))
		{
			Think("I shouldn't wander there!");
			FailToParent();
			return;
		}
		if (careful && cellFromDirection.GetNavigationWeightFor(base.ParentObject) >= 10)
		{
			Think("That's too dangerous!");
			FailToParent();
			return;
		}
		if (!juggernaut)
		{
			if (CellHasHostile(cellFromDirection) || (cellFromDirection == The.Player.CurrentCell && base.ParentObject.PhaseAndFlightMatches(The.Player)))
			{
				Think("There's something in my way!");
				FailToParent();
				return;
			}
			int chance = 100;
			GameObject Skip = null;
			if (CellHasSomeoneMobileWithSameTarget(cellFromDirection, ref Skip))
			{
				chance = 2;
			}
			else if (CellHasLedBySame(cellFromDirection, ref Skip))
			{
				chance = 3;
			}
			else if (CellHasMobileAlly(cellFromDirection, ref Skip))
			{
				chance = 5;
			}
			else if (CellHasMobileNonEnemy(cellFromDirection, ref Skip))
			{
				chance = 30;
			}
			if (!chance.in100())
			{
				Think("There's something in my way!");
				FailToParent();
				return;
			}
		}
		try
		{
			if (base.ParentObject != null && !base.ParentObject.Move(Dir))
			{
				if (base.ParentObject.GetIntProperty("AIKeepMoving") > 0)
				{
					base.ParentObject.SetIntProperty("AIKeepMoving", 0);
				}
				else
				{
					FailToParent();
				}
				return;
			}
		}
		catch (Exception x)
		{
			MetricsManager.LogException("Step::MoveDirection", x);
		}
		Pop();
	}

	public Cell GetDestinationCell()
	{
		Cell currentCell = base.ParentObject.CurrentCell;
		if (currentCell != null)
		{
			Cell cellFromDirection = currentCell.GetCellFromDirection(Dir);
			if (cellFromDirection != null)
			{
				return cellFromDirection;
			}
		}
		return null;
	}
}
