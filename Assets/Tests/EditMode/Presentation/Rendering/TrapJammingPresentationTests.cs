using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Controlled placements and revealed geometry test authoritative native
    /// commands and actual imported art. This is not an ordinary discovery/feel claim.</summary>
    public sealed class TrapJammingPresentationTests
    {
        static Part Jam(Entity owner)
        {
            var part = owner.Parts.SingleOrDefault(p => p.GetType().FullName == "CavesOfOoo.Core.TrapJammingPart");
            Assert.NotNull(part, "Actual trap blueprint requires the finite jamming opt-in."); return part;
        }
        static bool Jammed(Entity owner) => (bool)Jam(owner).GetType().GetField("Jammed").GetValue(Jam(owner));
        static void SetJammed(Entity owner, bool value) => Jam(owner).GetType().GetField("Jammed").SetValue(Jam(owner), value);
        static string Id(string blueprint, int variant, bool jammed) => "spread-scenery-" + blueprint.ToLowerInvariant() + (jammed ? "-jammed-" : "-") + variant;

        [TestCase("SpikeTrap", 0)] [TestCase("SpikeTrap", 1)]
        [TestCase("BearTrap", 0)] [TestCase("BearTrap", 1)]
        [TestCase("FireTrap", 0)] [TestCase("FireTrap", 1)]
        [TestCase("PressurePlate", 0)] [TestCase("PressurePlate", 1)]
        public void SameOwnerUsesDistinctArmedAndJammedModelsWithoutChangingGameplay(string blueprint, int variant)
        {
            using (var scope = new DensityLootTestScope())
            {
                var owner = scope.Factory.CreateEntity(blueprint); var parts = owner.Parts.ToArray();
                var trigger = owner.GetPart<TriggerOnStepPart>(); bool consume = trigger.ConsumeOnTrigger;
                Assert.False(Jammed(owner)); Assert.True(SpreadSceneryRecipes.TryModel(owner, variant, out var armed));
                Assert.AreEqual(Id(blueprint, variant, false), armed);
                SetJammed(owner, true);
                Assert.True(SpreadSceneryRecipes.TryModel(owner, variant, out var jammed));
                Assert.AreEqual(Id(blueprint, variant, true), jammed); Assert.AreNotEqual(armed, jammed);
                Assert.True(Jammed(owner)); Assert.AreEqual(consume, trigger.ConsumeOnTrigger); CollectionAssert.AreEqual(parts, owner.Parts);
                SetJammed(owner, false); Assert.True(SpreadSceneryRecipes.TryModel(owner, variant, out var restored)); Assert.AreEqual(armed, restored);
            }
        }

        [TestCase("SpikeTrap")] [TestCase("BearTrap")] [TestCase("FireTrap")] [TestCase("PressurePlate")]
        public void ExamineReportsActualCurrentStateAndPhysicalMaterialUse(string blueprint)
        {
            using (var scope = new DensityLootTestScope())
            {
                var owner = scope.Factory.CreateEntity(blueprint); var examine = owner.GetPart<ExaminablePart>();
                Assert.NotNull(examine); string armed = examine.BuildExamineLine().ToLowerInvariant();
                StringAssert.Contains("timber", armed); StringAssert.Contains("jam", armed);
                SetJammed(owner, true); string jammed = examine.BuildExamineLine().ToLowerInvariant();
                StringAssert.Contains("jammed", jammed); Assert.AreNotEqual(armed, jammed);
                StringAssert.DoesNotContain("repair:", jammed);
            }
        }

        [Test] public void AuthoredPackHasEightDistinctWoodenJamStatesAndRetainsArmedGeometry()
        {
            var source = JsonUtility.FromJson<SpreadScenerySource>(File.ReadAllText(Path.Combine(Application.dataPath, "../ArtSource/SpreadScenery3D/kit.json")));
            source.Validate(); var jammed = source.models.Where(m => m.id.Contains("-jammed-")).ToArray(); Assert.AreEqual(8, jammed.Length);
            foreach (var model in jammed)
            {
                var armed = source.models.Single(m => m.id == model.id.Replace("-jammed-", "-"));
                Assert.AreNotEqual(string.Join("|", armed.boxes.Select(b => JsonUtility.ToJson(b))), string.Join("|", model.boxes.Select(b => JsonUtility.ToJson(b))));
                Assert.True(model.boxes.Any(b => b.color >= 18 && b.color <= 22 && b.size[0] >= .35f), "Readable broad timber crosspiece: " + model.id);
                Assert.NotNull(SpreadScenery3DLibrary.Load().Find(model.id), "Real imported mesh/prefab: " + model.id);
            }
            Assert.AreEqual(8, jammed.Select(m => string.Join("|", m.boxes.Select(b => JsonUtility.ToJson(b)))).Distinct().Count());
        }

        [TestCase("SpikeTrap")] [TestCase("BearTrap")] [TestCase("FireTrap")] [TestCase("PressurePlate")]
        public void NativeTransactionChangesExactRenderedOwnerAndSavedStateWhileLegacyActivationStillWorks(string blueprint)
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                foreach (var e in f.Zone.GetReadOnlyEntities().ToArray()) if (e != f.Player) f.Zone.RemoveEntity(e);
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) f.Zone.TileState.Clear(x, y);
                Assert.True(f.Zone.MoveEntity(f.Player, 19, 10)); var owner = f.Add(blueprint, 20, 10); string ownerId = owner.ID;
                var timber = f.Factory.CreateEntity("SalvagedTimber"); timber.GetPart<StackerPart>().StackCount = 2;
                Assert.True(f.Player.GetPart<InventoryPart>().AddObject(timber)); f.Refresh();
                string armed = Exact(f, owner); Assert.False(armed.Contains("-jammed-")); int hooks = 0;
                var prior = EntityVisualHooks.InteractionCallback;
                try
                {
                    EntityVisualHooks.InteractionCallback += (actor, target, zone) =>
                    { Assert.AreSame(f.Player, actor); Assert.AreSame(owner, target); Assert.AreSame(f.Zone, zone); Assert.True(Jammed(owner)); hooks++; };
                    Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(owner, "JamTrap"), f.Player, f.Zone).Success);
                    Assert.AreEqual(1, hooks); Assert.AreEqual(1, timber.GetPart<StackerPart>().StackCount);
                }
                finally { EntityVisualHooks.InteractionCallback = prior; }
                f.Refresh(); string jammed = Exact(f, owner); Assert.That(jammed, Does.Contain("-jammed-")); Assert.AreNotEqual(armed, jammed);
                int hp = f.Player.GetStatValue("Hitpoints"); Assert.True(MovementSystem.TryMove(f.Player, f.Zone, 1, 0));
                Assert.AreEqual(hp, f.Player.GetStatValue("Hitpoints")); Assert.NotNull(f.Zone.GetEntityCell(owner));
                f.BindLoaded(f.RoundTrip()); var replacement = f.Zone.GetReadOnlyEntities().Single(e => e.ID == ownerId);
                Assert.AreNotSame(owner, replacement); Assert.True(Jammed(replacement)); Assert.AreEqual(jammed, Exact(f, replacement));
                Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner, out _));
                // Identical mechanical trap without opt-in retains its existing trigger/removal behavior.
                var legacy = f.Add(blueprint, 21, 10); legacy.RemovePart(Jam(legacy)); f.Refresh(); Assert.False(Exact(f, legacy).Contains("-jammed-"));
                hp = f.Player.GetStatValue("Hitpoints"); Assert.True(MovementSystem.TryMove(f.Player, f.Zone, 1, 0)); Assert.Less(f.Player.GetStatValue("Hitpoints"), hp);
                f.Refresh(); Assert.AreEqual(blueprint == "PressurePlate", f.Zone.GetEntityCell(legacy) != null);
                if (blueprint != "PressurePlate") Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(legacy, out _));
                else Assert.False(Exact(f, legacy).Contains("-jammed-"));
            }
        }

        static string Exact(SpawnRing3DIntegrationFixture f, Entity owner)
        {
            var recipe = SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition); Assert.IsNull(recipe.Failure);
            Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner, out var proof), proof.Failure);
            var entry = SpreadScenery3DLibrary.Load().Find(recipe.ModelId); Assert.NotNull(entry);
            Assert.AreSame(owner, recipe.Owner); Assert.AreSame(entry.Mesh, proof.ExpectedMesh); Assert.AreSame(SpreadScenery3DLibrary.Load().Material, proof.ExpectedMaterial);
            Assert.True(proof.Batched); Assert.Greater(proof.SubmittedMesh.vertexCount, 0); return recipe.ModelId;
        }

        [TestCase("hidden")] [TestCase("foreign-part")] [TestCase("foreign-physics")] [TestCase("custom-visual")]
        public void InvalidJammedAppearanceNeverBorrowsApprovedSafetyModel(string change)
        {
            using (var scope = new DensityLootTestScope())
            {
                var owner = scope.Factory.CreateEntity("SpikeTrap"); SetJammed(owner, true);
                Assert.True(SpreadSceneryRecipes.TryModel(owner, 0, out var model)); StringAssert.Contains("-jammed-", model);
                if (change == "hidden") owner.GetPart<RenderPart>().Visible = false;
                else if (change == "foreign-part") Jam(owner).ParentEntity = new Entity();
                else if (change == "foreign-physics") owner.GetPart<PhysicsPart>().ParentEntity = new Entity();
                else owner.GetPart<RenderPart>().VisualID = "unrelated-art";
                Assert.False(SpreadSceneryRecipes.TryModel(owner, 0, out _));
            }
        }

        [TestCase("SpikeTrap")] [TestCase("BearTrap")] [TestCase("FireTrap")] [TestCase("PressurePlate")]
        public void NearbyHintIsPureVisibleCurrentAndGoesAwayAfterJamming(string blueprint)
        {
            using (var scope = new DensityLootTestScope())
            using (var f = new WorldAffordanceQueryTests.Fixture())
            {
                f.Player.AddPart(new InventoryPart()); var owner = scope.Factory.CreateEntity(blueprint); Assert.True(f.Zone.AddEntity(owner, 11, 10));
                f.Zone.GetCell(11, 10).IsVisible = f.Zone.GetCell(11, 10).Explored = true;
                var actorProbe = new WorldAffordanceQueryTests.Trap(); var ownerProbe = new WorldAffordanceQueryTests.Trap();
                f.Player.AddPart(actorProbe); owner.AddPart(ownerProbe);
                var cue = WorldAffordanceQuery.Find(f.Player, f.Zone, false, 11, 10); Assert.True(cue.HasValue);
                Assert.AreSame(owner, cue.Value.Target); Assert.AreEqual("JamTrap", cue.Value.Command); StringAssert.Contains("jam", cue.Value.Hint);
                Assert.AreEqual(0, actorProbe.Calls + ownerProbe.Calls); Assert.False(Jammed(owner));
                f.Zone.GetCell(11, 10).IsVisible = false; Assert.False(WorldAffordanceQuery.Current(f.Player, f.Zone, cue.Value));
                f.Zone.GetCell(11, 10).IsVisible = true; Assert.True(WorldAffordanceQuery.Current(f.Player, f.Zone, cue.Value));
                SetJammed(owner, true); Assert.False(WorldAffordanceQuery.Find(f.Player, f.Zone, false, 11, 10).HasValue);
                Assert.False(WorldAffordanceQuery.Current(f.Player, f.Zone, cue.Value)); Assert.AreEqual(0, actorProbe.Calls + ownerProbe.Calls);
            }
        }
    }
}
