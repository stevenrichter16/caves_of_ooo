using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Installs a fresh physical Overwrit surface. No procedural
    /// bleed, inherited ruin scatter, or repair of existing native owners.</summary>
    public sealed class OverwritCompositionBuilder : IZoneBuilder
    {
        public string Name => "OverwritComposition";
        public int Priority => 2000;
        public OverwritCompositionPlan Plan { get; private set; }
        private readonly int seed;
        public OverwritCompositionBuilder(int worldSeed) { seed = worldSeed; }

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            Plan = null;
            if (zone == null || factory == null || rng == null)
                return Reject(zone, "missing-input");
            if (zone.EntityCount != 0) return Reject(zone, "nonempty-zone");
            if (!OverwritCompositionPlan.IsWildernessZone(zone.ZoneID)) return Reject(zone, "ineligible-zone");
            var plan = OverwritCompositionPlan.Create(zone.ZoneID, seed);
            var required = new HashSet<string> { "OverwritGround" };
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                if (plan.ObjectAt(x, y) != null) required.Add(plan.ObjectAt(x, y));
            foreach (string bp in required)
                if (factory.Blueprints == null || !factory.Blueprints.ContainsKey(bp)) return Reject(zone, "missing-blueprint:" + bp);

            // Factory creation can fail softly. Stage all instances before
            // mutating cells/reservations, not just one prototype per family.
            var staged = new List<(Entity owner, int x, int y)>(Zone.Width * Zone.Height + 40);
            try
            {
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                {
                    var ground = factory.CreateEntity("OverwritGround");
                    string problem = ContractViolation(ground, "OverwritGround");
                    if (problem != null) return Reject(zone, "OverwritGround:" + problem);
                    staged.Add((ground, x, y));
                    string bp = plan.ObjectAt(x, y); if (bp == null) continue;
                    var owner = factory.CreateEntity(bp);
                    problem = ContractViolation(owner, bp);
                    if (problem != null) return Reject(zone, bp + ":" + problem);
                    staged.Add((owner, x, y));
                }
            }
            catch (Exception) { return Reject(zone, "factory-creation"); }

            foreach (var e in staged) zone.AddEntity(e.owner, e.x, e.y);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
            {
                if (plan.IsApproach(x, y)) zone.GenReservedCells.Add((x, y));
                string bp = plan.ObjectAt(x, y);
                if (bp != "OverwritPilgrimBench" && bp != "OverwritWaymarker") continue;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    zone.GenReservedCells.Add((x + dx, y + dy));
            }
            Plan = plan;
            Diag.Record("worldgen", "OverwritCompositionPlanned", payload: new {
                zoneId = zone.ZoneID, seed, role = plan.Role.ToString(), formation = plan.Formation.ToString(),
                objects = staged.Count - Zone.Width * Zone.Height });
            return true;
        }

        // Non-null is not sufficient: the factory skips missing Parts and
        // unknown parameter names. AR10's native corruption sweep caught 27
        // ways that a false success could commit invisible or broken terrain.
        // Validate these exact staged instances; do not create replacements or
        // detach/reassign their native identity while checking the contract.
        private static string ContractViolation(Entity owner, string blueprint)
        {
            if (owner == null || owner.BlueprintName != blueprint) return "owner-identity";
            bool ground = blueprint == "OverwritGround";
            bool growth = blueprint == "OverwritNewGrowth";
            bool marker = blueprint == "OverwritWaymarker";
            bool bench = blueprint == "OverwritPilgrimBench";
            if (!ground && !growth && !marker && !bench) return "unknown-contract";

            var render = owner.GetPart<RenderPart>();
            string glyph = ground ? "." : growth ? "," : marker ? "|" : "_";
            if (render == null || !render.Visible || render.RenderString != glyph
                || string.IsNullOrWhiteSpace(render.DisplayName) || !string.IsNullOrEmpty(render.GlyphVariants)
                || render.RenderLayer != (ground ? 0 : 1)) return "render-contract";
            var physics = owner.GetPart<PhysicsPart>();
            if (physics == null || physics.Takeable || physics.Solid != (marker || bench)
                || ((ground || growth) && owner.HasTag("Solid"))) return "physics-contract";
            var examine = owner.GetPart<ExaminablePart>();
            if (examine == null || string.IsNullOrWhiteSpace(examine.Text)) return "examine-contract";
            if (owner.HasPart<LiquidPoolPart>() || owner.HasPart<TileStateSourcePart>()
                || owner.HasPart<HarvestablePart>() || owner.HasPart<BrainPart>()
                || owner.HasTag("Creature") || owner.Statistics.ContainsKey("Hitpoints")
                || owner.HasPart("Restable") || owner.HasTag("Restable")) return "unexpected-mechanic";
            if (ground) return null;

            var destruction = owner.GetPart<DestructiblePart>();
            if (destruction == null || destruction.Indestructible || destruction.Gone
                || destruction.HP <= 0 || destruction.MaxHP < destruction.HP
                || destruction.Hardness < 0) return "destruction-contract";
            if (marker ? destruction.WreckageBlueprint != "Rubble" : !string.IsNullOrEmpty(destruction.WreckageBlueprint))
                return "wreckage-contract";
            var material = owner.GetPart<MaterialPart>();
            if (material == null || material.MaterialID != (marker ? "Stone" : growth ? "Plant" : "Wood")
                || !Finite(material.Combustibility)) return "material-contract";
            if (marker)
                return material.Combustibility == 0 && material.HasMaterialTag("Stone") && material.HasMaterialTag("Mineral")
                    ? null : "stone-contract";
            if (material.Combustibility <= 0 || !material.HasMaterialTag("Organic")
                || !material.HasMaterialTag(growth ? "Plant" : "Wood")) return "flammability-contract";
            var thermal = owner.GetPart<ThermalPart>();
            if (thermal == null || !Finite(thermal.HeatCapacity) || thermal.HeatCapacity <= 0
                || !Finite(thermal.FlameTemperature) || !Finite(thermal.Temperature)
                || thermal.FlameTemperature <= thermal.Temperature || !Finite(thermal.AmbientDecayRate)
                || thermal.AmbientDecayRate <= 0 || thermal.AmbientDecayRate > 1) return "thermal-contract";
            return null;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool Reject(Zone zone, string reason)
        { Diag.Record("worldgen", "OverwritCompositionRejected", payload: new { zoneId = zone?.ZoneID, reason }); return false; }
    }
}
