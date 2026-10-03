using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Two bounded, additive cold-generation sites. Existing owners are never moved,
    /// harvested or restocked. Saved accepted graphs retain their actual crops and trader shelves.</summary>
    public static class SpreadExplorationResidents
    {
        public const string RoleKey = "SpreadResident.Role";
        public const string ReserveBedKey = "ConnectedSpread.ReserveBed";
        const int MaxTrials = 256;
        static readonly (int x, int y) Arrival = (40, 12);

        /// <summary>Stage one passive resident and its small functional site against the exact
        /// terrain/attempt authority. The returned proof is transient; it never replays on load.</summary>
        public static bool TryPlace(Zone zone, EntityFactory factory, SpreadCompositionBuilder terrain,
            SpreadExplorationFamily family, Func<bool> authority, out Entity[] owners, out Func<bool> final)
            => TryPlaceConnected(zone,factory,terrain,family,authority,out owners,out final,false);

        public static bool TryPlaceConnected(Zone zone, EntityFactory factory, SpreadCompositionBuilder terrain,
            SpreadExplorationFamily family, Func<bool> authority, out Entity[] owners, out Func<bool> final, bool connected, string worldKey = null)
        {
            owners = null; final = null;
            string stage="authority";
            bool Refuse(string reason){Diag.Record("worldgen","SpreadResidentRefused",payload:new{zoneId=zone?.ZoneID,reason,stage});return false;}
            connected = connected && (zone?.ZoneID == "Overworld.11.8.0" || zone?.ZoneID == "Overworld.12.11.0");
            bool plot = family == SpreadExplorationFamily.SeedKeepersPlot;
            if ((!plot && family != SpreadExplorationFamily.WaysideKitchen) || zone == null || factory == null || authority == null) return Refuse("admission");
            if (connected && !Guid.TryParseExact(worldKey, "N", out _)) return Refuse("world-key");
            var plan = terrain?.Plan;
            bool Identity() => terrain?.SourceZone == zone && terrain.Plan == plan && plan?.ZoneID == zone.ZoneID;
            if (!Identity() || !authority() || !Identity() || zone.GetReadOnlyEntities().Any(e => e.Properties.ContainsKey(RoleKey))) return Refuse("admission");
            // Preserve the legacy domestic-site guard. The two connected addresses
            // can coexist with an existing farmhouse only when Fits proves a separate
            // footprint and preserves its approaches and all original owners.
            if (!connected && zone.GetReadOnlyEntities().Any(e => e.HasTag("Creature") && e.HasPart<ConversationPart>())) return Refuse("admission");
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
            if (connected && !plot) specs[3] = ("Chair", "chair", 2, -1);
            if (connected)
                specs = specs.Concat(plot
                    ? new[] { ("MarlrootCrop", "reserve-crop", -2, -3), ("PitchpodCrop", "reserve-crop", -1, -3),
                        ("ConnectedReserveTray", "reserve-tray", 2, -2), ("Signpost", "reserve-sign", 1, -3) }
                    : new[] { ("ConnectedBatchPan", "pan", 0, -2), ("ConnectedKitchenEscrow", "escrow", 1, -2),
                        ("ConnectedKitchenPickup", "pickup", 2, -2), ("ClaspbeanCrop", "kitchen-crop", -2, 2) }).ToArray();
            if (specs.Any(s => !factory.Blueprints.ContainsKey(s.bp)) || !Current()) return Refuse("admission");
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
                    if (connected && specs.Select((spec, i) => (spec, i)).Where(s => s.spec.role == "reserve-crop" || s.spec.role == "kitchen-crop")
                        .Any(s => PreparedGround(zone, candidate[s.i], plan) == null)) continue;
                    if (++trials > MaxTrials) return Refuse("admission");
                    if (!Fits(zone, before, before, candidate, plot, at, turn, plan)) continue;
                    positions = candidate; origin = at; rotation = turn; break;
                }
                if (positions != null) break;
            }
            if (positions == null || !Current()) return Refuse("no-safe-footprint");
            var soils = new List<(Entity terrain, CultivatedSoilPart part, bool plantable)>();
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
                    if (!Current()) return Refuse("admission");
                    stage="create:"+spec.bp;
                    var entity = factory.CreateEntity(spec.bp);
                    if (!Fresh(entity, spec.bp) || !Current()) return Refuse("admission");
                    if (spec.role == "resident")
                    {
                        if (!Resident(entity, plot)) return Refuse("resident-payload");
                        entity.SetIntProperty(TraderRestockSystem.LastRestockProp, WorldClock.CurrentTick);
                        if (connected) entity.GetPart<RenderPart>().DisplayName = plot ? "Nella Tern, seedkeeper" : "Orven Kett, wayside cook";
                    }
                    if (spec.role == "crop" && !(entity.GetPart<CropPart>() is CropPart crop && crop.GrowthStage == 0 && crop.TicksInStage == 0 && crop.MoistureTicks == 0)) return Refuse("admission");
                    if (spec.role == "reserve-crop" || spec.role == "kitchen-crop")
                    {
                        var plant = entity.GetPart<CropPart>();
                        if (plant == null || !plant.HarvestAtMaturity || plant.YieldCount != 2 || plant.SeedYieldCount != 1) return Refuse("admission");
                        bool clayReady = SpreadExplorationPlan.Rank(plan.Seed, zone.ZoneID, "reserve-priority") % 2 == 0;
                        plant.GrowthStage = spec.role == "kitchen-crop" || (entity.BlueprintName == "MarlrootCrop") == clayReady ? 2 : 0;
                        plant.TicksInStage = 0; plant.MoistureTicks = 0;
                        entity.GetPart<RenderPart>().RenderString = plant.GlyphForStage(plant.GrowthStage).ToString();
                        entity.GetPart<RenderPart>().ColorString = plant.ColorForStage(plant.GrowthStage);
                    }
                    if (spec.role == "reserve-sign") Describe(entity, "Nella's tied row: marlroot for clay, pitchpod for resin. Ask Nella for gathering rights before harvesting these two cord-marked beds or taking from the matching tray. The four open beds remain public. Orven's kitchen lies southeast; he can introduce a dependable repairer. Dry seedlings need water before they can grow while you travel.");
                    if (spec.role == "cot" && !(entity.GetPart<BedPart>() is BedPart bed && string.IsNullOrEmpty(bed.Owner) && !bed.Occupied)) return Refuse("admission");
                    if (spec.role == "chair" && !entity.HasPart<ChairPart>()) return Refuse("admission");
                    if (spec.role == "shelter" && entity.GetPart<PhysicsPart>()?.Solid != true) return Refuse("admission");
                    if (spec.role == "oven")
                    {
                        // Ordinary Oven blueprints are scenery. Only this exact new site owner
                        // becomes an existing cooking station; resting is supplied by its cot.
                        if (entity.HasPart<CampfirePart>()) return Refuse("admission");
                        entity.AddPart(new CampfirePart { AllowRest = false, FiniteCooking = false });
                        Describe(entity, "A shared field oven. Stand beside it and Cook carried raw meat, mushrooms or emberwheat. The cot is for resting when no enemies are nearby.");
                    }
                    if (spec.role == "sign") Describe(entity, "Seeds wait in two open rows; the gaps are yours to plant. Carry a gladroot or emberwheat seed onto empty plantable ground and use Plant. Use a watering grimoire's Conjure Rain to moisten crops. Dry crops pause. Gladroot takes 40 moist rounds here; emberwheat takes 70 and needs another watering. Ripe produce falls ready to pick up. Watered crops keep growing while you travel and rest; dry time gives no growth.");
                    if (spec.role == "cot") Describe(entity, "An unclaimed field cot. Stand on it to sleep when no hostile is nearby. Rest heals and advances time; it does not water the growing rows.");
                    entity.Properties[RoleKey] = spec.role;
                    if (connected) entity.Properties["ConnectedSpread.Role"] = spec.role;
                    staged.Add(entity);
                }
                if (!UniqueGraph(zone, staged) || !Current()) return Refuse("staged-graph");
                stage="publication";
                for (int i = 0; i < staged.Count; i++)
                {
                    var entity = staged[i]; var at = positions[i];
                    if (!Current() || !placed.Values.All(proof => proof()) || !new SpreadWildernessSituationBuilder.Geometry(zone, added).Place(at.x, at.y)) return Refuse("admission");
                    if (!zone.AddEntity(entity, at.x, at.y)) return Refuse("admission");
                    added.Add(entity); placed.Add(entity, SpreadGenerationReceipt.CaptureFinalState(zone, new[] { entity }));
                    if (!Current() || !placed.Values.All(proof => proof())) return Refuse("admission");
                }
                stage="soil-and-binding";
                if (connected)
                {
                    if (!Current() || !placed.Values.All(proof => proof())) return Refuse("admission");
                    foreach (var plant in staged.Where(e => e.GetProperty(RoleKey) == "reserve-crop" || e.GetProperty(RoleKey) == "kitchen-crop"))
                    {
                        var cell = zone.GetEntityCell(plant);
                        var ground = PreparedGround(zone, (cell.X, cell.Y), plan);
                        if (ground == null || ground.HasPart<CultivatedSoilPart>() || BarrenGroundRules.IsBarren(cell)) return Refuse("admission");
                        var soil = new CultivatedSoilPart(); bool had = ground.HasTag("Plantable");
                        ground.SetTag("Plantable"); zone.NotifyEntityTagAdded(ground,"Plantable"); ground.AddPart(soil); soils.Add((ground,soil,had));
                        if (plot) ground.Properties[ReserveBedKey] = staged[0].ID;
                    }
                    // The only changes to original owners are our prepared-bed Parts/tags.
                    // Capture those synchronous writes only after the original proof held.
                    source = SpreadGenerationReceipt.CaptureFinalState(zone, original);
                    // These mutations call only sealed, owned binders, with no
                    // content factories or virtual behavior. Refresh exactly the
                    // changed owner's proof so a later refusal can reclaim it.
                    bool Bind(Entity owner, Func<bool> action)
                    {
                        if (!Current() || !placed.Values.All(proof => proof())) return false;
                        bool result;
                        try { result = action(); }
                        finally { placed[owner] = SpreadGenerationReceipt.CaptureFinalState(zone, new[] { owner }); }
                        return result && Current() && placed.Values.All(proof => proof());
                    }
                    if (plot)
                    {
                        if (soils.Count != 2 || !Bind(staged[0], () =>
                        {
                            var claim = new LocalGatheringClaimPart(); staged[0].AddPart(claim);
                            return claim.BindWorldKey(worldKey) && claim.Configure(zone, soils[0].terrain, soils[1].terrain,
                                staged.Single(e => e.GetProperty(RoleKey) == "reserve-tray"), plan.Seed);
                        })) return Refuse("reserve-binding");
                    }
                    else
                    {
                        var pan = staged.Single(e => e.GetProperty(RoleKey) == "pan");
                        if (!Bind(pan, () => pan.GetPart<KitchenBatchPart>().Configure(zone, staged[0],
                            staged.Single(e => e.GetProperty(RoleKey) == "escrow"), staged.Single(e => e.GetProperty(RoleKey) == "pickup"))
                            && ConnectedSpreadProgress.BindRepair(pan, worldKey) && ConnectedSpreadProgress.BindKitchen(pan, worldKey))) return Refuse("kitchen-binding");
                        if (!Bind(staged[0], () =>
                        {
                            var introduction = new CookIntroductionPart(); staged[0].AddPart(introduction);
                            return introduction.BindWorldKey(worldKey) && introduction.Configure(zone, pan, plan.Seed);
                        })) return Refuse("introduction-binding");
                    }
                    foreach (var e in staged) placed[e] = SpreadGenerationReceipt.CaptureFinalState(zone,new[]{e});
                }
                var packet = staged.ToArray(); var packetProof = SpreadGenerationReceipt.CaptureFinalState(zone, packet);
                // The generic field snapshot pins the collection identity;
                // this cold semantic contract also pins its initial members.
                bool Final() => Current() && packetProof()
                    && (!connected || !plot || staged[0].GetPart<LocalGatheringClaimPart>()?.Permissions?.Count == 0) && Fits(zone,
                    new SpreadWildernessSituationBuilder.Geometry(zone, new HashSet<Entity>(packet)), before, positions, plot, origin, rotation, plan);
                if (!Final()) return Refuse("final-proof");
                owners = packet; final = Final; success = true; return true;
            }
            catch (Exception error) { return Refuse(error.GetType().Name+":"+error.Message); }
            finally
            {
                LoadoutPart.Factory = oldLoadoutFactory; TraderPart.Factory = oldTraderFactory;
                LoadoutPart.Rng = oldLoadoutRng; TraderPart.Rng = oldTraderRng;
                // Callback-owned changes cannot be silently reclaimed. The caller rejects a
                // changed cold graph; only still-exact new owners are ours to roll back here.
                if (!success)
                {
                    foreach (var soil in soils)
                    {
                        soil.terrain.RemovePart(soil.part);
                        if (plot && soil.terrain.GetProperty(ReserveBedKey) == staged[0].ID) soil.terrain.Properties.Remove(ReserveBedKey);
                        if (!soil.plantable) { soil.terrain.Tags.Remove("Plantable"); zone.NotifyEntityTagRemoved(soil.terrain,"Plantable"); }
                    }
                    foreach (var entity in staged.AsEnumerable().Reverse())
                        if (placed.TryGetValue(entity, out var proof) && proof()) zone.RemoveEntity(entity);
                }
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
            var expected = plot ? new[] { "CandyCarrotSeed", "CandyCarrotSeed", "EmberwheatSeed", "EmberwheatSeed", "WateringGrimoire", "KnotflaxSeed", "HearthbulbSeed", "SeamleafSeed" }
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
        // Bare-ground classification also accepts harmless decorative Terrain
        // overlays. Cultivation belongs to the one actual planned ground owner,
        // never an overlay or the crop itself; ambiguous ground is not admitted.
        static Entity PreparedGround(Zone zone, (int x, int y) at, SpreadCompositionPlan plan)
        {
            var cell = zone.GetCell(at.x, at.y);
            if (cell == null || BarrenGroundRules.IsBarren(cell)) return null;
            var candidates = cell.Objects.Where(e => e.BlueprintName == plan.GroundAt(at.x, at.y)
                && e.HasTag("Plantable") && !e.HasTag("Crop") && !e.HasPart<CultivatedSoilPart>()
                && !e.Properties.ContainsKey(ReserveBedKey)
                && DoorPart.IsBareGround(e) && LocalGatheringClaims.Ground(e, zone)).Take(2).ToArray();
            return candidates.Length == 1 ? candidates[0] : null;
        }
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
