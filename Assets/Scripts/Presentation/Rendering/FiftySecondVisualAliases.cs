using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>Explicit reuse of existing physical forms. These aliases never rewrite
    /// blueprint, assembly or equipment identity and do not authorize gameplay.</summary>
    public static class FiftySecondVisualAliases
    {
        public static string HaftForm(string blueprint)
        {
            switch (blueprint)
            {
                case "OakHaftComponent": case "FieldHaftComponent": case "BracedHaftComponent": return "oak";
                case "WillowHaftComponent": return "willow";
                default: return null;
            }
        }

        public static string BindingForm(string blueprint)
        {
            switch (blueprint)
            {
                case "LeatherBindingComponent": case "PlainCordBindingComponent": case "GuardLashingComponent": return "leather";
                case "SerratedEdgeComponent": return "serrated";
                default: return null;
            }
        }

        static string PortableModel(string blueprint)
        {
            switch (blueprint)
            {
                case "FieldHaftComponent": case "BracedHaftComponent": return "spread-portable-oakhaftcomponent";
                case "PlainCordBindingComponent": case "GuardLashingComponent": return "spread-portable-leatherbindingcomponent";
                case "FilterHood": return "spread-portable-leathercap";
                case "AcidworkerApron": return "equipment-discovery-kilnfelt-apron";
                case "ColdwardCloak": return "spread-portable-cloak";
                case "KnotflaxBandage": return SoddenDistrictArtLibrary.Dressing;
                case "ConcentratedMendleaf": return "spread-portable-mendleafsprig";
                case "CleansedGrovePulp": return "spread-portable-grovered";
                case "QuillholdLoanWardGleam": return "spread-portable-wardgleamgrimoire";
                case "QuillholdLoanDryingBreeze": return "spread-portable-dryingbreezegrimoire";
                case "CounterStoreKey": return "spread-portable-ironkey";
                case "MarlbackStormbinderCorpse": return "spread-portable-marlbackcorpse";
                case "BeetleJar": return OlderdeepVoxelLibrary.ModelId("jar", 0);
                default: return null;
            }
        }

        public static bool IsPortable(string blueprint) => PortableModel(blueprint) != null;

        public static bool IsEquipment(string blueprint) => WornModel(blueprint) != null || blueprint == "BeetleJar";

        public static string WornModel(string blueprint)
        {
            switch (blueprint)
            {
                case "FilterHood": return "spread-worn-leathercap";
                case "AcidworkerApron": return "equipment-discovery-worn-kilnfelt-apron";
                case "ColdwardCloak": return "spread-worn-cloak";
                default: return null;
            }
        }

        public static bool TryPortable(Entity owner, out string model)
        {
            model = null;
            string candidate = PortableModel(owner?.BlueprintName);
            if (candidate == null || (!owner.HasTag("Item") && owner.BlueprintName != "CounterStoreKey") || owner.HasTag("Creature") || owner.HasTag("Natural")) return false;
            var physics = owner.GetPart<PhysicsPart>(); var render = owner.GetPart<RenderPart>();
            if (physics?.ParentEntity != owner || !physics.Takeable || render?.ParentEntity != owner || !render.Visible
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || (owner.GetPart<StackerPart>()?.StackCount ?? 1) <= 0) return false;
            string blueprint = owner.BlueprintName;
            if (blueprint.EndsWith("Component", System.StringComparison.Ordinal))
            {
                var component = owner.GetPart<WeaponComponentPart>();
                string slot = blueprint == "FieldHaftComponent" || blueprint == "BracedHaftComponent" ? "Haft" : "Binding";
                if (component?.ParentEntity != owner || component.Slot != slot) return false;
            }
            else if (WornModel(blueprint) != null)
            {
                var armor = owner.GetPart<ArmorPart>(); var equip = owner.GetPart<EquippablePart>();
                string slot = blueprint == "FilterHood" ? "Head" : blueprint == "AcidworkerApron" ? "Body" : "Back";
                if (armor?.ParentEntity != owner || equip?.ParentEntity != owner || equip.Slot != slot) return false;
            }
            else if (blueprint == "KnotflaxBandage")
            {
                if (owner.GetPart<TonicPart>()?.ParentEntity != owner || owner.GetPart<CureTonicPart>()?.ParentEntity != owner) return false;
            }
            else if (blueprint == "BeetleJar")
            {
                if (owner.GetPart<RecoverableLampPart>()?.Recovered != true || owner.GetPart<LightSourcePart>()?.ParentEntity != owner
                    || owner.GetPart<EquippablePart>()?.Slot != "Hand") return false;
            }
            else if (blueprint == "CounterStoreKey")
            {
                if (owner.GetPart<KeyPart>()?.ParentEntity != owner) return false;
            }
            else if (blueprint == "MarlbackStormbinderCorpse")
            {
                if (owner.GetProperty("SourceBlueprint") != "MarlbackStormbinder"
                    || string.IsNullOrEmpty(owner.GetProperty("SourceID")) || owner.GetProperty("SourceID") == owner.ID) return false;
            }
            else if (blueprint == "QuillholdLoanWardGleam" || blueprint == "QuillholdLoanDryingBreeze")
            {
                if (owner.GetPart<GrimoirePart>()?.ParentEntity != owner) return false;
            }
            else if (owner.GetPart<ReagentPart>()?.ParentEntity != owner) return false;
            model = candidate;
            return true;
        }

        public static bool IsWorldOwner(string blueprint)
        {
            switch (blueprint)
            {
                case "WellmeetGuestLocker": case "QuillholdLoanShelf": case "FrontierClothScreen":
                case "SoddenPassagePost": case "SoddenPassageGuard": case "FrontierPenGate":
                case "CounterStoreChest": case "CausticFilm": case "Graveyard": return true;
                default: return false;
            }
        }

        // Native membership and zone authority are checked by the caller. Every
        // alias keeps its own actual functional Part and destruction/door state.
        public static string WorldModel(Entity owner, out int quarterTurns)
        {
            quarterTurns = 0;
            if (!IsWorldOwner(owner?.BlueprintName)) return null;
            var physics = owner.GetPart<PhysicsPart>(); var render = owner.GetPart<RenderPart>();
            if (physics?.ParentEntity != owner || physics.Takeable || physics.InInventory != null || physics.Equipped != null
                || render?.ParentEntity != owner || !render.Visible || owner.HasPart<SpatialFootprintPart>()
                || owner.HasPart<MultiCellPilotPropPart>() || owner.GetPart<DestructiblePart>()?.Gone == true
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants)) return null;
            switch (owner.BlueprintName)
            {
                case "Graveyard":
                    return owner.GetPart<BurialPart>()?.ParentEntity == owner && owner.GetPart<ContainerPart>()?.ParentEntity == owner
                        ? OlderdeepVoxelLibrary.ModelId("plaque", 0) : null;
                case "WellmeetGuestLocker": case "CounterStoreChest":
                    return owner.GetPart<ContainerPart>()?.ParentEntity == owner ? "ring-chest" : null;
                case "QuillholdLoanShelf":
                    return owner.GetPart<InventoryPart>()?.ParentEntity == owner ? QuillholdVoxelKitLibrary.ModelId("shelf", 0) : null;
                case "FrontierClothScreen":
                    return physics.Solid ? WellmeetVoxelLibrary.ModelId("tent", 0) : null;
                case "SoddenPassagePost": return SoddenDistrictArtLibrary.Notice;
                case "SoddenPassageGuard":
                    return owner.HasTag("Creature") && owner.GetPart<BrainPart>()?.ParentEntity == owner
                        && !CombatSystem.IsDeathHandled(owner) ? SumpholdVoxelKitLibrary.ModelId("cutter", 0) : null;
                case "CausticFilm":
                    return !physics.Solid && owner.GetPart<LifespanPart>()?.ParentEntity == owner ? "density-pool-acid" : null;
                case "FrontierPenGate":
                    var door = owner.GetPart<DoorPart>();
                    if (door?.ParentEntity != owner || physics.Solid || door.QuarterTurns < 0 || door.QuarterTurns > 3
                        || render.RenderString != (door.IsClosed ? "+" : "/")) return null;
                    quarterTurns = door.QuarterTurns;
                    return door.IsClosed ? SpreadFieldGate3DLibrary.Closed : SpreadFieldGate3DLibrary.Open;
                default: return null;
            }
        }

        /// <summary>Caller already checked the active native zone, cell and catalog.
        /// The new caster borrows the original Marlback rig and its five real clips.</summary>
        public static string StormbinderModel(Entity owner)
        {
            if (owner?.BlueprintName != "MarlbackStormbinder" || !owner.HasTag("Creature") || owner.HasTag("Item")) return null;
            var physics = owner.GetPart<PhysicsPart>(); var render = owner.GetPart<RenderPart>(); var brain = owner.GetPart<BrainPart>();
            if (physics?.ParentEntity != owner || physics.Takeable || physics.InInventory != null || physics.Equipped != null
                || render?.ParentEntity != owner || !render.Visible || brain?.ParentEntity != owner
                || owner.HasPart<SpatialFootprintPart>() || owner.HasPart<MultiCellPilotPropPart>()
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants)) return null;
            return "ring-snapjaw"; // Frozen asset ID; gameplay creature is the original Marlback.
        }
    }
}
