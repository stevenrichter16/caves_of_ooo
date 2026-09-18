using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Regression hypotheses from the independent material-loss review.
    /// Destroy the actual owned material, not a field or a borrowed library asset.</summary>
    public sealed class NativeSpellReadabilityLifecycleTests
    {
        SpellFxMode oldMode;
        float oldSpeed, oldFlash;

        [SetUp] public void Setup()
        {
            oldMode=SpellFxSettings.Mode; oldSpeed=SpellFxSettings.AnimationSpeed; oldFlash=SpellFxSettings.FlashIntensity;
            SpellFxSettings.Mode=SpellFxMode.Full; SpellFxSettings.AnimationSpeed=1; SpellFxSettings.FlashIntensity=0;
            AsciiFxBus.Clear(); SpellFxBus.Clear();
        }
        [TearDown] public void Teardown()
        {
            AsciiFxBus.Clear(); SpellFxBus.Clear();
            SpellFxSettings.Mode=oldMode; SpellFxSettings.AnimationSpeed=oldSpeed; SpellFxSettings.FlashIntensity=oldFlash;
        }
        static Material OwnMaterial(NativeSpellFixture f,string shaderName)
        {
            var shader=Shader.Find(shaderName); Assert.NotNull(shader,"The installed shader is a fixture prerequisite.");
            return f.Own(new Material(shader));
        }
        static Material Configure(NativeSpellFixture f,string kind)
        {
            f.Library.LuminousMaterial=OwnMaterial(f,"CavesOfOoo/Spell3D/Luminous");
            f.Library.GlowMaterial=OwnMaterial(f,"CavesOfOoo/Spell3D/SoftGlow");
            var piece=f.Entry.Pieces[0]; piece.Color=new Color(.23f,.51f,.84f,1);
            piece.Emission=kind=="palette"?0:.65f; piece.Glow=kind=="glow";
            if(piece.Glow)
            {
                piece.VisualBounds=NativeSpellFxBounds.AirborneDecoration;
                f.Mesh.colors=new[]{new Color(1,1,1,0),new Color(1,1,1,1),new Color(1,1,1,.4f)};
            }
            return kind=="palette"?f.Library.Material:kind=="luminous"?f.Library.LuminousMaterial:f.Library.GlowMaterial;
        }
        static void AssertRendered(NativeSpellFixture f,Material source)
        {
            var view=f.Drawn().Single(); Assert.NotNull(view.sharedMaterial);
            Assert.AreEqual(source.shader,view.sharedMaterial.shader); Assert.AreNotSame(source,view.sharedMaterial);
            Assert.AreSame(f.Mesh,view.GetComponent<MeshFilter>().sharedMesh);
            Assert.AreSame(f.Surface.FogTexture,view.sharedMaterial.GetTexture("_FogLight"));
            var block=new MaterialPropertyBlock(); view.GetPropertyBlock(block);
            Assert.Less(Vector4.Distance(f.Entry.Pieces[0].Color,block.GetVector("_BaseColor")),.00001f);
            Assert.That(block.GetFloat("_Emission"),Is.EqualTo(f.Entry.Pieces[0].Emission).Within(.00001f));
        }
        static WorldFxCoordinator World(NativeSpellFixture f,NativeSpellFxLibrary library=null)
        {
            var tile=new GameObject("Material recovery legacy surface",typeof(Tilemap),typeof(TilemapRenderer));
            tile.transform.SetParent(f.Host.transform);
            var world=new WorldFxCoordinator(new AsciiFxRenderer(tile.GetComponent<Tilemap>()),f.Host.transform,library??f.Library);
            f.Fx=world.NativeRenderer; world.SetZone(f.Zone); world.SetNativeSurface(f.Surface);
            return world;
        }

        [TestCase("palette")] [TestCase("luminous")] [TestCase("glow")]
        public void DestroyedIdleOwnedMaterialRecoversBeforeTheNextCast(string kind)
        {
            using(var f=new NativeSpellFixture())
            {
                var source=Configure(f,kind); var sourceColor=source.GetColor("_BaseColor"); var sourceFog=source.GetTexture("_FogLight");
                f.Create(); Assert.IsTrue(f.Fx.Prepare()); Assert.IsTrue(f.Fx.IsPrepared);
                Assert.Greater(f.Fx.Play(f.Sequence()),0); f.Fx.Update(.33f); AssertRendered(f,source);
                var root=f.Fx.Root; var view=f.Drawn().Single(); var lost=view.sharedMaterial;
                Assert.AreNotSame(source,lost); int pool=f.Fx.AllocatedViewCount;
                f.Fx.ClearAll(); Assert.IsEmpty(f.Drawn()); Assert.IsFalse(f.Fx.HasBlockingFx);
                Object.DestroyImmediate(lost); Assert.IsTrue(lost==null); Assert.IsTrue(root!=null);
                Assert.IsFalse(f.Fx.IsPrepared,"The missing owned material must actually invalidate readiness.");
                Assert.IsTrue(f.Fx.Prepare());
                Assert.IsTrue(f.Fx.IsPrepared,"Prepare must repair the missing clone, not merely return true with an existing root.");
                Assert.AreSame(root,f.Fx.Root); Assert.AreEqual(pool,f.Fx.AllocatedViewCount);
                Assert.Greater(f.Fx.Play(f.Sequence()),0); f.Fx.Update(.33f); AssertRendered(f,source);
                Assert.AreSame(view,f.Drawn().Single()); Assert.AreNotSame(lost,view.sharedMaterial);
                Assert.AreEqual(sourceColor,source.GetColor("_BaseColor")); Assert.AreSame(sourceFog,source.GetTexture("_FogLight"));
                Assert.IsTrue(source!=null&&f.Mesh!=null); Assert.AreEqual(0,f.Zone.GetAllEntities().Count);
            }
        }

        [TestCase("palette")] [TestCase("luminous")] [TestCase("glow")]
        public void ActiveMaterialLossCancelsTheRealCoordinatorWaitAndCanRecover(string kind)
        {
            using(var f=new NativeSpellFixture())
            {
                var source=Configure(f,kind); var sourceColor=source.GetColor("_BaseColor"); var sourceFog=source.GetTexture("_FogLight");
                using(var world=World(f))
                {
                    Assert.IsTrue(f.Fx.IsPrepared); int pool=f.Fx.AllocatedViewCount;
                    var playback=world.Play(f.Sequence()); world.Update(.33f); AssertRendered(f,source);
                    Assert.AreEqual(WorldFxPlaybackState.Playing,playback.State); Assert.IsTrue(world.HasBlockingFx);
                    var lost=f.Drawn().Single().sharedMaterial; Assert.AreNotSame(source,lost);
                    Object.DestroyImmediate(lost); Assert.IsTrue(lost==null);
                    // This exercises actual ordering: coordinator readiness runs
                    // before native Update, so repairing a material cannot conceal
                    // the interrupted cast and retain its blocking handle.
                    Assert.DoesNotThrow(()=>world.Update(.01f));
                    Assert.AreEqual(WorldFxPlaybackState.Cancelled,playback.State);
                    Assert.IsFalse(world.HasBlockingFx); Assert.IsFalse(f.Fx.HasBlockingFx);
                    Assert.AreEqual(0,f.Fx.ActiveCount); Assert.IsEmpty(f.Drawn());
                    world.Update(0); Assert.IsTrue(f.Fx.IsPrepared);
                    var next=world.Play(f.Sequence()); world.Update(.33f);
                    Assert.AreEqual(WorldFxPlaybackState.Playing,next.State); AssertRendered(f,source);
                    Assert.AreEqual(pool,f.Fx.AllocatedViewCount);
                    world.CancelAll(); Assert.IsFalse(world.HasBlockingFx); Assert.IsEmpty(f.Drawn());
                }
                Assert.AreEqual(sourceColor,source.GetColor("_BaseColor")); Assert.AreSame(sourceFog,source.GetTexture("_FogLight"));
                Assert.IsTrue(source!=null&&f.Mesh!=null&&f.Surface.FogTexture!=null);
                Assert.AreEqual(0,f.Zone.GetAllEntities().Count);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void SuccessfulActualRainKeepsItsImportedCasterGestureWithOnlyRealCropMeshes(bool hasCrop)
        {
            const string spell="Hydromancy_ConjureRain";
            var library=Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);
            Assert.NotNull(library); var entry=library.Find(spell); Assert.NotNull(entry); Assert.NotNull(entry.CastClip);
            Assert.That(entry.CastDuration,Is.GreaterThan(.24f).And.LessThanOrEqualTo(WorldFxPlayback.HardTimeoutSeconds),
                "The imported gesture is distinguishable from the short fallback gesture.");
            var previous=EntityVisualHooks.CastCallback;
            try
            {
                using(var f=new NativeSpellFixture(spell))
                {
                    var caster=StarterSpell3DCaptureFixture.Actor(f.Zone,"rain-caster",10,10);
                    CropPart crop=null;
                    if(hasCrop)
                    {
                        var owner=StarterSpell3DCaptureFixture.Prop(f.Zone,"rain-crop",12,10);
                        crop=new CropPart(); owner.AddPart(crop);
                    }
                    var context=StarterSpell3DCaptureFixture.Context(f.Zone,caster);
                    Assert.IsTrue(new Hydromancy_ConjureRain().OnCommand(context),"No-crop Rain is a successful real skill command.");
                    Assert.IsTrue(context.BlocksTurnAdvance);
                    var sequence=StarterSpell3DCaptureFixture.Single();
                    Assert.AreEqual(spell,sequence.SpellId); Assert.AreSame(caster,sequence.Caster);
                    Assert.AreEqual(hasCrop?1:0,sequence.Targets.Count);
                    if(hasCrop) Assert.AreEqual(40,crop.MoistureTicks);
                    int observed=0; float gestureSeconds=0;
                    EntityVisualHooks.CastCallback=(actor,zone,id,sx,sy,tx,ty,seconds)=>
                    {
                        Assert.AreSame(caster,actor); Assert.AreSame(f.Zone,zone); Assert.AreEqual(spell,id);
                        Assert.AreEqual(10,sx); Assert.AreEqual(10,sy); observed++; gestureSeconds=seconds;
                    };
                    using(var world=World(f,library))
                    {
                        var playback=world.Play(sequence);
                        Assert.AreSame(entry,world.NativeRenderer.LastEntry,
                            "A successful empty Rain cast must keep the actual native entry instead of borrowing fallback motion.");
                        Assert.AreSame(entry.CastClip,world.NativeRenderer.LastEntry.CastClip);
                        Assert.AreEqual(1,observed);
                        Assert.That(gestureSeconds,Is.EqualTo(entry.CastDuration/SpellFxSettings.AnimationSpeed).Within(.00001f));
                        Assert.AreEqual(WorldFxPlaybackState.Playing,playback.State);
                        world.Update(.33f);
                        if(hasCrop)
                        {
                            Assert.Greater(f.Fx.ActiveMeshCount,0);
                            var cropMeshes=entry.Pieces.Where(p=>p.Role==NativeSpellFxRole.CropCell).Select(p=>p.Mesh).ToArray();
                            Assert.IsTrue(f.Drawn().All(r=>cropMeshes.Contains(r.GetComponent<MeshFilter>().sharedMesh)));
                        }
                        else { Assert.AreEqual(0,f.Fx.ActiveMeshCount); Assert.IsEmpty(f.Drawn()); }
                        world.Update(WorldFxPlayback.HardTimeoutSeconds-.5f);
                        Assert.AreEqual(WorldFxPlaybackState.Completed,playback.State,"Native Rain clears normally before the hard timeout.");
                        Assert.IsFalse(world.HasBlockingFx); Assert.AreEqual(0,f.Fx.ActiveCount); Assert.IsEmpty(f.Drawn());
                        Assert.AreSame(entry,f.Fx.LastEntry); Assert.AreEqual(1,observed);
                        Assert.AreEqual(hasCrop?2:1,f.Zone.GetAllEntities().Count);
                        Assert.AreEqual((10,10),f.Zone.GetEntityPosition(caster));
                        if(hasCrop) Assert.AreEqual(40,crop.MoistureTicks,"Presentation cannot water again or tick the crop.");
                    }
                }
            }
            finally { EntityVisualHooks.CastCallback=previous; }
        }

        [Test] public void IntactReadinessPreservesItsReceiptPoolAndAllThreeOwnedMaterials()
        {
            using(var f=new NativeSpellFixture("palette"))
            {
                Configure(f,"palette");
                var bright=f.Piece("bright",NativeSpellFxRole.TargetImpact,NativeSpellFxAnchor.Target); bright.Emission=.65f;
                var glow=f.Piece("glow",NativeSpellFxRole.TargetImpact,NativeSpellFxAnchor.Target); glow.Glow=true; glow.Emission=.4f;
                glow.Mesh=f.Own(Object.Instantiate(f.Mesh)); glow.Mesh.colors=new[]{new Color(1,1,1,0),new Color(1,1,1,1),new Color(1,1,1,.4f)};
                var entries=new[]{f.Entry,
                    new NativeSpellFxEntry{SpellId="luminous",CastClip=f.Entry.CastClip,SampleRate=100,ReleaseFrame=22,StudyContactFrame=32,StudyClearFrame=70,AuthoredDistanceCells=4,Pieces=new[]{bright}},
                    new NativeSpellFxEntry{SpellId="glow",CastClip=f.Entry.CastClip,SampleRate=100,ReleaseFrame=22,StudyContactFrame=32,StudyClearFrame=70,AuthoredDistanceCells=4,Pieces=new[]{glow}}};
                f.Library.Entries=entries; f.Create(); Assert.IsTrue(f.Fx.Prepare());
                var root=f.Fx.Root; var receipt=f.Fx.LastPreparation; int pool=f.Fx.AllocatedViewCount;
                var owned=new Material[3];
                for(int round=0;round<2;round++) for(int i=0;i<entries.Length;i++)
                {
                    Assert.IsTrue(f.Fx.Prepare()); Assert.AreSame(receipt,f.Fx.LastPreparation); Assert.AreSame(root,f.Fx.Root);
                    Assert.Greater(f.Fx.Play(f.Sequence(spell:entries[i].SpellId)),0); f.Fx.Update(.33f);
                    var material=f.Drawn().Single().sharedMaterial; Assert.NotNull(material);
                    if(round==0) owned[i]=material; else Assert.AreSame(owned[i],material);
                    f.Fx.ClearAll(); Assert.IsFalse(f.Fx.HasBlockingFx); Assert.AreEqual(pool,f.Fx.AllocatedViewCount);
                }
                Assert.AreEqual(3,owned.Distinct().Count()); Assert.AreSame(receipt,f.Fx.LastPreparation);
                Assert.IsTrue(f.Fx.IsPrepared); Assert.AreEqual(0,f.Zone.GetAllEntities().Count);
            }
        }
    }
}
