using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// T2.2 — LightSourcePart propagation through equipment.
    ///
    /// User-visible invariant: when a wielder equips an item that has a
    /// `LightSourcePart` (FlamingSword, IceSword, ThunderHammer, future
    /// torches/lanterns), the wielder's cell becomes a light source with
    /// the item's radius/color/intensity. Holding the item in inventory
    /// (not equipped) does NOT produce light.
    ///
    /// Tests pin:
    ///   1. Positive: equipped FlamingSword adds light at the wielder's cell.
    ///   2. Counter-check: same FlamingSword in inventory but NOT equipped
    ///      → no light.
    ///   3. Tint pass-through: red light color reaches the cell tint.
    ///   4. Counter-check: equipped item without `LightSourcePart` → no light.
    ///   5. Adversarial: wielder without InventoryPart → no crash.
    ///   6. Adversarial: equipped item is null in EquippedItems dict → no crash.
    /// </summary>
    public class LightMapEquipmentLightingTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // 1. Positive: equipped FlamingSword adds light at wielder cell
        // ====================================================================

        [Test]
        public void EquippedFlamingSword_AddsLightAtWielderCell()
        {
            var zone = new Zone();
            var wielder = MakeWielder();
            zone.AddEntity(wielder, 10, 10);

            var sword = _harness.Factory.CreateEntity("FlamingSword");
            Assert.IsNotNull(sword, "FlamingSword blueprint must exist");
            Assert.IsNotNull(sword.GetPart<LightSourcePart>(),
                "FlamingSword blueprint must declare LightSource part (T2.2 wiring)");

            var inv = wielder.GetPart<InventoryPart>();
            inv.AddObject(sword);
            inv.Equip(sword, "Hand");

            var lightMap = new LightMap();
            lightMap.Compute(zone);

            float brightness = lightMap.GetBrightness(10, 10);
            Assert.Greater(brightness, lightMap.AmbientLevel,
                "Wielder's cell brightness must exceed ambient when an equipped " +
                $"item has LightSourcePart. Got {brightness} vs ambient {lightMap.AmbientLevel}.");
        }

        // ====================================================================
        // 2. Counter-check: same sword carried but NOT equipped → no light
        // ====================================================================

        [Test]
        public void UnequippedFlamingSword_DoesNotAddLight()
        {
            var zone = new Zone();
            var wielder = MakeWielder();
            zone.AddEntity(wielder, 10, 10);

            var sword = _harness.Factory.CreateEntity("FlamingSword");
            // Add to inventory but DO NOT equip.
            wielder.GetPart<InventoryPart>().AddObject(sword);

            var lightMap = new LightMap();
            lightMap.Compute(zone);

            float brightness = lightMap.GetBrightness(10, 10);
            Assert.AreEqual(lightMap.AmbientLevel, brightness, 0.0001f,
                "Wielder's cell brightness must equal ambient when the FlamingSword " +
                "is carried but not equipped (only equipped items project light). " +
                $"Got {brightness}.");
        }

        // ====================================================================
        // 3. Light color tints the cell
        // ====================================================================

        [Test]
        public void EquippedFlamingSword_TintAtWielderCellHasRedComponent()
        {
            // FlamingSword's LightColor is "&R" — red. We pin that the LightMap
            // tints the wielder's cell toward red, not just brightens it
            // (catches "we got brightness but the wrong color" bugs where a
            // future refactor mis-routes the LightSourcePart.LightColor field).
            var zone = new Zone();
            var wielder = MakeWielder();
            zone.AddEntity(wielder, 10, 10);

            var sword = _harness.Factory.CreateEntity("FlamingSword");
            wielder.GetPart<InventoryPart>().AddObject(sword);
            wielder.GetPart<InventoryPart>().Equip(sword, "Hand");

            var lightMap = new LightMap();
            lightMap.Compute(zone);

            Color tint = lightMap.GetTint(10, 10);
            Color baseTint = zone.AmbientTint;
            // LightMap.AddLight calls Color.Lerp(existingTint, lightColor, blend).
            // If baseline tint is white (1,1,1) and lightColor is red (1,0,0),
            // the lerp drives green and blue DOWN while red stays at 1.0 —
            // so we can't assert "red goes up", we have to assert "red
            // dominates green and blue after the tint blend".
            Assert.Greater(tint.r, tint.g,
                $"Red component must dominate green under a red-light tint. " +
                $"Got tint=(r={tint.r}, g={tint.g}, b={tint.b}).");
            Assert.Greater(tint.r, tint.b,
                $"Red component must dominate blue under a red-light tint. " +
                $"Got tint=(r={tint.r}, g={tint.g}, b={tint.b}).");
            // Counter-pin: green and blue should have actually dropped below
            // the baseline's white-flat values (not just stayed equal — that
            // would mean the lerp didn't fire).
            Assert.Less(tint.g, baseTint.g,
                $"Green should drop below baseline ({baseTint.g}) under red tint. Got {tint.g}.");
            Assert.Less(tint.b, baseTint.b,
                $"Blue should drop below baseline ({baseTint.b}) under red tint. Got {tint.b}.");
        }

        // ====================================================================
        // 4. Equipped item without LightSourcePart → no light contribution
        // ====================================================================

        [Test]
        public void EquippedNonLightItem_DoesNotAddLight()
        {
            // Counter-check: a regular non-glowing weapon, equipped, must NOT
            // produce light. Proves the light comes from the LightSourcePart
            // gate, not a bug where ANY equipped item lights up.
            var zone = new Zone();
            var wielder = MakeWielder();
            zone.AddEntity(wielder, 10, 10);

            // LongSword has no LightSource — used as the negative control.
            var plainSword = _harness.Factory.CreateEntity("LongSword");
            Assert.IsNotNull(plainSword, "LongSword blueprint must exist");
            Assert.IsNull(plainSword.GetPart<LightSourcePart>(),
                "Precondition: LongSword must NOT have LightSourcePart " +
                "(if it does, this test's negative-control assumption breaks).");

            wielder.GetPart<InventoryPart>().AddObject(plainSword);
            wielder.GetPart<InventoryPart>().Equip(plainSword, "Hand");

            var lightMap = new LightMap();
            lightMap.Compute(zone);

            float brightness = lightMap.GetBrightness(10, 10);
            Assert.AreEqual(lightMap.AmbientLevel, brightness, 0.0001f,
                "Equipped LongSword (no LightSourcePart) must not produce light. " +
                $"Got {brightness}.");
        }

        // ====================================================================
        // 5. Wielder without InventoryPart does not crash
        // ====================================================================

        [Test]
        public void WielderWithoutInventoryPart_DoesNotCrash()
        {
            var zone = new Zone();
            var wielder = new Entity { ID = "no-inv", BlueprintName = "TestNoInv" };
            wielder.AddPart(new RenderPart { DisplayName = "no-inv" });
            // Intentionally NO InventoryPart.
            zone.AddEntity(wielder, 10, 10);

            var lightMap = new LightMap();
            Assert.DoesNotThrow(() => lightMap.Compute(zone),
                "LightMap.Compute must tolerate entities without InventoryPart");

            float brightness = lightMap.GetBrightness(10, 10);
            Assert.AreEqual(lightMap.AmbientLevel, brightness, 0.0001f,
                "No equipment to light up → cell stays at ambient.");
        }

        // ====================================================================
        // 6.5. Cache-staleness invariant: stationary equip with NO move
        //     does NOT update light on the next Compute call (until the
        //     EntityVersion bumps via an entity move/add/remove). Pins the
        //     0-1-frame-delay contract documented inline at LightMap.cs:64-73
        //     and in TIER2-CLOSEOUT.md's 🟡 self-review finding.
        //     Cold-eye Finding 6 (post-fix): the docstring's load-bearing
        //     claim was previously unverified by tests.
        // ====================================================================

        [Test]
        public void EquippedAfterFirstCompute_NotVisibleUntilEntityVersionBumps()
        {
            var zone = new Zone();
            var wielder = MakeWielder();
            zone.AddEntity(wielder, 10, 10);

            var lightMap = new LightMap();
            // First compute with no equipped lights → cell at ambient.
            lightMap.Compute(zone);
            Assert.AreEqual(lightMap.AmbientLevel, lightMap.GetBrightness(10, 10), 0.0001f,
                "Precondition: no equipped lights → ambient.");

            // Equip the FlamingSword. EquipmentChanged does NOT bump
            // Zone.EntityVersion (that's the documented v1 limitation).
            var sword = _harness.Factory.CreateEntity("FlamingSword");
            wielder.GetPart<InventoryPart>().AddObject(sword);
            wielder.GetPart<InventoryPart>().Equip(sword, "Hand");

            // Compute again WITHOUT bumping EntityVersion. The cache
            // short-circuit kicks in and the new equipped light is NOT
            // visible yet. (This is the documented contract — if a future
            // refactor adds equip-bumps EntityVersion, this test will
            // catch the silent contract change.)
            lightMap.Compute(zone);
            Assert.AreEqual(lightMap.AmbientLevel, lightMap.GetBrightness(10, 10), 0.0001f,
                "Stationary equip with no entity-move/add/remove must " +
                "leave light cached at ambient until EntityVersion bumps. " +
                "Documented in LightMap.cs:64-73 as 'next entity move' " +
                "eventual consistency.");

            // Move the wielder one cell — bumps EntityVersion. Compute
            // recomputes and the equipped light becomes visible at the
            // wielder's NEW cell.
            zone.MoveEntity(wielder, 11, 10);
            lightMap.Compute(zone);
            Assert.Greater(lightMap.GetBrightness(11, 10), lightMap.AmbientLevel,
                "After entity move (EntityVersion bumped), equipped " +
                "FlamingSword's light becomes visible at the wielder's new cell.");
        }

        // ====================================================================
        // 7. Null entries in EquippedItems dict → no crash
        // ====================================================================

        [Test]
        public void NullEquippedEntry_DoesNotCrash()
        {
            // Adversarial: a save-loaded entity that ended up with a null
            // EquippedItems entry (e.g. an item that got stripped between
            // saves but the dict entry persisted). The Pass 2 walk must
            // tolerate that without NRE'ing.
            var zone = new Zone();
            var wielder = MakeWielder();
            zone.AddEntity(wielder, 10, 10);

            // Inject a null entry directly.
            wielder.GetPart<InventoryPart>().EquippedItems["BogusSlot"] = null;

            var lightMap = new LightMap();
            Assert.DoesNotThrow(() => lightMap.Compute(zone),
                "LightMap.Compute must tolerate null entries in EquippedItems " +
                "(can occur after save-load if an equipped item was stripped).");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakeWielder()
        {
            var entity = new Entity { ID = "wielder", BlueprintName = "TestWielder" };
            entity.Statistics["Hitpoints"] = new Stat
            { Owner = entity, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            entity.Statistics["Strength"] = new Stat
            { Owner = entity, Name = "Strength", BaseValue = 10 };
            entity.AddPart(new RenderPart { DisplayName = "wielder" });
            entity.AddPart(new InventoryPart());
            return entity;
        }
    }
}
