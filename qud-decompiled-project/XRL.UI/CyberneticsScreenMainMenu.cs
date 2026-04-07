using XRL.Language;

namespace XRL.UI;

public class CyberneticsScreenMainMenu : CyberneticsScreen
{
	private int SelectOption = -1;

	public CyberneticsScreenMainMenu()
	{
		MainText = "Welcome, Aristocrat, to a becoming nook. " + base.Terminal.Subject.Does("are", int.MaxValue, null, null, null, AsIfKnown: false, Single: false, NoConfusion: false, NoColor: false, Stripped: false, WithoutTitles: true, Short: true, BaseOnly: false, WithIndefiniteArticle: false, null, IndicateHidden: false, Pronoun: false, SecondPerson: true, null, AsPossessed: true, null, Reference: true) + " one step closer to the Grand Unification. Please choose from the following options.";
		Options.Add("Learn About Cybernetics");
		Options.Add("Install Cybernetics");
		Options.Add("Uninstall Cybernetics");
		Options.Add("Upgrade Your License");
		if (!XRL.UI.Options.SifrahHacking)
		{
			return;
		}
		SelectOption = -1;
		HackOption = -1;
		int possibleSubjectCount = base.Terminal.GetPossibleSubjectCount();
		if (possibleSubjectCount > 1)
		{
			int authorizedSubjectCount = base.Terminal.GetAuthorizedSubjectCount();
			if (authorizedSubjectCount > 1)
			{
				SelectOption = Options.Count;
				Options.Add("Select Subject");
			}
			if (!base.Terminal.HackActive && possibleSubjectCount > authorizedSubjectCount)
			{
				HackOption = Options.Count;
				Options.Add(TextFilters.Leet("Attempt Hack to Select Subject").Color("R"));
			}
		}
	}

	public override void Activate()
	{
		switch (base.Terminal.Selected)
		{
		case 0:
			base.Terminal.CurrentScreen = new CyberneticsScreenLearn();
			return;
		case 1:
			base.Terminal.CurrentScreen = new CyberneticsScreenInstall();
			return;
		case 2:
			base.Terminal.CurrentScreen = new CyberneticsScreenRemove();
			return;
		case 3:
			base.Terminal.CurrentScreen = new CyberneticsScreenUpgrade();
			return;
		}
		if (base.Terminal.Selected == SelectOption)
		{
			base.Terminal.CurrentScreen = new CyberneticsScreenSelectSubject();
		}
		else if (base.Terminal.Selected == HackOption)
		{
			if (base.Terminal.HackActive || base.Terminal.AttemptHack())
			{
				base.Terminal.CurrentScreen = new CyberneticsScreenSelectSubject();
			}
			else
			{
				base.Terminal.CurrentScreen = new CyberneticsScreenGoodbye(AllowHack: false);
			}
		}
	}
}
