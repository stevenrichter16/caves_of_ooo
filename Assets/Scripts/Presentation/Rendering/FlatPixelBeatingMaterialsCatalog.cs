using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Visible static materials for current ordinary Beating wilderness.
    /// Borrows immutable textures and the current-owner selector; owns only cached
    /// tiles/sprites. Never generates, advances state or widens opening admission.</summary>
    public sealed class FlatPixelBeatingMaterialsCatalog : IDisposable
    {
        // Wilderness by default; a separate instance may admit its biome's ordinary special sites.
        System.Func<Zone, bool> admits = IsSupportedZone;
        /// <summary>Make this instance draw its biome's ordinary sinkhole surfaces
        /// (FlatPixelSiteAdmission) instead of the wilderness, with exactly the same rules and art.</summary>
        internal FlatPixelBeatingMaterialsCatalog AdmittingSites(BiomeType biome)
        { admits = zone => FlatPixelSiteAdmission.IsOrdinarySite(zone, biome); return this; }

        public const string ResourcePath = "Sprites/Live16x24/beating-materials";
        public const int FrameCount = 34;
        /// <summary>Independent optional two-state TarSeep atlas: frame 0 stocked,
        /// frame 1 empty stock. Both states stay oily; neither is dry or safe.</summary>
        public const string TarResourcePath = "Sprites/Live16x24/tar-seep";
        const int Width = 16, Height = 24;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] materials, vegetation, anchors, tar, masonry;
        bool disposed;
        static readonly Dictionary<string, Vector2Int> coordinates = BuildCoordinates();

        static Dictionary<string, Vector2Int> BuildCoordinates()
        {
            var result = new Dictionary<string, Vector2Int>(StringComparer.Ordinal);
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    string id = WorldMap.ToZoneID(x, y);
                    if (BeatingCompositionPlan.IsWildernessZone(id)) result.Add(id, new Vector2Int(x, y));
                }
            return result;
        }

        public FlatPixelBeatingMaterialsCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.VegetationResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath),
                Resources.Load<Texture2D>(TarResourcePath),
                Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath), borrowedSelector) { }

        /// <summary>Independent injected groups never reload Resources on failure.
        /// No borrowed texture or selector is changed or disposed.</summary>
        public FlatPixelBeatingMaterialsCatalog(Texture2D atlas34, Texture2D vegetation4,
            Texture2D anchors12, FlatPixelFallbackCatalog borrowedSelector)
            : this(atlas34, vegetation4, anchors12, null, borrowedSelector) { }

        /// <summary>The tar atlas is a separate optional group: missing or
        /// malformed tar leaves every other material, the oil mark and the
        /// fallback glyph untouched. The older constructor never loads it.</summary>
        public FlatPixelBeatingMaterialsCatalog(Texture2D atlas34, Texture2D vegetation4,
            Texture2D anchors12, Texture2D tar2, FlatPixelFallbackCatalog borrowedSelector)
            : this(atlas34, vegetation4, anchors12, tar2, null, borrowedSelector) { }

        /// <summary>Also the approved straight shaded stone masonry (sill-environment frame 8 of 9) for
        /// the western Beating's grey StoneWall ruins; an independent optional group.</summary>
        public FlatPixelBeatingMaterialsCatalog(Texture2D atlas34, Texture2D vegetation4,
            Texture2D anchors12, Texture2D tar2, Texture2D sillEnvironment9, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null) return;
            materials = Frames(atlas34, FrameCount, "materials");
            vegetation = Frames(vegetation4, 4, "vegetation");
            anchors = Frames(anchors12, 12, "anchors");
            tar = Frames(tar2, 2, "tar");
            var environment = Frames(sillEnvironment9, 9, "stone masonry");
            masonry = environment == null ? null : new[] { environment[8] };
            if (environment != null) for (int i = 0; i < 8; i++) { DestroyOwned(environment[i].sprite); DestroyOwned(environment[i]); }
        }

        public static bool IsSupportedZone(Zone zone)
        {
            if (zone?.ZoneID == null || !coordinates.TryGetValue(zone.ZoneID, out var point)) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height) return false;
            // Address metadata is immutable; receiving-world authority is not.
            // HasSavedColumn observes without creating/adopting a lair ledger.
            return map.GetBiome(point.x, point.y) == BiomeType.Beating
                && map.GetPOI(point.x, point.y) == null
                && !LairStacks.HasSavedColumn(manager, zone.ZoneID);
        }

        public bool TryResolveForeground(Zone zone, int x, int y, Entity owner, out Tile tile)
        {
            tile = null;
            if (!ReadyCell(zone, x, y, out _) || !Selected(zone, x, y, owner)) return false;
            int frame = ForegroundFrame(owner, zone.TileState.Get(x, y), x, y);
            if (frame < 0) return false;
            if (owner.BlueprintName == "DryBrush") tile = vegetation == null ? null : vegetation[3];
            else if (owner.BlueprintName == "Signpost") tile = anchors == null ? null : anchors[8];
            else if (owner.BlueprintName == "TarSeep") tile = tar == null ? null : tar[frame];
            else if (owner.BlueprintName == "StoneWall") tile = masonry == null ? null : masonry[0];
            else tile = materials == null ? null : materials[frame];
            return tile != null;
        }

        /// <summary>Current dry material below a covering subject. Real crust
        /// outranks sand; incompatible bases are ambiguous and retain fallback.
        /// Hidden or foreign owners cannot erase disclosed native ground.</summary>
        public bool TryResolveGround(Zone zone, int x, int y, out Tile tile, out Entity floor)
        {
            tile = null; floor = null;
            if (!ReadyCell(zone, x, y, out var cell)) return false;
            var state = zone.TileState.Get(x, y);
            bool live = state != null && !state.IsEmpty;
            Entity substrate = null, crust = null, seep = null;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                // One canonical disclosed TarSeep may keep its real substrate
                // beneath it; its sole live oil stays on the independent mark
                // layer. Static eligibility, so a missing tar atlas still shows
                // the true ground under the fallback glyph.
                if (owner.BlueprintName == "TarSeep" && TarFrame(owner, state) >= 0)
                { if (seep != null) return false; seep = owner; continue; }
                bool ground = IsGroundName(owner.BlueprintName);
                if (ground)
                {
                    if (!OrdinaryGround(owner)) return false;
                    if (owner.BlueprintName == "SaltCrust")
                    { if (crust != null) return false; crust = owner; }
                    else
                    { if (substrate != null) return false; substrate = owner; }
                }
                else if (FlatPixelSettlementGroundCatalog.IsOrdinaryHearth(owner)) { } // a lit hearth stands on the real ground
                else if (CompetingGround(owner, state, x, y)) return false;
            }
            // Any other live state (residual oil after removal, mixtures, energy)
            // keeps the fallback cue rather than a clean native floor.
            if (live && seep == null) return false;
            // Salt plates form above ordinary sand. Do not invent a preference
            // over an independently supplied constructed floor or road.
            if (crust != null && substrate != null && substrate.BlueprintName != "Sand") return false;
            floor = crust ?? substrate;
            if (floor == null) return false;
            int variation = VariantAt(x, y);
            switch (floor.BlueprintName)
            {
                case "Sand": tile = materials == null ? null : materials[variation]; break;
                case "SaltCrust": tile = materials == null ? null : materials[4 + variation]; break;
                case "SandstoneFloor": tile = materials == null ? null : materials[12 + variation]; break;
                case "RoadStone": tile = materials == null ? null : materials[32 + ((x + y) & 1)]; break;
            }
            if (tile != null) return true;
            floor = null; return false;
        }

        /// <summary>Small sandstone-wall shade pilot: one disclosed dry sand or
        /// paving owner, with no other visible physical occupant. Hidden owners
        /// cannot disclose themselves by suppressing the presentation layer.</summary>
        internal static bool CanReceiveWallShadow(Zone zone, int x, int y)
        {
            if (!IsSupportedZone(zone)) return false;
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.ParentZone != zone || cell.X != x || cell.Y != y
                || !cell.Explored || !cell.IsVisible) return false;
            var state = zone.TileState.Get(x, y);
            if (state != null && !state.IsEmpty) return false;
            Entity floor = null;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                if (floor != null || (owner.BlueprintName != "Sand" && owner.BlueprintName != "SandstoneFloor")
                    || !OrdinaryGround(owner)) return false;
                floor = owner;
            }
            return floor != null;
        }

        /// <summary>Only suppress a selected literal ground glyph whose material
        /// is represented by the successful ground query. A reverse Sand/Crust
        /// layer tie does not disclose sand dots through a real crust surface.
        /// Covering actors, items, structures and unsupported terrain never qualify.</summary>
        public bool IsRepresentedGroundOwner(Zone zone, int x, int y, Entity owner)
        {
            if (!Selected(zone, x, y, owner) || !TryResolveGround(zone, x, y, out _, out var floor)) return false;
            return ReferenceEquals(owner, floor)
                || floor.BlueprintName == "SaltCrust" && owner.BlueprintName == "Sand" && OrdinaryGround(owner);
        }

        /// <summary>Caller authorizes the examine document; native portrait still
        /// requires the current visible world subject. Passable brush may be
        /// examined beneath the player without replacing that foreground.</summary>
        public bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (!ReadyCell(zone, x, y, out var cell)) return false;
            if (owner?.BlueprintName == "DryBrush" && Placed(zone, cell, owner)
                && ForegroundFrame(owner, zone.TileState.Get(x, y), x, y) >= 0 && vegetation != null)
            { sprite = vegetation[3].sprite; return sprite != null; }
            // The exact placed seep may be read beneath the player; world
            // selection stays with the selector.
            if (owner?.BlueprintName == "TarSeep" && Placed(zone, cell, owner) && tar != null)
            {
                int frame = TarFrame(owner, zone.TileState.Get(x, y));
                if (frame < 0) return false;
                sprite = tar[frame].sprite; return sprite != null;
            }
            if (!Selected(zone, x, y, owner)) return false;
            if (TryResolveForeground(zone, x, y, owner, out var foreground))
            { sprite = foreground.sprite; return sprite != null; }
            if (TryResolveGround(zone, x, y, out var ground, out var floor) && ReferenceEquals(floor, owner))
            { sprite = ground.sprite; return sprite != null; }
            return false;
        }

        bool ReadyCell(Zone zone, int x, int y, out Cell cell)
        {
            cell = null;
            if (disposed || selector == null || !admits(zone)) return false;
            cell = zone.GetCell(x, y);
            return cell != null && cell.ParentZone == zone && cell.X == x && cell.Y == y
                && cell.Explored && cell.IsVisible;
        }
        bool Selected(Zone zone, int x, int y, Entity owner)
            => !disposed && selector != null && owner != null
                && selector.TryResolve(zone, x, y, out var selected)
                && !selected.Remembered && ReferenceEquals(selected.Owner, owner);

        /// <summary>The approved sandstone wall frame (16 or 17) for an owner the Beating would draw
        /// natively, by exactly the same rule; -1 otherwise. Shared with ordinary settlements.</summary>
        internal static int SandstoneWallFrame(Entity owner, int x, int y)
            => owner?.BlueprintName == "SandstoneWall" ? ForegroundFrame(owner, null, x, y) : -1;
        /// <summary>A DryBrush the Beating would draw natively, by exactly its rule.</summary>
        internal static bool IsEligibleDryBrush(Entity owner)
            => owner?.BlueprintName == "DryBrush" && ForegroundFrame(owner, null, 0, 0) >= 0;

        static int ForegroundFrame(Entity owner, ZoneTileState.TileState state, int x, int y)
        {
            if (!PlainOwner(owner) || HasForeignMeaning(owner)) return -1;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (!string.IsNullOrEmpty(render.GlyphVariants)) return -1;
            switch (owner.BlueprintName)
            {
                case "DuneCrest":
                    return render.RenderString == "^" && render.RenderLayer == 1 && owner.HasTag("Terrain")
                        && owner.HasTag("Solid") && physics.Solid && !owner.HasTag("Wall") && !owner.HasTag("Vegetation")
                        && NoMaterialState(owner) ? 8 + VariantAt(x, y) : -1;
                case "SandstoneWall":
                    return render.RenderString == "#" && render.RenderLayer == 0 && owner.HasTag("Wall")
                        && owner.HasTag("Solid") && physics.Solid && !owner.HasTag("Vegetation")
                        && Material(owner, "Stone") && Intact(owner) && NeutralThermal(owner) ? 16 + ((x + y) & 1) : -1;
                case "StoneWall":
                    // Grey stone ruins: the same static shape Sill uses for its StoneWall, plus the
                    // Beating's intact/neutral-temperature condition rules. Sandstone keeps its own brick.
                    return render.RenderString == "#" && render.RenderLayer == 0 && owner.HasTag("Wall")
                        && owner.HasTag("Solid") && physics.Solid && !owner.HasTag("Vegetation")
                        && Intact(owner) && (!owner.HasPart<ThermalPart>() || NeutralThermal(owner)) ? 0 : -1;
                case "Rock":
                    return render.RenderString == "o" && render.RenderLayer == 1 && owner.HasTag("Solid") && physics.Solid
                        && !owner.HasTag("Wall") && !owner.HasTag("Vegetation") && !owner.HasTag("Terrain")
                        && Material(owner, "Stone") && NeutralThermal(owner) && !owner.HasPart<DestructiblePart>()
                        ? 18 + ((x + y) & 1) : -1;
                case "BrinePool":
                    var pool = Owned<LiquidPoolPart>(owner); var source = Owned<TileStateSourcePart>(owner);
                    return render.RenderString == "~" && render.RenderLayer == 1 && owner.HasTag("Terrain")
                        && !owner.HasTag("Wall") && !owner.HasTag("Vegetation") && !owner.HasTag("Solid") && !physics.Solid
                        && Material(owner, "Brine") && NeutralThermal(owner) && !owner.HasPart<DestructiblePart>()
                        && pool != null && pool.LiquidId == "brine" && pool.Volume >= 0
                        && source != null && source.EffectiveCoating == "brine" && source.CoatingTurns > 0
                        && FlatPixelKitchenBrineCatalog.IsSoleLiveBrine(state)
                        ? (pool.Volume == 0 ? 24 : 20) + VariantAt(x, y) : -1;
                case "TarSeep":
                    return TarFrame(owner, state);
                case "PaleSaltVein":
                    return render.RenderString == "*" && render.RenderLayer == 4 && owner.HasTag("Solid") && physics.Solid
                        && owner.HasTag("MineralVein") && !owner.HasTag("Terrain") && !owner.HasTag("Wall") && !owner.HasTag("Vegetation")
                        && NoMaterialState(owner) && Harvest(owner, "PaleSalt") ? 28 : -1;
                case "Bones":
                    return render.RenderString == "%" && render.RenderLayer == 1 && owner.HasTag("Terrain")
                        && !owner.HasTag("Wall") && !owner.HasTag("Vegetation") && !owner.HasTag("Solid") && !physics.Solid
                        && NoMaterialState(owner) && Harvest(owner, "Bone") ? 29 : -1;
                case "Saltbriar":
                    return render.RenderString == ";" && render.RenderLayer == 4 && owner.HasTag("Vegetation")
                        && !owner.HasTag("Wall") && !owner.HasTag("Terrain") && !owner.HasTag("Solid") && !physics.Solid
                        && Material(owner, "Plant") && Intact(owner) && NeutralThermal(owner)
                        && Harvest(owner, "SaltbriarSprig") ? 30 + ((x + y) & 1) : -1;
                case "DryBrush":
                    return render.RenderString == "\"" && render.RenderLayer == 1 && owner.HasTag("Vegetation") && owner.HasTag("Terrain")
                        && !owner.HasTag("Wall") && !owner.HasTag("Solid") && !physics.Solid
                        && Material(owner, "DryPlant") && Intact(owner) && NeutralThermal(owner) ? 3 : -1;
                case "Signpost":
                    return render.RenderString == "I" && render.RenderLayer == 1 && physics.Solid
                        && !owner.HasTag("Wall") && !owner.HasTag("Terrain") && !owner.HasTag("Vegetation")
                        && NoMaterialState(owner) && Owned<RegionalSignpostPart>(owner) != null ? 8 : -1;
                default: return -1;
            }
        }
        /// <summary>Canonical ordinary TarSeep: exact blueprint presentation,
        /// tar material, one owned oil pool with non-negative stock, one owned
        /// oil source with no energies, neutral temperature, no status effects
        /// and the cell's sole live state is that oil. 0 stocked, 1 empty
        /// stock (still oily), -1 refuse.</summary>
        static int TarFrame(Entity owner, ZoneTileState.TileState state)
        {
            if (owner?.BlueprintName != "TarSeep" || !PlainOwner(owner) || HasForeignMeaning(owner)) return -1;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            var pool = Owned<LiquidPoolPart>(owner); var source = Owned<TileStateSourcePart>(owner);
            if (render.RenderString != "~" || render.ColorString != "&K" || render.RenderLayer != 1
                || !string.IsNullOrEmpty(render.GlyphVariants)
                || !owner.HasTag("Terrain") || owner.HasTag("Wall") || owner.HasTag("Vegetation") || owner.HasTag("Solid")
                || physics.Solid || !Material(owner, "Tar") || !NeutralThermal(owner) || owner.HasPart<DestructiblePart>()
                || pool == null || pool.LiquidId != "oil" || pool.Volume < 0
                || source == null || source.EffectiveCoating != "oil" || source.CoatingTurns <= 0
                || source.Heat != 0 || source.Cold != 0 || source.Charge != 0 || !string.IsNullOrEmpty(source.Residue)
                || !NoEffects(owner) || !IsSoleLiveOil(state)) return -1;
            return pool.Volume == 0 ? 1 : 0;
        }
        static bool NoEffects(Entity owner)
        {
            if (!owner.HasPart<StatusEffectsPart>()) return true;
            var effects = Owned<StatusEffectsPart>(owner);
            return effects != null && effects.GetAllEffects().Count == 0;
        }
        /// <summary>One live oil coating and nothing else: no second coating,
        /// residue, cloud, heat, cold or charge. Observation only.</summary>
        static bool IsSoleLiveOil(ZoneTileState.TileState state)
            => state != null && state.Coatings.Count == 1 && state.Coatings[0].Id == "oil" && state.Coatings[0].Turns > 0
                && state.Residues.Count == 0 && string.IsNullOrEmpty(state.Cloud)
                && state.Heat == 0 && state.Cold == 0 && state.Charge == 0;
        static bool IsGroundName(string name)
            => name == "Sand" || name == "SaltCrust" || name == "SandstoneFloor" || name == "RoadStone";
        static bool OrdinaryGround(Entity owner)
        {
            if (!PlainOwner(owner) || HasForeignMeaning(owner) || !owner.HasTag("Terrain")
                || owner.HasTag("Barren") || owner.HasTag("Wall") || owner.HasTag("Solid") || owner.HasTag("Vegetation")
                || Owned<PhysicsPart>(owner).Solid || !NoMaterialState(owner)) return false;
            var render = Owned<RenderPart>(owner);
            if (render.RenderLayer != 0) return false;
            switch (owner.BlueprintName)
            {
                case "Sand":
                case "SandstoneFloor": return render.RenderString == "." && render.GlyphVariants == "...,.'`";
                case "SaltCrust": return render.RenderString == "+" && render.GlyphVariants == "+-|";
                case "RoadStone": return render.RenderString == "=" && string.IsNullOrEmpty(render.GlyphVariants);
                default: return false;
            }
        }
        static bool PlainOwner(Entity owner)
        {
            if (owner == null || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (owner.HasPart<SpatialFootprintPart>())
            {
                var footprint = Owned<SpatialFootprintPart>(owner);
                if (footprint == null || footprint.Offsets.Length != 1 || footprint.Offsets[0] != Vector2Int.zero) return false;
            }
            // Observe effects without firing a Render event or suppressing its
            // eventual real tint. White native RGB is only the neutral treatment.
            if (owner.HasPart<StatusEffectsPart>())
            {
                var effects = Owned<StatusEffectsPart>(owner);
                if (effects == null || effects.GetRenderColorOverride() != null) return false;
            }
            return true;
        }
        static bool HasForeignMeaning(Entity owner)
        {
            bool harvest = owner.BlueprintName == "PaleSaltVein" || owner.BlueprintName == "Bones" || owner.BlueprintName == "Saltbriar";
            bool brine = owner.BlueprintName == "BrinePool", seep = owner.BlueprintName == "TarSeep";
            return owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || !harvest && owner.HasPart<HarvestablePart>() || owner.HasPart<RepairablePart>() || owner.HasPart<DoorPart>()
                || owner.HasPart<CampfirePart>() || owner.HasPart<TorchLightPart>() || owner.HasPart<AlchemyStillPart>()
                || owner.HasPart<SoddenPreparationPart>() || owner.HasPart<ContainerPart>() || owner.HasPart<TriggerOnStepPart>()
                || owner.HasPart<GasPoolPart>() || owner.HasPart<FiniteLiquidFilmPart>() || owner.HasPart<BurnOffGasPart>()
                || !brine && !seep && (owner.HasPart<LiquidPoolPart>() || owner.HasPart<TileStateSourcePart>())
                || owner.HasPart<WellPart>()
                || owner.BlueprintName != "Signpost" && owner.HasPart<RegionalSignpostPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
        }
        static bool Harvest(Entity owner, string yield)
        {
            var part = Owned<HarvestablePart>(owner);
            return part != null && !part.Harvested && part.YieldBlueprint == yield
                && part.YieldMin == 1 && part.YieldMax == 2 && part.YieldChance == 100;
        }
        static bool NoMaterialState(Entity owner)
            => !owner.HasPart<MaterialPart>() && !owner.HasPart<ThermalPart>() && !owner.HasPart<DestructiblePart>();
        static bool Material(Entity owner, string id) => Owned<MaterialPart>(owner)?.MaterialID == id;
        static bool Intact(Entity owner)
        { var part = Owned<DestructiblePart>(owner); return part != null && !part.Gone && part.HP > 0; }
        static bool NeutralThermal(Entity owner)
        {
            var part = Owned<ThermalPart>(owner);
            return part != null && float.IsFinite(part.Temperature) && float.IsFinite(part.FreezeTemperature)
                && float.IsFinite(part.FlameTemperature) && part.Temperature > part.FreezeTemperature
                && part.Temperature < part.FlameTemperature;
        }
        static bool Placed(Zone zone, Cell cell, Entity owner)
        {
            if (owner == null || owner.SpatialZone != zone) return false;
            var anchor = zone.GetEntityCell(owner);
            if (anchor == null || !anchor.Objects.Contains(owner)) return false;
            if (!owner.HasPart<SpatialFootprintPart>()) return anchor == cell;
            var footprint = Owned<SpatialFootprintPart>(owner);
            return footprint != null && zone.IsFootprintCurrent(owner)
                && SpatialQuery.DistanceToCell(zone, owner, cell.X, cell.Y) == 0;
        }
        static bool VisiblePhysicalOwner(Entity owner)
        {
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.InInventory != null || physics.Equipped != null) return false;
            if (!owner.HasPart<StackerPart>()) return true;
            var stack = Owned<StackerPart>(owner); return stack != null && stack.StackCount > 0;
        }
        static bool CompetingGround(Entity owner, ZoneTileState.TileState state, int x, int y)
            => Owned<CropPart>(owner) != null || Owned<FieldHarvestPart>(owner) != null || Owned<CultivatedSoilPart>(owner) != null
                || Owned<LiquidPoolPart>(owner) != null || Owned<GasPoolPart>(owner) != null || Owned<TileStateSourcePart>(owner) != null
                || Owned<TriggerOnStepPart>(owner) != null || owner.HasTag("WellGroundMarker")
                || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker")
                || owner.HasTag("Terrain") && !owner.HasTag("Wall") && !NeutralDryOverlay(owner, state, x, y);
        static bool NeutralDryOverlay(Entity owner, ZoneTileState.TileState state, int x, int y)
            => (owner.BlueprintName == "DryBrush" || owner.BlueprintName == "DuneCrest" || owner.BlueprintName == "Bones")
                && ForegroundFrame(owner, state, x, y) >= 0;
        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            if (owner == null) return null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part)
                { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        static int VariantAt(int x, int y)
        {
            uint value = unchecked((uint)x * 0x9e3779b1u ^ (uint)y * 0x85ebca77u);
            value ^= value >> 16; value = unchecked(value * 0x7feb352du); value ^= value >> 15;
            return (value & 15u) < 12u ? 0 : 1 + (int)((value >> 4) % 3u);
        }
        static Tile[] Frames(Texture2D texture, int count, string group)
        {
            if (texture == null || texture.width != count * Width || texture.height != Height) return null;
            var tiles = new Tile[count];
            for (int i = 0; i < count; i++)
            {
                var sprite = Sprite.Create(texture, new Rect(i * Width, 0, Width, Height),
                    new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 Beating " + group + " " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; tiles[i] = tile;
            }
            return tiles;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrames(materials); DestroyFrames(vegetation); DestroyFrames(anchors); DestroyFrames(tar); DestroyFrames(masonry);
        }
        static void DestroyFrames(Tile[] tiles)
        {
            if (tiles == null) return;
            foreach (var tile in tiles)
            { if (tile == null) continue; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
