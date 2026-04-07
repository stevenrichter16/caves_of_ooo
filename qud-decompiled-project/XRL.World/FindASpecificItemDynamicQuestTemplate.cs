using System;
using XRL.Rules;
using XRL.World.Parts;

namespace XRL.World;

[Serializable]
public class FindASpecificItemDynamicQuestTemplate : BaseDynamicQuestTemplate
{
	public override void init(DynamicQuestContext context)
	{
		Array values = Enum.GetValues(typeof(QuestStoryType_FindASpecificItem));
		QuestStoryType_FindASpecificItem questStoryType_FindASpecificItem = (QuestStoryType_FindASpecificItem)values.GetValue(Stat.Random(0, values.Length - 1));
		GameObject questDeliveryItem = context.getQuestDeliveryItem();
		questDeliveryItem.AddPart(new DynamicQuestTarget());
		questDeliveryItem.SetStringProperty("HasPregeneratedName", "yes");
		questDeliveryItem.Render.DisplayName = context.getQuestItemNameMutation(questDeliveryItem.Render.DisplayName);
		string text = base.zoneManager.CacheObject(questDeliveryItem);
		DynamicQuestDeliveryTarget questDeliveryTarget = context.getQuestDeliveryTarget();
		base.zoneManager.AddZoneBuilder(context.getQuestGiverZone(), 6000, "FindASpecificItemDynamicQuestTemplate_FabricateQuestGiver", "questContext", context, "QST", questStoryType_FindASpecificItem, "deliveryItemID", text, "deliveryTarget", questDeliveryTarget);
		base.zoneManager.AddZoneBuilder(questDeliveryTarget.zoneId, 6000, "FindASpecificItemDynamicQuestTemplate_FabricateQuestItem", "deliveryItemID", text);
	}
}
