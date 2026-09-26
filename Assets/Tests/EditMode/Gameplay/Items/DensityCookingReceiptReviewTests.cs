using System;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class DensityCookingReceiptReviewTests
    {
        [TestCase(false)] [TestCase(true)]
        public void CapacityRefusal_AfterLocalRestore_DoesNotUndoIndependentDroppedResident(bool outerTransaction)
        {
            var factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            var actor=new Entity{ID="capacity-cook"};var inventory=new InventoryPart();actor.AddPart(inventory);
            var raw=factory.CreateEntity("RawMeat");raw.GetPart<StackerPart>().StackCount=3;
            var cooked=factory.CreateEntity("CookedMeat");cooked.GetPart<PhysicsPart>().Weight=10;
            Assert.True(inventory.AddObject(raw));Assert.True(inventory.AddObject(cooked));inventory.MaxWeight=16;
            var zone=new Zone("capacity-callback");zone.AddEntity(actor,10,10);zone.AddEntity(factory.CreateEntity("Campfire"),11,10);
            var old=MessageLog.OnMessage;bool? dropped=null;
            var tx=outerTransaction?new CavesOfOoo.Core.Inventory.InventoryTransaction():null;
            try
            {
                MessageLog.OnMessage=message=>{if(message.Contains("raw food is unchanged"))dropped=InventorySystem.Drop(actor,cooked,zone);};
                Assert.False(CookingService.TryCook(actor,raw,zone,factory,tx));tx?.Rollback();
                Assert.AreEqual(true,dropped,"The refusal observer's independent transfer must succeed.");
                Assert.False(inventory.Objects.Contains(cooked),"Second receipt restore must not resurrect the dropped resident.");
                Assert.NotNull(zone.GetEntityCell(cooked));Assert.Null(cooked.GetPart<PhysicsPart>().InInventory);
                Assert.True(inventory.Objects.Contains(raw));Assert.AreEqual(3,raw.GetPart<StackerPart>().StackCount);
                Assert.AreEqual(1,cooked.GetPart<StackerPart>().StackCount);
            }
            finally{MessageLog.OnMessage=old;tx?.Rollback();}
        }
    }
}
