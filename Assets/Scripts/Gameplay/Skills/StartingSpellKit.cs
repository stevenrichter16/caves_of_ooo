using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// One elemental primer per school, granted at character start.
    ///
    /// <para>The whole status system is built on the idea that two
    /// statuses beat one — soak a target, then shock it. A player who
    /// starts with a single element cannot discover that; the
    /// interaction only exists once they own two schools, which used to
    /// mean buying their way there before the game's central mechanic
    /// switched on. These four make the combination available on turn
    /// one.</para>
    ///
    /// <para><b>Why these four specifically.</b> Each is the cheapest
    /// active in its tree, so the set teaches the grammar rather than
    /// handing over the payoff:</para>
    /// <list type="bullet">
    /// <item><b>Ember Spit</b> (CD 8) — the cheap repeatable. Sets
    /// Burning, and refuses to ignite a soaked target, which is how the
    /// player learns water beats fire.</item>
    /// <item><b>Jet Blast</b> (CD 20) — soaks. The setup half of the
    /// headline combination.</item>
    /// <item><b>Ground Surge</b> (CD 30) — the payoff half: electricity
    /// through a wet target, and through wet ground.</item>
    /// <item><b>Rime Grip</b> (CD 35) — freezes, which is the setup for
    /// shatter damage later.</item>
    /// </list>
    ///
    /// <para>Deliberately NOT the heavy actives (Flame Jet, Pyroclasm,
    /// Overload). Those are the reward for investing in a tree; starting
    /// with them would flatten the whole progression.</para>
    ///
    /// <para>Static and engine-free so the grant is unit-testable, and
    /// pinned against the real skill classes by
    /// <c>StartingSpellKitTests</c> — renaming a skill breaks a test,
    /// not the kit silently. Same shape as
    /// <see cref="CavesOfOoo.Core.CraftingStarterKit"/>.</para>
    /// </summary>
    public static class StartingSpellKit
    {
        /// <summary>
        /// The four class names, one per element. Order is the order
        /// they land on the hotbar: fire, water, electric, cold.
        /// </summary>
        public static readonly string[] SpellClasses =
        {
            "Pyromancy_EmberSpit",   // fire     — CD 8
            // Migration: the two former StartingMutations move here as
            // the mutations are ported. FlamingHands landed with the
            // Pyromancy batch; Calm follows with Spellcraft.
            "Pyromancy_FlamingHands", // fire, point-blank — CD 10
            "Hydromancy_JetBlast",   // water    — CD 20
            "Galvanism_GroundSurge", // electric — CD 30
            "Cryomancy_RimeGrip",    // cold     — CD 35
        };

        /// <summary>
        /// Grant every spell in the kit to <paramref name="player"/>.
        /// Returns how many were actually added.
        ///
        /// <para>Already-known spells are skipped rather than
        /// duplicated: <c>SkillsPart.AddSkill</c> rejects a repeat by
        /// type, so re-running this on a loaded save is a no-op instead
        /// of an error.</para>
        /// </summary>
        public static int GrantAll(Entity player)
        {
            if (player == null) return 0;

            var skills = player.GetPart<SkillsPart>();
            if (skills == null) return 0;

            int granted = 0;
            for (int i = 0; i < SpellClasses.Length; i++)
            {
                if (skills.AddSkill(SpellClasses[i], source: "starting-kit"))
                    granted++;
            }

            return granted;
        }

        /// <summary>Which of the kit's spells this actor does NOT yet
        /// know. Used by the grant's own test and useful for a future
        /// "you have forgotten something" check.</summary>
        public static List<string> MissingFrom(Entity player)
        {
            var missing = new List<string>();
            var skills = player?.GetPart<SkillsPart>();
            if (skills == null)
            {
                missing.AddRange(SpellClasses);
                return missing;
            }

            for (int i = 0; i < SpellClasses.Length; i++)
            {
                if (!skills.HasSkill(SpellClasses[i]))
                    missing.Add(SpellClasses[i]);
            }

            return missing;
        }
    }
}
