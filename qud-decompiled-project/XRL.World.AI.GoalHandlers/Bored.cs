using System;
using System.Collections.Generic;
using XRL.Rules;
using XRL.World.AI.Pathfinding;

namespace XRL.World.AI.GoalHandlers;

[Serializable]
public class Bored : GoalHandler
{
	public int ExtinguishSelfTries;

	public long NextLeaderPathFind;

	public long NextLeaderAltPathFind;

	public override bool Finished()
	{
		return false;
	}

	public override bool IsBusy()
	{
		return false;
	}

	public bool TryMovementAbilities(Cell TargetCell, int Distance = -1, int StandoffDistance = 0)
	{
		if (GameObject.Validate(base.ParentObject) && AICommandList.HandleCommandList(AIGetMovementAbilityListEvent.GetFor(base.ParentObject, null, TargetCell, Distance, StandoffDistance), base.ParentObject, null, TargetCell))
		{
			Think("Did a movement ability with a target cell");
			return true;
		}
		return false;
	}

	public bool TryMovementAbilities()
	{
		if (GameObject.Validate(base.ParentObject) && AICommandList.HandleCommandList(AIGetMovementAbilityListEvent.GetFor(base.ParentObject), base.ParentObject))
		{
			Think("Did a movement ability");
			return true;
		}
		return false;
	}

	public bool TryPassiveAbilities()
	{
		if (GameObject.Validate(base.ParentObject) && AICommandList.HandleCommandList(AIGetPassiveAbilityListEvent.GetFor(base.ParentObject), base.ParentObject))
		{
			Think("Did a passive ability");
			return true;
		}
		return false;
	}

	public bool TryPassiveItems()
	{
		if (GameObject.Validate(base.ParentObject) && AICommandList.HandleCommandList(AIGetPassiveItemListEvent.GetFor(base.ParentObject), base.ParentObject))
		{
			Think("Did a passive item");
			return true;
		}
		return false;
	}

	public void TakeActionWithPartyLeader()
	{
		if (base.ParentObject == null || ParentBrain == null)
		{
			return;
		}
		GameObject partyLeader = ParentBrain.PartyLeader;
		Cell currentCell = base.ParentObject.CurrentCell;
		if (currentCell == null || currentCell.ParentZone == null || ParentBrain.GoToPartyLeader())
		{
			return;
		}
		Zone parentZone = currentCell.ParentZone;
		if (parentZone == null || !parentZone.IsActive())
		{
			return;
		}
		if (ParentBrain.Target == null && ParentBrain.CanAcquireTarget())
		{
			if (partyLeader.IsPlayerControlled())
			{
				GameObject target = partyLeader.Target;
				if (target != null && ParentBrain.CheckPerceptionOf(target))
				{
					ParentBrain.WantToKill(target, "to boredly aid my leader");
					if (ParentBrain.Target != null)
					{
						return;
					}
				}
			}
			GameObject gameObject = ParentBrain.FindProspectiveTarget(currentCell);
			if (gameObject != null)
			{
				ParentBrain.WantToKill(gameObject, "out of bored hostility while having a party leader");
				if (ParentBrain.Target != null)
				{
					return;
				}
			}
			else
			{
				Think("I boredly looked for a target while having a party leader, but didn't find one.");
			}
		}
		if (TryMovementAbilities() || TryPassiveAbilities() || TryPassiveItems())
		{
			return;
		}
		if (!base.ParentObject.IsMobile())
		{
			base.ParentObject.UseEnergy(1000);
		}
		else if (ParentBrain.Staying)
		{
			bool flag = false;
			if (!ParentBrain.Wanders && !ParentBrain.WandersRandomly)
			{
				Cell cell = ParentBrain?.StartingCell?.ResolveCell();
				if (cell != null && currentCell != cell && cell.ParentZone == parentZone && base.ParentObject.FireEvent("CanAIDoIndependentBehavior"))
				{
					PushChildGoal(new MoveTo(parentZone.ZoneID, cell.X, cell.Y, careful: true));
					flag = true;
				}
			}
			if (!flag)
			{
				base.ParentObject.UseEnergy(1000);
			}
		}
		else if (The.Game.TimeTicks > NextLeaderPathFind)
		{
			Cell currentCell2 = partyLeader.CurrentCell;
			if (currentCell2 == null || currentCell2.ParentZone == null || currentCell2.OnWorldMap())
			{
				return;
			}
			int num = GetPartyLeaderFollowDistanceEvent.GetFor(base.ParentObject, partyLeader);
			int num2 = base.ParentObject.DistanceTo(currentCell2);
			if (num2 <= num || TryMovementAbilities(currentCell2, num2, num))
			{
				return;
			}
			FindPath findPath = new FindPath(currentCell, currentCell2, PathGlobal: false, PathUnlimited: true, base.ParentObject, 95);
			if (findPath.Usable)
			{
				NextLeaderPathFind = The.Game.TimeTicks;
				PathTowardLeader(findPath, num);
				return;
			}
			if (base.ParentObject.IsPlayerLed())
			{
				NextLeaderPathFind = The.Game.TimeTicks + 2;
			}
			else
			{
				NextLeaderPathFind = The.Game.TimeTicks + Stat.Random(50, 100);
			}
			if (The.Game.TimeTicks <= NextLeaderAltPathFind)
			{
				return;
			}
			Cell closestPassableCellFor = currentCell2.getClosestPassableCellFor(base.ParentObject);
			if (closestPassableCellFor != null && closestPassableCellFor != currentCell2)
			{
				FindPath findPath2 = new FindPath(currentCell, closestPassableCellFor, PathGlobal: false, PathUnlimited: true, base.ParentObject, 95);
				if (findPath2.Usable)
				{
					NextLeaderPathFind = The.Game.TimeTicks + 2;
					PathTowardLeader(findPath2, num);
				}
				else if (base.ParentObject.IsPlayerLed())
				{
					NextLeaderAltPathFind = The.Game.TimeTicks + 10;
				}
				else
				{
					NextLeaderAltPathFind = The.Game.TimeTicks + Stat.Random(100, 200);
				}
			}
		}
		else
		{
			Think("Waiting for party leader");
			base.ParentObject.UseEnergy(1000);
		}
	}

	private void PathTowardLeader(FindPath PathFinder, int Distance = 1)
	{
		for (int num = PathFinder.Steps.Count - Distance - 1; num >= 1; num--)
		{
			if (PathFinder.Steps[num].IsEmpty())
			{
				if (!TryMovementAbilities(PathFinder.Steps[num]))
				{
					break;
				}
				return;
			}
		}
		for (int num2 = Math.Min(Stat.Random(0, 4), PathFinder.Directions.Count - Distance - 1); num2 >= 0; num2--)
		{
			PushChildGoal(new Step(PathFinder.Directions[num2]));
		}
	}

	public override void TakeAction()
	{
		Cell cell = base.ParentObject?.CurrentCell;
		if (cell == null || The.Player?.CurrentCell == null)
		{
			return;
		}
		if (base.ParentObject.IsAflame() && ++ExtinguishSelfTries < 5)
		{
			PushChildGoal(new ExtinguishSelf());
			return;
		}
		Think("I'm bored.");
		string stringProperty = base.ParentObject.GetStringProperty("WhenBoredReturnToOnce");
		if (!string.IsNullOrEmpty(stringProperty))
		{
			string[] array = stringProperty.Split(',');
			int num = Convert.ToInt32(array[0]);
			int num2 = Convert.ToInt32(array[1]);
			if (cell.X == num && cell.Y == num2)
			{
				base.ParentObject.DeleteStringProperty("WhenBoredReturnToOnce");
			}
			else
			{
				Cell cell2 = cell.ParentZone.GetCell(num, num2);
				ParentBrain.PushGoal(new MoveTo(cell2));
			}
			base.ParentObject.UseEnergy(1000);
		}
		else
		{
			if (!AIBoredEvent.Check(base.ParentObject) || base.ParentObject.Energy.Value < 1000)
			{
				return;
			}
			if (ParentBrain.PartyLeader != null)
			{
				TakeActionWithPartyLeader();
				return;
			}
			if (ParentBrain.Target == null)
			{
				GameObject gameObject = ParentBrain.FindProspectiveTarget(cell);
				if (gameObject != null)
				{
					ParentBrain.WantToKill(gameObject, "out of bored hostility");
					if (ParentBrain.Target != null)
					{
						return;
					}
				}
				else
				{
					Think("I boredly looked for a target, but didn't find one.");
				}
			}
			if (TryMovementAbilities() || TryPassiveAbilities() || TryPassiveItems() || base.ParentObject == null)
			{
				return;
			}
			Cell cell3 = ParentBrain?.StartingCell?.ResolveCell();
			if (cell3 != null && !ParentBrain.Wanders && !ParentBrain.WandersRandomly && cell != cell3 && base.ParentObject.FireEvent("CanAIDoIndependentBehavior"))
			{
				PushChildGoal(new MoveTo(cell3.ParentZone.ZoneID, cell3.X, cell3.Y, careful: true));
			}
			else if (!base.ParentObject.InSameZone(The.Player))
			{
				PushChildGoal(new Wait(Stat.Random(1, 20), "I'm not in the same zone as the player"));
			}
			else if (base.ParentObject.FireEvent("CanAIDoIndependentBehavior"))
			{
				if (ParentBrain.Wanders)
				{
					if ((base.ParentObject.HasTagOrProperty("Restless") && !base.ParentObject.HasTagOrProperty("Social")) || 10.in100())
					{
						PushChildGoal(new Wander());
						return;
					}
				}
				else if (ParentBrain.WandersRandomly && ((base.ParentObject.HasTagOrProperty("Restless") && !base.ParentObject.HasTagOrProperty("Social")) || 20.in100()))
				{
					PushChildGoal(new WanderRandomly(5));
					return;
				}
				if (base.ParentObject.HasTagOrProperty("AllowIdleBehavior") && !base.ParentObject.HasTagOrProperty("PreventIdleBehavior") && cell.ParentZone.WantEvent(PooledEvent<IdleQueryEvent>.ID, MinEvent.CascadeLevel))
				{
					List<GameObject> list = null;
					Zone parentZone = cell.ParentZone;
					int i = 0;
					for (int height = parentZone.Height; i < height; i++)
					{
						int j = 0;
						for (int width = parentZone.Width; j < width; j++)
						{
							Cell cell4 = parentZone.GetCell(j, i);
							int k = 0;
							for (int count = cell4.Objects.Count; k < count; k++)
							{
								GameObject gameObject2 = cell4.Objects[k];
								if (gameObject2.WantEvent(PooledEvent<IdleQueryEvent>.ID, MinEvent.CascadeLevel) || gameObject2.HasRegisteredEvent("IdleQuery"))
								{
									if (list == null)
									{
										list = Event.NewGameObjectList();
									}
									list.Add(gameObject2);
								}
							}
						}
					}
					if (list != null)
					{
						list.ShuffleInPlace();
						IdleQueryEvent e = IdleQueryEvent.FromPool(ParentBrain.ParentObject);
						Event obj = Event.New("IdleQuery", "Object", ParentBrain.ParentObject);
						int l = 0;
						for (int count2 = list.Count; l < count2; l++)
						{
							GameObject gameObject3 = list[l];
							if (!gameObject3.HandleEvent(e))
							{
								base.ParentObject.UseEnergy(1000);
								return;
							}
							if (gameObject3.HasRegisteredEvent(obj.ID) && gameObject3.FireEvent(obj))
							{
								base.ParentObject.UseEnergy(1000);
								return;
							}
						}
					}
				}
				PushChildGoal(new Wait(Stat.Random(1, 10), "I couldn't find anything to do"));
			}
			else
			{
				PushChildGoal(new Wait(Stat.Random(1, 10), "I couldn't perform independent behavior"));
			}
		}
	}
}
