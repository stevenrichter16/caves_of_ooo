using ConsoleLib.Console;
using XRL.Language;

namespace XRL.UI;

public class CyberneticsScreen : TerminalScreen
{
	public override void BeforeRender(ScreenBuffer buffer, ref string footerText)
	{
		footerText = $" Credits: {base.Terminal.Credits}  License Tier: {base.Terminal.Licenses}  Points Used: {base.Terminal.LicensesUsed}";
		if (buffer != null)
		{
			int num = 3;
			buffer.Goto(num + 3, 24);
			buffer.Write(" Credits: " + base.Terminal.Credits + " ");
			buffer.Goto(num + 38, 24);
			buffer.Write(" License Tier: " + base.Terminal.Licenses + " ");
			buffer.Write(" Points Used: " + base.Terminal.LicensesUsed + " ");
			bool hackActive = base.Terminal.HackActive;
			int hackLevel = base.Terminal.HackLevel;
			bool lowLevelHack = base.Terminal.LowLevelHack;
			if (hackActive || hackLevel > 0)
			{
				string text = ((hackActive && lowLevelHack) ? TextFilters.Leet("HACK LEVEL") : "Hack Level");
				buffer.Goto(num + 3, 2);
				buffer.Write(" {{G|" + text + ": " + hackLevel + "}} ");
			}
			int securityAlertLevel = base.Terminal.SecurityAlertLevel;
			if (hackActive || securityAlertLevel > 0)
			{
				string text2 = ((securityAlertLevel <= 0) ? "y" : ((securityAlertLevel >= hackLevel - 1) ? "R" : ((securityAlertLevel > hackLevel - 3) ? "r" : "W")));
				string text3 = ((hackActive && lowLevelHack) ? TextFilters.Leet("SECURITY ALERT LEVEL") : "Security Alert Level");
				buffer.Goto(72 - num - text3.StrippedLength(), 2);
				buffer.Write(" {{" + text2 + "|" + text3 + ": " + securityAlertLevel + "}} ");
			}
		}
	}
}
