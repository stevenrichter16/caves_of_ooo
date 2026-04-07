using System.Text;
using UnityEngine;

namespace Kobold;

public class AtlasDefinition
{
	public string Prefix;

	public int XSize;

	public int YSize;

	public string Compression;

	public bool Trim;

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("-atlasdef " + Prefix + " x:" + XSize + " y:" + YSize + " compression:" + Compression);
		return stringBuilder.ToString();
	}

	public exAtlas GenerateAtlas()
	{
		exAtlas exAtlas = ScriptableObject.CreateInstance<exAtlas>();
		exAtlas.width = XSize;
		exAtlas.height = YSize;
		exAtlas.useContourBleed = true;
		exAtlas.usePaddingBleed = true;
		exAtlas.trimElements = false;
		exAtlas.trimThreshold = 1000;
		exAtlas.customPadding = 3;
		return exAtlas;
	}
}
