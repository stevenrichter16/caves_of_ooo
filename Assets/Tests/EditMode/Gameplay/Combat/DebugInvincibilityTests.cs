using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DebugInvincibilityTests
    {
        public static Entity Player()
        {
            var e=new Entity { ID=Guid.NewGuid().ToString(), BlueprintName="Player" };
            e.Tags["Player"]="";e.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=40,Min=0,Max=40};return e;
        }
        [TestCase(false)][TestCase(true)]
        public void DamageAndToggleOffUseActualHpWithoutStatInflation(bool enabled)
        {
            var p=Player();var hp=p.GetStat("Hitpoints");hp.BaseValue=17;
            if(enabled)Assert.IsTrue(DebugInvincibility.TryToggle(p,out _));
            CombatSystem.ApplyDamage(p,5,null,null);Assert.AreEqual(enabled?17:12,hp.BaseValue);Assert.AreEqual(40,hp.Max);
            if(enabled){Assert.IsTrue(DebugInvincibility.TryToggle(p,out bool on));Assert.IsFalse(on);CombatSystem.ApplyDamage(p,5,null,null);Assert.AreEqual(12,hp.BaseValue);}
        }
        [TestCase(false)][TestCase(true)]
        public void DirectDeathCannotDropGearOrRemoveProtectedPlayer(bool enabled)
        {
            using(var f=new MortalDeathFixture())
            {
                f.Victim.Tags["Player"]="";if(enabled)DebugInvincibility.TryToggle(f.Victim,out _);
                CombatSystem.HandleDeath(f.Victim,f.Attacker,f.Zone);
                Assert.AreEqual(!enabled,CombatSystem.IsDeathHandled(f.Victim));Assert.AreEqual(enabled?0:1,f.Observer.Died);
                Assert.AreEqual(enabled,f.Zone.GetEntityCell(f.Victim)!=null);if(enabled)Assert.AreEqual(100,f.Victim.GetStat("Hitpoints").BaseValue);
            }
        }
        [TestCase(false,false)][TestCase(false,true)][TestCase(true,false)][TestCase(true,true)]
        public void DismembermentIsStoppedBeforeAnatomyOrLimbDrops(bool enabled,bool mortal)
        {
            using(var f=new MortalDeathFixture())
            {
                f.Victim.Tags["Player"]="";if(enabled)DebugInvincibility.TryToggle(f.Victim,out _);
                var part=mortal?f.Head:f.Scope.LeftArm;int count=f.Zone.EntityCount;
                Assert.AreEqual(!enabled,f.Cut(part,f.Attacker));Assert.AreEqual(enabled,part.ParentPart!=null);
                if(enabled){Assert.AreEqual(count,f.Zone.EntityCount);Assert.AreEqual(0,f.Body.DismemberedParts.Count);Assert.AreEqual(0,f.Observer.AfterCuts);}
            }
        }
        [TestCase(false)][TestCase(true)]
        public void PoisonAndBleedingTicksRespectProtection(bool enabled)
        {
            var p=Player();if(enabled)DebugInvincibility.TryToggle(p,out _);
            new PoisonedEffect(5,"1d1+2").OnTurnStart(p,null);new BleedingEffect(15,"1d1+1").OnTurnStart(p,null);
            Assert.AreEqual(enabled?40:35,p.GetStat("Hitpoints").BaseValue);
        }
        [Test] public void DiagnosticsExposeToggleRejectionAndBlockedDamage()
        {
            Diag.ResetAll();var p=Player();DebugInvincibility.TryToggle(p,out _);CombatSystem.ApplyDamage(p,5,null,null);DebugInvincibility.TryToggle(null,out _);
            Assert.AreEqual(1,DiagQuery.Count(new DiagQuery.Filter{Category="event",Kind="DebugInvincibilityToggled"}).Count);
            Assert.AreEqual(1,DiagQuery.Count(new DiagQuery.Filter{Category="event",Kind="DebugInvincibilityRejected"}).Count);
            Assert.AreEqual(1,DiagQuery.Count(new DiagQuery.Filter{Category="damage",Kind="DebugInvincibilityBlocked"}).Count);Diag.ResetAll();
        }
        [Test] public void SavedPlayerDoesNotSerializeDebugProtection()
        {
            var oldTurns=TurnManager.Active;
            try
            {
                var p=Player();var z=new Zone("Overworld.10.10.0");z.AddEntity(p,5,5);
                var manager=new OverworldZoneManager(null,333);manager.ReplaceLoadedState(new System.Collections.Generic.Dictionary<string,Zone>{{z.ZoneID,z}},z.ZoneID,new System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<ZoneConnection>>());
                var turns=new TurnManager();turns.AddEntity(p);turns.ProcessUntilPlayerTurn();DebugInvincibility.TryToggle(p,out _);
                var state=GameSessionState.Capture(Guid.NewGuid().ToString(),"debug toggle",manager,turns,p);
                var restored=HotbarSaveFixture.RoundTrip(state).Player;Assert.AreEqual(p.ID,restored.ID);Assert.IsFalse(DebugInvincibility.IsEnabled(restored));Assert.IsTrue(DebugInvincibility.IsEnabled(p));
            }
            finally { typeof(TurnManager).GetProperty("Active").GetSetMethod(true).Invoke(null,new object[]{oldTurns}); }
        }
    }
}
