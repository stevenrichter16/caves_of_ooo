using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Ward Gleam — a cleansing gleam over your equipment. Port of
    /// <c>WardGleamMutation</c>; taught by WardGleamGrimoire, buyable in
    /// Spellcraft.
    ///
    /// <para>Verbatim: cooldown 15, self-only. Strips
    /// <see cref="AcidicEffect"/> and <see cref="CharredEffect"/> from
    /// every equipped item. "Nothing to cleanse." is a free refusal — no
    /// cooldown, no turn — exactly the mutation's contract.</para>
    /// </summary>
    public class Spellcraft_WardGleam : SpellSkillPart
    {
        public override string Name => nameof(Spellcraft_WardGleam);

        public const int COOLDOWN = 15;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Ward Gleam",
                Command = "CommandWardGleam",
                Class = "Spellcraft",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = 0,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) { EmitSkillRejectedDiag(ctx, "no_inventory"); return false; }

            bool removedAny = false;
            var equipped = inventory.GetAllEquipped();
            for (int i = 0; i < equipped.Count; i++)
            {
                Entity item = equipped[i];
                bool removedFromItem = false;
                if (item.HasEffect<AcidicEffect>())
                {
                    if (item.RemoveEffect<AcidicEffect>())
                        SpellFxCapture.RecordOutcome(ctx.Zone, actor, "cleansing", "Acidic", 1);
                    removedFromItem = true;
                }
                if (item.HasEffect<CharredEffect>())
                {
                    if (item.RemoveEffect<CharredEffect>())
                        SpellFxCapture.RecordOutcome(ctx.Zone, actor, "cleansing", "Charred", 1);
                    removedFromItem = true;
                }
                removedAny |= removedFromItem;
            }

            if (!removedAny)
            {
                MessageLog.Add("Nothing to cleanse.");
                EmitSkillRejectedDiag(ctx, "nothing_to_cleanse");
                return false;
            }

            return true;
        }
    }
}
