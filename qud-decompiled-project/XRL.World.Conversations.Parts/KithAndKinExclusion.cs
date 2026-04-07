namespace XRL.World.Conversations.Parts;

public class KithAndKinExclusion : IKithAndKinPart
{
	public override bool WantEvent(int ID, int Propagation)
	{
		if (!base.WantEvent(ID, Propagation) && ID != EnteredElementEvent.ID && ID != LeftElementEvent.ID && ID != GetTextElementEvent.ID)
		{
			return ID == PrepareTextEvent.ID;
		}
		return true;
	}

	public override bool HandleEvent(LeftElementEvent E)
	{
		if (!base.Eliminated.Contains(base.Thief))
		{
			base.Eliminated.Add(base.Thief);
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(GetTextElementEvent E)
	{
		if (!base.Eliminated.Contains(base.Thief))
		{
			E.Selected = E.Texts.Find((ConversationText x) => x.ID == base.Circumstance?.ID) ?? E.Selected;
		}
		return base.HandleEvent(E);
	}

	public override bool HandleEvent(PrepareTextEvent E)
	{
		E.Text.Replace("=thief.name=", base.ThiefName);
		return base.HandleEvent(E);
	}
}
