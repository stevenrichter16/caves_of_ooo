using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FiftySecondCombatTerrainTests
    {
        FiftySecondCombatFixture f;
        [SetUp] public void Setup()=>f=new FiftySecondCombatFixture();
        [TearDown] public void Teardown()=>f.Dispose();
        [TestCase("FlameJet")] [TestCase("Backdraft")] [TestCase("JetBlast")]
        public void ConeWritesTheActualSideOfItsFan(string name)
        {
            BaseSkillPart s=name=="FlameJet"?(BaseSkillPart)new Pyromancy_FlameJet():name=="Backdraft"?new Pyromancy_Backdraft():new Hydromancy_JetBlast();
            var a=f.Actor(s); Assert.True(f.Cast(a,s));
            var state=f.Zone.TileState.Get(12,11); Assert.NotNull(state,"The lateral second band is inside the actual cone.");
            if(name=="JetBlast")Assert.True(f.Zone.TileState.HasCoating(12,11,"water")); else Assert.Greater(state.Heat,0);
            Assert.False(f.Zone.TileState.Has(10,11)); Assert.False(f.Zone.TileState.Has(14,10));
            Assert.AreEqual(s.DeclareActivatedAbility(a).Cooldown,FiftySecondCombatFixture.Cooldown(a,s));
        }
        [TestCase("FlameJet")] [TestCase("JetBlast")]
        public void SolidFirstCellShadowsWholeCone(string name)
        {
            BaseSkillPart s=name=="FlameJet"?(BaseSkillPart)new Pyromancy_FlameJet():new Hydromancy_JetBlast();
            var a=f.Actor(s); f.Wall(11,10); f.Cast(a,s);
            Assert.False(f.Zone.TileState.Has(12,11)); Assert.False(f.Zone.TileState.Has(12,10));
        }
        [TestCase(6,true)] [TestCase(ZoneTileState.Permanent,false)]
        public void BreezeRemovesTransientWaterOnly(int lifetime,bool removed)
        {
            var s=new Hydromancy_DryingBreeze();var a=f.Actor(s);
            f.Zone.TileState.WriteCoating(11,10,"water",lifetime);f.Zone.TileState.WriteCoating(11,10,"oil",6);
            f.Zone.TileState.WriteCoating(12,10,"water",6);f.Zone.TileState.AddCharge(11,10,1);
            Assert.True(f.Cast(a,s));Assert.AreEqual(!removed,f.Zone.TileState.HasCoating(11,10,"water"));
            Assert.True(f.Zone.TileState.HasCoating(11,10,"oil"));Assert.True(f.Zone.TileState.HasCoating(12,10,"water"));
            Assert.AreEqual(1,f.Zone.TileState.Get(11,10).Charge,"Drying is not free electrical absorption.");
        }
        [TestCase(1,0)] [TestCase(0,1)] [TestCase(-1,0)] [TestCase(0,-1)] [TestCase(1,1)] [TestCase(-1,1)] [TestCase(1,-1)] [TestCase(-1,-1)]
        public void WallFormsAcrossChosenDirection(int dx,int dy)
        {
            var s=new Cryomancy_GlacialWall();var a=f.Actor(s);Assert.True(f.Cast(a,s,dx:dx,dy:dy));
            var ice=f.Zone.GetAllEntities().Where(e=>e.BlueprintName=="IceWall").ToArray();Assert.AreEqual(3,ice.Length);
            for(int offset=-1;offset<=1;offset++)
                Assert.True(ice.Any(e=>f.Zone.GetEntityPosition(e)==(10+2*dx-dy*offset,10+2*dy+dx*offset)),"Missing crosswise slot "+offset);
            Assert.False(ice.Any(e=>f.Zone.GetEntityPosition(e)==(10+dx,10+dy)));
        }
        [Test] public void WallSkipsOccupiedSideAndRefusesOpaqueApproach()
        {
            var s=new Cryomancy_GlacialWall();var a=f.Actor(s);var victim=f.Target(12,11);
            Assert.True(f.Cast(a,s));Assert.AreEqual(2,f.Zone.GetAllEntities().Count(e=>e.BlueprintName=="IceWall"));
            Assert.AreSame(victim,f.Zone.GetCell(12,11).Occupants.Single());
            foreach(var ice in f.Zone.GetAllEntities().Where(e=>e.BlueprintName=="IceWall").ToArray())f.Zone.RemoveEntity(ice);
            FiftySecondCombatFixture.Ready(a);f.Wall(11,10);Assert.False(f.Cast(a,s));Assert.AreEqual(0,FiftySecondCombatFixture.Cooldown(a,s));
        }
        [Test] public void NaturalIceWallExpiryLeavesFiniteShallowWater()
        {
            var ice=f.Factory.CreateEntity("IceWall");Assert.True(f.Zone.AddEntity(ice,12,10));f.Tick(ice,7);
            Assert.NotNull(f.Zone.GetEntityCell(ice));Assert.False(f.Zone.TileState.HasCoating(12,10,"water"));f.Tick(ice);
            Assert.Null(f.Zone.GetEntityCell(ice));Assert.AreEqual(4,f.Zone.TileState.CoatingTurns(12,10,"water"));
            for(int i=0;i<4;i++)f.Zone.TileState.Tick();Assert.False(f.Zone.TileState.HasCoating(12,10,"water"));
        }
        [Test] public void RemovedWallAndOrdinaryExpiryDoNotCreateWater()
        {
            var ice=f.Factory.CreateEntity("IceWall");Assert.True(f.Zone.AddEntity(ice,12,10));f.Zone.RemoveEntity(ice);f.Tick(ice,8);
            var other=new Entity();other.AddPart(new LifespanPart { TurnsRemaining=1 });Assert.True(f.Zone.AddEntity(other,14,10));f.Tick(other);
            Assert.False(f.Zone.TileState.HasCoating(12,10,"water"));Assert.False(f.Zone.TileState.HasCoating(14,10,"water"));
        }
        [Test] public void TrailCreatesOneFiniteFilmPerRayCellAndOrdinaryEntryCoats()
        {
            var s=FiftySecondCombatFixture.Skill("Corrosion_CausticTrail");var a=f.Actor(s);var target=f.Target(12,11);
            Assert.True(f.Cast(a,s));var film=f.Zone.GetAllEntities().Where(e=>e.BlueprintName=="CausticFilm").ToArray();Assert.AreEqual(3,film.Length);
            Assert.True(MovementSystem.TryMoveTo(target,f.Zone,12,10));Assert.NotNull(target.GetEffect<AcidicEffect>());
            Assert.AreEqual(.35f,target.GetEffect<AcidicEffect>().Corrosion,.001f);Assert.AreEqual(1000,target.GetStatValue("Hitpoints"));
            Assert.True(film.All(e=>e.GetPart<PhysicsPart>().Takeable==false&&e.GetPart<LiquidPoolPart>()==null));
            FiftySecondCombatFixture.Ready(a);Assert.True(f.Cast(a,s));Assert.AreEqual(3,f.Zone.GetAllEntities().Count(e=>e.BlueprintName=="CausticFilm"));
            foreach(var e in film)f.Tick(e,4);Assert.False(f.Zone.GetAllEntities().Any(e=>e.BlueprintName=="CausticFilm"));
        }
        [Test] public void TrailStopsAtWallAndNoRoomIsFree()
        {
            var s=FiftySecondCombatFixture.Skill("Corrosion_CausticTrail");var a=f.Actor(s);f.Wall(11,10);
            Assert.False(f.Cast(a,s));Assert.AreEqual(0,FiftySecondCombatFixture.Cooldown(a,s));
            Assert.False(f.Zone.GetAllEntities().Any(e=>e.BlueprintName=="CausticFilm"));
        }
    }
}
