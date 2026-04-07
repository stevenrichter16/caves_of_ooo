using System;
using System.Collections.Generic;
using XRL.Rules;
using XRL.World.AI.Pathfinding;

namespace XRL.World.AI.GoalHandlers;

[Serializable]
public class Retreat : IMovementGoal
{
	public const int DEFAULT_MAX_WEIGHT = 95;

	public int Duration = -1;

	public Cell Target;

	public int MaxWeight = 95;

	public Retreat()
	{
	}

	public Retreat(int Duration = -1, Cell Target = null, int MaxWeight = 95)
		: this()
	{
		this.Duration = Duration;
		this.Target = Target;
		this.MaxWeight = MaxWeight;
	}

	public override bool Finished()
	{
		return Duration == 0;
	}

	public override bool CanFight()
	{
		return false;
	}

	public bool TryRetreatAbilities()
	{
		return AICommandList.HandleCommandList(AIGetRetreatAbilityListEvent.GetFor(base.ParentObject, null, Target), base.ParentObject, null, Target);
	}

	public GameObject FindWorstVisibleEnemy()
	{
		Cell currentCell = base.ParentObject.CurrentCell;
		GameObject result = null;
		int num = int.MinValue;
		foreach (GameObject item in currentCell.ParentZone.FastFloodVisibility(currentCell.X, currentCell.Y, 30, "Brain", base.ParentObject))
		{
			if (item.IsHostileTowards(base.ParentObject))
			{
				int valueOrDefault = base.ParentObject.Con(item).GetValueOrDefault();
				if (valueOrDefault > num)
				{
					result = item;
					num = valueOrDefault;
				}
			}
		}
		return result;
	}

	public List<GameObject> FindVisibleEnemies()
	{
		List<GameObject> list = Event.NewGameObjectList();
		Cell currentCell = base.ParentObject.CurrentCell;
		foreach (GameObject item in currentCell.ParentZone.FastFloodVisibility(currentCell.X, currentCell.Y, 30, "Brain", base.ParentObject))
		{
			if (item.IsHostileTowards(base.ParentObject))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public List<GameObject> FindVisibleEnemies(out GameObject WorstEnemy)
	{
		List<GameObject> list = Event.NewGameObjectList();
		Cell currentCell = base.ParentObject.CurrentCell;
		WorstEnemy = null;
		int num = int.MinValue;
		foreach (GameObject item in currentCell.ParentZone.FastFloodVisibility(currentCell.X, currentCell.Y, 30, "Brain", base.ParentObject))
		{
			if (item.IsHostileTowards(base.ParentObject))
			{
				list.Add(item);
				int valueOrDefault = base.ParentObject.Con(item).GetValueOrDefault();
				if (valueOrDefault > num)
				{
					WorstEnemy = item;
					num = valueOrDefault;
				}
			}
		}
		return list;
	}

	public override void TakeAction()
	{
		if (Duration > 0)
		{
			Duration--;
		}
		else if (Duration == 0)
		{
			FailToParent();
			return;
		}
		base.ParentObject.FireEvent("AIRetreatStart");
		bool flag = false;
		while (true)
		{
			if (Target != null)
			{
				flag = true;
				if (Target.DistanceTo(base.ParentObject) == 0)
				{
					Think("I'm at my chosen retreat point.");
					if (base.ParentObject.AreHostilesAdjacent())
					{
						Think("There are hostiles here, so I'm going to choose a new retreat point.");
						Target = null;
					}
					else
					{
						bool flag2 = Target.IsHealingLocation(base.ParentObject);
						if (base.ParentObject.AreHostilesNearby() && !flag2)
						{
							Think("There are hostiles nearby and I'm not at a healing location, so I'm going to choose a new retreat point.");
							Target = null;
						}
						else
						{
							if (flag2)
							{
								Think("I'm going to try to use my healing location.");
								int num = base.ParentObject.Stat("Energy");
								Target.UseHealingLocation(base.ParentObject);
								if (base.ParentObject.Stat("Energy") >= num)
								{
									base.ParentObject.UseEnergy(1000);
								}
								break;
							}
							if (Stat.Random(1, 10) != 1)
							{
								base.ParentObject.UseEnergy(1000);
								break;
							}
							Target = null;
						}
					}
				}
				else
				{
					if (!base.ParentObject.IsMobile())
					{
						base.ParentObject.UseEnergy(1000);
						Think("My target is too far and I'm immobile.");
						FailToParent();
						break;
					}
					if (!base.ParentObject.FireEvent(Event.New("AIMovingToRetreat", "Target", Target)))
					{
						base.ParentObject.UseEnergy(1000);
						Think("I'm not allowed to move.");
						break;
					}
					Think("I'm going to move towards my chosen retreat point.");
					Cell currentCell = base.ParentObject.CurrentCell;
					Zone parentZone = currentCell.ParentZone;
					Zone parentZone2 = Target.ParentZone;
					if (parentZone2.IsWorldMap())
					{
						Think("Retreat point is on the world map, can't go there!");
						Target = null;
					}
					else
					{
						if (parentZone.ZoneID != null && parentZone2.ZoneID != null)
						{
							Think("I'm going to try to use abilities suitable for retreating.");
							if (TryRetreatAbilities())
							{
								break;
							}
							Think("I'm going to try to pathfind.");
							FindPath findPath = new FindPath(currentCell, Target, base.ParentObject.IsPlayer(), PathUnlimited: true, base.ParentObject, MaxWeight);
							if (findPath.Usable && findPath.Directions.Count > 0)
							{
								PushChildGoal(new Step(findPath.Directions[0]));
								break;
							}
							Think("I can't find a path.");
							if (!base.ParentObject.FireEvent(Event.New("AIFailRetreatPathfind", "Target", Target)))
							{
								FailToParent();
								break;
							}
							Target = null;
							GameObject gameObject = FindWorstVisibleEnemy();
							if (gameObject != null)
							{
								PushChildGoal(new Flee(gameObject, 2));
							}
							else
							{
								base.ParentObject.UseEnergy(1000);
							}
							break;
						}
						Think("Retreat point is invalid!");
						Target = null;
					}
				}
			}
			Think("I'm going to choose a retreat point.");
			Cell currentCell2 = base.ParentObject.CurrentCell;
			List<Cell> list = currentCell2.ParentZone.FastFloodNeighbors(currentCell2.X, currentCell2.Y, 30);
			GameObject WorstEnemy;
			List<GameObject> enemies = FindVisibleEnemies(out WorstEnemy);
			int num2 = int.MaxValue;
			List<Cell> list2 = null;
			int Nav = 268435456;
			foreach (Cell item in list)
			{
				int? num3 = CellScore(item, WorstEnemy, enemies, ref Nav);
				if (num3.HasValue)
				{
					if (list2 == null)
					{
						list2 = new List<Cell>(16);
					}
					int value = num3.Value;
					if (value < num2)
					{
						num2 = value;
						list2.Clear();
						list2.Add(item);
					}
					else if (value == num2)
					{
						list2.Add(item);
					}
				}
			}
			if (list2 != null)
			{
				Target = list2.GetRandomElement();
				if (flag)
				{
					base.ParentObject.UseEnergy(1000);
					break;
				}
				continue;
			}
			if (WorstEnemy != null)
			{
				Think("I couldn't choose a retreat point so I'm going to flee from my worst enemy.");
				PushChildGoal(new Flee(WorstEnemy, 2));
			}
			else
			{
				Think("I couldn't choose a retreat point or going to flee from my worst enemy so I'm going to wait.");
				base.ParentObject.UseEnergy(1000);
			}
			break;
		}
	}

	private int? CellScore(Cell C, GameObject WorstEnemy, List<GameObject> Enemies, ref int Nav)
	{
		if (!C.IsEmpty())
		{
			return null;
		}
		if (C.HasObjectWithPart("Combat"))
		{
			return null;
		}
		int num = C.NavigationWeight(base.ParentObject, ref Nav);
		if (num >= 98)
		{
			return null;
		}
		if (!base.ParentObject.HasLOSTo(C))
		{
			return null;
		}
		int num2 = num / 5;
		int healingLocationValue = C.GetHealingLocationValue(base.ParentObject);
		if (healingLocationValue > 0)
		{
			num2 -= healingLocationValue;
		}
		if (WorstEnemy != null)
		{
			num2 -= C.DistanceTo(WorstEnemy);
		}
		if (Enemies != null)
		{
			GameObject gameObject = null;
			int num3 = int.MaxValue;
			foreach (GameObject Enemy in Enemies)
			{
				if (Enemy != WorstEnemy)
				{
					int num4 = C.DistanceTo(Enemy);
					if (num4 < num3)
					{
						gameObject = Enemy;
						num3 = num4;
					}
				}
			}
			if (gameObject != null)
			{
				num2 -= num3;
			}
		}
		return num2;
	}
}
