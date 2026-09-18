using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cryomancy active: raises a short wall of ice across a chosen
    /// direction. It melts on its own after a few turns.
    ///
    /// <para>SPELLCRAFT SM6 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.4) —
    /// the only power in any tree that EDITS THE MAP. Everything else
    /// damages, primes, or moves a creature; this one changes where
    /// creatures can walk. That is its entire identity, and why it deals
    /// no damage at all.</para>
    ///
    /// <para><b>Why the ice melts.</b> A permanent wall would let a
    /// player brick a corridor and farm it forever. The power buys a few
    /// turns of tempo — long enough to finish a setup, short enough that
    /// it is a decision rather than a solution.</para>
    ///
    /// <para><b>It never buries anyone.</b> Occupied cells are skipped,
    /// so the wall forms AROUND whatever is standing in the line. Raising
    /// solid rock on top of a creature would either delete it from play
    /// or stack a solid on an occupied cell; neither is a state the rest
    /// of the game is prepared for.</para>
    /// </summary>
    public class Cryomancy_GlacialWall : SpellSkillPart
    {
        public override string Name => nameof(Cryomancy_GlacialWall);

        public const int COOLDOWN = 40;
        public const int WALL_LENGTH = 3;

        /// <summary>Blueprint raised in each free cell. Carries a
        /// LifespanPart so it melts without anyone driving it.</summary>
        public const string IceBlueprint = "IceWall";

        /// <summary>Injected by GameBootstrap; null = graceful no-op with
        /// a diag record (the CorpsePart.Factory convention).</summary>
        public static EntityFactory Factory;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Glacial Wall",
                Command = "CommandGlacialWall",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = WALL_LENGTH,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return false; }

            if (Factory == null)
            {
                // Headless contexts have no factory. Decline loudly
                // rather than throw — and say WHY, because "my wall did
                // nothing" is otherwise unanswerable.
                EmitSkillRejectedDiag(ctx, "no_factory");
                return false;
            }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return false; }

            int raised = 0, skipped = 0;
            int x = actorPos.x, y = actorPos.y;

            for (int step = 0; step < WALL_LENGTH; step++)
            {
                x += dx; y += dy;
                if (!ctx.Zone.InBounds(x, y)) break;

                var cell = ctx.Zone.GetCell(x, y);
                if (cell == null) break;

                // Already stone: nothing to add, and the wall continues
                // past it rather than stopping — an ice wall butting
                // against a pillar is still a useful wall.
                if (cell.IsSolid()) { skipped++; continue; }

                // Never bury a creature.
                bool occupied = false;
                for (int i = 0; i < cell.Occupants.Count; i++)
                {
                    var e = cell.Occupants[i];
                    if (e == null) continue;
                    if (e.Tags.ContainsKey("Creature")) { occupied = true; break; }
                }
                if (occupied) { skipped++; continue; }

                Entity ice;
                try { ice = Factory.CreateEntity(IceBlueprint); }
                catch (System.Exception) { continue; }
                if (ice == null) continue;

                ctx.Zone.AddEntity(ice, x, y);
                SpellFxCapture.AffectCell(ctx.Zone, x, y);
                raised++;
            }

            if (raised == 0)
            {
                EmitSkillRejectedDiag(ctx, skipped > 0 ? "line_obstructed" : "no_room");
                MessageLog.Add(actor.GetDisplayName() + "'s glacial wall finds no ground to take.");
                return false;
            }

            MessageLog.Add(actor.GetDisplayName() + " raises a wall of ice, "
                + raised + " pace" + (raised == 1 ? "" : "s") + " wide!");
        
            return true;
        }
    }
}
