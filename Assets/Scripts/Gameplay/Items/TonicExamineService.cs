using System.Collections.Generic;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Builds the inventory-Examine popup text for consumables: succinct but
    /// EXACT lines describing what the item does to whoever drinks it or is
    /// caught in its shatter. Drift-proof by construction: every status
    /// effect is built through <see cref="TonicEffectFactory"/> - the same
    /// path the drink/throw pipeline uses - and the lines read the RESULTING
    /// effect fields, so clamps and defaults (e.g. acid's 0..1 coating
    /// fraction) are reported as they will actually land, not as the raw
    /// blueprint numbers suggest.
    ///
    /// Presentation: the text is shown through the existing AnnouncementUI
    /// modal (MessageLog.AddAnnouncement), which pops over the inventory -
    /// the same popup grimoire-learning uses.
    /// </summary>
    public static class TonicExamineService
    {
        /// <summary>
        /// True when <paramref name="item"/> carries any consumable payload
        /// worth a popup (Tonic / StatusTonic / CureTonic / BrewItem part
        /// with at least one concrete line). False → caller shows no row.
        /// </summary>
        public static bool TryDescribe(Entity item, out string text)
        {
            text = null;
            if (item == null)
                return false;

            var tonic = item.GetPart<TonicPart>();
            var status = item.GetPart<StatusTonicPart>();
            var cure = item.GetPart<CureTonicPart>();
            var brew = item.GetPart<BrewItemPart>();
            if (tonic == null && status == null && cure == null && brew == null)
                return false;

            var lines = new List<string>();

            if (tonic != null && !string.IsNullOrWhiteSpace(tonic.Healing))
                lines.Add("Heals " + tonic.Healing.Trim() + " HP on the spot.");

            if (tonic != null && !string.IsNullOrWhiteSpace(tonic.StatBoost))
            {
                int colon = tonic.StatBoost.IndexOf(':');
                if (colon > 0 && int.TryParse(tonic.StatBoost.Substring(colon + 1), out int amount))
                {
                    lines.Add("Stat surge - +" + amount + " "
                        + tonic.StatBoost.Substring(0, colon) + ".");
                }
            }

            if (status != null && !string.IsNullOrWhiteSpace(status.EffectName))
            {
                Effect constructed = TonicEffectFactory.Create(
                    status.EffectName, status.EffectDuration,
                    status.EffectDamageDice, status.EffectMagnitude, null);
                lines.Add(DescribeEffect(constructed, status.EffectName));
            }

            if (brew != null)
            {
                IReadOnlyList<BrewPropertyAmount> effects = brew.GetEffects();
                for (int i = 0; i < effects.Count; i++)
                {
                    Effect constructed = TonicEffectFactory.Create(
                        effects[i].Property, 0, "", effects[i].Potency, null);
                    lines.Add(DescribeEffect(constructed, effects[i].Property));
                }
            }

            if (cure != null && !string.IsNullOrWhiteSpace(cure.CureEffect))
                lines.Add("Cures: " + cure.CureEffect.Trim() + ".");

            if (lines.Count == 0)
                return false;

            var sb = new StringBuilder();
            sb.Append(item.GetDisplayName());
            sb.Append('\n');
            sb.Append('\n');
            sb.Append("On whoever it takes hold of:");
            for (int i = 0; i < lines.Count; i++)
            {
                sb.Append('\n');
                sb.Append(" - ");
                sb.Append(lines[i]);
            }

            string delivery = DescribeDelivery(tonic, brew);
            if (!string.IsNullOrEmpty(delivery))
            {
                sb.Append('\n');
                sb.Append('\n');
                sb.Append(delivery);
            }

            text = sb.ToString();
            return true;
        }

        /// <summary>
        /// One exact line per effect via the shared <see cref="EffectDescriber"/>
        /// (the same lines the look-mode FOCUS panel shows), keeping the
        /// honest label for names the factory cannot construct.
        /// </summary>
        private static string DescribeEffect(Effect effect, string rawName)
        {
            if (effect == null)
                return "Carries '" + rawName + "' - nothing known comes of it.";

            return EffectDescriber.Describe(effect);
        }

        private static string DescribeDelivery(TonicPart tonic, BrewItemPart brew)
        {
            if (brew != null)
            {
                switch (brew.Form)
                {
                    case "Coating":
                        return "A quench medium - temper a weapon in it at the tinker's forge.";
                    case "Throwable":
                        return "Made to be thrown - shatters over the target and adjacent tiles."
                            + " Can also quench a blade at the tinker's forge.";
                    case "Food":
                        return "A simple ration - edible anywhere, no still needed.";
                }

                // Tonic-form brews fall through to the drink wording below,
                // but they too can quench (user-directed change, 2026-07-19).
                if (tonic != null && brew.GetEffects().Count > 0)
                {
                    return (tonic.Drink ? "Drunk when used." : "Applied when used.")
                        + " Can be thrown to shatter, or quench a blade at the tinker's forge.";
                }
            }

            string use = tonic != null && tonic.Drink
                ? "Drunk when used."
                : "Applied when used.";

            if (tonic != null && tonic.HasThrowablePayload())
                use += " Can be thrown - shatters over the target and adjacent tiles.";

            return use;
        }
    }
}
