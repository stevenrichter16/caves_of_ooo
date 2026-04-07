using System.Collections.Generic;
using XRL.Rules;

namespace XRL.World.ZoneBuilders;

public class CryptOfPriests : ZoneBuilderSandbox
{
	public static HashSet<string> cryptVertexList;

	public int lastseed;

	public static void Reset()
	{
		cryptVertexList = null;
	}

	public bool BuildZone(Zone Z)
	{
		Stat.ReseedFrom(Z.ZoneID);
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				int num = i + Z.X * 16;
				int num2 = j + Z.Y * 5;
				bool num3 = cryptVertexList.Contains(num + "_" + num2);
				Box box = new Box(i * 5, j * 5, i * 5 + 4, j * 5 + 4);
				if (num == 4 && num2 == 12)
				{
					Z.GetCell(box.x1 + 2, box.y1 + 2).AddObject("StairsUp");
				}
				if (num == 43 && num2 == 12)
				{
					Z.GetCell(box.x1 + 2, box.y1 + 2).AddObject("StairsDown");
				}
				if (num3)
				{
					Z.FillHollowBox(box, "TombWarriorCryptWall");
				}
			}
		}
		ZoneBuilderSandbox.EnsureAllVoidsConnected(Z);
		Z.ForeachCell(delegate(Cell c)
		{
			c.SetReachable(State: true);
		});
		Z.GetCell(0, 0).AddObject("Finish_TombOfTheEaters_EnterTheTombOfTheEaters");
		new ChildrenOfTheTomb().BuildZone(Z);
		return true;
	}
}
