using UnityEngine;
using UnityEngine.UI;

namespace QupKit;

public class PanelControl : BaseControl
{
	public PanelControl(string Name, float Width, float Height)
	{
		base.rootObject = new GameObject();
		base.rootObject.AddComponent<RectTransform>();
		base.rootObject.AddComponent<CanvasRenderer>();
		base.rootObject.AddComponent<Image>();
		base.rootObject.AddComponent<Mask>();
		base.MainColor = new Color(0f, 0f, 0f, 1f);
		base.rootObject.name = Name;
		base.Width = Width;
		base.Height = Height;
		base.Name = Name;
	}
}
