using UnityEngine;

namespace Qud.UI;

public class ModalWindowBase<T> : WindowBase where T : WindowBase, new()
{
	public static T instance;

	public static void ShowModal()
	{
		instance.Show();
	}

	public override void Init()
	{
		instance = this as T;
	}

	public override void Hide()
	{
		base.canvas.enabled = false;
		base.raycaster.enabled = false;
		if (this != instance)
		{
			Object.Destroy(base.gameObject);
		}
	}

	public override void Show()
	{
		GameObject obj = Object.Instantiate(base.gameObject);
		obj.SetActive(value: true);
		obj.GetComponent<ModalWindowBase<T>>().canvas.enabled = true;
		obj.GetComponent<ModalWindowBase<T>>().raycaster.enabled = true;
		obj.transform.SetParent(base.gameObject.transform.parent, worldPositionStays: false);
		obj.transform.SetAsLastSibling();
		RectTransform component = GetComponent<RectTransform>();
		RectTransform component2 = obj.GetComponent<RectTransform>();
		component2.anchorMin = component.anchorMin;
		component2.anchorMax = component.anchorMax;
		component2.anchoredPosition = component.anchoredPosition;
		component2.sizeDelta = component.sizeDelta;
	}
}
