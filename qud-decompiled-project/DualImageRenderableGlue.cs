using ConsoleLib.Console;
using ModelShark;
using UnityEngine;
using XRL.UI;

public class DualImageRenderableGlue : MonoBehaviour, Tooltip.SetupHelper
{
	public struct DualImageRenderableGlueData
	{
		public IRenderable imageOneRenderable;

		public IRenderable imageTwoRenderable;

		public DualImageRenderableGlueData(IRenderable one, IRenderable two)
		{
			imageOneRenderable = one;
			imageTwoRenderable = two;
		}
	}

	public IRenderableDisplayManager image;

	public IRenderableDisplayManager image2;

	public void BeforeShow(TooltipTrigger trigger, Tooltip tooltip)
	{
		if (trigger.AdditionalData is DualImageRenderableGlueData dualImageRenderableGlueData)
		{
			image.FromRenderable(dualImageRenderableGlueData.imageOneRenderable);
			image2.FromRenderable(dualImageRenderableGlueData.imageTwoRenderable);
		}
		if (Media.sizeClass < Media.SizeClass.Medium)
		{
			base.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
		}
		else
		{
			base.transform.localScale = new Vector3(1f, 1f, 1f);
		}
	}
}
