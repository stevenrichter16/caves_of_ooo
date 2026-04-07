using System;
using System.Collections.Generic;
using ConsoleLib.Console;
using Wintellect.PowerCollections;
using XRL.Core;
using XRL.UI;

namespace XRL.World.AI.Pathfinding;

public class FindPath
{
	public const int MAX_CELL_NAV = 4000;

	public static CellNavigationValue[] CellNavs = new CellNavigationValue[4000];

	public static int CellNavCount = -1;

	[NonSerialized]
	private static List<string> ZoneFilter = new List<string>();

	[NonSerialized]
	private static Dictionary<Cell, CellNavigationValue> OpenList = new Dictionary<Cell, CellNavigationValue>();

	[NonSerialized]
	private static Dictionary<Cell, CellNavigationValue> CloseList = new Dictionary<Cell, CellNavigationValue>();

	[NonSerialized]
	private static OrderedBag<CellNavigationValue> OrderedNavigationValues = new OrderedBag<CellNavigationValue>();

	public static readonly string[] DirectionList = new string[8] { "NW", "N", "NE", "E", "SE", "S", "SW", "W" };

	public static readonly string[] DirectionListSorted = new string[8] { "N", "E", "S", "W", "NW", "NE", "SE", "SW" };

	public static readonly string[] DirectionListU = new string[9] { "U", "NW", "N", "NE", "E", "SE", "S", "SW", "W" };

	public static readonly string[] DirectionListUSorted = new string[9] { "U", "N", "E", "S", "W", "NW", "NE", "SE", "SW" };

	public static readonly string[] DirectionListD = new string[9] { "D", "NW", "N", "NE", "E", "SE", "S", "SW", "W" };

	public static readonly string[] DirectionListDSorted = new string[9] { "D", "N", "E", "S", "W", "NW", "NE", "SE", "SW" };

	public static readonly string[] DirectionListCardinalOnly = new string[4] { "E", "S", "N", "W" };

	public bool Found;

	public List<string> Directions = new List<string>();

	public List<Cell> Steps = new List<Cell>();

	public bool Usable
	{
		get
		{
			if (Found && Steps != null)
			{
				return Steps.Count > 1;
			}
			return false;
		}
	}

	public static void Initalize()
	{
		for (int i = 0; i < 4000; i++)
		{
			CellNavs[i] = new CellNavigationValue(0.0, 0.0, null, null, null);
		}
	}

	public CellNavigationValue NewCellNav(double Cost = 0.0, double Estimate = 0.0, Cell C = null, Cell Parent = null, string Direction = null)
	{
		if (CellNavCount >= 3999)
		{
			return null;
		}
		CellNavCount++;
		return CellNavs[CellNavCount].Set(Cost, Estimate, C, Parent, Direction);
	}

	public FindPath()
	{
	}

	public FindPath(string StartZoneID, int X1, int Y1, string EndZoneID, int X2, int Y2, bool PathGlobal = false, bool PathUnlimited = false, GameObject Looker = null, bool Juggernaut = false, bool IgnoreCreatures = false, bool IgnoreGases = false, bool FlexPhase = false, int MaxWeight = 100)
	{
		if (StartZoneID != null && EndZoneID != null)
		{
			Zone zone = The.ZoneManager.GetZone(StartZoneID);
			Zone zone2 = The.ZoneManager.GetZone(EndZoneID);
			bool juggernaut = Juggernaut;
			bool ignoreCreatures = IgnoreCreatures;
			bool ignoreGases = IgnoreGases;
			bool flexPhase = FlexPhase;
			PerformPathfind(zone, X1, Y1, zone2, X2, Y2, PathGlobal, Looker, PathUnlimited, AddNoise: false, CardinalDirectionsOnly: false, null, MaxWeight, ExploredOnly: false, juggernaut, ignoreCreatures, ignoreGases, flexPhase);
		}
	}

	public FindPath(Zone StartZone, int X1, int Y1, Zone EndZone, int X2, int Y2, bool PathGlobal, bool PathUnlimited, GameObject Looker, bool AddNoise)
	{
		if (StartZone != null && EndZone != null)
		{
			PerformPathfind(StartZone, X1, Y1, EndZone, X2, Y2, PathGlobal, Looker, PathUnlimited, AddNoise);
		}
	}

	public FindPath(Zone StartZone, int X1, int Y1, Zone EndZone, int X2, int Y2, bool PathGlobal = false, bool PathUnlimited = false, GameObject Looker = null, bool AddNoise = false, bool CardinalOnly = false, bool Juggernaut = false, bool IgnoreCreatures = false, bool IgnoreGases = false, bool FlexPhase = false)
	{
		if (StartZone != null && EndZone != null)
		{
			PerformPathfind(StartZone, X1, Y1, EndZone, X2, Y2, PathGlobal, Looker, PathUnlimited, AddNoise, CardinalOnly, null, 100, ExploredOnly: false, Juggernaut, IgnoreCreatures, IgnoreGases, FlexPhase);
		}
	}

	public FindPath(Zone StartZone, int X1, int Y1, Zone EndZone, int X2, int Y2, bool PathGlobal = false, bool PathUnlimited = false, bool Juggernaut = false, bool IgnoreCreatures = false, bool IgnoreGases = false, bool FlexPhase = false, GameObject Looker = null, int MaxWeight = 100)
	{
		if (StartZone != null && EndZone != null)
		{
			bool juggernaut = Juggernaut;
			bool ignoreCreatures = IgnoreCreatures;
			bool ignoreGases = IgnoreGases;
			bool flexPhase = FlexPhase;
			PerformPathfind(StartZone, X1, Y1, EndZone, X2, Y2, PathGlobal, Looker, PathUnlimited, AddNoise: false, CardinalDirectionsOnly: false, null, MaxWeight, ExploredOnly: false, juggernaut, ignoreCreatures, ignoreGases, flexPhase);
		}
	}

	public FindPath(Cell StartCell, Cell EndCell, bool PathGlobal = false, bool PathUnlimited = true, GameObject Looker = null, int MaxWeight = 100, bool ExploredOnly = false, bool Juggernaut = false, bool IgnoreCreatures = false, bool IgnoreGases = false, bool FlexPhase = false, CleanQueue<XRLCore.SortPoint> Avoid = null)
	{
		if (StartCell != null && EndCell != null)
		{
			Zone parentZone = StartCell.ParentZone;
			Zone parentZone2 = EndCell.ParentZone;
			if (parentZone != null && parentZone2 != null)
			{
				PerformPathfind(parentZone, StartCell.X, StartCell.Y, parentZone2, EndCell.X, EndCell.Y, PathGlobal, Looker, Unlimited: true, AddNoise: false, CardinalDirectionsOnly: false, Avoid, MaxWeight, ExploredOnly, Juggernaut, IgnoreCreatures, IgnoreGases, FlexPhase);
			}
		}
	}

	public FindPath(Zone StartZone, int X1, int Y1, Zone EndZone, int X2, int Y2, bool PathGlobal, bool PathUnlimited, GameObject Looker, CleanQueue<XRLCore.SortPoint> Avoid, bool ExploredOnly = false, bool IgnoreCreatures = false, bool IgnoreGases = false, bool FlexPhase = false)
	{
		if (StartZone != null && EndZone != null)
		{
			PerformPathfind(StartZone, X1, Y1, EndZone, X2, Y2, PathGlobal, Looker, PathUnlimited, AddNoise: false, CardinalDirectionsOnly: false, Avoid, 100, ExploredOnly, Juggernaut: false, IgnoreCreatures, IgnoreGases, FlexPhase);
		}
	}

	public void PerformPathfind(Zone StartZone, int X1, int Y1, Zone EndZone, int X2, int Y2, bool PathGlobal = false, GameObject Looker = null, bool Unlimited = false, bool AddNoise = false, bool CardinalDirectionsOnly = false, CleanQueue<XRLCore.SortPoint> Avoid = null, int MaxWeight = 100, bool ExploredOnly = false, bool Juggernaut = false, bool IgnoreCreatures = false, bool IgnoreGases = false, bool FlexPhase = false)
	{
		if (StartZone == null || EndZone == null || (StartZone == EndZone && X1 == X2 && Y1 == Y2))
		{
			return;
		}
		string directionFromZone = EndZone.GetDirectionFromZone(StartZone);
		CellNavCount = 0;
		bool drawPathfinder = Options.DrawPathfinder;
		Cell cell = StartZone.GetCell(X1, Y1);
		Cell cell2 = EndZone.GetCell(X2, Y2);
		StartZone.CalculateNavigationMap(Looker, AddNoise, ExploredOnly, Juggernaut, IgnoreCreatures, IgnoreGases, FlexPhase);
		if (StartZone != EndZone)
		{
			EndZone.CalculateNavigationMap(Looker, AddNoise, ExploredOnly, Juggernaut, IgnoreCreatures, IgnoreGases, FlexPhase);
		}
		List<Zone> list = null;
		OpenList.Clear();
		CloseList.Clear();
		OrderedNavigationValues.Clear();
		OpenList.Add(StartZone.GetCell(X1, Y1), NewCellNav(0.0, 0.0, null, null, "."));
		try
		{
			ScreenBuffer screenBuffer = Popup._ScreenBuffer;
			ZoneFilter.Clear();
			ZoneFilter.Add(StartZone.ZoneID);
			ZoneFilter.Add(EndZone.ZoneID);
			int num = (Unlimited ? 9999 : Math.Max(80, cell.ManhattanDistanceTo(cell2) * 5 / 4));
			if (num > 1 && !cell.IsAdjacentTo(cell2))
			{
				if (StartZone.NavigationMap[X1, Y1].Weight <= MaxWeight && !cell.AnyLocalAdjacentCell((Cell C) => C.ParentZone.NavigationMap[C.X, C.Y].Weight <= MaxWeight))
				{
					Found = false;
					return;
				}
				if (!cell2.AnyLocalAdjacentCell((Cell C) => C.ParentZone.NavigationMap[C.X, C.Y].Weight <= MaxWeight))
				{
					Found = false;
					return;
				}
			}
			int num2 = 0;
			while (OpenList.Count > 0)
			{
				Cell cell3 = null;
				double num3 = double.MaxValue;
				foreach (KeyValuePair<Cell, CellNavigationValue> open in OpenList)
				{
					if (open.Value.Total < num3)
					{
						num3 = open.Value.Total;
						cell3 = open.Key;
					}
				}
				if (drawPathfinder)
				{
					screenBuffer.WriteAt(cell3, "&Y!").Draw();
				}
				CellNavigationValue cellNavigationValue = OpenList[cell3];
				if (drawPathfinder)
				{
					screenBuffer.WriteAt(cell, "&GS").WriteAt(cell2, "&RE").Draw();
				}
				string[] array = DirectionListSorted;
				if (CardinalDirectionsOnly)
				{
					array = DirectionListCardinalOnly;
				}
				else if (PathGlobal && StartZone != EndZone)
				{
					if (cell3.HasObjectWithPart("StairsUp"))
					{
						array = DirectionListUSorted;
					}
					else if (cell3.HasObjectWithPart("StairsDown"))
					{
						array = DirectionListDSorted;
					}
				}
				int num4 = 0;
				for (int num5 = array.Length; num4 < num5; num4++)
				{
					string text = array[num4];
					num2++;
					if (num2 > 2000 && !Unlimited)
					{
						Found = false;
						return;
					}
					Cell cell4 = null;
					cell4 = cell3.GetCellFromDirectionWithOneValidExternalDirection(text, directionFromZone, EndZone.ZoneID);
					NavigationWeight[,] navigationMap = StartZone.NavigationMap;
					if (cell4 != null && cell4.ParentZone != StartZone)
					{
						if (cell4.ParentZone == cell2.ParentZone)
						{
							navigationMap = EndZone.NavigationMap;
						}
						else
						{
							if (list == null)
							{
								list = new List<Zone>();
							}
							if (!list.Contains(cell4.ParentZone))
							{
								cell4.ParentZone.CalculateNavigationMap(Looker, AddNoise, ExploredOnly, Juggernaut, IgnoreCreatures, IgnoreGases, FlexPhase);
							}
							navigationMap = cell4.ParentZone.NavigationMap;
						}
					}
					if (cell4 != cell2 && (cell4 == null || CloseList.ContainsKey(cell4) || navigationMap[cell4.X, cell4.Y].Weight > MaxWeight))
					{
						continue;
					}
					if (drawPathfinder)
					{
						screenBuffer.WriteAt(cell4, "&K?").Draw();
					}
					double num6 = cell2.RealDistanceTo(cell4, indefiniteWorld: false);
					if (CellNavCount >= 4000)
					{
						Found = false;
						return;
					}
					if (OpenList.TryGetValue(cell4, out var value))
					{
						double num7 = cellNavigationValue.Cost + (double)navigationMap[cell4.X, cell4.Y].Weight;
						if (Avoid != null)
						{
							for (int num8 = 0; num8 < Avoid.Items.Count; num8++)
							{
								XRLCore.SortPoint sortPoint = Avoid.Items[num8];
								if (sortPoint.X == cell4.X && sortPoint.Y == cell4.Y)
								{
									num7 = 99999.0;
								}
							}
						}
						if (text.Length > 1)
						{
							num7 += 0.001;
						}
						double num9 = num6;
						if (cell4.ParentZone.Z != cell2.ParentZone.Z)
						{
							num9 *= 10.0;
						}
						if (text != cellNavigationValue.Direction)
						{
							num9 += 60.0;
						}
						if (num7 + num9 < value.Total)
						{
							value.Set(num7, num9, null, cell3, text);
						}
					}
					else if (CloseList.TryGetValue(cell4, out value))
					{
						double num10 = cellNavigationValue.Cost + (double)navigationMap[cell4.X, cell4.Y].Weight;
						if (Avoid != null)
						{
							for (int num11 = 0; num11 < Avoid.Items.Count; num11++)
							{
								XRLCore.SortPoint sortPoint2 = Avoid.Items[num11];
								if (sortPoint2.X == cell4.X && sortPoint2.Y == cell4.Y)
								{
									num10 = 99999.0;
								}
							}
						}
						if (text.Length > 1)
						{
							num10 += 0.001;
						}
						double num12 = num6;
						if (cell4.ParentZone.Z != cell2.ParentZone.Z)
						{
							num12 *= 10.0;
						}
						if (num10 + num12 < value.Total)
						{
							value.Set(num10, num12, null, cell3, text);
							OpenList.Add(cell4, value);
							CloseList.Remove(cell4);
						}
					}
					else if (num6 <= (double)num)
					{
						double num13 = cellNavigationValue.Cost + (double)navigationMap[cell4.X, cell4.Y].Weight;
						if (Avoid != null)
						{
							for (int num14 = 0; num14 < Avoid.Items.Count; num14++)
							{
								XRLCore.SortPoint sortPoint3 = Avoid.Items[num14];
								if (sortPoint3.X == cell4.X && sortPoint3.Y == cell4.Y)
								{
									num13 = 99999.0;
								}
							}
						}
						if (text.Length > 1)
						{
							num13 += 0.001;
						}
						double num15 = num6;
						if (cell4.ParentZone.Z != cell2.ParentZone.Z)
						{
							num15 *= 10.0;
						}
						OpenList.Add(cell4, NewCellNav(num13, num15, null, cell3, text));
					}
					if (cell4 != cell2)
					{
						continue;
					}
					Found = true;
					if (drawPathfinder)
					{
						screenBuffer.WriteAt(cell, "S").WriteAt(cell2, "E").Draw();
					}
					if (text != ".")
					{
						Steps.Add(cell4);
						Directions.Add(text);
					}
					Cell cell5 = cell3;
					while (cell5 != null)
					{
						Steps.Add(cell5);
						if (drawPathfinder)
						{
							screenBuffer.WriteAt(cell5, ".").Draw();
						}
						if (OpenList.ContainsKey(cell5))
						{
							if (OpenList[cell5].Direction != ".")
							{
								Directions.Add(OpenList[cell5].Direction);
							}
							cell5 = OpenList[cell5].Parent;
						}
						else
						{
							if (CloseList[cell5].Direction != ".")
							{
								Directions.Add(CloseList[cell5].Direction);
							}
							cell5 = CloseList[cell5].Parent;
						}
					}
					Directions.Reverse();
					Steps.Reverse();
					if (drawPathfinder && Options.DrawPathfinderHalt)
					{
						screenBuffer.Draw();
						Keyboard.getch();
					}
					return;
				}
				if (!CloseList.ContainsKey(cell3))
				{
					CloseList.Add(cell3, cellNavigationValue);
					OpenList.Remove(cell3);
				}
			}
			Found = false;
			if (drawPathfinder && Options.DrawPathfinderHalt)
			{
				screenBuffer.Draw();
				Keyboard.getch();
			}
		}
		finally
		{
			OpenList.Clear();
			CloseList.Clear();
			OrderedNavigationValues.Clear();
			for (int num16 = 0; num16 <= CellNavCount; num16++)
			{
				CellNavs[num16].Parent = null;
				CellNavs[num16].C = null;
			}
		}
	}
}
