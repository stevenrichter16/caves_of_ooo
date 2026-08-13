namespace CavesOfOoo.Core
{
    /// <summary>
    /// What an elemental ability is allowed to hit.
    ///
    /// <para><b>Why this exists.</b> Every ability family grew its own
    /// target filter, and all of them said the same thing:
    /// <c>if (!e.Tags.ContainsKey("Creature")) continue;</c>. That is
    /// correct for a weapon skill — you cannot disarm a barrel — but it is
    /// wrong for fire and lightning, which is why a fire bolt aimed at a
    /// tree reported "sputters out" and a shock into a brine pool "finds
    /// no conductors".</para>
    ///
    /// <para><b>Why the rule is not simply "breakable".</b> That was the
    /// obvious answer and it is wrong: a <c>BrinePool</c> has no
    /// <c>PhysicsPart</c> and no <see cref="DestructiblePart"/> — you
    /// cannot break a puddle — but it is 95%-conductive salt water and
    /// electrifying it is the whole point of the water/shock grammar. So
    /// the rule is "takes part in the world's physical simulation": it has
    /// structural HP, or a material, or a temperature. Anything with one
    /// of those can be meaningfully burned, frozen, shocked or corroded;
    /// <see cref="ObjectStatusMatrix"/> then decides which of those
    /// actually mean anything for the material in question.</para>
    ///
    /// <para>Deliberately NOT used by the weapon skills (Disarm, Backstab,
    /// Flurry, Lunge, Vault, Tumble). Those stay creature-only, because
    /// "backstab a wall" has no meaning to define. Objects are reachable
    /// by melee through the bump-to-break path and the interact menu's
    /// Break row instead.</para>
    /// </summary>
    public static class AbilityTargeting
    {
        /// <summary>
        /// Can an elemental ability meaningfully hit this?
        /// </summary>
        /// <param name="candidate">The thing in the cell.</param>
        /// <param name="actor">The caster, who is never their own target.</param>
        public static bool IsElementalTarget(Entity candidate, Entity actor)
        {
            if (candidate == null || candidate == actor) return false;
            if (candidate.HasTag("Creature")) return true;

            // Structural HP — a tree, a barrel, a wall.
            var structural = candidate.GetPart<DestructiblePart>();
            if (structural != null && !structural.Gone) return true;

            // No HP of any kind, but it takes part in the material sim: a
            // pool, a seep, a patch of ice. Damage will land nowhere (see
            // DestructionSystem.RouteDamage) but a status can.
            return candidate.GetPart<MaterialPart>() != null
                || candidate.GetPart<ThermalPart>() != null;
        }

        /// <summary>
        /// True when <paramref name="candidate"/> is a creature — the
        /// stricter rule the weapon skills keep. Present here so the two
        /// rules sit side by side and a reader can see that the difference
        /// is deliberate.
        /// </summary>
        public static bool IsCreatureTarget(Entity candidate, Entity actor)
            => candidate != null && candidate != actor && candidate.HasTag("Creature");
    }
}
