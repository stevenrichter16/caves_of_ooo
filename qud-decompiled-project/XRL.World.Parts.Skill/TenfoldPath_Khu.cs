using System;

namespace XRL.World.Parts.Skill;

/// This part is not used in the base game.
[Serializable]
public class TenfoldPath_Khu : BaseInitiatorySkill
{
	public override bool AddSkill(GameObject Object)
	{
		base.StatShifter.SetStatShift("MA", 2);
		return base.AddSkill(Object);
	}

	public override bool RemoveSkill(GameObject Object)
	{
		base.StatShifter.RemoveStatShifts();
		return base.RemoveSkill(Object);
	}
}
