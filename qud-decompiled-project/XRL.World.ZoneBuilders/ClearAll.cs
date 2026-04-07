namespace XRL.World.ZoneBuilders;

public class ClearAll : ZoneBuilderSandbox
{
	public bool IncludeCombat = true;

	public bool BuildZone(Zone Z)
	{
		Z.ForeachCell(delegate(Cell c)
		{
			c.Clear(null, Important: false, IncludeCombat);
		});
		return true;
	}
}
