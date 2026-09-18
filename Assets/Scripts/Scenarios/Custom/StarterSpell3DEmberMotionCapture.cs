using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Explicit Ember media appendix only. Copies actual final GameView
    /// frames; never renders another camera, samples animation, or changes clocks.
    /// All buffers are prepared after the original performance workload.</summary>
    public sealed class StarterSpell3DEmberMotionCapture : IDisposable
    {
        public const int Width=1920, Height=1080, MaximumSamples=32, RingTextures=3;
        public const int BytesPerFrame=Width*Height*4;
        const string Spell="Pyromancy_EmberSpit";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        readonly NativeArray<byte>[] buffers=new NativeArray<byte>[MaximumSamples];
        readonly RenderTexture[] textures=new RenderTexture[RingTextures];
        readonly bool[] busy=new bool[RingTextures];
        readonly int[] ringForSlot=new int[MaximumSamples];
        readonly Action<AsyncGPUReadbackRequest>[] callbacks=new Action<AsyncGPUReadbackRequest>[MaximumSamples];
        readonly Frame[] samples=new Frame[MaximumSamples];
        readonly string directory,stem;
        readonly Receipt receipt;
        NativeSpellFxRenderer native;
        NativeSpellFxEntry entry;
        MeshFilter[] filters; MeshRenderer[] renderers;
        Dictionary<Mesh,NativeSpellFxRole> roles;
        Mesh trackedHead;
        Vector3 source,forward;
        FieldInfo castsField,ageField,contactField,durationField;
        MethodInfo studyFrameMethod;
        Cast row;
        int requested,pending,finished,readbackErrors,overflow;
        double start,lastCaptureAge=double.NegativeInfinity;
        bool clearCaptured,disposed,releaseRequested,flipped;
        public Receipt Evidence=>receipt;
        public string ReportPath=>Path.Combine(directory,stem+"-ember-motion.json");
        public int PendingRequests=>pending;
        static double Now=>Time.realtimeSinceStartupAsDouble;

        public static bool IsRequested(string[] args)=>args!=null&&args.Contains("-emberMotionCapture",StringComparer.Ordinal);
        public static bool ShouldRecord(bool requested,string spell,string mode,int profilePhase,bool originalComplete)
            =>requested&&originalComplete&&profilePhase<0&&spell==Spell&&(mode=="ember-motion-east"||mode=="ember-motion-northeast");

        public StarterSpell3DEmberMotionCapture(string runId,string directory)
        {
            Require(Guid.TryParseExact(runId,"N",out var parsed)&&parsed!=Guid.Empty,"Fresh motion run ID required.");
            Require(Screen.width==Width&&Screen.height==Height,"Motion capture requires unchanged native1080p GameView.");
            Require(SystemInfo.supportsAsyncGPUReadback,"GPU readback unsupported; no synchronous motion fallback.");
            this.directory=directory;stem="SSN-"+runId;
            receipt=new Receipt{runId=runId,enabled=true,width=Width,height=Height,maximumSamples=MaximumSamples,ringTextures=RingTextures,
                allocatedRawBytes=(long)MaximumSamples*BytesPerFrame,allocatedRenderTextureBytes=(long)RingTextures*BytesPerFrame,
                bounds="Optional two-command Ember media appendix after unchanged native acceptance/profiles. Actual end-of-frame1080p pixels and variable wall timestamps, at most120 samples/s near flight and30 during settling. No interpolated motion, camera/render-time change, or PNG encoding during the cast. Raw GPU readback may still affect frame timing; this is not a performance benchmark."};
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            receipt.libraryAssetSha256=FileHash(Path.Combine(project,"Assets/Resources/SpellFx3D/Library.asset"));
            receipt.runtimeExportSha256=FileHash(Path.Combine(project,"ArtSource/StarterSpell3D/runtime/starter_spell_library.json"));
            var timer=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                for(int i=0;i<MaximumSamples;i++)
                {
                    buffers[i]=new NativeArray<byte>(BytesPerFrame,Allocator.Persistent,NativeArrayOptions.UninitializedMemory);
                    samples[i]=new Frame();int slot=i;callbacks[i]=request=>ReadbackComplete(slot,request);
                }
                for(int i=0;i<RingTextures;i++)
                {
                    textures[i]=new RenderTexture(Width,Height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB)
                        {name="Ember motion readback "+i,hideFlags=HideFlags.HideAndDontSave,useMipMap=false,autoGenerateMips=false,antiAliasing=1};
                    Require(textures[i].Create(),"Motion readback texture allocation failed.");
                }
            }
            catch { Dispose(); throw; }
            receipt.preparationSeconds=timer.Elapsed.TotalSeconds;
        }

        /// <summary>Static same-frame calibration before either cast. A row flip
        /// is permitted only when exact RGB equality proves its necessity.</summary>
        public IEnumerator Calibrate()
        {
            Texture2D reference=null;
            try
            {
                yield return new WaitForEndOfFrame();
                double captured=Now;int frame=Time.frameCount;
                reference=ScreenCapture.CaptureScreenshotAsTexture();
                Require(reference!=null&&reference.width==Width&&reference.height==Height,"Normal GameView calibration screenshot absent.");
                samples[0].wallSeconds=0;samples[0].unityFrame=frame;start=captured;
                Request(0);
                double deadline=Now+5;
                while(pending>0){Require(Now<deadline,"Calibration GPU readback timed out.");yield return null;}
                Require(readbackErrors==0,"Calibration GPU readback failed.");
                var referencePixels=reference.GetPixels32();
                string normal=HashRgb(buffers[0],false,false),reverse=HashRgb(buffers[0],true,false);
                string expected=HashColors(referencePixels);
                Require(expected==normal||expected==reverse,"Buffered native capture changes RGB; calibration refuses correction/grading.");
                flipped=expected!=normal;
                receipt.calibration=new Calibration{width=Width,height=Height,unityFrame=frame,
                    referenceRgbSha256=expected,bufferedRgbSha256=flipped?reverse:normal,flippedRgbSha256=flipped?normal:reverse,
                    doubleLinearRgbSha256=HashRgb(buffers[0],flipped,true),verticalRowFlip=flipped,
                    referencePath=Path.Combine(directory,stem+"-ember-motion-calibration-reference.png"),
                    bufferedPath=Path.Combine(directory,stem+"-ember-motion-calibration-buffered.png")};
                Require(receipt.calibration.flippedRgbSha256!=expected&&receipt.calibration.doubleLinearRgbSha256!=expected,"Calibration counters are visually vacuous.");
                File.WriteAllBytes(receipt.calibration.referencePath,reference.EncodeToPNG());
                WritePng(0,receipt.calibration.bufferedPath);
                receipt.calibration.referencePngSha256=FileHash(receipt.calibration.referencePath);
                receipt.calibration.bufferedPngSha256=FileHash(receipt.calibration.bufferedPath);
            }
            finally { if(reference!=null)UnityEngine.Object.Destroy(reference); }
        }

        public void Begin(NativeSpellFxRenderer renderer,StarterSpell3DNativeAudit.CastRow command,double commandStart)
        {
            Require(!disposed&&!releaseRequested&&pending==0&&receipt.calibration!=null,"Calibrated unowned motion buffers required.");
            Require(command.spell==Spell&&command.fxMode=="Full","Only the approved Full Ember appendix can record.");
            native=renderer;entry=Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath).Find(Spell);
            Require(native!=null&&native.Root!=null&&entry!=null,"Actual native Ember renderer/library required.");
            requested=pending=finished=readbackErrors=overflow=0;clearCaptured=false;lastCaptureAge=double.NegativeInfinity;start=commandStart;
            source=Village3DProjection.CellCentre(command.sourceX,command.sourceY);
            forward=(Village3DProjection.CellCentre(command.intendedX,command.intendedY)-source).normalized;
            roles=new Dictionary<Mesh,NativeSpellFxRole>();
            foreach(var piece in entry.Pieces)if(!roles.ContainsKey(piece.Mesh))roles.Add(piece.Mesh,piece.Role);
            trackedHead=entry.Pieces.First(p=>p.Role==NativeSpellFxRole.ProjectileHead&&!p.Glow&&p.ReducedEssential).Mesh;
            filters=native.Root.GetComponentsInChildren<MeshFilter>(true);renderers=filters.Select(f=>f.GetComponent<MeshRenderer>()).ToArray();
            castsField=typeof(NativeSpellFxRenderer).GetField("_casts",Private);
            var castType=typeof(NativeSpellFxRenderer).GetNestedType("Cast",BindingFlags.NonPublic);
            ageField=castType.GetField("Age");contactField=castType.GetField("Contact");durationField=castType.GetField("Duration");
            studyFrameMethod=typeof(NativeSpellFxRenderer).GetMethod("StudyFrame",BindingFlags.Static|BindingFlags.NonPublic);
            Require(castsField!=null&&ageField!=null&&contactField!=null&&durationField!=null&&studyFrameMethod!=null,"Read-only native clock observation contract changed.");
            row=new Cast{runId=receipt.runId,spell=Spell,mode=command.mode,fxMode=command.fxMode,directionX=command.directionX,directionY=command.directionY,
                sourceX=command.sourceX,sourceY=command.sourceY,intendedX=command.intendedX,intendedY=command.intendedY,
                releaseSeconds=entry.ReleaseFrame/entry.SampleRate,releaseFrame=entry.ReleaseFrame,contactFrame=entry.StudyContactFrame};
            foreach(var sample in samples)sample.Reset();
        }

        object ActiveCast()
        {
            var casts=(IList)castsField.GetValue(native);
            Require(casts.Count<=1,"Motion appendix cannot mix native casts.");
            return casts.Count==0?null:casts[0];
        }

        public bool WantsFrame()
        {
            if(row==null||disposed||releaseRequested||clearCaptured)return false;
            var cast=ActiveCast();
            if(cast==null)return requested>0&&row.contactSeconds>0; // One genuine clear frame after acceptance.
            double age=(float)ageField.GetValue(cast),contact=(float)contactField.GetValue(cast);
            row.contactSeconds=contact;row.clearSeconds=(float)durationField.GetValue(cast);
            if(requested==0)return true; // One gather/preflight image.
            if(age<row.releaseSeconds-.04)return false;
            double interval=age<=contact+.02?1d/120:1d/30;
            return age-lastCaptureAge>=interval;
        }

        /// <summary>Called only after the driver's WaitForEndOfFrame. No encoding,
        /// filesystem work, new buffers, or synchronous GPU wait occurs here.</summary>
        public void CaptureEndOfFrame()
        {
            Require(row!=null&&!disposed&&!releaseRequested,"Motion cast must be armed.");
            var cast=ActiveCast();bool clear=cast==null;
            if(clear&&row.contactSeconds<=0)return;
            if(requested>=MaximumSamples||(requested==MaximumSamples-1&&!clear))
            {overflow++;throw new InvalidOperationException("Ember motion32-frame budget exhausted; no samples are silently dropped.");}
            int slot=requested;var sample=samples[slot];sample.unityFrame=Time.frameCount;sample.wallSeconds=Now-start;
            sample.nativeAge=clear?-1:(float)ageField.GetValue(cast);
            sample.studyFrame=clear?-1:(float)studyFrameMethod.Invoke(null,new[]{cast});
            sample.headForward=double.NaN;
            for(int i=0;i<filters.Length;i++)
            {
                if(renderers[i]==null||!renderers[i].enabled||!filters[i].gameObject.activeInHierarchy)continue;
                if(!roles.TryGetValue(filters[i].sharedMesh,out var role))continue;
                sample.meshes++;
                if(role==NativeSpellFxRole.ProjectileHead)sample.heads++;
                if(role==NativeSpellFxRole.ProjectileTrail)sample.trails++;
                if(role==NativeSpellFxRole.TargetImpact)sample.impacts++;
                if(filters[i].sharedMesh==trackedHead)sample.headForward=Vector3.Dot(filters[i].transform.position-source,forward);
            }
            if(double.IsNaN(sample.headForward))sample.headForward=-1;
            Request(slot);requested++;lastCaptureAge=sample.nativeAge;clearCaptured=clear;
        }

        void Request(int slot)
        {
            int ring=Array.FindIndex(busy,b=>!b);
            Require(ring>=0,"All three readback textures are busy; motion capture refuses synchronous fallback.");
            ringForSlot[slot]=ring;busy[ring]=true;pending++;
            try
            {
                ScreenCapture.CaptureScreenshotIntoRenderTexture(textures[ring]);
                AsyncGPUReadback.RequestIntoNativeArray(ref buffers[slot],textures[ring],0,TextureFormat.RGBA32,callbacks[slot]);
            }
            catch { busy[ring]=false;pending--;readbackErrors++;throw; }
        }
        void ReadbackComplete(int slot,AsyncGPUReadbackRequest request)
        {
            var frame=samples[slot];frame.completedFrame=Time.frameCount;frame.completedWallSeconds=Now-start;
            if(request.hasError||request.layerDataSize!=BytesPerFrame)readbackErrors++;
            busy[ringForSlot[slot]]=false;pending--;finished++;
            if(releaseRequested&&pending==0)ReleaseOwned();
        }

        public IEnumerator Complete(StarterSpell3DNativeAudit.CastRow command,bool blocking,bool gestureIdle)
        {
            Require(native.ActiveCount==0&&!blocking&&gestureIdle,"PNG encoding must wait for real FX and gesture clear.");
            if(!clearCaptured){yield return new WaitForEndOfFrame();CaptureEndOfFrame();}
            double deadline=Now+5;
            while(pending>0){Require(Now<deadline,"Motion readback drain timed out.");yield return null;}
            row.requested=requested;row.completed=finished;row.pendingAtEncode=pending;row.activeAtEncode=native.ActiveCount;
            row.gestureIdleAtEncode=gestureIdle;row.readbackErrors=readbackErrors;row.overflow=overflow;
            row.seconds=Math.Max(command.seconds,requested>0?samples[requested-1].wallSeconds:0);
            row.encodingStarted=Now-start;
            Require(readbackErrors==0&&overflow==0,"Incomplete GPU motion capture cannot be encoded as accepted evidence.");
            for(int i=0;i<requested;i++)
            {
                var sample=samples[i];sample.path=Path.Combine(directory,stem+"-"+row.mode+"-"+i.ToString("D3")+".png");
                sample.rgbSha256=HashRgb(buffers[i],flipped,false);WritePng(i,sample.path);sample.sha256=FileHash(sample.path);row.encoded++;
                yield return null;
            }
            row.frames=samples.Take(requested).Select(f=>JsonUtility.FromJson<Frame>(JsonUtility.ToJson(f))).ToArray();
            receipt.casts=receipt.casts.Concat(new[]{row}).ToArray();row=null;
            File.WriteAllText(ReportPath,JsonUtility.ToJson(receipt,true));
        }

        public void Finish()
        {
            Require(pending==0&&row==null,"Motion appendix cannot finish with in-flight ownership.");
            receipt.passed=HasMotionEvidence(JsonUtility.ToJson(receipt));
            File.WriteAllText(ReportPath,JsonUtility.ToJson(receipt,true));
            Require(receipt.passed,"Ember motion evidence lacks calibrated moving flight, impact, or clear.");
        }
        public void Dispose()
        {
            if(releaseRequested)return;releaseRequested=true;
            if(pending==0)ReleaseOwned(); // Requests retain buffers until their own completion callbacks.
        }
        void ReleaseOwned()
        {
            if(disposed)return;disposed=true;
            if(row!=null)
            {
                row.requested=requested;row.completed=finished;row.readbackErrors=readbackErrors;row.overflow=overflow;
                row.frames=samples.Take(requested).Select(f=>JsonUtility.FromJson<Frame>(JsonUtility.ToJson(f))).ToArray();
                receipt.casts=receipt.casts.Concat(new[]{row}).ToArray();receipt.failure="incomplete-cast-disposed";row=null;
            }
            foreach(var texture in textures)if(texture!=null){texture.Release();UnityEngine.Object.Destroy(texture);}
            for(int i=0;i<buffers.Length;i++)if(buffers[i].IsCreated)buffers[i].Dispose();
            receipt.released=true;receipt.pendingAtRelease=pending;
            receipt.rawBuffersRemaining=buffers.Count(b=>b.IsCreated);
            receipt.renderTexturesRemaining=textures.Count(t=>t!=null&&t.IsCreated());
            if(Directory.Exists(directory))File.WriteAllText(ReportPath,JsonUtility.ToJson(receipt,true));
        }

        void WritePng(int slot,string path)
        {
            var texture=new Texture2D(Width,Height,TextureFormat.RGBA32,false,false);
            try
            {
                if(!flipped)texture.LoadRawTextureData(buffers[slot]);
                else
                {
                    var rows=new byte[BytesPerFrame];
                    for(int y=0;y<Height;y++)NativeArray<byte>.Copy(buffers[slot],(Height-1-y)*Width*4,rows,y*Width*4,Width*4);
                    texture.LoadRawTextureData(rows);
                }
                texture.Apply(false,false);File.WriteAllBytes(path,texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.Destroy(texture); }
        }
        static string HashRgb(NativeArray<byte> data,bool flip,bool doubleLinear)
        {
            var rgb=new byte[Width*Height*3];
            for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)for(int c=0;c<3;c++)
            {
                byte value=data[((flip?Height-1-y:y)*Width+x)*4+c];
                rgb[(y*Width+x)*3+c]=doubleLinear?(byte)Mathf.RoundToInt(Mathf.GammaToLinearSpace(value/255f)*255):value;
            }
            return Hash(rgb);
        }
        static string HashColors(Color32[] colors)
        {
            var rgb=new byte[colors.Length*3];for(int i=0;i<colors.Length;i++){rgb[i*3]=colors[i].r;rgb[i*3+1]=colors[i].g;rgb[i*3+2]=colors[i].b;}return Hash(rgb);
        }
        static string Hash(byte[] data){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
        static string FileHash(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
        static bool HashShape(string value)=>value!=null&&value.Length==64&&value.All(c=>c>='0'&&c<='9'||c>='a'&&c<='f');
        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}

        public static bool ValidateFinalFiles(string path,string expectedRunId)
        {
            try
            {
                var r=JsonUtility.FromJson<Receipt>(File.ReadAllText(path));
                if(r.runId!=expectedRunId||!r.passed||!r.released||r.pendingAtRelease!=0||r.rawBuffersRemaining!=0||r.renderTexturesRemaining!=0
                    ||!string.IsNullOrEmpty(r.failure)||!HashShape(r.libraryAssetSha256)||!HashShape(r.runtimeExportSha256)||!HasMotionEvidence(JsonUtility.ToJson(r)))return false;
                string folder=Path.GetDirectoryName(Path.GetFullPath(path)),prefix="SSN-"+expectedRunId+"-";
                var owned=new HashSet<string>(StringComparer.Ordinal);
                foreach(var pair in new[]{(r.calibration.referencePath,r.calibration.referencePngSha256),(r.calibration.bufferedPath,r.calibration.bufferedPngSha256)}
                    .Concat(r.casts.SelectMany(c=>c.frames).Select(f=>(f.path,f.sha256))))
                {
                    if(string.IsNullOrEmpty(pair.Item1)||Path.GetDirectoryName(Path.GetFullPath(pair.Item1))!=folder||!Path.GetFileName(pair.Item1).StartsWith(prefix,StringComparison.Ordinal)
                        ||!owned.Add(pair.Item1)||!HashShape(pair.Item2)||FileHash(pair.Item1)!=pair.Item2)return false;
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>Metadata gate, paired with separate real PNG/hash validation.
        /// It never treats contact-only frames or a constant-position head as flight.</summary>
        public static bool HasMotionEvidence(string json)
        {
            try
            {
                var r=JsonUtility.FromJson<Receipt>(json);
                if(r==null||!r.enabled||!Guid.TryParseExact(r.runId,"N",out var id)||id==Guid.Empty||r.width!=Width||r.height!=Height||r.maximumSamples!=MaximumSamples||r.ringTextures!=RingTextures||r.casts==null||r.casts.Length!=2)return false;
                var c=r.calibration;
                if(c==null||c.width!=Width||c.height!=Height||!HashShape(c.referenceRgbSha256)||c.bufferedRgbSha256!=c.referenceRgbSha256
                    ||!HashShape(c.flippedRgbSha256)||c.flippedRgbSha256==c.referenceRgbSha256||!HashShape(c.doubleLinearRgbSha256)||c.doubleLinearRgbSha256==c.referenceRgbSha256)return false;
                for(int index=0;index<2;index++)
                {
                    var row=r.casts[index];int dy=index==0?0:-1;
                    if(row==null||row.runId!=r.runId||row.spell!=Spell||row.fxMode!="Full"||row.mode!=(index==0?"ember-motion-east":"ember-motion-northeast")
                        ||row.directionX!=1||row.directionY!=dy||row.intendedX!=row.sourceX+3||row.intendedY!=row.sourceY+3*dy
                        ||!Finite(row.releaseSeconds)||!Finite(row.contactSeconds)||!Finite(row.clearSeconds)||row.releaseSeconds<0||row.contactSeconds<=row.releaseSeconds||row.clearSeconds<=row.contactSeconds
                        ||!Finite(row.releaseFrame)||!Finite(row.contactFrame)||row.contactFrame<=row.releaseFrame
                        ||row.frames==null||row.frames.Length<5||row.frames.Length>MaximumSamples||row.requested!=row.frames.Length||row.completed!=row.requested||row.encoded!=row.completed
                        ||row.pendingAtEncode!=0||row.activeAtEncode!=0||!row.gestureIdleAtEncode||row.readbackErrors!=0||row.overflow!=0
                        ||!Finite(row.encodingStarted)||!Finite(row.seconds)||row.seconds<row.clearSeconds||row.encodingStarted<row.seconds)return false;
                    int lastFrame=-1,flight=0;double lastTime=-1,lastForward=double.NegativeInfinity;bool impact=false,clear=false;
                    var images=new HashSet<string>(StringComparer.Ordinal);
                    foreach(var frame in row.frames)
                    {
                        if(frame==null||frame.unityFrame<=lastFrame||frame.completedFrame<frame.unityFrame||!Finite(frame.wallSeconds)||frame.wallSeconds<=lastTime
                            ||!Finite(frame.completedWallSeconds)||frame.completedWallSeconds<frame.wallSeconds||frame.completedWallSeconds>row.encodingStarted
                            ||!Finite(frame.nativeAge)||!Finite(frame.studyFrame)||frame.heads<0||frame.trails<0||frame.impacts<0||frame.meshes<frame.heads+frame.trails+frame.impacts||!HashShape(frame.rgbSha256))return false;
                        lastFrame=frame.unityFrame;lastTime=frame.wallSeconds;
                        bool traveling=frame.nativeAge>=row.releaseSeconds&&frame.nativeAge<row.contactSeconds&&frame.studyFrame>=row.releaseFrame&&frame.studyFrame<row.contactFrame;
                        if(traveling&&frame.heads>0&&frame.trails>0&&frame.impacts==0&&Finite(frame.headForward))
                        {
                            if(frame.headForward<=lastForward||!images.Add(frame.rgbSha256))return false;
                            lastForward=frame.headForward;flight++;
                        }
                        if(frame.nativeAge>=row.contactSeconds&&frame.impacts>0)impact=true;
                        if(impact&&frame.meshes==0&&(frame.nativeAge<0||frame.nativeAge>=row.clearSeconds))clear=true;
                    }
                    if(flight<3||!impact||!clear||lastTime>row.seconds)return false;
                }
                return true;
            }
            catch { return false; }
        }

        [Serializable] public sealed class Receipt
        {public string runId,bounds,failure,libraryAssetSha256,runtimeExportSha256;public bool enabled,passed,released;public int width,height,maximumSamples,ringTextures,pendingAtRelease,rawBuffersRemaining,renderTexturesRemaining;public long allocatedRawBytes,allocatedRenderTextureBytes;public double preparationSeconds;public Calibration calibration;public Cast[] casts=Array.Empty<Cast>();}
        [Serializable] public sealed class Calibration
        {public int width,height,unityFrame;public bool verticalRowFlip;public string referenceRgbSha256,bufferedRgbSha256,flippedRgbSha256,doubleLinearRgbSha256,referencePath,bufferedPath,referencePngSha256,bufferedPngSha256;}
        [Serializable] public sealed class Cast
        {public string runId,spell,mode,fxMode;public int directionX,directionY,sourceX,sourceY,intendedX,intendedY,requested,completed,encoded,pendingAtEncode,activeAtEncode,readbackErrors,overflow;
            public double releaseSeconds,contactSeconds,clearSeconds,releaseFrame,contactFrame,encodingStarted,seconds;public bool gestureIdleAtEncode;public Frame[] frames;}
        [Serializable] public sealed class Frame
        {public int unityFrame,completedFrame,heads,trails,impacts,meshes;public double wallSeconds,completedWallSeconds,nativeAge,studyFrame,headForward;public string rgbSha256,path,sha256;
            public void Reset(){unityFrame=completedFrame=heads=trails=impacts=meshes=0;wallSeconds=completedWallSeconds=nativeAge=studyFrame=headForward=0;rgbSha256=path=sha256=null;}}
    }
}
