using System;

namespace XRL.World.Parts.Skill;

[Serializable]
public class Endurance_Weathered : BaseSkill
{
	public override bool AddSkill(GameObject GO)
	{
		base.StatShifter.SetStatShift("HeatResistance", 15);
		base.StatShifter.SetStatShift("ColdResistance", 15);
		return base.AddSkill(GO);
	}

	public override bool RemoveSkill(GameObject GO)
	{
		base.StatShifter.RemoveStatShifts();
		return base.RemoveSkill(GO);
	}
}
