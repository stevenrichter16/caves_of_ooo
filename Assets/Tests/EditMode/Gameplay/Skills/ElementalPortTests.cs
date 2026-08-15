using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The second (final) elemental batch of the mutations→skills
    /// migration — twelve ported powers across Cryomancy (IceLance,
    /// RimeNova, ChillDraft), Galvanism (ArcBolt, Thunderclap),
    /// Hydromancy (Quench, ConjureWater, ConjureRain, DryingBreeze),
    /// Corrosion (AcidSpray) and Spellcraft (WardGleam, Calm) — plus the
    /// grimoire repoints and the Calm starting-kit flip.
    ///
    /// <para>All casts dispatch through <c>FireEvent</c> with the
    /// InputHandler's exact parameter conventions — never by calling
    /// OnCommand directly (the blind spot that let six rites ship
    /// dead).</para>
    /// </summary>
    public class ElementalPortTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            string path = Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(path));
        }

        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
            AsciiFxBus.Clear();
        }

        // ── Fixtures ─────────────────────────────────────────────────

        private static Entity Caster(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "caster", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        private static T Learn<T>(Entity caster) where T : BaseSkillPart, new()
        {
            var skill = new T();
            Assert.IsTrue(caster.GetPart<SkillsPart>().AddSkill(skill),
                $"fixture: {typeof(T).Name} must attach");
            return skill;
        }

        private static Entity Snapjaw(Zone zone, int x, int y, int hp = 20)
        {
            var e = new Entity { ID = "snapjaw" + x + "_" + y, BlueprintName = "Snapjaw" };
            e.Tags["Creature"] = "";
            e.AddPart(new PhysicsPart { Solid = true });
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            zone.AddEntity(e, x, y);
            return e;
        }

        /// <summary>A thermally-live prop (a tar seep, a barrel).</summary>
        private static Entity ThermalProp(Zone zone, int x, int y, float temperature = 25f)
        {
            var e = new Entity { ID = "prop" + x + "_" + y, BlueprintName = "TarSeep" };
            e.AddPart(new PhysicsPart { Solid = false });
            e.AddPart(new MaterialPart { MaterialTagsRaw = "Organic", Combustibility = 0.5f });
            e.AddPart(new ThermalPart { Temperature = temperature, HeatCapacity = 1.0f });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity Crate(Zone zone, int x, int y, string materialTags)
        {
            var e = new Entity { ID = "crate" + x + "_" + y, BlueprintName = "Crate" };
            e.AddPart(new PhysicsPart { Solid = false });
            e.AddPart(new DestructiblePart { HP = 10, MaxHP = 10 });
            e.AddPart(new MaterialPart { MaterialTagsRaw = materialTags });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static (bool handled, bool blocks) Cast(
            Entity caster, Zone zone, string command,
            int dx = 0, int dy = 0, Cell targetCell = null, int range = 5)
        {
            var src = zone.GetEntityCell(caster);
            var cmd = GameEvent.New(command);
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("RNG", (object)new System.Random(11));
            cmd.SetParameter("SourceCell", (object)src);
            cmd.SetParameter("DirectionX", dx);
            cmd.SetParameter("DirectionY", dy);
            cmd.SetParameter("Range", range);
            if (targetCell != null) cmd.SetParameter("TargetCell", (object)targetCell);
            caster.FireEvent(cmd);
            bool handled = cmd.Handled;
            bool blocks = cmd.GetParameter<bool>("BlocksTurnAdvance");
            cmd.Release();
            return (handled, blocks);
        }

        private static ActivatedAbility Ability(Entity caster, string command)
        {
            var abilities = caster.GetPart<ActivatedAbilitiesPart>();
            for (int i = 0; i < abilities.AbilityList.Count; i++)
                if (abilities.AbilityList[i].Command == command)
                    return abilities.AbilityList[i];
            return null;
        }

        // ── Projectiles: the four bolts land their riders ────────────

        [Test]
        public void IceLance_ChillsWhatItImpales()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Cryomancy_IceLance>(caster);
            var target = Snapjaw(zone, 8, 5);
            target.AddPart(new ThermalPart { Temperature = 25f, HeatCapacity = 1.0f });

            var (handled, _) = Cast(caster, zone, "CommandIceLance", dx: 1, range: 6);

            Assert.IsTrue(handled, "the lance fired");
            Assert.Less(target.GetStatValue("Hitpoints", 20), 20, "1d6 Cold landed");
            Assert.Less(target.GetPart<ThermalPart>().Temperature, 25f,
                "the −300J chill landed through the thermal pipeline");
            Assert.Greater(Ability(caster, "CommandIceLance").CooldownRemaining, 0,
                "a fired lance is a consumed cast");
        }

        [Test]
        public void ArcBolt_ElectrifiesTheStruckCreature()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Galvanism_ArcBolt>(caster);
            var target = Snapjaw(zone, 7, 5, hp: 30);

            var (handled, _) = Cast(caster, zone, "CommandArcBolt", dx: 1);

            Assert.IsTrue(handled);
            Assert.Less(target.GetStatValue("Hitpoints", 30), 30, "1d8 Electric landed");
            Assert.IsTrue(target.HasEffect<ElectrifiedEffect>(),
                "the survivor carries the full charge");
        }

        [Test]
        public void Quench_SoaksAndCoolsTheTarget()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Hydromancy_Quench>(caster);
            var target = Snapjaw(zone, 7, 5, hp: 30);
            target.AddPart(new ThermalPart { Temperature = 60f, HeatCapacity = 1.0f });

            var (handled, _) = Cast(caster, zone, "CommandQuench", dx: 1);

            Assert.IsTrue(handled);
            Assert.IsTrue(target.HasEffect<WetEffect>(), "the soak is the spell's point");
            Assert.Less(target.GetPart<ThermalPart>().Temperature, 60f,
                "the −150J cooling pulse landed");
        }

        [Test]
        public void AcidSpray_EtchesTheTarget()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Corrosion_AcidSpray>(caster);
            var target = Snapjaw(zone, 7, 5, hp: 30);

            var (handled, _) = Cast(caster, zone, "CommandAcidSpray", dx: 1, range: 4);

            Assert.IsTrue(handled);
            Assert.IsTrue(target.HasEffect<AcidicEffect>(), "Acidic(0.8) rides the hit");
        }

        // ── Calm: pacify semantics, frozen duration, no damage ───────

        [Test]
        public void Calm_PacifiesWithoutDamage_AndDoesNotStack()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Spellcraft_Calm>(caster);
            var target = Snapjaw(zone, 8, 5);
            var brain = new BrainPart();
            target.AddPart(brain);

            var (handled, _) = Cast(caster, zone, "CommandCalm", dx: 1, range: 6);

            Assert.IsTrue(handled, "the pulse fired");
            Assert.AreEqual(20, target.GetStatValue("Hitpoints", 0), "dice \"0\" — no damage");
            Assert.IsTrue(brain.HasGoal<NoFightGoal>(), "the target laid down arms");
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("becomes peaceful")),
                "the pacify message names the change");

            // Second cast: the bolt still flies (consumed) but peace
            // does not stack — the mutation's exact idempotence.
            Ability(caster, "CommandCalm").CooldownRemaining = 0;
            MessageLog.Clear();
            var (handled2, _) = Cast(caster, zone, "CommandCalm", dx: 1, range: 6);
            Assert.IsTrue(handled2);
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("is already at peace")),
                "re-calming reports rather than stacking");
        }

        [Test]
        public void Calm_DurationIsFrozenAtTheLevelOneValue()
        {
            // The level-scaling decision (plan §5): 40 + 1×10, flat.
            Assert.AreEqual(50, Spellcraft_Calm.CALM_DURATION);
        }

        // ── Rime Nova: creatures rime, scenery chills ────────────────

        [Test]
        public void RimeNova_RimesCreaturesAndChillsTheScenery()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Cryomancy_RimeNova>(caster);
            var target = Snapjaw(zone, 6, 5, hp: 30);
            var prop = ThermalProp(zone, 4, 5, temperature: 25f);

            var (handled, blocks) = Cast(caster, zone, "CommandRimeNova");

            Assert.IsTrue(handled);
            Assert.IsTrue(blocks, "the nova's ring FX gate the turn");
            Assert.Less(target.GetStatValue("Hitpoints", 30), 30, "1d6 Cold landed");
            Assert.IsTrue(target.HasEffect<FrozenEffect>(), "survivors are rimed");
            Assert.Less(prop.GetPart<ThermalPart>().Temperature, 25f,
                "the −200J sweep chills every ThermalPart prop in radius");
        }

        // ── Thunderclap: the conductor sweep goes through the matrix ─

        [Test]
        public void Thunderclap_ChargesConductorsAndRefusesWood()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Galvanism_Thunderclap>(caster);
            var target = Snapjaw(zone, 6, 5, hp: 40);
            target.ApplyEffect(new WetEffect(0.5f));
            var anvil = Crate(zone, 4, 5, "Metal");
            var woodCrate = Crate(zone, 5, 6, "Wood");

            var (handled, _) = Cast(caster, zone, "CommandThunderclap");

            Assert.IsTrue(handled);
            Assert.Less(target.GetStatValue("Hitpoints", 40), 40, "2d6 Electric landed");
            Assert.IsTrue(target.HasEffect<ElectrifiedEffect>(), "the soaked survivor is charged");
            Assert.IsTrue(anvil.HasEffect<ElectrifiedEffect>(),
                "metal scenery picks up the charge (via ObjectStatusMatrix)");
            Assert.IsFalse(woodCrate.HasEffect<ElectrifiedEffect>(),
                "counter-check: the matrix refuses a charge into wood");
        }

        // ── Utility casts ────────────────────────────────────────────

        [Test]
        public void ChillDraft_CoolsTheNeighborhood_AndAnEmptyCastStillConsumes()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Cryomancy_ChillDraft>(caster);
            var prop = ThermalProp(zone, 6, 5, temperature: 25f);

            var (handled, _) = Cast(caster, zone, "CommandChillDraft");

            Assert.IsTrue(handled);
            Assert.Less(prop.GetPart<ThermalPart>().Temperature, 25f);

            // Empty cast: the draft blew anyway — consumed, like the mutation.
            var zone2 = new Zone("Z2");
            var lonely = Caster(zone2, 5, 5);
            Learn<Cryomancy_ChillDraft>(lonely);
            var (handled2, _) = Cast(lonely, zone2, "CommandChillDraft");
            Assert.IsTrue(handled2, "an empty draft still consumes the cast");
            Assert.Greater(Ability(lonely, "CommandChillDraft").CooldownRemaining, 0);
        }

        [Test]
        public void DryingBreeze_StripsWetAroundAndOnTheCaster()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Hydromancy_DryingBreeze>(caster);
            caster.ApplyEffect(new WetEffect(0.9f));
            var neighbor = Snapjaw(zone, 6, 5);
            neighbor.ApplyEffect(new WetEffect(0.9f));

            var (handled, _) = Cast(caster, zone, "CommandDryingBreeze");

            Assert.IsTrue(handled);
            Assert.IsFalse(neighbor.HasEffect<WetEffect>(), "the neighbor dried off");
            Assert.IsFalse(caster.HasEffect<WetEffect>(), "the caster dries too");
        }

        [Test]
        public void ConjureRain_WatersTheCrop_AndFindsNoneGracefully()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 10, 10);
            Learn<Hydromancy_ConjureRain>(caster);
            var cropEntity = new Entity { ID = "crop", BlueprintName = "TestCrop" };
            var crop = new CropPart();
            cropEntity.AddPart(crop);
            zone.AddEntity(cropEntity, 12, 10);

            var (handled, _) = Cast(caster, zone, "CommandConjureRain");

            Assert.IsTrue(handled);
            Assert.AreEqual(Hydromancy_ConjureRain.MOISTURE_TICKS, crop.MoistureTicks,
                "the crop's moisture topped up to the full 40 ticks");

            // No crops anywhere: the rain was still called — consumed.
            var zone2 = new Zone("Z2");
            var dryCaster = Caster(zone2, 5, 5);
            Learn<Hydromancy_ConjureRain>(dryCaster);
            MessageLog.Clear();
            var (handled2, _) = Cast(dryCaster, zone2, "CommandConjureRain");
            Assert.IsTrue(handled2, "rain with no crops is still a real cast");
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("finds no crops")),
                "the message tells the player what the rain found");
        }

        [Test]
        public void ConjureWater_SpawnsAPuddleAndSoaksTheCell_RefusalIsFree()
        {
            var previousFactory = MaterialReactionResolver.Factory;
            MaterialReactionResolver.Factory = _factory;
            try
            {
                var zone = new Zone("Z");
                var caster = Caster(zone, 5, 5);
                Learn<Hydromancy_ConjureWater>(caster);
                // Non-solid, so the stream walks past it to full range —
                // the furthest passable cell is the landing spot.
                var prop = ThermalProp(zone, 7, 5);

                var (handled, _) = Cast(caster, zone, "CommandConjureWater",
                    dx: 1, range: 2);

                Assert.IsTrue(handled, "the stream landed");
                var landCell = zone.GetCell(7, 5);
                bool puddleThere = false;
                for (int i = 0; i < landCell.Objects.Count; i++)
                    if (landCell.Objects[i].BlueprintName == "WaterPuddle") puddleThere = true;
                Assert.IsTrue(puddleThere, "a WaterPuddle spawned at the landing cell");
                Assert.IsTrue(prop.HasEffect<WetEffect>(), "cell occupants are soaked");

                // Counter-check: no direction is a FREE refusal — no
                // cooldown burned (the F17 keybind bug, now impossible
                // by construction).
                var zone2 = new Zone("Z2");
                var caster2 = Caster(zone2, 5, 5);
                Learn<Hydromancy_ConjureWater>(caster2);
                var (handled2, _) = Cast(caster2, zone2, "CommandConjureWater",
                    dx: 0, dy: 0, range: 2);
                Assert.IsFalse(handled2, "no direction refuses the cast");
                Assert.AreEqual(0, Ability(caster2, "CommandConjureWater").CooldownRemaining,
                    "a refused cast burns no cooldown");
            }
            finally
            {
                MaterialReactionResolver.Factory = previousFactory;
            }
        }

        [Test]
        public void WardGleam_CleansesEquipment_AndRefusesFreeWhenClean()
        {
            var zone = new Zone("Z");
            var caster = Caster(zone, 5, 5);
            Learn<Spellcraft_WardGleam>(caster);
            var inventory = new InventoryPart();
            caster.AddPart(inventory);

            var sword = new Entity { BlueprintName = "Sword" };
            sword.AddPart(new RenderPart { DisplayName = "sword" });
            sword.AddPart(new MaterialPart { MaterialID = "Steel", MaterialTagsRaw = "Metal" });
            sword.ApplyEffect(new AcidicEffect(0.8f));
            sword.ApplyEffect(new CharredEffect());
            inventory.Objects.Add(sword);
            inventory.Equip(sword, "Hand");

            var (handled, _) = Cast(caster, zone, "CommandWardGleam");

            Assert.IsTrue(handled, "the gleam cleansed");
            Assert.IsFalse(sword.HasEffect<AcidicEffect>());
            Assert.IsFalse(sword.HasEffect<CharredEffect>());
            Assert.Greater(Ability(caster, "CommandWardGleam").CooldownRemaining, 0);

            // Nothing left to cleanse: free refusal, no cooldown.
            Ability(caster, "CommandWardGleam").CooldownRemaining = 0;
            MessageLog.Clear();
            var (handled2, _) = Cast(caster, zone, "CommandWardGleam");
            Assert.IsFalse(handled2, "clean gear refuses the cast");
            Assert.AreEqual(0, Ability(caster, "CommandWardGleam").CooldownRemaining,
                "the refusal is free");
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("Nothing to cleanse")));
        }

        // ── Grants: grimoires teach skills, the kit carries Calm ─────

        [Test]
        public void QuenchGrimoire_TeachesTheSkillOnRead()
        {
            // Representative of the eleven repointed blueprints: the
            // Grimoire part now carries SkillClassName, so DoRead takes
            // the skill branch.
            var zone = new Zone("Z");
            var reader = Caster(zone, 5, 5);
            reader.AddPart(new InventoryPart { MaxWeight = 150 });
            var book = _factory.CreateEntity("QuenchGrimoire");
            Assert.IsNotNull(book, "QuenchGrimoire blueprint must exist");
            reader.GetPart<InventoryPart>().AddObject(book);

            var read = GameEvent.New("InventoryAction");
            read.SetParameter("Command", "ReadGrimoire");
            read.SetParameter("Actor", (object)reader);
            book.FireEvent(read);
            read.Release();

            Assert.IsTrue(reader.GetPart<SkillsPart>().HasSkill("Hydromancy_Quench"),
                "reading the repointed grimoire teaches the skill");
            Assert.IsNotNull(Ability(reader, "CommandQuench"),
                "the taught skill declared its activated ability");
        }

        [Test]
        public void StartingKit_CarriesCalm()
        {
            Assert.Contains("Spellcraft_Calm", StartingSpellKit.SpellClasses,
                "the former StartingMutation moved into the kit");

            var zone = new Zone("Z");
            var player = Caster(zone, 5, 5);
            StartingSpellKit.GrantAll(player);
            Assert.IsTrue(player.GetPart<SkillsPart>().HasSkill("Spellcraft_Calm"));
            Assert.IsNotNull(Ability(player, "CommandCalm"));
        }
    }
}
