using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Audit guard tests only. These do not substitute for actual native
    /// command execution, real FOV, image inspection or the four measured profiles.</summary>
    public sealed class StarterSpell3DNativeDirectionTests
    {
        static MethodInfo Method(string name)
        {
            var method=typeof(StarterSpell3DNativeAudit).GetMethod(name,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
            Assert.NotNull(method,"The directional audit must provide the actual guarded lane/receipt implementation.");
            return method;
        }
        static bool Lane(Zone zone,Entity actor,int x,int y,int dx,int dy)
            =>(bool)Method("CanUseDirectionalLane").Invoke(null,new object[]{zone,actor,x,y,dx,dy});
        static bool Evidence(Receipt receipt)
        {
            var actual=JsonUtility.FromJson<StarterSpell3DNativeAudit.Report>(JsonUtility.ToJson(receipt));
            return (bool)Method("HasDirectionalEvidence").Invoke(null,new object[]{actual.casts});
        }

        [TestCase(0,-1)] [TestCase(0,1)] [TestCase(1,-1)]
        public void ClearDirectionalCorridorRejectsAnActualNonSolidSideConeRecipientWithoutDeletingIt(int dx,int dy)
        {
            var zone=new Zone();var player=StarterSpell3DCaptureFixture.Actor(zone,"player",40,12);
            Assert.IsTrue(Lane(zone,player,40,12,dx,dy));
            int x=40+2*dx-dy,y=12+2*dy+dx;
            var unrelated=StarterSpell3DCaptureFixture.Actor(zone,"authored-neighbor",x,y);
            Assert.IsFalse(zone.GetCell(x,y).IsSolid(),"A solidity-only predicate would miss this real cone recipient.");
            Assert.IsTrue(zone.GetCell(x,y).Occupants.Contains(unrelated));
            var membership=zone.GetAllEntities().ToArray();string ground=zone.TileState.ToSaveString();
            Assert.IsFalse(Lane(zone,player,40,12,dx,dy));
            CollectionAssert.AreEquivalent(membership,zone.GetAllEntities());Assert.AreEqual((x,y),zone.GetEntityPosition(unrelated));
            Assert.AreEqual(ground,zone.TileState.ToSaveString(),"Searching must never clear ground to manufacture a corridor.");
            Assert.IsTrue(zone.RemoveEntity(unrelated));Assert.IsTrue(Lane(zone,player,40,12,dx,dy));
        }

        [TestCase(40,3,0,-1)] [TestCase(40,21,0,1)] [TestCase(77,3,1,-1)]
        public void EdgeClippedDirectionalLaneIsRejectedButTheSameDirectionWorksInTheInterior(int x,int y,int dx,int dy)
        {
            var zone=new Zone();var player=StarterSpell3DCaptureFixture.Actor(zone,"player",40,12);
            Assert.IsTrue(Lane(zone,player,40,12,dx,dy));
            Assert.IsFalse(Lane(zone,player,x,y,dx,dy),"A truncated ray is not a full directional visual fixture.");
            Assert.AreEqual((40,12),zone.GetEntityPosition(player));
        }

        [Test] public void AuthoredCropOutsideTheRayStillProtectsTheExistingRainIsolationRadius()
        {
            var zone=new Zone();var player=StarterSpell3DCaptureFixture.Actor(zone,"player",40,12);
            Assert.IsTrue(Lane(zone,player,40,12,0,-1));
            var crop=new Entity{ID="authored-crop"};crop.AddPart(new CropPart());
            Assert.IsTrue(zone.AddEntity(crop,37,15));
            Assert.IsFalse(zone.GetCell(37,15).IsSolid());
            Assert.IsFalse(Lane(zone,player,40,12,0,-1));
            Assert.AreEqual((37,15),zone.GetEntityPosition(crop));
            Assert.IsTrue(zone.RemoveEntity(crop));Assert.IsTrue(Lane(zone,player,40,12,0,-1));
        }

        [Test] public void NorthLaneRestoresItsOldGroundIncludingChargeBeyondAnEastRectangleAndPreservesOutsideWritingAndEntities()
        {
            var zone=new Zone();var player=StarterSpell3DCaptureFixture.Actor(zone,"player",40,12);
            var authored=StarterSpell3DCaptureFixture.Prop(zone,"authored-prop",41,8);
            zone.TileState.WriteCoating(40,8,"water",6);zone.TileState.AddCold(40,8,3);
            var baseline=new ZoneTileState();baseline.LoadFromString(zone.TileState.ToSaveString());
            string original=JsonUtility.ToJson(zone.TileState.Get(40,8));
            zone.TileState.AddCharge(40,8,9);zone.TileState.AddCharge(40,7,5);
            zone.TileState.AddHeat(60,12,23);string outside=JsonUtility.ToJson(zone.TileState.Get(60,12));
            Assert.AreNotEqual(original,JsonUtility.ToJson(zone.TileState.Get(40,8)));
            Assert.IsNull(baseline.Get(40,7));Assert.NotNull(zone.TileState.Get(40,7));
            var bounds=(RectInt)Method("FixtureGroundBounds").Invoke(null,new object[]{40,12,0,-1});
            Method("RestoreFixtureGround").Invoke(null,new object[]{zone,baseline,bounds});
            Assert.AreEqual(original,JsonUtility.ToJson(zone.TileState.Get(40,8)));
            Assert.IsNull(zone.TileState.Get(40,7));Assert.AreEqual(outside,JsonUtility.ToJson(zone.TileState.Get(60,12)));
            Assert.AreEqual((41,8),zone.GetEntityPosition(authored));Assert.AreEqual((40,12),zone.GetEntityPosition(player));
            var east=(RectInt)Method("FixtureGroundBounds").Invoke(null,new object[]{40,12,1,0});
            Assert.AreEqual(new RectInt(37,9,9,7),east,"The original east showcase/profile restoration extent remains exact.");
        }

        [Test] public void CompleteFifteenCastDirectionalMatrixAcceptsMetadataWithoutSubstitutingForNativeFiles()
            =>Assert.IsTrue(Evidence(Good()));

        [TestCase("missing-direction")] [TestCase("east-relabelled")]
        [TestCase("hidden-recipient")] [TestCase("surge-not-pushed")]
        [TestCase("jet-center-only")] [TestCase("rime-neutral-only")]
        public void PositiveLookingDirectionalLabelsCannotReplaceTheActualOutcomeEvidence(string fault)
        {
            var report=Good();Assert.IsTrue(Evidence(report),"The complete paired fixture must first be accepted.");
            if(fault=="missing-direction")report.casts.RemoveAll(r=>r.mode=="direction-north");
            else
            {
                var row=report.casts.First(r=>r.mode=="direction-northeast"&&r.spell==(fault=="surge-not-pushed"?"Galvanism_GroundSurge":fault=="jet-center-only"?"Hydromancy_JetBlast":fault=="rime-neutral-only"?"Cryomancy_RimeGrip":"Pyromancy_EmberSpit"));
                if(fault=="east-relabelled")
                {
                    row.directionX=1;row.directionY=0;row.intendedX=43;row.intendedY=12;row.finalX=43;row.finalY=12;
                    row.targetCells=new[]{"43,12->43,12"};row.path=new[]{"41,12","42,12","43,12"};
                }
                if(fault=="hidden-recipient")row.targetVisible=false;
                if(fault=="surge-not-pushed"){row.finalX=row.intendedX;row.finalY=row.intendedY;row.targetCells=new[]{row.intendedX+","+row.intendedY+"->"+row.finalX+","+row.finalY};}
                if(fault=="jet-center-only")row.affected=row.path.ToArray();
                if(fault=="rime-neutral-only")row.observedConditionalMeshes=Array.Empty<string>();
            }
            Assert.IsFalse(Evidence(report),"A native matrix must fail this concrete contradictory outcome despite all pass labels staying true.");
            Assert.IsTrue(Evidence(Good()),"Corrected evidence remains acceptable.");
        }

        static Receipt Good()
        {
            var result=new Receipt();
            foreach(var d in new[]{(name:"north",dx:0,dy:-1),(name:"south",dx:0,dy:1),(name:"northeast",dx:1,dy:-1)})
                foreach(string spell in new[]{"Pyromancy_EmberSpit","Hydromancy_JetBlast","Galvanism_GroundSurge","Cryomancy_RimeGrip","Spellcraft_Calm"})
                {
                    bool jet=spell=="Hydromancy_JetBlast",surge=spell=="Galvanism_GroundSurge",rime=spell=="Cryomancy_RimeGrip",calm=spell=="Spellcraft_Calm";
                    int distance=jet?2:3,push=jet||surge?1:0,tx=40+distance*d.dx,ty=12+distance*d.dy,fx=tx+push*d.dx,fy=ty+push*d.dy;
                    result.casts.Add(new Row{spell=spell,nativeEntry=spell,mode="direction-"+d.name,zoneId="Overworld.3.7.0",fxMode="Full",
                        sourceX=40,sourceY=12,directionX=d.dx,directionY=d.dy,intendedX=tx,intendedY=ty,finalX=fx,finalY=fy,
                        targets=1,damage=calm?0:3,cooldown=10,peakMeshes=20,gestureSamples=10,gesturePeakDegrees=15,seconds=1.21,
                        sourceVisible=true,targetVisible=true,pass=true,resultStable=true,groundFixtureReset=true,sawGesture=true,gestureRestored=true,
                        targetCells=new[]{tx+","+ty+"->"+fx+","+fy},copiedTargetIds=new[]{"owned-recipient"},
                        path=Enumerable.Range(1,jet?2:surge?4:3).Select(n=>(40+n*d.dx)+","+(12+n*d.dy)).ToArray(),
                        affected=jet?new[]{(40+d.dx)+","+(12+d.dy)}.Concat(Enumerable.Range(-1,3).Select(off=>(40+2*d.dx-d.dy*off)+","+(12+2*d.dy+d.dx*off))).Concat(new[]{fx+","+fy}).ToArray():Array.Empty<string>(),
                        applied=jet?new[]{nameof(WetEffect)}:rime?new[]{nameof(FrozenEffect)}:calm?new[]{"Pacified"}:Array.Empty<string>(),
                        observedConditionalMeshes=rime||calm?new[]{"actual-condition-piece"}:Array.Empty<string>(),
                        frames=Enumerable.Range(0,4).Select(i=>new StarterSpell3DNativeAudit.CaptureFrame{path="metadata-only-"+d.name+"-"+spell+"-"+i,wallSeconds=i*.1,meshes=i>0?20:0}).ToList()});
                }
            return result;
        }
        [Serializable] sealed class Receipt{public List<Row> casts=new List<Row>();}
        [Serializable] sealed class Row
        {
            public string spell,nativeEntry,mode,zoneId,fxMode;
            public int sourceX,sourceY,directionX,directionY,intendedX,intendedY,finalX,finalY,targets,damage,cooldown,peakMeshes,gestureSamples;
            public float gesturePeakDegrees;public double seconds;
            public bool sourceVisible,targetVisible,pass,resultStable,groundFixtureReset,sawGesture,gestureRestored;
            public string[] targetCells,copiedTargetIds,path,affected,applied,observedConditionalMeshes;
            public List<StarterSpell3DNativeAudit.CaptureFrame> frames;
        }
    }
}
