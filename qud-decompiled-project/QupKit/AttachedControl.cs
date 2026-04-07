using UnityEngine;

namespace QupKit;

public class AttachedControl : BaseControl
{
	public AttachedControl(GameObject GO)
	{
		base.rootObject = GO;
		base.Name = GO.name;
		base.Width = GO.GetComponent<RectTransform>().sizeDelta.x;
		base.Height = GO.GetComponent<RectTransform>().sizeDelta.y;
	}
}
