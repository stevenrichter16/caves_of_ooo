using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Multiple Arms mutation: adds an extra arm/hand pair to the creature's body tree.
    /// Now uses the real body part system (mirrors Qud's MultipleArms mutation pattern):
    /// - Adds arm + hand body parts using a deterministic ManagerID
    /// - Hand can hold a weapon for multi-weapon combat
    /// - Removes cleanly via manager ID on unmutate
    ///
    /// Falls back to legacy prototype behavior if entity has no Body part.
    /// </summary>
    public class ExtraArmPrototypeMutation : BaseMutation
    {
        public const string MANAGER_ID = "ExtraArmMutation";
        public const string EXTRA_LIMB_SLOT = "ExtraHand";
        public const string STRENGTH_BONUS = "Strength:1";

        private Entity _generatedLimbItem;

        public override string Name => "ExtraArmPrototype";
        public override string MutationType => "Physical";
        public override string DisplayName => "Extra Arm";
        public override bool AffectsBodyParts => true;
        public override bool GeneratesEquipment => true;

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);

            var body = entity.GetPart<Body>();
            if (body != null)
            {
                // Real body system: add arm + hand body parts
                AddExtraArm(body);
            }
            else
            {
                // Legacy fallback: generated equipment item
                EnsureGeneratedItem();
                RegisterGeneratedEquipment(
                    _generatedLimbItem,
                    autoEquip: true,
                    autoRemoveOnMutationLoss: true);
            }
        }

        public override void Unmutate(Entity entity)
        {
            var body = entity.GetPart<Body>();
            if (body != null)
            {
                body.RemovePartsByManager(MANAGER_ID, evenIfDismembered: true);
                body.UpdateBodyParts();
            }
            base.Unmutate(entity);
        }

        private void AddExtraArm(Body body)
        {
            var root = body.GetBody();
            if (root == null) return;

            // Add arm as child of root body
            var arm = new BodyPart
            {
                Type = "Arm",
                Name = "extra arm",
                Description = "Arm",
                Appendage = true,
                Contact = true,
            };
            body.AddPartByManager(MANAGER_ID, root, arm);

            // Add hand as child of the new arm
            var hand = new BodyPart
            {
                Type = "Hand",
                Name = "extra hand",
                Description = "Hand",
                Appendage = true,
                Contact = true,
                SupportsDependent = "extra hand",
            };
            body.AddPartByManager(MANAGER_ID, arm, hand);

            body.UpdateBodyParts();
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
