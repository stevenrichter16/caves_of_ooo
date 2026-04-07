using ModelShark;
using UnityEngine;
using UnityEngine.UI;
using XRL.UI;
using XRL.UI.Framework;

namespace Qud.UI;

[ExecuteInEditMode]
public class OptionsRow : MonoBehaviour, IFrameworkControl, IFrameworkControlSubcontexts
{
	public Image background;

	public NavigationContext proxyContext;

	public FrameworkContext frameworkContext;

	public OptionsDataRow data;

	public RectTransform SubtypesSelection;

	public TooltipTrigger TooltipTrigger;

	public IFrameworkControl CurrentControl;

	public bool selectedMode;

	private bool? wasSelected;

	public void SetupContexts(ScrollChildContext scontext)
	{
		frameworkContext.context = scontext;
		if (CurrentControl is IFrameworkControlSubcontexts frameworkControlSubcontexts)
		{
			frameworkControlSubcontexts.SetupContexts(scontext);
		}
		else if (scontext != null)
		{
			scontext.proxyTo = proxyContext ?? (proxyContext = new NavigationContext());
			proxyContext.parentContext = scontext;
		}
	}

	public NavigationContext GetNavigationContext()
	{
		return frameworkContext.context;
	}

	public void setData(FrameworkDataElement newData)
	{
		CurrentControl?.setData(null);
		data = newData as OptionsDataRow;
		if (string.IsNullOrEmpty(data?.HelpText))
		{
			TooltipTrigger.enabled = false;
		}
		else
		{
			TooltipTrigger.enabled = true;
			TooltipTrigger.SetText("BodyText", RTF.FormatToRTF(data.HelpText));
		}
		TooltipTrigger.maxTextWidth = (int)(base.transform as RectTransform).rect.width;
		bool flag = false;
		foreach (RectTransform item in SubtypesSelection)
		{
			if (item.name == newData?.GetType().Name)
			{
				item.gameObject.SetActive(value: true);
				CurrentControl = item.GetComponent<IFrameworkControl>();
				CurrentControl?.setData(newData);
				flag = true;
			}
			else
			{
				item.gameObject.SetActive(value: false);
			}
		}
		if (newData != null && !flag)
		{
			Transform obj = SubtypesSelection.Find("Unknown");
			obj.gameObject.SetActive(value: true);
			obj.GetComponent<UITextSkin>().SetText("Unhandled " + newData?.GetType().Name + ":" + newData.Id);
		}
	}

	public void Update()
	{
		bool? flag = GetNavigationContext()?.IsActive();
		if (wasSelected != flag)
		{
			wasSelected = flag;
			bool flag2 = (background.enabled = flag == true);
			selectedMode = flag2;
		}
	}
}
