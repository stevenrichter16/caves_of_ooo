using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public abstract class ContainerCurrencyFixture : FiftyWorldFixture
    {
        protected Entity Box;
        protected ContainerPart Contents => Box.GetPart<ContainerPart>();
        [SetUp] public void SetupCurrency() { Box=Place("Chest");TradeSystem.SetDrams(Actor,100);Diag.ResetAll();Diag.SetChannel("event",true); }
        [TearDown] public void ClearCurrencyDiagnostics() => Diag.ResetAll();
        protected Entity Coin(int quantity=3) { var item=Factory.CreateEntity("GoldCoin");item.GetPart<StackerPart>().StackCount=quantity;Assert.True(Contents.AddItem(item));return item; }
        protected InventoryCommandResult Take(Entity item,bool rollback=false) { IInventoryCommand command=new TakeFromContainerCommand(Box,item);return InventorySystem.ExecuteCommand(rollback?new FailAfterCommand(command):command,Actor,Zone); }
        protected sealed class Hook : Part { public Action<GameEvent> Action; public override string Name=>"ContainerCurrencyHook"; public override bool HandleEvent(GameEvent e) {if(e.ID=="Taken")Action(e);return true;} }
        sealed class FailAfterCommand : IInventoryCommand
        {
            readonly IInventoryCommand inner; public FailAfterCommand(IInventoryCommand inner){this.inner=inner;} public string Name=>inner.Name;
            public InventoryValidationResult Validate(InventoryContext context)=>inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context,InventoryTransaction tx){var result=inner.Execute(context,tx);return result.Success?InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed,"forced outer refusal"):result;}
        }
        protected int Records(string kind,Entity target)=>DiagQuery.Count(new DiagQuery.Filter{Kind=kind,Actor=Actor.ID,Target=target.ID}).Count;
    }
    public sealed class ContainerCurrencyTests : ContainerCurrencyFixture
    {
        [TestCase(1)] [TestCase(3)] [TestCase(100)]
        public void ContainerCoinsBecomeFiveDramsPerUnitExactlyOnce(int count)
        {
            var coin=Coin(count);int taken=0;coin.AddPart(new Hook{Action=e=>{taken++;Assert.AreSame(Actor,e.GetParameter<Entity>("Actor"));Assert.AreEqual(100,TradeSystem.GetDrams(Actor),"credit is not available in provisional callbacks");Assert.AreEqual(0,coin.GetPart<StackerPart>().StackCount);}});
            Assert.True(Take(coin).Success);Assert.AreEqual(100+count*5,TradeSystem.GetDrams(Actor));Assert.AreEqual(1,taken);Assert.False(Contents.Contents.Contains(coin));Assert.False(Pack.Objects.Contains(coin));Assert.Null(coin.GetPart<PhysicsPart>().InInventory);
            Assert.False(Take(coin).Success);Assert.AreEqual(100+count*5,TradeSystem.GetDrams(Actor));Assert.AreEqual(1,taken);Assert.AreEqual(1,Records("ItemAcquisitionApplied",coin));
        }
        [Test] public void OrdinaryLootStillTransfersAsAnItem()
        {var item=Factory.CreateEntity("Starapple");Assert.True(Contents.AddItem(item));Assert.True(Take(item).Success);Assert.Contains(item,Pack.Objects);Assert.AreEqual(100,TradeSystem.GetDrams(Actor));}
        [Test] public void CoinConversionDoesNotRequireInventoryWeightSpace()
        {var coin=Coin();Pack.MaxWeight=0;Assert.True(Take(coin).Success);Assert.AreEqual(115,TradeSystem.GetDrams(Actor));Assert.AreEqual(0,Pack.Objects.Count);}
        [Test] public void AuthoredBanditCacheCoinRollUsesNormalContainerAcquisitionAndSave()
        {
            var field=typeof(LootTableRegistry).GetField("_byName",BindingFlags.Static|BindingFlags.NonPublic);var init=typeof(LootTableRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
            var tables=(Dictionary<string,LootTableData>)field.GetValue(null);var old=new Dictionary<string,LootTableData>(tables);bool was=(bool)init.GetValue(null);
            try { LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Loot/LootTables.json")));
                var roll=LootTableRegistry.Roll("BanditCacheT2",new System.Random(4));int count=roll.Count(x=>x=="GoldCoin");Assert.That(count,Is.InRange(5,12));
                foreach(string blueprint in roll)Assert.True(Contents.AddItem(Factory.CreateEntity(blueprint)));
                var coin=Contents.Contents.Single(x=>x.BlueprintName=="GoldCoin");Assert.AreEqual(count,coin.GetPart<StackerPart>().StackCount);Assert.True(Take(coin).Success);Assert.AreEqual(100+5*count,TradeSystem.GetDrams(Actor));
                using(var stream=new MemoryStream()){var writer=new SaveWriter(stream);writer.WriteEntityReference(Actor);writer.WriteEntityReference(Box);writer.WriteQueuedEntityBodies();stream.Position=0;var reader=new SaveReader(stream,Factory);var player=reader.ReadEntityReference();var box=reader.ReadEntityReference();reader.ReadEntityBodies();Assert.AreEqual(100+5*count,TradeSystem.GetDrams(player));Assert.False(box.GetPart<ContainerPart>().Contents.Any(x=>x.BlueprintName=="GoldCoin"));Assert.False(player.GetPart<InventoryPart>().Objects.Any(x=>x.BlueprintName=="GoldCoin"));}
            } finally {tables.Clear();foreach(var pair in old)tables.Add(pair.Key,pair.Value);init.SetValue(null,was);}
        }
    }
}
