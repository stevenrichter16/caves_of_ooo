using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W5.5 — the Choir Cathedral floor: a substrate-grown vault, and a
    /// node in the network that connects every Cathedral to every other
    /// (Lore/Factions/01_RotChoir.md:126-129).
    ///
    /// <para><b>Scope, decided from canon:</b> canon puts TWO different
    /// things at this address. The Choir Cathedral ARCHETYPE is a
    /// generic node — "some are large and old, some small and recent".
    /// The DEEPEST Cathedral is Tier 5, holds the Wedded's original
    /// body, and is "the most-difficult-to-earn audience with any of
    /// the Six gods" (:122, :218) — god-room content, which the
    /// milestone map puts in W8. W5.5 builds the archetype; the god
    /// stays W8's, and lands additively on top. The same call W5.4 made
    /// for Olderdeep, whose founding-village identity belongs to
    /// W6.</para>
    /// </summary>
    public class ChoirCathedralTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static Zone BuildVault(int seed = 4)
        {
            var zone = new Zone("Overworld.5.4.2");
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var f = _factory.CreateEntity("StoneFloor");
                    if (f != null) zone.AddEntity(f, x, y);
                }
            new ChoirCathedralBuilder().BuildZone(zone, _factory, new Random(seed));
            return zone;
        }

        private static int CountOf(Zone zone, string bp)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == bp) n++;
            return n;
        }

        [Test]
        public void TheCathedral_IsRoutedToTheDeepestCathedral()
        {
            Assert.AreEqual(SinkholeArchetype.ChoirCathedral,
                SinkholeArchetypes.For("the Deepest Cathedral"));
        }

        [Test]
        public void TheVault_IsGrown_NotBuilt()
        {
            // "substrate-grown vault" — the walls of this room are the
            // Choir, not masonry.
            var zone = BuildVault();
            Assert.Greater(CountOf(zone, "SubstrateVault"), 30,
                "the vault is the room's shape");
        }

        [Test]
        public void TheNodeIsTheHeart_AndItGlows()
        {
            var zone = BuildVault();
            Assert.AreEqual(1, CountOf(zone, "ChoirNode"), "one node per Cathedral");
            Entity node = null;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "ChoirNode") node = e;
            Assert.IsNotNull(node.GetPart<LightSourcePart>(),
                "the substrate carries its own light");
        }

        [Test]
        public void ThereAreEncasedElders_AndTheySpeak()
        {
            // Canon: at sufficient Choir reputation the player may enter
            // peacefully and speak with an encased elder — the Choir
            // answering through a person in the wall.
            var zone = BuildVault();
            Assert.That(CountOf(zone, "EncasedElder"), Is.InRange(2, 4),
                "a few of the Wedded, held in the wall");

            foreach (var e in zone.GetAllEntities())
            {
                if (e.BlueprintName != "EncasedElder") continue;
                Assert.AreEqual("RotChoir", e.Tags.ContainsKey("Faction")
                    ? e.Tags["Faction"] : null);
                Assert.IsNotNull(e.GetPart<ConversationPart>(),
                    "the wall answers");
            }
        }

        [Test]
        public void TheTendrilsAreHere_Reused_NotReinvented()
        {
            // ChoirTendril already ships with a 37-node tree (W4.1).
            var zone = BuildVault();
            Assert.GreaterOrEqual(CountOf(zone, "ChoirTendril"), 1,
                "the grove's own face reaches this deep");
        }

        [Test]
        public void TheVaultIsWalkable()
        {
            // A vault you cannot cross is a screenshot. The nave stays
            // open from the entry side.
            var zone = BuildVault();
            var reached = FormationReachability.FloodFromWest(zone, out bool crossed);
            Assert.IsTrue(crossed, "you can walk the nave");
        }

        [Test]
        public void TheFootprintIsClaimed()
        {
            // Same hazard the village had: PopulationBuilder runs after
            // this and rolls snapjaws. A vault full of snapjaws is not a
            // cathedral.
            var zone = BuildVault();
            Assert.Greater(zone.GenReservedCells.Count, 100);
        }

        [Test]
        public void TheChoirStillNeverSaysDead()
        {
            // The voice lint reaches the new speaker too.
            ConversationLoader.Reset();
            try
            {
                ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(
                    Application.dataPath, "Resources/Content/Conversations/Catacomb.json")));
                ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(
                    Application.dataPath, "Resources/Content/Conversations/RotChoir.json")));
                var conv = ConversationLoader.Get("EncasedElder_1");
                Assert.IsNotNull(conv, "the elder's tree loads");
                foreach (var node in conv.Nodes)
                {
                    var t = (node.Text ?? "").ToLowerInvariant();
                    StringAssert.DoesNotContain("dead", t, node.ID);
                    StringAssert.DoesNotContain(" died", t, node.ID);
                }
            }
            finally { ConversationLoader.Reset(); }
        }

        [Test]
        public void TheVaultHasArt()
        {
            foreach (var bp in new[] { "SubstrateVault", "ChoirNode", "EncasedElder" })
            {
                bool mapped = false;
                foreach (var (b, _) in CavesOfOoo.Rendering.EnvironmentSpriteRenderer.FixtureSprites)
                    if (b == bp) mapped = true;
                Assert.IsTrue(mapped, bp + " ships with a sprite, not just a letter");
            }
        }
    }
}
