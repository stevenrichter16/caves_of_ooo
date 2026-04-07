using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XRL.UI;

namespace Qud.UI;

public class MissileWeaponAreaInfo : MonoBehaviour
{
	public UIThreeColorProperties image;

	public GameObject fullBarPrefab;

	public GameObject barContainer;

	public UITextSkin text;

	public Color fullColor;

	public Color emptyColor;

	public List<Image> bars = new List<Image>();

	public int totalBars;

	public void UpdateFrom(MissileWeaponArea.MissileWeaponAreaWeaponStatus status)
	{
		bool flag = false;
		image.FromRenderable(status.renderable);
		if (totalBars != status.displayAmmoTotalBars)
		{
			if (totalBars > status.displayAmmoTotalBars)
			{
				int num = totalBars - status.displayAmmoTotalBars;
				for (int i = 0; i < num; i++)
				{
					Object.Destroy(bars[0].gameObject);
					bars.RemoveAt(0);
				}
			}
			else
			{
				int num2 = status.displayAmmoTotalBars - totalBars;
				for (int j = 0; j < num2; j++)
				{
					Image component = Object.Instantiate(fullBarPrefab).GetComponent<Image>();
					component.gameObject.SetActive(value: true);
					component.transform.SetParent(barContainer.transform, worldPositionStays: false);
					component.transform.SetAsLastSibling();
					bars.Add(component);
				}
			}
			totalBars = status.displayAmmoTotalBars;
			flag = false;
		}
		for (int k = 0; k < totalBars; k++)
		{
			if (k < status.displayAmmoRemainingBars)
			{
				bars[k].color = fullColor;
			}
			else
			{
				bars[k].color = emptyColor;
			}
		}
		if ((status.text == null) ? text.SetText("") : text.SetText(status.text))
		{
			LayoutRebuilder.MarkLayoutForRebuild(base.gameObject.transform as RectTransform);
			Canvas.ForceUpdateCanvases();
		}
	}
}
