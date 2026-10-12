using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadOpeningAdversarialTests
    {
        HaulingContentScope scope;
        [SetUp] public void Setup(){scope=new HaulingContentScope();scope.Seed(64);}
        [TearDown] public void Cleanup(){scope.Dispose();}
        static Entity Owner(Zone z,string role)=>z.GetReadOnlyEntities().Single(e=>e.GetProperty(GleanersCellarBuilder.RoleKey)==role);
        [TestCase(10)] [TestCase(11)] [TestCase(12)] [TestCase(15)] [TestCase(16)]
        public void HistoricalUnvisitedGladeAndCellarRemainLiteral(int version)
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
            var world=new Entity();world.Properties[SpreadExplorationPlan.PropertyKey]=version+"|64|0";
            if(version>=11)world.Properties[SpreadExplorationPlan.WorldKeyProperty]=Guid.NewGuid().ToString("N");
            var restored=typeof(SpreadExplorationPlan).GetMethod("Restore",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{manager,world});
            typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(manager,restored);
            var glade=manager.GetZone(GleanersDistrict.SurfaceID);var cellar=manager.GetZone(GleanersCellarBuilder.ZoneID);
            Assert.False(glade.GetReadOnlyEntities().Any(e=>e.GetProperty(GleanersDistrict.RoleKey)=="crossing"));
            Assert.False(Owner(cellar,"supplies").GetPart<ContainerPart>().Contents.Any(e=>e.BlueprintName=="DitchkeepersFootwork"));
            Assert.False(Owner(cellar,"guard").HasPart<MeleeTraumaProfilePart>());
            Assert.AreEqual(version,manager.Exploration.Version);
        }
        [TestCase("missing")] [TestCase("wrong-skill")] [TestCase("full-cache")]
        public void InvalidNewRewardCannotPublishHalfACellar(string fault)
        {
            if(fault=="missing")scope.Factory.Blueprints.Remove("DitchkeepersFootwork");
            if(fault=="wrong-skill")scope.Factory.Blueprints["DitchkeepersFootwork"].Parts["Grimoire"]["SkillClassName"]="Rites_ShatteredRime";
            if(fault=="full-cache")scope.Factory.Blueprints["Crate"].Parts["Container"]["MaxItems"]="1";
            var zone=new Zone(GleanersCellarBuilder.ZoneID);
            Assert.False(new GleanersCellarBuilder(64,true,Guid.NewGuid().ToString("N"),true,true).BuildZone(zone,scope.Factory,new Random(3)));
            Assert.Zero(zone.EntityCount);Assert.IsEmpty(zone.GenReservedCells);
        }
        [TestCase(false)] [TestCase(true)]
        public void ProfileSurvivesRealSaveAndDoesNotBlockExplicitSevering(bool allowed)
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);
            var zone=manager.GetZone(GleanersDistrict.SurfaceID);manager.SetActiveZone(zone);
            var player=scope.Factory.CreateEntity("Player");zone.AddEntity(player,40,12);
            player.AddPart(new MeleeTraumaProfilePart{AllowIncidentalDismemberment=allowed});
            var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("opening-profile","fixture",manager,null,player));
            Assert.AreEqual(allowed,loaded.Player.GetPart<MeleeTraumaProfilePart>().AllowIncidentalDismemberment);
            var body=loaded.Player.GetPart<Body>();int before=body.CountParts("Hand");
            Assert.True(body.Dismember(body.GetParts().First(p=>p.Type=="Hand"),loaded.ZoneManager.ActiveZone,loaded.Player));
            Assert.AreEqual(before-1,body.CountParts("Hand"));
        }
        [TestCase(false)] [TestCase(true)]
        public void EarnedManualVaultUsesActualGladeGeometryAndRejectsOccupiedLanding(bool occupied)
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var glade=manager.GetZone(GleanersDistrict.SurfaceID);
            var cellar=manager.GetZone(GleanersCellarBuilder.ZoneID);var crate=Owner(cellar,"supplies").GetPart<ContainerPart>();
            var manual=crate.Contents.Single(e=>e.BlueprintName=="DitchkeepersFootwork");var player=scope.Factory.CreateEntity("Player");
            Assert.True(crate.RemoveItem(manual));Assert.True(player.GetPart<InventoryPart>().AddObject(manual));Assert.True(glade.AddEntity(player,27,17));
            int sp=player.GetStatValue("SP");
            for(int i=0;i<2;i++){var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)player);e.SetParameter("Command","ReadGrimoire");manual.FireEventAndRelease(e);}
            Assert.AreEqual(sp,player.GetStatValue("SP"));Assert.AreEqual(1,player.GetPart<ActivatedAbilitiesPart>().AbilityList.Count(a=>a.Command=="CommandVault"));
            if(occupied)Assert.True(glade.AddEntity(scope.Factory.CreateEntity("MarlbackScrabbler"),29,17));
            var skill=player.Parts.OfType<Acrobatics_Vault>().Single();
            Assert.AreEqual(!occupied,skill.OnCommand(new SkillEventContext{Attacker=player,Defender=player,Zone=glade,Rng=new Random(1),DirectionX=1,DirectionY=0}));
            Assert.AreEqual(occupied?(27,17):(29,17),glade.GetEntityPosition(player));
        }
        [TestCase("WetCrossing")] [TestCase("HeavyFrame")]
        public void NewSatelliteOffersChargedBookWithoutTouchingRolledGoods(string family)
        {
            using(var f=new ConnectedSatelliteTests.Fixture())
            {
                var original=f.Containers.SourceReceipt.Owners.SelectMany(e=>e.GetPart<ContainerPart>().Contents).ToArray();
                Assert.True(ConnectedSpreadSatellite.TryPlace(f.Zone,f.Factory,f.Terrain,f.Population,f.Containers,(SpreadExplorationFamily)Enum.Parse(typeof(SpreadExplorationFamily),family),()=>true,out var owners,out var final,true));
                Assert.True(final());var all=f.Containers.SourceReceipt.Owners.SelectMany(e=>e.GetPart<ContainerPart>().Contents).ToArray();
                Assert.True(original.All(all.Contains));var added=all.Except(original).ToArray();
                Assert.False(added.Any(e=>e.BlueprintName=="DitchkeepersFootwork"));
                Assert.AreEqual(10,added.Single(e=>e.BlueprintName=="ShatteredRimeGrimoire").GetPart<GrimoireChargePart>().Charges);
            }
        }
        [TestCase("skill")] [TestCase("charges")]
        public void MalformedSatelliteBookCannotPublish(string fault)
        {
            using(var f=new ConnectedSatelliteTests.Fixture())
            {
                var bp=f.Factory.Blueprints["ShatteredRimeGrimoire"];
                if(fault=="skill")bp.Parts["Grimoire"]["SkillClassName"]="Acrobatics_Vault";
                else bp.Parts["GrimoireCharge"]["Charges"]="0";
                var before=f.Zone.GetReadOnlyEntities().ToArray();
                Assert.False(ConnectedSpreadSatellite.TryPlace(f.Zone,f.Factory,f.Terrain,f.Population,f.Containers,SpreadExplorationFamily.HeavyFrame,()=>true,out var owners,out var final,true));
                CollectionAssert.AreEquivalent(before,f.Zone.GetReadOnlyEntities());
                Assert.True(f.Containers.SourceReceipt.IsCurrent);
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void RealAttackDamageIsUnchangedByTraumaCapability(int seed)
        {
            int Damage(bool restricted)
            {
                var zone=new Zone();var attacker=scope.Factory.CreateEntity("MarlbackScrabbler");var target=scope.Factory.CreateEntity("Player");
                if(restricted)attacker.AddPart(new MeleeTraumaProfilePart{AllowIncidentalDismemberment=false});
                zone.AddEntity(attacker,5,5);zone.AddEntity(target,6,5);
                var weapon=(seed%2==0?scope.Factory.CreateEntity("Dagger"):NaturalWeaponFactory.Create("MarlbackRake")).GetPart<MeleeWeaponPart>();
                weapon.BaseDamage="12";weapon.PenBonus=4;
                CombatSystem.PerformSingleAttack(attacker,target,weapon,true,zone,new Random(seed));
                return target.GetStatValue("Hitpoints");
            }
            Assert.AreEqual(Damage(false),Damage(true),"Attacker capability may change incidental anatomy only, never the hit or damage.");
        }
    }
}
