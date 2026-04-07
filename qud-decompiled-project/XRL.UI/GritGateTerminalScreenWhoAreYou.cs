namespace XRL.UI;

public class GritGateTerminalScreenWhoAreYou : GritGateTerminalScreen
{
	public GritGateTerminalScreenWhoAreYou()
	{
		MainText = "I am Ereshkigal, your liaison with the Thin World. Ask of me.";
		Options.Add("Are you sapient?");
		Options.Add("What is the Thin World?");
		Options.Add("Deliver me a bit of knowledge from the Thin World.");
		Options.Add("Exit.");
	}

	public override void Back()
	{
		base.Terminal.CurrentScreen = new GritGateTerminalScreenBasicAccess();
	}

	public override void Activate()
	{
		if (base.Terminal.Selected == 0)
		{
			base.Terminal.CurrentScreen = new GritGateTerminalScreenSapient();
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
