using System.Text;
using XRL.World.Effects;

namespace XRL.World.Parts;

public class ShugruithChord : INephalChord
{
	public override string Source => "Shugruith";

	public virtual int AVBonus => 10;

	public virtual int DVBOnus => 10;

	public override void Initialize()
	{
		base.StatShifter.SetStatShift("AV", AVBonus);
		base.StatShifter.SetStatShift("DV", DVBOnus);
		ParentObject.ApplyEffect(new Omniphase("ShugruithChord"));
	}

	public override void Remove()
	{
		base.StatShifter.RemoveStatShifts();
		ParentObject.RemoveEffect((Effect x) => x is Omniphase omniphase && omniphase.SourceKey == "ShugruithChord");
	}

	public override void AppendRules(StringBuilder Postfix)
	{
		Postfix.Append("\n• ");
		Statistic.AppendStatAdjustDescription(Postfix, "AV", AVBonus);
		Postfix.Append("\n• ");
		Statistic.AppendStatAdjustDescription(Postfix, "DV", DVBOnus);
		Postfix.Append("\n• Omniphase");
	}
}
