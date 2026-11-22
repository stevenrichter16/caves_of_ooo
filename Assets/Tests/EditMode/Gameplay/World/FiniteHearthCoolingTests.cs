using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FiniteHearthCoolingTests : FiftyWorldFixture
    {
        Entity Hearth(float temperature = 450f, bool owned = true)
        {
            var h = Place("CampHearth");
            if (owned) { h.AddPart(new InstallationMarkerPart { InstallationId = "player:" + h.ID, Origin = HeldFixtureActions.PlayerOrigin, RetainZone = true });
                var i = h.GetPart<InstallablePart>() ?? new InstallablePart(); if (h.GetPart<InstallablePart>() == null) h.AddPart(i); i.State = InstallablePart.Installed; i.Mode = InstallablePart.Portable; }
            h.GetPart<ThermalPart>().Temperature = temperature;
            h.GetPart<ThermalPart>().AmbientTemperature = 25f;
            h.GetPart<FuelPart>().FuelMass = 10f;
            h.GetPart<LightSourcePart>().Enabled = temperature >= 150f;
            return h;
        }
        InventoryAction PourRow(Entity f, bool one = false) => Actions(f).Single(a => a.Name == (one ? "PourOneLiquid" : "PourLiquid") && (a.Display.EndsWith(" east") || a.Display.Contains(" east (")));
        float Temp(Entity h) => h.GetPart<ThermalPart>().Temperature;
        [SetUp] public void SetupCooling() { Pack.MaxWeight = 400; PackableFixtureRegistry.ResetForTests(); }
        [TearDown] public void CleanupCooling() { PackableFixtureRegistry.ResetForTests(); }

        [TestCase("water")][TestCase("bog-mire")][TestCase("brine")]
        public void OnePaidCoolingUnitIsNotRecoverable(string liquid)
        {
            var h = Hearth(); var f = Flask(1, liquid); Assert.True(Act(f, PourRow(f).Command));
            Assert.AreEqual(410f, Temp(h), .01f); Assert.AreEqual(0, Units(f));
            Assert.AreEqual(0, PoolUnits()); Assert.False(Zone.GetCell(11,10).Objects.Any(e => e.HasPart<LiquidPoolPart>()));
            Assert.False(Zone.TileState.HasCoating(11,10,liquid));
            Assert.False(Actions(f).Any(a => a.Name == "FillLiquid"));
        }
        [TestCase(450f,12,11,1,25f)][TestCase(200f,12,5,7,25f)]
        [TestCase(150f,4,4,0,25f)][TestCase(450f,4,4,0,290f)]
        public void TariffLeavesOnlyUnusedSurplus(float t,int n,int used,int surplus,float after)
        {
            var h=Hearth(t); var f=Flask(n); Assert.True(Act(f,PourRow(f).Command));
            Assert.AreEqual(after,Temp(h),.01f); Assert.AreEqual(surplus,PoolUnits());
            Assert.AreEqual(n-used,PoolUnits()); Assert.AreEqual(10f,h.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void OlderPoolIsPreservedAndOnlyNewSurplusIsAdded()
        {
            var h=Hearth(); var p=Pool(7); var f=Flask(12);
            Assert.True(Act(f,PourRow(f).Command)); Assert.AreEqual(8,p.GetPart<LiquidPoolPart>().Volume); Assert.AreEqual(25f,Temp(h),.01f);
            Assert.True(Act(f,Choice(f,"FillLiquidVessel|"))); Assert.AreEqual(8,Units(f));
        }
        [Test] public void FullAbsorptionNeedsNoPoolFactoryAndPreservesOlderCoating()
        {
            var h=Hearth(); var f=Flask(1); Zone.TileState.WriteCoating(11,10,"water",8);
            string command=PourRow(f).Command; MaterialReactionResolver.Factory=null;
            Assert.True(Act(f,command)); Assert.AreEqual(410f,Temp(h),.01f);
            Assert.AreEqual(8,Zone.TileState.CoatingTurns(11,10,"water")); Assert.AreEqual(0,PoolUnits());
        }
        [TestCase(false)][TestCase(true)] public void TownAndColdHearthsKeepOrdinaryRecoverablePour(bool cold)
        {
            var h=Hearth(cold?25f:450f, cold); var f=Flask(3);
            Assert.True(Act(f,PourRow(f).Command)); Assert.AreEqual(3,PoolUnits()); Assert.AreEqual(cold?25f:450f,Temp(h));
        }
        [TestCase(350f)][TestCase(600f)] public void ChangedMenuQuoteRefusesNewPrice(float changed)
        {
            var h=Hearth(); var f=Flask(12); string command=PourRow(f).Command;
            h.GetPart<ThermalPart>().Temperature=changed;
            Assert.False(Act(f,command)); Assert.AreEqual(12,Units(f)); Assert.AreEqual(changed,Temp(h)); Assert.AreEqual(0,PoolUnits());
        }
        [Test] public void UnquotedNewFireCannotSilentlyConsumeOrdinaryGroundPour()
        {
            var f=Flask(1); string command=PourRow(f).Command; var h=Hearth();
            Assert.False(Act(f,command)); Assert.AreEqual(1,Units(f)); Assert.AreEqual(450f,Temp(h));
        }
        [Test] public void PreviewDisclosesExactSpentAndRecoverableUnits()
        {
            Hearth(); var f=Flask(12); var hint=PourRow(f).Hint;
            StringAssert.Contains("11",hint); StringAssert.Contains("1 recoverable",hint); StringAssert.Contains("evaporat",hint);
        }
        [Test] public void OuterRollbackHasNoCoolingAndNoPoolGhost()
        {
            var h=Hearth(); var f=Flask(12); FailAfter();
            Assert.False(Act(f,PourRow(f).Command)); Assert.AreEqual(12,Units(f)); Assert.AreEqual(450f,Temp(h)); Assert.AreEqual(0,PoolUnits());
        }
        [TestCase(false)][TestCase(true)] public void StaleCommitPreservesIndependentInjuryAndForcedDrop(bool drop)
        {
            var h=Hearth(); var f=Flask(1); int hp=Actor.GetStat("Hitpoints").Value;
            Actor.AddPart(new CoolingProbePart { OnAfter=() => {
                h.GetPart<ThermalPart>().Temperature=500f; Actor.GetStat("Hitpoints").BaseValue-=1;
                if(drop) { Assert.True(Pack.RemoveObject(f)); Assert.True(Zone.AddEntity(f,10,10)); }
            }});
            Assert.False(Act(f,PourRow(f).Command)); Assert.AreEqual(hp-1,Actor.GetStat("Hitpoints").Value);
            Assert.AreEqual(500f,Temp(h)); Assert.AreEqual(1,Units(f)); Assert.AreEqual(0,PoolUnits());
            Assert.AreEqual(!drop,Pack.Objects.Contains(f)); if(drop) Assert.NotNull(Zone.GetEntityCell(f));
        }
        [Test] public void IndependentOlderPoolAdditionSurvivesCoolingRollback()
        {
            var h=Hearth(); var p=Pool(7); var f=Flask(12);
            Actor.AddPart(new CoolingProbePart { OnAfter=()=>{ p.GetPart<LiquidPoolPart>().Volume+=2; h.GetPart<ThermalPart>().Temperature=500f; }});
            Assert.False(Act(f,PourRow(f).Command)); Assert.AreEqual(9,p.GetPart<LiquidPoolPart>().Volume); Assert.AreEqual(12,Units(f));
        }
        [Test] public void NaturalSourceKeepsItsIdentityAndOldStock()
        {
            var h=Hearth(); var p=Place("MirePool"); var pool=p.GetPart<LiquidPoolPart>(); pool.LiquidId="bog-mire"; pool.Volume=7;
            var f=Flask(12,"bog-mire"); Assert.True(Act(f,PourRow(f).Command)); Assert.AreEqual(8,pool.Volume); Assert.NotNull(Zone.GetEntityCell(p));
        }
        [TestCase("missing")][TestCase("moved")][TestCase("replaced")][TestCase("fuel")][TestCase("ambient")][TestCase("capacity")][TestCase("ambiguous")]
        public void StaleQuotedHearthCannotBeSubstituted(string change)
        {
            var h=Hearth(); var f=Flask(12); string command=PourRow(f).Command;
            switch(change) {
                case "missing":Zone.RemoveEntity(h);break;
                case "moved":Zone.RemoveEntity(h);Zone.AddEntity(h,12,10);break;
                case "replaced":Zone.RemoveEntity(h);Hearth();break;
                case "fuel":h.GetPart<FuelPart>().FuelMass=9f;break;
                case "ambient":h.GetPart<ThermalPart>().AmbientTemperature=26f;break;
                case "capacity":h.GetPart<ThermalPart>().HeatCapacity=2f;break;
                case "ambiguous":Hearth();break;
            }
            Assert.False(Act(f,command)); Assert.AreEqual(12,Units(f)); Assert.AreEqual(0,PoolUnits());
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(float.NegativeInfinity)]
        public void MalformedAmbientCannotBecomeAnUnpricedDose(float ambient)
        {
            var h=Hearth(); var f=Flask(1); string command=PourRow(f).Command; h.GetPart<ThermalPart>().AmbientTemperature=ambient;
            Assert.False(Act(f,command)); Assert.AreEqual(1,Units(f)); Assert.AreEqual(0,PoolUnits());
        }
        [Test] public void OnlySurplusCanWaterACropOnTheSameCell()
        {
            Crop(); var h=Hearth(); var f=Flask(12);
            Assert.True(Act(f,PourRow(f).Command)); Assert.AreEqual(25f,Temp(h),.01f); Assert.AreEqual(0,PoolUnits());
            Assert.AreEqual(40,Zone.GetCell(11,10).Objects.Single(e=>e.HasPart<CropPart>()).GetPart<CropPart>().MoistureTicks);
        }
        [Test] public void CropRollbackPreservesIndependentPoolAddition()
        {
            var crop=Crop(); var h=Hearth(); var f=Flask(12);
            Actor.AddPart(new CoolingProbePart { OnAfter=()=>{ Zone.GetCell(11,10).Objects.Single(e=>e.HasPart<LiquidPoolPart>()).GetPart<LiquidPoolPart>().Volume+=2; h.GetPart<ThermalPart>().Temperature=500f; }});
            Assert.False(Act(f,PourRow(f).Command)); Assert.AreEqual(2,PoolUnits()); Assert.AreEqual(12,Units(f));
            Assert.AreEqual(0,crop.GetPart<CropPart>().MoistureTicks);
        }
        [TestCase("flask-full")][TestCase("flask-unlike")][TestCase("pool-empty")]
        public void RawProvisionalCreditMutationIsBoundedAndNeverChangesLiquid(string change)
        {
            var h=Hearth(); var f=Flask(12); string command=PourRow(f,change!="pool-empty").Command;
            Actor.AddPart(new CoolingProbePart { OnAfter=()=>{
                if(change=="flask-full") f.GetPart<LiquidVesselPart>().Volume=12;
                if(change=="flask-unlike") {f.GetPart<LiquidVesselPart>().Volume=1;f.GetPart<LiquidVesselPart>().LiquidId="oil";}
                if(change=="pool-empty") Zone.GetCell(11,10).Objects.Single(e=>e.HasPart<LiquidPoolPart>()).GetPart<LiquidPoolPart>().Volume=0;
                h.GetPart<ThermalPart>().Temperature=500f;
            }});
            Assert.False(Act(f,command)); Assert.That(Units(f),Is.InRange(0,12)); Assert.That(PoolUnits(),Is.GreaterThanOrEqualTo(0));
            if(change=="flask-unlike") { Assert.AreEqual("oil",f.GetPart<LiquidVesselPart>().LiquidId); Assert.AreEqual(1,Units(f)); }
        }
        [Test] public void MovedResidualPoolCannotCoatItsFormerCell()
        {
            var h=Hearth(); var f=Flask(12);
            h.AddPart(new CoolingProbePart { OnHeat=()=>{var p=Zone.GetCell(11,10).Objects.Single(e=>e.HasPart<LiquidPoolPart>());Assert.True(Zone.RemoveEntity(p));Assert.True(Zone.AddEntity(p,12,10));}});
            Assert.True(Act(f,PourRow(f).Command)); Assert.AreEqual(1,PoolUnits(12,10)); Assert.AreEqual(0,PoolUnits());
            Assert.False(Zone.TileState.HasCoating(11,10,"water"));
        }
        [Test] public void ReplacedThermalPartCannotPublishSuccessFromDetachedState()
        {
            var h=Hearth(); var f=Flask(12);
            h.AddPart(new CoolingProbePart { OnHeat=()=>{h.RemovePart(h.GetPart<ThermalPart>());var replacement=new ThermalPart { Temperature=500f };h.AddPart(replacement);h.Parts.Remove(replacement);h.Parts.Insert(0,replacement);}});
            MessageLog.Clear(); Assert.True(Act(f,PourRow(f).Command)); Assert.AreEqual(500f,Temp(h)); Assert.AreEqual(0,Units(f));
            StringAssert.DoesNotContain("goes out",string.Join(" ",MessageLog.GetRecent(8)));
            StringAssert.DoesNotContain("burns lower",string.Join(" ",MessageLog.GetRecent(8)));
        }
        [Test] public void PreparationCallbackCannotChangeTheAcceptedQuoteOrRewindInjury()
        {
            var h=Hearth(); var f=Flask(12); string command=PourRow(f).Command; int hp=Actor.GetStat("Hitpoints").Value;
            Factory.RegisterPartType<CoolingFactoryProbe>();
            Factory.Blueprints[LiquidVesselService.PoolBlueprint].Parts[nameof(CoolingFactoryProbe)]=new System.Collections.Generic.Dictionary<string,string>();
            CoolingFactoryProbe.Callback=()=>{h.GetPart<ThermalPart>().Temperature=500f;Actor.GetStat("Hitpoints").BaseValue-=1;};
            try {Assert.False(Act(f,command));Assert.AreEqual(12,Units(f));Assert.AreEqual(hp-1,Actor.GetStat("Hitpoints").Value);Assert.AreEqual(500f,Temp(h));Assert.AreEqual(0,PoolUnits());}
            finally {CoolingFactoryProbe.Callback=null;}
        }
        [Test] public void EarlierCommittedObserverCannotRetargetPaidCooling()
        {
            var h=Hearth(); var f=Flask(1); string command=PourRow(f).Command; MessageLog.Clear();
            var tx=new InventoryTransaction();
            typeof(InventoryTransaction).GetMethod("AfterCommit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(tx,new object[]{new Action(()=>h.GetPart<ThermalPart>().Temperature=500f)});
            Assert.True(LiquidVesselService.TryAct(Actor,f,Zone,command,tx)); tx.Commit();
            Assert.AreEqual(500f,Temp(h));Assert.AreEqual(0,Units(f));Assert.AreEqual(0,PoolUnits());
            StringAssert.DoesNotContain("burns lower",string.Join(" ",MessageLog.GetRecent(8)));
        }
        [Test] public void InterceptedHeatDoesNotAnnounceCoolingOrRefundPaidAttempt()
        {
            var h=Hearth(); var f=Flask(1);var blocker=new CoolingProbePart { StopHeat=true };h.AddPart(blocker);h.Parts.Remove(blocker);h.Parts.Insert(0,blocker);
            MessageLog.Clear();Assert.True(Act(f,PourRow(f).Command));Assert.AreEqual(450f,Temp(h));Assert.AreEqual(0,Units(f));Assert.AreEqual(0,PoolUnits());
            StringAssert.DoesNotContain("burns lower",string.Join(" ",MessageLog.GetRecent(8)));
        }
        [Test] public void RollbackCannotRetireAnIndependentReplacementPoolPart()
        {
            var h=Hearth(); var f=Flask(12); Entity changed=null; LiquidPoolPart replacement=null;
            Actor.AddPart(new CoolingProbePart { OnAfter=()=>{
                changed=Zone.GetCell(11,10).Objects.Single(e=>e.HasPart<LiquidPoolPart>());
                changed.RemovePart(changed.GetPart<LiquidPoolPart>()); replacement=new LiquidPoolPart { LiquidId="water",Volume=5 };changed.AddPart(replacement);
                h.GetPart<ThermalPart>().Temperature=500f;
            }});
            Assert.False(Act(f,PourRow(f).Command)); Assert.AreEqual(12,Units(f));Assert.NotNull(Zone.GetEntityCell(changed));
            Assert.AreSame(replacement,changed.GetPart<LiquidPoolPart>());Assert.AreEqual(5,replacement.Volume);Assert.AreEqual(500f,Temp(h));
        }
        [Test] public void ThermalCallbacksSeePaidReservationAndCannotRecoverIt()
        {
            var h=Hearth(); var f=Flask(1); bool called=false;
            h.AddPart(new CoolingProbePart { OnHeat=() => { called=true; Assert.AreEqual(0,Units(f)); Assert.AreEqual(0,PoolUnits()); Assert.False(Actions(f).Any(a=>a.Name=="FillLiquid")); }});
            Assert.True(Act(f,PourRow(f).Command)); Assert.True(called); Assert.AreEqual(410f,Temp(h));
        }
        [Test] public void ThrowingThermalCallbackNeverRefundsCommittedDose()
        {
            var h=Hearth(); var f=Flask(1); h.AddPart(new CoolingProbePart { OnHeat=()=>throw new InvalidOperationException("committed cooling observer") });
            Assert.True(Act(f,PourRow(f).Command)); Assert.AreEqual(0,Units(f)); Assert.AreEqual(0,PoolUnits());
        }
    }
    public sealed class CoolingProbePart : Part
    {
        public Action OnAfter, OnHeat; public bool StopHeat;
        public override bool HandleEvent(GameEvent e) { if(e.ID=="AfterInventoryAction") OnAfter?.Invoke(); if(e.ID=="ApplyHeat") { OnHeat?.Invoke(); if(StopHeat)return false; } return true; }
    }
    public sealed class CoolingFactoryProbe : Part
    {
        public static Action Callback;
        public override bool HandleEvent(GameEvent e) {if(e.ID=="ObjectCreated") Callback?.Invoke();return true;}
    }

}
