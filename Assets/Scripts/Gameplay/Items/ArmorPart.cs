namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part representing armor/natural defense.
    /// Faithful to Qud's Armor part.
    /// AV (Armor Value) blocks penetration. DV (Dodge Value) avoids hits.
    /// </summary>
    public class ArmorPart : Part, IMaterialReclaimPermission, IDescribeUse, IDescribeUseRows, IInventoryActionReceiptOwner
    {
        public override string Name => "Armor";

        /// <summary>
        /// Armor Value — compared against penetration rolls.
        /// </summary>
        public int AV = 0;

        /// <summary>
        /// Dodge Value — added to dodge defense.
        /// </summary>
        public int DV = 0;

        /// <summary>
        /// Speed penalty from wearing this armor.
        /// </summary>
        public int SpeedPenalty = 0;
        // Material processing owns payment/custody. Its refusal must not replay
        // the generic action snapshot over an independent injury to the actor.
        public bool OwnsInventoryReceipt(string command)=>ParentEntity?.GetPart<ArmorPart>()==this && MaterialProcessInputPart.IsCommand(command);

        // Old saved armor already has this Part. Only a declared reclamation
        // manifest grants work; material and composition never imply a yield.
        bool IMaterialProcessPermission.Allows(MaterialProcessDefinition definition)
            => ParentEntity?.GetPart<ArmorPart>() == this && definition != null
                && definition.SourceBlueprint == ParentEntity.BlueprintName
                && definition.Mode == "Reclaim" && definition.WorkKind == "HandCutting"
                && definition.SourceRecipeId.Length == 0;
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID != "GetInventoryActions" && e.ID != "InventoryAction")return true;
            if(ParentEntity == null || ParentEntity.HasPart<MaterialProcessInputPart>())return true;
            var ids=MaterialProcessRegistry.ForReclamation(ParentEntity);
            return ids.Length==0 || MaterialProcessActions.Handle(ParentEntity,e,ids);
        }
        public int Order => 40;
        public void AddUseRows(Entity actor,Zone zone,InventoryActionList rows)
        {
            if(ParentEntity==null || ParentEntity.HasPart<MaterialProcessInputPart>())return;
            foreach(var id in MaterialProcessRegistry.ForReclamation(ParentEntity))
            {
                var plan=MaterialProcessPlan.TryCreate(actor,ParentEntity,id,zone,ForgePart.Factory);
                if(MaterialProcessService.TryQuote(plan)!=null)
                    rows.AddAction("describe-reclamation",plan.Definition.ActionText,"description-only",'\0',19);
            }
        }
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            if(ParentEntity == null || ParentEntity.HasPart<MaterialProcessInputPart>()
                || MaterialProcessRegistry.ForReclamation(ParentEntity).Length==0)return;
            guide.How.Add("In your pack, cut one spare plain, unworn armor into a cured leather offcut.");
            guide.Needs.Add("one sound carried or equipped cutting tool");
            guide.Limits.Add("Spends one armor and one action. Armor and stitching are lost; the tool is kept. Altered or damaged armor refuses. Wearing, repairing, selling, or buying prepared leather are alternatives.");
        }
    }
}
