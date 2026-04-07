using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using XRL.UI;
using XRL.UI.Framework;

namespace XRL.CharacterBuilds.Qud.UI;

[UIView("CharacterCreation:PickAttributes", false, false, false, null, null, false, 0, false, NavCategory = "Chargen", UICanvas = "Chargen/PickAttributes", UICanvasHost = 1)]
public class QudAttributesModuleWindow : EmbarkBuilderModuleWindowPrefabBase<QudAttributesModule, HorizontalScroller>
{
	public static readonly string EID_BEFORE_GET_BASE_ATTRIBUTES = "BeforeGetBaseAttributes";

	public static readonly string EID_GET_BASE_ATTRIBUTES = "GetBaseAttributes";

	public static readonly string EID_GET_BASE_AP = "GetBaseAP";

	public List<AttributeDataElement> attributes;

	private int baseAP;

	public int apRemaining
	{
		get
		{
			if (attributes == null)
			{
				return baseAP;
			}
			return baseAP - attributes.Sum((AttributeDataElement a) => a.APSpent);
		}
	}

	public void updateData()
	{
		base.module.data = new QudAttributesModuleData();
		base.module.data.apSpent = apRemaining - baseAP;
		base.module.data.apRemaining = apRemaining;
		base.module.data.baseAp = baseAP;
		attributes.ForEach(delegate(AttributeDataElement e)
		{
			base.module.data.PointsPurchased.Add(e.Attribute, e.Purchased);
		});
		GetOverlayWindow().UpdateMenuBars(GetOverlayWindow().currentWindowDescriptor);
		base.module.setData(base.module.data);
	}

	public IEnumerable<AttributeDataElement> GetSelections()
	{
		object element = base.module.builder.handleUIEvent(EID_BEFORE_GET_BASE_ATTRIBUTES, new List<AttributeDataElement>());
		element = base.module.builder.handleUIEvent(EID_GET_BASE_ATTRIBUTES, element);
		foreach (AttributeDataElement item in element as List<AttributeDataElement>)
		{
			yield return item;
		}
	}

	public override IEnumerable<MenuOption> GetKeyMenuBar()
	{
		yield return new MenuOption
		{
			Id = null,
			InputCommand = "",
			KeyDescription = null,
			Description = ((base.module.data.apRemaining < 0) ? ("{{R|Points Remaining: " + base.module.data.apRemaining + "}}") : ("{{y|Points Remaining: " + base.module.data.apRemaining + "}}"))
		};
		foreach (MenuOption item in base.GetKeyMenuBar())
		{
			yield return item;
		}
	}

	public override void Init()
	{
		base.prefabComponent.selectionPrefab = ((GameObject)Resources.Load("Prefabs/AttributeSelectionControl")).GetComponent<FrameworkUnityScrollChild>();
		base.prefabComponent.GetComponentInChildren<GridLayoutGroup>(includeInactive: true).cellSize = new Vector2(153f, base.prefabComponent.GetComponentInChildren<GridLayoutGroup>().cellSize.y);
	}

	public void UpdateControls()
	{
		if (attributes == null)
		{
			return;
		}
		attributes.ForEach(delegate(AttributeDataElement a)
		{
			a.window = this;
			if (base.module?.data?.PointsPurchased != null)
			{
				if (base.module.data.PointsPurchased.TryGetValue(a.Attribute, out var value))
				{
					a.Purchased = value;
				}
				else
				{
					a.Purchased = 0;
				}
			}
			else
			{
				a.Purchased = 0;
			}
		});
		foreach (AttributeDataElement attribute in attributes)
		{
			attribute?.control?.Updated();
		}
	}

	public override void ResetSelection()
	{
		QudAttributesModuleData data = new QudAttributesModuleData();
		baseAP = (int)base.module.builder.handleUIEvent(EID_GET_BASE_AP, 0);
		base.module.setData(data);
		UpdateControls();
	}

	public override void BeforeShow(EmbarkBuilderModuleWindowDescriptor descriptor)
	{
		attributes = new List<AttributeDataElement>(GetSelections());
		UpdateControls();
		base.prefabComponent.BeforeShow(descriptor, attributes);
		base.prefabComponent.scrollContext.wraps = false;
		foreach (AbstractScrollContext gridSibling in GetGridSiblings())
		{
			gridSibling.gridSiblings = GetGridSiblings;
		}
		baseAP = (int)base.module.builder.handleUIEvent(EID_GET_BASE_AP, 0);
		updateData();
		base.BeforeShow(descriptor);
	}

	public override void RandomSelection()
	{
		while (apRemaining > 0)
		{
			attributes.GetRandomElement().raise();
		}
		UpdateControls();
	}

	public IEnumerable<AbstractScrollContext> GetGridSiblings()
	{
		foreach (ScrollChildContext context in base.prefabComponent.scrollContext.contexts)
		{
			if (context.proxyTo is AbstractScrollContext abstractScrollContext)
			{
				yield return abstractScrollContext;
			}
		}
	}

	public void AttributeUpdated(AttributeDataElement att)
	{
		updateData();
	}

	public override UIBreadcrumb GetBreadcrumb()
	{
		return new UIBreadcrumb
		{
			Id = GetType().FullName,
			Title = "Attributes",
			IconPath = "Items/sw_sign_medical.bmp",
			IconDetailColor = Color.clear,
			IconForegroundColor = The.Color.Gray
		};
	}
}
