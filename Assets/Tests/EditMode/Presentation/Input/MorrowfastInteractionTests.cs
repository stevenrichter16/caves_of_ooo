using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastInteractionTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [Test] public void DirectionalInteractionWithTheSideOfACisternSelectsItsActualOwner()
        {
            var f=MorrowfastTestWorld.Factory();var z=new OverworldZoneManager(f,64).GetZone(MorrowfastSceneRuntime.ZoneID);
            var p=MorrowfastTestWorld.Actor(f,z);var owner=MorrowfastSceneRuntime.FindOwner(z,"central-cistern");
            var spec=MorrowfastSceneDefinition.Load().FindOwner("central-cistern");
            var contact=spec.footprint.Select(c=>z.GetCell(c.x,c.y)).First(c=>!c.Objects.Contains(owner)&&MorrowfastSceneRuntime.BlockingOwner(c)==owner);
            var go=new GameObject("Morrowfast input contract");var ui=new GameObject("Morrowfast menu contract");
            try {
                var input=go.AddComponent<InputHandler>();input.PlayerEntity=p;input.CurrentZone=z;input.WorldActionMenuUI=ui.AddComponent<WorldActionMenuUI>();
                typeof(InputHandler).GetMethod("OpenWorldActionMenu",Private).Invoke(input,new object[]{contact.X,contact.Y});
                Assert.AreSame(owner,input.WorldActionMenuUI.SelectedTarget,"The footprint must select the real water provider, not terrain or an invisible proxy.");
                Assert.IsFalse(input.WorldActionMenuUI.SelectedCellIsPile);
            } finally {Object.DestroyImmediate(go);Object.DestroyImmediate(ui);}
        }
        [TestCase(true)] [TestCase(false)] public void NativeComponentClearSpendsExactlyOneTurnOnlyOnSuccess(bool adjacent)
        {
            var f=MorrowfastTestWorld.Factory();var z=new OverworldZoneManager(f,64).GetZone(MorrowfastSceneRuntime.ZoneID);var p=MorrowfastTestWorld.Actor(f,z);
            var target=MorrowfastTestWorld.Clearable(z,p);if(!adjacent)z.MoveEntity(p,79,24);
            var turns=new TurnManager();turns.AddEntity(p);turns.ProcessUntilPlayerTurn();int before=turns.TickCount;
            var go=new GameObject("Morrowfast turn contract");
            try {
                var input=go.AddComponent<InputHandler>();input.PlayerEntity=p;input.CurrentZone=z;input.TurnManager=turns;
                var action=WorldInteractionSystem.GatherActions(target,p).Single(a=>a.Command==MorrowfastPropPart.ClearCommand);
                var execute=typeof(InputHandler).GetMethod("ExecuteWorldActionSelection",Private);var cell=z.GetEntityCell(target);
                execute.Invoke(input,new object[]{action,target,cell,false});
                Assert.AreEqual(!adjacent,MorrowfastSceneRuntime.IsPresent(z,"provisioners-stall"));
                Assert.AreEqual(before+(adjacent?TurnManager.ActionThreshold/p.GetStatValue("Speed"):0),turns.TickCount);
                int after=turns.TickCount;execute.Invoke(input,new object[]{action,target,cell,false});Assert.AreEqual(after,turns.TickCount);
            } finally {Object.DestroyImmediate(go);}
        }
        [TestCase("inn-outdoor-stove")] [TestCase("inn-kindling-crate")]
        public void RealOverlappingSourceOwnersRemainIndividuallySelectable(string selectedId)
        {
            var f=MorrowfastTestWorld.Factory();var z=new OverworldZoneManager(f,64).GetZone(MorrowfastSceneRuntime.ZoneID);
            var actor=MorrowfastTestWorld.Actor(f,z);var stove=MorrowfastSceneRuntime.FindOwner(z,"inn-outdoor-stove");
            var crate=MorrowfastSceneRuntime.FindOwner(z,"inn-kindling-crate");var contact=z.GetEntityCell(crate);
            Assert.AreSame(contact,z.GetEntityCell(stove),"This fixture must exercise the actual authored overlap, not a synthetic empty cell.");
            Assert.IsTrue(contact.Objects.Contains(stove)&&contact.Objects.Contains(crate));
            Assert.IsNotNull(MorrowfastSceneRuntime.BlockingOwner(contact),"The footprint override must actually be eligible before its local-owner guard.");
            var go=new GameObject("Morrowfast overlap input");var menu=new GameObject("Morrowfast overlap menu");
            try {
                var input=go.AddComponent<InputHandler>();input.PlayerEntity=actor;input.CurrentZone=z;input.WorldActionMenuUI=menu.AddComponent<WorldActionMenuUI>();
                typeof(InputHandler).GetMethod("OpenWorldActionMenu",Private).Invoke(input,new object[]{contact.X,contact.Y});
                Assert.IsTrue(input.WorldActionMenuUI.SelectedCellIsPile,"A blocking source footprint cannot hide another live source owner occupying the contacted cell.");
                var rows=(System.Collections.Generic.List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Private).GetValue(input.WorldActionMenuUI);
                var everything=rows.Single(a=>a.Command==WorldInteractionSystem.PickCellCommand);
                var execute=typeof(InputHandler).GetMethod("ExecuteWorldActionSelection",Private);
                execute.Invoke(input,new object[]{everything,input.WorldActionMenuUI.SelectedTarget,contact,true});
                rows=(System.Collections.Generic.List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Private).GetValue(input.WorldActionMenuUI);
                Assert.IsTrue(rows.Any(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+stove.ID));
                Assert.IsTrue(rows.Any(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+crate.ID));
                var selected=MorrowfastSceneRuntime.FindOwner(z,selectedId);
                var choose=rows.Single(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+selected.ID);
                execute.Invoke(input,new object[]{choose,input.WorldActionMenuUI.SelectedTarget,contact,false});
                Assert.AreSame(selected,input.WorldActionMenuUI.SelectedTarget);Assert.IsFalse(input.WorldActionMenuUI.SelectedCellIsPile);
                rows=(System.Collections.Generic.List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Private).GetValue(input.WorldActionMenuUI);
                Assert.IsTrue(rows.Any(a=>a.Command==(selectedId=="inn-kindling-crate"?"OpenContainer":"RestAtCampfire")),"The selected owner exposes its actual individual service.");
            } finally {Object.DestroyImmediate(go);Object.DestroyImmediate(menu);}
        }
        [Test] public void OrdinaryLooseItemInsideASourceFootprintRetainsItsNativeMenu()
        {
            var f=MorrowfastTestWorld.Factory();var z=new OverworldZoneManager(f,64).GetZone(MorrowfastSceneRuntime.ZoneID);
            var actor=MorrowfastTestWorld.Actor(f,z);var cistern=MorrowfastSceneRuntime.FindOwner(z,"central-cistern");
            var spec=MorrowfastSceneDefinition.Load().FindOwner("central-cistern");
            var contact=spec.footprint.Select(c=>z.GetCell(c.x,c.y)).First(c=>!c.Objects.Contains(cistern)&&MorrowfastSceneRuntime.BlockingOwner(c)==cistern);
            var item=f.CreateEntity("Tepuibone");Assert.IsTrue(z.AddEntity(item,contact.X,contact.Y));
            Assert.IsFalse(item.HasPart<MorrowfastPropPart>());Assert.IsFalse(WorldInteractionSystem.IsTerrain(item));
            var go=new GameObject("Morrowfast loose-item input");var menu=new GameObject("Morrowfast loose-item menu");
            try {
                var input=go.AddComponent<InputHandler>();input.PlayerEntity=actor;input.CurrentZone=z;input.WorldActionMenuUI=menu.AddComponent<WorldActionMenuUI>();
                typeof(InputHandler).GetMethod("OpenWorldActionMenu",Private).Invoke(input,new object[]{contact.X,contact.Y});
                Assert.AreSame(item,input.WorldActionMenuUI.SelectedTarget,"Footprint geometry must not swallow a real loose item's ordinary native menu.");
                Assert.AreSame(contact,input.WorldActionMenuUI.SelectedCell);Assert.IsFalse(input.WorldActionMenuUI.SelectedCellIsPile);
            } finally {Object.DestroyImmediate(go);Object.DestroyImmediate(menu);}
        }
    }
}
