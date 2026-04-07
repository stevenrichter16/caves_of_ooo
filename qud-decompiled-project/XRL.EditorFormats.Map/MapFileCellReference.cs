namespace XRL.EditorFormats.Map;

public struct MapFileCellReference
{
	public readonly MapFileRegion region;

	public readonly int x;

	public readonly int y;

	public readonly MapFileCell cell;

	public MapFileCellReference(MapFileRegion region, int x, int y, MapFileCell cell)
	{
		this.region = region;
		this.x = x;
		this.y = y;
		this.cell = cell;
	}
}
