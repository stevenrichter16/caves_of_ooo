using UnityEngine;

public class TogglePanel : MonoBehaviour
{
	public void ToggleClick()
	{
		base.gameObject.SetActive(!base.gameObject.activeInHierarchy);
	}
}
