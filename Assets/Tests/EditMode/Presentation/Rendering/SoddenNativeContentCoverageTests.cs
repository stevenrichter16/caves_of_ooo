using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Deliberately introduced native owners close categories absent
    /// from the natural-zone census. This is receiving-region presentation and
    /// lifecycle evidence, not proof of acquisition rates or a player journey.</summary>
    public sealed class SoddenNativeContentCoverageTests
    {
        const string Bog = "Overworld.15.7.0";
        public static readonly string[] Crops = { "Sumpsieve", "Drowsebell", "Chillcress", "Slipsedge", "Peatlantern" };
        static SpawnRing3DPresenter Presenter(SpawnRing3DIntegrationFixture f) => (SpawnRing3DPresenter)f.Presenter;
        static SpreadBiomeStyleEvidence Approved(SpawnRing3DIntegrationFixture f, Entity owner, string expected = null)
        {
            Assert.True(f.Authored(owner), owner.BlueprintName);
            Assert.True(f.Rendered(owner), owner.BlueprintName);
            Assert.True(Presenter(f).TryGetApprovedStyle(owner, out var proof), owner.BlueprintName + ": " + proof.Failure);
            if (expected != null) Assert.AreEqual(expected, proof.ModelId);
            Assert.Greater(proof.PieceCount, 0); Assert.NotNull(proof.ExpectedMesh); Assert.NotNull(proof.SubmittedMesh);
            Assert.Greater(proof.SubmittedMesh.vertexCount, 0);
            return proof;
        }
        static void Stage(Entity owner, int stage, int moisture)
        {
            var crop = owner.GetPart<CropPart>(); crop.GrowthStage = stage; crop.MoistureTicks = moisture;
            var render = owner.GetPart<RenderPart>();
            render.RenderString = crop.GlyphForStage(stage).ToString(); render.ColorString = crop.ColorForStage(stage);
        }
        static string Model(string species, string state) => "biome-crop-" + species.ToLowerInvariant() + "-" + state;
        static void Absent(SpawnRing3DIntegrationFixture f, Entity owner)
        {
            Assert.False(Presenter(f).TryGetApprovedStyle(owner, out _));
            f.Refresh(); Assert.False(f.Rendered(owner)); Assert.False(f.Authored(owner));
        }

        [TestCaseSource(nameof(Crops))]
        public void EverySoddenCropHasApprovedCurrentGeometryForAllSixNativeStates(string species)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var owner = f.Add(species + "Crop"); var cell = f.Zone.GetEntityCell(owner);
                var parts = owner.Parts.ToArray(); var crop = owner.GetPart<CropPart>();
                var meshes = new Mesh[3];
                for (int stage = 0; stage < 3; stage++)
                {
                    Stage(owner, stage, 0); int version = f.Zone.EntityVersion;
                    f.Refresh(f.Dirty(owner)); var dry = Approved(f, owner, Model(species, stage + "-dry"));
                    meshes[stage] = dry.ExpectedMesh;
                    Stage(owner, stage, 3); f.Refresh(f.Dirty(owner));
                    var wet = Approved(f, owner, Model(species, stage + "-wet"));
                    Assert.AreNotSame(dry.ExpectedMesh, wet.ExpectedMesh);
                    Assert.AreEqual(stage, crop.GrowthStage); Assert.AreEqual(3, crop.MoistureTicks);
                    Assert.AreEqual(version, f.Zone.EntityVersion); Assert.AreSame(cell, f.Zone.GetEntityCell(owner));
                    CollectionAssert.AreEqual(parts, owner.Parts);
                }
                Assert.AreEqual(3, meshes.Distinct().Count(), "A ripe plant cannot reuse the seed-stage mesh.");
                Assert.True(f.Zone.RemoveEntity(owner)); Absent(f, owner);
            }
        }

        [TestCaseSource(nameof(Crops))]
        public void UsefulSoddenHarvestAndSeedKeepTheirIdentityAcrossNativePickupAndDrop(string species)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var crop = f.Factory.CreateEntity(species + "Crop").GetPart<CropPart>();
                foreach (var pair in new[] { new[] { crop.SeedYieldBlueprint, "seed" }, new[] { crop.YieldBlueprint, "harvest" } })
                {
                    var owner = f.Add(pair[0]); f.Refresh(); var before = Approved(f, owner, Model(species, pair[1]));
                    string id = owner.ID; f.Approach(owner); Assert.True(InventorySystem.Pickup(f.Player, owner, f.Zone));
                    Absent(f, owner);
                    var pack = f.Player.GetPart<InventoryPart>(); var physics = owner.GetPart<PhysicsPart>();
                    if (pack.Objects.Contains(owner))
                    { Assert.AreSame(f.Player, physics.InInventory); Assert.IsNull(physics.Equipped); }
                    else
                    {
                        // A peatlantern cup is a real holdable light: native
                        // pickup can use its automatic free-hand equipment path.
                        Assert.AreSame(f.Player, physics.Equipped); Assert.IsNull(physics.InInventory);
                        Assert.True(pack.EquippedItems.ContainsValue(owner));
                        var slot = pack.FindEquippedBodyPart(owner); Assert.NotNull(slot);
                        Assert.AreSame(owner, slot._Equipped); Assert.AreSame(f.Player.GetPart<Body>(), slot.ParentBody);
                    }
                    Assert.True(InventorySystem.Drop(f.Player, owner, f.Zone)); f.Refresh();
                    var after = Approved(f, owner, Model(species, pair[1]));
                    Assert.AreSame(before.ExpectedMesh, after.ExpectedMesh); Assert.AreEqual(id, owner.ID);
                    Assert.True(f.Zone.RemoveEntity(owner));
                }
            }
        }

        [Test] public void ActualElapsedGrowthChangesChillcressMeshWithoutRendererAdvancingTheCrop()
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var owner = f.Add("ChillcressCrop"); var crop = owner.GetPart<CropPart>();
                Stage(owner, 0, crop.TicksPerStage * 2 + 1);
                crop.GrowthTimingVersion = CropTime.CurrentVersion; crop.LastGrowthWorldTick = 0;
                crop.GrowthWetTickRemainder = 0; crop.TicksInStage = 0;
                f.Refresh(); var seed = Approved(f, owner, Model("Chillcress", "0-wet"));
                int ticks = crop.TicksPerStage * CropTime.WorldTicksPerUnit;
                Assert.True(CropTime.Reconcile(crop, f.Zone, ticks)); f.Refresh();
                var growing = Approved(f, owner, Model("Chillcress", "1-wet"));
                Assert.True(CropTime.Reconcile(crop, f.Zone, ticks * 2)); f.Refresh();
                var ripe = Approved(f, owner, Model("Chillcress", "2-wet"));
                Assert.AreNotSame(seed.ExpectedMesh, growing.ExpectedMesh); Assert.AreNotSame(growing.ExpectedMesh, ripe.ExpectedMesh);
                Assert.AreEqual(1, crop.MoistureTicks); f.Refresh(); f.Refresh();
                Assert.AreEqual(1, crop.MoistureTicks); Assert.AreEqual(ticks * 2, crop.LastGrowthWorldTick);
                Assert.True(CropTime.Reconcile(crop, f.Zone, ticks * 2 + CropTime.WorldTicksPerUnit)); f.Refresh();
                Approved(f, owner, Model("Chillcress", "2-dry")); Assert.AreEqual(2, crop.GrowthStage);
            }
        }

        [TestCase("Dagger")] [TestCase("Torch")] [TestCase("GroundwireScreen")]
        [TestCase("IronshodBoots")] [TestCase("FilterHood")]
        public void PlayerAndCurrentEquipmentRemainApprovedThroughWearCarryAndGroundTransitions(string blueprint)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                f.CleanGear(f.Player); var item = f.Equip(f.Player, blueprint); f.Refresh();
                Approved(f, f.Player); var presenter = Presenter(f);
                Assert.True(f.Equipment(f.Player, item, out var worn)); Assert.True(SpawnRing3DIntegrationFixture.Drawn(worn));
                Assert.True(presenter.TryGetApprovedEquipmentStyle(f.Player, item, out var proof), blueprint + ": " + proof.Failure);
                Assert.Greater(proof.PieceCount, 0); Assert.False(f.Rendered(item), "Equipped items are attachments, not duplicate ground owners.");
                int version = EquipmentChangeBus.GlobalVersion; f.Refresh(); Assert.AreEqual(version, EquipmentChangeBus.GlobalVersion);
                Assert.True(InventorySystem.UnequipItem(f.Player, item));
                Assert.False(presenter.TryGetApprovedEquipmentStyle(f.Player, item, out _)); f.Refresh();
                Assert.False(f.Equipment(f.Player, item, out _)); SpawnRing3DIntegrationFixture.Hidden(worn);
                Assert.True(InventorySystem.Drop(f.Player, item, f.Zone)); f.Refresh(); Approved(f, item);
                Assert.False(presenter.TryGetApprovedEquipmentStyle(f.Player, item, out _));
                Assert.True(InventorySystem.Pickup(f.Player, item, f.Zone)); f.Refresh(); Assert.False(f.Rendered(item));
                Assert.True(InventorySystem.Equip(f.Player, item)); f.Refresh();
                Assert.True(presenter.TryGetApprovedEquipmentStyle(f.Player, item, out proof), proof.Failure);
            }
        }

        [Test] public void RealCordTriggerRetiresTheArmedMeshWhileKeepingTheRootedPlayerVisible()
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var owner = f.Add("KnotflaxSnare"); var at = f.Zone.GetEntityPosition(owner); f.Refresh();
                Approved(f, owner); Assert.True(MovementSystem.ForceMoveTo(f.Player, f.Zone, at.x, at.y));
                Assert.True(owner.GetPart<CordSnarePart>().Spent); Assert.NotNull(f.Player.GetEffect<RootedEffect>());
                Assert.IsNull(f.Zone.GetEntityCell(owner)); Absent(f, owner); Approved(f, f.Player);
            }
        }

        [Test] public void RealQuartzExpiryRetiresItsGroundGeometryAtTheSameNativeLifetimeBoundary()
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var owner = f.Add("CrackedGlowQuartz"); var lifetime = owner.GetPart<LifespanPart>();
                int turns = lifetime.TurnsRemaining; Assert.Greater(turns, 1); f.Refresh(); Approved(f, owner);
                for (int i = 0; i < turns - 1; i++)
                { var e = GameEvent.New("EndTurn"); e.SetParameter("Zone", f.Zone); owner.FireEventAndRelease(e); }
                f.Refresh(); Approved(f, owner); Assert.AreEqual(1, lifetime.TurnsRemaining);
                var last = GameEvent.New("EndTurn"); last.SetParameter("Zone", f.Zone); owner.FireEventAndRelease(last);
                Assert.AreEqual(0, lifetime.TurnsRemaining); Assert.IsNull(f.Zone.GetEntityCell(owner)); Absent(f, owner);
            }
        }

        [TestCase("crop-part")] [TestCase("yield-contract")]
        public void CurrentCropAuthorityCannotBeReplacedByForeignStateEvenAtTheSameCell(string fault)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var owner = f.Add("PeatlanternCrop"); Stage(owner, 2, 1); f.Refresh(); Approved(f, owner);
                var crop = owner.GetPart<CropPart>();
                if (fault == "crop-part") crop.ParentEntity = f.Player; else crop.YieldBlueprint = "Dagger";
                Absent(f, owner);
            }
        }

        [Test] public void ReplacedCurrentWorldGraphRevokesCropPlayerAndEquipmentEvidenceTogether()
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                var crop = f.Add("SumpsieveCrop"); f.CleanGear(f.Player); var item = f.Equip(f.Player, "Dagger"); f.Refresh();
                Approved(f, crop); Approved(f, f.Player);
                Assert.True(Presenter(f).TryGetApprovedEquipmentStyle(f.Player, item, out _));
                f.Manager.CachedZones[Bog] = new Zone(Bog);
                try
                {
                    Assert.False(Presenter(f).TryGetApprovedStyle(crop, out _));
                    Assert.False(Presenter(f).TryGetApprovedStyle(f.Player, out _));
                    Assert.False(Presenter(f).TryGetApprovedEquipmentStyle(f.Player, item, out _));
                }
                finally { f.Manager.CachedZones[Bog] = f.Zone; }
                Approved(f, crop); Approved(f, f.Player);
            }
        }

        [Test] public void ARealUnequipCannotKeepAnOldAttachmentApprovedThroughItsFormerBodySlot()
        {
            using (var f = new SpawnRing3DIntegrationFixture(Bog))
            {
                f.CleanGear(f.Player); var item = f.Equip(f.Player, "GroundwireScreen"); f.Refresh();
                Assert.True(Presenter(f).TryGetApprovedEquipmentStyle(f.Player, item, out _));
                Assert.True(InventorySystem.UnequipItem(f.Player, item));
                // Counter: a stale physics pointer alone cannot restore the
                // real inventory/body-slot ownership removed by UnequipItem.
                item.GetPart<PhysicsPart>().Equipped = f.Player;
                Assert.False(Presenter(f).TryGetApprovedEquipmentStyle(f.Player, item, out _));
                f.Refresh(); Assert.False(f.Equipment(f.Player, item, out _));
            }
        }
    }
}
