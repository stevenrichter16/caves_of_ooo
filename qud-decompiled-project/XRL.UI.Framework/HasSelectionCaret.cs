using UnityEngine;
using UnityEngine.UI;

namespace XRL.UI.Framework;

public class HasSelectionCaret : MonoBehaviour
{
	public bool useSelectEnabled = true;

	private bool? wasSelected;

	public Image image;

	public FrameworkContext selectable;

	public bool useSelectColor;

	public Color unselectedColor = Color.gray;

	public Color selectedColor = Color.yellow;

	public bool selected => GetSelectable().context?.IsActive() ?? false;

	public FrameworkContext GetSelectable()
	{
		return selectable ?? (selectable = GetComponentInParent<FrameworkContext>());
	}

	public void LateUpdate()
	{
		if (selected != wasSelected && useSelectEnabled && image != null)
		{
			image.enabled = selected;
		}
		if (selected != wasSelected && useSelectColor && image != null)
		{
			image.color = (selected ? selectedColor : unselectedColor);
		}
		wasSelected = selected;
	}
}
