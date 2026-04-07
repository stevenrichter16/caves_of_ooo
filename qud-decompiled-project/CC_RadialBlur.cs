using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Colorful/Radial Blur")]
public class CC_RadialBlur : CC_Base
{
	[Range(0f, 1f)]
	public float amount = 0.1f;

	public Vector2 center = new Vector2(0.5f, 0.5f);

	public int quality = 1;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		base.material.SetFloat("_Amount", amount);
		base.material.SetVector("_Center", center);
		if (amount == 0f)
		{
			Graphics.Blit(source, destination);
		}
		else
		{
			Graphics.Blit(source, destination, base.material, quality);
		}
	}
}
