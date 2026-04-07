using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace RedShadow.CommonDialogs;

public class Menu : DialogBase
{
	public MenuItem MenuItemPrefab;

	public GameObject MenuSeparatorPrefab;

	private readonly List<MenuItem> _items = new List<MenuItem>();

	[FormerlySerializedAs("onClick")]
	[SerializeField]
	private MenuItemEvent _onClick = new MenuItemEvent();

	private Canvas canvas;

	public MenuItemEvent onClick
	{
		get
		{
			return _onClick;
		}
		set
		{
			_onClick = value;
		}
	}

	public override void Update()
	{
		base.Update();
		if (isTop() && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
		{
			RectTransform component = GetComponent<RectTransform>();
			Vector2 screenPoint = Input.mousePosition;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(component, screenPoint, null, out var localPoint);
			if (!component.rect.Contains(localPoint))
			{
				cancel();
			}
		}
	}

	public void show(Vector2 pos)
	{
		base.gameObject.SetActive(value: true);
		GetComponent<RectTransform>().position = pos;
		bool flag = false;
		foreach (MenuItem item in _items)
		{
			flag |= item.Checkable;
		}
		foreach (MenuItem item2 in _items)
		{
			item2.showCheckbox(flag);
		}
		Canvas.ForceUpdateCanvases();
		ensureVisible();
		StartCoroutine(show_co(0.01f));
	}

	public void show(RectTransform parent)
	{
		Vector3[] array = new Vector3[4];
		parent.GetWorldCorners(array);
		show(array[0]);
	}

	public MenuItem addItem(Sprite icon, string text)
	{
		MenuItem component = Object.Instantiate(MenuItemPrefab).GetComponent<MenuItem>();
		component.transform.SetParent(base.transform, worldPositionStays: false);
		component.setText(text);
		component.setIcon(icon);
		component.setCallback(delegate
		{
			onItemClick(text);
		});
		_items.Add(component);
		return component;
	}

	public void addSeparator()
	{
		Object.Instantiate(MenuSeparatorPrefab).transform.SetParent(base.transform, worldPositionStays: false);
	}

	public MenuItem findItem(string text)
	{
		return _items.Find((MenuItem i) => i.Text == text);
	}

	public void clear()
	{
		_items.Clear();
		foreach (Transform item in base.transform)
		{
			Object.Destroy(item.gameObject);
		}
	}

	private void onItemClick(string text)
	{
		_onClick.Invoke(text);
		hide();
	}
}
