using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    // Controlled owner placements and revealed fixture geometry; actual factory,
    // BoredGoal actions and presenter view lifecycle. Explicit refresh is not a
    // native-input, naturally visible animation or ordinary-discovery claim.
    public sealed class PredatorDiversionPresentationTests
    {
        static void Turn(Entity actor) => actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
        static Entity Place(SpawnRing3DIntegrationFixture f, string blueprint, int x, int y)
        {
            var owner = f.Factory.CreateEntity(blueprint); Assert.NotNull(owner); Assert.True(f.Zone.AddEntity(owner, x, y));
            var brain = owner.GetPart<BrainPart>(); if (brain != null) { brain.CurrentZone = f.Zone; brain.Rng = new System.Random(1); }
            return owner;
        }
        static void Configure(SpreadPredatorPart role, Zone zone, Entity prey, bool enabled)
        {
            var method = typeof(SpreadPredatorPart).GetMethod("Configure");
            Assert.True((bool)method.Invoke(role, method.GetParameters().Length == 3 ? new object[] { zone, prey, enabled } : new object[] { zone, prey }));
        }
        static int Progress(SpreadPredatorPart role) => typeof(SpreadPredatorPart).GetField("MeatFeedProgress") is FieldInfo f ? (int)f.GetValue(role) : 0;
        static string Phase(SpreadPredatorPart role) => typeof(SpreadPredatorPart).GetField("MeatDiversionPhase")?.GetValue(role)?.ToString() ?? "None";
        static void Exact(SpawnRing3DIntegrationFixture f, Entity owner, string model)
        {
            Assert.True(f.Find(owner, out var root, out var actual)); Assert.AreEqual(model, actual); Assert.True(f.Rendered(owner));
            Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner, out var proof), proof.Failure);
            Assert.AreSame(owner, SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition).Owner); Assert.AreEqual(model, proof.ModelId);
        }
        [TestCase("RawMeat", 1, true)] [TestCase("RawMeat", 1, false)]
        [TestCase("DriedMeat", 2, true)] [TestCase("DriedMeat", 2, false)]
        public void ActualRoleFeedingKeepsCurrentMeatViewUntilOneUnitCommitsAndLegacyCounterKeepsFood(string blueprint, int units, bool enabled)
        {
            using (var scope = new SpreadExplorationActorTests.Scope())
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                FactionManager.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Factions.json")));
                // Explicit controlled fixture replaces terrain only inside this test.
                f.Zone.RemoveEntity(f.Player); f.Zone = new Zone("Overworld.12.10.0"); f.Manager.SetActiveZone(f.Zone);
                Assert.True(f.Zone.AddEntity(f.Player, 60, 20));
                var hunter = Place(f, "Furrowstalker", 10, 10); var prey = Place(f, "ReedbackGrazer", 10, 16);
                var meat = Place(f, blueprint, 11, 10); meat.GetPart<StackerPart>().StackCount = units;
                var role = hunter.GetPart<SpreadPredatorPart>(); Configure(role, f.Zone, prey, enabled);
                f.Reveal(); f.Bind(f.Zone); f.Refresh();
                Assert.True(SpreadPortableRecipes.TryRecipe(meat, out var meatModel)); Exact(f, meat, meatModel); Exact(f, hunter, "spread-furrowstalker");
                var meatView = f.View(meat); int poses = 0; var observed = new System.Collections.Generic.List<int>();
                var prior = EntityVisualHooks.InteractionCallback;
                try
                {
                    EntityVisualHooks.InteractionCallback += (actor, target, zone) =>
                    {
                        if (actor != hunter) return;
                        Assert.True(enabled); Assert.AreSame(meat, target); Assert.AreSame(f.Zone, zone); Assert.NotNull(zone.GetEntityCell(target));
                        Assert.Contains(target, zone.GetReadOnlyEntities().ToArray()); Assert.AreSame(target, target.GetPart<PhysicsPart>().ParentEntity);
                        Assert.AreEqual(units, meat.GetPart<StackerPart>().StackCount);
                        Assert.AreEqual("Feeding", Phase(role)); Assert.That(Progress(role), Is.InRange(1, 2));
                        Exact(f, meat, meatModel); poses++; observed.Add(Progress(role));
                    };
                    Turn(hunter); f.Refresh(); Assert.AreSame(meatView, f.View(meat));
                    Turn(hunter); f.Refresh(); Assert.AreSame(meatView, f.View(meat));
                    Assert.AreEqual(enabled ? 2 : 0, poses); if (enabled) CollectionAssert.AreEqual(new[] { 1, 2 }, observed);
                    Turn(hunter); f.Refresh(); Assert.AreEqual(enabled ? "Consumed" : "None", Phase(role));
                    if (enabled && units == 1)
                    {
                        Assert.IsNull(f.Zone.GetEntityCell(meat)); Assert.False(f.Find(meat, out _, out _)); SpawnRing3DIntegrationFixture.Hidden(meatView);
                    }
                    else
                    {
                        Assert.NotNull(f.Zone.GetEntityCell(meat)); Assert.AreSame(meatView, f.View(meat)); Exact(f, meat, meatModel);
                        Assert.AreEqual(enabled ? units - 1 : units, meat.GetPart<StackerPart>().StackCount);
                    }
                    Exact(f, hunter, "spread-furrowstalker"); Assert.AreEqual(enabled ? 2 : 0, poses);
                }
                finally { EntityVisualHooks.InteractionCallback = prior; }
            }
        }
    }
}
