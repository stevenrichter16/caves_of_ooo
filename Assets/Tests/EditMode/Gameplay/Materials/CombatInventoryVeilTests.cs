using System;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CombatInventoryVeilTests
    {
        Zone zone;
        [SetUp] public void Setup()
        {
            zone = new Zone("combat-inventory-veil");
            GasRegistry.Initialize(@"{""Gases"":[{""Id"":""cryo-mist"",""GasType"":""Cryo"",""Glyph"":""°"",""Color"":""&C"",""BehaviorKind"":""Cryo""}]}");
        }
        [TearDown] public void Cleanup() => GasRegistry.ResetForTests();
        GasGrenadePart Grenade(bool cover)
        {
            var item = new Entity(); var part = new GasGrenadePart { GasId = "cryo-mist", Density = 10, Level = 1 }; item.AddPart(part);
            if (cover) { CombatInventoryPursuitTests.Set(part, "SightCloud", "veil-mist"); CombatInventoryPursuitTests.Set(part, "SightCloudTurns", 4); }
            return part;
        }
        [TestCase("veil-mist", true)] [TestCase("smoke", true)] [TestCase("steam", false)] [TestCase("cryo-mist", false)]
        public void SharedOpacityPolicyRecognizesOnlyDeliberatelyOpaqueClouds(string cloud, bool opaque)
        {
            zone.TileState.WriteCloud(5, 5, cloud, 3); FieldOfView.Compute(zone, 3, 5, 10);
            Assert.AreEqual(opaque, zone.TileState.ObscuresSight(5, 5)); Assert.AreEqual(!opaque, zone.GetCell(7, 5).IsVisible);
            Assert.AreEqual(!opaque, AIHelpers.HasLineOfSight(zone, 3, 5, 7, 5)); Assert.False(zone.GetCell(5,5).BlocksMovement());
        }
        [TestCase(false)] [TestCase(true)] public void OptionalCoverPreservesNineRealColdGasPayloads(bool cover)
        {
            var actor = new Entity(); var grenade = Grenade(cover);
            Assert.AreEqual(9, grenade.Detonate(actor, zone.GetCell(5, 5), zone));
            foreach (var gas in zone.GetEntitiesWithTag("Gas"))
            {
                var at = zone.GetEntityCell(gas); Assert.NotNull(gas.GetPart<GasCryoPart>());
                Assert.AreEqual(10, gas.GetPart<GasPoolPart>().Density); Assert.AreSame(actor, gas.GetPart<GasPoolPart>().Creator);
                Assert.AreEqual(cover, zone.TileState.ObscuresSight(at.X, at.Y));
                if (cover) Assert.AreEqual(4, zone.TileState.Get(at.X,at.Y).CloudTurns);
            }
        }
        [Test] public void OpaqueMistDoesNotLeakIntoSolidCellsOrAcrossASealedDiagonal()
        {
            var a = new Entity(); a.SetTag("Solid"); zone.AddEntity(a, 6, 5);
            var b = new Entity(); b.SetTag("Solid"); zone.AddEntity(b, 5, 6);
            Grenade(true).Detonate(null, zone.GetCell(5,5), zone);
            Assert.False(zone.TileState.ObscuresSight(6,5)); Assert.False(zone.TileState.ObscuresSight(5,6));
            Assert.False(zone.TileState.ObscuresSight(6,6)); Assert.True(zone.TileState.ObscuresSight(4,4));
        }
        [Test] public void FailedGasSpawnDoesNotLeaveAnUnpaidVisualCoverPayload()
        {
            var grenade = Grenade(true); grenade.GasId = "not-a-gas";
            Assert.Zero(grenade.Detonate(null, zone.GetCell(5,5), zone)); Assert.Zero(zone.TileState.WrittenCount);
        }
        [Test] public void RealGasCloudIsNotAnImpactObjectForAThrownProjectile()
        {
            var source = new Entity(); source.SetTag("Creature"); zone.AddEntity(source,2,5);
            var target = new Entity(); target.SetTag("Creature"); zone.AddEntity(target,8,5);
            Grenade(false).Detonate(source,zone.GetCell(5,5),zone);
            Assert.AreSame(target,LineTargeting.TraceFirstImpactToTarget(zone,source,2,5,8,5,10).HitEntity);
        }
        [Test] public void OrdinaryPhysicalPropStillInterceptsAThrownProjectile()
        {
            var source = new Entity(); source.SetTag("Creature"); zone.AddEntity(source,2,5);
            var target = new Entity(); target.SetTag("Creature"); zone.AddEntity(target,8,5);
            var prop = new Entity(); prop.AddPart(new PhysicsPart{Takeable=true,Solid=false});zone.AddEntity(prop,5,5);
            Assert.AreSame(prop,LineTargeting.TraceFirstImpactToTarget(zone,source,2,5,8,5,10).HitEntity);
        }
        [Test] public void VeilExpiryAndReplacementInvalidateVisibilityAsSmokeDoes()
        {
            int changes=0; zone.TileState.OnSightChanged=()=>changes++;
            zone.TileState.WriteCloud(5,5,"veil-mist",2); Assert.AreEqual(1,changes);
            zone.TileState.Tick(); Assert.AreEqual(1,changes); zone.TileState.Tick(); Assert.AreEqual(2,changes);
            zone.TileState.WriteCloud(5,5,"veil-mist",4); zone.TileState.WriteCloud(5,5,"steam",3); Assert.AreEqual(4,changes);
        }
        [Test] public void VeilRoundTripRetainsRemainingLifetimeAndLoadedTopologyInvalidation()
        {
            zone.TileState.WriteCloud(5,5,"veil-mist",4); zone.TileState.Tick();
            var loaded = new Zone("veil-loaded"); int changes=0; loaded.TileState.OnSightChanged=()=>changes++;
            loaded.TileState.LoadFromString(zone.TileState.ToSaveString()); Assert.AreEqual(1,changes);
            Assert.True(loaded.TileState.ObscuresSight(5,5)); Assert.AreEqual(3,loaded.TileState.Get(5,5).CloudTurns);
            loaded.TileState.Tick(); loaded.TileState.Tick(); loaded.TileState.Tick(); Assert.False(loaded.TileState.ObscuresSight(5,5));
        }
        [Test] public void VeilCutsLightWithoutMovingSourceAndRecoversAfterClearing()
        {
            zone.AmbientLevel=0; var lamp=new Entity(); lamp.AddPart(new LightSourcePart{Radius=10,Intensity=1,LightColor="&W"});zone.AddEntity(lamp,3,5);
            var light=new LightMap();light.Compute(zone);float clear=light.GetBrightness(7,5);Assert.Greater(clear,0);
            zone.TileState.WriteCloud(5,5,"veil-mist",4); light.Compute(zone);Assert.Zero(light.GetBrightness(7,5));
            zone.TileState.Clear(5,5);light.Compute(zone);Assert.AreEqual(clear,light.GetBrightness(7,5));
        }
        [TestCase("VeilpuffBladder", "cryo-mist", true)]
        [TestCase("SomeCryoGrenade", "cryo-mist", false)]
        [TestCase("VeilpuffBladder", "not-a-gas", false)]
        public void ExistingSavedVeilpuffGetsItsBoundedCoverWithoutChangingOtherPayloads(string blueprint,string gas,bool opaque)
        {
            var grenade=Grenade(false);grenade.ParentEntity.BlueprintName=blueprint;grenade.GasId=gas;
            grenade.Detonate(null,zone.GetCell(5,5),zone);Assert.AreEqual(opaque,zone.TileState.ObscuresSight(5,5));
            if(opaque)Assert.AreEqual(4,zone.TileState.Get(5,5).CloudTurns);
        }
        [TestCase(-1)][TestCase(0)]
        public void ExplicitCoverConfigurationCanDisableSavedVeilpuffFallback(int turns)
        {
            var grenade=Grenade(false);grenade.ParentEntity.BlueprintName="VeilpuffBladder";
            CombatInventoryPursuitTests.Set(grenade,"SightCloud",turns==0?"steam":"");
            CombatInventoryPursuitTests.Set(grenade,"SightCloudTurns",turns);
            grenade.Detonate(null,zone.GetCell(5,5),zone);Assert.False(zone.TileState.ObscuresSight(5,5));
        }
    }
}
