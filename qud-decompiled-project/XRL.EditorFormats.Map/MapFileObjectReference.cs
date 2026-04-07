namespace XRL.EditorFormats.Map;

public struct MapFileObjectReference
{
	public readonly MapFileRegion region;

	public readonly int x;

	public readonly int y;

	public readonly MapFileCell cell;

	public readonly MapFileObjectBlueprint blueprint;

	public MapFileObjectReference(MapFileRegion region, int x, int y, MapFileCell cell, MapFileObjectBlueprint blueprint)
	{
		this.region = region;
		this.x = x;
		this.y = y;
		this.cell = cell;
		this.blueprint = blueprint;
	}
}
