using Qud.UI;
using QupKit;
using UnityEngine;

public class EditorMenuView : BaseView
{
	public static EditorMenuView Instance;

	public override void OnCreate()
	{
		AddChild(new ButtonControl("SelectMapEditor", "Map Editor", 300, 150, delegate
		{
			UIManager.getWindow("MapEditor").Show();
		}), new ControlLayout(ControlAnchor.Middle, new Vector3(0f, 0f, 0f)));
	}
}
