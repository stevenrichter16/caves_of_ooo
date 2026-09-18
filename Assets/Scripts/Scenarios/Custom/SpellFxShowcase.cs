using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Actual skill commands in a repeatable arena. Each case gets a fresh caster
    /// and fresh targets so buffs, cooldowns, and consumed marks cannot bleed into
    /// the next demonstration. The player observes; no simulation is replayed by FX.
    /// </summary>
    [Scenario(name: "Spell FX Showcase", category: "Combat Stress",
        description: "All registered magic, every animation family, zero/high resonance, resistance, walls, death, movement and hidden casts.")]
    public sealed class SpellFxShowcase : IScenario
    {
        public const int CasterX = 38;
        public const int CasterY = 12;
        public enum Variant { Normal, ZeroMarks, HighResonance, Resisted, Blocked, Rejected, Death, ForcedMovement, Offscreen }

        public sealed class CaseDefinition
        {
            public string SkillID { get; }
            public Variant Outcome { get; }
            public string Label { get; }
            public bool ExpectAccepted => Outcome != Variant.Rejected
                && !(SkillID == "Rites_ScaldingVeil" && Outcome == Variant.ZeroMarks);

            public CaseDefinition(string skillID, Variant outcome, string label)
            {
                SkillID = skillID;
                Outcome = outcome;
                Label = label;
            }
        }

        /// <summary>A prepared case can be inspected before it resolves exactly once.</summary>
        public sealed class Stage
        {
            public CaseDefinition Definition { get; internal set; }
            public Entity Caster { get; internal set; }
            public Entity PrimaryTarget { get; internal set; }
            public readonly List<Entity> Targets = new List<Entity>();
            public bool HasExecuted { get; private set; }
            public bool Accepted { get; private set; }
            public bool BlocksTurnAdvance { get; private set; }
            internal ScenarioContext Context;
            internal ActivatedAbilitySpec Ability;
            internal Cell TargetCell;

            public bool Execute()
            {
                if (HasExecuted) return false;
                HasExecuted = true;
                int dx = Ability.TargetingMode == AbilityTargetingMode.SelfCentered
                    || Definition.Outcome == Variant.Rejected ? 0 : 1;
                Accepted = Caster.GetPart<SkillsPart>().TryRouteSkillCommand(
                    Ability.Command, Context.Zone, new System.Random(1729), dx, 0,
                    Context.Zone.GetEntityCell(Caster), TargetCell, Ability.Range, out bool blocks);
                BlocksTurnAdvance = blocks;
                return Accepted;
            }
        }

        public void Apply(ScenarioContext ctx)
        {
            ctx.World.RemoveEntitiesWithTag("Creature");
            ClearArena(ctx);
            ctx.Player.Teleport(34, CasterY).SetHpMax();
            EnsureFloor(ctx, 28, 3, 54, 21);
            EnsureFloor(ctx, 2, 3, 22, 21);
            if (Application.isPlaying)
            {
                var previous = UnityEngine.Object.FindFirstObjectByType<SpellFxShowcasePlayer>();
                if (previous != null) UnityEngine.Object.Destroy(previous.gameObject);
                new GameObject("Spell FX Showcase").AddComponent<SpellFxShowcasePlayer>().Initialize(ctx);
            }
            ctx.Log("Spell FX Showcase: actual casts, fresh targets, all schools. Pause or step with the showcase controls.");
        }

        /// <summary>Registry-driven so newly registered magic cannot silently miss the showcase.</summary>
        public static IReadOnlyList<CaseDefinition> CreateCases()
        {
            var available = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var tree in SkillRegistry.GetAllSkills())
            {
                if (tree.Powers == null) continue;
                foreach (var power in tree.Powers)
                {
                    if (!IsMagicID(power.Class)) continue;
                    BaseSkillPart skill = CreateSkill(power.Class);
                    ActivatedAbilitySpec spec = skill?.DeclareActivatedAbility(null);
                    if (spec != null && !string.IsNullOrEmpty(spec.Command))
                        available[power.Class] = power.Name;
                }
            }
            var result = new List<CaseDefinition>();
            var added = new HashSet<string>(StringComparer.Ordinal);
            string[] representative = { "Pyromancy_Kindle", "Pyromancy_EmberVein", "Hydromancy_JetBlast",
                "Hydromancy_DrenchLob", "Galvanism_Thunderclap", "Cryomancy_GlacialWall",
                "Spellcraft_WardGleam", "Rites_BloodletterLedger" };
            foreach (string id in representative)
                AddPrimaryCase(result, available, added, id);
            foreach (var pair in available)
                AddPrimaryCase(result, available, added, pair.Key);
            foreach (var pair in available)
            {
                if (!pair.Key.StartsWith("Rites_", StringComparison.Ordinal)) continue;
                result.Add(new CaseDefinition(pair.Key, Variant.ZeroMarks,
                    pair.Value + (pair.Key == "Rites_ScaldingVeil" ? " · dry refusal" : " · zero marks")));
            }
            result.Add(new CaseDefinition("Pyromancy_Kindle", Variant.Resisted, "Kindle · heat immunity"));
            result.Add(new CaseDefinition("Pyromancy_Kindle", Variant.Blocked, "Kindle · blocked path"));
            result.Add(new CaseDefinition("Hydromancy_DrenchLob", Variant.Rejected, "Drench Lob · no-direction refusal"));
            result.Add(new CaseDefinition("Pyromancy_Kindle", Variant.Death, "Kindle · lethal impact"));
            result.Add(new CaseDefinition("Hydromancy_JetBlast", Variant.ForcedMovement, "Jet Blast · captured position, resolved push"));
            result.Add(new CaseDefinition("Pyromancy_Kindle", Variant.Offscreen, "Kindle · outside field of view"));
            return result.AsReadOnly();
        }

        private static void AddPrimaryCase(List<CaseDefinition> result,
            SortedDictionary<string, string> available, HashSet<string> added, string id)
        {
            if (!available.TryGetValue(id, out string label) || !added.Add(id)) return;
            bool rite = id.StartsWith("Rites_", StringComparison.Ordinal);
            result.Add(new CaseDefinition(id, rite ? Variant.HighResonance : Variant.Normal,
                label + (rite ? " · fed marks" : "")));
        }

        private static bool IsMagicID(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (string school in new[] { "Pyromancy_", "Cryomancy_", "Galvanism_", "Hydromancy_", "Corrosion_", "Spellcraft_", "Rites_" })
                if (id.StartsWith(school, StringComparison.Ordinal)) return true;
            return false;
        }

        private static BaseSkillPart CreateSkill(string id)
        {
            Type type = typeof(SkillsPart).Assembly.GetType("CavesOfOoo.Skills." + id);
            return type != null && !type.IsAbstract && typeof(BaseSkillPart).IsAssignableFrom(type)
                ? Activator.CreateInstance(type) as BaseSkillPart : null;
        }

        /// <summary>Stages real material/status prerequisites without invoking a cast or emitting a sequence.</summary>
        public static Stage PrepareCase(ScenarioContext ctx, CaseDefinition definition)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            ClearArena(ctx);
            // PrepareCase is also a standalone inspection/test entry point. The
            // Terrain-derived StoneFloor blueprint does not carry a Floor tag.
            EnsureFloor(ctx, 28, 3, 54, 21);
            EnsureFloor(ctx, 2, 3, 22, 21);
            ctx.Player.Teleport(34, CasterY);
            Cryomancy_GlacialWall.Factory = ctx.Factory;
            MaterialReactionResolver.Factory = ctx.Factory;
            ResonanceSystem.EnsureInitialized();
            int x = definition.Outcome == Variant.Offscreen ? 8 : CasterX;
            int y = CasterY;
            Entity caster = CreateActor(ctx.Zone, "Spell FX caster", "Player", "@", x, y, 250);
            caster.AddPart(new ActivatedAbilitiesPart());
            caster.AddPart(new SkillsPart());
            caster.AddPart(new InventoryPart { MaxWeight = 1000 });
            BaseSkillPart skill = CreateSkill(definition.SkillID);
            if (skill == null || !caster.GetPart<SkillsPart>().AddSkill(skill, "scenario"))
                throw new InvalidOperationException("Cannot stage registered magic " + definition.SkillID);
            var spec = skill.DeclareActivatedAbility(caster);
            var stage = new Stage { Context = ctx, Definition = definition, Caster = caster, Ability = spec };
            int distance = spec.TargetingMode == AbilityTargetingMode.AdjacentCell || spec.Range == 1 ? 1 : 2;
            if (definition.SkillID != "Cryomancy_GlacialWall")
            {
                stage.PrimaryTarget = CreateActor(ctx.Zone, "First target", "Snapjaw", "s", x + distance, y,
                    definition.Outcome == Variant.Death ? 1 : 250);
                stage.Targets.Add(stage.PrimaryTarget);
                stage.Targets.Add(CreateActor(ctx.Zone, "Far target", "Snapjaw", "s", x + 4, y, 250));
                stage.Targets.Add(CreateActor(ctx.Zone, "Upper target", "Snapjaw", "s", x + 2, y - 1, 250));
                stage.Targets.Add(CreateActor(ctx.Zone, "Lower target", "Snapjaw", "s", x + 2, y + 1, 250));
            }
            stage.TargetCell = ctx.Zone.GetCell(x + distance, y);

            if (definition.Outcome == Variant.Resisted)
                stage.PrimaryTarget.GetStat("HeatResistance").BaseValue = 100;
            if (definition.Outcome == Variant.Blocked)
            {
                var wall = new Entity { BlueprintName = "StoneWall" };
                wall.Tags["Wall"] = "";
                wall.Tags["Solid"] = "";
                wall.AddPart(new PhysicsPart { Solid = true });
                wall.AddPart(new RenderPart { DisplayName = "Blocking stone", RenderString = "#", ColorString = "&w", RenderLayer = 15 });
                ctx.Zone.AddEntity(wall, x + 1, y);
            }
            if (skill is ConsumingRiteSkillBase rite)
            {
                var book = new Entity { BlueprintName = "BloodletterLedgerGrimoire" };
                book.AddPart(new GrimoireChargePart { Charges = 10, MaxCharges = 10 });
                caster.GetPart<InventoryPart>().AddObject(book);
                if (definition.Outcome == Variant.HighResonance)
                {
                    if (rite.Shape == ConsumingRiteSkillBase.RiteShape.Self)
                        caster.ApplyEffect(new WetEffect(), caster, ctx.Zone);
                    else
                        foreach (Entity target in stage.Targets) PrimeMarks(target, caster, ctx.Zone, rite.Element);
                }
            }
            PrepareMaterials(ctx, stage, x, y);
            FieldOfView.Compute(ctx.Zone, 34, CasterY, 12);
            return stage;
        }

        private static void PrepareMaterials(ScenarioContext ctx, Stage stage, int x, int y)
        {
            string id = stage.Definition.SkillID;
            if (id == "Pyromancy_Pyroclasm" || id == "Hydromancy_ConjureWater" || id == "Hydromancy_Quench")
                stage.PrimaryTarget.ApplyEffect(new BurningEffect(rng: new System.Random(1729)), stage.Caster, ctx.Zone);
            if (id == "Galvanism_Overload" || id == "Hydromancy_DryingBreeze")
                foreach (Entity target in stage.Targets) target.ApplyEffect(new WetEffect(), stage.Caster, ctx.Zone);
            if (id == "Spellcraft_WardGleam")
            {
                var item = new Entity { BlueprintName = "ShowcaseWardItem" };
                item.AddPart(new RenderPart { DisplayName = "acid-etched ward plate", RenderString = "]" });
                item.ApplyEffect(new AcidicEffect(), stage.Caster, ctx.Zone);
                stage.Caster.GetPart<InventoryPart>().Equip(item, "ShowcaseWard");
            }
            if (id == "Pyromancy_KindleFlame" || id == "Pyromancy_Hearthwarm")
            {
                var tinder = new Entity { BlueprintName = "ShowcaseTinder" };
                tinder.AddPart(new RenderPart { DisplayName = "dry kindling", RenderString = ";", ColorString = "&y", RenderLayer = 5 });
                tinder.AddPart(new ThermalPart { FlameTemperature = 100f });
                ctx.Zone.AddEntity(tinder, x + 1, y);
            }
            if (id == "Hydromancy_ConjureRain")
            {
                for (int i = -1; i <= 1; i++)
                {
                    var crop = new Entity { BlueprintName = "ShowcaseCrop" };
                    crop.AddPart(new RenderPart { DisplayName = "garden shoots", RenderString = "\u03c4", ColorString = "&g", RenderLayer = 5 });
                    crop.AddPart(new CropPart());
                    ctx.Zone.AddEntity(crop, x + i, y + 2);
                }
            }
            if (id == "Pyromancy_FlamingHands" || id == "Pyromancy_EmberVein")
                ctx.Zone.TileState.WriteCoating(x + 1, y, "oil", 6);
        }

        private static void PrimeMarks(Entity target, Entity caster, Zone zone, string element)
        {
            target.ApplyEffect(new WetEffect(), caster, zone);
            if (element == "Acid") target.ApplyEffect(new AcidicEffect(), caster, zone);
            else
            {
                target.ApplyEffect(new FrozenEffect(), caster, zone);
                if (element == "Electric" || element == "Any")
                    target.ApplyEffect(new ElectrifiedEffect(), caster, zone);
            }
        }

        private static Entity CreateActor(Zone zone, string label, string blueprint, string glyph, int x, int y, int hp)
        {
            var actor = new Entity { BlueprintName = blueprint };
            actor.Tags["Creature"] = "";
            actor.Tags["SpellFxShowcase"] = "";
            actor.AddPart(new RenderPart { DisplayName = label, RenderString = glyph, RenderLayer = 20,
                VisualID = blueprint == "Player" ? "actor.player" : "actor.snapjaw" });
            actor.AddPart(new PhysicsPart());
            actor.AddPart(new StatusEffectsPart());
            actor.AddPart(new ThermalPart());
            actor.AddPart(new Body());
            actor.Statistics["Hitpoints"] = new Stat { Owner = actor, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            actor.Statistics["Toughness"] = new Stat { Owner = actor, Name = "Toughness", BaseValue = 10, Min = 1, Max = 30 };
            foreach (string name in new[] { "HeatResistance", "ColdResistance", "ElectricResistance", "AcidResistance" })
                actor.Statistics[name] = new Stat { Owner = actor, Name = name, BaseValue = 0, Min = -100, Max = 100 };
            zone.AddEntity(actor, x, y);
            return actor;
        }

        private static void ClearArena(ScenarioContext ctx)
        {
            foreach (Entity entity in ctx.Zone.GetAllEntities())
            {
                if (entity == ctx.PlayerEntity) continue;
                var p = ctx.Zone.GetEntityPosition(entity);
                if (InArena(p.x, p.y))
                {
                    if (IsArenaFloor(entity))
                    {
                        entity.RemoveEffect(_ => true);
                        continue;
                    }
                    ctx.Turns.RemoveEntity(entity);
                    ctx.Zone.RemoveEntity(entity);
                }
            }
            for (int y = 3; y <= 21; y++)
                for (int x = 2; x <= 54; x++)
                    if (InArena(x, y)) ctx.Zone.TileState.Clear(x, y);
        }

        private static bool InArena(int x, int y)
            => y >= 3 && y <= 21 && ((x >= 28 && x <= 54) || (x >= 2 && x <= 22));

        private static bool IsArenaFloor(Entity entity)
            => entity != null && (entity.HasTag("Floor")
                || entity.BlueprintName == "StoneFloor" || entity.BlueprintName == "Floor");

        private static void EnsureFloor(ScenarioContext ctx, int minX, int minY, int maxX, int maxY)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    bool hasFloor = false;
                    foreach (Entity entity in ctx.Zone.GetCell(x, y).Objects)
                        if (IsArenaFloor(entity)) { hasFloor = true; break; }
                    if (hasFloor) continue;
                    Entity floor = ctx.Factory.CreateEntity("StoneFloor");
                    if (floor != null) ctx.Zone.AddEntity(floor, x, y);
                }
            }
        }
    }
}
