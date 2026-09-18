using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Hypotheses target malformed data, forged identities, occupied doorways,
    // cross-instance actions, repeated removal and persistence boundaries.
    public sealed class MorrowfastSceneAdversarialTests
    {
        private EntityFactory factory;
        [SetUp] public void Setup() => factory = MorrowfastTestWorld.Factory();
        private Zone Fresh() => new OverworldZoneManager(factory, 64).GetZone(MorrowfastSceneRuntime.ZoneID);

        [TestCase(null)] [TestCase("")] [TestCase(" ")] [TestCase("{}")] [TestCase("[]")]
        public void EmptyOrIncompleteDefinitionFailsClosed(string json)
            => Assert.Throws<ArgumentException>(() => MorrowfastSceneDefinition.Parse(json));

        [TestCase("duplicate-owner")] [TestCase("duplicate-cell")] [TestCase("missing-cell")]
        [TestCase("outside-cell")] [TestCase("outside-anchor")] [TestCase("missing-roof")] [TestCase("missing-door")]
        public void MalformedDomainReferencesCannotValidate(string mutation)
        {
            var d = JsonUtility.FromJson<MorrowfastSceneDefinition>(JsonUtility.ToJson(MorrowfastSceneDefinition.Load()));
            switch (mutation)
            {
                case "duplicate-owner": d.owners[1].id = d.owners[0].id; break;
                case "duplicate-cell": d.cells[1].x = d.cells[0].x; d.cells[1].y = d.cells[0].y; break;
                case "missing-cell": d.cells = d.cells.Skip(1).ToArray(); break;
                case "outside-cell": d.cells[0].x = 80; break;
                case "outside-anchor": d.owners[0].anchorY = 25; break;
                case "missing-roof": d.buildings[0].roofId = "missing-roof"; break;
                case "missing-door": d.buildings[0].doorId = "missing-door"; break;
            }
            Assert.Throws<ArgumentException>(d.Validate);
        }
        [Test] public void WrongZoneUpgradeDoesNotTouchForeignOccupants()
        {
            var z = new Zone("Overworld.2.6.0"); var actor = MorrowfastTestWorld.Actor(factory, z);
            Assert.IsFalse(MorrowfastSceneRuntime.UpgradeCachedZone(z, factory));
            CollectionAssert.AreEqual(new[] { actor }, z.GetAllEntities()); Assert.IsFalse(MorrowfastSceneRuntime.IsActive(z));
        }
        [Test] public void NullAndUnknownLookupsDoNotInventAnOwner()
        {
            Assert.IsFalse(MorrowfastSceneRuntime.IsActive(null)); Assert.IsNull(MorrowfastSceneRuntime.FindOwner(null, null));
            Assert.IsFalse(MorrowfastSceneRuntime.IsPresent(null, "missing")); var z = Fresh();
            Assert.IsNull(MorrowfastSceneRuntime.FindOwner(z, "missing")); Assert.IsFalse(MorrowfastSceneRuntime.IsPresent(z, "missing"));
        }
        [TestCase("null")] [TestCase("remote")] [TestCase("dead")] [TestCase("detached")] [TestCase("wrong-zone")]
        public void InvalidClearDoesNotRemoveTheOwnerOrChangeDurableState(string reason)
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); var e = MorrowfastTestWorld.Clearable(z, actor);
            string before = JsonUtility.ToJson(MorrowfastSceneRuntime.GetState(z));
            if (reason == "remote") z.MoveEntity(actor, 79, 24);
            if (reason == "dead") actor.GetStat("Hitpoints").Penalty = actor.GetStat("Hitpoints").BaseValue;
            if (reason == "detached") z.RemoveEntity(actor);
            Assert.IsFalse(e.GetPart<MorrowfastPropPart>().TryRemove(reason == "null" ? null : actor, reason == "wrong-zone" ? new Zone() : z));
            Assert.IsNotNull(z.GetEntityCell(e)); Assert.AreEqual(before, JsonUtility.ToJson(MorrowfastSceneRuntime.GetState(z)));
        }
        [Test] public void RepeatedClearCannotSucceedTwice()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); var e = MorrowfastTestWorld.Clearable(z, actor);
            var part = e.GetPart<MorrowfastPropPart>(); Assert.IsTrue(part.TryRemove(actor, z));
            string state = JsonUtility.ToJson(MorrowfastSceneRuntime.GetState(z)); Assert.IsFalse(part.TryRemove(actor, z));
            Assert.AreEqual(state, JsonUtility.ToJson(MorrowfastSceneRuntime.GetState(z)));
        }
        [Test] public void ForgedDuplicatePartCannotRemoveTheRealSourceOwner()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); var e = MorrowfastTestWorld.Clearable(z, actor);
            string id = e.GetPart<MorrowfastPropPart>().ComponentId; var p = z.GetEntityPosition(actor);
            var fake = new Entity { ID = Guid.NewGuid().ToString("N") }; fake.AddPart(new MorrowfastPropPart { ComponentId = id });
            Assert.IsTrue(z.AddEntity(fake, p.x, p.y)); Assert.IsFalse(fake.GetPart<MorrowfastPropPart>().TryRemove(actor, z));
            Assert.AreSame(e, MorrowfastSceneRuntime.FindOwner(z, id)); Assert.IsNotNull(z.GetEntityCell(e));
        }
        [Test] public void DisplacedSceneryCannotControlArtworkAtItsOldPosition()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); var e = MorrowfastTestWorld.Clearable(z, actor);
            z.MoveEntity(e, 78, 24); z.MoveEntity(actor, 79, 24);
            Assert.IsFalse(e.GetPart<MorrowfastPropPart>().TryRemove(actor, z)); Assert.IsNotNull(z.GetEntityCell(e));
        }
        [Test] public void SameCoordinatesInAnotherInstanceDoNotAuthorizeAnAction()
        {
            var a = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, a); var e = MorrowfastTestWorld.Clearable(a, actor); var p = a.GetEntityPosition(actor);
            var b = Fresh(); var stranger = MorrowfastTestWorld.Actor(factory, b, p.x, p.y);
            Assert.IsFalse(e.GetPart<MorrowfastPropPart>().TryRemove(stranger, b)); Assert.IsNotNull(a.GetEntityCell(e));
        }
        [TestCase("player")] [TestCase("other-actor")]
        public void DoorCloseRefusesAnOccupiedOpening(string blocker)
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); var b = MorrowfastSceneDefinition.Load().buildings[0];
            var e = MorrowfastSceneRuntime.FindOwner(z, b.doorId); var p = MorrowfastTestWorld.Approach(z, actor, e); z.MoveEntity(actor, p.x, p.y);
            var door = e.GetPart<MorrowfastDoorPart>(); Assert.IsTrue(door.TrySetOpen(actor, z, true)); var at = z.GetEntityPosition(e);
            if (blocker == "player") z.MoveEntity(actor, at.x, at.y); else MorrowfastTestWorld.Actor(factory, z, at.x, at.y);
            Assert.IsFalse(door.TrySetOpen(actor, z, false)); Assert.IsTrue(door.IsOpen);
        }
        [TestCase("remote")] [TestCase("dead")] [TestCase("detached")] [TestCase("wrong-zone")]
        public void InvalidDoorActionLeavesItClosed(string reason)
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); var b = MorrowfastSceneDefinition.Load().buildings[0];
            var e = MorrowfastSceneRuntime.FindOwner(z, b.doorId); var p = MorrowfastTestWorld.Approach(z, actor, e); z.MoveEntity(actor, p.x, p.y);
            if (reason == "remote") z.MoveEntity(actor, 79, 24);
            if (reason == "dead") actor.GetStat("Hitpoints").Penalty = actor.GetStat("Hitpoints").BaseValue;
            if (reason == "detached") z.RemoveEntity(actor);
            Assert.IsFalse(e.GetPart<MorrowfastDoorPart>().TrySetOpen(actor, reason == "wrong-zone" ? new Zone() : z, true));
            Assert.IsFalse(MorrowfastSceneRuntime.IsDoorOpen(z, b.doorId));
        }
        [Test] public void MissingFactoryContentCannotEraseALegacyZone()
        {
            var z = new Zone(MorrowfastSceneRuntime.ZoneID); var item = factory.CreateEntity("Tepuibone"); Assert.IsTrue(z.AddEntity(item, 40, 24));
            var empty = new EntityFactory(); Assert.IsFalse(MorrowfastSceneRuntime.UpgradeCachedZone(z, empty));
            CollectionAssert.AreEqual(new[] { item }, z.GetAllEntities()); Assert.IsFalse(MorrowfastSceneRuntime.IsActive(z));
        }
        [Test] public void CachedUpgradePreservesAChangedSceneWithoutRespawningRemovedOwners()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); var e = MorrowfastTestWorld.Clearable(z, actor);
            string id = e.GetPart<MorrowfastPropPart>().ComponentId; Assert.IsTrue(e.GetPart<MorrowfastPropPart>().TryRemove(actor, z));
            var identities = z.GetAllEntities().Select(x => x.ID).OrderBy(x => x).ToArray();
            Assert.IsTrue(MorrowfastSceneRuntime.UpgradeCachedZone(z, factory)); Assert.IsFalse(MorrowfastSceneRuntime.IsPresent(z, id));
            CollectionAssert.AreEqual(identities, z.GetAllEntities().Select(x => x.ID).OrderBy(x => x).ToArray());
        }
        [Test] public void ClearingARealLargePropReleasesItsAuthoredFootprint()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); var owner = MorrowfastTestWorld.Clearable(z, actor);
            var spec = MorrowfastSceneDefinition.Load().owners.Single(x => x.id == "provisioners-stall");
            Assert.IsTrue(spec.blocksMovement); Assert.Greater(spec.footprint.Length, 1, "Exercise a genuinely multicell obstruction.");
            var blocked = spec.footprint.Where(c => z.GetCell(c.x, c.y).BlocksMovement(actor)).ToArray();
            Assert.IsNotEmpty(blocked); Assert.IsTrue(owner.GetPart<MorrowfastPropPart>().TryRemove(actor, z));
            Assert.IsTrue(blocked.Any(c => !z.GetCell(c.x, c.y).BlocksMovement(actor)), "At least one real cell must open; owner deletion alone is insufficient.");
        }
        [Test] public void OccupiedFootbridgeCannotBeRemovedFromUnderAnActor()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z);
            var spec = MorrowfastSceneDefinition.Load().owners.Single(x => x.id == "western-footbridge");
            var owner = MorrowfastSceneRuntime.FindOwner(z, spec.id); var p = MorrowfastTestWorld.Approach(z, actor, owner); z.MoveEntity(actor, p.x, p.y);
            Assert.IsNotEmpty(spec.bridgeSupport); var crossing = spec.bridgeSupport.First(c => !z.GetCell(c.x, c.y).BlocksMovement(actor));
            var visitor = MorrowfastTestWorld.Actor(factory, z, crossing.x, crossing.y);
            string before = JsonUtility.ToJson(MorrowfastSceneRuntime.GetState(z));
            Assert.IsFalse(owner.GetPart<MorrowfastPropPart>().TryRemove(actor, z)); Assert.IsNotNull(z.GetEntityCell(owner));
            Assert.AreEqual(before, JsonUtility.ToJson(MorrowfastSceneRuntime.GetState(z))); Assert.IsNotNull(z.GetEntityCell(visitor));
        }
        [Test] public void NonemptyContainerRefusesRemovalAndRetainsEveryItemIdentity()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); MorrowfastTestWorld.OpenAllDoors(z, actor);
            var owner = MorrowfastSceneDefinition.Load().owners.Select(o => MorrowfastSceneRuntime.FindOwner(z, o.id))
                .FirstOrDefault(e => e?.GetPart<ContainerPart>()?.Contents.Count > 0 && e.HasPart<MorrowfastPropPart>());
            Assert.IsNotNull(owner, "A real stocked container is required for this countercheck.");
            var p = MorrowfastTestWorld.Approach(z, actor, owner); z.MoveEntity(actor, p.x, p.y);
            var items = owner.GetPart<ContainerPart>().Contents.Select(e => e.ID).ToArray();
            Assert.IsFalse(owner.GetPart<MorrowfastPropPart>().TryRemove(actor, z));
            CollectionAssert.AreEqual(items, owner.GetPart<ContainerPart>().Contents.Select(e => e.ID)); Assert.IsNotNull(z.GetEntityCell(owner));
        }
        [Test] public void RoofVisibilityCannotChangeWallOrFurnitureCollision()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); MorrowfastTestWorld.OpenAllDoors(z, actor);
            var b = MorrowfastSceneDefinition.Load().buildings[0]; var roof = MorrowfastSceneRuntime.FindOwner(z, b.roofId);
            var p = MorrowfastTestWorld.Approach(z, actor, roof); z.MoveEntity(actor, p.x, p.y);
            var cells = MorrowfastSceneDefinition.Load().cells; var blocked = cells.Select(c => z.GetCell(c.x, c.y).BlocksMovement(actor)).ToArray();
            Assert.IsTrue(roof.GetPart<MorrowfastPropPart>().TrySetRoofLifted(actor, z, true)); Assert.IsTrue(MorrowfastSceneRuntime.IsRoofLifted(z, b.roofId));
            CollectionAssert.AreEqual(blocked, cells.Select(c => z.GetCell(c.x, c.y).BlocksMovement(actor)));
            Assert.IsTrue(roof.GetPart<MorrowfastPropPart>().TrySetRoofLifted(actor, z, false)); Assert.IsFalse(MorrowfastSceneRuntime.IsRoofLifted(z, b.roofId));
            CollectionAssert.AreEqual(blocked, cells.Select(c => z.GetCell(c.x, c.y).BlocksMovement(actor)));
        }
        [Test] public void ClosedUnenteredRoomCannotBeRevealedByARemoteRoofToggle()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z);
            var b = MorrowfastSceneDefinition.Load().buildings[0]; var roof = MorrowfastSceneRuntime.FindOwner(z, b.roofId);
            var p = MorrowfastTestWorld.Approach(z, actor, roof); z.MoveEntity(actor, p.x, p.y);
            Assert.IsFalse(MorrowfastSceneRuntime.IsDoorOpen(z, b.doorId));
            Assert.IsFalse(roof.GetPart<MorrowfastPropPart>().TrySetRoofLifted(actor, z, true)); Assert.IsFalse(MorrowfastSceneRuntime.IsRoofLifted(z, b.roofId));
        }
        [TestCase("building-shell")] [TestCase("npc")] [TestCase("creature")] [TestCase("door")] [TestCase("roof")]
        public void ProtectedOwnerKindsNeverOfferGenericClear(string kind)
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z);
            var owners = MorrowfastSceneDefinition.Load().owners.Where(o => o.kind == kind).ToArray(); Assert.IsNotEmpty(owners);
            foreach (var o in owners)
                Assert.IsFalse(WorldInteractionSystem.GatherActions(MorrowfastSceneRuntime.FindOwner(z, o.id), actor).Any(a => a.Command == MorrowfastPropPart.ClearCommand), o.id);
        }
    }
}
