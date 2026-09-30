using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Two bounded, additive cold-generation sites. Existing owners are never moved,
    /// harvested or restocked. Saved accepted graphs retain their actual crops and trader shelves.</summary>
    public static class SpreadExplorationResidents
    {
        public const string RoleKey = "SpreadResident.Role";
        const int MaxTrials = 256;
        static readonly (int x, int y) Arrival = (40, 12);

        /// <summary>Stage one passive resident and its small functional site against the exact
        /// terrain/attempt authority. The returned proof is transient; it never replays on load.</summary>
        public static bool TryPlace(Zone zone, EntityFactory factory, SpreadCompositionBuilder terrain,
            SpreadExplorationFamily family, Func<bool> authority, out Entity[] owners, out Func<bool> final)
        {
            owners = null; final = null;
            bool plot = family == SpreadExplorationFamily.SeedKeepersPlot;
            if ((!plot && family != SpreadExplorationFamily.WaysideKitchen) || zone == null || factory == null || authority == null) return false;
            var plan = terrain?.Plan;
            bool Identity() => terrain?.SourceZone == zone && terrain.Plan == plan && plan?.ZoneID == zone.ZoneID;
            if (!Identity() || !authority() || !Identity() || zone.GetReadOnlyEntities().Any(e => e.Properties.ContainsKey(RoleKey))) return false;
            // A rolled farmhouse already supplies a domestic resident. Keep that landmark's
            // ordinary naming/stock lifecycle, rather than stacking a second household here.
            if (zone.GetReadOnlyEntities().Any(e => e.HasTag("Creature") && e.HasPart<ConversationPart>())) return false;
            var original = new HashSet<Entity>(zone.GetReadOnlyEntities());
            var source = SpreadGenerationReceipt.CaptureFinalState(zone, original);
            var added = new HashSet<Entity>();
            bool Sources() => Identity() && source() && original.Concat(added).ToHashSet().SetEquals(zone.GetReadOnlyEntities());
            bool Current() => authority() && Sources();
            (string bp, string role, int x, int y)[] specs = plot
                ? new[] { ("SpreadSeedKeeper", "resident", 0, 0), ("CandyCarrotCrop", "crop", -2, -1), ("CandyCarrotCrop", "crop", -1, -1),
                    ("EmberwheatCrop", "crop", -2, 1), ("EmberwheatCrop", "crop", -1, 1), ("Signpost", "sign", 2, 0) }
                : new[] { ("SpreadWaysideCook", "resident", 0, 0), ("Oven", "oven", -2, 0), ("Bed", "cot", 2, 0),
                    ("Chair", "chair", 2, 1), ("StoneWall", "shelter", -2, -2), ("StoneWall", "shelter", -1, -2) };
            if (specs.Any(s => !factory.Blueprints.ContainsKey(s.bp)) || !Current()) return false;
            var before = new SpreadWildernessSituationBuilder.Geometry(zone, new HashSet<Entity>());
            var anchor = (x: SpreadExplorationPlan.Rank(plan.Seed, zone.ZoneID, "resident-anchor") % 2 == 0 ? 22 : 58, y: 12);
            (int x, int y)[] positions = null; (int x, int y) origin = default; int rotation = 0, trials = 0;
            foreach (var at in from y in Enumerable.Range(4, Zone.Height - 8) from x in Enumerable.Range(4, Zone.Width - 8)
                orderby Distance(anchor, (x, y)), y, x select (x, y))
            {
                for (int turn = 0; turn < 2; turn++)
                {
                    var candidate = specs.Select(s => At(at, (s.x, s.y), turn)).ToArray();
                    if (candidate.Any(p => Distance(p, Arrival) <= 6 || !before.Place(p.x, p.y))) continue;
                    if (++trials > MaxTrials) return false;
                    if (!Fits(zone, before, before, candidate, plot, at, turn, plan)) continue;
                    positions = candidate; origin = at; rotation = turn; break;
                }
                if (positions != null) break;
            }
            if (positions == null || !Current()) return false;
            var staged = new List<Entity>(); var placed = new Dictionary<Entity, Func<bool>>(); bool success = false;
            // Stock and inherited loadout handlers execute synchronously during creation. They
            // share one private stream for this site and cannot advance the ordinary population RNG.
            var oldLoadoutFactory = LoadoutPart.Factory; var oldTraderFactory = TraderPart.Factory;
            var oldLoadoutRng = LoadoutPart.Rng; var oldTraderRng = TraderPart.Rng;
            try
            {
                LoadoutPart.Factory = TraderPart.Factory = factory;
                LoadoutPart.Rng = TraderPart.Rng = new Random(unchecked((int)SpreadExplorationPlan.Rank(plan.Seed, zone.ZoneID, "resident-stock")));
                foreach (var spec in specs)
                {
                    if (!Current()) return false;
                    var entity = factory.CreateEntity(spec.bp);
                    if (!Fresh(entity, spec.bp) || !Current()) return false;
                    if (spec.role == "resident")
                    {
                        if (!Resident(entity, plot)) return false;
                        entity.SetIntProperty(TraderRestockSystem.LastRestockProp, WorldClock.CurrentTick);
                    }
                    if (spec.role == "crop" && !(entity.GetPart<CropPart>() is CropPart crop && crop.GrowthStage == 0 && crop.TicksInStage == 0 && crop.MoistureTicks == 0)) return false;
                    if (spec.role == "cot" && !(entity.GetPart<BedPart>() is BedPart bed && string.IsNullOrEmpty(bed.Owner) && !bed.Occupied)) return false;
                    if (spec.role == "chair" && !entity.HasPart<ChairPart>()) return false;
                    if (spec.role == "shelter" && entity.GetPart<PhysicsPart>()?.Solid != true) return false;
                    if (spec.role == "oven")
                    {
                        // Ordinary Oven blueprints are scenery. Only this exact new site owner
                        // becomes an existing cooking station; resting is supplied by its cot.
                        if (entity.HasPart<CampfirePart>()) return false;
                        entity.AddPart(new CampfirePart { AllowRest = false, FiniteCooking = false });
                        Describe(entity, "A shared field oven. Stand beside it and Cook carried raw meat, mushrooms or emberwheat. The cot is for resting when no enemies are nearby.");
                    }
                    if (spec.role == "sign") Describe(entity, "Seeds wait in two open rows; the gaps are yours to plant. Carry a gladroot or emberwheat seed onto empty plantable ground and use Plant. Use a watering grimoire's Conjure Rain to moisten crops. Dry crops pause. Gladroot takes 40 moist rounds here; emberwheat takes 70 and needs another watering. Ripe produce falls ready to pick up. Crops grow while you act in this area, not while you travel elsewhere.");
                    if (spec.role == "cot") Describe(entity, "An unclaimed field cot. Stand on it to sleep when no hostile is nearby. Rest heals and advances time; it does not water the growing rows.");
                    entity.Properties[RoleKey] = spec.role; staged.Add(entity);
                }
                if (!UniqueGraph(zone, staged) || !Current()) return false;
                for (int i = 0; i < staged.Count; i++)
                {
                    var entity = staged[i]; var at = positions[i];
                    if (!Current() || !placed.Values.All(proof => proof()) || !new SpreadWildernessSituationBuilder.Geometry(zone, added).Place(at.x, at.y)) return false;
                    if (!zone.AddEntity(entity, at.x, at.y)) return false;
                    added.Add(entity); placed.Add(entity, SpreadGenerationReceipt.CaptureFinalState(zone, new[] { entity }));
                    if (!Current() || !placed.Values.All(proof => proof())) return false;
                }
                var packet = staged.ToArray(); var packetProof = SpreadGenerationReceipt.CaptureFinalState(zone, packet);
                bool Final() => Current() && packetProof() && Fits(zone,
                    new SpreadWildernessSituationBuilder.Geometry(zone, new HashSet<Entity>(packet)), before, positions, plot, origin, rotation, plan);
                if (!Final()) return false;
                owners = packet; final = Final; success = true; return true;
            }
            catch (Exception) { return false; }
            finally
            {
                LoadoutPart.Factory = oldLoadoutFactory; TraderPart.Factory = oldTraderFactory;
                LoadoutPart.Rng = oldLoadoutRng; TraderPart.Rng = oldTraderRng;
                // Callback-owned changes cannot be silently reclaimed. The caller rejects a
                // changed cold graph; only still-exact new owners are ours to roll back here.
                if (!success) foreach (var entity in staged.AsEnumerable().Reverse())
                    if (placed.TryGetValue(entity, out var proof) && proof()) zone.RemoveEntity(entity);
            }
        }

        static bool Fresh(Entity e, string blueprint) => e != null && e.BlueprintName == blueprint && !string.IsNullOrEmpty(e.ID) && e.SpatialZone == null
            && e.GetPart<PhysicsPart>() is PhysicsPart p && p.InInventory == null && p.Equipped == null
            && e.GetPart<RenderPart>()?.Visible == true && !e.HasPart<SpatialFootprintPart>() && e.Parts.All(part => part != null && part.ParentEntity == e);
        static bool Resident(Entity e, bool plot)
        {
            var brain = e.GetPart<BrainPart>(); var trader = e.GetPart<TraderPart>(); var inventory = e.GetPart<InventoryPart>();
            if (!e.HasTag("Creature") || e.HasTag("Player") || e.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(e)
                || brain == null || !brain.Passive || brain.Target != null || brain.PartyLeader != null || brain.PartyMembers.Count != 0 || brain.HasGoalOtherThan("BoredGoal")
                || e.GetPart<ConversationPart>() == null || trader?.StockTable != (plot ? "SeedKeeperStock" : "WaysideCookStock")
                || inventory == null || inventory.EquippedItems.Count != 0 || TradeSystem.GetDrams(e) != (plot ? 40 : 35)) return false;
            var expected = plot ? new[] { "CandyCarrotSeed", "CandyCarrotSeed", "EmberwheatSeed", "EmberwheatSeed", "WateringGrimoire" }
                : new[] { "RawMeat", "Mushroom", "Emberwheat", "DriedMeat", "ToastedEmberwheat" };
            var actual = new List<string>();
            foreach (var item in inventory.Objects)
            {
                int count = item?.GetPart<StackerPart>()?.StackCount ?? 1;
                if (item == null || count < 1 || count > expected.Length || actual.Count + count > expected.Length) return false;
                for (int i = 0; i < count; i++) actual.Add(item.BlueprintName);
            }
            return actual.OrderBy(s => s, StringComparer.Ordinal).SequenceEqual(expected.OrderBy(s => s, StringComparer.Ordinal));
        }
        static void Describe(Entity e, string text)
        { var part = e.GetPart<ExaminablePart>(); if (part == null) { part = new ExaminablePart(); e.AddPart(part); } part.Text = text; }
        static bool UniqueGraph(Zone zone, IEnumerable<Entity> roots)
        {
            var occupied = new HashSet<string>(); var originalSeen = new HashSet<Entity>(); var original = new Queue<Entity>(zone.GetReadOnlyEntities());
            while (original.Count > 0)
            {
                var e = original.Dequeue(); if (e == null || !originalSeen.Add(e)) continue; occupied.Add(e.ID);
                foreach (var child in Children(e)) original.Enqueue(child);
            }
            var seen = new HashSet<Entity>(); var pending = new Queue<Entity>(roots);
            while (pending.Count > 0)
            {
                var e = pending.Dequeue();
                if (e == null || !seen.Add(e) || seen.Count > 64 || string.IsNullOrEmpty(e.ID) || !occupied.Add(e.ID) || e.SpatialZone != null || e.Parts.Any(p => p == null || p.ParentEntity != e)) return false;
                foreach (var item in e.GetPart<InventoryPart>()?.Objects ?? Enumerable.Empty<Entity>())
                { if (item?.GetPart<PhysicsPart>()?.InInventory != e || item.GetPart<PhysicsPart>().Equipped != null) return false; pending.Enqueue(item); }
                if (e.GetPart<ContainerPart>()?.Contents.Count > 0 || e.GetPart<Body>()?.GetParts().Any(p => p.Equipped != null) == true) return false;
            }
            return true;
        }
        static IEnumerable<Entity> Children(Entity e) => (e.GetPart<InventoryPart>()?.Objects ?? Enumerable.Empty<Entity>())
            .Concat(e.GetPart<InventoryPart>()?.EquippedItems.Values ?? Enumerable.Empty<Entity>())
            .Concat(e.GetPart<ContainerPart>()?.Contents ?? Enumerable.Empty<Entity>())
            .Concat(e.GetPart<Body>()?.GetParts().Where(p => p.Equipped != null).Select(p => p.Equipped) ?? Enumerable.Empty<Entity>());
        static int Distance((int x, int y) a, (int x, int y) b) => Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        static (int x, int y) At((int x, int y) origin, (int x, int y) offset, int turn) => turn == 0 ? (origin.x + offset.x, origin.y + offset.y) : (origin.x - offset.y, origin.y + offset.x);
        static bool Plantable(Zone zone, (int x, int y) p) => zone.GetCell(p.x, p.y) is Cell cell && !BarrenGroundRules.IsBarren(cell) && cell.Objects.Any(e => e.HasTag("Terrain") && e.HasTag("Plantable"));
        static bool Fits(Zone zone, SpreadWildernessSituationBuilder.Geometry geometry, SpreadWildernessSituationBuilder.Geometry before,
            (int x, int y)[] positions, bool plot, (int x, int y) origin, int rotation, SpreadCompositionPlan plan)
        {
            if (positions.Any(p => Distance(p, Arrival) <= 6 || !geometry.Place(p.x, p.y)) || !geometry.PreservesAgainst(before, positions)) return false;
            // Keep all existing creatures outside the opening footprint. This is deliberately
            // conservative for a friendly site, not a promise that roaming threats stay away.
            if (zone.GetReadOnlyEntities().Any(e => e.HasTag("Creature") && e.GetProperty(RoleKey) != "resident" && positions.Any(p => Distance(p, zone.GetEntityPosition(e)) <= 3))) return false;
            if (plot)
            {
                for (int i = 1; i <= 4; i++) if (!Plantable(zone, positions[i])) return false;
                foreach (var offset in new[] { (0, -1), (0, 1) })
                { var p = At(origin, offset, rotation); if (!geometry.Place(p.x, p.y) || !Plantable(zone, p)) return false; }
            }
            var blocked = new HashSet<(int x, int y)>(positions); var reached = geometry.Flood(blocked, Arrival, int.MaxValue, null);
            if (!reached[Arrival.x, Arrival.y]) return false;
            foreach (var p in new[] { (x: 0, y: plan.WestY), (x: Zone.Width - 1, y: plan.EastY), (x: plan.NorthX, y: 0), (x: plan.SouthX, y: Zone.Height - 1) })
                if (before.BorderReach[p.x, p.y] && !reached[p.x, p.y]) return false;
            for (int i = 0; i < positions.Length; i++)
            {
                if (!plot && i >= 4) continue;
                var p = positions[i];
                if (new[] { (p.x - 1, p.y), (p.x + 1, p.y), (p.x, p.y - 1), (p.x, p.y + 1) }.Count(n => geometry.Walk(n.Item1, n.Item2, blocked) && reached[n.Item1, n.Item2]) < 2) return false;
            }
            return true;
        }
    }
}
