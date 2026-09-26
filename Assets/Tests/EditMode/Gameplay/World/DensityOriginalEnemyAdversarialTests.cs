using System;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityOriginalEnemyAdversarialTests
    {
        [TearDown] public void TearDown() => PlayerReputation.Reset();
        static Entity Actor(string id = "Snapjaw", string display = "snapjaw")
        {
            var e = new Entity { BlueprintName = id, ID = "custom-Snapjaw-identity" };
            e.AddPart(new RenderPart { DisplayName = display });
            return e;
        }
        [TestCase("SnapjawClaw", "MarlbackRake")]
        [TestCase("SnapjawHunterClaw", "MarlbackGuardRake")]
        [TestCase("WarlordCleaver", "BreacherCleaver")]
        [TestCase("MySnapjawClaw", "MySnapjawClaw")]
        public void SavedRecipePropertiesAreMappedExactly(string before, string after)
        {
            var e = Actor(); e.Properties["NaturalWeapon"] = before;
            e.Properties["Notes"] = before; e.Properties[before] = "custom field";
            var loaded = PartRoundTripHelper.RoundTripEntity(e);
            Assert.AreEqual(after, loaded.GetProperty("NaturalWeapon"));
            Assert.AreEqual(before, loaded.GetProperty("Notes"));
            Assert.AreEqual("custom field", loaded.Properties[before]);
            Assert.AreEqual(e.ID, loaded.ID);
        }
        [TestCase("SnapjawClaw", "MarlbackRake")]
        [TestCase("SnapjawHunterClaw", "MarlbackGuardRake")]
        [TestCase("WarlordCleaver", "BreacherCleaver")]
        [TestCase("CustomSnapjawClaw", "CustomSnapjawClaw")]
        public void SavedBodyRecipeMapsWithoutReplacingExistingFallbackReference(string before, string after)
        {
            var e = Actor(); var body = new Body(); e.AddPart(body);
            var weapon = Actor("MyNaturalPayload", "custom claw"); weapon.AddPart(new MeleeWeaponPart { BaseDamage = "9d3" });
            var root = new BodyPart { Type = "Body", Name = "body", ID = 88, DefaultBehaviorBlueprint = before, _DefaultBehavior = weapon };
            var child = new BodyPart { Type = "Hand", Name = "hand", ID = 89, DefaultBehaviorBlueprint = before, _DefaultBehavior = weapon };
            root.AddPart(child); body.SetBody(root);
            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(e).GetPart<Body>();
            Assert.AreEqual(after, loaded.GetBody().DefaultBehaviorBlueprint);
            Assert.AreEqual(after, loaded.GetPartByType("Hand").DefaultBehaviorBlueprint);
            Assert.AreSame(loaded.GetBody()._DefaultBehavior, loaded.GetPartByType("Hand")._DefaultBehavior);
            Assert.AreEqual("9d3", loaded.GetBody()._DefaultBehavior.GetPart<MeleeWeaponPart>().BaseDamage);
            Assert.AreEqual("custom claw", loaded.GetBody()._DefaultBehavior.GetPart<RenderPart>().DisplayName);
        }
        [TestCase("soot gremlin")] [TestCase("dirt gnome")]
        public void ACustomNameAloneCannotReclassifyAnOrdinarySavedCreatureAsAQuestActor(string name)
        {
            var loaded = PartRoundTripHelper.RoundTripEntity(Actor(display: name));
            Assert.AreEqual("MarlbackScrabbler", loaded.BlueprintName);
            Assert.AreEqual(name, loaded.GetPart<RenderPart>().DisplayName);
        }
        [TestCase(true)] [TestCase(false)]
        public void AuthoredQuestFactIdentifiesLegacyReskinAndKeepsItsDeathProgress(bool soot)
        {
            var e = Actor(display: soot ? "soot gremlin" : "dirt gnome");
            if (soot) e.AddPart(new SetFactWhenSlain { Fact = "rbg_gremlin_routed", Value = 1 });
            else e.AddPart(new AddFactWhenSlain { Fact = "warren_gnomes_routed", Amount = 1 });
            e.AddPart(new CorpsePart { CorpseBlueprint = "SnapjawCorpse" });
            var loaded = PartRoundTripHelper.RoundTripEntity(e);
            Assert.AreEqual(soot ? "SootGremlin" : "DirtGnome", loaded.BlueprintName);
            Assert.AreEqual("CreatureCorpse", loaded.GetPart<CorpsePart>().CorpseBlueprint);
            if (soot) Assert.AreEqual("rbg_gremlin_routed", loaded.GetPart<SetFactWhenSlain>().Fact);
            else Assert.AreEqual(1, loaded.GetPart<AddFactWhenSlain>().Amount);
        }
        [Test] public void SavedCurrentReputationWinsOverStaleLegacyEntry()
        {
            PlayerReputation.Set("Snapjaws", 127); PlayerReputation.Set("OutlandRaiders", -33);
            PlayerReputation.Set("MySnapjaws", 91);
            using (var stream = new MemoryStream())
            {
                SaveGraphSerializer.SavePlayerReputation(new SaveWriter(stream)); stream.Position = 0;
                SaveGraphSerializer.LoadPlayerReputation(new SaveReader(stream, null));
            }
            Assert.AreEqual(-33, PlayerReputation.Get("OutlandRaiders"));
            Assert.AreEqual(91, PlayerReputation.Get("MySnapjaws"));
            Assert.IsFalse(PlayerReputation.GetAll().ContainsKey("Snapjaws"));
        }
        [TestCase("Snapjaw Lair", "Marlback Burrow", "Snapjaws", "OutlandRaiders", "SnapjawChieftain", "MarlbackWallkeeper")]
        [TestCase("My Snapjaw refuge", "My Snapjaw refuge", "MySnapjaws", "MySnapjaws", "CustomSnapjaw", "CustomSnapjaw")]
        public void SavedLairReferencesMapWithoutRewritingCustomPlaceNames(string beforeName, string afterName, string beforeFaction, string afterFaction, string beforeBoss, string afterBoss)
        {
            var poi = new PointOfInterest(POIType.Lair, beforeName, beforeFaction, 4, beforeBoss);
            using (var stream = new MemoryStream())
            {
                typeof(SaveGraphSerializer).GetMethod("SavePointOfInterest", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { poi, new SaveWriter(stream) });
                stream.Position = 0;
                var loaded = (PointOfInterest)typeof(SaveGraphSerializer).GetMethod("LoadPointOfInterest", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { new SaveReader(stream, null) });
                Assert.AreEqual(afterName, loaded.Name); Assert.AreEqual(afterFaction, loaded.Faction);
                Assert.AreEqual(afterBoss, loaded.BossBlueprint); Assert.AreEqual(4, loaded.Tier); Assert.AreEqual(POIType.Lair, loaded.Type);
            }
        }
        [TestCase(true)] [TestCase(false)]
        public void CleaverIdentityReplacesOnlyDefaultItemFlavor(bool custom)
        {
            var e = Actor("WarlordCleaver", custom ? "My heirloom" : "warlord's cleaver");
            string text = custom ? "A personal note about a snapjaw." : "A snapjaw warband banner-blade, notched from a hundred raids. Heavier than it looks; meaner than it needs to be.";
            e.AddPart(new ExaminablePart { Description = text });
            var loaded = PartRoundTripHelper.RoundTripEntity(e);
            Assert.AreEqual("BreacherCleaver", loaded.BlueprintName);
            Assert.AreEqual(custom ? "My heirloom" : "wedge-cleaver", loaded.GetPart<RenderPart>().DisplayName);
            if (custom) Assert.AreEqual(text, loaded.GetPart<ExaminablePart>().Description);
            else StringAssert.DoesNotContain("snapjaw", loaded.GetPart<ExaminablePart>().Description);
        }
        [Test] public void DeathAndUnknownFactionStatesAreNotResetByIdentityMigration()
        {
            var e = Actor(); e.Tags["Faction"] = "CustomSnapjaws";
            e.IntProperties["_DeathHandled"] = 1; e.Tags["KilledSnapjaw"] = "legacy";
            e.Tags["KilledMarlback"] = "current";
            var loaded = PartRoundTripHelper.RoundTripEntity(e);
            Assert.AreEqual("CustomSnapjaws", loaded.GetTag("Faction"));
            Assert.AreEqual(1, loaded.GetIntProperty("_DeathHandled"));
            Assert.AreEqual("current", loaded.GetTag("KilledMarlback"));
            Assert.IsFalse(loaded.HasTag("KilledSnapjaw"));
        }

        [TestCase("CinnamonBunFavor", "drive_off_gremlin", "SootGremlin")]
        [TestCase("OtherQuest", "drive_off_gremlin", "MarlbackScrabbler")]
        [TestCase("CinnamonBunFavor", "other_objective", "MarlbackScrabbler")]
        public void LegacyPlayableQuestMarkerIsRecognizedOnlyForItsExactQuestAndObjective(string quest, string objective, string expected)
        {
            var e = Actor(display: "soot gremlin");
            e.AddPart(new FinishObjectiveWhenSlain { Quest = quest, Objective = objective });
            var loaded = PartRoundTripHelper.RoundTripEntity(e);
            Assert.AreEqual(expected, loaded.BlueprintName);
            Assert.AreEqual(quest, loaded.GetPart<FinishObjectiveWhenSlain>().Quest);
            Assert.AreEqual(objective, loaded.GetPart<FinishObjectiveWhenSlain>().Objective);
        }
        [TestCase(true)] [TestCase(false)]
        public void LegacyQuestRecipesAreGenericIncludingDetachedLimbsWithoutReplacingNaturalPayloads(bool soot)
        {
            var e = Actor(display: soot ? "soot gremlin" : "dirt gnome");
            if (soot) e.AddPart(new SetFactWhenSlain { Fact = "rbg_gremlin_routed", Value = 1 });
            else e.AddPart(new AddFactWhenSlain { Fact = "warren_gnomes_routed", Amount = 1 });
            e.Properties["NaturalWeapon"] = "SnapjawClaw";
            var body = new Body(); e.AddPart(body);
            var natural = Actor("MyWeapon", "custom rake"); natural.AddPart(new MeleeWeaponPart { BaseDamage = "7d2" });
            var hand = new BodyPart { ID = 12, Type = "Hand", DefaultBehaviorBlueprint = "SnapjawClaw", _DefaultBehavior = natural };
            body.SetBody(hand);
            body.DismemberedParts.Add(new DismemberedPart { Part = new BodyPart { ID = 13, Type = "Hand", DefaultBehaviorBlueprint = "SnapjawClaw", _DefaultBehavior = natural }, ParentPartID = 12, OriginalPosition = 2 });
            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(e);
            var b = loaded.GetPart<Body>();
            Assert.AreEqual("ScavengerClaw", loaded.GetProperty("NaturalWeapon"));
            Assert.AreEqual("ScavengerClaw", b.GetBody().DefaultBehaviorBlueprint);
            Assert.AreEqual("ScavengerClaw", b.DismemberedParts[0].Part.DefaultBehaviorBlueprint);
            Assert.AreSame(b.GetBody()._DefaultBehavior, b.DismemberedParts[0].Part._DefaultBehavior);
            Assert.AreEqual("7d2", b.GetBody()._DefaultBehavior.GetPart<MeleeWeaponPart>().BaseDamage);
            Assert.AreEqual(2, b.DismemberedParts[0].OriginalPosition);
        }
        [Test] public void RenamedQuestCreatureRetainsItsCustomNameAndCustomRecipe()
        {
            var e = Actor(display: "Sooty");
            e.AddPart(new SetFactWhenSlain { Fact = "rbg_gremlin_routed", Value = 1 });
            e.Properties["NaturalWeapon"] = "CustomSnapjawClaw";
            var loaded = PartRoundTripHelper.RoundTripEntity(e);
            Assert.AreEqual("SootGremlin", loaded.BlueprintName);
            Assert.AreEqual("Sooty", loaded.GetPart<RenderPart>().DisplayName);
            Assert.AreEqual("CustomSnapjawClaw", loaded.GetProperty("NaturalWeapon"));
        }
    }
}
