using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class CivicQuartetPreviewTests
    {
        // Actual manager seed0 means random. A pure plan labelled seed0 and a
        // native manager requested with0 can show unrelated formations.
        [TestCase("Overworld.7.8.0")][TestCase("Overworld.13.7.0")]
        [TestCase("Overworld.14.9.0")][TestCase("Overworld.10.14.0")]
        public void PreviewSeedsAreReproducibleAndActuallyRepresentThreeNativeFormations(string id)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.CivicQuartetCompositionPreviewBatch")).FirstOrDefault(t=>t!=null);Assert.NotNull(type);
            var method=type.GetMethod("Seeds",BindingFlags.Static|BindingFlags.NonPublic);Assert.NotNull(method);
            var seeds=((IEnumerable<int>)method.Invoke(null,new object[]{id})).ToArray();Assert.AreEqual(3,seeds.Length);Assert.IsFalse(seeds.Contains(0),"Zero is the native nondeterministic sentinel, not a reproducible demonstration seed.");
            var factory=GrovelandsCompositionTests.Factory();var forms=new HashSet<string>();
            foreach(int seed in seeds)
            {
                var m=OverworldZoneManager.CreateDetached(factory,seed);Assert.AreEqual(seed,m.WorldSeed);
                forms.Add(id==GantryCompositionPlan.ZoneID?GantryCompositionPlan.Create(id,m.WorldSeed).FormationName:id==TineCompositionPlan.ZoneID?TineCompositionPlan.Create(id,m.WorldSeed).FormationName:id==QuillholdCompositionPlan.ZoneID?QuillholdCompositionPlan.Create(id,m.WorldSeed).FormationName:TallyCompositionPlan.Create(id,m.WorldSeed).FormationName);
            }
            Assert.AreEqual(3,forms.Count);
        }
    }
}
