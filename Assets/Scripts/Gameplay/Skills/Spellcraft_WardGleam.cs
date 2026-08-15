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
    public class Spellcraft_WardGleam : BaseSkillPart
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

        public override bool OnCommand(SkillEventContext ctx)
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
                    item.RemoveEffect<AcidicEffect>();
                    removedFromItem = true;
                }
                if (item.HasEffect<CharredEffect>())
                {
                    item.RemoveEffect<CharredEffect>();
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
