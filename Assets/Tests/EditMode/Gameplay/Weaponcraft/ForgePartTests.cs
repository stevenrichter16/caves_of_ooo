using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests.Gameplay.Weaponcraft
{
    /// <summary>
    /// M3-L3 SM1 — ForgePart: the weaponcraft station marker, mirror of
    /// AlchemyStillPart. IsNearForge is the adjacency gate the forge/temper/
    /// reforge commands use in Validate. Every positive assertion pairs with
    /// a counter-check (§3.4), including the cross-station probe: a nearby
    /// alchemy still must NOT satisfy forge adjacency.
    /// </summary>
    public class ForgePartTests
    {
        private const string TestBlueprintsJson = @"{
  ""Objects"": [
    {
      ""Name"": ""TinkersForge"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [
          { ""Key"": ""DisplayName"", ""Value"": ""tinker's forge"" }
        ]},
        { ""Name"": ""Forge"", ""Params"": [] }
      ]
    },
    {
      ""Name"": ""AlchemyStill"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [
          { ""Key"": ""DisplayName"", ""Value"": ""alchemy still"" }
        ]},
        { ""Name"": ""AlchemyStill"", ""Params"": [] }
      ]
    }
  ]
}";

        private static EntityFactory CreateFactory()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(TestBlueprintsJson);
            return factory;
        }

        private static Entity CreateActor()
        {
            var actor = new Entity { ID = "smith", BlueprintName = "Player" };
            actor.AddPart(new RenderPart { DisplayName = "smith" });
            actor.AddPart(new InventoryPart());
            return actor;
        }

        /// <summary>Zone with the actor at (5,5); optionally a station at the given offset.</summary>
        private static Zone MakeZone(Entity actor, EntityFactory factory,
            string stationBlueprint = null, int dx = 0, int dy = 0)
        {
            var zone = new Zone("ForgeTestZone");
            Assert.IsTrue(zone.AddEntity(actor, 5, 5));

            if (stationBlueprint != null)
            {
                Entity station = factory.CreateEntity(stationBlueprint);
                Assert.IsNotNull(station, $"fixture blueprint '{stationBlueprint}' must exist");
                Assert.IsTrue(zone.AddEntity(station, 5 + dx, 5 + dy));
            }

            return zone;
        }

        // ════════════════ IsNearForge — adjacency box ════════════════

        [Test]
        public void IsNearForge_SameCell_True()
        {
            var factory = CreateFactory();
            var actor = CreateActor();
            var zone = MakeZone(actor, factory, "TinkersForge", 0, 0);

            Assert.IsTrue(ForgePart.IsNearForge(actor, zone));
        }

        [Test]
        public void IsNearForge_OrthogonalAndDiagonalNeighbors_True()
        {
            var factory = CreateFactory();

            var orth = CreateActor();
            var zoneOrth = MakeZone(orth, factory, "TinkersForge", 1, 0);
            Assert.IsTrue(ForgePart.IsNearForge(orth, zoneOrth), "orthogonal neighbor");

            var diag = CreateActor();
            var zoneDiag = MakeZone(diag, factory, "TinkersForge", -1, -1);
            Assert.IsTrue(ForgePart.IsNearForge(diag, zoneDiag), "diagonal neighbor");
        }

        [Test]
        public void IsNearForge_TwoCellsAway_False()
        {
            // Counter-check: the box is 3×3, not a radius-2 scan. A forge at
            // distance 2 must NOT satisfy the gate.
            var factory = CreateFactory();
            var actor = CreateActor();
            var zone = MakeZone(actor, factory, "TinkersForge", 2, 0);

            Assert.IsFalse(ForgePart.IsNearForge(actor, zone));
        }

        [Test]
        public void IsNearForge_NoForgeInZone_False()
        {
            // Counter-check: an empty neighborhood is not "near a forge".
            var factory = CreateFactory();
            var actor = CreateActor();
            var zone = MakeZone(actor, factory);

            Assert.IsFalse(ForgePart.IsNearForge(actor, zone));
        }

        [Test]
        public void IsNearForge_AdjacentStillDoesNotCount_False()
        {
            // Cross-station counter-check: the stations are distinct gates.
            // If a future change aliased IsNearForge to the still scan (or
            // vice versa), this fails.
            var factory = CreateFactory();
            var actor = CreateActor();
            var zone = MakeZone(actor, factory, "AlchemyStill", 1, 0);

            Assert.IsFalse(ForgePart.IsNearForge(actor, zone));
        }

        [Test]
        public void IsNearForge_NullZoneOrUnplacedActor_False()
        {
            var factory = CreateFactory();
            var actor = CreateActor();

            Assert.IsFalse(ForgePart.IsNearForge(actor, null), "null zone");
            Assert.IsFalse(ForgePart.IsNearForge(null, new Zone("Empty")), "null actor");
            Assert.IsFalse(ForgePart.IsNearForge(actor, new Zone("Empty")),
                "actor not placed in the zone");
        }

        // ════════════════ Production content pins ════════════════

        [Test]
        public void Production_TinkersForge_Blueprint_ExistsWithForgePart()
        {
            // A typo'd part name in Objects.json would produce a forge that
            // renders but never gates — pin the real content.
            TextAsset asset = Resources.Load<TextAsset>("Content/Blueprints/Objects");
            Assert.IsNotNull(asset, "production Objects.json must be loadable.");

            var factory = new EntityFactory();
            factory.LoadBlueprints(asset.text);

            Entity forge = factory.CreateEntity("TinkersForge");
            Assert.IsNotNull(forge, "production blueprint 'TinkersForge' must exist.");
            Assert.IsTrue(forge.HasPart<ForgePart>(),
                "TinkersForge must carry ForgePart or station gating never passes.");

            // Counter-check within the pin: the forge is furniture, not loot —
            // it must not be takeable into an inventory.
            var physics = forge.GetPart<PhysicsPart>();
            Assert.IsTrue(physics == null || !physics.Takeable,
                "TinkersForge must not be takeable.");
        }
    }
}
