using System.Text;

namespace XRL.World.Parts;

public class RermadonChord : INephalChord
{
	public override string Source => "Rermadon";

	public virtual int ResistBonus => 80;

	public override void Initialize()
	{
		base.StatShifter.SetStatShift("HeatResistance", ResistBonus);
		base.StatShifter.SetStatShift("ColdResistance", ResistBonus);
		base.StatShifter.SetStatShift("ElectricResistance", ResistBonus);
		base.StatShifter.SetStatShift("AcidResistance", ResistBonus);
	}

	public override void Remove()
	{
		base.StatShifter.RemoveStatShifts();
	}

	public override void AppendRules(StringBuilder Postfix)
	{
		Postfix.Append("\n• +").Append(ResistBonus).Append(" all resists");
	}
}
