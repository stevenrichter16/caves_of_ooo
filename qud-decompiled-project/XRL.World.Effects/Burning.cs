using System;

namespace XRL.World.Effects;

[Serializable]
public class Burning : Effect
{
	public Burning()
	{
		DisplayName = "{{R|burning}}";
	}

	public Burning(int Duration)
		: this()
	{
		base.Duration = Duration;
	}

	public override int GetEffectType()
	{
		return 33554944;
	}

	public override string GetStateDescription()
	{
		return "{{R|on fire}}";
	}

	public override bool WantEvent(int ID, int cascade)
	{
		if (!base.WantEvent(ID, cascade))
		{
			return ID == PooledEvent<GetCompanionStatusEvent>.ID;
		}
		return true;
	}

	public override bool HandleEvent(GetCompanionStatusEvent E)
	{
		if (E.Object == base.Object)
		{
			E.AddStatus("on fire", 50);
		}
		return base.HandleEvent(E);
	}

	public static string GetBurningAmount(GameObject go)
	{
		if (go == null)
		{
			return "1";
		}
		int num = go.Physics.Temperature - go.Physics.FlameTemperature;
		if (num < 0)
		{
			return "1";
		}
		if (num <= 100)
		{
			return "1";
		}
		if (num <= 300)
		{
			return "1-2";
		}
		if (num <= 500)
		{
			return "2-3";
		}
		if (num <= 700)
		{
			return "3-4";
		}
		if (num <= 900)
		{
			return "4-5";
		}
		return "5-6";
	}

	public override string GetDetails()
	{
		return GetBurningAmount(base.Object) + " damage per turn.";
	}
}
