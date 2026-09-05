using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FellingW6CloseoutTests
    {
        private EntityFactory _factory;
        private NarrativeStatePart _oldNarrative;
        [SetUp] public void Setup()
        {
            _factory=new EntityFactory();_factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            Diag.ResetAll();MessageLog.Clear();
            _oldNarrative=NarrativeStatePart.Current; NarrativeStatePart.Current=new NarrativeStatePart();
        }
        [TearDown] public void Clean(){Diag.ResetAll(); NarrativeStatePart.Current=_oldNarrative;}
        private void Attack(Entity actor,string dice,bool bleed)
        {
            var natural=actor.GetPart<MeleeWeaponPart>();natural.HitBonus=100;natural.PenBonus=20;
            var target=new Entity();target.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=100000,Max=100000};
            target.Statistics["DV"]=new Stat{Name="DV",BaseValue=-100};
            var z=new Zone();z.AddEntity(actor,10,10);z.AddEntity(target,11,10);
            for(int seed=0;seed<30;seed++)CombatSystem.PerformMeleeAttack(actor,target,z,new System.Random(seed));
            var rolls=DiagQuery.Apply(new DiagQuery.Filter{Category="damage",Kind="DamageRoll",Limit=100}).Records;
            Assert.Greater(rolls.Count,0,"real strikes must penetrate");
            foreach(var r in rolls)StringAssert.Contains("\"damageDice\":\""+dice+"\"",r.PayloadJson);
            Assert.AreEqual(bleed,target.HasEffect<BleedingEffect>());
        }
        [TestCase("SariSnake","1d6",true,false)] [TestCase("SariSnake","1d6",true,true)]
        [TestCase("Wardline","1d4",false,false)] [TestCase("Wardline","1d4",false,true)]
        public void AuthoredSnakeAttackSurvivesBodyMaintenance(string bp,string dice,bool bleed,bool maintain)
        {
            var actor=_factory.CreateEntity(bp);if(maintain)actor.GetPart<Body>().UpdateBodyParts();
            Attack(actor,dice,bleed);
            Assert.IsFalse(actor.GetPart<Body>().GetParts().Any(p=>p.Type=="Hand"));
            Assert.AreEqual(bp=="Wardline",actor.GetPart<BrainPart>().Passive);
        }
        [TestCase("SariSnake")] [TestCase("Wardline")]
        public void RemovingNaturalOptInKeepsOrdinaryFallbackWithoutSnakeBleed(string bp)
        {
            var actor=_factory.CreateEntity(bp);actor.Tags.Remove("BodyNaturalAttack");Attack(actor,"1d2",false);
        }
        [Test] public void AnEquippedHandWeaponStillOverridesSnakeNaturalDamageAndBleed()
        {
            var actor=_factory.CreateEntity("SariSnake");var root=AnatomyFactory.CreateSimple();var hand=AnatomyFactory.CreatePart("Hand");root.AddPart(hand);
            var weapon=new Entity();weapon.AddPart(new MeleeWeaponPart{BaseDamage="1d7",HitBonus=100,PenBonus=20});
            hand._Equipped=weapon;hand.FirstSlotForEquipped=true;hand.Primary=true;actor.GetPart<Body>().SetBody(root);
            Attack(actor,"1d7",false);
        }
        [TestCase("SprayPool",true)] [TestCase("WaterPuddle",false)] [TestCase("StoneFloor",false)]
        public void CascadeIndicatorRequiresActualSprayInItsOwnCell(string floor,bool allowed)
        {
            var z=new Zone();z.AddEntity(_factory.CreateEntity(floor),10,10);
            Assert.AreEqual(allowed,StumpFaunaHabitat.Allows("CascadeFather",z.GetCell(10,10)));
            Assert.IsFalse(StumpFaunaHabitat.Allows("CascadeFather",z.GetCell(11,10)),"nearby spray is not the same microhabitat");
            Assert.IsTrue(StumpFaunaHabitat.Allows("YellowfootWayfarer",z.GetCell(11,10)));
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualPopulationUsesRemainingSprayAndSkipsMissingHabitat(bool keepSpray)
        {
            var z=new Zone();var spray=_factory.CreateEntity("SprayPool");z.AddEntity(spray,10,10);
            if(!keepSpray)z.RemoveEntity(spray);
            var table=new PopulationTable{Name="CascadeControl"};table.Entries.Add(new PopulationEntry{BlueprintName="CascadeFather",MinCount=1,MaxCount=1,Weight=1});
            Assert.IsTrue(new PopulationBuilder(table){HabitatFilter=StumpFaunaHabitat.Allows}.BuildZone(z,_factory,new System.Random(3)));
            var frogs=z.GetAllEntities().Where(e=>e.BlueprintName=="CascadeFather").ToList();Assert.AreEqual(keepSpray?1:0,frogs.Count);
            if(keepSpray)Assert.AreEqual((10,10),z.GetEntityPosition(frogs[0]));
        }
        [TestCase(false)] [TestCase(true)] public void GeneratedFoothillsPlaceCascadeFathersOnlyInHealthySpray(bool damaged)
        {
            NarrativeStatePart.Current.SetFact("EcologyDamaged",damaged?1:0);
            var m=new OverworldZoneManager(_factory,67);int found=0;
            for(int x=0;x<WorldMap.Width;x++)for(int y=0;y<WorldMap.Height;y++)
            {
                if(StumpBands.BandAt(x,y)!=StumpBand.Foothills)continue;
                var z=m.GetZone($"Overworld.{x}.{y}.0");
                foreach(var frog in z.GetAllEntities().Where(e=>e.BlueprintName=="CascadeFather"))
                {found++;Assert.IsTrue(StumpFaunaHabitat.Contains(z.GetEntityCell(frog),"SprayPool"));}
            }
            if(damaged) Assert.AreEqual(0,found);
            else Assert.Greater(found,0,"the habitat filter must not erase the species from the healthy world");
        }
    }
}
