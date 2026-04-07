using System.Collections.Generic;
using XRL.World;
using XRL.World.Anatomy;

namespace XRL.UI;

public class CyberneticsScreenRemove : CyberneticsScreen
{
	private List<GameObject> Cybernetics = new List<GameObject>();

	private List<BodyPart> BodyParts = new List<BodyPart>();

	protected override void OnUpdate()
	{
		ClearOptions();
		Cybernetics.Clear();
		BodyParts.Clear();
		foreach (BodyPart part in base.Terminal.Subject.Body.GetBody().GetParts())
		{
			if (part.Cybernetics != null && !Cybernetics.Contains(part.Cybernetics))
			{
				string text = "";
				if (part.Cybernetics.HasTagOrProperty("CyberneticsNoRemove"))
				{
					text += " {{R|[cannot be uninstalled]}}";
				}
				if (part.Cybernetics.HasTag("CyberneticsDestroyOnRemoval"))
				{
					text += " {{R|[destroyed on uninstall]}}";
				}
				Options.Add(part.Cybernetics.BaseKnownDisplayName + " [" + GetBodyPartNameForTerminal(part) + "]" + text);
				BodyParts.Add(part);
				Cybernetics.Add(part.Cybernetics);
			}
		}
		MainText = "You are given to whimsy, Aristocrat. Choose an implant to uninstall.";
		if (Options.Count == 0)
		{
			MainText += "\n\n{{R|<no implants installed>}}";
		}
		Options.Add("Return To Main Menu");
	}

	private string GetBodyPartNameForTerminal(BodyPart Part)
	{
		return Part.Name.Replace("Worn on ", "");
	}

	public override void Back()
	{
		base.Terminal.CurrentScreen = new CyberneticsScreenMainMenu();
	}

	public override void Activate()
	{
		if (base.Terminal.Selected < Cybernetics.Count)
		{
			if (Cybernetics[base.Terminal.Selected].HasTagOrProperty("CyberneticsNoRemove"))
			{
				base.Terminal.CurrentScreen = new CyberneticsScreenSimpleText("Whimsy must yield to necessity, Aristocrat.", new CyberneticsScreenRemove(), 1);
			}
			else if (Cybernetics[base.Terminal.Selected].HasTag("CyberneticsDestroyOnRemoval"))
			{
				Cybernetics[base.Terminal.Selected].Unimplant();
				Cybernetics[base.Terminal.Selected].Destroy();
				CyberneticsRemoveResult cyberneticsRemoveResult = new CyberneticsRemoveResult(Cybernetics[base.Terminal.Selected]);
				cyberneticsRemoveResult.Destroyed = true;
				base.Terminal.CurrentScreen = cyberneticsRemoveResult;
			}
			else
			{
				Cybernetics[base.Terminal.Selected].Unimplant(MoveToInventory: true);
				base.Terminal.CurrentScreen = new CyberneticsRemoveResult(Cybernetics[base.Terminal.Selected]);
			}
		}
		else
		{
			base.Terminal.CheckSecurity(1, new CyberneticsScreenMainMenu());
		}
	}
}
