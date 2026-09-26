using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class DensityCorpseCoverageTests
    {
        DensityLootTestScope scope; EntityFactory oldCorpse,oldHarvest;
        [SetUp] public void Setup(){scope=new DensityLootTestScope();oldCorpse=CorpsePart.Factory;oldHarvest=HarvestablePart.Factory;CorpsePart.Factory=HarvestablePart.Factory=scope.Factory;}
        [TearDown] public void Cleanup(){CorpsePart.Factory=oldCorpse;HarvestablePart.Factory=oldHarvest;scope.Dispose();}
        [TestCase("SariSnake","VenomGland")] [TestCase("SunStriker","RawMeat")] [TestCase("SporeShambler","ShamblerSporeSac")]
        public void AuthoredBiologicalFamilyDeathProducesARealHarvestableProduct(string species,string product)
        {
            var zone=new Zone("family");var actor=scope.Factory.CreateEntity("Player");var creature=scope.Factory.CreateEntity(species);zone.AddEntity(actor,10,10);zone.AddEntity(creature,11,10);
            CombatSystem.HandleDeath(creature,actor,zone);
            var corpse=zone.GetAllEntities().Single(e=>e.GetProperty("SourceID")==creature.ID);
            Assert.NotNull(corpse.GetPart<HarvestablePart>(),species);Assert.AreEqual(product,corpse.GetPart<HarvestablePart>().YieldBlueprint);
            Assert.True(DensityHarvestSecurityTests.Act(corpse,actor,zone));Assert.True(actor.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName==product));Assert.IsNull(zone.GetEntityCell(corpse));
        }
        [Test] public void CleanBoneSceneryYieldsExistingBoneWithoutRemovingOtherOccupants()
        {
            var zone=new Zone("bones");var actor=scope.Factory.CreateEntity("Player");var bones=scope.Factory.CreateEntity("Bones");var floor=scope.Factory.CreateEntity("Floor");zone.AddEntity(actor,10,10);zone.AddEntity(bones,11,10);zone.AddEntity(floor,11,10);
            Assert.True(WorldInteractionSystem.GatherActions(bones,actor).Any(a=>a.Command=="Harvest"));Assert.True(DensityHarvestSecurityTests.Act(bones,actor,zone));
            Assert.True(actor.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="Bone"));Assert.IsNull(zone.GetEntityCell(bones));Assert.NotNull(zone.GetEntityCell(floor));
        }
        [TestCase("Villager")] [TestCase("VillageChild")] [TestCase("PetDog")] [TestCase("GlassScorpion")] [TestCase("GinFrog")] [TestCase("Mogu")]
        public void ProtectedPeopleAndUnresolvedFamiliesDoNotAcquireMeatRewards(string species)
        {Assert.IsNull(scope.Factory.CreateEntity(species).GetPart<CorpsePart>()?.HarvestBlueprint);}
        [Test] public void AuthoredBogBodyRemainsUntouchedAndConstructSalvageStaysMineral()
        {Assert.IsNull(scope.Factory.CreateEntity("BogTakenBody").GetPart<HarvestablePart>());Assert.AreEqual("GlowQuartz",scope.Factory.CreateEntity("StoneGolem").GetPart<CorpsePart>().HarvestBlueprint);}
    }
}
