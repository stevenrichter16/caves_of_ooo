using UnityEngine;
using UnityEngine.UI;

namespace ModelShark;

[RequireComponent(typeof(Button))]
public class CloseAllTooltips : MonoBehaviour
{
	private void Start()
	{
		base.gameObject.GetComponent<Button>().onClick.AddListener(delegate
		{
			TooltipManager.Instance.CloseAll();
		});
	}
}
