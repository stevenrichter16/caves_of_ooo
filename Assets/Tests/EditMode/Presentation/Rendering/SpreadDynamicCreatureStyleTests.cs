using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadDynamicCreatureStyleTests
    {
        [Serializable] private sealed class Source { public Row[] rows; }
        [Serializable] private sealed class Row { public string blueprint; }
        [Test]
        public void EveryCurrentConcreteCreatureHasItsApprovedReceivingBiomeBody()
        {
            string path=Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/SpreadBiome/Authority/all-creature-source-requirements.json");
            var names=JsonUtility.FromJson<Source>("{\"rows\":"+File.ReadAllText(path)+"}").rows.Select(r=>r.blueprint).ToArray();
            Assert.AreEqual(107,names.Length);Assert.AreEqual(107,names.Distinct().Count());
            var failures=new List<string>();int visible=0,hidden=0;
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                f.Set("FullReveal",true);var presenter=(SpawnRing3DPresenter)f.Presenter;
                foreach(string blueprint in names)
                {
                    var actor=blueprint=="Player"?f.Player:f.Factory.CreateEntity(blueprint);
                    Assert.NotNull(actor,blueprint);Assert.True(actor.HasTag("Creature")||actor.HasTag("Player"),blueprint);
                    bool added=actor==f.Player;
                    for(int y=1;y<Zone.Height-2&&!added;y++)for(int x=1;x<Zone.Width-2&&!added;x++)
                        if(f.Zone.CanPlaceFootprint(actor,x,y)&&!f.Zone.GetOccupiedCells(actor,x,y).Any(c=>c.Occupants.Any(e=>e.HasTag("Creature")||e.HasTag("Player"))))
                            added=f.Zone.AddEntity(actor,x,y);
                    Assert.True(added,blueprint+": real legal receiving footprint");
                    var position=f.Zone.GetEntityPosition(actor);var render=actor.GetPart<RenderPart>();Assert.NotNull(render);
                    string glyph=render.RenderString,color=render.ColorString,id=actor.ID;
                    try
                    {
                        f.Reveal();f.Refresh();
                        bool approved=presenter.TryGetApprovedStyle(actor,out var proof);
                        if(!render.Visible)
                        {
                            hidden++;Assert.AreEqual("Glowmaw",blueprint,"No unexamined hidden exclusion.");
                            Assert.False(actor.GetPart<GlowmawAmbushPart>().HasDropped);
                            Assert.False(approved);Assert.False(presenter.IsRenderedEntity(actor));
                        }
                        else
                        {
                            visible++;
                            if(!approved)failures.Add(blueprint+":"+proof.Failure);
                            else
                            {
                                Assert.NotNull(proof.ExpectedMesh);Assert.NotNull(proof.ExpectedMaterial);
                                Assert.True(f.Find(actor,out var root,out _));Assert.True(f.Rendered(actor));Assert.True(f.Pick(actor,out _));
                                var animator=root.GetComponentInChildren<Animator>(true);Assert.NotNull(animator,blueprint);
                                Assert.NotNull(animator.runtimeAnimatorController,blueprint);
                                CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit"},animator.runtimeAnimatorController.animationClips.Select(c=>c.name),blueprint);
                            }
                        }
                        Assert.AreEqual(position,f.Zone.GetEntityPosition(actor));Assert.AreEqual(id,actor.ID);
                        Assert.AreEqual(glyph,render.RenderString);Assert.AreEqual(color,render.ColorString);
                    }
                    finally
                    {
                        if(actor!=f.Player)
                        {
                            Assert.True(f.Zone.RemoveEntity(actor));f.Refresh();
                            Assert.False(presenter.TryGetApprovedStyle(actor,out _),blueprint+": detached owner cannot keep an adopted body");
                            Assert.False(presenter.IsRenderedEntity(actor));
                        }
                    }
                }
            }
            Assert.AreEqual(106,visible);Assert.AreEqual(1,hidden);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
    }
}
