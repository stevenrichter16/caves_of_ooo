using System;
using ConsoleLib.Console;
using XRL.Core;

namespace XRL.World.Parts;

[Serializable]
public class GritGatePowerDisplay : IPart
{
	public int Radius = 3;

	public override bool Render(RenderEvent E)
	{
		if (ParentObject.CurrentCell != null)
		{
			E.WantsToPaint = true;
		}
		return true;
	}

	public override void OnPaint(ScreenBuffer Buffer)
	{
		if (Globals.RenderMode == RenderModeType.Text)
		{
			Buffer.WriteAt(ParentObject.CurrentCell, GritGateAmperageImposter.display);
		}
	}
}
