using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    [Category("ThrowingComparison")]
    public sealed class ThrowingExpeditionComparisonTests:FiftyWorldFixture
    {
        ThrowingOpportunityComparisonTests registries;
        [SetUp] public void SetupExpedition(){registries=new ThrowingOpportunityComparisonTests();registries.ComparisonSetup();}
        [TearDown] public void RestoreExpedition(){registries?.ComparisonCleanup();}
        bool precision100;
        [Test] public void A3D12FiniteExpeditionAlternativeExtendsOnceTo100Seeds()
        {
            precision100=true;
            try{W6TwoTraversalsPreserveFiniteSupplyAndAllEnemyConsequences(6);}
            finally{precision100=false;}
        }
        [TestCase(0)][TestCase(2)][TestCase(4)][TestCase(6)]
        public void W6TwoTraversalsPreserveFiniteSupplyAndAllEnemyConsequences(int budget)
        {
            var receipts=new List<ThrowComparisonReceipt>();
            var packages=budget==0?new[]{"bank"}:budget==2?new[]{"bank","measured","driving","recovery","focused","puncture","tumble"}
                :budget==4?new[]{"bank","physical4","focus-tumble","water-tumble","blades4"}
                :new[]{"bank","all5-water","physical-dodge","water-acrobatics"};
            foreach(var package in packages)foreach(var policy in new[]{"conserve","spend-for-distance"})
            for(int seed=101;seed<=(precision100?200:120);seed++)
            {
                if(precision100&&(policy!="spend-for-distance"||package=="physical-dodge"))continue;
                var w=new ThrowingComparisonWorld(Factory,seed,budget,package,"A3-W6","outbound-return",policy);
                w.Enemy("MarlbackScrabbler",6,5);w.Enemy("Viper",10,7);w.Enemy("MarlbackScrabbler",11,3);
                for(int y=2;y<=4;y++)w.Spawn("StoneWall",8,y);
                w.Spawn("WaterPuddle",8,5);w.Spawn("WaterPuddle",9,5);
                var dagger=w.Item("Dagger");w.Start();bool reached=false,returning=false,extracted=false;
                int stalled=0;
                while(w.Alive && w.Receipt.actions<100 && stalled<6)
                {
                    var at=w.Zone.GetEntityCell(w.Player);
                    if(at.X==12&&at.Y==5){reached=true;returning=true;}
                    if(returning&&at.X==1&&at.Y==5){extracted=true;break;}
                    if(w.Player.GetStatValue("Hitpoints")<12)returning=true;
                    int before=w.Receipt.actions;
                    var enemy=w.Enemies.Where(e=>w.Zone.GetEntityCell(e)!=null&&e.GetStatValue("Hitpoints")>0)
                        .OrderBy(e=>w.Distance(e)).ThenBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault();
                    var healing=w.Item("HealingTonic");
                    if(w.Player.GetStatValue("Hitpoints")<24 && healing!=null)
                        w.Do("drink-finite-healing",()=>InventorySystem.PerformAction(w.Player,healing,"ApplyTonic",w.Zone));
                    else if(enemy!=null && w.Distance(enemy)<=1)
                    {
                        var weapon=StartingBuildService.PrimaryHandWeapon(w.Player);
                        if(weapon==null && w.Pack.Contains(dagger))w.Do("equip-recovered-dagger",()=>InventorySystem.Equip(w.Player,dagger));
                        else if(w.Has("ShortBlades_Puncture") && w.Cast("CommandPuncture",enemy)){}
                        else w.Melee(enemy);
                    }
                    else if(enemy!=null && w.Distance(enemy)<=5 && policy=="spend-for-distance")
                    {
                        var payload=w.Item("FrostTonic")??w.Item("LightningTonic")??w.Item("PoisonGasGrenade");
                        if(payload!=null)w.Throw(payload,enemy);
                        else if(w.Pack.Contains(dagger))
                        {
                            if(w.Has(ThrowingComparisonWorld.Measured)&&w.Player.GetPart<ThrowingStatePart>()?.HasPreparation!=true
                                &&ThrowingRules.CooldownRemaining(w.Player,ThrowingComparisonWorld.Measured)==0)w.Prepare(dagger,enemy);
                            else w.Throw(dagger,enemy);
                        }
                    }
                    if(before==w.Receipt.actions && w.Has(ThrowingComparisonWorld.Recovery)
                        && ThrowingRules.CanRecover(w.Player,dagger,w.Zone,out _))w.Recover(dagger);
                    if(before==w.Receipt.actions)
                    {
                        var floor=w.Zone.GetEntityCell(dagger);
                        if(floor!=null && floor==at)w.Pickup(dagger);
                        else
                        {
                            int gx=returning?1:12,gy=5;
                            if(floor!=null && AIHelpers.ChebyshevDistance(at.X,at.Y,floor.X,floor.Y)<=3 && w.Player.GetStatValue("Hitpoints")>=12)
                            {gx=floor.X;gy=floor.Y;}
                            var path=FindPath.Search(w.Zone,at.X,at.Y,gx,gy,actor:w.Player);
                            if(path.Usable && path.Steps.Count>0)w.Move(path.Steps[0].dx,path.Steps[0].dy);
                        }
                    }
                    if(before==w.Receipt.actions){stalled++;w.Wait();}else stalled=0;
                }
                string outcome=!w.Alive?"death":extracted?(reached?(w.Pack.Contains(dagger)?"objective-and-weapon":"objective-weapon-lost"):"early-retreat")
                    :w.Receipt.actions>=100?"timeout":reached?"partial-return":"route-stalled";
                receipts.Add(w.Finish(outcome,dagger));
                Assert.That(w.Receipt.actions,Is.LessThanOrEqualTo(100));
                Assert.IsEmpty(w.Receipt.supplied,"The expedition only has its exact starting supply.");
            }
            Assert.Greater(receipts.Count,0,"Selected comparison cannot silently run zero trials.");
            ThrowingComparisonWorld.SaveReceipts("W6-"+budget+(precision100?"-precision100":""),receipts);
        }
    }
}
