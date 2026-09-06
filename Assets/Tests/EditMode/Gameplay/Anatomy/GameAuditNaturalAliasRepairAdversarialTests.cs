using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GameAuditNaturalAliasRepairAdversarialTests
    {
        static Entity[] Defaults(Entity actor) => actor.GetPart<Body>().GetPartsByType("Hand").Select(h => h._DefaultBehavior).ToArray();
        static int Gathered(Entity actor) => ((IList)typeof(CombatSystem).GetMethod("GatherMeleeWeapons", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { actor, actor.GetPart<Body>() })).Count;
        static Entity RoundTrip(Entity actor)
        {
            var zone = new Zone("Overworld.10.10.0"); Assert.IsTrue(zone.AddEntity(actor, 5, 5));
            var manager = new OverworldZoneManager(null, 333);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } }, zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            var turns = new TurnManager(); turns.RestoreSavedState(17, true, actor,
                new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = actor, Energy = 1000 } });
            return HotbarSaveFixture.RoundTrip(GameSessionState.Capture(Guid.NewGuid().ToString("N"), "natural alias", manager, turns, actor)).Player;
        }
        [Test]
        public void SharedCustomNaturalInDetachedNonFirstHandDoesNotGainASecondAttackAfterLoadAndHealing()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SnapjawWarlord"); var body = actor.GetPart<Body>(); var hands = body.GetPartsByType("Hand");
                Assert.NotNull(hands[0]._DefaultBehavior, "Fresh factory must materialize without fixture repair.");
                var shared = hands[0]._DefaultBehavior; shared.ID = "shared-custom-natural"; shared.GetPart<MeleeWeaponPart>().BaseDamage = "3d7";
                hands[1]._DefaultBehavior = shared; body.GetBody().RecalculateFirstDefaultBehavior();
                Assert.IsTrue(hands[0].FirstSlotForDefaultBehavior); Assert.IsFalse(hands[1].FirstSlotForDefaultBehavior); Assert.AreEqual(1, Gathered(actor));
                int detachedID = hands[1].ID; Assert.IsTrue(body.Dismember(hands[1]));
                Assert.IsFalse(hands[1].FirstSlotForDefaultBehavior);
                var loaded = RoundTrip(actor); var restored = loaded.GetPart<Body>();
                var detached = restored.DismemberedParts.Single(x => x.Part.ID == detachedID).Part;
                var attached = restored.GetPartsByType("Hand").Single(); bool savedNonFirst = !detached.FirstSlotForDefaultBehavior;
                Assert.AreSame(attached._DefaultBehavior, detached._DefaultBehavior, "Full saved token graph must keep the shared custom alias.");
                Assert.AreEqual("3d7", detached._DefaultBehavior.GetPart<MeleeWeaponPart>().BaseDamage);
                Assert.IsTrue(restored.RegenerateLimb("Hand"));
                Assert.AreSame(attached._DefaultBehavior, restored.GetParts().Single(x => x.ID == detachedID)._DefaultBehavior);
                Assert.IsTrue(savedNonFirst, "Loading detached roots must not invent a second first-default flag for an existing shared reference.");
                Assert.AreEqual(1, restored.GetPartsByType("Hand").Count(x => x.FirstSlotForDefaultBehavior));
                Assert.AreEqual(1, Gathered(loaded), "Healing must not swing one shared custom weapon twice.");
            }
        }
        [Test]
        public void SharedCustomNaturalOnBothAttachedHandsStillHasExactlyOneAttackAfterLoad()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SnapjawWarlord"); var body = actor.GetPart<Body>(); var hands = body.GetPartsByType("Hand");
                Assert.NotNull(hands[0]._DefaultBehavior); hands[1]._DefaultBehavior = hands[0]._DefaultBehavior;
                body.GetBody().RecalculateFirstDefaultBehavior(); Assert.AreEqual(1, Gathered(actor));
                var loaded = RoundTrip(actor); var restored = loaded.GetPart<Body>().GetPartsByType("Hand");
                Assert.AreSame(restored[0]._DefaultBehavior, restored[1]._DefaultBehavior);
                Assert.AreEqual(1, restored.Count(x => x.FirstSlotForDefaultBehavior)); Assert.AreEqual(1, Gathered(loaded));
            }
        }
        [Test]
        public void UniqueDetachedNaturalRetainsItsOwnFirstFlagAndReturnsAsTheSecondWeapon()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var actor = f.Create("SnapjawWarlord"); var body = actor.GetPart<Body>(); var hands = body.GetPartsByType("Hand");
                Assert.NotNull(hands[0]._DefaultBehavior); Assert.NotNull(hands[1]._DefaultBehavior);
                Assert.AreNotSame(hands[0]._DefaultBehavior, hands[1]._DefaultBehavior); Assert.AreEqual(2, Gathered(actor));
                int detachedID = hands[1].ID; Assert.IsTrue(body.Dismember(hands[1]));
                var loaded = RoundTrip(actor); var restored = loaded.GetPart<Body>(); var detached = restored.DismemberedParts.Single(x => x.Part.ID == detachedID).Part;
                var attached = restored.GetPartsByType("Hand").Single(); Assert.AreNotSame(attached._DefaultBehavior, detached._DefaultBehavior);
                Assert.IsTrue(detached.FirstSlotForDefaultBehavior); Assert.AreEqual(1, Gathered(loaded));
                Assert.IsTrue(restored.RegenerateLimb("Hand")); Assert.AreEqual(2, Gathered(loaded));
            }
        }
    }
}
