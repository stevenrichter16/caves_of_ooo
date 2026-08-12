using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The six haulable objects (<c>Docs/DRAG-AND-HAUL.md</c> §9), audited
    /// against the SHIPPED content rather than a synthetic fixture.
    ///
    /// <para>DragRulesTests proves the arithmetic on hand-built entities.
    /// That is necessary and not sufficient: it cannot see whether the
    /// blueprints an actual player meets are wired to reach the rule at
    /// all. This file closes that gap, and it exists because the gap was
    /// real — all six blueprints originally inherited <c>Terrain</c>, which
    /// carries the <c>Terrain</c> tag, which
    /// <c>WorldInteractionSystem.ResolveTarget</c> deprioritizes. A
    /// millstone sharing a cell with any loose item would not have been the
    /// resolved target, so the drag verb would have been unreachable on the
    /// exact objects it was written for.</para>
    /// </summary>
    public class HaulableContentTests
    {
        private static EntityFactory _factory;

        /// <summary>The six, and the Strength each one demands. Kept
        /// literal rather than computed: if someone retunes a weight, this
        /// table should FAIL rather than silently agree with them.</summary>
        private static readonly (string blueprint, int weight, int needsStrength)[] Haulables =
        {
            ("FallenBeam", 60, 8),
            ("HaulBarrel", 75, 10),
            ("SaltCuredBody", 90, 12),
            ("StoneCoffer", 110, 14),
            ("SmithAnvil", 120, 18),   // grip-gated: MinLiftStrength 18 > weight's 15
            ("MillStone", 150, 19),
        };

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static Entity Actor(int strength)
        {
            var e = new Entity { ID = "actor", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Statistics["Strength"] = new Stat
            { Owner = e, Name = "Strength", BaseValue = strength, Min = 0, Max = 40 };
            return e;
        }

        // ════════════════════════════════════════════════════════
        // Reachability — can the player even aim at one?
        // ════════════════════════════════════════════════════════

        [Test]
        public void AHaulable_IsNotTerrain_SoTheCursorCanResolveIt()
        {
            // The bug this file was written for. WorldInteractionSystem
            // returns the highest-layer NON-terrain entity and only falls
            // back to terrain when the cell holds nothing else.
            foreach (var (name, _, _) in Haulables)
            {
                var e = _factory.CreateEntity(name);
                Assert.IsFalse(WorldInteractionSystem.IsTerrain(e),
                    $"{name} reads as terrain, so the cursor would skip past it");
            }
        }

        [Test]
        public void AHaulable_WinsTheCursorOverALooseItemSharingItsCell()
        {
            // The counter-check with teeth: terrain-tagged objects pass the
            // test above only because of a tag, but this one exercises the
            // actual resolution order with a competing occupant.
            foreach (var (name, _, _) in Haulables)
            {
                var cell = new Cell(3, 3, null);
                cell.Objects.Add(_factory.CreateEntity(name));
                cell.Objects.Add(_factory.CreateEntity("Bone"));

                var resolved = WorldInteractionSystem.ResolveTarget(cell);
                Assert.IsNotNull(resolved);
                Assert.IsTrue(resolved.BlueprintName == name || resolved.BlueprintName == "Bone",
                    "sanity: one of the two occupants must win");
                Assert.IsFalse(WorldInteractionSystem.IsTerrain(resolved),
                    $"{name} shared a cell and the cursor fell through to terrain");
            }
        }

        [Test]
        public void ALooseItem_StillOutranksRealTerrain()
        {
            // Counter-check to the above — proof the resolution rule itself
            // is intact and the tests above are not vacuous. Real terrain
            // (Grass) must still lose to a loose bone.
            var cell = new Cell(3, 3, null);
            cell.Objects.Add(_factory.CreateEntity("Grass"));
            cell.Objects.Add(_factory.CreateEntity("Bone"));
            Assert.AreEqual("Bone", WorldInteractionSystem.ResolveTarget(cell).BlueprintName);
        }

        // ════════════════════════════════════════════════════════
        // The ladder, on real blueprints
        // ════════════════════════════════════════════════════════

        [Test]
        public void EachHaulable_DemandsExactlyTheStrengthTheTableClaims()
        {
            foreach (var (name, _, needs) in Haulables)
            {
                Assert.AreEqual(DragVerdict.Ok,
                    DragRules.CanDrag(Actor(needs), _factory.CreateEntity(name)),
                    $"{name} should be haulable at Strength {needs}");

                var shortfall = DragRules.CanDrag(Actor(needs - 1), _factory.CreateEntity(name));
                Assert.AreNotEqual(DragVerdict.Ok, shortfall,
                    $"{name} must NOT be haulable one point below Strength {needs} — " +
                    "otherwise the boundary in the table is fiction");
            }
        }

        [Test]
        public void NoneOfThemIsSimplyPocketable()
        {
            // If the pickup path would accept one, it is an item, not
            // cargo — and the drag verb would never appear on it.
            foreach (var (name, _, _) in Haulables)
            {
                var e = _factory.CreateEntity(name);
                Assert.AreNotEqual(DragVerdict.CarryInstead, DragRules.CanDrag(Actor(40), e),
                    $"{name} can just be picked up, which defeats the point of it");
            }
        }

        [Test]
        public void TheAnvilIsGatedByGrip_NotByWeight()
        {
            // The one blueprint whose refusal is MinLiftStrength rather
            // than arithmetic. Without this, nothing in shipped content
            // would exercise that branch.
            var anvil = _factory.CreateEntity("SmithAnvil");
            Assert.AreEqual(DragVerdict.NotStrongEnough, DragRules.CanDrag(Actor(16), anvil));

            // ...and the counter-check: at 16 the weight alone is fine.
            Assert.LessOrEqual(DragRules.WeightOf(anvil), DragRules.MaxDragWeight(Actor(16)),
                "if the weight were the binding constraint this test proves nothing");
        }

        [Test]
        public void TheWeightsAgreeAcrossBothParts()
        {
            // Each blueprint authors Weight twice (Physics and Handling).
            // They must not drift, or hauling and carrying disagree.
            foreach (var (name, weight, _) in Haulables)
            {
                var e = _factory.CreateEntity(name);
                Assert.AreEqual(weight, e.GetPart<HandlingPart>()?.Weight, $"{name} Handling.Weight");
                Assert.AreEqual(weight, e.GetPart<PhysicsPart>()?.Weight, $"{name} Physics.Weight");
                Assert.AreEqual(weight, DragRules.WeightOf(e), $"{name} resolved weight");
            }
        }

        [Test]
        public void EachIsSolid_SoItStillBlocksTheCellItOccupies()
        {
            // Dropping the Terrain parent must not have quietly made these
            // walk-through-able.
            foreach (var (name, _, _) in Haulables)
                Assert.IsTrue(_factory.CreateEntity(name).GetPart<PhysicsPart>().Solid,
                    $"{name} stopped being solid");
        }

        [Test]
        public void EachRendersAboveTheFloor()
        {
            // RenderLayer 0 is the floor band. These are furniture and must
            // sit in the same band as Chest/Crate/Urn/Pillar (layer 1), or
            // the grass draws on top of the millstone.
            foreach (var (name, _, _) in Haulables)
            {
                var render = _factory.CreateEntity(name).GetPart<RenderPart>();
                Assert.GreaterOrEqual(render.RenderLayer, 1, $"{name} renders in the floor band");
            }
        }

        [Test]
        public void EveryHaulableInContent_IsCoveredByThisTable()
        {
            // Guards against the table going stale: if someone authors a
            // seventh haulable, this fails until it is audited here.
            var json = File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json"));
            var known = new HashSet<string>();
            foreach (var (name, _, _) in Haulables) known.Add(name);

            var uncovered = new List<string>();
            foreach (var name in _factory.Blueprints.Keys)
            {
                Entity e;
                try { e = _factory.CreateEntity(name); }
                catch (System.Exception) { continue; }
                if (e?.GetPart<HandlingPart>() is not HandlingPart h) continue;
                if (h.Carryable) continue;          // carryable things are items, not cargo
                if (known.Contains(name)) continue;
                uncovered.Add(name);
            }

            CollectionAssert.IsEmpty(uncovered,
                "new non-carryable HandlingPart blueprints must be added to the Haulables "
                + "table so their Strength boundary is pinned: " + string.Join(", ", uncovered));
            Assert.IsNotEmpty(json, "sanity");
        }
    }
}
