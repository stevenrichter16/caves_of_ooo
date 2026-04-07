using UnityEngine;
using XRL.UI;
using XRL.UI.Framework;

namespace Qud.UI;

public class LeftSideCategory : MonoBehaviour, IFrameworkControl
{
	public UITextSkin text;

	public NavigationContext GetNavigationContext()
	{
		return GetComponent<FrameworkContext>().context;
	}

	public void setData(FrameworkDataElement data)
	{
		if (data is KeybindCategoryRow keybindCategoryRow)
		{
			text.SetText("{{C|" + keybindCategoryRow.CategoryDescription + "}}");
		}
		if (data is MenuOption menuOption)
		{
			text.SetText("{{C|" + menuOption.getMenuText() + "}}");
		}
		if (data is HelpDataRow helpDataRow)
		{
			text.SetText("{{C|" + helpDataRow.CategoryId + "}}");
		}
		if (data is OptionsCategoryRow optionsCategoryRow)
		{
			text.SetText("{{C|" + optionsCategoryRow.CategoryId + "}}");
		}
	}
}
