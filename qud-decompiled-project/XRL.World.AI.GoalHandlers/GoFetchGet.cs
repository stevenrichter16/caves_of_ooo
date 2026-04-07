using System;

namespace XRL.World.AI.GoalHandlers;

[Serializable]
public class GoFetchGet : GoalHandler
{
	public override bool Finished()
	{
		return false;
	}

	public override void Create()
	{
	}

	public override void TakeAction()
	{
		Pop();
		if (base.ParentObject.CurrentCell != null)
		{
			GameObject randomElement = base.ParentObject.CurrentCell.GetObjects((GameObject o) => o.ShouldAutoget()).GetRandomElement();
			if (randomElement != null)
			{
				ParentBrain.ParentObject.ReceiveObject(randomElement);
			}
		}
	}
}
