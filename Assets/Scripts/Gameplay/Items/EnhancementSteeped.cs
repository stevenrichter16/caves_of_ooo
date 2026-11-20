using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A garment steeped against acid (Docs/MACHINE-INVENTED-USES.md 3h; the V1 of Docs/MachineDesign/machine-bath.md): +<see cref="Amount"/> acid resistance for <see cref="TurnsTotal"/> turns of
    /// wear. The state is on the garment and travels with it (carried, sold, dropped); it is paused while the garment is not worn, so it is spent only on the wearer's own turns. Putting the
    /// garment on gives the wearer a <see cref="SteepedAcidEffect"/> for the turns that remain; taking it off keeps what is left. Acid still does at least 1 damage per hit.
    /// <para>It is gear-only by construction: a trough steeps only garments (a Part allow-list: armor made of organic or cloth material), never a person, body or specimen.</para>
    /// </summary>
    public sealed class EnhancementSteeped : IItemEnhancement
    {
        public const int DefaultAmount = 35, DefaultTurns = 100;

        /// <summary>"acid" (the steeping, the default and every earlier save) or "fire" (the clay-wash, Docs/MACHINE-INVENTED-USES.md 3l). Set by the trough that applies it.</summary>
        public string Kind = SteepedAcidEffect.AcidKind;
        public bool IsFire => Kind == SteepedAcidEffect.FireKind;

        public int Amount = DefaultAmount;
        public int TurnsTotal = DefaultTurns;
        public int TurnsLeft = DefaultTurns;

        public override string Name => "EnhancementSteeped";
        public override string GetDisplayName() => IsFire ? "clay-washed" : "steeped";

        public override bool Applicable(Entity item)
            => item != null && item.GetPart<ArmorPart>() != null && !item.HasTag("Creature") && GarmentMaterial(item);

        /// <summary>Leather, hide and cloth take a steeping; metal and stone do not.</summary>
        public static bool GarmentMaterial(Entity item)
        {
            var material = item?.GetPart<MaterialPart>(); if (material == null) return false;
            return material.HasMaterialTag("Organic") || material.HasMaterialTag("Cloth");
        }

        public override string GetEffectDescription()
            => (IsFire ? "Clay-washed against fire: +" + Amount + " heat resistance while worn, " : "Steeped against acid: +" + Amount + " acid resistance while worn, ") + TurnsLeft + " more turns of wear" + (TurnsLeft < TurnsTotal ? " (of " + TurnsTotal + ")" : "")
               + (IsFire ? ". Fire still does at least 1 damage per hit until your resistance reaches 100." : ". Acid still does at least 1 damage per hit until your resistance reaches 100.");

        public override void OnEquipped(Entity actor, Entity item)
        {
            if (actor == null || item == null) return;
            if (TurnsLeft <= 0) { ItemEnhancing.Remove(item, "EnhancementSteeped"); return; }
            if (FindEffect(actor, item.ID) != null) return;   // already worn: a second dispatch is a no-op
            actor.ApplyEffect(new SteepedAcidEffect(Amount, TurnsLeft, item.ID, Kind));
        }

        public override void OnUnequipped(Entity actor, Entity item)
        {
            if (actor == null || item == null) return;
            var effect = FindEffect(actor, item.ID); if (effect == null) return;
            TurnsLeft = System.Math.Max(0, effect.Duration);   // paused: what is left stays with the garment
            actor.GetPart<StatusEffectsPart>()?.RemoveEffect(effect);
        }

        static SteepedAcidEffect FindEffect(Entity actor, string garmentId)
        {
            var effects = actor?.GetPart<StatusEffectsPart>()?.GetAllEffects(); if (effects == null) return null;
            for (int i = 0; i < effects.Count; i++) if (effects[i] is SteepedAcidEffect s && s.GarmentId == garmentId) return s;
            return null;
        }

        /// <summary>The steeping ran out while the garment was worn: strip it from the garment and say so.</summary>
        public static void Expire(Entity wearer, string garmentId)
        {
            var inventory = wearer?.GetPart<InventoryPart>(); if (inventory?.EquippedItems == null) return;
            foreach (var pair in inventory.EquippedItems)
            {
                var garment = pair.Value; if (garment == null || garment.ID != garmentId) continue;
                ItemEnhancing.Remove(garment, "EnhancementSteeped");
                MessageLog.Add("The steeping on your " + garment.GetDisplayName() + " has worn off.");
                if (Diag.IsChannelEnabled("effect")) Diag.Record("effect", "SteepingExpired", target: wearer, payload: new { garment = garment.BlueprintName });
                return;
            }
        }
    }
}
