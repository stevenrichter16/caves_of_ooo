using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class GrovelandsCompositionTests
    {
        public static EntityFactory Factory()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            return f;
        }

        [Test]
        public void PlanIsDeterministicButWorldSeedsChangeItsComposition()
        {
            var a = GrovelandsCompositionPlan.Create("Overworld.2.6.0", 64);
            var b = GrovelandsCompositionPlan.Create("Overworld.2.6.0", 64);
            var c = GrovelandsCompositionPlan.Create("Overworld.2.6.0", 65);
            Assert.AreEqual(a.Signature(), b.Signature());
            Assert.AreNotEqual(a.Signature(), c.Signature());
            Assert.AreEqual(Formation.CompostingField, a.Formation);
        }

        [TestCase(0)] [TestCase(64)] [TestCase(-123)]
        public void NeighborApproachesAgreeAndAllAreReserved(int seed)
        {
            var a = GrovelandsCompositionPlan.Create("Overworld.2.6.0", seed);
            var east = GrovelandsCompositionPlan.Create("Overworld.3.6.0", seed);
            var south = GrovelandsCompositionPlan.Create("Overworld.2.7.0", seed);
            Assert.AreEqual(a.EastY, east.WestY);
            Assert.AreEqual(a.SouthX, south.NorthX);
            Assert.IsTrue(a.IsApproach(0,a.WestY));
            Assert.IsTrue(a.IsApproach(Zone.Width-1,a.EastY));
            Assert.IsTrue(a.IsApproach(a.NorthX,0));
            Assert.IsTrue(a.IsApproach(a.SouthX,Zone.Height-1));
        }

        [TestCase(Formation.Grove,"GroveSeep")]
        [TestCase(Formation.TendrilFen,"ChoirTendril")]
        [TestCase(Formation.FruitingWall,"FruitingBody")]
        [TestCase(Formation.CompostingField,"CompostCache")]
        public void ActualTerrainAndFormationPreserveSignatureAndOpenApproaches(Formation formation,string signature)
        {
            var f=Factory();
            for(int seed=0;seed<12;seed++)
            {
                var zone=new Zone("Overworld.2.6.0");
                var terrain=new GrovelandsCompositionBuilder(seed) { FormationOverride=formation };
                Assert.IsTrue(terrain.BuildZone(zone,f,new System.Random(seed)));
                Assert.IsTrue(new GrovelandsFormationBuilder { Composition=terrain, Override=formation }.BuildZone(zone,f,new System.Random(seed)));
                var p=terrain.Plan;
                Assert.IsTrue(zone.GetAllEntities().Any(e=>e.BlueprintName==signature),formation+" seed="+seed);
                Assert.IsFalse(zone.GetCell(0,p.WestY).BlocksMovement());
                Assert.IsFalse(zone.GetCell(Zone.Width-1,p.EastY).BlocksMovement());
                Assert.IsFalse(zone.GetCell(p.NorthX,0).BlocksMovement());
                Assert.IsFalse(zone.GetCell(p.SouthX,Zone.Height-1).BlocksMovement());
                int caches=zone.GetAllEntities().Count(e=>e.BlueprintName=="CompostCache");
                if(formation==Formation.CompostingField) Assert.That(caches,Is.InRange(2,4));
                else Assert.AreEqual(0,caches);
                Assert.AreEqual(Zone.Width*Zone.Height,zone.GetAllEntities().Count(e=>e.BlueprintName=="Grass"));
                Assert.Less(zone.GetAllEntities().Count(e=>e.BlueprintName=="Tree"),100,"Trees must form sparse masses, not fill every opening.");
            }
        }
    }
}
