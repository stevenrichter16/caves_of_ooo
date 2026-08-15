using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Status-effects study fixes 3 + 4 (Docs/STATUS-EFFECTS-STUDY-2026-08.md
    /// §4 violators #3 and #4): the freeze and conduction gates each had
    /// two disagreeing authorities.
    ///
    /// <para><b>Freeze:</b> ThermalPart.TryFreeze applied FrozenEffect to
    /// ANYTHING that crossed FreezeTemperature (chill a stone wall — it
    /// froze), while ObjectStatusMatrix's Freezable row would refuse it.
    /// But the matrix was missing "Metal", which shipped content depends
    /// on (cold_plus_metal.json, Ice Lance's brittle-shatter identity).
    /// Fix: Metal joins Freezable, and TryFreeze routes through the
    /// matrix — creatures pass through unchanged.</para>
    ///
    /// <para><b>Conduction:</b> the electric chain accepted
    /// Conductivity>=50 OR Metal/Conductor tags; the matrix accepts
    /// Conductor/Metal/Water tags. Verified drift: Thunderclap could
    /// electrify a WaterPuddle that the chain then refused to pass
    /// charge into, and MetalGrate (Conductivity 100, NO tags) conducted
    /// numerically while the matrix refused it. Fix: MetalGrate gets its
    /// Metal tag, and the chain asks the matrix.</para>
    /// </summary>
    public class StatusGateUnificationTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixtures ─────────────────────────────────────────────────

        private static Entity Prop(Zone zone, int x, int y, string tags,
            float conductivity = 0f)
        {
            var e = new Entity { ID = "prop" + x + "_" + y, BlueprintName = "Prop" };
            e.AddPart(new RenderPart { DisplayName = "prop" });
            e.AddPart(new MaterialPart
            { MaterialTagsRaw = tags, Conductivity = conductivity });
            e.AddPart(new ThermalPart
            { Temperature = 25f, FreezeTemperature = 0f, HeatCapacity = 1f });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static void Chill(Entity target, Zone zone, float joules)
        {
            var cool = GameEvent.New("ApplyHeat");
            cool.SetParameter("Joules", (object)(-joules));
            cool.SetParameter("Radiant", (object)false);
            cool.SetParameter("Source", (object)null);
            cool.SetParameter("Zone", (object)zone);
            target.FireEvent(cool);
            cool.Release();
        }

        // ── Freeze gate (violator #3) ────────────────────────────────

        [Test]
        public void Metal_StillFreezesByChilling()
        {
            // Ice Lance's identity and cold_plus_metal.json both depend
            // on frozen metal. The matrix gate must NOT break them.
            var zone = new Zone("Z");
            var sword = Prop(zone, 5, 5, "Metal");

            Chill(sword, zone, 100f);

            Assert.IsTrue(sword.HasEffect<FrozenEffect>(),
                "metal crossing FreezeTemperature freezes — brittle-shatter setup");
        }

        [Test]
        public void Stone_RefusesFreezeByChilling()
        {
            // The violator: freeze-by-cooling had no material gate, so a
            // chilled stone wall carried FrozenEffect the matrix calls
            // WrongMaterial. The refusal must also be diag-visible.
            var zone = new Zone("Z");
            var wall = Prop(zone, 5, 5, "Stone");

            Chill(wall, zone, 100f);

            Assert.IsFalse(wall.HasEffect<FrozenEffect>(),
                "stone is not Freezable — the matrix verdict now holds on the cooling path");
            var refused = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "effect", Kind = "ObjectEffectRefused", Limit = 5 }).Records;
            Assert.GreaterOrEqual(refused.Count, 1,
                "a gate that can reject must emit a record (observability rule)");
        }

        [Test]
        public void Creature_StillFreezesByChilling()
        {
            // Counter-check: the matrix passes creatures through — the
            // gate must not change the creature path at all.
            var zone = new Zone("Z");
            var creature = new Entity { ID = "c", BlueprintName = "Snapjaw" };
            creature.Tags["Creature"] = "";
            creature.AddPart(new RenderPart { DisplayName = "snapjaw" });
            creature.AddPart(new ThermalPart
            { Temperature = 25f, FreezeTemperature = 0f, HeatCapacity = 1f });
            creature.Statistics["Hitpoints"] = new Stat
            { Owner = creature, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            zone.AddEntity(creature, 5, 5);

            Chill(creature, zone, 100f);

            Assert.IsTrue(creature.HasEffect<FrozenEffect>());
        }

        [Test]
        public void Matrix_MetalIsFreezable()
        {
            var zone = new Zone("Z");
            var sword = Prop(zone, 5, 5, "Metal");
            Assert.AreEqual(ObjectStatusVerdict.Applies,
                ObjectStatusMatrix.Evaluate(new FrozenEffect(1f), sword),
                "Metal belongs in Freezable — cold_plus_metal.json already ships");
        }

        // ── Conduction gate (violator #4) ────────────────────────────

        [Test]
        public void MetalGrate_CarriesTheMetalTag()
        {
            // Content fix: MetalGrate was authored Conductivity 100 with
            // NO material tags — conductive to the numeric test, refused
            // by the tag-based matrix. Tags are the authority (the
            // numeric field is authored on two scales); the grate needs
            // its tag.
            var grate = _factory.CreateEntity("MetalGrate");
            Assert.IsNotNull(grate, "MetalGrate blueprint must exist");
            Assert.IsTrue(grate.GetPart<MaterialPart>().HasMaterialTag("Metal"),
                "MetalGrate must carry the Metal tag so every conduction authority agrees");
        }

        [Test]
        public void Matrix_ConductionVocabulary()
        {
            var zone = new Zone("Z");
            var puddle = Prop(zone, 4, 4, "Liquid,Water");
            var crate = Prop(zone, 6, 4, "Wood");
            Assert.IsTrue(ObjectStatusMatrix.IsConductiveMaterial(
                    puddle.GetPart<MaterialPart>()),
                "water conducts — the status grammar's own rule");
            Assert.IsFalse(ObjectStatusMatrix.IsConductiveMaterial(
                    crate.GetPart<MaterialPart>()),
                "counter-check: wood does not");
        }

        [Test]
        public void Chain_PassesChargeIntoAWaterPuddle()
        {
            // The verified drift: Thunderclap can electrify a puddle via
            // the matrix, but the chain's own conductor test refused to
            // pass charge INTO the same puddle. One vocabulary now.
            var zone = new Zone("Z");
            var anvil = Prop(zone, 5, 5, "Metal", conductivity: 80f);
            var puddle = Prop(zone, 6, 5, "Liquid,Water");

            var chain = GameEvent.New("TryChainElectricity");
            chain.SetParameter("Charge", (object)1.0f);
            chain.SetParameter("Zone", (object)zone);
            chain.SetParameter("Source", (object)anvil);
            anvil.FireEvent(chain);
            chain.Release();

            Assert.IsTrue(puddle.HasEffect<ElectrifiedEffect>(),
                "charge chains into the puddle the matrix already lets Thunderclap electrify");
        }

        [Test]
        public void Chain_RefusesAWoodenCrate()
        {
            // Counter-check: unifying on the matrix vocabulary must not
            // widen conduction to non-conductors.
            var zone = new Zone("Z");
            var anvil = Prop(zone, 5, 5, "Metal", conductivity: 80f);
            var crate = Prop(zone, 6, 5, "Wood");

            var chain = GameEvent.New("TryChainElectricity");
            chain.SetParameter("Charge", (object)1.0f);
            chain.SetParameter("Zone", (object)zone);
            chain.SetParameter("Source", (object)anvil);
            anvil.FireEvent(chain);
            chain.Release();

            Assert.IsFalse(crate.HasEffect<ElectrifiedEffect>());
        }
    }
}
