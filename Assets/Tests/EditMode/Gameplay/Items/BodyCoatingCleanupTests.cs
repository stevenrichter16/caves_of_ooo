using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public abstract class BodyCoatingCleanupFixture : FiftyWorldFixture
    {
        [SetUp] public void Reveal() { foreach (var cell in Zone.Cells) cell.IsVisible = cell.Explored = true; }
        protected Entity Friend() { var friend = Place("MarlbackScrabbler"); Assert.True(friend.GetPart<BrainPart>().SetPartyLeader(Actor)); return friend; }
        protected LiquidCoveredEffect Coat(Entity target, string liquid = "pitch", int amount = 35)
        { var coat = new LiquidCoveredEffect(liquid, amount); Assert.True(target.ApplyEffect(coat)); return coat; }
        protected string Pick(Entity pith, Entity target) => Choice(pith, "WickBody|", a => a.Command.Split('|')[4] == Id(target));
        protected bool Offered(Entity pith) => Actions(pith).Any(a => a.Command.StartsWith("WickBody|", StringComparison.Ordinal));
        protected sealed class Hook : Part
        {
            readonly string id; readonly Func<GameEvent, bool> callback;
            public Hook(string id, Func<GameEvent, bool> callback) { this.id = id; this.callback = callback; }
            public override bool HandleEvent(GameEvent e) => e.ID != id || callback(e);
        }
    }
    public sealed class BodyCoatingCleanupTests : BodyCoatingCleanupFixture
    {
        [TestCase("oil", false)] [TestCase("pitch", false)] [TestCase("honey", false)]
        [TestCase("oil", true)] [TestCase("pitch", true)] [TestCase("honey", true)]
        public void CarriedPithRemovesTwentyUnitsFromExactSelfOrPartyCoat(string liquid, bool friend)
        {
            var target = friend ? Friend() : Actor; var coat = Coat(target, liquid); var pith = Carry("PrismreedPith");
            int dv = target.GetStatValue("DV");
            Assert.True(Act(pith, Pick(pith, target))); Assert.AreSame(coat, target.GetEffect<LiquidCoveredEffect>());
            Assert.AreEqual(15, coat.Amount); Assert.AreEqual(dv, target.GetStatValue("DV")); Assert.False(Pack.Objects.Contains(pith));
        }
        [TestCase(1)] [TestCase(20)]
        public void ExhaustingCoatUsesNormalRemovalAndRestoresItsActualPenalties(int amount)
        {
            var target = Friend(); int dv = target.GetStatValue("DV"), agi = target.GetStatValue("Agility");
            Coat(target, "pitch", amount); Assert.AreEqual(dv - 3, target.GetStatValue("DV"));
            var pith = Carry("PrismreedPith"); Assert.True(Act(pith, Pick(pith, target)));
            Assert.False(target.HasEffect<LiquidCoveredEffect>()); Assert.AreEqual(dv, target.GetStatValue("DV")); Assert.AreEqual(agi, target.GetStatValue("Agility"));
        }
        [Test] public void CleanupPreservesWetInternalPoisonBurningAndGroundFilm()
        {
            var target = Friend(); var wet = new WetEffect(1); var poison = new PoisonedEffect(); var fire = new BurningEffect();
            Assert.True(target.ApplyEffect(wet)); Assert.True(target.ApplyEffect(poison)); Assert.True(target.ApplyEffect(fire));
            Coat(target, "oil", 10); Zone.TileState.WriteCoating(11, 10, "oil", 6);
            var pith = Carry("PrismreedPith"); Assert.True(Act(pith, Pick(pith, target)));
            Assert.AreSame(wet, target.GetEffect<WetEffect>()); Assert.AreSame(poison, target.GetEffect<PoisonedEffect>());
            Assert.AreSame(fire, target.GetEffect<BurningEffect>()); Assert.AreEqual(6, Zone.TileState.CoatingTurns(11, 10, "oil"));
        }
        [Test] public void AuthoredCropYieldIsUsablePithAndCleanupPersistsAcrossBinarySave()
        {
            Bed(); var crop = Place("PrismreedCrop"); var part = crop.GetPart<CropPart>(); part.GrowthStage = 2;
            Assert.True(Act(crop, "HarvestCultivatedCrop")); var pith = Zone.GetReadOnlyEntities().First(e => e.BlueprintName == "PrismreedPith");
            Assert.True(Zone.RemoveEntity(pith)); Assert.True(Pack.AddObject(pith));
            Coat(Actor, "pitch", 35); Assert.True(Act(pith, Pick(pith, Actor)));
            using (var stream = new MemoryStream()) {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(Actor); writer.WriteQueuedEntityBodies(); stream.Position = 0;
                var reader = new SaveReader(stream, Factory); var loaded = reader.ReadEntityReference(); reader.ReadEntityBodies();
                Assert.AreEqual(15, loaded.GetEffect<LiquidCoveredEffect>().Amount); Assert.AreEqual(Actor.GetStatValue("DV"), loaded.GetStatValue("DV"));
                Assert.False(loaded.GetPart<InventoryPart>().Objects.Any(e => e.ID == pith.ID));
            }
        }
    }
}
