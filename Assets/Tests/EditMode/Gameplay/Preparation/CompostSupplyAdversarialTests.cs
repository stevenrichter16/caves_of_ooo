using System.IO;
using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    [TestFixture] public class CompostSupplyAdversarialTests : CompostSupplyFixture
    {
        [TestCase("already")] [TestCase("sprout")] [TestCase("ripe")] [TestCase("progress")]
        [TestCase("wet-progress")] [TestCase("hidden-cell")] [TestCase("hidden-owner")] [TestCase("far")]
        [TestCase("removed")] [TestCase("foreign-zone")] [TestCase("unprepared")]
        [TestCase("missing-sludge")] [TestCase("foreign-sludge")] [TestCase("empty-stack")]
        [TestCase("short-stage")] [TestCase("veto")] [TestCase("after-failure")]
        public void IneligibleOrRolledBackCompostSpendsNoFurtherSupply(string fault)
        {
            var sludge = BuySludge(); var crop = FreshPlant(); var part = crop.GetPart<CropPart>(); var probe = new ActionProbe(); Actor.AddPart(probe);
            switch (fault)
            {
                case "already": part.Composted = true; break; case "sprout": part.GrowthStage = 1; break; case "ripe": part.GrowthStage = 2; break;
                case "progress": part.TicksInStage = 1; break; case "wet-progress": part.MoistureTicks = 5; part.GrowthWetTickRemainder = 1; break;
                case "hidden-cell": Zone.GetEntityCell(crop).IsVisible = false; break; case "hidden-owner": crop.GetPart<RenderPart>().Visible = false; break;
                case "far": Zone.RemoveEntity(crop); Zone.AddEntity(crop, 40, 20); break; case "removed": Zone.RemoveEntity(crop); break;
                case "foreign-zone": Zone.RemoveEntity(crop); new Zone("foreign").AddEntity(crop, 11, 10); break;
                case "unprepared": var bed = Zone.GetCell(11, 10).Objects.Single(e => e.HasPart<CultivatedSoilPart>()); bed.RemovePart(bed.GetPart<CultivatedSoilPart>()); break;
                case "missing-sludge": Pack.RemoveObject(sludge); break;
                case "foreign-sludge": Pack.RemoveObject(sludge); var other = new Entity(); other.AddPart(new InventoryPart()); other.GetPart<InventoryPart>().AddObject(sludge); break;
                case "empty-stack": if (!sludge.HasPart<StackerPart>()) sludge.AddPart(new StackerPart()); sludge.GetPart<StackerPart>().StackCount = 0; break;
                case "short-stage": part.TicksPerStage = 3; break; case "veto": probe.Veto = true; break; case "after-failure": probe.ThrowAfter = true; break;
            }
            int units = SludgeUnits(Actor), duration = part.TicksPerStage; bool composted = part.Composted;
            Assert.False(Act(crop, CompostCommand(sludge)), fault); Assert.AreEqual(units, SludgeUnits(Actor));
            Assert.AreEqual(duration, part.TicksPerStage); Assert.AreEqual(composted, part.Composted); Assert.AreEqual(fault == "after-failure" ? 1 : 0, probe.After);
        }
        [TestCase(false)] [TestCase(true)]
        public void PoorBuyerCannotAcquireFreeSludge(bool provisioner)
        {
            var seller = Seller(provisioner); Assert.AreEqual(2, SludgeUnits(seller)); var item = seller.GetPart<InventoryPart>().Objects.First(i => i.BlueprintName == "InertSludge");
            TradeSystem.SetDrams(Actor, 0); int purse = TradeSystem.GetDrams(seller); Assert.False(TradeSystem.BuyFromTrader(Actor, seller, item));
            Assert.AreEqual(0, SludgeUnits(Actor)); Assert.AreEqual(2, SludgeUnits(seller)); Assert.AreEqual(purse, TradeSystem.GetDrams(seller));
        }
        [Test] public void SaveRetainsSpentSupplyAndOneTimeCropBenefit()
        {
            var sludge = BuySludge(); var crop = FreshPlant(); Assert.True(Act(crop, CompostCommand(sludge)));
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(Actor); writer.WriteEntityReference(crop); writer.WriteQueuedEntityBodies(); stream.Position = 0;
                var reader = new SaveReader(stream, Factory); var savedActor = reader.ReadEntityReference(); var savedCrop = reader.ReadEntityReference(); reader.ReadEntityBodies();
                Assert.AreEqual(1, SludgeUnits(savedActor)); Assert.True(savedCrop.GetPart<CropPart>().Composted);
                Assert.AreEqual(crop.GetPart<CropPart>().TicksPerStage, savedCrop.GetPart<CropPart>().TicksPerStage); Assert.AreEqual(0, savedCrop.GetPart<CropPart>().MoistureTicks);
            }
        }
        [Test] public void FullBackpackLeavesCompostForAnotherCustomer()
        {
            var seller = Seller(); Assert.AreEqual(2, SludgeUnits(seller)); var item = seller.GetPart<InventoryPart>().Objects.First(i => i.BlueprintName == "InertSludge");
            Pack.MaxWeight = Pack.GetCarriedWeight(); Assert.False(TradeSystem.BuyFromTrader(Actor, seller, item));
            Assert.AreEqual(2, SludgeUnits(seller)); Assert.AreEqual(0, SludgeUnits(Actor)); Assert.AreEqual(1000, TradeSystem.GetDrams(Actor));
        }
    }
}
