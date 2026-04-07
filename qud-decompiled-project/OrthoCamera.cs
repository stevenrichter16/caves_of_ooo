using UnityEngine;

public class OrthoCamera : MonoBehaviour
{
	protected void Awake()
	{
		base.gameObject.GetComponent<Camera>().transparencySortMode = TransparencySortMode.Orthographic;
	}
}
