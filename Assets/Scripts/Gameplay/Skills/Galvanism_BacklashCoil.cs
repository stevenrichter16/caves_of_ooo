using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Galvanism active: a static discharge that throws every adjacent
    /// creature back a cell. Needs no aiming.
    ///
    /// <para>SPELLCRAFT SM3 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.1) —
    /// the escape variant of the Ground Surge family. Its entire reason
    /// to exist is the moment you are surrounded and have no turn to
    /// spare on choosing a direction.</para>
    ///
    /// <para><b>The deliberate trade: it does NOT prime.</b> No
    /// <see cref="ElectrifiedEffect"/>, ever. If Backlash Coil both
    /// broke a surround and left everyone charged it would strictly
    /// dominate <see cref="Galvanism_GroundSurge"/> and the family would
    /// collapse into a single power. You buy distance here and prime
    /// somewhere else — that tension is the design.</para>
    /// </summary>
    public class Galvanism_BacklashCoil : BaseSkillPart
    {
        public override string Name => nameof(Galvanism_BacklashCoil);

        public const int COOLDOWN = 40;
        public const int COIL_DAMAGE = 4;

        /// <summary>Cells each adjacent creature is thrown. Two, not
        /// one: a single cell leaves them still able to step back and
        /// swing, which would make this a worse Ground Surge instead of
        /// a real disengage.</summary>
        public const int COIL_PUSH_CELLS = 2;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Backlash Coil",
                Command = "CommandBacklashCoil",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return; }

            // NOTE: no direction check. A self-centred power must not
            // reject on a zero direction — requiring a facing would mean
            // aiming the panic button.
            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return; }

            // Snapshot the ring before shoving anyone: each push moves a
            // creature out of the ring, and reading the ring lazily
            // would make the result depend on iteration order.
            var ring = new List<Entity>(8);
            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    if (ox == 0 && oy == 0) continue;
                    int cx = actorPos.x + ox, cy = actorPos.y + oy;
                    if (!ctx.Zone.InBounds(cx, cy)) continue;

                    var cell = ctx.Zone.GetCell(cx, cy);
                    if (cell == null) continue;

                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        var e = cell.Objects[i];
                        if (e == null || e == actor) continue;
                        if (!e.Tags.ContainsKey("Creature")) continue;
                        ring.Add(e);
                    }
                }
            }

            if (ring.Count == 0)
            {
                EmitSkillRejectedDiag(ctx, "no_target");
                MessageLog.Add(actor.GetDisplayName()
                    + "'s coil discharges into empty air.");
                return;
            }

            int thrown = 0, survivors = 0;
            for (int i = 0; i < ring.Count; i++)
            {
                var target = ring[i];

                var dmg = new Damage(COIL_DAMAGE);
                dmg.AddAttribute("Electric");
                dmg.AddAttribute("Lightning");
                CombatSystem.ApplyDamage(target, dmg, actor, ctx.Zone);

                if (target.GetStatValue("Hitpoints") <= 0) continue;
                survivors++;
                if (SkillCombatHelpers.TryPush(actor, target, ctx.Zone, COIL_PUSH_CELLS))
                    thrown++;
            }

            // Distinguish "walls refused every shove" from "the coil
            // killed everyone", the way Ground Surge does. Blaming the
            // geometry when nobody was left standing sends a future
            // debugger hunting a wall that was never there.
            if (thrown == 0 && survivors > 0)
                EmitSkillRejectedDiag(ctx, "push_blocked");
            else if (survivors == 0)
                EmitSkillRejectedDiag(ctx, "all_targets_died");

            // Report who was HIT, not who moved — the sibling powers both
            // report their target count, and a cast that damaged two
            // cornered enemies but could not move them must not read
            // "hurls 0 attackers clear!", which is the exact corridor
            // situation this power exists for. Latent bug surfaced by
            // adversarial review.
            MessageLog.Add(actor.GetDisplayName() + "'s backlash coil blasts "
                + ring.Count + " attacker" + (ring.Count == 1 ? "" : "s")
                + (thrown > 0 ? ", hurling " + thrown + " clear!" : " — but nothing gives!"));
        }
    }
}
