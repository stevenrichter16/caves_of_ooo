using ConsoleLib.Console;
using UnityEngine;
using UnityEngine.UI;
using XRL.UI;
using XRL.UI.Framework;
using XRL.World;

namespace Qud.UI;

[RequireComponent(typeof(LayoutElement))]
public class BaseStatusScreen<T> : SingletonWindowBase<T>, IStatusScreen where T : class, new()
{
	protected string filterText;

	public LayoutGroup _layoutGroup;

	public int layoutGroupFrame;

	public int lastWidth = int.MinValue;

	public double lastStageScale;

	public bool LayoutFramesReset;

	public LayoutGroup layoutGroup
	{
		get
		{
			if (_layoutGroup == null)
			{
				_layoutGroup = GetComponent<LayoutGroup>();
			}
			return _layoutGroup;
		}
	}

	public virtual bool Exit()
	{
		return true;
	}

	public virtual string GetNavigationCategory()
	{
		return "StatusScreens";
	}

	public virtual bool WantsCategoryBar()
	{
		return false;
	}

	public virtual IRenderable GetTabIcon()
	{
		return null;
	}

	public virtual string GetTabString()
	{
		return "<override GetTabString>";
	}

	public virtual void HideScreen()
	{
		Hide();
		GetComponent<LayoutElement>().ignoreLayout = true;
	}

	public virtual void FilterUpdated(string filterText)
	{
		this.filterText = filterText;
		UpdateViewFromData();
	}

	public virtual void UpdateViewFromData()
	{
	}

	public virtual void HandleMenuOption(FrameworkDataElement data)
	{
		MetricsManager.LogWarning("Handling menu option in base. " + base.gameObject.name);
	}

	public virtual NavigationContext ShowScreen(XRL.World.GameObject GO, StatusScreensScreen parent)
	{
		Show();
		GetComponent<LayoutElement>().ignoreLayout = false;
		return null;
	}

	public virtual void PrepareLayoutFrame()
	{
		LayoutFramesReset = true;
	}

	public virtual void ResetLayoutFrame(int frameAmount = 3)
	{
		layoutGroup.enabled = true;
		lastStageScale = Options.StageScale;
		lastWidth = Screen.width;
		layoutGroupFrame = frameAmount;
	}

	public virtual void CheckLayoutFrame()
	{
		if (base.canvas.enabled)
		{
			if (LayoutFramesReset || lastWidth != Screen.width || lastStageScale != Options.StageScale)
			{
				ResetLayoutFrame();
			}
			LayoutFramesReset = false;
		}
	}

	public virtual void LateUpdate()
	{
		if (!base.canvas.enabled)
		{
			return;
		}
		if (layoutGroupFrame > 0)
		{
			layoutGroupFrame--;
			if (layoutGroupFrame <= 0)
			{
				layoutGroup.enabled = false;
			}
		}
		CheckLayoutFrame();
	}
}
