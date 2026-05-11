using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Followers F.2.3 — Persuasion_Recruit active ability. Player-owned
    /// skill that recruits an adjacent NPC into the player's party.
    ///
    /// <para><b>Qud parity:</b> mirrors <c>Persuasion_Proselytize</c>
    /// (<c>/Users/steven/qud-decompiled-project/XRL.World.Parts.Skill/Persuasion_Proselytize.cs</c>).
    /// CoO simplifications versus Qud:
    /// <list type="bullet">
    ///   <item>Roll is <c>d20 + Ego-mod</c> vs <c>DC = 10 + max(target.Level - attacker.Level, 0)</c>
    ///         instead of Qud's <c>MentalAttack</c> penetration roll (no MA stat in CoO).</item>
    ///   <item>Over-recruit denied outright (Veto #5) instead of Qud's
    ///         +1 DC stack; F.5+ will revisit when multiple recruitment
    ///         paths land.</item>
    ///   <item>Hostile target denied outright (Veto #7); Qud doesn't gate
    ///         on hostility, relying on combat/conversation possibility
    ///         instead. CoO design choice: stops spam-recruit during
    ///         combat.</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Mechanic:</b> activated ability, 25-turn cooldown,
    /// adjacent-cell targeting. The 8-veto chain (from FOLLOWERS-F2.md
    /// Lockdown #2) is evaluated in order; each veto emits
    /// <c>skill/SkillRejected reason=&lt;tag&gt;</c> and bails. If all
    /// vetos clear, rolls <c>d20 + StatUtils.GetModifier(actor, "Ego")</c>
    /// against the dynamic DC. On success, applies
    /// <see cref="RecruitedEffect"/> (which installs the leader link
    /// via F.1.2's <c>SetPartyLeader</c> and pushes a FollowLeaderGoal
    /// via F.1.5). On roll failure, emits
    /// <c>SkillRejected reason=roll_failed</c> with the roll + DC in the
    /// payload (the verification flow per CLAUDE.md Observability).</para>
    ///
    /// <para><b>Forgive contract:</b> re-uses F.1.2's
    /// <c>BrainPart.SetPartyLeader</c> Forgive step (clears recruiter
    /// from target's PersonalEnemies). Veto #8 deliberately fires
    /// BEFORE the roll, so a target with an ACTIVE personal grudge
    /// can't be coerce-converted by a single skill cast.</para>
    /// </summary>
    public class Persuasion_Recruit : BaseSkillPart
    {
        public override string Name => nameof(Persuasion_Recruit);
        public override string DisplayName => "Recruit";

        /// <summary>Direct Qud parity — `Persuasion_Proselytize.COOLDOWN = 25`.</summary>
        public const int COOLDOWN = 25;

        /// <summary>Base DC for the contested Ego-vs-Level roll. TTRPG-
        /// standard moderate DC; an Ego-16 character (modifier 0) needs
        /// d20 ≥ 10 vs a same-level target = coinflip baseline.</summary>
        public const int BASE_DC = 10;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = DisplayName,
                Command = "CommandRecruit",
                Class = "Persuasion",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            // Veto #1 — null context (defense-in-depth; should be unreachable
            // via normal dispatch, but caught by EmitSkillRejectedDiag's
            // own null-safety so a malformed call doesn't NRE).
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null || ctx.Zone == null)
            {
                EmitSkillRejectedDiag(ctx, "null_context");
                return;
            }
            var actor = ctx.Attacker;

            // Veto #2 — no adjacent target.
            var target = SkillCombatHelpers.FindAdjacentCleaveTarget(actor, actor, ctx.Zone);
            if (target == null)
            {
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            // Veto #3 — self (defense-in-depth; FindAdjacentCleaveTarget
            // excludes the actor itself, but defense-in-depth for future
            // code paths that might bypass the adjacency picker).
            if (target == actor)
            {
                EmitSkillRejectedDiag(ctx, "self_target");
                return;
            }

            // Veto #4 — target has no Brain (creature without AI; can't be
            // a follower).
            var brain = target.GetPart<BrainPart>();
            if (brain == null)
            {
                EmitSkillRejectedDiag(ctx, "target_no_brain");
                return;
            }

            // Veto #5 — already recruited by anyone. F.2 v1 simplification:
            // Qud allows over-recruit with +1 DC stack; CoO denies outright
            // until multiple recruitment paths exist (F.5+).
            if (target.GetEffect<RecruitedEffect>() != null)
            {
                EmitSkillRejectedDiag(ctx, "already_recruited");
                return;
            }

            // Veto #6 — already following someone else (PartyLeader set
            // bypassing the Effect path — e.g. via direct SetPartyLeader
            // from a test, mutation, or future faction-allegiance code).
            if (brain.PartyLeader != null && brain.PartyLeader != actor)
            {
                EmitSkillRejectedDiag(ctx, "follows_another");
                return;
            }

            // Veto #7 — target is hostile (faction or two-way personal
            // grudge). F.1.4's FactionManager.GetFeeling already accounts
            // for both PersonalEnemies sides and party-alignment.
            if (FactionManager.GetFeeling(target, actor) <= FactionManager.HOSTILE_THRESHOLD)
            {
                EmitSkillRejectedDiag(ctx, "target_hostile");
                return;
            }

            // Veto #8 — defender-side personal grudge. Mostly redundant
            // with Veto #7 (PersonalEnemies feeds GetFeeling), but
            // explicit for clarity + symmetry with Qud's CheckInfluence
            // gate, and handles the edge case where GetFeeling might
            // ignore PersonalEnemies due to a stronger override (e.g.
            // ArePartyAligned overriding hostility).
            if (brain.PersonalEnemies.Contains(actor))
            {
                EmitSkillRejectedDiag(ctx, "personal_grudge");
                return;
            }

            // Roll: d20 + StatUtils.GetModifier(actor, "Ego")
            //   vs DC = BASE_DC + max(target.Level - actor.Level, 0)
            int d20 = ctx.Rng.Next(1, 21); // [1, 20]
            int egoMod = StatUtils.GetModifier(actor, "Ego");
            int roll = d20 + egoMod;
            int targetLevel = target.GetStatValue("Level", 1);
            int actorLevel = actor.GetStatValue("Level", 1);
            int dc = BASE_DC + System.Math.Max(targetLevel - actorLevel, 0);

            if (roll < dc)
            {
                // Roll failure — record the payload so diag_query can
                // surface "why didn't recruit succeed?" with the actual
                // numbers, per CLAUDE.md Observability.
                if (Diag.IsChannelEnabled("skill"))
                {
                    Diag.Record(
                        category: "skill",
                        kind: "SkillRejected",
                        actor: actor,
                        target: target,
                        payload: new
                        {
                            skillClass = nameof(Persuasion_Recruit),
                            displayName = DisplayName,
                            reason = "roll_failed",
                            d20 = d20,
                            egoMod = egoMod,
                            roll = roll,
                            dc = dc,
                            actorLevel = actorLevel,
                            targetLevel = targetLevel
                        });
                }
                MessageLog.Add(target.GetDisplayName() + " is unconvinced.");
                return;
            }

            // Success — apply the effect. RecruitedEffect.OnApply handles
            // the rest (SetPartyLeader, FollowLeaderGoal push, Forgive).
            target.ApplyEffect(new RecruitedEffect(actor), source: actor, zone: ctx.Zone);

            if (Diag.IsChannelEnabled("skill"))
            {
                Diag.Record(
                    category: "skill",
                    kind: "Recruited",
                    actor: actor,
                    target: target,
                    payload: new
                    {
                        skillClass = nameof(Persuasion_Recruit),
                        d20 = d20,
                        egoMod = egoMod,
                        roll = roll,
                        dc = dc
                    });
            }
        }
    }
}
