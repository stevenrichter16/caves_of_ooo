namespace XRL.World;

public struct OrderedBuilderBlueprint
{
	public ZoneBuilderBlueprint Blueprint;

	public int Priority;

	public OrderedBuilderBlueprint(ZoneBuilderBlueprint Blueprint, int Priority)
	{
		this.Blueprint = Blueprint;
		this.Priority = Priority;
	}
}
