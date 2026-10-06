using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class FiftySecondCombatDiscoveryTests
    {
        [TestCase("Corrosion","Corrosion_CausticTrail",16)]
        [TestCase("Corrosion","Corrosion_CausticDraw",20)]
        [TestCase("LongBlades","LongBlades_EnGarde",12)]
        [TestCase("LongBlades","LongBlades_FollowThrough",12)]
        public void NewTacticalChoicesAreOrdinaryPricedSkillPowers(string tree,string name,int cooldown)
        {
            SkillRegistry.ResetForTests();
            try
            {
                SkillRegistry.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Data/Skills",tree+".json")),"second-fifty-discovery");
                Assert.True(SkillRegistry.TryGetPowerByClass(name,out var definition));Assert.AreEqual(1,definition.Cost);
                var part=FiftySecondCombatFixture.Skill(name);Assert.AreEqual(cooldown,part.DeclareActivatedAbility(null).Cooldown);
                StringAssert.Contains("cooldown",definition.Description.ToLowerInvariant());Assert.Greater(definition.Description.Length,100);
                if(name=="Corrosion_CausticDraw")Assert.AreEqual("Corrosion_AcidSpray",definition.Requires);
            }
            finally{SkillRegistry.ResetForTests();}
        }
        [Test] public void TacticalStatusReadoutsExposeRemainingUseAndCounter()
        {
            var guard=EffectDescriber.Describe(new EnGardeEffect(1));StringAssert.Contains("1 of your turns",guard);StringAssert.Contains("long blade",guard);StringAssert.Contains("Spells",guard);
            var poise=EffectDescriber.Describe(new VaultPoiseEffect(1));StringAssert.Contains("1 adjacent melee hit roll",poise);StringAssert.Contains("miss",poise);
        }
    }
}
