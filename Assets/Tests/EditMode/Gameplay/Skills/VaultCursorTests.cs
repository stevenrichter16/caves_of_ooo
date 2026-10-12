using System;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class VaultCursorTests
    {
        HaulingContentScope content;
        Zone zone; Entity player;
        [SetUp] public void Setup()
        {
            content=new HaulingContentScope();content.Seed(64);zone=new Zone();
            player=content.Factory.CreateEntity("Player");zone.AddEntity(player,10,10);
            player.GetStat("Agility").BaseValue=100;
        }
        [TearDown] public void Cleanup(){content.Dispose();}
        bool Cast(int x,int y)=>new Acrobatics_Vault().OnCommand(new SkillEventContext{Attacker=player,Zone=zone,TargetCell=zone.GetCell(x,y),Rng=new Random(64)});
        [Test] public void DeclaresCursorTargetingWithoutChangingSerializedOlderModes()
        {Assert.AreEqual(3,(int)new Acrobatics_Vault().DeclareActivatedAbility(player).TargetingMode);}
        [TestCase(11,10)] [TestCase(12,10)] [TestCase(12,12)] [TestCase(12,11)]
        public void EmptyTileWithinTwoCellsIsTheActualLanding(int x,int y)
        {Assert.True(Cast(x,y));Assert.AreEqual((x,y),zone.GetEntityPosition(player));}
        [TestCase(11,10,10,10)] [TestCase(12,10,11,10)] [TestCase(12,12,11,11)] [TestCase(12,11,11,10)]
        public void CreatureTargetLandsOneCellShortAndUsesRealMelee(int x,int y,int lx,int ly)
        {
            var enemy=content.Factory.CreateEntity("MarlbackScrabbler");zone.AddEntity(enemy,x,y);int hp=enemy.GetStatValue("Hitpoints");
            Assert.True(Cast(x,y));Assert.AreEqual((lx,ly),zone.GetEntityPosition(player));
            Assert.Less(enemy.GetStatValue("Hitpoints"),hp,"The ordinary combat pipeline must actually strike.");
        }
        [TestCase(13,10)] [TestCase(10,10)]
        public void InvalidDistanceDoesNotMove(int x,int y)
        {Assert.False(Cast(x,y));Assert.AreEqual((10,10),zone.GetEntityPosition(player));}
        [Test] public void BlockedAttackLandingDoesNotJumpOntoTheEnemy()
        {
            var enemy=content.Factory.CreateEntity("MarlbackScrabbler");zone.AddEntity(enemy,12,10);
            zone.AddEntity(content.Factory.CreateEntity("StoneWall"),11,10);int hp=enemy.GetStatValue("Hitpoints");
            Assert.False(Cast(12,10));Assert.AreEqual((10,10),zone.GetEntityPosition(player));Assert.AreEqual(hp,enemy.GetStatValue("Hitpoints"));
        }
        [Test] public void EmptyLandingStillJumpsOverAnOrdinaryWall()
        {zone.AddEntity(content.Factory.CreateEntity("StoneWall"),11,10);Assert.True(Cast(12,10));Assert.AreEqual((12,10),zone.GetEntityPosition(player));}
        public sealed class LandingProbe : Part
        {
            public override string Name=>"VaultLandingProbe";public Action Run;
            public override bool HandleEvent(GameEvent e){if(e.ID=="AfterMove")Run();return true;}
        }
        [TestCase("death")] [TestCase("displace")] [TestCase("target-moved")]
        public void LandingCallbacksCannotCauseStaleRemoteAttacks(string change)
        {
            var enemy=content.Factory.CreateEntity("MarlbackScrabbler");zone.AddEntity(enemy,12,10);int hp=enemy.GetStatValue("Hitpoints");
            player.AddPart(new LandingProbe{Run=()=>{if(change=="death")player.GetStat("Hitpoints").BaseValue=0;else if(change=="displace")zone.MoveEntity(player,8,10);else zone.MoveEntity(enemy,15,10);}});
            Assert.True(Cast(12,10));Assert.AreEqual(hp,enemy.GetStatValue("Hitpoints"));
        }
        [TestCase(false)] [TestCase(true)]
        public void ArchiveSealBlocksCursorVaultButOrdinaryWallDoesNot(bool sealedArchive)
        {
            var wall = content.Factory.CreateEntity("StoneWall");
            if (sealedArchive) wall.AddPart(new SealedLibraryBarrierPart());
            zone.AddEntity(wall, 11, 10);
            Assert.AreEqual(!sealedArchive, Cast(12, 10));
            Assert.AreEqual(sealedArchive ? (10,10) : (12,10), zone.GetEntityPosition(player));
        }
        [Test]
        public void AnotherCreatureAtTheLandingPreventsTheAttack()
        {
            var target = content.Factory.CreateEntity("MarlbackScrabbler"); zone.AddEntity(target,12,10);
            zone.AddEntity(content.Factory.CreateEntity("MarlbackScrabbler"),11,10);
            int hp = target.GetStatValue("Hitpoints");
            Assert.False(Cast(12,10)); Assert.AreEqual(hp,target.GetStatValue("Hitpoints"));
            Assert.AreEqual((10,10),zone.GetEntityPosition(player));
        }
        [TestCase(false)] [TestCase(true)]
        public void OffsetStrikeOnlyChecksBarriersOnItsActualLanding(bool onLanding)
        {
            var target=content.Factory.CreateEntity("MarlbackScrabbler"); zone.AddEntity(target,12,11);
            var barrier=content.Factory.CreateEntity("StoneWall"); barrier.AddPart(new SealedLibraryBarrierPart());
            zone.AddEntity(barrier,11,onLanding?10:11);
            Assert.AreEqual(!onLanding,Cast(12,11));
            Assert.AreEqual(onLanding?(10,10):(11,10),zone.GetEntityPosition(player));
        }
        [Test]
        public void ForeignZoneCellIsRejectedWithoutMovement()
        {
            Assert.False(new Acrobatics_Vault().OnCommand(new SkillEventContext {
                Attacker=player, Zone=zone, TargetCell=new Zone().GetCell(12,10), Rng=new Random(64) }));
            Assert.AreEqual((10,10),zone.GetEntityPosition(player));
        }

        [Test] public void PreviewHasNoMovementDamageOrRngSideEffects()
        {
            var enemy=content.Factory.CreateEntity("MarlbackScrabbler");zone.AddEntity(enemy,12,10);int hp=enemy.GetStatValue("Hitpoints");
            Assert.True(VaultTargeting.TryPlan(player,zone,12,10,out var land,out var target,out var reason));
            Assert.AreSame(enemy,target);Assert.AreSame(zone.GetCell(11,10),land);Assert.Null(reason);
            Assert.AreEqual((10,10),zone.GetEntityPosition(player));Assert.AreEqual(hp,enemy.GetStatValue("Hitpoints"));Assert.False(player.HasEffect<VaultPoiseEffect>());
        }
    }
}
