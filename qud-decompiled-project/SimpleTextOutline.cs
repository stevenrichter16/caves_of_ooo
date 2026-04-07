using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SimpleTextOutline : MonoBehaviour
{
	public float pixelSize = 1f;

	public Color outlineColor = Color.black;

	public bool resolutionDependant;

	public int doubleResolution = 1024;

	private TextMesh textMesh;

	private MeshRenderer meshRenderer;

	private List<TextMesh> shadowmeshes = new List<TextMesh>();

	private List<MeshRenderer> renderers = new List<MeshRenderer>();

	private string lastString;

	private float lastAlpha = -1f;

	private void LateUpdate()
	{
		if (shadowmeshes.Count == 0)
		{
			textMesh = GetComponent<TextMesh>();
			meshRenderer = GetComponent<MeshRenderer>();
			for (int i = 0; i < 8; i++)
			{
				GameObject gameObject = new GameObject("outline " + i, typeof(TextMesh));
				gameObject.transform.parent = base.transform;
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
				MeshRenderer component = gameObject.GetComponent<MeshRenderer>();
				component.material = new Material(meshRenderer.material);
				component.shadowCastingMode = ShadowCastingMode.Off;
				component.receiveShadows = false;
				component.sortingLayerID = meshRenderer.sortingLayerID;
				component.sortingLayerName = meshRenderer.sortingLayerName;
				shadowmeshes.Add(gameObject.GetComponent<TextMesh>());
				renderers.Add(component);
			}
		}
		if (lastString == textMesh.text)
		{
			if (lastAlpha == textMesh.color.a)
			{
				return;
			}
			foreach (TextMesh shadowmesh in shadowmeshes)
			{
				_ = shadowmesh;
			}
			lastAlpha = textMesh.color.a;
			return;
		}
		lastString = textMesh.text;
		lastAlpha = textMesh.color.a;
		Vector3 vector = Camera.main.WorldToScreenPoint(base.transform.position);
		outlineColor.a = textMesh.color.a * textMesh.color.a;
		for (int j = 0; j < shadowmeshes.Count; j++)
		{
			TextMesh obj = shadowmeshes[j];
			obj.color = outlineColor;
			obj.text = textMesh.text;
			obj.alignment = textMesh.alignment;
			obj.anchor = textMesh.anchor;
			obj.characterSize = textMesh.characterSize;
			obj.font = textMesh.font;
			obj.fontSize = textMesh.fontSize;
			obj.fontStyle = textMesh.fontStyle;
			obj.richText = textMesh.richText;
			obj.tabSize = textMesh.tabSize;
			obj.lineSpacing = textMesh.lineSpacing;
			obj.offsetZ = textMesh.offsetZ;
			bool flag = resolutionDependant && (Screen.width > doubleResolution || Screen.height > doubleResolution);
			Vector3 vector2 = GetOffset(j) * (flag ? (2f * pixelSize) : pixelSize);
			Vector3 position = Camera.main.ScreenToWorldPoint(vector + vector2);
			obj.transform.position = position;
			MeshRenderer obj2 = renderers[j];
			obj2.sortingLayerID = meshRenderer.sortingLayerID;
			obj2.sortingLayerName = meshRenderer.sortingLayerName;
		}
	}

	private Vector3 GetOffset(int i)
	{
		return (i % 8) switch
		{
			0 => new Vector3(0f, 1f, 1f), 
			1 => new Vector3(1f, 1f, 1f), 
			2 => new Vector3(1f, 0f, 1f), 
			3 => new Vector3(1f, -1f, 1f), 
			4 => new Vector3(0f, -1f, 1f), 
			5 => new Vector3(-1f, -1f, 1f), 
			6 => new Vector3(-1f, 0f, 1f), 
			7 => new Vector3(-1f, 1f, 1f), 
			_ => Vector3.zero, 
		};
	}
}
