using System;

namespace XRL.World.Effects;

[Serializable]
public class Frozen : Effect
{
	public Frozen()
	{
		DisplayName = "{{freezing|frozen}}";
	}

	public Frozen(int Duration)
		: this()
	{
		base.Duration = Duration;
	}

	public override int GetEffectType()
	{
		return 33555456;
	}

	public override string GetDetails()
	{
		return "Can't take physical actions.";
	}

	public override bool CanApplyToStack()
	{
		return true;
	}

	public override bool Apply(GameObject Object)
	{
		Object.MovementModeChanged("Frozen", Involuntary: true);
		return true;
	}
}
