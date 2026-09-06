namespace CavesOfOoo.Core
{
    /// <summary>Computed identity for mutable crafted payloads. No save fields or parser caches.
    /// This deliberately does not claim complete entity/stat/material equivalence.</summary>
    internal static class StackPayloadIdentity
    {
        internal static bool Same(Entity a, Entity b)
        {
            // Preserve order only among parts sharing a dispatch path. A temper marker's
            // position relative to an enhancement is merely crafting history.
            for (int group = 1; group <= 4; group++)
            {
                int ai = 0, bi = 0;
                while (true)
                {
                    var ap = Next(a, ref ai, group); var bp = Next(b, ref bi, group);
                    if (ap == null || bp == null)
                    {
                        if (ap != bp) return false;
                        break;
                    }
                    if (!SamePart(ap, bp)) return false;
                }
            }
            return true;
        }

        private static Part Next(Entity item, ref int cursor, int group)
        {
            while (cursor < item.Parts.Count)
            {
                var part = item.Parts[cursor++];
                if (Group(item, part) == group) return part;
            }
            return null;
        }

        private static int Group(Entity item, Part part)
        {
            if (part is TonicPart || part is BrewItemPart || part is StatusTonicPart || part is CureTonicPart) return 1;
            if (part is IItemEnhancement) return 2;
            if (part is MeleeWeaponPart) return 3;
            if (part is WeaponTemperPart temper)
            {
                // ClearTemper leaves this inert marker attached after reforge.
                if (part.GetType() == typeof(WeaponTemperPart) && temper.TemperCount == 0
                    && temper.HpPenaltyTotal == 0 && string.IsNullOrEmpty(temper.AppliedSpecsRaw)
                    && HasSingleTemper(item)) return 0;
                return 4;
            }
            return 0;
        }

        private static bool HasSingleTemper(Entity item)
        {
            int count = 0;
            foreach (var part in item.Parts) if (part is WeaponTemperPart && ++count > 1) return false;
            return count == 1;
        }

        private static bool SamePart(Part a, Part b)
        {
            var type = a.GetType();
            if (type != b.GetType()) return false;
            // Exact types: an extension may add state that these comparisons do not know.
            if (type == typeof(TonicPart))
            {
                var x = (TonicPart)a; var y = (TonicPart)b;
                return x.Effect == y.Effect && x.Duration == y.Duration && x.Healing == y.Healing
                    && x.StatBoost == y.StatBoost && x.Message == y.Message && x.Drink == y.Drink;
            }
            if (type == typeof(BrewItemPart))
            { var x = (BrewItemPart)a; var y = (BrewItemPart)b; return x.EffectsRaw == y.EffectsRaw && x.Form == y.Form; }
            if (type == typeof(StatusTonicPart))
            {
                var x = (StatusTonicPart)a; var y = (StatusTonicPart)b;
                return x.EffectName == y.EffectName && x.EffectDuration == y.EffectDuration
                    && x.EffectDamageDice == y.EffectDamageDice && x.EffectMagnitude.Equals(y.EffectMagnitude);
            }
            if (type == typeof(CureTonicPart)) return ((CureTonicPart)a).CureEffect == ((CureTonicPart)b).CureEffect;
            if (type == typeof(MeleeWeaponPart))
            {
                var x = (MeleeWeaponPart)a; var y = (MeleeWeaponPart)b;
                return x.BaseDamage == y.BaseDamage && x.PenBonus == y.PenBonus && x.HitBonus == y.HitBonus
                    && x.MaxStrengthBonus == y.MaxStrengthBonus && x.Stat == y.Stat && x.Attributes == y.Attributes
                    && x.OnHitEffectsRaw == y.OnHitEffectsRaw && x.EmitGasOnHitRaw == y.EmitGasOnHitRaw;
            }
            if (type == typeof(WeaponTemperPart))
            {
                var x = (WeaponTemperPart)a; var y = (WeaponTemperPart)b;
                return x.TemperCount == y.TemperCount && x.HpPenaltyTotal == y.HpPenaltyTotal && x.AppliedSpecsRaw == y.AppliedSpecsRaw;
            }
            if (a is IItemEnhancement ae && b is IItemEnhancement be)
            {
                if (ae.Tier != be.Tier) return false;
                if (type == typeof(EnhancementPaleSalt) || type == typeof(EnhancementChoirIron))
                    return ((EnhancementTagBonusBase)a).BonusDamage == ((EnhancementTagBonusBase)b).BonusDamage;
                if (type == typeof(EnhancementSerrated))
                {
                    var x = (EnhancementSerrated)a; var y = (EnhancementSerrated)b;
                    return x.ChancePercent == y.ChancePercent && x.SaveTarget == y.SaveTarget && x.DamageDice == y.DamageDice;
                }
                if (type == typeof(EnhancementLacquered))
                { var x = (EnhancementLacquered)a; var y = (EnhancementLacquered)b; return x.AvBonus == y.AvBonus && x.AppliedBonus == y.AppliedBonus; }
                if (type == typeof(EnhancementGlowQuartz))
                { var x = (EnhancementGlowQuartz)a; var y = (EnhancementGlowQuartz)b; return x.RadiusBonus == y.RadiusBonus && x.AppliedBonus == y.AppliedBonus; }
                if (type == typeof(EnhancementEngraved))
                {
                    var x = (EnhancementEngraved)a; var y = (EnhancementEngraved)b;
                    return x.Faction == y.Faction && x.RepDelta == y.RepDelta && x.AppliedBonus == y.AppliedBonus;
                }
            }
            return false;
        }
    }
}
