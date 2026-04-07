namespace XRL.UI;

public class GritGateTerminalScreenThinWorld : GritGateTerminalScreen
{
	public GritGateTerminalScreenThinWorld()
	{
		MainText = "The Thin World is where I traverse, as the Thick World is where you do. I've no domain in your world, and but for me, you've none in mine.";
		Options.Add("Who are you?");
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
			base.Terminal.CurrentScreen = new GritGateTerminalScreenWhoAreYou();
		}
		if (base.Terminal.Selected == 1)
		{
			base.Terminal.CurrentScreen = new GritGateTerminalScreenKnowledge();
		}
		if (base.Terminal.Selected == 2)
		{
			base.Terminal.CurrentScreen = null;
		}
	}
}
