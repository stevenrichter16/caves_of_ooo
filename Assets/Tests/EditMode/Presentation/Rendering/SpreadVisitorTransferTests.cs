using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    // Deliberate party setup and native transfer, not natural recruitment.
    public sealed class SpreadVisitorTransferTests
    {
        [TestCase("MawToad")][TestCase("GroveLanternMoth")]
        [TestCase("CaveBat")][TestCase("GlassScorpion")]
        public void CurrentPartyOwnerChangesPresentationAcrossActualReceivingZones(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var spread=f.Zone;var foreign=f.Manager.GetZone("Overworld.2.5.0");
                Assert.True(SpreadPresentationScope.IsActive(spread));Assert.False(SpreadPresentationScope.IsActive(foreign));
                var actor=f.Factory.CreateEntity(blueprint);Assert.NotNull(actor);Place(foreign,actor);
                var brain=actor.GetPart<BrainPart>();Assert.NotNull(brain);brain.CurrentZone=foreign;
                var leader=f.Player.GetPart<BrainPart>();Assert.NotNull(leader);leader.PartyMembers.Add(actor);
                var originalParts=actor.Parts.ToArray();var render=actor.GetPart<RenderPart>();
                string id=actor.ID,glyph=render.RenderString,color=render.ColorString;
                var arrival=spread.GetEntityCell(f.Player);
                ZoneTransitionSystem.TransitPartyMembers(f.Player,foreign,spread,arrival.X,arrival.Y);
                Assert.IsNull(foreign.GetEntityCell(actor));Assert.NotNull(spread.GetEntityCell(actor));Assert.AreSame(spread,brain.CurrentZone);
                f.Reveal();f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(actor,out var evidence),blueprint+":"+evidence.Failure);
                Assert.True(presenter.IsRenderedEntity(actor));Assert.True(f.Find(actor,out var spreadRoot,out _));
                var target=FirstPlacement(foreign,f.Player);
                Assert.True(spread.TryTransferEntityTo(f.Player,foreign,target.x,target.y));
                ZoneTransitionSystem.TransitPartyMembers(f.Player,spread,foreign,target.x,target.y);
                Assert.IsNull(spread.GetEntityCell(actor));Assert.NotNull(foreign.GetEntityCell(actor));Assert.AreSame(foreign,brain.CurrentZone);
                f.Refresh();Assert.False(presenter.TryGetApprovedStyle(actor,out _));Assert.False(presenter.IsRenderedEntity(actor));
                SpawnRing3DIntegrationFixture.Hidden(spreadRoot);
                f.Zone=foreign;f.Manager.SetActiveZone(foreign);f.Reveal();f.Bind(foreign);f.Refresh();
                Assert.False(presenter.TryGetApprovedStyle(actor,out _),"Same exact owner loses scoped approval outside Spread.");
                if(blueprint=="MawToad"||blueprint=="GroveLanternMoth")
                {
                    string expected=blueprint=="MawToad"?"ring-maw-toad":"ring-grove-lantern-moth";
                    Assert.True(f.Find(actor,out var root,out var model));Assert.AreEqual(expected,model);Assert.True(f.Rendered(actor));
                    var source=f.Library.FindModel(expected).GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
                    Assert.AreSame(Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath).Resolve(source),root.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh);
                }
                Assert.AreEqual(id,actor.ID);Assert.AreEqual(glyph,render.RenderString);Assert.AreEqual(color,render.ColorString);
                CollectionAssert.AreEqual(originalParts,actor.Parts);Assert.True(leader.PartyMembers.Contains(actor));
            }
        }
        [TestCase(false)][TestCase(true)]
        public void NonpartyOrWrongSourceOwnerDoesNotAcquireReceivingPresentation(bool wrongSource)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var source=f.Manager.GetZone("Overworld.2.5.0");var actor=f.Factory.CreateEntity("MawToad");Place(source,actor);
                var brain=actor.GetPart<BrainPart>();brain.CurrentZone=wrongSource?f.Zone:source;
                if(wrongSource)f.Player.GetPart<BrainPart>().PartyMembers.Add(actor);
                var before=source.GetEntityPosition(actor);var at=f.Zone.GetEntityCell(f.Player);
                ZoneTransitionSystem.TransitPartyMembers(f.Player,source,f.Zone,at.X,at.Y);
                Assert.AreEqual(before,source.GetEntityPosition(actor));Assert.IsNull(f.Zone.GetEntityCell(actor));
                f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.False(presenter.TryGetApprovedStyle(actor,out _));Assert.False(presenter.IsRenderedEntity(actor));
            }
        }
        private static (int x,int y) FirstPlacement(Zone zone,Entity actor)
        {
            for(int y=2;y<Zone.Height-2;y++)for(int x=2;x<Zone.Width-2;x++)
                if(zone.CanPlaceFootprint(actor,x,y)&&!zone.GetOccupiedCells(actor,x,y).Any(c=>c.Occupants.Any(e=>e.HasTag("Creature")||e.HasTag("Player"))))return(x,y);
            Assert.Fail("Generated fixture has no legal actor footprint.");return(-1,-1);
        }
        private static void Place(Zone zone,Entity actor){var p=FirstPlacement(zone,actor);Assert.True(zone.AddEntity(actor,p.x,p.y));}
    }
}
