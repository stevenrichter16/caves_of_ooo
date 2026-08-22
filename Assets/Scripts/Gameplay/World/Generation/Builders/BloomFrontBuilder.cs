using System;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W4.4 SM-D (Docs/FELLING-W4-PLAN.md §6) — the Bloom-front: a
    /// zone-level condition on a few wilderness Grovelands zones where
    /// the Driving Bloom is further along. Seeds blooming fruiting
    /// bodies (burn one and it burns off bloom-spores — the peat
    /// lesson, rhymed), takes 1-2 of the zone's existing creatures as
    /// driven hosts, and leaves the unfinished workshop: tools down
    /// mid-task.
    ///
    /// <para><b>Membership is a pure function of the zone id</b> —
    /// FormationSelector.StableIndex over a salted key, NEVER the
    /// builder rng: pipeline-retry reseeding
    /// (ZoneGenerationPipeline.cs:47) would make front membership
    /// non-deterministic, and a pure function needs no Zone field and
    /// no save plumbing. The salt decorrelates the roll from the
    /// formation pick, which hashes the bare id.</para>
    ///
    /// <para><b>R8 — where the front may not land.</b> POI zones
    /// (villages incl. Cinderhold, lairs, river chunks, merchant
    /// camps) are excluded STRUCTURALLY: this builder is registered
    /// only in GetPipelineForZone's Grovelands WILDERNESS arm (poi ==
    /// null) — builders cannot reach WorldMap.POIs, so the exclusion
    /// lives at pipeline assembly. GroveShrine is not a POI (an
    /// ambient LandmarkBuilder stamp at priority 3800), so this
    /// builder runs later and stands down when any of the five
    /// shrine keepers is present.</para>
    ///
    /// <para>Priority 4300: after LandmarkBuilder (3800 — shrines
    /// exist to be checked) and PopulationBuilder (4000 — creatures
    /// exist to be taken), beside HouseDramaZoneBuilder's 4500
    /// precedent for post-population roster edits.</para>
    /// </summary>
    public sealed class BloomFrontBuilder : IZoneBuilder
    {
        public string Name => "BloomFront";
        public int Priority => 4300;

        /// <summary>Percent of wilderness Grovelands zones the front
        /// holds. Low: a front is a place you talk about finding.</summary>
        public const int FrontChancePercent = 8;

        public const int FruitingMin = 6;
        public const int FruitingMax = 10;
        public const int HostMax = 2;

        /// <summary>The five named Choir keepers the GroveShrine stamp
        /// places (LandmarkBuilder.GroveShrineStamp) — their presence
        /// vetoes the front (R8).</summary>
        public static readonly string[] ShrineKeepers =
            { "Mogu", "Grib", "Nam", "Sien", "Sopp" };

        /// <summary>Test override: force the front on/off regardless of
        /// the zone-id roll. Null = roll normally.</summary>
        public bool? Override;

        /// <summary>Pure membership roll — deterministic across
        /// sessions, retries, and builder-order changes.</summary>
        public static bool IsBloomFront(string zoneID)
        {
            if (string.IsNullOrEmpty(zoneID)) return false;
            return FormationSelector.StableIndex("BloomFront|" + zoneID, 100)
                < FrontChancePercent;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;
            bool isFront = Override ?? IsBloomFront(zone.ZoneID);
            if (!isFront) return true;

            // R8 content-level veto: where the Choir keeps a shrine, the
            // front does not land (a Bloomed shrine is a whole design
            // conversation, not a roll).
            foreach (var e in zone.GetAllEntities())
            {
                for (int i = 0; i < ShrineKeepers.Length; i++)
                {
                    if (e.BlueprintName != ShrineKeepers[i]) continue;
                    if (Diag.IsChannelEnabled("worldgen"))
                        Diag.Record("worldgen", "BloomFrontSkipped", null, e,
                            new { zoneId = zone.ZoneID, reason = "shrine",
                                  keeper = e.BlueprintName });
                    return true;
                }
            }

            // The growth. Solid placements use the all-8-neighbors-open
            // rule: with 8-connectivity, a cell whose full ring is open
            // is never a cut vertex — sealing it cannot disconnect the
            // zone, so no flood-fill repair pass is needed (the
            // Grovelands reachability lesson, solved locally).
            int target = FruitingMin + rng.Next(FruitingMax - FruitingMin + 1);
            int fruiting = 0;
            for (int attempt = 0; attempt < 300 && fruiting < target; attempt++)
            {
                int x = 2 + rng.Next(Zone.Width - 4);
                int y = 2 + rng.Next(Zone.Height - 4);
                if (!IsSafeSolidSite(zone, x, y)) continue;
                if (BuilderSpawn.TryPlaceOnce(zone, factory, "BloomingFruitingBody", x, y) != null)
                    fruiting++;
            }

            // The hosts: 1-2 of the zone's existing creatures, already
            // worn. Only bodies with a will to override (BrainPart).
            // No StatusEffectsPart filter (close-out 🔴 #2): no blueprint
            // ships that part — Entity.ApplyEffect creates it lazily —
            // so requiring it up front rejected EVERY live creature and
            // fronts generated with hosts=0.
            var candidates = new System.Collections.Generic.List<Entity>();
            foreach (var e in zone.GetAllEntities())
            {
                if (!e.Tags.ContainsKey("Creature")) continue;
                if (e.GetPart<BrainPart>() == null) continue;
                if (e.GetStatValue("Hitpoints", 0) <= 0) continue;
                if (e.HasEffect<BloomedEffect>()) continue;
                candidates.Add(e);
            }
            int hostTarget = 1 + rng.Next(HostMax);
            int hosts = 0;
            int candidateCount = candidates.Count;
            while (hosts < hostTarget && candidates.Count > 0)
            {
                var pick = candidates[rng.Next(candidates.Count)];
                candidates.Remove(pick);
                pick.ApplyEffect(new BloomedEffect());
                if (pick.HasEffect<BloomedEffect>()) hosts++; // count what actually took
            }

            // The unfinished workshop: tools down mid-task. An anvil
            // (solid — same safe-site rule) with the hammer left beside
            // it on the ground.
            bool workshop = false;
            for (int attempt = 0; attempt < 120 && !workshop; attempt++)
            {
                int x = 3 + rng.Next(Zone.Width - 6);
                int y = 3 + rng.Next(Zone.Height - 6);
                if (!IsSafeSolidSite(zone, x, y)) continue;
                if (BuilderSpawn.TryPlaceOnce(zone, factory, "SmithAnvil", x, y) == null) continue;
                BuilderSpawn.TryPlaceOnce(zone, factory, "Warhammer", x + 1, y);
                workshop = true;
            }

            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "BloomFront", null, null,
                    new { zoneId = zone.ZoneID, fruiting, fruitingTarget = target,
                          hosts, hostTarget, candidateCount, workshop });
            return true;
        }

        /// <summary>A cell is a safe site for a solid iff it is open and
        /// its full 8-ring is open: removing such a cell from the walk
        /// graph can never disconnect it (any route through the center
        /// re-routes along the ring), so no reachability repair is
        /// needed afterward. Public: a pure geometric predicate, and
        /// the safe-site contract is pinned from the test assembly.</summary>
        public static bool IsSafeSolidSite(Zone zone, int x, int y)
        {
            // BlocksMovement, not tag-only IsPassable: GroveSign,
            // SmithAnvil, and haulables are Part-solid with NO Solid
            // tag — tag-only checks would bury furniture and void the
            // no-disconnect proof (close-out 🟡; the W4.1
            // ConnectivityBuilder lesson recurring).
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.BlocksMovement()) return false;
            for (int ox = -1; ox <= 1; ox++)
                for (int oy = -1; oy <= 1; oy++)
                {
                    if (ox == 0 && oy == 0) continue;
                    var n = zone.GetCell(x + ox, y + oy);
                    if (n == null || n.BlocksMovement()) return false;
                }
            return true;
        }
    }
}
