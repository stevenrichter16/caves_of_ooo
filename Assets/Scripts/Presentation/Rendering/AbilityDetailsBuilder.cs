using System;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Rendering
{
    /// <summary>Explicit, read-only details for a current registry row or owned ability.
    /// Declarations are inspected on detached skill instances; nothing is learned or activated.</summary>
    public static class AbilityDetailsBuilder
    {
        public static string BuildForSkill(Entity actor, string className)
        {
            if (actor == null || string.IsNullOrEmpty(className)) return null;
            string name, description; int cost, flags; PowerData power = null;
            if (SkillRegistry.TryGetSkillByClass(className, out var root))
            { name = root.Name; description = root.Description; cost = root.Cost; flags = root.Flags; }
            else if (SkillRegistry.TryGetPowerByClass(className, out power))
            { name = power.Name; description = power.Description; cost = power.Cost; flags = power.Flags; }
            else return null;
            var eligibility = SkillPurchaseEligibility.Evaluate(actor, className);
            bool owned = eligibility.Reason == BuySkillAction.FailureReason.AlreadyOwned;
            if (!owned && (flags & SkillData.FLAG_HIDDEN) != 0) return null;
            if (!owned && (flags & SkillData.FLAG_OBFUSCATED) != 0 && !eligibility.Succeeded
                && eligibility.Reason != BuySkillAction.FailureReason.InsufficientSP) return "Details remain unknown until requirements are met.";

            var text = new StringBuilder(name).Append("\n\n").Append(description);
            var skill = Declaration(className);
            var spec = skill?.DeclareActivatedAbility(actor);
            AppendActivation(text, actor, skill, spec);
            text.Append("\n\nLearning: ").Append(owned ? "Already learned." : PurchaseState(eligibility));
            if (cost >= 0) text.Append("\nPurchase: ").Append(cost).Append(" SP. Available: ").Append(actor.GetStatValue("SP")).Append('.');
            if (power != null)
            {
                text.Append("\nRequired tree: ").Append(power.ParentSkillName).Append('.');
                if (!string.IsNullOrWhiteSpace(power.Requires)) text.Append("\nRequires: ").Append(ClassNames(power.Requires)).Append('.');
                if (!string.IsNullOrWhiteSpace(power.Exclusion)) text.Append("\nIncompatible with: ").Append(ClassNames(power.Exclusion)).Append('.');
                if (!string.IsNullOrWhiteSpace(power.Minimum)) text.Append("\nAttributes: ").Append(Attributes(power.Attribute, power.Minimum)).Append('.');
            }
            return text.ToString();
        }

        public static string BuildForAbility(Entity actor, Guid abilityId)
        {
            var ability = actor?.GetPart<ActivatedAbilitiesPart>()?.GetAbility(abilityId);
            if (ability == null) return null;
            string details = BuildForSkill(actor, ability.SourcePowerClass);
            var text = new StringBuilder(details ?? ability.DisplayName);
            if (details == null)
                AppendActivation(text, actor, null, new ActivatedAbilitySpec { TargetingMode = ability.TargetingMode, Range = ability.Range, Cooldown = ability.MaxCooldown });
            text.Append("\n\nReady: ").Append(ability.CooldownRemaining > 0 ? ability.CooldownRemaining + " remaining on cooldown." : "off cooldown.");
            int slot = actor.GetPart<ActivatedAbilitiesPart>().GetSlotForAbility(abilityId);
            text.Append("\nShortcut: ").Append(slot < 0 ? "unbound; Enter in the ability manager still activates." : AbilityManagerStateBuilder.SlotToHotkey(slot).ToString());
            AppendLiveBuffs(text, actor);
            return text.ToString();
        }

        internal static BaseSkillPart Declaration(string className)
        {
            if (string.IsNullOrEmpty(className) || !SkillsPart.IsKnownSkillClass(className)) return null;
            var type = typeof(BaseSkillPart).Assembly.GetType("CavesOfOoo.Skills." + className);
            return type != null && !type.IsAbstract && typeof(BaseSkillPart).IsAssignableFrom(type)
                ? Activator.CreateInstance(type) as BaseSkillPart : null;
        }

        static void AppendActivation(StringBuilder text, Entity actor, BaseSkillPart skill, ActivatedAbilitySpec spec)
        {
            if (spec == null) { text.Append("\n\nPassive - no activation or cooldown."); return; }
            text.Append("\n\nRange: ").Append(spec.Range).Append(" cells.\nTargeting: ");
            if (skill is ConsumingRiteSkillBase rite)
            {
                switch (rite.Shape)
                {
                    case ConsumingRiteSkillBase.RiteShape.Self: text.Append("yourself."); break;
                    case ConsumingRiteSkillBase.RiteShape.Radius: text.Append("radius ").Append(rite.Range).Append(" around you, excluding you."); break;
                    case ConsumingRiteSkillBase.RiteShape.Cone: text.Append("widening cone in one of eight directions; solid cover blocks its rays."); break;
                    default: text.Append("first creature along one of eight directions; solid cover stops the line."); break;
                }
                text.Append("\nCost: 1 ink from a carried charged grimoire per cast.");
                text.Append("\nSpends up to ").Append(rite.Slots).Append(" marks per target.");
                text.Append(rite.Shape == ConsumingRiteSkillBase.RiteShape.Self
                    ? "\nCollateral: this rite targets you."
                    : "\nCollateral: creatures in its shape include companions and neutral creatures.");
                var ink = GrimoireInk.FindInked(actor);
                text.Append(ink == null ? "\nNo ink available in your pack." : "\nNext carried book: " + ink.Charges + " ink remaining.");
            }
            else text.Append(spec.TargetingMode == AbilityTargetingMode.SelfCentered ? "centered on you; area and effects as described above."
                : spec.TargetingMode == AbilityTargetingMode.AdjacentCell ? "chosen adjacent cell." : "one of eight directions; shape and interception as described above.");
            text.Append("\nCooldown: ").Append(spec.Cooldown).Append(" turns after successful use.");
        }

        static void AppendLiveBuffs(StringBuilder text, Entity actor)
        {
            var effects = actor.GetPart<StatusEffectsPart>()?.GetAllEffects();
            if (effects == null) return;
            foreach (var effect in effects)
                if (effect != null && effect.Duration > 0 && (effect.GetType().Name == "LeyTapEffect" || effect.GetType().Name == "HeartFlameEffect"))
                    text.Append("\nActive spell bonus: ").Append(EffectDescriber.Describe(effect));
        }
        static string PurchaseState(BuySkillAction.Result result)
        {
            if (result.Succeeded) return "Available to buy.";
            switch (result.Reason)
            {
                case BuySkillAction.FailureReason.InsufficientSP: return "More SP needed.";
                case BuySkillAction.FailureReason.MissingPrereq: return "Requires " + ClassNames(result.Detail) + ".";
                case BuySkillAction.FailureReason.StatMinNotMet: return result.Detail + " requirement not met.";
                case BuySkillAction.FailureReason.Exclusion: return "Incompatible with " + ClassNames(result.Detail) + ".";
                case BuySkillAction.FailureReason.NotPurchasable: return "Learned through discovery; not sold here.";
                default: return "Currently unavailable.";
            }
        }
        static string ClassNames(string classes)
        {
            var names = classes.Split(',');
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i].Trim();
                if (SkillRegistry.TryGetSkillByClass(name, out var root)) names[i] = root.Name;
                else if (SkillRegistry.TryGetPowerByClass(name, out var power)) names[i] = power.Name;
            }
            return string.Join(", ", names);
        }
        static string Attributes(string attributes, string minimum)
        {
            var attrs = attributes.Split('|'); var mins = minimum.Split('|'); var groups = new string[Math.Min(attrs.Length, mins.Length)];
            for (int i = 0; i < groups.Length; i++)
            {
                var a = attrs[i].Split(','); var m = mins[i].Split(','); var requirements = new string[Math.Min(a.Length, m.Length)];
                for (int j = 0; j < requirements.Length; j++) requirements[j] = a[j].Trim() + " " + m[j].Trim();
                groups[i] = string.Join(" and ", requirements);
            }
            return string.Join(" or ", groups);
        }
    }
}
