using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GameAuditNaturalWeaponActivationTests
    {
        sealed class HighRoll : Random
        {
            int _calls;
            void Budget() { if (++_calls > 500) throw new InvalidOperationException("Natural attack RNG exceeded its finite budget."); }
            public override int Next(int maxValue) { Budget(); return Math.Max(0, maxValue - 1); }
            public override int Next(int minValue, int maxValue)
            {
                Budget();
                // Penetration d10=10 explodes; a constant maximum would never terminate.
                return minValue == 1 && maxValue == 11 ? 9 : Math.Max(minValue, maxValue - 1);
            }
        }
        static GameSessionState RoundTrip(Entity actor)
        {
            var zone = new Zone("Overworld.10.10.0"); Assert.IsTrue(zone.AddEntity(actor, 5, 5));
            var manager = new OverworldZoneManager(null, 333);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } }, zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            var turns = new TurnManager(); turns.RestoreSavedState(17, true, actor,
                new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = actor, Energy = 1000 } });
            return HotbarSaveFixture.RoundTrip(GameSessionState.Capture(Guid.NewGuid().ToString("N"), "natural activation", manager, turns, actor));
        }

        [TestCase("SnapjawWarlord", "cleaver", "2d5", "Axe")]
        [TestCase("SkeletalSentry", "bone blade", "1d6+1", "Cutting")]
        public void FreshOrdinaryEnemyDispatchesItsDeclaredNaturalAttack(string name, string weapon, string dice, string attribute)
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create(name); actor.ID = "GA03i-natural-" + Guid.NewGuid().ToString("N");
                var target = HaulLifecycleFixture.MakeActor("natural attack target");
                target.GetStat("Hitpoints").BaseValue = 10000; target.GetStat("Hitpoints").Max = 10000;
                var zone = new Zone("natural-first-swing"); Assert.IsTrue(zone.AddEntity(actor, 5, 5)); Assert.IsTrue(zone.AddEntity(target, 6, 5));
                bool old = Diag.IsChannelEnabled("damage"); Diag.SetChannel("damage", true);
                try
                {
                    Assert.IsTrue(CombatSystem.PerformMeleeAttack(actor, target, zone, new HighRoll()));
                    var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "DamageRoll", Actor = actor.ID, Limit = 10 }).Records;
                    Assert.Greater(records.Count, 0, "The legal first swing must actually dispatch damage.");
                    StringAssert.Contains("\"weapon\":\"" + weapon + "\"", records[0].PayloadJson);
                    StringAssert.Contains("\"damageDice\":\"" + dice + "\"", records[0].PayloadJson);
                    StringAssert.Contains(attribute, records[0].PayloadJson);
                    Assert.Less(target.GetStatValue("Hitpoints"), 10000);
                }
                finally { Diag.SetChannel("damage", old); }
            }
        }

        [TestCase("SnapjawWarlord", "2d5")] [TestCase("SkeletalSentry", "1d6+1")]
        public void OlderSavedMissingDefaultsAreRepairedWithoutRestockingGear(string name, string dice)
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create(name); var hands = actor.GetPart<Body>().GetPartsByType("Hand");
                foreach (var hand in hands) { hand._DefaultBehavior = null; hand.FirstSlotForDefaultBehavior = false; }
                var gear = actor.GetPart<InventoryPart>().GetAllEquipped().Select(x => x.ID).OrderBy(x => x).ToArray();
                f.Messages.Clear(); var loaded = RoundTrip(actor).Player;
                foreach (var hand in loaded.GetPart<Body>().GetPartsByType("Hand"))
                { Assert.NotNull(hand._DefaultBehavior); Assert.AreEqual(dice, hand._DefaultBehavior.GetPart<MeleeWeaponPart>().BaseDamage); Assert.IsTrue(hand.FirstSlotForDefaultBehavior); }
                CollectionAssert.AreEqual(gear, loaded.GetPart<InventoryPart>().GetAllEquipped().Select(x => x.ID).OrderBy(x => x).ToArray());
                Assert.IsFalse(f.Messages.Any(x => x.Contains(" equips ")));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void SavedCustomDefaultObjectsSurviveWithOrWithoutDeclaration(bool declaration)
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SnapjawWarlord"); var hand = actor.GetPart<Body>().GetPartsByType("Hand")[0];
                var custom = NaturalWeaponFactory.Create("DefaultFist"); custom.ID = "custom-natural";
                custom.GetPart<MeleeWeaponPart>().BaseDamage = "3d7";
                hand._DefaultBehavior = custom; if (!declaration) hand.DefaultBehaviorBlueprint = "";
                actor.GetPart<Body>().GetBody().RecalculateFirstDefaultBehavior();
                var loaded = RoundTrip(actor).Player; var restored = loaded.GetPart<Body>().GetParts().Single(x => x.ID == hand.ID)._DefaultBehavior;
                Assert.NotNull(restored); Assert.AreEqual("custom-natural", restored.ID); Assert.AreEqual("3d7", restored.GetPart<MeleeWeaponPart>().BaseDamage);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void OldDetachedHandReturnsWithItsNaturalWeaponWhenHealed(bool missing)
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SnapjawWarlord"); var body = actor.GetPart<Body>();
                var hand = body.GetPartsByType("Hand")[0];
                hand._DefaultBehavior = missing ? null : NaturalWeaponFactory.Create("WarlordCleaver");
                if (!missing) hand._DefaultBehavior.ID = "retained-detached-natural";
                Assert.IsTrue(body.Dismember(hand));
                var loaded = RoundTrip(actor).Player; var restored = loaded.GetPart<Body>();
                var detached = restored.DismemberedParts.Single(x => x.Part.ID == hand.ID).Part;
                Assert.IsNull(detached.ParentPart); Assert.IsFalse(restored.GetParts().Contains(detached));
                Assert.NotNull(detached._DefaultBehavior);
                var natural = detached._DefaultBehavior;
                Assert.AreEqual("2d5", natural.GetPart<MeleeWeaponPart>().BaseDamage);
                if (!missing) Assert.AreEqual("retained-detached-natural", natural.ID);
                Assert.IsTrue(restored.RegenerateLimb("Hand"));
                Assert.AreSame(natural, restored.GetParts().Single(x => x.ID == hand.ID)._DefaultBehavior);
            }
        }

        [Test]
        public void UnknownSavedRecipeDoesNotInventAFallbackOrAlterStoredFlags()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SnapjawWarlord"); var hand = actor.GetPart<Body>().GetPartsByType("Hand")[0];
                hand.DefaultBehaviorBlueprint = "custom-unsupported-recipe";
                hand._DefaultBehavior = null; hand.FirstSlotForDefaultBehavior = false; int flags = hand.Flags;
                var loaded = RoundTrip(actor).Player.GetPart<Body>().GetParts().Single(x => x.ID == hand.ID);
                Assert.IsNull(loaded._DefaultBehavior); Assert.AreEqual(flags, loaded.Flags);
                Assert.AreEqual("custom-unsupported-recipe", loaded.DefaultBehaviorBlueprint);
            }
        }

        [Test]
        public void ExplicitUnknownRecipeFactoryRetainsItsHistoricalFallback()
        {
            var item = NaturalWeaponFactory.Create("custom-unsupported-recipe");
            Assert.NotNull(item); Assert.AreEqual("1d2", item.GetPart<MeleeWeaponPart>().BaseDamage);
            Assert.IsTrue(item.HasTag("Natural"));
        }

        [TestCase("SummitSinger")] [TestCase("SkySari")]
        public void HandlessAnatomyCannotAcquireFictitiousNaturalHands(string name)
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create(name); Assert.AreEqual(0, actor.GetPart<Body>().GetPartsByType("Hand").Count);
                Assert.IsFalse(actor.GetPart<Body>().GetParts().Any(x => x._DefaultBehavior != null));
                Assert.AreEqual(0, actor.GetPart<InventoryPart>().GetAllEquipped().Count);
            }
        }
    }
}
