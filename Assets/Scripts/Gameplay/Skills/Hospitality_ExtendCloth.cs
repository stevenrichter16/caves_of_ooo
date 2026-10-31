using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// H1 — Extend the Cloth. A sworn host names an adjacent person guest:
    /// <see cref="UnderTheClothEffect"/> with the host recorded. For three days
    /// no person may harm the guest, the guest keeps the peace, and the host
    /// must not strike them (<see cref="UnderTheClothEffect.Break"/>).
    ///
    /// <para><b>Who accepts:</b> a person (factioned, not Beasts) who is not
    /// already another host's guest. Someone who wants you dead takes the
    /// cloth only when beaten: below <see cref="BeatenPercent"/>% of their
    /// health. That is the claim canon describes, a pursued person reaching
    /// for the hem, not a spell that ends any fight. Their standing toward
    /// you is unchanged.</para>
    ///
    /// <para>Adjacent-cell targeting, as <see cref="Persuasion_Dismiss"/>.
    /// Diag: skill/ClothExtended, skill/SkillRejected (not_sworn, no_target,
    /// not_a_person, another_host, not_yet_beaten).</para>
    /// </summary>
    public class Hospitality_ExtendCloth : BaseSkillPart
    {
        public const int BeatenPercent = 50;
        public const int CooldownTurns = 30;

        public override string Name => nameof(Hospitality_ExtendCloth);
        public override string DisplayName => "Extend the Cloth";

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = DisplayName,
                Command = "CommandExtendCloth",
                Class = "Hospitality",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = CooldownTurns,
            };
        }

        /// <summary>Why <paramref name="target"/> will not take the cloth from
        /// <paramref name="host"/>, or null if they will.</summary>
        public static string RefusalFor(Entity host, Entity target)
        {
            if (!TentRightOath.IsSworn()) return "not_sworn";
            if (target == null || target == host || !target.HasTag("Creature")) return "no_target";
            if (!UnderTheClothEffect.IsPerson(target)) return "not_a_person";
            var cloth = target.GetEffect<UnderTheClothEffect>();
            if (cloth != null && cloth.HostId != host.ID) return "another_host";
            if (FactionManager.GetFeelingUnfloored(target, host) <= FactionManager.HOSTILE_THRESHOLD
                && target.Statistics.TryGetValue("Hitpoints", out var hp) && hp != null && hp.Max > 0
                && hp.BaseValue * 100 >= BeatenPercent * hp.Max) return "not_yet_beaten";
            return null;
        }

        public override bool OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Zone == null)
            {
                EmitSkillRejectedDiag(ctx, "null_context");
                return false;
            }
            var actor = ctx.Attacker;
            var target = SkillCombatHelpers.FindAdjacentSkillTarget(actor, ctx.Zone, ctx.TargetCell, out _);
            string refusal = RefusalFor(actor, target);
            if (refusal != null)
            {
                EmitSkillRejectedDiag(ctx, refusal);
                if (refusal == "not_yet_beaten")
                    MessageLog.Add(target.GetDisplayName() + " will not take your cloth while they think they can win.");
                else if (refusal == "another_host")
                    MessageLog.Add(target.GetDisplayName() + " is already another's guest.");
                return false;
            }

            target.ApplyEffect(new UnderTheClothEffect(actor, sworn: true), actor, null);
            MessageLog.Add(target.GetDisplayName() + " takes the hem of your cloth.");
            // W3: a concern's guest (a Host approach) is settled by the naming.
            CavesOfOoo.Concerns.ConcernWorld.OnGuestHosted(target);
            if (Diag.IsChannelEnabled("skill"))
                Diag.Record(category: "skill", kind: "ClothExtended", actor: actor, target: target,
                    payload: new { skillClass = nameof(Hospitality_ExtendCloth) });
            return true;
        }
    }
}
