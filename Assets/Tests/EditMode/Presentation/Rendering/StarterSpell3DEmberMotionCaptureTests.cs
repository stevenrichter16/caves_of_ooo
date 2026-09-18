using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>New optional media-recorder contracts. Metadata controls do not
    /// replace the native same-frame pixel calibration or observed flight movie.</summary>
    public sealed class StarterSpell3DEmberMotionCaptureTests
    {
        const string Spell="Pyromancy_EmberSpit";
        static Type CaptureType
        {
            get
            {
                var type=typeof(StarterSpell3DNativeAudit).Assembly.GetType("CavesOfOoo.Scenarios.Custom.StarterSpell3DEmberMotionCapture");
                Assert.NotNull(type,"The optional Ember recorder must exist; ordinary native gates alone cannot prove its short flight was sampled.");
                return type;
            }
        }
        static object Call(string name,params object[] args)
        {
            var method=CaptureType.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
            Assert.NotNull(method,"Recorder contract missing: "+name);
            return method.Invoke(null,args);
        }
        static bool Evidence(Receipt receipt)=>(bool)Call("HasMotionEvidence",JsonUtility.ToJson(receipt));
        static bool Requested(string[] args)=>(bool)Call("IsRequested",new object[]{args});
        static bool Eligible(bool enabled,string spell,string mode,int phase,bool originalComplete)
            =>(bool)Call("ShouldRecord",enabled,spell,mode,phase,originalComplete);

        [Test] public void OnlyTheExactExplicitOptionRequestsMotionCapture()
        {
            Assert.IsTrue(Requested(new[]{"Unity","-emberMotionCapture"}));
            Assert.IsFalse(Requested(new[]{"Unity","-executeMethod","normal"}));
            Assert.IsFalse(Requested(new[]{"-emberMotionCapture=false"}));
            Assert.IsFalse(Requested(null));
        }

        [TestCase("disabled")] [TestCase("other-spell")] [TestCase("profile")]
        [TestCase("original-unfinished")] [TestCase("ordinary-showcase")] [TestCase("direction-matrix")]
        public void RecorderCannotChangeOriginalCastsOrEnterAProfile(string mutation)
        {
            Assert.IsTrue(Eligible(true,Spell,"ember-motion-east",-1,true));
            Assert.IsTrue(Eligible(true,Spell,"ember-motion-northeast",-1,true));
            bool enabled=true,complete=true;string spell=Spell,mode="ember-motion-east";int phase=-1;
            switch(mutation)
            {
                case "disabled":enabled=false;break;
                case "other-spell":spell="Pyromancy_FlamingHands";break;
                case "profile":phase=0;break;
                case "original-unfinished":complete=false;break;
                case "ordinary-showcase":mode="showcase";break;
                case "direction-matrix":mode="direction-northeast";break;
            }
            Assert.IsFalse(Eligible(enabled,spell,mode,phase,complete));
        }

        [Test] public void TwoActualDirectionsWithMovingPrecontactHeadTailThenImpactAndClearAreRequired()
            =>Assert.IsTrue(Evidence(Good()));

        [TestCase("contact-only")] [TestCase("two-flight-samples")] [TestCase("no-tail")]
        [TestCase("stationary-head")] [TestCase("duplicate-unity-frame")] [TestCase("duplicate-wall-time")]
        [TestCase("same-image-flight")] [TestCase("missing-impact")] [TestCase("missing-clear")]
        [TestCase("relabeled-east")] [TestCase("missing-northeast")] [TestCase("foreign-spell")]
        [TestCase("early-encoding")] [TestCase("pending-at-encode")] [TestCase("active-at-encode")]
        [TestCase("unsettled-gesture")] [TestCase("readback-error")] [TestCase("overflow")]
        [TestCase("larger-memory-cap")] [TestCase("larger-ring")]
        [TestCase("wrong-color")] [TestCase("vacuous-flip-counter")] [TestCase("vacuous-gamma-counter")]
        [TestCase("upscaled-view")] [TestCase("stale-run")] [TestCase("completion-before-request")]
        public void IncompleteOrMisleadingMotionEvidenceIsRejectedAgainstTheSameValidFixture(string mutation)
        {
            Assert.IsTrue(Evidence(Good()),"Positive fixture must prove the exact metadata gate before its single mutation.");
            var receipt=Good();var row=receipt.casts[0];var first=row.frames[0];
            switch(mutation)
            {
                case "contact-only":foreach(var frame in row.frames){frame.nativeAge=.31;frame.studyFrame=31;}break;
                case "two-flight-samples":row.frames=row.frames.Skip(1).ToArray();row.requested=row.completed=row.encoded=row.frames.Length;break;
                case "no-tail":foreach(var frame in row.frames)frame.trails=0;break;
                case "stationary-head":foreach(var frame in row.frames)frame.headForward=.4;break;
                case "duplicate-unity-frame":row.frames[1].unityFrame=first.unityFrame;break;
                case "duplicate-wall-time":row.frames[1].wallSeconds=first.wallSeconds;break;
                case "same-image-flight":foreach(var frame in row.frames)frame.rgbSha256=Hash(4);break;
                case "missing-impact":foreach(var frame in row.frames)frame.impacts=0;break;
                case "missing-clear":row.frames=row.frames.Take(4).ToArray();row.requested=row.completed=row.encoded=4;break;
                case "relabeled-east":receipt.casts[1].directionY=0;break;
                case "missing-northeast":receipt.casts=new[]{row};break;
                case "foreign-spell":row.spell="Pyromancy_FlamingHands";break;
                case "early-encoding":row.encodingStarted=.4;break;
                case "pending-at-encode":row.pendingAtEncode=1;break;
                case "active-at-encode":row.activeAtEncode=1;break;
                case "unsettled-gesture":row.gestureIdleAtEncode=false;break;
                case "readback-error":row.readbackErrors=1;break;
                case "overflow":row.overflow=1;break;
                case "larger-memory-cap":receipt.maximumSamples=33;break;
                case "larger-ring":receipt.ringTextures=4;break;
                case "wrong-color":receipt.calibration.bufferedRgbSha256=Hash(8);break;
                case "vacuous-flip-counter":receipt.calibration.flippedRgbSha256=receipt.calibration.referenceRgbSha256;break;
                case "vacuous-gamma-counter":receipt.calibration.doubleLinearRgbSha256=receipt.calibration.referenceRgbSha256;break;
                case "upscaled-view":receipt.width=3840;break;
                case "stale-run":row.runId=Guid.NewGuid().ToString("N");break;
                case "completion-before-request":first.completedWallSeconds=first.wallSeconds-.01;break;
            }
            Assert.IsFalse(Evidence(receipt),mutation);
        }

        [Test] public void RealWalltimeGapsAreRetainedButDoNotBecomeSyntheticFlightFrames()
        {
            var receipt=Good();var row=receipt.casts[0];
            for(int i=2;i<row.frames.Length;i++){row.frames[i].wallSeconds+=.5;row.frames[i].completedWallSeconds+=.5;}
            row.encodingStarted+=.5;row.seconds+=.5;
            Assert.IsTrue(Evidence(receipt),"A recorded gap is not itself falsified evidence; the actual distinct precontact samples still exist.");
            row.frames[2].nativeAge=.31;row.frames[2].studyFrame=31;
            Assert.IsFalse(Evidence(receipt),"A gap that leaves only two true flight samples cannot be repaired with hold/interpolation.");
        }

        static string Hash(int n)=>n.ToString("x").PadLeft(64,'0');
        static Receipt Good()
        {
            string run=Guid.NewGuid().ToString("N");
            return new Receipt{runId=run,enabled=true,width=1920,height=1080,maximumSamples=32,ringTextures=3,
                calibration=new Calibration{width=1920,height=1080,referenceRgbSha256=Hash(1),bufferedRgbSha256=Hash(1),flippedRgbSha256=Hash(2),doubleLinearRgbSha256=Hash(3)},
                casts=new[]{GoodCast(run,"east",1,0),GoodCast(run,"northeast",1,-1)}};
        }
        static Cast GoodCast(string run,string direction,int dx,int dy)
        {
            return new Cast{runId=run,spell=Spell,mode="ember-motion-"+direction,directionX=dx,directionY=dy,sourceX=40,sourceY=12,intendedX=40+3*dx,intendedY=12+3*dy,
                fxMode="Full",releaseSeconds=.22,contactSeconds=.295,clearSeconds=.675,releaseFrame=22,contactFrame=29.5,
                requested=5,completed=5,encoded=5,encodingStarted=1.21,seconds=1.2,gestureIdleAtEncode=true,
                frames=new[]{Sample(100,.225,22.5,.4,2,8,0,5),Sample(102,.24,24,1.2,2,8,0,6),Sample(106,.27,27,2.4,2,8,0,7),Sample(111,.31,31,3,0,0,5,8),Sample(301,1.2,110,3,0,0,0,9)}};
        }
        static Frame Sample(int frame,double age,double study,double forward,int heads,int trails,int impacts,int hash)
            =>new Frame{unityFrame=frame,completedFrame=frame+2,wallSeconds=age,completedWallSeconds=age+.006,nativeAge=age,studyFrame=study,
                headForward=forward,heads=heads,trails=trails,impacts=impacts,meshes=heads+trails+impacts,rgbSha256=Hash(hash)};

        [Serializable] public sealed class Receipt
        {public string runId;public bool enabled;public int width,height,maximumSamples,ringTextures;public Calibration calibration;public Cast[] casts;}
        [Serializable] public sealed class Calibration
        {public int width,height;public string referenceRgbSha256,bufferedRgbSha256,flippedRgbSha256,doubleLinearRgbSha256;}
        [Serializable] public sealed class Cast
        {public string runId,spell,mode,fxMode;public int directionX,directionY,sourceX,sourceY,intendedX,intendedY,requested,completed,encoded,pendingAtEncode,activeAtEncode,readbackErrors,overflow;
            public double releaseSeconds,contactSeconds,clearSeconds,releaseFrame,contactFrame,encodingStarted,seconds;public bool gestureIdleAtEncode;public Frame[] frames;}
        [Serializable] public sealed class Frame
        {public int unityFrame,completedFrame,heads,trails,impacts,meshes;public double wallSeconds,completedWallSeconds,nativeAge,studyFrame,headForward;public string rgbSha256;}
    }
}
