using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// P5 B4 (Docs/UPPER-TIER-BESTIARY.md §3.2) — held in a grip. The bearer can act
    /// (strike, cast, use an ability) but cannot walk away: each move it attempts is a
    /// Strength contest against the grappler, one roll per tick (repeating the key in the
    /// same tick does not re-roll). Winning frees the bearer and the move goes ahead.
    ///
    /// <para>The grip ends when the grappler dies, leaves, or is no longer adjacent
    /// (checked each turn start). A constrictor's grip also squeezes for
    /// <see cref="SqueezeDamage"/> at the start of each of the bearer's turns. The
    /// grappler is recorded by ID (a reference would write its body into the save).</para>
    ///
    /// <para>Diag (ai): GrappleBroken, GrippedFast, Squeezed.</para>
    /// </summary>
    public class GrappledEffect : Effect
    {
        public override string DisplayName => "held";
        public override int GetEffectType() => TYPE_GENERAL;

        public string GrapplerId;
        public int SqueezeDamage;
        public int LastAttemptTick = -1;
        /// <summary>Broken free: moves pass until the ordinary cleanup removes the effect
        /// (removing it here would mutate the list the move check is walking).</summary>
        public bool Broken;

        /// <summary>The contest's d20. Tests may replace it.</summary>
        public static Random Rng = new Random();

        public GrappledEffect() { Duration = DURATION_INDEFINITE; }

        public GrappledEffect(Entity grappler, int squeeze) : this()
        {
            GrapplerId = grappler?.ID;
            SqueezeDamage = Math.Max(0, squeeze);
        }

        public Entity FindGrappler(Entity bearer)
        {
            var zone = bearer?.SpatialZone;
            if (zone == null || string.IsNullOrEmpty(GrapplerId)) return null;
            foreach (var e in zone.GetReadOnlyEntities()) if (e.ID == GrapplerId) return e;
            return null;
        }

        bool Holds(Entity bearer, out Entity grappler)
        {
            grappler = FindGrappler(bearer);
            return grappler != null && grappler.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(grappler)
                && SpatialQuery.Distance(bearer.SpatialZone, bearer, grappler) == 1;
        }

        public override void OnApply(Entity target)
        {
            var grappler = FindGrappler(target);
            MessageLog.Add((grappler?.GetDisplayName() ?? "Something") + (target.HasTag("Player") ? " seizes you!" : " seizes " + target.GetDisplayName() + "!"));
        }

        public override void OnRemove(Entity target)
            => MessageLog.Add(target.HasTag("Player") ? "You are free of the grip." : target.GetDisplayName() + " is free of the grip.");

        public override bool AllowMovement(Entity target)
        {
            if (Broken) return true;
            if (!Holds(target, out var grappler)) { Broken = true; Duration = 0; return true; }
            int tick = WorldClock.CurrentTick;
            if (LastAttemptTick == tick) return false;
            LastAttemptTick = tick;
            int mine = Rng.Next(1, 21) + StatUtils.GetModifier(target, "Strength");
            int theirs = 10 + StatUtils.GetModifier(grappler, "Strength");
            if (mine > theirs)
            {
                Record("GrappleBroken", target, grappler, mine, theirs);
                Broken = true; Duration = 0;
                MessageLog.Add(target.HasTag("Player") ? "You tear free of " + grappler.GetDisplayName() + "'s grip." : target.GetDisplayName() + " tears free.");
                return true;
            }
            Record("GrippedFast", target, grappler, mine, theirs);
            MessageLog.Add(target.HasTag("Player") ? "You strain against " + grappler.GetDisplayName() + "'s grip." : target.GetDisplayName() + " strains against the grip.");
            return false;
        }

        public override void OnTurnStart(Entity target)
        {
            if (Broken) return;
            if (!Holds(target, out var grappler)) { Broken = true; Duration = 0; return; }
            if (SqueezeDamage <= 0) return;
            Record("Squeezed", target, grappler, SqueezeDamage, 0);
            MessageLog.Add(grappler.GetDisplayName() + (target.HasTag("Player") ? " squeezes you." : " squeezes " + target.GetDisplayName() + "."));
            CombatSystem.ApplyDamage(target, SqueezeDamage, grappler, target.SpatialZone);
        }

        public override bool OnStack(Effect incoming) => true; // one grip at a time; the first holds

        static void Record(string kind, Entity bearer, Entity grappler, int a, int b)
        {
            if (Diag.IsChannelEnabled("ai")) Diag.Record("ai", kind, grappler, bearer, new { roll = a, against = b });
        }
    }
}
