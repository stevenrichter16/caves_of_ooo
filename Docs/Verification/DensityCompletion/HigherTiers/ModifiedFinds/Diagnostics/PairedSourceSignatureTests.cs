using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class PairedSourceSignatureTests
    {
        [Test] public void ArchiveActualSourceStockAndNextRandomValue()
        {
            string output=Environment.GetEnvironmentVariable("COO_MODIFIER_SIGNATURE");
            Assert.IsNotEmpty(output);
            var factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Loot/LootTables.json")));
            EnhancementFactory.ForceReinitialize();var rows=new List<string>();
            foreach(string table in new[]{"DeepReliquaryT4","SealedVaultT3","BasketT3","HollowLogT3"})
            for(int seed=0;seed<32;seed++)
            {
                var rng=new Random(seed);var chest=factory.CreateEntity("LockedChest");
                int count=LootStocker.StockContainer(chest,table,factory,rng);
                var contents=chest.GetPart<ContainerPart>().Contents;
                rows.Add(table+"|"+seed+"|count="+count+"|"+string.Join(";",contents.Select(item=>item.BlueprintName
                    +":units="+(item.GetPart<StackerPart>()?.StackCount??1)+":value="+TradeSystem.GetItemValue(item)
                    +":marker="+item.Properties.ContainsKey(FoundEquipmentEnhancements.RollMarker)
                    +":mod="+string.Join(",",item.Parts.OfType<IItemEnhancement>().Select(e=>e.Name+":"+e.Tier))
                    +":AV="+item.GetPart<ArmorPart>()?.AV+":light="+item.GetPart<LightSourcePart>()?.Radius))
                    +"|next="+rng.Next());
            }
            File.WriteAllLines(output,rows);Assert.AreEqual(128,rows.Count);
        }
    }
}
