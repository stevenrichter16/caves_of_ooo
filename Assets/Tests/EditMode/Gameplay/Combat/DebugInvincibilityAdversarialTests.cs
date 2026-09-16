using System;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class DebugInvincibilityAdversarialTests
    {
        [TestCase("null")][TestCase("npc")][TestCase("no-hp")][TestCase("zero")][TestCase("negative")][TestCase("committed")]
        public void InvalidActorsCannotAcquireProtectionOrBeRevived(string invalid)
        {
            var p=DebugInvincibilityTests.Player();if(invalid=="null")p=null;else if(invalid=="npc")p.Tags.Remove("Player");else if(invalid=="no-hp")p.Statistics.Remove("Hitpoints");else if(invalid=="zero")p.GetStat("Hitpoints").BaseValue=0;else if(invalid=="negative")p.GetStat("Hitpoints").BaseValue=-3;else p.Tags["_DeathHandled"]="";
            Assert.IsFalse(DebugInvincibility.TryToggle(p,out bool on));Assert.IsFalse(on);Assert.IsFalse(DebugInvincibility.IsEnabled(p));
        }
        [TestCase("Fire",false)][TestCase("Fire",true)][TestCase("Cold",false)][TestCase("Cold",true)]
        [TestCase("Poison",false)][TestCase("Poison",true)][TestCase("Acid",false)][TestCase("Acid",true)]
        [TestCase("Electrical",false)][TestCase("Electrical",true)]
        public void TypedEnvironmentalDamageAndLethalAmountsRemainPlayerScoped(string attribute,bool enabled)
        {
            var p=DebugInvincibilityTests.Player();var npc=DebugInvincibilityTests.Player();npc.Tags.Remove("Player");if(enabled)DebugInvincibility.TryToggle(p,out _);
            var d=new Damage(999);d.AddAttribute(attribute);CombatSystem.ApplyDamage(p,d,null,null);CombatSystem.ApplyDamage(npc,999,null,null);
            Assert.AreEqual(enabled, p.GetStat("Hitpoints").BaseValue>0);Assert.IsTrue(CombatSystem.IsDeathHandled(npc));Assert.AreEqual(!enabled,CombatSystem.IsDeathHandled(p));
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(40)]
        public void RepeatedTogglesHaveNoAccumulatingHpOrTagEffects(int count)
        {
            var p=DebugInvincibilityTests.Player();for(int i=0;i<count;i++)Assert.IsTrue(DebugInvincibility.TryToggle(p,out _));
            Assert.AreEqual(count%2==1,DebugInvincibility.IsEnabled(p));Assert.AreEqual(40,p.GetStat("Hitpoints").BaseValue);Assert.AreEqual(1,p.Tags.Count);Assert.AreEqual(0,p.Properties.Count);
        }
        [Test] public void SameIdReplacementAndNpcCannotBorrowAPlayersProtection()
        {
            var p=DebugInvincibilityTests.Player();var other=DebugInvincibilityTests.Player();other.ID=p.ID;DebugInvincibility.TryToggle(p,out _);
            Assert.IsFalse(DebugInvincibility.IsEnabled(other));CombatSystem.ApplyDamage(other,1,null,null);Assert.AreEqual(39,other.GetStat("Hitpoints").BaseValue);Assert.AreEqual(40,p.GetStat("Hitpoints").BaseValue);
        }
        [Test] public void BlockedDamageDoesNotReachReactiveCallbacks()
        {
            var p=DebugInvincibilityTests.Player();var probe=new DamageProbe();p.AddPart(probe);DebugInvincibility.TryToggle(p,out _);CombatSystem.ApplyDamage(p,2,null,null);Assert.AreEqual(0,probe.Calls);
            DebugInvincibility.TryToggle(p,out _);CombatSystem.ApplyDamage(p,2,null,null);Assert.Greater(probe.Calls,0);
        }
        public sealed class DamageProbe:Part
        {public int Calls;public override bool HandleEvent(GameEvent e){if(e.ID=="BeforeTakeDamage"||e.ID=="TakeDamage"||e.ID=="Died")Calls++;return true;}}
    }
}
