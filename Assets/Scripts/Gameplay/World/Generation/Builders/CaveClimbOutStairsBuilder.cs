using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Extra ways up on ordinary cave floors (user direction 2026-10-29: "there needs to be
    /// more stairs up in the caves below the surface level"). Before this, a cave floor's only
    /// up-stair came from a down-stair registered by the zone above, so most first-floor zones
    /// reached sideways had none and no floor had more than one.
    ///
    /// <para>After the paired stairs are placed, each horizontal third of the floor that has no
    /// up-stair gets one, on open floor, chosen by the zone's own generation RNG (deterministic
    /// per world, from its own seed). These carry <see cref="MarkerProperty"/>: taking one leads to the zone above
    /// and, where that zone has no way down yet, opens one where the climber arrives
    /// (<see cref="ZoneTransitionSystem"/>). Only the ordinary underground pipeline adds this
    /// builder; authored sinkholes, lairs and special floors keep their own stairs.</para>
    /// Priority 3550: after StairsUp/StairsDown (3500), before StairConnector (3600).
    /// </summary>
    public class CaveClimbOutStairsBuilder : IZoneBuilder
    {
        public const string MarkerProperty = "CaveClimbOut";
        public const int Thirds = 3;
        public string Name => "CaveClimbOutStairsBuilder";
        public int Priority => 3550;
        readonly int worldSeed;

        public CaveClimbOutStairsBuilder(int worldSeed) { this.worldSeed = worldSeed; }

        /// <summary>Uses its own sequence seeded by world and zone, never the pipeline RNG,
        /// so every later builder on the floor (population, hazards, crops) generates exactly
        /// what it did before this builder existed.</summary>
        public bool BuildZone(Zone zone, EntityFactory factory, System.Random pipelineRng)
        {
            var rng = new System.Random(StableSeed(worldSeed, zone?.ZoneID));
            if (zone == null || factory == null || !factory.Blueprints.ContainsKey("StairsUp")) return true;
            if (WorldMap.GetDepth(zone.ZoneID) < 1 || WorldMap.GetZoneAbove(zone.ZoneID) == null) return true;
            var covered = new bool[Thirds];
            foreach (var e in zone.GetAllEntities())
                if (e.HasPart<StairsUpPart>()) covered[ThirdOf(zone.GetEntityPosition(e).x)] = true;
            var candidates = new List<(int x, int y)>();
            for (int third = 0; third < Thirds; third++)
            {
                if (covered[third]) continue;
                candidates.Clear();
                for (int x = 1; x < Zone.Width - 1; x++)
                {
                    if (ThirdOf(x) != third) continue; // one rule for "which third" everywhere
                    for (int y = 1; y < Zone.Height - 1; y++)
                        if (Open(zone.GetCell(x, y))) candidates.Add((x, y));
                }
                if (candidates.Count == 0) continue;
                var (cx, cy) = candidates[rng.Next(candidates.Count)];
                var stairs = factory.CreateEntity("StairsUp");
                if (stairs == null) continue;
                stairs.SetIntProperty(MarkerProperty, 1);
                if (!zone.AddEntity(stairs, cx, cy)) continue;
            }
            return true;
        }

        static int StableSeed(int seed, string id)
        {
            unchecked
            {
                uint h = 2166136261u ^ (uint)seed;
                if (id != null) foreach (char c in id) { h ^= c; h *= 16777619u; }
                h ^= 0x9e3779b9u; return (int)(h & 0x7fffffff);
            }
        }

        internal static int ThirdOf(int x) => System.Math.Min(Thirds - 1, System.Math.Max(0, x * Thirds / Zone.Width));

        static bool Open(Cell cell)
        {
            if (cell == null || !cell.IsPassable()) return false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var o = cell.Objects[i];
                if (o.HasPart<StairsUpPart>() || o.HasPart<StairsDownPart>() || o.HasTag("StairsUp") || o.HasTag("StairsDown")) return false;
            }
            return true;
        }

        /// <summary>True for an up-stair this builder placed.</summary>
        public static bool IsClimbOut(Entity e) => e != null && e.HasPart<StairsUpPart>() && e.GetIntProperty(MarkerProperty) == 1;
    }
}
