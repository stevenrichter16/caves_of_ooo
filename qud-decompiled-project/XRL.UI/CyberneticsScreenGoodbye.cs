using XRL.Language;

namespace XRL.UI;

public class CyberneticsScreenGoodbye : CyberneticsScreen
{
	public CyberneticsScreenGoodbye(bool AllowHack = true)
	{
		MainText = "You are no aristocrat. Goodbye.";
		Options.Add("...");
		if (AllowHack && XRL.UI.Options.SifrahHacking)
		{
			HackOption = Options.Count;
			Options.Add(TextFilters.Leet("attempt hack"));
		}
	}

	public override void Activate()
	{
		base.Terminal.CurrentScreen = null;
		if (base.Terminal.Selected == HackOption && base.Terminal.AttemptHack())
		{
			base.Terminal.CurrentScreen = new CyberneticsScreenMainMenu();
		}
	}
}
