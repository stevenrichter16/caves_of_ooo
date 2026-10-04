using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class GalleryTacticsContentTests
    {
        [TestCase("CurationHalfSet", 70)] [TestCase("Shambler", 80)]
        public void ExistingSlowTendrilCreaturesTeachTheSameOptInCommitment(string blueprint, int speed)
        {
            using (var scope = new HaulingContentScope())
            {
                var actor = scope.Factory.CreateEntity(blueprint);
                Assert.NotNull(actor.GetPart("CommittedMelee"));
                Assert.AreEqual(speed, actor.GetStatValue("Speed"));
                Assert.AreEqual("DefaultTendril", actor.GetProperty("NaturalWeapon"));
                Assert.Null(scope.Factory.CreateEntity("MarlbackScrabbler").GetPart("CommittedMelee"), "Other enemy kits do not inherit a universal delay.");
            }
        }

        [TestCase(64)] [TestCase(1729)]
        public void GalleryOffersPartialRecoveryAndClearLateralRoutesAroundItsSolidSlab(int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(seed); var zone = new Zone(MarrowstyeCompositionPlan.ZoneID);
                var terrain = new MarrowstyeCompositionBuilder(seed);
                Assert.True(terrain.BuildZone(zone, scope.Factory, new Random(seed)));
                Assert.True(new MarrowstyeProfileBuilder(terrain).BuildZone(zone, scope.Factory, new Random(seed)));
                var receiving = new CurationReceivingBuilder(terrain);
                Assert.True(receiving.BuildZone(zone, scope.Factory, new Random(seed)));
                var room = terrain.Plan.Rooms.Single(r => r.Role == "DisusedWing");
                Func<string, Entity> owner = bp => zone.GetReadOnlyEntities().Single(e => e.BlueprintName == bp);
                var near = owner("CurationRecoveryCabinet").GetPart<ContainerPart>();
                CollectionAssert.AreEquivalent(new[] { "FireClay", "SoddenFieldDressing", "LeatherGloves" }, near.Contents.Select(e => e.BlueprintName));
                var deep = owner("CurationConservationCase");
                Assert.AreEqual((room.X + 18, room.Y + 5), zone.GetEntityPosition(deep));
                CollectionAssert.AreEquivalent(new[] { "SootrootPulp", "PitchpodResin" }, deep.GetPart<ContainerPart>().Contents.Select(e => e.BlueprintName));
                Assert.AreEqual(2, deep.GetPart<ContainerPart>().Contents.Single(e => e.BlueprintName == "SootrootPulp").GetPart<StackerPart>().StackCount);
                var slab = owner("CurationInspectionSlab");
                Assert.AreEqual((room.X + 16, room.Y + 3), zone.GetEntityPosition(slab));
                Assert.True(slab.GetPart<PhysicsPart>().Solid);
                Assert.AreEqual((room.X + 18, room.Y + 1), zone.GetEntityPosition(owner("CurationQuarantineRail")));
                foreach (var p in new[] { (16, 2), (16, 4), (19, 3) })
                    Assert.False(zone.GetCell(room.X + p.Item1, room.Y + p.Item2).Objects.Any(e => !e.HasTag("Creature") && (e.HasTag("Solid") || e.GetPart<PhysicsPart>()?.Solid == true)));
                var ids = deep.GetPart<ContainerPart>().Contents.Select(e => e.ID).ToArray();
                Assert.False(receiving.BuildZone(zone, scope.Factory, new Random(seed)), "Repeat enrichment must not duplicate either reward tier.");
                CollectionAssert.AreEqual(ids, deep.GetPart<ContainerPart>().Contents.Select(e => e.ID));
            }
        }

        [Test]
        public void SeparateConservationCaseHasRealStockAuthorityAndApprovedExistingCabinetArt()
        {
            using (var f = new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
            {
                Assert.True(f.Factory.Blueprints.ContainsKey("CurationConservationCase"));
                var e = f.Add("CurationConservationCase"); Assert.NotNull(e.GetPart<ContainerPart>());
                Assert.False(e.GetPart<PhysicsPart>().Takeable); Assert.IsEmpty(e.GetPart<ContainerPart>().Contents);
                Assert.AreEqual("curation-yard-recovery-cabinet", CurationYard3DLibrary.ResolveModel(f.Zone, e));
                f.Refresh(); Assert.True(f.Rendered(e));
                e.GetPart<RenderPart>().Visible = false;
                Assert.Null(CurationYard3DLibrary.ResolveModel(f.Zone, e));
            }
        }
    }
}
