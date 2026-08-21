using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Playtest bug, third occurrence of the class: DryBrush visibly
    /// caught fire and took no damage — no Destructible part, no
    /// Hitpoints stat, so BurningEffect's ticks fell into the exact
    /// silent void RouteDamage's doc-comment exists to name. The W3
    /// re-review had already caught Duckboard the same way; two
    /// instances found one-at-a-time means the CLASS needs a lint.
    ///
    /// <para>The rule: anything that can IGNITE (Thermal part +
    /// Combustibility &gt; 0) must either carry a damage pool
    /// (Destructible or Hitpoints) or be on the short exemption list
    /// with a reason. Takeable items are out of scope — item combustion
    /// is its own future feature, and a raw steak is not scenery that
    /// visibly burns forever.</para>
    /// </summary>
    public class FlammableContentLintTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp() => Diag.ResetAll();

        /// <summary>Scenery that ignites without a pool, DELIBERATELY:
        /// each entry is a thing fire cannot use up.</summary>
        private static readonly HashSet<string> BurnsForeverByDesign = new HashSet<string>
        {
            // A campfire IS fire. Burning it more is feeding it.
            "Campfire",
            // Seeps are fed from below — the surface burns, the source
            // replenishes. Inexhaustible is the fiction.
            "OilSeep", "TarSeep",
        };

        [Test]
        public void EverythingIgnitable_CanActuallyBurnDown()
        {
            var offenders = new List<string>();
            foreach (var name in _factory.Blueprints.Keys)
            {
                Entity e;
                try { e = _factory.CreateEntity(name); }
                catch (System.Exception) { continue; }
                if (e == null) continue;

                var material = e.GetPart<MaterialPart>();
                var thermal = e.GetPart<ThermalPart>();
                if (material == null || thermal == null) continue;
                if (material.Combustibility <= 0) continue;

                var phys = e.GetPart<PhysicsPart>();
                if (phys != null && phys.Takeable) continue;   // items: out of scope

                if (BurnsForeverByDesign.Contains(name)) continue;
                bool hasPool = e.GetPart<DestructiblePart>() != null
                    || e.GetStat("Hitpoints") != null;
                if (!hasPool) offenders.Add(name);
            }

            CollectionAssert.IsEmpty(offenders,
                "These ignite but nothing can ever burn them down — the player "
                + "watches them 'burn' with no HP and no consequence. Give them a "
                + "Destructible pool or add them to BurnsForeverByDesign with a "
                + "reason: " + string.Join(", ", offenders));
        }

        [Test]
        public void TheExemptionList_NamesOnlyRealIgnitables()
        {
            // A stale exemption is a hole in the lint.
            foreach (var name in BurnsForeverByDesign)
            {
                var e = _factory.CreateEntity(name);
                Assert.IsNotNull(e, name + " no longer exists");
                Assert.IsNotNull(e.GetPart<ThermalPart>(), name + " cannot ignite — why exempt it?");
            }
        }

        [Test]
        public void TheDryBrush_BurnsAway()
        {
            // The reported bug, pinned end-to-end through the structural
            // damage path burning actually takes.
            var zone = new Zone("Z");
            var brush = _factory.CreateEntity("DryBrush");
            zone.AddEntity(brush, 10, 10);
            var part = brush.GetPart<DestructiblePart>();
            Assert.IsNotNull(part, "dry brush must have something for the fire to take");

            var d = new Damage(2);
            d.AddAttribute("Fire");
            DestructionSystem.RouteDamage(brush, d, null, zone);
            Assert.Less(part.HP, part.MaxHP, "the fire does something every tick");

            for (int i = 0; i < 6; i++)
            {
                var more = new Damage(2);
                more.AddAttribute("Fire");
                DestructionSystem.RouteDamage(brush, more, null, zone);
            }
            Assert.IsNull(zone.GetEntityCell(brush),
                "dry brush is FUEL — a few ticks and it is gone");
        }

        [Test]
        public void TheAshBed_DoesNotReignite()
        {
            // Content fix riding the same sweep: ash is spent fuel. It
            // radiates heat (its Thermal stays — the steam interaction
            // depends on it); it does not catch fire again.
            var ash = _factory.CreateEntity("AshBed");
            Assert.AreEqual(0f, ash.GetPart<MaterialPart>().Combustibility,
                "what burned once is done burning");
            Assert.IsNotNull(ash.GetPart<ThermalPart>(), "but it stays hot");
        }
    }
}
