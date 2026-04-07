using System.Collections.Generic;
using XRL.World;

namespace XRL.UI;

public class CyberneticsScreenSelectSubject : CyberneticsScreen
{
	private static List<GameObject> Subjects = new List<GameObject>();

	protected override void OnUpdate()
	{
		MainText = "Select who is to Become, aristocrat.";
		Subjects.Clear();
		base.Terminal.GetAuthorizedSubjects(Subjects);
		ClearOptions();
		foreach (GameObject subject in Subjects)
		{
			Options.Add(subject.GetReferenceDisplayName());
		}
		Options.Add("Return to main menu");
	}

	public override void Back()
	{
		base.Terminal.CurrentScreen = new CyberneticsScreenMainMenu();
	}

	public override void Activate()
	{
		if (base.Terminal.Selected < Subjects.Count)
		{
			base.Terminal.Subject = Subjects[base.Terminal.Selected];
		}
		base.Terminal.CheckSecurity(20, new CyberneticsScreenMainMenu());
	}
}
