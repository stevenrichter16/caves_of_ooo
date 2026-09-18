using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Readability contracts pair rich effects with identical sparse,
    /// unlit, denied and unavailable controls; GPU acceptance measures pixels.</summary>
    public sealed class NativeSpellReadabilityTests
    {
        static readonly string[] Concepts = { "Pyromancy_EmberSpit", "Hydromancy_JetBlast",
            "Galvanism_GroundSurge", "Cryomancy_RimeGrip", "Spellcraft_Calm" };
        [SetUp] public void Setup() { SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.FlashIntensity = 0; }
        [TearDown] public void Teardown() { SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.FlashIntensity = .15f; }
        static void Set(object owner, string name, object value)
        {
            var field = owner.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field, "Explicit authored field is required: " + name); field.SetValue(owner, value);
        }
        static T Get<T>(object owner, string name)
        {
            var field = owner.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field, "Explicit authored field is required: " + name); return (T)field.GetValue(owner);
        }
        static Material Material(NativeSpellFixture f, string shader)
        {
            var actual = Shader.Find(shader); Assert.NotNull(actual, "Owned spell shader must exist: " + shader);
            return f.Own(new Material(actual));
        }
        static void Materials(NativeSpellFixture f)
        {
            Set(f.Library, "LuminousMaterial", Material(f,"CavesOfOoo/Spell3D/Luminous"));
            Set(f.Library, "GlowMaterial", Material(f,"CavesOfOoo/Spell3D/SoftGlow"));
        }
        [TestCase(false)] [TestCase(true)]
        public void LaterEssentialContactSurvivesDenseOptionalPieces(bool detail)
        {
            using (var f = new NativeSpellFixture())
            {
                var main = f.Piece("main-readable-contact",NativeSpellFxRole.TargetImpact,NativeSpellFxAnchor.Target);
                var pieces = Enumerable.Range(0,detail ? NativeSpellFxRenderer.MaximumScheduledFragments : 0)
                    .Select(i=>f.Piece("detail-"+i,NativeSpellFxRole.TargetImpact,NativeSpellFxAnchor.Target,essential:false)).ToList();
                main.Color=Color.green; pieces.Add(main); f.Entry.Pieces=pieces.ToArray(); f.Create();
                Assert.Greater(f.Fx.Play(f.Sequence()),0);f.Fx.Update(.33f);
                var block=new MaterialPropertyBlock();
                Assert.IsTrue(f.Drawn().Any(r=>{r.GetPropertyBlock(block);return block.GetVector("_BaseColor").y>.9f;}),
                    "An optional particle bank must not erase the later main contact shape.");
                Assert.LessOrEqual(f.Fx.ActiveCount,NativeSpellFxRenderer.MaximumScheduledFragments);
                Assert.LessOrEqual(f.Fx.ActiveMeshCount,NativeSpellFxRenderer.MaximumFullMeshes);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void LaterCastCoreSurvivesEarlierOptionalDrawPressure(bool pressure)
        {
            using (var f=new NativeSpellFixture())
            {
                var old=f.Piece("earlier-detail",NativeSpellFxRole.TargetImpact,NativeSpellFxAnchor.Target,essential:false);
                f.Entry.Pieces=pressure ? Enumerable.Range(0,400).Select(i=>f.Piece("detail-"+i,old.Role,old.Anchor,essential:false)).ToArray() : new[]{old};
                f.Create();Assert.Greater(f.Fx.Play(f.Sequence()),0);
                var main=f.Piece("later-core",old.Role,old.Anchor);main.Color=Color.green;
                f.Entry.Pieces=new[]{main};Assert.Greater(f.Fx.Play(f.Sequence()),0);f.Fx.Update(.33f);
                var block=new MaterialPropertyBlock();
                Assert.IsTrue(f.Drawn().Any(r=>{r.GetPropertyBlock(block);return block.GetVector("_BaseColor").y>.9f;}));
            }
        }
        [TestCase(0f)] [TestCase(.85f)]
        public void AuthoredEmissionSelectsOwnedMaterialAndUploadsWithoutMutatingSharedAssets(float emission)
        {
            using(var f=new NativeSpellFixture())
            {
                Materials(f);Set(f.Entry.Pieces[0],"Emission",emission);f.Create();
                Assert.Greater(f.Fx.Play(f.Sequence()),0);f.Fx.Update(.33f);var view=f.Drawn().Single();
                Assert.AreEqual(emission>0 ? "CavesOfOoo/Spell3D/Luminous" : "CavesOfOoo/Village3D/Palette",view.sharedMaterial.shader.name);
                Assert.AreNotSame(f.Library.Material,view.sharedMaterial);
                Assert.AreNotSame(Get<Material>(f.Library,"LuminousMaterial"),view.sharedMaterial);
                Assert.AreSame(f.Surface.FogTexture,view.sharedMaterial.GetTexture("_FogLight"));
                var b=new MaterialPropertyBlock();view.GetPropertyBlock(b);Assert.AreEqual(emission,b.GetFloat("_Emission"));
                Assert.AreEqual(0,Get<Material>(f.Library,"LuminousMaterial").GetFloat("_Emission"),"Borrowed source material stays unchanged.");
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void GlowUsesAuthoredAlphaGradientAndCanReturnItsPooledViewToOpaque(bool glow)
        {
            using(var f=new NativeSpellFixture())
            {
                Materials(f);f.Mesh.colors=new[]{new Color(1,1,1,0),new Color(1,1,1,.35f),Color.white};
                Set(f.Entry.Pieces[0],"Glow",glow);Set(f.Entry.Pieces[0],"Emission",glow ? .25f : 0f);
                f.Create();f.Fx.Play(f.Sequence());f.Fx.Update(.33f);var view=f.Drawn().Single();
                Assert.AreEqual(glow ? "CavesOfOoo/Spell3D/SoftGlow" : "CavesOfOoo/Village3D/Palette",view.sharedMaterial.shader.name);
                Assert.AreEqual(0,f.Mesh.colors[0].a);Assert.AreEqual(1,f.Mesh.colors[2].a);
                f.Fx.ClearAll();Set(f.Entry.Pieces[0],"Glow",false);Set(f.Entry.Pieces[0],"Emission",0f);
                f.Fx.Play(f.Sequence());f.Fx.Update(.33f);
                Assert.AreSame(view,f.Drawn().Single());Assert.AreEqual("CavesOfOoo/Village3D/Palette",view.sharedMaterial.shader.name);
                var b=new MaterialPropertyBlock();view.GetPropertyBlock(b);Assert.AreEqual(0,b.GetFloat("_Emission"));
            }
        }
        [Test] public void TargetAccentWaitsForRecordedContactWhileGatherCanAnticipate()
        {
            using(var f=new NativeSpellFixture())
            {
                var gather=f.Piece("gather",NativeSpellFxRole.SourceGather,NativeSpellFxAnchor.Source);
                gather.Color=Color.green; f.Entry.Pieces=new[]{f.Entry.Pieces[0],gather};
                f.Create();Assert.Greater(f.Fx.Play(f.Sequence()),0);f.Fx.Update(.25f);
                Assert.AreEqual(1,f.Drawn().Length,"The source may gather before impact, target success cannot pre-empt travel.");
                var b=new MaterialPropertyBlock();f.Drawn()[0].GetPropertyBlock(b);Assert.AreEqual(1,b.GetVector("_BaseColor").y);
                f.Fx.Update(.08f);Assert.AreEqual(2,f.Drawn().Length);
            }
        }
        [Test] public void FilteredBodyKeepsTheValidNativeCasterGestureWithoutInventingImpactGeometry()
        {
            // The initial zero-body-is-no-work hypothesis was false: the entry
            // still supplies a real caster gesture (including successful empty Rain).
            using(var f=new NativeSpellFixture())
            {
                f.Entry.Pieces=new[]{f.Piece("success-only",NativeSpellFxRole.TargetImpact,NativeSpellFxAnchor.Target,NativeSpellFxCondition.FrozenApplied)};
                f.Create();var seconds=f.Fx.Play(f.Sequence());
                Assert.That(seconds,Is.GreaterThan(0).And.LessThanOrEqualTo(WorldFxPlayback.HardTimeoutSeconds));
                Assert.AreSame(f.Entry,f.Fx.LastEntry);Assert.AreEqual(0,f.Fx.ActiveCount);Assert.IsTrue(f.Fx.HasBlockingFx);
                f.Fx.Update(seconds);Assert.IsFalse(f.Fx.HasBlockingFx);Assert.IsEmpty(f.Drawn());
                Assert.Greater(f.Fx.Play(f.Sequence(targets:new[]{NativeSpellFixture.Hit("body",14,10,applied:new[]{"FrozenEffect"})})),0);
                f.Fx.Update(.33f);Assert.IsNotEmpty(f.Drawn());
            }
        }
        [TestCaseSource(nameof(Concepts))]
        public void ApprovedConceptImportsConnectedGeometryAndLocalGlow(string spell)
        {
            var library=Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);Assert.NotNull(library);
            var e=library.Find(spell);Assert.NotNull(e);
            var luminous=e.Pieces.Where(p=>Get<float>(p,"Emission")>=.5f&&!Get<bool>(p,"Glow")).ToArray();
            var halos=e.Pieces.Where(p=>Get<bool>(p,"Glow")).ToArray();
            Assert.IsNotEmpty(luminous,"Each approved concept needs a bright connected main shape.");
            Assert.IsNotEmpty(halos,"Mesh-local glow must be actual imported art.");
            Assert.IsTrue(luminous.Any(p=>p.Mesh.triangles.Length>=36),"Use authored surfaces, not only tiny dots.");
            foreach(var p in halos)
            {
                var colors=p.Mesh.colors;Assert.AreEqual(p.Mesh.vertexCount,colors.Length);
                Assert.LessOrEqual(colors.Min(c=>c.a),.001f);Assert.Greater(colors.Max(c=>c.a),.01f);
                Assert.IsTrue(p.Poses.Any(pose=>pose.Scale.sqrMagnitude>.01f));
            }
            Assert.IsTrue(e.Pieces.Any(p=>!p.ReducedEssential),"Reduced mode retains a real lower-detail option.");
        }
    }
}
