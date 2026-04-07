using System;
using XRL.World.Capabilities;

namespace XRL.World.AI.GoalHandlers;

[Serializable]
public class GiveATreatToPartyLeader : GoalHandler
{
	private string treatTable;

	public GiveATreatToPartyLeader()
	{
	}

	public GiveATreatToPartyLeader(string treatTable)
		: this()
	{
		this.treatTable = treatTable;
	}

	public override bool Finished()
	{
		return false;
	}

	public override void Create()
	{
	}

	public override void TakeAction()
	{
		if (!base.ParentObject.IsPlayerLed())
		{
			Pop();
		}
		else if (base.ParentObject.DistanceTo(The.Player) <= 1)
		{
			GameObject gameObject = GameObject.Create(PopulationManager.RollOneFrom(treatTable).Blueprint);
			if (gameObject != null)
			{
				Messaging.XDidYToZ(base.ParentObject, "give", null, The.Player, "a treat");
				The.Player.ReceiveObject(gameObject);
			}
			Pop();
		}
		else
		{
			ParentBrain.PushGoal(new MoveTo(The.Player, careful: false, overridesCombat: false, 1));
		}
	}
}
