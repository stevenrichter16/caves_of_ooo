using XRL.UI;

namespace XRL.World.Terminals;

public class ExampleTerminalWelcome : GenericTerminalScreen
{
	public ExampleTerminalWelcome()
	{
		mainText = "<Welcome to this example terminal>";
		Options.Add("Goto 10");
		Options.Add("Exit.");
	}

	public override void Back()
	{
		base.terminal.currentScreen = null;
	}

	public override void Activate()
	{
		if (base.terminal.nSelected == 0)
		{
			base.terminal.currentScreen = new ExampleTerminalWelcome();
		}
		if (base.terminal.nSelected == 1)
		{
			base.terminal.currentScreen = null;
		}
	}
}
