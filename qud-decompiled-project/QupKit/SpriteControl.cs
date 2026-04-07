using UnityEngine;
using UnityEngine.UI;

namespace QupKit;

public class SpriteControl : BaseControl
{
	public SpriteControl(string Name, string Sprite, float Scale)
	{
		base.rootObject = new GameObject();
		base.rootObject.name = Name;
		base.rootObject.AddComponent<RectTransform>();
		base.rootObject.AddComponent<CanvasRenderer>();
		base.rootObject.AddComponent<Image>();
		base.MainColor = new Color(1f, 1f, 1f, 1f);
		Debug.Log(Sprite);
		base.Sprite = Sprite;
		base.Width = base.rootObject.GetComponent<Image>().preferredWidth * Scale;
		base.Height = base.rootObject.GetComponent<Image>().preferredHeight * Scale;
	}
}
