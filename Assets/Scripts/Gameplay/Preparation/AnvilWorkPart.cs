using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>A stationary bearing surface for cold dressing prepared stock.</summary>
    public sealed class AnvilWorkPart : Part, IStationCapability, IStationReadinessGuard, IDescribeUse
    {
        internal const string Kind = "anvil-dressing";
        internal const string DefinitionSeen = "AnvilWorkDefinitionSeen";
        static readonly IReadOnlyList<string> Capabilities = Array.AsReadOnly(new[] { Kind });

        public override string Name => "AnvilWork";
        public IReadOnlyList<string> Kinds => Capabilities;
        public int Reach => 1;
        public ReadinessMode Readiness => ReadinessMode.Strict;
        public int Order => 36;

        public override void Initialize()
        {
            if (ParentEntity != null) ParentEntity.IntProperties[DefinitionSeen] = 1;
        }

        internal static void UpgradeLoadedDefinition(Entity owner, EntityFactory factory)
        {
            // Add the stateless part to the exact prior anvil body, without
            // replaying creation or restoring a part removed on a newer save.
            if (owner?.BlueprintName != "SmithAnvil" || owner.IntProperties.ContainsKey(DefinitionSeen)
                || owner.HasPart<AnvilWorkPart>() || owner.HasPart<ForgePart>()
                || factory?.Blueprints.TryGetValue(owner.BlueprintName, out var blueprint) != true
                || !blueprint.Parts.ContainsKey("AnvilWork")
                || owner.GetPart<HandlingPart>() is not { Weight: 120, MinLiftStrength: 18, Carryable: false, Throwable: false } handling
                || handling.ParentEntity != owner || owner.GetPart<PhysicsPart>() is not { Weight: 120, Solid: true, Takeable: false } physics
                || physics.ParentEntity != owner)
                return;
            owner.AddPart(new AnvilWorkPart());
        }

        public bool TryBlock(Entity station, Zone zone, out string code)
        {
            bool readyBody = station == ParentEntity
                && station.GetPart<PhysicsPart>() is { Solid: true, Takeable: false } physics
                && physics.ParentEntity == station && physics.InInventory == null && physics.Equipped == null
                && !station.HasEffect<BrokenEffect>() && !station.HasEffect<FrozenEffect>();
            code = readyBody ? null : "anvil-unavailable";
            return !readyBody;
        }

        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Stand beside the anvil and choose Dress iron point on a recovered iron blank.");
            guide.Needs.Add("one recovered iron blank and a sound warhammer or assembled peat-packing mallet loose in your pack");
            guide.Limits.Add("One action spends the blank and makes one iron spike in your pack. Keep the hammer.");
        }
    }

    internal sealed class AnvilPointWorkReceipt : IMaterialWorkReceipt
    {
        internal const string Action = MaterialProcessInputPart.CommandPrefix + "point|";
        readonly Entity actor, source;
        readonly Zone zone;
        readonly EntityFactory factory;
        readonly Cell actorCell, anvilCell;
        readonly Func<bool> toolIdentity;
        readonly PhysicsPart physics;
        readonly int weight;
        readonly WeaponAssemblyPart assembly;
        readonly string head, haft, binding;

        internal Entity Anvil { get; }
        public Entity Tool { get; }

        AnvilPointWorkReceipt(Entity actor, Entity source, Entity anvil, Entity tool, Zone zone, EntityFactory factory)
        {
            this.actor = actor;
            this.source = source;
            this.zone = zone;
            this.factory = factory;
            Anvil = anvil;
            Tool = tool;
            actorCell = zone.GetEntityCell(actor);
            anvilCell = zone.GetEntityCell(anvil);
            toolIdentity = RouteWorkMenu.Identity(tool);
            physics = tool.GetPart<PhysicsPart>();
            weight = physics.Weight;
            assembly = tool.GetPart<WeaponAssemblyPart>();
            head = assembly?.BladeBlueprint;
            haft = assembly?.HaftBlueprint;
            binding = assembly?.BindingBlueprint;
        }

        internal static bool Supports(MaterialProcessDefinition definition)
            => definition?.Id == "point" && definition.SourceBlueprint == "RecoveredIronBlank"
                && definition.Mode == "Process" && definition.WorkKind == "ForgeCold" && definition.SourceRecipeId.Length == 0
                && definition.InputCount == 1 && definition.AdditionalInputBlueprints.Count == 0
                && definition.OutputBlueprints.Count == 1 && definition.OutputBlueprints[0] == "IronSpikeComponent"
                && definition.OutputCounts[0] == 1;

        static bool Accessible(Entity actor, Entity anvil, Zone zone, string action)
            => anvil != null && !string.IsNullOrEmpty(anvil.ID) && StationQuery.Near(actor, anvil, zone, AnvilWorkPart.Kind)
                && zone.GetEntityCell(anvil) is { IsVisible: true, Explored: true }
                && !zone.GetReadOnlyEntities().Any(e => e != anvil && e.ID == anvil.ID)
                && StationAccess.Allowed(actor, anvil, zone, AnvilWorkPart.Kind, action, out _, out _);

        internal static Entity FindAnvil(Entity actor, Zone zone)
        {
            var candidates = new List<StationCandidate>();
            StationQuery.Collect(actor, zone, AnvilWorkPart.Kind, candidates);
            return candidates.FirstOrDefault(candidate => candidate.Ready && Accessible(actor, candidate.Station, zone, Action)).Station;
        }

        internal static AnvilPointWorkReceipt Resolve(Entity actor, Entity source, Zone zone, EntityFactory factory)
        {
            var anvil = FindAnvil(actor, zone);
            if (anvil == null) return null;
            var hammer = actor.GetPart<InventoryPart>().Objects
                .Where(item => Available(actor, source, anvil, item, zone, factory))
                .OrderBy(item => item.ID, StringComparer.Ordinal).FirstOrDefault();
            return hammer == null ? null : new AnvilPointWorkReceipt(actor, source, anvil, hammer, zone, factory);
        }

        static bool Available(Entity actor, Entity source, Entity anvil, Entity hammer, Zone zone, EntityFactory factory)
        {
            if (hammer == null || hammer.ID == actor.ID || hammer.ID == source.ID || hammer.ID == anvil.ID
                || !WorldResourceActions.Carried(actor, hammer) || hammer.GetPart<PhysicsPart>().Solid
                || !hammer.HasTag("Item") || hammer.HasTag("Creature") || hammer.HasTag("Terrain")
                || hammer.HasPart<SpatialFootprintPart>() || hammer.HasEffect<BrokenEffect>() || hammer.HasEffect<FrozenEffect>()
                || RepairablePart.BlocksFunction(hammer) || CombatSystem.IsDeathHandled(hammer)
                || hammer.GetStat("Hitpoints") != null && hammer.GetStatValue("Hitpoints") <= 0
                || hammer.GetPart<DestructiblePart>() is { Gone: true } || hammer.GetPart<DestructiblePart>() is { HP: <= 0 }
                || hammer.GetPart<MeleeWeaponPart>()?.ParentEntity != hammer
                || zone.GetReadOnlyEntities().Any(e => e.ID == hammer.ID)
                || actor.GetPart<InventoryPart>().EquippedItems.Values.Any(e => e?.ID == hammer.ID))
                return false;

            if (hammer.BlueprintName == "Warhammer") return !hammer.HasPart<WeaponAssemblyPart>();
            return hammer.BlueprintName == WeaponForgingService.ForgedWeaponBlueprintName
                && hammer.GetPart<WeaponAssemblyPart>() is { BladeBlueprint: "PeatMalletHeadComponent" } parts
                && parts.ParentEntity == hammer && hammer.Parts.Count(p => p is WeaponAssemblyPart) == 1
                && ComponentSlot(parts.BladeBlueprint, "Blade", factory)
                && ComponentSlot(parts.HaftBlueprint, "Haft", factory)
                && ComponentSlot(parts.BindingBlueprint, "Binding", factory);
        }

        static bool ComponentSlot(string blueprint, string slot, EntityFactory factory)
            => !string.IsNullOrEmpty(blueprint) && factory.Blueprints.TryGetValue(blueprint, out var entry)
                && entry.Parts.TryGetValue("WeaponComponent", out var component)
                && component.TryGetValue("Slot", out var declared) && string.Equals(declared, slot, StringComparison.OrdinalIgnoreCase);

        public bool Current()
            => toolIdentity() && Tool.GetPart<PhysicsPart>() == physics && physics.Weight == weight
                && Tool.GetPart<WeaponAssemblyPart>() == assembly && assembly?.BladeBlueprint == head
                && assembly?.HaftBlueprint == haft && assembly?.BindingBlueprint == binding
                && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(Anvil) == anvilCell
                && Accessible(actor, Anvil, zone, Action) && Available(actor, source, Anvil, Tool, zone, factory);

        public bool Begin(InventoryTransaction transaction, string command)
        {
            if (!Current() || StationReceipt.Begin(actor, zone, transaction, command, AnvilWorkPart.Kind, Anvil) == null
                || !transaction.TryClaim(Tool, actor, command))
                return false;
            transaction.BeforeCommit(() => Current() && Accessible(actor, Anvil, zone, command));
            return true;
        }
    }
}
