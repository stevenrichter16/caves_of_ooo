using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class CanonicalVillageQuestTests
    {
        // Six existing global stories, not six reusable quest templates.
        internal static readonly Dictionary<string,string> Hosts=new Dictionary<string,string>
        {
            {"Overworld.13.7.0","CrunchyLocket"}, // Tine: a lost personal token.
            {"Overworld.14.9.0","HiddenShrine"}, // Quillhold: investigate a remembered shrine.
            {"Overworld.8.16.0","ClearTheWarren"}, // Wellmeet: a farmer needs protection.
            {"Overworld.7.8.0","TheCandyTax"}, // Gantry: a clerk at the exchange.
            {"Overworld.5.9.0","MessageForHermit"}, // Posy: baker and withdrawn neighbor.
            {"Overworld.15.6.0","StrongestInOoo"}, // Sumphold: a physical labor feat.
        };
        private HotbarSaveFixture scope;
        private Dictionary<string,LootTableData> oldLoot;
        private bool initialized;
        private static readonly FieldInfo Tables=typeof(LootTableRegistry).GetField("_byName",BindingFlags.Static|BindingFlags.NonPublic);
        private static readonly FieldInfo Initialized=typeof(LootTableRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
        [SetUp] public void Setup()
        {
            oldLoot=new Dictionary<string,LootTableData>((Dictionary<string,LootTableData>)Tables.GetValue(null));initialized=LootTableRegistry.IsInitialized;
            scope=new HotbarSaveFixture(false,false);CinderholdCompositionTests.LoadLoot();
        }
        [TearDown] public void Teardown()
        {
            try {var t=(Dictionary<string,LootTableData>)Tables.GetValue(null);t.Clear();foreach(var p in oldLoot)t.Add(p.Key,p.Value);Initialized.SetValue(null,initialized);}
            finally {scope?.Dispose();}
        }
        [Test] public void AllSurfaceAddressesContainExactlySixUniqueCanonicalAssignments()
        {
            var found=new Dictionary<string,string>();
            for(int y=0;y<20;y++)for(int x=0;x<20;x++)
            {
                string id=WorldMap.ToZoneID(x,y,0),q=VillagePopulationBuilder.PickVillageQuest(id);
                if(!string.IsNullOrEmpty(q))found.Add(id,q);
            }
            CollectionAssert.AreEquivalent(Hosts.Keys,found.Keys,"Only actual assigned host towns may create these global stories.");
            foreach(var p in Hosts)Assert.AreEqual(p.Value,found[p.Key],p.Key);
            CollectionAssert.AreEquivalent(VillagePopulationBuilder.VillageQuestPoolIds,found.Values);
        }
        [TestCase(null)][TestCase("")][TestCase("Overworld.13.7.1")]
        [TestCase("Overworld.3.6.0")][TestCase("Overworld.10.10.0")][TestCase("Overworld.0.0.0")]
        public void NonHostsHaveNoPoolAssignment(string id)
        {Assert.IsTrue(string.IsNullOrEmpty(VillagePopulationBuilder.PickVillageQuest(id)),id);}

        [TestCase(64)][TestCase(1729)]
        public void ActualNamedVillagePipelinesPlaceOneInstanceOfEachGlobalStory(int seed)
        {
            var manager=OverworldZoneManager.CreateDetached(GrovelandsCompositionTests.Factory(),seed);
            var seen=new Dictionary<string,int>();var pool=new HashSet<string>(VillagePopulationBuilder.VillageQuestPoolIds);
            for(int y=0;y<20;y++)for(int x=0;x<20;x++)
            {
                var poi=manager.WorldMap.GetPOI(x,y);if(poi?.Type!=POIType.Village)continue;
                string id=WorldMap.ToZoneID(x,y,0);var zone=manager.GetZone(id);
                var beacons=zone.GetAllEntities().Select(e=>e.GetPart<QuestBeaconPart>()).Where(b=>b!=null&&pool.Contains(b.Quest)).ToArray();
                if(Hosts.TryGetValue(id,out string expected))
                {Assert.AreEqual(1,beacons.Length,id);Assert.AreEqual(expected,beacons[0].Quest,id);}
                else Assert.IsEmpty(beacons,id+" must not duplicate another town's story.");
                foreach(var b in beacons)seen[b.Quest]=seen.TryGetValue(b.Quest,out int n)?n+1:1;
            }
            CollectionAssert.AreEquivalent(pool,seen.Keys);Assert.IsTrue(seen.Values.All(n=>n==1));
        }
    }
}
