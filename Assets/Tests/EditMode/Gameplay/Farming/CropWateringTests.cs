using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Crops SM3 — ConjureRainMutation + the WateringGrimoire.
    /// See <c>Docs/CROPS-WATERING-GRIMOIRE.md §2.4</c>.
    ///
    /// Honesty bounds: these tests verify the script-observable half —
    /// moisture/bg state, diag records, and the FX REQUESTS enqueued on
    /// SpellFxBus (via Drain()). How the rain looks in motion is the
    /// showcase scenario's job (SM4).
    /// </summary>
    [TestFixture]
    public class CropWateringTests
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
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            AsciiFxBus.Clear();
            SpellFxBus.Clear();
        }

        // ── Helpers ──────────────────────────────────────────────

        private static Entity CreateCaster(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "caster", BlueprintName = "TestCaster" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "caster" });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new CavesOfOoo.Skills.SkillsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        private static CavesOfOoo.Skills.Hydromancy_ConjureRain GrantRain(Entity caster)
        {
            var rain = new CavesOfOoo.Skills.Hydromancy_ConjureRain();
            caster.GetPart<CavesOfOoo.Skills.SkillsPart>().AddSkill(rain);
            return rain;
        }

        private static Entity PlantCrop(Zone zone, int x, int y, string blueprint = "CandyCarrotCrop")
        {
            var crop = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(crop);
            zone.AddEntity(crop, x, y);
            return crop;
        }

        // ── Watering ─────────────────────────────────────────────

        [Test]
        public void Cast_WatersCropInRadius_SetsMoistureAndWetBg()
        {
            var zone = new Zone("z");
            var caster = CreateCaster(zone, 10, 10);
            var rain = GrantRain(caster);
            var cropEntity = PlantCrop(zone, 12, 10); // Chebyshev 2 <= RADIUS 3

            bool cast = rain.Cast(zone, zone.GetCell(10, 10));

            Assert.IsTrue(cast);
            var crop = cropEntity.GetPart<CropPart>();
            Assert.AreEqual(CavesOfOoo.Skills.Hydromancy_ConjureRain.MOISTURE_TICKS, crop.MoistureTicks);
            Assert.AreEqual(CropPart.WET_SOIL_BG,
                cropEntity.GetPart<RenderPart>().BackgroundColor,
                "watered crop's soil darkens (wet bg block)");
        }

        [Test]
        public void Cast_RadiusBoundary_ThreeWatered_FourNot()
        {
            var zone = new Zone("z");
            var caster = CreateCaster(zone, 10, 10);
            var rain = GrantRain(caster);
            var inside = PlantCrop(zone, 13, 10);  // Chebyshev 3: watered
            var outside = PlantCrop(zone, 14, 10); // Chebyshev 4: NOT watered

            rain.Cast(zone, zone.GetCell(10, 10));

            Assert.Greater(inside.GetPart<CropPart>().MoistureTicks, 0,
                "crop at exactly radius 3 is watered");
            Assert.AreEqual(0, outside.GetPart<CropPart>().MoistureTicks,
                "crop at radius 4 is untouched (counter-check)");
        }

        [Test]
        public void Cast_MultipleCrops_AllWatered_DiagCountsThem()
        {
            var zone = new Zone("z");
            var caster = CreateCaster(zone, 10, 10);
            var rain = GrantRain(caster);
            PlantCrop(zone, 9, 9);
            PlantCrop(zone, 11, 11);
            PlantCrop(zone, 10, 12);

            rain.Cast(zone, zone.GetCell(10, 10));

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "RainConjured", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"cropsWatered\":3", recs[0].PayloadJson);
        }

        [Test]
        public void Cast_Twice_TopUpNotAdditive()
        {
            var zone = new Zone("z");
            var caster = CreateCaster(zone, 10, 10);
            var rain = GrantRain(caster);
            var crop = PlantCrop(zone, 11, 10).GetPart<CropPart>();

            rain.Cast(zone, zone.GetCell(10, 10));
            rain.Cast(zone, zone.GetCell(10, 10));

            Assert.AreEqual(CavesOfOoo.Skills.Hydromancy_ConjureRain.MOISTURE_TICKS, crop.MoistureTicks,
                "double-cast tops up to the cap, never stacks additively");
        }

        [Test]
        public void Cast_NoCropsNearby_StillSucceeds_ZeroWateredDiag_NoFx()
        {
            var zone = new Zone("z");
            var caster = CreateCaster(zone, 10, 10);
            var rain = GrantRain(caster);

            bool cast = rain.Cast(zone, zone.GetCell(10, 10));

            Assert.IsTrue(cast, "spell still casts (and cools down) with no crops — DryingBreeze convention");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "RainConjured", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"cropsWatered\":0", recs[0].PayloadJson);
            Assert.AreEqual(0, AsciiFxBus.Drain().Count, "no rain FX when nothing was watered");
        }

        [Test]
        public void Cast_NullZoneOrCell_ReturnsFalse_NoCrash()
        {
            var zone = new Zone("z");
            var caster = CreateCaster(zone, 10, 10);
            var rain = GrantRain(caster);

            Assert.IsFalse(rain.Cast(null, zone.GetCell(10, 10)));
            Assert.IsFalse(rain.Cast(zone, null));
        }

        // ── Rain FX (script-observable half) ─────────────────────

        [Test]
        public void Cast_EmitsOneResolvedRainSequence_PerWateredCell()
        {
            var zone = new Zone("z");
            var caster = CreateCaster(zone, 10, 10);
            var rain = GrantRain(caster);
            PlantCrop(zone, 12, 10);

            rain.Cast(zone, zone.GetCell(10, 10));

            var sequences = SpellFxBus.Drain();
            Assert.AreEqual(1, sequences.Count);
            Assert.AreEqual("Hydromancy_ConjureRain", sequences[0].SpellId);
            Assert.AreEqual(1, sequences[0].AffectedCells.Count);
            Assert.AreEqual(12, sequences[0].AffectedCells[0].X);
            Assert.AreEqual(10, sequences[0].AffectedCells[0].Y);
            Assert.AreEqual(0, AsciiFxBus.Drain().Count, "the coordinator owns fallback emission");
        }

        [Test]
        public void Cast_TwoCrops_FxScalesPerCell()
        {
            var zone = new Zone("z");
            var caster = CreateCaster(zone, 10, 10);
            var rain = GrantRain(caster);
            PlantCrop(zone, 9, 10);
            PlantCrop(zone, 11, 10);

            rain.Cast(zone, zone.GetCell(10, 10));

            var sequences = SpellFxBus.Drain();
            Assert.AreEqual(1, sequences.Count, "one cast owns both watered cells");
            Assert.AreEqual(2, sequences[0].AffectedCells.Count);
            Assert.AreEqual(9, sequences[0].AffectedCells[0].X);
            Assert.AreEqual(11, sequences[0].AffectedCells[1].X);
        }

        // ── The grimoire itself ──────────────────────────────────

        [Test]
        public void Content_WateringGrimoire_TeachesConjureRain_OnRead()
        {
            // Migration: the grimoire teaches the Hydromancy_ConjureRain
            // SKILL now (mutations→skills port); same read flow.
            var zone = new Zone("z");
            var caster = CreateCaster(zone, 5, 5);
            caster.AddPart(new InventoryPart { MaxWeight = 150 });
            caster.AddPart(new CavesOfOoo.Skills.SkillsPart());
            var grimoire = _factory.CreateEntity("WateringGrimoire");
            Assert.IsNotNull(grimoire, "WateringGrimoire blueprint must exist");
            caster.GetPart<InventoryPart>().AddObject(grimoire);

            var result = InventorySystem.ExecuteCommand(
                new PerformInventoryActionCommand(grimoire, "ReadGrimoire"), caster, zone);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsTrue(caster.GetPart<CavesOfOoo.Skills.SkillsPart>()
                    .HasSkill("Hydromancy_ConjureRain"),
                "reading the grimoire teaches Conjure Rain");
            Assert.IsTrue(caster.GetPart<InventoryPart>().Objects.Contains(grimoire),
                "grimoires are never consumed on read");
        }

        [Test]
        public void Content_WateringGrimoire_SecondRead_AlreadyKnown_NoDuplicate()
        {
            var zone = new Zone("z");
            var caster = CreateCaster(zone, 5, 5);
            caster.AddPart(new InventoryPart { MaxWeight = 150 });
            caster.AddPart(new CavesOfOoo.Skills.SkillsPart());
            var grimoire = _factory.CreateEntity("WateringGrimoire");
            caster.GetPart<InventoryPart>().AddObject(grimoire);

            InventorySystem.ExecuteCommand(
                new PerformInventoryActionCommand(grimoire, "ReadGrimoire"), caster, zone);
            InventorySystem.ExecuteCommand(
                new PerformInventoryActionCommand(grimoire, "ReadGrimoire"), caster, zone);

            int count = 0;
            var skillList = caster.GetPart<CavesOfOoo.Skills.SkillsPart>().SkillList;
            for (int i = 0; i < skillList.Count; i++)
                if (skillList[i] is CavesOfOoo.Skills.Hydromancy_ConjureRain) count++;
            Assert.AreEqual(1, count, "re-reading must not grant a second instance");
        }

        [Test]
        public void ConjureRain_RegisteredAsGrimoireSpell_SelfCenteredRadius3()
        {
            var zone = new Zone("z");
            var caster = CreateCaster(zone, 5, 5);
            GrantRain(caster);

            var abilities = caster.GetPart<ActivatedAbilitiesPart>();
            ActivatedAbility rainAbility = null;
            for (int i = 0; i < abilities.AbilityList.Count; i++)
                if (abilities.AbilityList[i].Command == "CommandConjureRain")
                    rainAbility = abilities.AbilityList[i];

            Assert.IsNotNull(rainAbility, "Conjure Rain appears as an activated ability");
            Assert.AreEqual("Hydromancy", rainAbility.Class);
            Assert.AreEqual(AbilityTargetingMode.SelfCentered, rainAbility.TargetingMode);
        }
    }

    /// <summary>Migration M4: Conjure Rain is a skill. This adapter keeps
    /// the mutation-era `rain.Cast(zone, cell)` call sites verbatim while
    /// routing through the dispatcher with the InputHandler's parameter
    /// shapes (cooldown zeroed first — the old direct-Cast path never
    /// checked its own cooldown either).</summary>
    internal static class ConjureRainTestCast
    {
        public static bool Cast(this CavesOfOoo.Skills.Hydromancy_ConjureRain rain,
            Zone zone, Cell sourceCell)
        {
            var caster = rain.ParentEntity;
            var abilities = caster.GetPart<ActivatedAbilitiesPart>();
            for (int i = 0; i < abilities.AbilityList.Count; i++)
                if (abilities.AbilityList[i].Command == "CommandConjureRain")
                    abilities.AbilityList[i].CooldownRemaining = 0;
            var cmd = GameEvent.New("CommandConjureRain");
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("SourceCell", (object)sourceCell);
            caster.FireEvent(cmd);
            bool handled = cmd.Handled;
            cmd.Release();
            return handled;
        }
    }
}
