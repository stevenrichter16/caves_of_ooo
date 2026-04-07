using UnityEngine;
using UnityEngine.UI;
using XRL.UI;
using XRL.UI.Framework;

namespace Qud.UI;

[ExecuteInEditMode]
public class KeybindBox : MonoBehaviour
{
	public Image backgroundImage;

	public Image fullBorderImage;

	public UITextSkin textSkin;

	public FrameworkContext context;

	public bool editMode;

	public bool selectedMode;

	public bool forceUpdate;

	public string boxText = "{{c|None}}";

	private bool? lastEditMode;

	private bool? wasSelected;

	private void Start()
	{
		forceUpdate = true;
	}

	public void Apply()
	{
		forceUpdate = true;
		Update();
	}

	public void Update()
	{
		bool? flag = context.context?.IsActive();
		if (wasSelected != flag || lastEditMode != editMode || forceUpdate)
		{
			lastEditMode = editMode;
			wasSelected = flag;
			forceUpdate = false;
			textSkin.text = boxText;
			selectedMode = flag == true;
			fullBorderImage.enabled = selectedMode || editMode;
			backgroundImage.enabled = editMode;
			if (editMode)
			{
				textSkin.text = "{{R|press key...}}";
			}
			textSkin.Apply();
		}
	}
}
