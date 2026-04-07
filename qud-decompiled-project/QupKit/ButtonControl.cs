using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QupKit;

public class ButtonControl : BaseControl
{
	public ButtonControl(string ID, string Text, int Width, int Height, Action<PointerEventData> OnClick = null)
	{
		base.rootObject = PrefabManager.Create("Button");
		base.rootObject.name = ID;
		base.rootObject.GetComponent<RectTransform>().sizeDelta = new Vector2(Width, Height);
		base.Label = Text;
		base.Width = base.rootObject.GetComponent<RectTransform>().sizeDelta.x;
		base.Height = base.rootObject.GetComponent<RectTransform>().sizeDelta.y;
		OnClicked = OnClick;
	}
}
