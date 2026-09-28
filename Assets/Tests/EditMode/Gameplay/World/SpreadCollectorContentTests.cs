using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadCollectorContentTests
    {
        EntityFactory factory;
        [SetUp] public void Setup()
        {
            factory = new EntityFactory();
            string path = Environment.GetEnvironmentVariable("COO_COLLECTOR_BLUEPRINTS")
                ?? Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            factory.LoadBlueprints(File.ReadAllText(path));
        }
        Entity Make(string name)
        {
            Assert.True(factory.Blueprints.ContainsKey(name), "Original collector content missing: " + name);
            return factory.CreateEntity(name);
        }
        [Test] public void CollectorUsesActualBirdAnatomyWithoutInventedWeaponsOrHumanoidHands()
        {
            var actor = Make("Tatterjay"); var body = actor.GetPart<Body>();
            Assert.NotNull(body); Assert.AreEqual("Avian", actor.GetProperty("Anatomy"));
            Assert.AreEqual(2, body.GetPartsByType("Wing").Count);
            Assert.AreEqual(0, body.GetPartsByType("Hand").Count);
            Assert.NotNull(body.GetPartByType("Head")); Assert.NotNull(body.GetPartByType("Tail"));
            Assert.False(actor.HasTag("BodyNaturalAttack"));
            Assert.True(string.IsNullOrEmpty(actor.GetProperty("NaturalWeapon")));
            Assert.False(body.GetParts().Any(p => p.DefaultBehavior != null));
            Assert.True(actor.GetPart<BrainPart>().Passive); Assert.AreEqual("Villagers", actor.GetTag("Faction"));
            Assert.AreEqual(8, actor.GetStatValue("Hitpoints")); Assert.AreEqual(0, actor.GetStatValue("XPValue"));
        }
        [Test] public void CollectorHasScopedEmptyRoleWithoutGenericShinyHoardingOrStartingStock()
        {
            var actor = Make("Tatterjay"); var role = actor.GetPart("SpreadCollector");
            Assert.NotNull(role); Assert.False(actor.HasPart<AIHoarderPart>());
            Assert.IsEmpty(actor.GetPart<InventoryPart>().Objects);
            Assert.IsEmpty(actor.GetPart<InventoryPart>().GetAllEquipped());
            Assert.AreEqual(20, actor.GetPart<InventoryPart>().MaxWeight);
            Assert.IsNull(role.GetType().GetProperty("CurrentCarriedItem").GetValue(role));
            Assert.IsNull(role.GetType().GetField("Home").GetValue(role));
            Assert.IsNull(role.GetType().GetField("Target").GetValue(role));
            Assert.AreEqual("j", actor.GetPart<RenderPart>().RenderString);
            Assert.AreEqual("&c", actor.GetPart<RenderPart>().ColorString);
        }
        [Test] public void OriginalRemainsHaveCorpseIdentityWithoutBonusFoodOrSalvage()
        {
            var actor = Make("Tatterjay"); var corpse = Make("TatterjayCorpse");
            Assert.AreEqual("TatterjayCorpse", actor.GetPart<CorpsePart>().CorpseBlueprint);
            Assert.AreEqual(70, actor.GetPart<CorpsePart>().CorpseChance);
            Assert.True(corpse.HasTag("Corpse")); Assert.True(corpse.GetPart<PhysicsPart>().Takeable);
            Assert.False(corpse.HasPart("LootDrop")); Assert.False(corpse.HasPart<HarvestablePart>());
            Assert.False(corpse.HasPart("SpreadCollector")); Assert.False(corpse.HasPart<AIHoarderPart>());
            Assert.AreEqual("%", corpse.GetPart<RenderPart>().RenderString);
            Assert.AreEqual("&c", corpse.GetPart<RenderPart>().ColorString);
            StringAssert.Contains("tatterjay", corpse.GetDisplayName());
        }
        [Test] public void OrdinaryStockingLeavesTheCollectorEmptyAndPreservesOtherBirdStockAndRng()
        {
            var collector = Make("Tatterjay"); var ordinary = Make("Magpie"); var control = Make("Magpie");
            Assert.True(collector.HasTag("NoRandomStock")); Assert.False(ordinary.HasTag("NoRandomStock"));
            var zone = new Zone("collector-stock"); var baseline = new Zone("collector-stock");
            zone.AddEntity(collector, 5, 5); zone.AddEntity(ordinary, 8, 5); baseline.AddEntity(control, 8, 5);
            var actualRng = new System.Random(73); var baselineRng = new System.Random(73);
            new TradeStockBuilder().BuildZone(zone, factory, actualRng);
            new TradeStockBuilder().BuildZone(baseline, factory, baselineRng);
            Assert.IsEmpty(collector.GetPart<InventoryPart>().Objects);
            Assert.IsNotEmpty(ordinary.GetPart<InventoryPart>().Objects);
            Func<Entity, string[]> stock = e => e.GetPart<InventoryPart>().Objects
                .Select(i => i.BlueprintName + ":" + (i.GetPart<StackerPart>()?.StackCount ?? 1)).ToArray();
            CollectionAssert.AreEqual(stock(control), stock(ordinary));
            Assert.AreEqual(baselineRng.Next(), actualRng.Next());
        }
        [Test] public void ExistingMagpieAndDogKeepTheirOwnBehaviorAndDoNotAcquireCollectorState()
        {
            var magpie = Make("Magpie"); var dog = Make("PetDog");
            Assert.NotNull(magpie.GetPart<AIHoarderPart>());
            Assert.AreEqual("Shiny", magpie.GetPart<AIHoarderPart>().TargetTag);
            Assert.AreEqual(15, magpie.GetPart<AIHoarderPart>().Chance);
            Assert.AreEqual("b", magpie.GetPart<RenderPart>().RenderString);
            Assert.False(magpie.HasPart("SpreadCollector")); Assert.False(dog.HasPart("SpreadCollector"));
        }
    }
}
