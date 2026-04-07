using UnityEngine;

namespace QupKit;

public class BaseView : BaseControl
{
	public LegacyViewManager views;

	public CanvasGroup canvasGroup;

	public virtual void OnCommand(string Command)
	{
	}

	public virtual void OnGUI()
	{
	}

	public virtual void Update()
	{
	}

	public virtual void OnAttach(GameObject Canvas)
	{
	}

	public void AttachTo(GameObject CanvasGroup)
	{
		base.rootObject = CanvasGroup;
		base.rootObject.SetActive(value: false);
		canvasGroup = CanvasGroup.GetComponent<CanvasGroup>();
		base.Layout = new ControlLayout(ControlAnchor.Fill);
		foreach (Transform item in base.rootObject.transform)
		{
			if (!ChildrenByControl.ContainsKey(item.gameObject))
			{
				AddChild(new AttachedControl(item.gameObject), new ControlLayout(ControlAnchor.Custom));
			}
		}
		OnAttach(CanvasGroup);
		base.rootObject.SetActive(value: false);
	}

	public void Create(string ID)
	{
		base.rootObject = new GameObject();
		base.rootObject.AddComponent<RectTransform>();
		base.rootObject.name = ID;
		canvasGroup = base.rootObject.AddComponent<CanvasGroup>();
		canvasGroup.blocksRaycasts = false;
		canvasGroup.interactable = false;
		base.rootObject.SetActive(value: false);
		base.Layout = new ControlLayout(ControlAnchor.Fill);
	}

	public virtual void OnCreate()
	{
	}

	public virtual void BeforeEnter()
	{
	}

	public virtual void OnEnter()
	{
	}

	public virtual void ShowClickthroughOverlay()
	{
		base.rootObject.SetActive(value: true);
		canvasGroup.blocksRaycasts = false;
		canvasGroup.interactable = false;
		ApplyLayout();
	}

	public virtual void Enter()
	{
		BeforeEnter();
		base.rootObject.SetActive(value: true);
		canvasGroup.blocksRaycasts = true;
		canvasGroup.interactable = true;
		ApplyLayout();
		OnEnter();
	}

	public virtual bool BeforeLeave()
	{
		return true;
	}

	public virtual void OnLeave()
	{
	}

	public virtual void Overlapped()
	{
	}

	public virtual void Leave()
	{
		if (BeforeLeave())
		{
			base.rootObject.SetActive(value: false);
			canvasGroup.blocksRaycasts = false;
			canvasGroup.interactable = false;
			OnLeave();
		}
	}

	public virtual void DisableInput()
	{
		canvasGroup.interactable = false;
	}

	public virtual void EnableInput()
	{
		canvasGroup.interactable = true;
	}
}
