using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The register of what the world lets you break — asserted against the
    /// real blueprint file, not a fixture.
    ///
    /// <para>Destructibility is opt-in, so this file exists to protect the
    /// two directions that opt-in alone cannot: that the things which are
    /// SUPPOSED to break still do after a content edit, and — the one that
    /// actually softlocks a save — that the structural pieces holding the
    /// world together never quietly acquire a
    /// <see cref="DestructiblePart"/>.</para>
    /// </summary>
    [TestFixture]
    public class DestructibleRegisterTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static DestructiblePart PartOf(string blueprint)
        {
            var e = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(e, $"blueprint '{blueprint}' does not exist");
            return e.GetPart<DestructiblePart>();
        }

        private static void AssertBreakable(string blueprint)
        {
            var part = PartOf(blueprint);
            Assert.IsNotNull(part, $"{blueprint} should be breakable");
            Assert.Greater(part.HP, 0, $"{blueprint} HP");
            Assert.AreEqual(part.MaxHP, part.HP, $"{blueprint} starts at full HP");
            Assert.IsFalse(part.Indestructible, $"{blueprint} is not protected");
        }

        // ════════════════════════════════════════════════════════
        // Things that must break
        // ════════════════════════════════════════════════════════

        [Test]
        public void MasonryBreaks_SoWallsCanBecomeShortcuts()
        {
            AssertBreakable("Wall");
            AssertBreakable("SandstoneWall");
            AssertBreakable("ObsidianWall");
            AssertBreakable("Pillar");
        }

        [Test]
        public void ChildWalls_InheritTheirParentsMortality()
        {
            // StoneWall/SlateWall etc. only override Render, so they take
            // Destructible through the blueprint bake. If inheritance ever
            // stopped carrying Parts, half the register would silently
            // become immune with no test failing anywhere else.
            AssertBreakable("StoneWall");
            AssertBreakable("IceWall");
            AssertBreakable("VineWall");
        }

        [Test]
        public void GreeneryAndContainersBreak()
        {
            AssertBreakable("Hedge");
            AssertBreakable("Tree");
            AssertBreakable("WoodenBarrel");
            AssertBreakable("Chest");
            AssertBreakable("LockedChest");
            AssertBreakable("LockedDoor");
        }

        [Test]
        public void HarderStoneCostsMoreToGetThrough()
        {
            // The gradient is the whole point of Hardness: obsidian should
            // be a tool problem, sandstone a patience problem. Flat numbers
            // across every wall would make the material meaningless.
            Assert.Greater(PartOf("ObsidianWall").Hardness, PartOf("SandstoneWall").Hardness);
            Assert.Greater(PartOf("ObsidianWall").HP, PartOf("SandstoneWall").HP);
            Assert.AreEqual(0, PartOf("Hedge").Hardness, "a hedge resists nothing");
        }

        [Test]
        public void ABrokenWallLeavesRubble_NotPristineFloor()
        {
            Assert.AreEqual("Rubble", PartOf("Wall").WreckageBlueprint);

            // And the rubble must be walkable, or breaking the wall would
            // gain the player nothing.
            var rubble = _factory.CreateEntity("Rubble");
            Assert.IsNotNull(rubble);
            var physics = rubble.GetPart<PhysicsPart>();
            Assert.IsFalse(physics != null && physics.Solid, "rubble must not block");
            Assert.IsFalse(rubble.HasTag("Solid"), "rubble must not block");
        }

        [Test]
        public void AFelledTreeLeavesNothingInTheWay()
        {
            // HollowStump is Solid, so leaving one behind would keep the
            // cell blocked and make felling the tree pointless.
            Assert.IsEmpty(PartOf("Tree").WreckageBlueprint ?? "");
        }

        // ════════════════════════════════════════════════════════
        // Things that must NOT break — the softlock guard
        // ════════════════════════════════════════════════════════

        [Test]
        public void StaircasesCannotBeDestroyed()
        {
            // A player who breaks the only way down has softlocked a save
            // that, this being an RPG, they expect to keep playing.
            foreach (var stairs in new[] { "StairsDown", "StairsUp" })
            {
                var e = _factory.CreateEntity(stairs);
                Assert.IsNotNull(e, stairs);
                Assert.IsFalse(DestructionSystem.IsBreakable(e), $"{stairs} must be immune");
            }
        }

        [Test]
        public void OrdinaryTerrainAndCreaturesAreNotBreakable()
        {
            // Counter-check to the register above: "opt-in" has to actually
            // exclude the rest of the game, or the register is decorative.
            foreach (var blueprint in new[] { "Rubble", "Snapjaw", "Villager", "GoldCoin" })
            {
                var e = _factory.CreateEntity(blueprint);
                Assert.IsNotNull(e, blueprint);
                Assert.IsFalse(DestructionSystem.IsBreakable(e), $"{blueprint} must be immune");
            }
        }

        [Test]
        public void AMimicIsACreature_AndSoUsesTheDeathPath()
        {
            // MimicChest inherits Creature. It looks like the most
            // breakable thing in the game and must not be — killing it has
            // to award XP and drop loot like any other monster.
            var mimic = _factory.CreateEntity("MimicChest");
            Assert.IsNotNull(mimic);
            Assert.IsFalse(DestructionSystem.IsBreakable(mimic));
        }
    }
}
