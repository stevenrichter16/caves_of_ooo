using System.Linq;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A garment coated against an element (Docs/MACHINE-PROOFING.md; the steeping and clay-wash of MACHINE-INVENTED-USES.md 3h and 3l are the same thing): +<see cref="Amount"/> of that resistance for
    /// <see cref="TurnsTotal"/> turns of wear. The state is on the garment and travels with it (carried, sold, dropped); it is paused while the garment is not worn, so it is spent only on the wearer's
    /// own turns. Putting the garment on gives the wearer a <see cref="SteepedAcidEffect"/> for the turns that remain; taking it off keeps what is left. The element still does at least 1 damage per hit.
    /// <para>It is gear-only by construction: it goes only onto armor (a Part allow-list), never a person, body or specimen. The class keeps its saved name.</para>
    /// </summary>
    public sealed class EnhancementSteeped : IItemEnhancement
    {
        public const int DefaultAmount = ProofingRules.CoatAmount, DefaultTurns = ProofingRules.CoatTurns;

        /// <summary>An element id from <see cref="ProofingRegistry"/>: "acid" (the default and every earlier save), "heat" ("fire" in a clay-washed garment's save reads as heat), "cold" or "electric". Set by what applies it.</summary>
        public string Kind = SteepedAcidEffect.AcidKind;
        public bool IsFire => ProofingRegistry.Canonical(Kind) == "heat";

        /// <summary>The element's word as the player reads it ("heat", "cold", "shock", "acid").</summary>
        public string Label => SteepedAcidEffect.ElementLabel(Kind);

        public int Amount = DefaultAmount;
        public int TurnsTotal = DefaultTurns;
        public int TurnsLeft = DefaultTurns;

        public override string Name => "EnhancementSteeped";
        public override string GetDisplayName() => "coated";
        public override string GetInventoryState()
            => Amount <= 0 || TurnsTotal <= 0 || TurnsLeft < 0 || TurnsLeft > TurnsTotal ? "coat unavailable"
                : (TurnsLeft == 0 ? "spent " : "") + Label + " coat";

        public override bool Applicable(Entity item)
            => item != null && item.GetPart<ArmorPart>() != null && !item.HasTag("Creature");

        /// <summary>Leather, hide and cloth (the old steeping's allow-list; a coat now takes any armor, so this is only a description helper).</summary>
        public static bool GarmentMaterial(Entity item)
        {
            var material = item?.GetPart<MaterialPart>(); if (material == null) return false;
            return material.HasMaterialTag("Organic") || material.HasMaterialTag("Cloth");
        }

        public override string GetEffectDescription()
        {
            string status = ProofingRegistry.ById(Kind)?.Status ?? "corrosion";
            return "Coated against " + Label + ": +" + Amount + " " + Label + " resistance while worn, " + TurnsLeft + " more turns of wear" + (TurnsLeft < TurnsTotal ? " (of " + TurnsTotal + ")" : "")
                + ". It also blunts " + status + ". " + char.ToUpperInvariant(Label[0]) + Label.Substring(1) + " still does at least 1 damage per hit until your resistance reaches 100.";
        }

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

        internal static SteepedAcidEffect FindEffect(Entity actor, string garmentId)
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
                string kind = garment.Parts.OfType<EnhancementSteeped>().FirstOrDefault()?.Kind ?? SteepedAcidEffect.AcidKind;
                ItemEnhancing.Remove(garment, "EnhancementSteeped");
                MessageLog.Add("The " + SteepedAcidEffect.ElementLabel(kind) + " coat on your " + garment.GetDisplayName() + " has worn off.");
                if (Diag.IsChannelEnabled("effect")) Diag.Record("effect", "SteepingExpired", target: wearer, payload: new { garment = garment.BlueprintName, kind });
                return;
            }
        }
    }
}
