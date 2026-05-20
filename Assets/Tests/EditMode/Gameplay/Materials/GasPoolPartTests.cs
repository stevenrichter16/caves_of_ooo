using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.2 — gas-system foundation tests. Covers the minimum viable
    /// slice: a gas pool entity exists in a cell with correct render
    /// + state, the factory rejects bad inputs gracefully, the density
    /// property fires DensityChange events on mutation. No dispersal,
    /// merge, or behavior yet (those land in G.3/G.4/G.5+).
    /// </summary>
    public class GasPoolPartTests
    {
        private const string DefDir = "Resources/Content/Data/GasDefinitions";

        [SetUp]
        public void Setup() { MessageLog.Clear(); Diag.ResetAll(); }

        [TearDown]
        public void TearDown() => GasRegistry.ResetForTests();

        private static GasDefinition LoadFromFile(string id)
        {
            string path = Path.Combine(UnityEngine.Application.dataPath, DefDir, id + ".json");
            Assert.IsTrue(File.Exists(path),
                $"Shipped gas JSON missing: Assets/{DefDir}/{id}.json");
            GasRegistry.InitializeFromJsonSources(new[] { File.ReadAllText(path) });
            var def = GasRegistry.Get(id);
            Assert.IsNotNull(def, $"{id}.json failed to parse/register.");
            return def;
        }

        // ════════════════ Registry shape ════════════════

        [Test]
        public void GasRegistry_StartsUninitialized()
        {
            GasRegistry.ResetForTests();
            Assert.IsFalse(GasRegistry.IsInitialized);
            Assert.AreEqual(0, GasRegistry.Count);
            Assert.IsNull(GasRegistry.Get("anything"));
        }

        [Test]
        public void GasRegistry_Initialize_FromSingleJson_RegistersGas()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""smoke"", ""DisplayName"":""smoke"",
                ""GasType"":""Smoke"", ""Glyph"":""°"", ""Color"":""&K"" } ] }");
            Assert.IsTrue(GasRegistry.IsInitialized);
            Assert.AreEqual(1, GasRegistry.Count);
            var d = GasRegistry.Get("smoke");
            Assert.IsNotNull(d);
            Assert.AreEqual("Smoke", d.GasType);
        }

        [Test]
        public void GasRegistry_MalformedJson_DoesNotCrash()
        {
            Assert.DoesNotThrow(() => GasRegistry.Initialize(@"{ garbage }"));
            Assert.IsTrue(GasRegistry.IsInitialized, "init still flags true on malformed");
            Assert.AreEqual(0, GasRegistry.Count, "but no gases registered");
        }

        [Test]
        public void GasRegistry_LateRowWinsOnIdCollision()
        {
            GasRegistry.InitializeFromJsonSources(new[]
            {
                @"{ ""Gases"":[ { ""Id"":""dup"", ""GasType"":""First"" } ] }",
                @"{ ""Gases"":[ { ""Id"":""dup"", ""GasType"":""Second"" } ] }",
            });
            Assert.AreEqual("Second", GasRegistry.Get("dup").GasType,
                "later JSON file wins on Id collision (mirror LiquidRegistry)");
        }

        // ════════════════ JSON content (shipped poison-vapor) ════════════════

        [Test]
        public void PoisonVapor_Json_HasExpectedShape()
        {
            var d = LoadFromFile("poison-vapor");
            Assert.AreEqual("Poison", d.GasType);
            Assert.AreEqual("°", d.Glyph);
            Assert.AreEqual("&g", d.Color);
            Assert.AreEqual(100, d.DefaultDensity);
            Assert.AreEqual(1, d.DefaultLevel);
            Assert.IsFalse(d.Seeping, "default poison-vapor not seeping");
            Assert.IsFalse(d.Stable, "default poison-vapor not stable (decays)");
        }

        // ════════════════ GasFactory.SpawnGas ════════════════

        [Test]
        public void SpawnGas_RegistryUninitialized_ReturnsNull_EmitsDiag()
        {
            GasRegistry.ResetForTests();
            var zone = new Zone("GasFactoryTest");
            var result = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            Assert.IsNull(result, "no spawn when registry uninitialized");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "SpawnRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("RegistryUninitialized", recs[0].PayloadJson);
        }

        [Test]
        public void SpawnGas_UnknownId_ReturnsNull_EmitsDiag()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var zone = new Zone("GasFactoryTest");
            var result = GasFactory.SpawnGas(zone, 5, 5, "unknownGas");
            Assert.IsNull(result);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "SpawnRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("UnknownGas", recs[0].PayloadJson);
        }

        [Test]
        public void SpawnGas_NullZone_ReturnsNull_EmitsDiag()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var result = GasFactory.SpawnGas(null, 5, 5, "poison-vapor");
            Assert.IsNull(result);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "SpawnRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("NullZone", recs[0].PayloadJson);
        }

        [Test]
        public void SpawnGas_OutOfBounds_ReturnsNull_EmitsDiag()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var zone = new Zone("GasFactoryTest");
            var result = GasFactory.SpawnGas(zone, -5, -5, "poison-vapor");
            Assert.IsNull(result);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "SpawnRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("CellOutOfBounds", recs[0].PayloadJson);
        }

        [Test]
        public void SpawnGas_HappyPath_CreatesEntityWithAllThreeParts()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""DisplayName"":""poison vapor"",
                ""GasType"":""Poison"", ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":100, ""DefaultLevel"":1 } ] }");
            var zone = new Zone("GasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            Assert.IsNotNull(gas, "spawn succeeded");
            Assert.IsNotNull(gas.GetPart<RenderPart>(), "RenderPart attached");
            Assert.IsNotNull(gas.GetPart<PhysicsPart>(), "PhysicsPart attached");
            Assert.IsNotNull(gas.GetPart<GasPoolPart>(), "GasPoolPart attached");
        }

        [Test]
        public void SpawnGas_RenderPart_PulledFromDef()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""DisplayName"":""poison vapor"",
                ""GasType"":""Poison"", ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":100, ""DefaultLevel"":1 } ] }");
            var zone = new Zone("GasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            var render = gas.GetPart<RenderPart>();
            Assert.AreEqual("°", render.RenderString, "glyph from def");
            Assert.AreEqual("&g", render.ColorString, "color from def");
            Assert.AreEqual("poison vapor", render.DisplayName);
        }

        [Test]
        public void SpawnGas_PhysicsPart_NotSolid()
        {
            // Counter / boundary: a gas cloud must NOT be solid; creatures
            // walk through it. The factory hard-codes Solid=false.
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var zone = new Zone("GasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            Assert.IsFalse(gas.GetPart<PhysicsPart>().Solid,
                "gas is non-solid (creatures walk through)");
        }

        [Test]
        public void SpawnGas_DefaultsCopyFromDef()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""seeping-stable"", ""GasType"":""Custom"",
                ""Glyph"":""°"", ""Color"":""&Y"",
                ""DefaultDensity"":75, ""DefaultLevel"":3,
                ""Seeping"":true, ""Stable"":true } ] }");
            var zone = new Zone("GasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "seeping-stable");
            var pool = gas.GetPart<GasPoolPart>();
            Assert.AreEqual(75, pool.Density, "Density from DefaultDensity");
            Assert.AreEqual(3, pool.Level, "Level from DefaultLevel");
            Assert.IsTrue(pool.Seeping, "Seeping inherited");
            Assert.IsTrue(pool.Stable, "Stable inherited");
            Assert.AreEqual("Custom", pool.GasType, "GasType inherited");
            Assert.AreEqual("&Y", pool.ColorString, "ColorString inherited");
        }

        [Test]
        public void SpawnGas_DensityOverride_TakesPrecedence()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":100, ""DefaultLevel"":1 } ] }");
            var zone = new Zone("GasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor",
                density: 200, level: 5);
            var pool = gas.GetPart<GasPoolPart>();
            Assert.AreEqual(200, pool.Density, "explicit density overrides default");
            Assert.AreEqual(5, pool.Level, "explicit level overrides default");
        }

        [Test]
        public void SpawnGas_PlacedInZone_AtRequestedCell()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var zone = new Zone("GasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 7, 9, "poison-vapor");
            var pos = zone.GetEntityPosition(gas);
            Assert.AreEqual(7, pos.x);
            Assert.AreEqual(9, pos.y);
        }

        [Test]
        public void SpawnGas_GasTag_AppliedToEntity()
        {
            // Future systems (G.3 dispersal iteration, G.4 merge) find
            // gas entities via the "Gas" tag — pin it here.
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var zone = new Zone("GasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            Assert.IsTrue(gas.Tags.ContainsKey("Gas"), "gas entity carries 'Gas' tag");
        }

        [Test]
        public void SpawnGas_Creator_CarriedThrough()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var zone = new Zone("GasFactoryTest");
            var creator = new Entity { ID = "src", BlueprintName = "Source" };
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", creator: creator);
            Assert.AreSame(creator, gas.GetPart<GasPoolPart>().Creator,
                "Creator carried through factory to pool");
        }

        [Test]
        public void SpawnGas_EmitsCreatedDiag()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var zone = new Zone("GasFactoryTest");
            GasFactory.SpawnGas(zone, 7, 9, "poison-vapor", density: 150, level: 2);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Created", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"gasId\":\"poison-vapor\"", recs[0].PayloadJson);
            StringAssert.Contains("\"density\":150", recs[0].PayloadJson);
            StringAssert.Contains("\"level\":2", recs[0].PayloadJson);
            StringAssert.Contains("\"x\":7", recs[0].PayloadJson);
            StringAssert.Contains("\"y\":9", recs[0].PayloadJson);
            StringAssert.Contains("\"gasType\":\"Poison\"", recs[0].PayloadJson);
        }

        // ════════════════ GasPoolPart.Density property ════════════════

        [Test]
        public void GasPoolPart_Density_NegativeClampsToZero()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var zone = new Zone("GasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 50);
            var pool = gas.GetPart<GasPoolPart>();
            pool.Density = -100;
            Assert.AreEqual(0, pool.Density, "negative density clamped to 0 (mirror Volume convention)");
        }

        [Test]
        public void GasPoolPart_Density_FiresDensityChangeEventOnMutation()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var zone = new Zone("GasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 50);
            var pool = gas.GetPart<GasPoolPart>();
            int observedOld = -1, observedNew = -1;
            int fireCount = 0;
            gas.AddPart(new DensityChangeListenerPart(
                (oldV, newV) => { observedOld = oldV; observedNew = newV; fireCount++; }));
            pool.Density = 80;
            Assert.AreEqual(1, fireCount);
            Assert.AreEqual(50, observedOld);
            Assert.AreEqual(80, observedNew);
        }

        [Test]
        public void GasPoolPart_Density_NoEventOnZeroDelta_Counter()
        {
            // Counter: setting Density to its current value must NOT
            // fire the event (would flood the bus on every dispersal
            // tick that didn't actually change density). Mirrors Qud's
            // `_Density != value` gate.
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var zone = new Zone("GasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 50);
            var pool = gas.GetPart<GasPoolPart>();
            int fireCount = 0;
            gas.AddPart(new DensityChangeListenerPart((_, __) => fireCount++));
            pool.Density = 50; // same as current
            Assert.AreEqual(0, fireCount, "zero-delta assignment must be silent");
        }

        [Test]
        public void GasPoolPart_Density_NegativeToZero_NoEventIfAlreadyZero()
        {
            // Boundary: pool.Density already 0; pool.Density = -5 →
            // clamps to 0 → no event (zero-delta after clamp).
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":100 } ] }");
            var zone = new Zone("GasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 0);
            var pool = gas.GetPart<GasPoolPart>();
            int fireCount = 0;
            gas.AddPart(new DensityChangeListenerPart((_, __) => fireCount++));
            pool.Density = -5;
            Assert.AreEqual(0, pool.Density);
            Assert.AreEqual(0, fireCount, "0 → -5 clamps to 0; no net change, no event");
        }
    }

    /// <summary>
    /// Test-support Part that captures DensityChange event params via a
    /// callback. Used only by GasPoolPartTests.
    /// </summary>
    internal class DensityChangeListenerPart : Part
    {
        public override string Name => "DensityChangeListener";
        private readonly System.Action<int, int> _onChange;
        public DensityChangeListenerPart(System.Action<int, int> onChange)
        {
            _onChange = onChange;
        }
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "GasDensityChange") return true;
            int oldV = e.GetParameter<int>("OldValue");
            int newV = e.GetParameter<int>("NewValue");
            _onChange?.Invoke(oldV, newV);
            return true;
        }
    }
}
