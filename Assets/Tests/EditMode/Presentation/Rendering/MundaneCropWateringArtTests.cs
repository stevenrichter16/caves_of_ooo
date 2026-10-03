using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Actual generated prepared Sootroot + actual transaction + imported model
    // resolution/presenter refresh. This is not a paid-input or camera-pixel witness.
    public sealed class MundaneCropWateringArtTests
    {
        [TestCase(false)] [TestCase(true)]
        public void ActualWateringCommandPublishesWetArtOnlyWhenItsPaymentCommits(bool outerFailure)
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new SpawnRing3DIntegrationFixture(GleanersCellarBuilder.ZoneID))
            {
                SettlementRuntime.ActiveZone = f.Zone;
                var owner = f.Zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SootrootCrop"
                    && e.GetPart<CropPart>().GrowthStage == 0);
                var crop = owner.GetPart<CropPart>(); var cell = f.Zone.GetEntityCell(owner);
                var soil = cell.Objects.Single(e => e.HasPart<CultivatedSoilPart>());
                Assert.AreEqual("Floor", soil.BlueprintName);
                Assert.True(CultivatedSoilPart.IsCultivated(f.Zone, cell));
                Assert.AreEqual(0, crop.MoistureTicks);
                Assert.True(CropTime.Reconcile(crop, f.Zone, WorldClock.CurrentTick));
                int clock = crop.LastGrowthWorldTick;
                var dry = Exact(f, owner, "biome-crop-sootroot-0-dry");
                f.Approach(owner);
                var vessel = f.Factory.CreateEntity("Waterskin");
                vessel.GetPart<WaterskinPart>().Charges = 3;
                Assert.True(f.Player.GetPart<InventoryPart>().AddObject(vessel));
                string command = "WaterCrop|" + Uri.EscapeDataString(vessel.ID);
                Assert.True(WorldInteractionSystem.GatherActions(owner, f.Player).Any(a => a.Command == command));
                if (outerFailure) f.Player.AddPart(new RejectAfterWatering());
                var reasons = new List<string>();
                var oldDirty = ZoneRenderHooks.CellDirtyCallback;
                ZoneRenderHooks.CellDirtyCallback = (x, y, reason) =>
                {
                    if (x == cell.X && y == cell.Y) reasons.Add(reason);
                    oldDirty?.Invoke(x, y, reason);
                };
                try
                {
                    Assert.AreEqual(!outerFailure, InventorySystem.ExecuteCommand(
                        new PerformInventoryActionCommand(owner, command), f.Player, f.Zone).Success);
                }
                finally { ZoneRenderHooks.CellDirtyCallback = oldDirty; }
                Assert.AreEqual(outerFailure ? 3 : 2, vessel.GetPart<WaterskinPart>().Charges);
                Assert.AreEqual(outerFailure ? 0 : 40, crop.MoistureTicks);
                Assert.AreEqual(clock, crop.LastGrowthWorldTick);
                Assert.AreEqual(0, crop.TicksInStage);
                Assert.AreSame(cell, f.Zone.GetEntityCell(owner));
                Assert.AreSame(soil, cell.Objects.Single(e => e.HasPart<CultivatedSoilPart>()));
                Assert.True(CultivatedSoilPart.IsCultivated(f.Zone, cell));
                Assert.Contains(outerFailure ? "CropHandWaterRollback" : "CropHandWatered", reasons);
                Assert.False(reasons.Contains(outerFailure ? "CropHandWatered" : "CropHandWaterRollback"));
                var after = Exact(f, owner, "biome-crop-sootroot-0-" + (outerFailure ? "dry" : "wet"));
                if (outerFailure) Assert.AreSame(dry, after);
                else
                {
                    Assert.AreNotSame(dry, after);
                    CollectionAssert.AreEqual(dry.vertices, after.vertices);
                    Assert.False(dry.uv.SequenceEqual(after.uv), "Committed supply selects the actual wet-soil art.");
                }
            }
        }

        static Mesh Exact(SpawnRing3DIntegrationFixture f, Entity owner, string model)
        {
            var recipe = SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition);
            Assert.IsNull(recipe.Failure); Assert.AreSame(owner, recipe.Owner); Assert.AreEqual(model, recipe.ModelId);
            f.Refresh(f.Dirty(owner)); Assert.True(f.Rendered(owner), model);
            var prefab = f.Library.FindModel(model); Assert.NotNull(prefab, model);
            var mesh = prefab.GetComponent<MeshFilter>().sharedMesh; Assert.NotNull(mesh);
            return mesh;
        }

        sealed class RejectAfterWatering : Part
        {
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction" && CropWateringService.IsCommand(e.GetStringParameter("Command")))
                    throw new InvalidOperationException("Rollback new water and its wet appearance.");
                return true;
            }
        }
    }
}
