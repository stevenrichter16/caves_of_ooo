using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Colorful/LED")]
public class CC_Led : CC_Base
{
	[Range(1f, 255f)]
	public float scale = 80f;

	[Range(0f, 10f)]
	public float brightness = 1f;

	public bool automaticRatio;

	public float ratio = 1f;

	public int mode;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		switch (mode)
		{
		case 0:
			base.material.SetFloat("_Scale", scale);
			break;
		default:
			base.material.SetFloat("_Scale", (float)GetComponent<Camera>().pixelWidth / scale);
			break;
		}
		base.material.SetFloat("_Ratio", automaticRatio ? ((float)(GetComponent<Camera>().pixelWidth / GetComponent<Camera>().pixelHeight)) : ratio);
		base.material.SetFloat("_Brightness", brightness);
		Graphics.Blit(source, destination, base.material);
	}
}
