using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FellingSiteRulesTests
    {
        private EntityFactory _factory; private Entity _actor; private Zone _zone; private TurnManager _turns;
        [SetUp] public void Setup()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            _zone = new Zone("FellingRules"); _actor = _factory.CreateEntity("Player");
            _actor.GetStat("DV").Penalty = 0; _actor.GetStat("Agility").Penalty = 0;
            _zone.AddEntity(_actor, 10, 10); _zone.AddEntity(_factory.CreateEntity("SeventhPosition"), 10, 10);
            _turns = new TurnManager(); _turns.AddEntity(_actor); _turns.ProcessUntilPlayerTurn();
            MessageLog.Clear();
        }
        private StatusEffectsPart Effects => _actor.GetPart<StatusEffectsPart>();
        private void End() => _turns.EndTurn(_actor, _zone);
        [Test]
        public void WaitingOnTheActualPointAppliesTheRealStatPenalty()
        {
            int dv = _actor.GetStatValue("DV"), agi = _actor.GetStatValue("Agility");
            End(); Assert.IsNotNull(Effects); Assert.AreEqual(2, Effects.GetEffect<ConfusedEffect>()?.Duration);
            Assert.AreEqual(dv-2, _actor.GetStatValue("DV")); Assert.AreEqual(agi-2, _actor.GetStatValue("Agility"));
            Assert.IsFalse(Effects.GetEffect<ConfusedEffect>().JustApplied);
        }
        [TestCase(-1,-1)] [TestCase(0,-1)] [TestCase(1,-1)] [TestCase(-1,0)]
        [TestCase(1,0)] [TestCase(-1,1)] [TestCase(0,1)] [TestCase(1,1)]
        public void NoAdjacentCellHasTheStandingExposure(int dx, int dy)
        {
            _zone.MoveEntity(_actor, 10+dx, 10+dy); End();
            Assert.IsFalse(Effects?.HasEffect<ConfusedEffect>() == true);
        }
        [Test]
        public void LeavingAllowsTheBriefAftereffectToExpireWithoutStatDrift()
        {
            int dv = _actor.GetStatValue("DV"); End(); Assert.IsTrue(Effects?.HasEffect<ConfusedEffect>() == true);
            _zone.MoveEntity(_actor, 11, 10); End(); Assert.AreEqual(1, Effects.GetEffect<ConfusedEffect>()?.Duration);
            End(); Assert.IsFalse(Effects.HasEffect<ConfusedEffect>()); Assert.AreEqual(dv, _actor.GetStatValue("DV"));
        }
        [Test]
        public void RepeatedWaitingNeverStacksPenalties()
        {
            int dv = _actor.GetStatValue("DV");
            for(int i=0;i<12;i++) { End(); Assert.AreEqual(dv-2, _actor.GetStatValue("DV")); Assert.AreEqual(1, Effects.GetAllEffects().Count(e=>e is ConfusedEffect)); }
        }
        [Test]
        public void AnNPCOnThePointDoesNotApplyThePlayerEffect()
        {
            _actor.Tags.Remove("Player"); End(); Assert.IsFalse(Effects?.HasEffect<ConfusedEffect>() == true);
        }
        [TestCase("FlowerField")] [TestCase("CandyCarrotCrop")] [TestCase("TankBrocchinia")]
        [TestCase("Grass")] [TestCase("Tree")] [TestCase("FoundingPlume")]
        public void BarrenGroundRejectsVegetationAndOrdinaryGroundAcceptsIt(string blueprint)
        {
            _zone.AddEntity(_factory.CreateEntity("FellingBarePosition"), 20, 10);
            var plant = _factory.CreateEntity(blueprint);
            Assert.IsFalse(_zone.AddEntity(plant, 20, 10)); Assert.IsNull(_zone.GetEntityCell(plant));
            Assert.IsTrue(_zone.AddEntity(plant, 21, 10)); Assert.AreEqual((21,10),_zone.GetEntityPosition(plant));
        }
        [Test]
        public void RejectedPlantingNeverConsumesACarriedSeed()
        {
            // Explicitly plantable test substrate bypasses the ordinary hard
            // stone guard so this pins actual placement-failure consumption.
            var bare=_factory.CreateEntity("FellingBarePosition"); bare.SetTag("Plantable"); _zone.AddEntity(bare,10,10);
            var seed=_factory.CreateEntity("CandyCarrotSeed"); var inv=_actor.GetPart<InventoryPart>(); inv.AddObject(seed);
            var previous=SeedPart.Factory; SeedPart.Factory=_factory;
            try
            {
                var e=GameEvent.New("InventoryAction");e.SetParameter("Command","PlantSeed");e.SetParameter("Actor",(object)_actor);e.SetParameter("Zone",(object)_zone);
                seed.FireEventAndRelease(e);
                Assert.IsTrue(inv.Objects.Contains(seed));Assert.IsFalse(_zone.GetCell(10,10).HasObjectWithPart<CropPart>());
                Assert.IsFalse(MessageLog.GetMessages().Any(m=>m.Contains(" plants ")));
            }
            finally { SeedPart.Factory=previous; }
        }
    }
}
