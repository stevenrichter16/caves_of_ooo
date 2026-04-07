namespace XRL.UI;

public class GritGateTerminalScreenBasicAccess : GritGateTerminalScreen
{
	public GritGateTerminalScreenBasicAccess()
	{
		MainText = "Basic user access grant. What do you wish from the Thin World?";
		Options.Add("Who are you?");
		Options.Add("What is the Thin World?");
		Options.Add("Deliver me a bit of knowledge from the Thin World.");
		Options.Add("Exit.");
	}

	public override void Back()
	{
		base.Terminal.CurrentScreen = null;
	}

	public override void Activate()
	{
		if (base.Terminal.Selected == 0)
		{
			base.Terminal.CurrentScreen = new GritGateTerminalScreenWhoAreYou();
		}
		if (base.Terminal.Selected == 1)
		{
			base.Terminal.CurrentScreen = new GritGateTerminalScreenThinWorld();
		}
		if (base.Terminal.Selected == 2)
		{
			base.Terminal.CurrentScreen = new GritGateTerminalScreenKnowledge();
		}
		if (base.Terminal.Selected == 3)
		{
			base.Terminal.CurrentScreen = null;
		}
	}
}
