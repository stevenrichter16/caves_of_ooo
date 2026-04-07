using System.Collections.Generic;
using ConsoleLib.Console;
using UnityEngine;
using XRL.Rules;
using XRL.UI;
using XRL.UI.Framework;

namespace XRL.CharacterBuilds.Qud.UI;

[UIView("CharacterCreation:Chartype", false, false, false, null, null, false, 0, false, NavCategory = "Chargen", UICanvas = "Chargen/Chartype", UICanvasHost = 1)]
public class QudChartypeModuleWindow : EmbarkBuilderModuleWindowPrefabBase<QudChartypeModule, HorizontalScroller>
{
	public IEnumerable<ChoiceWithColorIcon> GetSelections()
	{
		foreach (QudChartypeModule.GameTypeDescriptor value in base.module.GameTypes.Values)
		{
			yield return new ChoiceWithColorIcon
			{
				Id = value.ID,
				Title = value.Title,
				IconPath = value.IconTile,
				IconDetailColor = ConsoleLib.Console.ColorUtility.ColorMap[value.IconDetail[0]],
				IconForegroundColor = ConsoleLib.Console.ColorUtility.ColorMap[value.IconForeground[0]],
				Description = value.Description,
				Chosen = IsChoiceSelected
			};
		}
	}

	public bool IsChoiceSelected(ChoiceWithColorIcon choice)
	{
		return base.module.getSelectedType() == choice?.Id;
	}

	public override UIBreadcrumb GetBreadcrumb()
	{
		UIBreadcrumb uIBreadcrumb = new UIBreadcrumb
		{
			Id = "CharOption",
			Title = "Select Character Option",
			IconPath = UIBreadcrumb.DEFAULT_ICON,
			IconDetailColor = Color.clear,
			IconForegroundColor = The.Color.Gray
		};
		foreach (QudChartypeModule.GameTypeDescriptor value in base.module.GameTypes.Values)
		{
			if (value.ID == base.module.getSelectedType())
			{
				uIBreadcrumb.Title = value.Title;
				uIBreadcrumb.IconPath = value.IconTile;
				uIBreadcrumb.IconDetailColor = ConsoleLib.Console.ColorUtility.ColorMap[value.IconDetail[0]];
				uIBreadcrumb.IconForegroundColor = ConsoleLib.Console.ColorUtility.ColorMap[value.IconForeground[0]];
				break;
			}
		}
		return uIBreadcrumb;
	}

	public override void DebugQuickstart(string type)
	{
		base.module.selectType("Random");
	}

	public void ChoiceSelected(FrameworkDataElement choice)
	{
		base.module.selectType(choice.Id);
	}

	public override void RandomSelection()
	{
		base.prefabComponent.scrollContext.GetContextAt(Stat.Random(0, base.prefabComponent.choices.Count - 1)).ActivateAndEnable();
	}

	public override void DailySelection()
	{
		base.module.selectType("Daily");
		_module.builder.RefreshActiveWindow();
	}

	public override void BeforeShow(EmbarkBuilderModuleWindowDescriptor descriptor)
	{
		base.prefabComponent.autoHotkey = true;
		base.prefabComponent.scrollContext.wraps = false;
		base.prefabComponent.onSelected.RemoveAllListeners();
		base.prefabComponent.onSelected.AddListener(ChoiceSelected);
		base.prefabComponent.BeforeShow(descriptor, GetSelections());
		base.BeforeShow(descriptor);
	}
}
