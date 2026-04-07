using System;
using ConsoleLib.Console;
using XRL.Core;
using XRL.World.Parts;

namespace XRL.World.Effects;

[Serializable]
public class IrisdualMolting : IEffectWithPrefabImposter
{
	[NonSerialized]
	private int LastColorIndex;

	[NonSerialized]
	private int NextFrame;

	[NonSerialized]
	private string SoundID;

	public IrisdualMolting()
	{
		DisplayName = "{{rainbow|irisdual molting}}";
		PrefabID = "Prefabs/Particles/IrisdualMolting";
	}

	public IrisdualMolting(int Duration)
		: this()
	{
		base.Duration = Duration;
	}

	public override string GetDetails()
	{
		return "Molting beams of prismatic light.";
	}

	public override bool UseStandardDurationCountdown()
	{
		return true;
	}

	public override int GetEffectType()
	{
		return 128;
	}

	public override bool Apply(GameObject Object)
	{
		if (Object.TryGetPart<ReflectProjectiles>(out var Part))
		{
			Part.Deactivated = true;
			Part.Check();
		}
		return true;
	}

	public override void Remove(GameObject Object)
	{
		if (Object.TryGetPart<ReflectProjectiles>(out var Part))
		{
			Part.Deactivated = false;
		}
	}

	public override bool WantEvent(int ID, int cascade)
	{
		if (!base.WantEvent(ID, cascade))
		{
			return ID == BeforeRenderEvent.ID;
		}
		return true;
	}

	public override bool HandleEvent(BeforeRenderEvent E)
	{
		Cell cell = base.Object.CurrentCell;
		cell.ParentZone.AddLight(cell.X, cell.Y, 1);
		cell.ParentZone.AddVisibility(cell.X, cell.Y, 1);
		return base.HandleEvent(E);
	}

	public override bool Render(RenderEvent E)
	{
		if (ImposterActive)
		{
			if (XRLCore.CurrentFrame >= NextFrame)
			{
				NextFrame = XRLCore.CurrentFrame + 4;
				if (NextFrame >= 62)
				{
					NextFrame -= 62;
				}
				LastColorIndex++;
				if (LastColorIndex >= Crayons.BrightColorStrings.Length)
				{
					LastColorIndex = 0;
				}
			}
			E.ColorString = Crayons.BrightColorStrings[LastColorIndex];
		}
		return base.Render(E);
	}

	public override bool RenderSound(ConsoleChar C)
	{
		if (SoundID == null)
		{
			SoundID = "IrisdualMolting" + "." + Guid.NewGuid().ToString();
		}
		C?.soundExtra.Add(SoundID, "Sounds/Creatures/Ability/sfx_creature_girshNephilim_irisdualBeam_molting_lp", 1f, 1f, 0.5f, 20);
		return true;
	}
}
