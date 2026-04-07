namespace XRL.World.AI;

public struct PartyMember
{
	public GameObjectReference Reference;

	public int Flags;

	public PartyMember(GameObjectReference Reference, int Flags)
	{
		this.Reference = Reference;
		this.Flags = Flags;
	}
}
