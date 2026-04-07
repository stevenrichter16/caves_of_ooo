using UnityEngine;

namespace QupKit;

public class PrefabControl : BaseControl
{
	public PrefabControl(string Name, string ID)
	{
		base.rootObject = PrefabManager.Create(ID);
		base.rootObject.name = Name;
		base.Width = base.rootObject.GetComponent<RectTransform>().sizeDelta.x;
		base.Height = base.rootObject.GetComponent<RectTransform>().sizeDelta.y;
	}
}
