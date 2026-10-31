using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// P4 (Docs/PLACE-WORK-AND-PROGRESSION.md §3 rule 3): the population floor of a
    /// zone. In a zone of tier T (<see cref="ZoneTierPolicy"/>), a hostile creature of
    /// tier below T-1 does not spawn: trivial threats do not belong in dangerous places.
    /// "Hostile" is the faction system's answer at generation time.
    /// Wildlife, non-creatures and blueprints the factory does not know always pass, and
    /// zones of tier 2 or less (or outside the world) are not filtered at all.
    /// </summary>
    public static class ZoneTierFloor
    {
        public const string WildlifeFaction = "Wildlife";

        struct Profile { public bool Threat; public int Tier; }
        static readonly Entity PlayerProbe = MakePlayerProbe();
        static Entity MakePlayerProbe() { var p = new Entity { BlueprintName = "Player" }; p.SetTag("Player"); return p; }
        static readonly ConditionalWeakTable<EntityFactory, Dictionary<string, Profile>> Profiles =
            new ConditionalWeakTable<EntityFactory, Dictionary<string, Profile>>();

        /// <summary>The admission predicate for a population roll in this zone, or null
        /// when the zone is not filtered.</summary>
        public static Func<string, bool> AdmitFor(Zone zone, EntityFactory factory)
        {
            int tier = ZoneTierPolicy.TierFor(zone?.ZoneID);
            if (tier < 3 || factory == null) return null;
            return blueprint => Admits(factory, blueprint, tier);
        }

        public static bool Admits(EntityFactory factory, string blueprint, int zoneTier)
        {
            if (zoneTier < 3 || factory == null || blueprint == null || !factory.Blueprints.ContainsKey(blueprint)) return true;
            var profile = ProfileOf(factory, blueprint);
            return !profile.Threat || profile.Tier >= zoneTier - 1;
        }

        static Profile ProfileOf(EntityFactory factory, string blueprint)
        {
            var cache = Profiles.GetOrCreateValue(factory);
            if (cache.TryGetValue(blueprint, out var known)) return known;
            Entity probe = null;
            try { probe = factory.CreateEntity(blueprint); } catch (Exception) { }
            var profile = new Profile
            {
                Threat = probe != null && probe.HasTag("Creature") && !probe.HasTag("Player")
                    && probe.GetTag("Faction") != WildlifeFaction && FactionManager.IsHostile(probe, PlayerProbe),
                Tier = ProgressionCurve.CreatureTier(probe),
            };
            cache[blueprint] = profile;
            return profile;
        }
    }
}
