namespace CavesOfOoo.Core
{
    /// <summary>
    /// Phase-7 prototype mutation:
    /// creates a body-linked "extra arm" equipment item and auto-equips it.
    /// This proves anatomy/equipment seam integration end-to-end without a full body graph.
    /// </summary>
    public class ExtraArmPrototypeMutation : BaseMutation
    {
        public const string EXTRA_LIMB_SLOT = "ExtraHand";
        public const string STRENGTH_BONUS = "Strength:1";

        private Entity _generatedLimbItem;

        public override string Name => "ExtraArmPrototype";
        public override string MutationType => "Physical";
        public override string DisplayName => "Extra Arm (Prototype)";
        public override bool AffectsBodyParts => true;
        public override bool GeneratesEquipment => true;

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            EnsureGeneratedItem();

            RegisterGeneratedEquipment(
                _generatedLimbItem,
                autoEquip: true,
                autoRemoveOnMutationLoss: true);
        }

        private void EnsureGeneratedItem()
        {
            if (_generatedLimbItem != null)
                return;

            _generatedLimbItem = new Entity
            {
                BlueprintName = "Mutation_ExtraArm_Prototype_Item"
            };
            _generatedLimbItem.AddPart(new RenderPart
            {
                DisplayName = "mutant extra arm",
                RenderString = ")",
                ColorString = "&g"
            });
            _generatedLimbItem.AddPart(new PhysicsPart
            {
                Takeable = true,
                Weight = 0
            });
            _generatedLimbItem.AddPart(new EquippablePart
            {
                Slot = EXTRA_LIMB_SLOT,
                EquipBonuses = STRENGTH_BONUS
            });
        }
    }
}
