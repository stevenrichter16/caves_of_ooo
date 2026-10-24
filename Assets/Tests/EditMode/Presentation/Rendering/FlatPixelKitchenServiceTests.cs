using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // New presentation only: actual factory commissions and submitted tiles,
    // never a manually assigned Working/Ready positive or manual transition dirty.
    public sealed class FlatPixelKitchenServiceTests
    {
        const string Resource = "Sprites/Live16x24/kitchen-service";
        const string BaseResource = "Sprites/Live16x24/east-kitchen";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        delegate void Reconcile(Zone zone, Action<int, int, string> dirty);
        static object Get(object owner, string name) => owner.GetType().GetField(name, I).GetValue(owner);
        static void Set(object owner, string name, object value) => owner.GetType().GetField(name, I).SetValue(owner, value);
        static void Frame(Tile tile, string path, int frame)
        {
            Assert.NotNull(tile, "The already submitted foreground must exist.");
            var texture = Resources.Load<Texture2D>(path); Assert.NotNull(texture, "Actual optional resource is absent: " + path);
            Assert.AreSame(texture, tile.sprite.texture, "The submitted world image must reflect this actual job.");
            Assert.AreEqual(new Rect(frame * 16, 0, 16, 24), tile.sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
        }
        static void StateFrame(Fixture f, Entity owner, int frame) => Frame(f.Paint.At(owner), Resource, frame);
        static void Neutral(Fixture f, Entity owner, int frame) => Frame(f.Paint.At(owner), BaseResource, frame);
        static void QueryFrame(Fixture f, Entity owner, int frame)
        {
            var at = f.Zone.GetEntityCell(owner);
            Assert.True(f.Paint.Catalog.TryResolvePresentation(f.Zone, at.X, at.Y, out var result));
            Assert.AreSame(owner, result.Owner); Frame(result.ForegroundTile, Resource, frame);
        }

        [Test] public void ActualPaidStartAndCompletionRepaintSubmittedPanAndBoard()
        {
            using (var f = new Fixture())
            {
                Neutral(f, f.Pan, 5); Neutral(f, f.Pickup, 7);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                f.Start(); Assert.AreEqual(48, TradeSystem.GetDrams(f.Player)); Assert.AreEqual(102, TradeSystem.GetDrams(f.Worker));
                Assert.AreEqual(3, f.Batch.InputCounts.Sum()); Assert.AreEqual(2, f.Stored.Contents.Count);
                f.Paint.Frame(); f.Paint.Quiet(); StateFrame(f, f.Pan, 0); Neutral(f, f.Pickup, 7);
                f.Advance(119); f.Paint.Frame(); StateFrame(f, f.Pan, 0); Assert.AreEqual("Working", f.Batch.State);
                f.Advance(1); f.Paint.Frame(); f.Paint.Quiet(); Neutral(f, f.Pan, 5); StateFrame(f, f.Pickup, 1);
                Assert.IsEmpty(f.Stored.Contents); Assert.AreSame(f.Batch.Output, f.Finished.Contents.Single());
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
            }
        }
        [Test] public void PickupCapacityDelaysTheRealJobWithoutInventingAReadyParcel()
        {
            using (var f = new Fixture())
            {
                f.Start(); f.Paint.Frame(); StateFrame(f, f.Pan, 0);
                f.Finished.MaxItems = 0; f.Advance(120); f.Paint.Frame();
                Assert.AreEqual("Working", f.Batch.State); Assert.AreEqual("pickup-full", f.Batch.BlockedReason);
                StateFrame(f, f.Pan, 0); Neutral(f, f.Pickup, 7); Assert.AreEqual(3, f.Batch.InputCounts.Sum());
                f.Finished.MaxItems = 8; f.Advance(1); f.Paint.Frame(); StateFrame(f, f.Pickup, 1);
            }
        }
        [Test] public void PartialCommandTakeRetiresReadyTileBeforeAReconcileOrTurn()
        {
            using (var f = new Fixture())
            {
                f.Ready(); StateFrame(f, f.Pickup, 1);
                var meal = f.Batch.Output; var unrelated = f.Factory.CreateEntity("Dagger"); Assert.True(f.Finished.AddItem(unrelated));
                f.Paint.Quiet(); int tick = WorldClock.CurrentTick; long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(f.Pickup, meal), f.Player, f.Zone).Success);
                Assert.AreEqual("Ready", f.Batch.State, "No player-boundary reconciliation has run.");
                Assert.AreEqual(tick, WorldClock.CurrentTick); Assert.AreSame(unrelated, f.Finished.Contents.Single());
                Assert.AreSame(f.Player, meal.GetPart<PhysicsPart>().InInventory); f.Paint.Quiet();
                f.Paint.Frame(); f.Paint.Quiet(); Neutral(f, f.Pickup, 7);
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                f.Batch.Reconcile(f.Zone); f.Paint.Frame(); Assert.AreEqual("Idle", f.Batch.State); Neutral(f, f.Pickup, 7);
                Assert.AreEqual(1, f.Pack.Objects.Count(e => e == meal));
            }
        }
        [Test] public void RemovingACommittedIngredientQuietlyRetiresCoveredPanAndCancellationDoesNotRestoreIt()
        {
            using (var f = new Fixture())
            {
                f.Start(); f.Paint.Frame(); StateFrame(f, f.Pan, 0); f.Paint.Quiet();
                var pulp = f.Stored.Contents.Single(e => e.BlueprintName == "ClaspbeanPulp");
                Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(f.Escrow, pulp), f.Player, f.Zone).Success);
                Assert.AreEqual("Working", f.Batch.State); f.Paint.Quiet(); f.Paint.Frame(); Neutral(f, f.Pan, 5);
                f.Batch.Reconcile(f.Zone); f.Paint.Frame(); Assert.AreEqual("Idle", f.Batch.State);
                var cell = f.Zone.GetEntityCell(f.Pan); var grain = cell.Objects.Single(e => e.BlueprintName == "Emberwheat");
                Assert.AreEqual(2, grain.GetPart<StackerPart>().StackCount, "Cancellation publishes remaining physical grain.");
                Assert.True(f.Paint.Catalog.TryResolvePresentation(f.Zone, cell.X, cell.Y, out var current));
                Assert.AreSame(grain, current.Owner); Assert.AreSame(current.ForegroundTile, f.Paint.At(f.Pan), "Returned grain legitimately covers the pan.");
                using (var portrait = new ExaminePortraitRenderer())
                {
                    var document = ExamineDocumentBuilder.BuildWorld(f.Pan, f.Zone, cell); Assert.NotNull(document);
                    var exactPan = portrait.GetPortrait(document, f.Paint.Renderer);
                    Assert.AreSame(Resources.Load<Texture2D>(BaseResource), exactPan.Texture);
                    Assert.AreEqual(new Rect(80f / 288, 0, 16f / 288, 1), exactPan.Uv);
                }
                Assert.IsEmpty(f.Finished.Contents); Assert.AreEqual(48, TradeSystem.GetDrams(f.Player));
            }
        }
        [Test] public void CompletedParcelSurvivesActualCookDeathButDisappearsOnTransfer()
        {
            using (var f = new Fixture())
            {
                f.Ready(); StateFrame(f, f.Pickup, 1); var meal = f.Batch.Output;
                CombatSystem.HandleDeath(f.Worker, null, f.Zone); Assert.Null(f.Zone.GetEntityCell(f.Worker));
                f.Paint.Frame(); StateFrame(f, f.Pickup, 1); Assert.AreSame(meal, f.Finished.Contents.Single());
                Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(f.Pickup, meal), f.Player, f.Zone).Success);
                f.Paint.Frame(); Neutral(f, f.Pickup, 7); Assert.AreEqual("Ready", f.Batch.State, "Dead worker does not authorize a renderer to repair/reset the job.");
            }
        }
        [Test] public void ArbitraryJobStringsAndMealsNeverAuthorizeTheTwoCues()
        {
            using (var f = new Fixture())
            {
                f.Batch.State = "Working"; f.Paint.Renderer.MarkDirty("fixture forged state"); f.Paint.Frame(); Neutral(f, f.Pan, 5);
                f.Batch.State = "Ready"; Assert.True(f.Finished.AddItem(f.Factory.CreateEntity("FieldMeal")));
                f.Paint.Renderer.MarkDirty("fixture unrelated meal"); f.Paint.Frame(); Neutral(f, f.Pickup, 7);
                f.Finished.Locked = true; f.Paint.Frame(); Neutral(f, f.Pickup, 7);
            }
        }
        [TestCase("duplicate-record")] [TestCase("record-count")] [TestCase("foreign-container")]
        [TestCase("missing-pickup")] [TestCase("missing-escrow")]
        public void WorkingRequiresExactDistinctEscrowRecordsAndQuietRecovery(string fault)
        {
            using (var f = new Fixture())
            {
                f.Start(); f.Paint.Frame(); StateFrame(f, f.Pan, 0);
                var inputs = f.Batch.Inputs.ToArray(); var ids = f.Batch.InputIDs.ToArray(); var counts = f.Batch.InputCounts.ToArray();
                if (fault == "duplicate-record") { f.Batch.Inputs[1] = inputs[0]; f.Batch.InputIDs[1] = ids[0]; f.Batch.InputCounts[1] = counts[0]; }
                if (fault == "record-count") f.Batch.InputCounts[0]++;
                if (fault == "foreign-container") f.Stored.ParentEntity = new Entity();
                if (fault == "missing-pickup") f.Batch.Pickup = null;
                if (fault == "missing-escrow") f.Batch.Escrow = null;
                f.Paint.Quiet(); f.Paint.Frame(); Neutral(f, f.Pan, 5);
                f.Stored.ParentEntity = f.Escrow;
                f.Batch.Pickup = f.Pickup; f.Batch.Escrow = f.Escrow;
                for (int n = 0; n < inputs.Length; n++) { f.Batch.Inputs[n] = inputs[n]; f.Batch.InputIDs[n] = ids[n]; f.Batch.InputCounts[n] = counts[n]; }
                f.Paint.Quiet(); f.Paint.Frame(); StateFrame(f, f.Pan, 0);
            }
        }
        [TestCase("output-id")] [TestCase("provenance")] [TestCase("duplicate-id")]
        [TestCase("foreign-fieldmeal")] [TestCase("foreign-container")]
        public void ReadyRequiresExactOutputCustodyAndProvenance(string fault)
        {
            using (var f = new Fixture())
            {
                f.Ready(); StateFrame(f, f.Pickup, 1); var meal = f.Batch.Output;
                if (fault == "output-id") f.Batch.OutputID = "not-this-output";
                if (fault == "provenance") meal.IntProperties["KitchenBatch.Ordinal"]++;
                if (fault == "duplicate-id") { var other = f.Factory.CreateEntity("FieldMeal"); other.ID = meal.ID; f.Finished.Contents.Add(other); }
                if (fault == "foreign-fieldmeal") meal.GetPart<FieldMealPart>().ParentEntity = new Entity();
                if (fault == "foreign-container") f.Finished.ParentEntity = new Entity();
                f.Paint.Quiet(); f.Paint.Frame();
                if (fault == "foreign-container")
                    Assert.AreNotSame(Resources.Load<Texture2D>(Resource), f.Paint.At(f.Pickup).sprite.texture,
                        "Foreign ownership must retain the existing glyph fallback, not borrow neutral furniture art.");
                else Neutral(f, f.Pickup, 7);
            }
        }
        [Test] public void ActualCurrentPortraitAndWorldAgreeAndRepeatedReadsArePure()
        {
            using (var f = new Fixture())
            using (var portrait = new ExaminePortraitRenderer())
            {
                f.Start(); f.Paint.Frame(); StateFrame(f, f.Pan, 0);
                var cell = f.Zone.GetEntityCell(f.Pan); var document = ExamineDocumentBuilder.BuildWorld(f.Pan, f.Zone, cell);
                Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                var inputs = f.Stored.Contents.ToArray(); int tick = WorldClock.CurrentTick, ordinal = f.Batch.JobOrdinal, due = f.Batch.DueTick;
                for (int n = 0; n < 20; n++)
                {
                    QueryFrame(f, f.Pan, 0); var result = portrait.GetPortrait(document, f.Paint.Renderer);
                    Assert.AreSame(Resources.Load<Texture2D>(Resource), result.Texture); Assert.False(result.IsModel);
                }
                CollectionAssert.AreEqual(inputs, f.Stored.Contents); Assert.AreEqual("Working", f.Batch.State);
                Assert.AreEqual(tick, WorldClock.CurrentTick); Assert.AreEqual(ordinal, f.Batch.JobOrdinal); Assert.AreEqual(due, f.Batch.DueTick);
                Assert.AreEqual(48, TradeSystem.GetDrams(f.Player)); Assert.AreEqual(102, TradeSystem.GetDrams(f.Worker)); Assert.IsEmpty(f.Finished.Contents);
                cell.IsVisible = false; Assert.IsNull(portrait.GetPortrait(document, f.Paint.Renderer).Texture);
            }
        }
        [Test] public void HiddenAndCoveredOwnersCannotLeaveAStaleParcelWhenTheyReappear()
        {
            using (var f = new Fixture())
            {
                f.Ready(); StateFrame(f, f.Pickup, 1); var cell = f.Zone.GetEntityCell(f.Pickup);
                var cover = f.Factory.CreateEntity("HealingTonic"); cover.GetPart<RenderPart>().RenderLayer = 100;
                Assert.True(f.Zone.AddEntity(cover, cell.X, cell.Y)); f.Paint.Frame();
                Assert.AreNotSame(Resources.Load<Texture2D>(Resource), f.Paint.At(f.Pickup).sprite.texture);
                Assert.True(f.Zone.RemoveEntity(cover)); f.Paint.Frame(); StateFrame(f, f.Pickup, 1);
                cell.IsVisible = false; f.Paint.Renderer.MarkCellDirty(cell.X, cell.Y, "FOV counter"); f.Paint.Frame(); Assert.Null(f.Paint.At(f.Pickup));
                var meal = f.Batch.Output; Assert.True(f.Finished.RemoveItem(meal)); Assert.True(f.Pack.AddObject(meal)); f.Paint.Frame();
                cell.IsVisible = true; f.Paint.Renderer.MarkCellDirty(cell.X, cell.Y, "FOV reveal"); f.Paint.Frame(); Neutral(f, f.Pickup, 7);
            }
        }
        [TestCase("removed")] [TestCase("replaced-cell")] [TestCase("replaced-graph")]
        public void ObservedOwnerLossRetiresCueAndCannotAttachToSameId(string fault)
        {
            using (var f = new Fixture())
            {
                f.Ready(); StateFrame(f, f.Pickup, 1); var cell = f.Zone.GetEntityCell(f.Pickup);
                if (fault == "removed")
                {
                    Assert.True(f.Zone.RemoveEntity(f.Pickup)); var replacement = f.Factory.CreateEntity("ConnectedKitchenPickup"); replacement.ID = f.Pickup.ID;
                    Assert.True(f.Zone.AddEntity(replacement, cell.X, cell.Y));
                }
                if (fault == "replaced-cell") f.Zone.Cells[cell.X, cell.Y] = new Cell(cell.X, cell.Y, f.Zone) { Explored = true, IsVisible = true };
                if (fault == "replaced-graph") f.Manager.CachedZones[f.Zone.ZoneID] = new Zone(f.Zone.ZoneID);
                f.Paint.Frame(); var result = f.Paint.At(cell.X, cell.Y);
                Assert.True(result == null || result.sprite.texture != Resources.Load<Texture2D>(Resource));
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void UnchangedVisibleServiceOwnersDoNotRepaintOnQuietFrames(bool ready)
        {
            using (var f = new Fixture())
            {
                if (ready) { f.Ready(); StateFrame(f, f.Pickup, 1); }
                else { f.Start(); f.Paint.Frame(); StateFrame(f, f.Pan, 0); }
                f.Paint.Quiet();
                var observations = (System.Collections.IDictionary)Get(f.Paint.Catalog, "_serviceOwners");
                Assert.AreEqual(2, observations.Count, "Both actual painted owners must participate.");
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                long cells = EnvironmentSpriteRenderer.Perf.CellsResolved;
                int tick = WorldClock.CurrentTick;
                for (int i = 0; i < 20; i++)
                {
                    f.Paint.Frame(); f.Paint.Quiet();
                    Assert.AreEqual(2, observations.Count, "A quiet pass must not silently discard the observed owners.");
                }
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                Assert.AreEqual(cells, EnvironmentSpriteRenderer.Perf.CellsResolved,
                    "Stable service state must not dirty or re-resolve a cell.");
                Assert.AreEqual(tick, WorldClock.CurrentTick);
                Assert.AreEqual(ready ? "Ready" : "Working", f.Batch.State);
                if (ready) { Neutral(f, f.Pan, 5); StateFrame(f, f.Pickup, 1); }
                else { StateFrame(f, f.Pan, 0); Neutral(f, f.Pickup, 7); }
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void UnchangedWarmObserverHasNoManagedAllocationsOrRepaints(bool ready)
        {
            using (var f = new Fixture())
            {
                if (ready) { f.Ready(); StateFrame(f, f.Pickup, 1); }
                else { f.Start(); f.Paint.Frame(); StateFrame(f, f.Pan, 0); }
                Assert.AreEqual(2, ((System.Collections.IDictionary)Get(f.Paint.Catalog, "_serviceOwners")).Count,
                    "Both actual painted fixtures must remain observed before measuring the pass.");
                RequireAllocationCounter();
                var method = typeof(FlatPixelGreyboxCatalog).GetMethod("ReconcileObservedOwners", I);
                var query = (Reconcile)Delegate.CreateDelegate(typeof(Reconcile), f.Paint.Catalog, method);
                int dirty = 0; Action<int, int, string> callback = (x, y, reason) => dirty++;
                for (int i = 0; i < 20; i++) query(f.Zone, callback);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 100; i++) query(f.Zone, callback);
                long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.Zero(bytes); Assert.Zero(dirty);
                Assert.AreEqual(2, ((System.Collections.IDictionary)Get(f.Paint.Catalog, "_serviceOwners")).Count);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void WarmServiceSelectorHasNoManagedAllocations(bool ready)
        {
            using (var f = new Fixture())
            {
                if (ready) f.Ready(); else { f.Start(); f.Paint.Frame(); }
                var art = (FlatPixelEastKitchenCatalog)Get(f.Paint.Catalog, "_eastKitchenArt"); Assert.NotNull(art);
                Assert.True(KitchenBatchPart.TryGetPresentation(f.Zone, ready ? f.Pickup : f.Pan, out var state));
                Assert.AreEqual(ready ? KitchenBatchPresentation.ReadyParcel : KitchenBatchPresentation.CoveredWorking, state);
                RequireAllocationCounter();
                for (int i = 0; i < 20; i++) { art.TryResolvePresentation(f.Zone, f.Pan, out _); art.TryResolvePresentation(f.Zone, f.Pickup, out _); }
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 100; i++) { art.TryResolvePresentation(f.Zone, f.Pan, out _); art.TryResolvePresentation(f.Zone, f.Pickup, out _); }
                long bytes = GC.GetAllocatedBytesForCurrentThread() - before; Assert.Zero(bytes);
            }
        }
        [Test] public void AllocationMeasurementDetectsARetainedManagedAllocation()
            => RequireAllocationCounter();
        static void RequireAllocationCounter()
        {
            long before = GC.GetAllocatedBytesForCurrentThread(); var allocation = new byte[4096];
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before; GC.KeepAlive(allocation);
            if (bytes < 4096) Assert.Ignore("This Unity runtime does not report retained managed allocations via GC.GetAllocatedBytesForCurrentThread; zero is not performance evidence.");
        }
        [TestCase("Masonry", true)] [TestCase("  Masonry\t", true)]
        [TestCase("Iron, Masonry ,Wood", true)] [TestCase("Iron,,\tMasonry\n,,", true)]
        [TestCase("\u2003Masonry\u00A0", true)] [TestCase("MasonryPatch", false)]
        [TestCase("SandMasonry", false)] [TestCase("masonry", false)]
        [TestCase("", false)] [TestCase(null, false)]
        public void MaterialTokensPreserveExactTrimmedOrdinalSemantics(string raw, bool expected)
        {
            using (var f = new Fixture())
            {
                f.Start(); f.Paint.Frame(); StateFrame(f, f.Pan, 0);
                var material = f.Pan.GetPart<CompositionPart>(); material.MaterialsRaw = raw;
                Assert.AreEqual(expected, material.Contains("Masonry"), "Existing repair semantics are the independent control.");
                Assert.AreEqual(expected, KitchenBatchPart.TryGetPresentation(f.Zone, f.Pan, out _));
                f.Paint.Frame();
                if (expected) StateFrame(f, f.Pan, 0);
                else Assert.AreNotSame(Resources.Load<Texture2D>(Resource), f.Paint.At(f.Pan).sprite.texture);
            }
        }
        [Test] public void OptionalThirdTextureIsCachedIndependentAndBorrowed()
        {
            var baseline = new Texture2D(288, 24); var cook = new Texture2D(16, 24); var states = new Texture2D(32, 24);
            IDisposable art = null;
            try
            {
                var ctor = typeof(FlatPixelEastKitchenCatalog).GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D), typeof(Texture2D) });
                Assert.NotNull(ctor, "Optional service art requires an isolated third borrowed group.");
                art = (IDisposable)ctor.Invoke(new object[] { baseline, cook, states });
                using (var f = new Fixture())
                {
                    f.Paint.Catalog.Dispose(); // Replace the owned test catalog before injecting borrowed artwork.
                    f.Paint.ReplaceCatalog(new FlatPixelGreyboxCatalog((Texture2D)null)); Set(f.Paint.Catalog, "_eastKitchenArt", art);
                    f.Start(); f.Paint.Renderer.MarkDirty("new injected catalog"); f.Paint.Frame();
                    var tile = f.Paint.At(f.Pan); Assert.AreSame(states, tile.sprite.texture); Assert.AreEqual(new Rect(0, 0, 16, 24), tile.sprite.rect);
                    QuerySame(f, tile); art.Dispose(); Assert.True(tile == null); Assert.True(states != null); Assert.True(baseline != null); Assert.True(cook != null);
                }
            }
            finally { art?.Dispose(); Object.DestroyImmediate(states); Object.DestroyImmediate(cook); Object.DestroyImmediate(baseline); }
        }
        static void QuerySame(Fixture f, Tile expected)
        { var cell = f.Zone.GetEntityCell(f.Pan); Assert.True(f.Paint.Catalog.TryResolvePresentation(f.Zone, cell.X, cell.Y, out var result)); Assert.AreSame(expected, result.ForegroundTile); }
        [Test] public void OriginalTwoTextureInjectionStaysNeutralEvenWhenDefaultResourceExists()
        {
            var baseline = new Texture2D(288, 24);
            try
            {
                using (var f = new Fixture())
                {
                    f.Paint.Catalog.Dispose(); f.Paint.ReplaceCatalog(new FlatPixelGreyboxCatalog((Texture2D)null));
                    Set(f.Paint.Catalog, "_eastKitchenArt", new FlatPixelEastKitchenCatalog(baseline, null));
                    f.Start(); f.Paint.Renderer.MarkDirty("old deterministic injection"); f.Paint.Frame();
                    Assert.AreSame(baseline, f.Paint.At(f.Pan).sprite.texture); Assert.AreEqual(new Rect(80, 0, 16, 24), f.Paint.At(f.Pan).sprite.rect);
                    f.Advance(120); f.Paint.Frame(); Assert.AreSame(baseline, f.Paint.At(f.Pickup).sprite.texture);
                    Assert.AreEqual(new Rect(112, 0, 16, 24), f.Paint.At(f.Pickup).sprite.rect);
                }
            }
            finally { Object.DestroyImmediate(baseline); }
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedServiceAtlasRetainsNeutralPeerWithoutResourceFallback(bool malformed)
        {
            var baseline = new Texture2D(288, 24); Texture2D states = malformed ? new Texture2D(31, 24) : null;
            try
            {
                var ctor = typeof(FlatPixelEastKitchenCatalog).GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D), typeof(Texture2D) }); Assert.NotNull(ctor);
                using (var f = new Fixture())
                {
                    f.Paint.Catalog.Dispose(); f.Paint.ReplaceCatalog(new FlatPixelGreyboxCatalog((Texture2D)null));
                    Set(f.Paint.Catalog, "_eastKitchenArt", ctor.Invoke(new object[] { baseline, null, states }));
                    f.Start(); f.Paint.Renderer.MarkDirty("injected optional absence"); f.Paint.Frame();
                    var tile = f.Paint.At(f.Pan); Assert.AreSame(baseline, tile.sprite.texture); Assert.AreEqual(new Rect(80, 0, 16, 24), tile.sprite.rect);
                    f.Advance(120); f.Paint.Frame(); tile = f.Paint.At(f.Pickup); Assert.AreSame(baseline, tile.sprite.texture); Assert.AreEqual(new Rect(112, 0, 16, 24), tile.sprite.rect);
                }
            }
            finally { if (states != null) Object.DestroyImmediate(states); Object.DestroyImmediate(baseline); }
        }
        [Test] public void ActualResourceHasTwoNativeFullFramesAndDefaultBinding()
        {
            var texture = Resources.Load<Texture2D>(Resource); Assert.NotNull(texture);
            Assert.AreEqual(32, texture.width); Assert.AreEqual(24, texture.height); Assert.AreEqual(FilterMode.Point, texture.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode); Assert.AreEqual(1, texture.mipmapCount);
            using (var f = new Fixture()) { f.Start(); f.Paint.Frame(); StateFrame(f, f.Pan, 0); f.Advance(120); f.Paint.Frame(); StateFrame(f, f.Pickup, 1); }
        }

        sealed class Fixture : IDisposable
        {
            readonly DensityLootTestScope content;
            readonly TurnManager oldClock = TurnManager.Active;
            readonly Zone oldZone = SettlementRuntime.ActiveZone;
            readonly EntityFactory oldFactory = CropSystem.Factory;
            readonly Dictionary<PropertyInfo, object> visualHooks = typeof(EntityVisualHooks).GetProperties(S)
                .Where(p => p.CanRead && p.CanWrite).ToDictionary(p => p, p => p.GetValue(null));
            public readonly EntityFactory Factory; public readonly Zone Zone; public readonly OverworldZoneManager Manager;
            public readonly Entity Player, Worker, Pan, Escrow, Pickup; public readonly KitchenBatchPart Batch; public readonly TurnManager Clock;
            public readonly PaintFixture Paint;
            public InventoryPart Pack => Player.GetPart<InventoryPart>(); public ContainerPart Stored => Escrow.GetPart<ContainerPart>(); public ContainerPart Finished => Pickup.GetPart<ContainerPart>();
            public Fixture()
            {
                try
                {
                    content = new DensityLootTestScope(); Factory = content.Factory; Manager = OverworldZoneManager.CreateDetached(Factory, 65, true);
                    Zone = new Zone(KitchenBatchPart.KitchenZoneID); Manager.SetActiveZone(Zone);
                    Player = Factory.CreateEntity("Player"); Worker = Factory.CreateEntity("SpreadWaysideCook"); Pan = Factory.CreateEntity("ConnectedBatchPan");
                    Escrow = Factory.CreateEntity("ConnectedKitchenEscrow"); Pickup = Factory.CreateEntity("ConnectedKitchenPickup");
                    Pack.MaxWeight = 1000; Player.IntProperties[TradeSystem.CURRENCY_PROP] = 50; Worker.IntProperties[TradeSystem.CURRENCY_PROP] = 100;
                    Pan.GetPart<RepairablePart>().Repaired = true;
                    Assert.True(Zone.AddEntity(Player, 9, 10)); Assert.True(Zone.AddEntity(Pan, 10, 10)); Assert.True(Zone.AddEntity(Worker, 10, 9));
                    Assert.True(Zone.AddEntity(Escrow, 11, 10)); Assert.True(Zone.AddEntity(Pickup, 10, 11));
                    for (int y = 8; y < 14; y++) for (int x = 8; x < 14; x++) Zone.GetCell(x, y).Explored = Zone.GetCell(x, y).IsVisible = true;
                    Batch = Pan.GetPart<KitchenBatchPart>(); Assert.True(Batch.Configure(Zone, Worker, Escrow, Pickup));
                    Give("Emberwheat", 2); Give("ClaspbeanPulp", 1);
                    Paint = new PaintFixture(Zone); Clock = new TurnManager(); SettlementRuntime.ActiveZone = Zone; CropSystem.Factory = Factory;
                    CorpsePart.Factory = Factory; EntityVisualHooks.Reset();
                    Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(Zone));
                }
                catch { Dispose(); throw; }
            }
            void Give(string blueprint, int count) { var item = Factory.CreateEntity(blueprint); item.GetPart<StackerPart>().StackCount = count; Assert.True(Pack.AddObject(item)); }
            public void Start() { Assert.True(Batch.TryStart(Player, Zone), "Real canonical factory batch must commit."); Assert.AreEqual("Working", Batch.State); }
            public void Advance(int ticks) { Clock.AdvanceClock(ticks); Batch.Reconcile(Zone); }
            public void Ready() { Start(); Paint.Frame(); Advance(120); Assert.AreEqual("Ready", Batch.State); Paint.Frame(); Paint.Quiet(); }
            public void Dispose()
            {
                try { Paint?.Dispose(); content?.Dispose(); }
                finally
                {
                    CropSystem.Factory = oldFactory; SettlementRuntime.ActiveZone = oldZone; typeof(TurnManager).GetProperty("Active", S).SetValue(null, oldClock);
                    foreach (var hook in visualHooks) hook.Key.SetValue(null, hook.Value);
                }
            }
        }

        sealed class PaintFixture : IDisposable
        {
            readonly HotbarSaveFixture saved; readonly GameObject grid; readonly EnvironmentSpriteRenderer env; readonly Tilemap overlay;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", S); readonly object oldBound;
            readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback; readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries(); readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue; readonly Action<string> oldMessage = MessageLog.OnMessage; readonly Func<int> oldTick = MessageLog.TickProvider;
            public FlatPixelGreyboxCatalog Catalog { get; private set; } public readonly Zone Zone; public readonly ZoneRenderer Renderer;
            public PaintFixture(Zone zone)
            {
                oldBound = bound.GetValue(null); if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    MessageLog.OnMessage = null; MessageLog.TickProvider = null; saved = new HotbarSaveFixture(); grid = new GameObject("Kitchen service Grid", typeof(Grid)); saved.Root.transform.SetParent(grid.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); var bg = new GameObject("Kitchen background", typeof(Tilemap)); bg.transform.SetParent(saved.Root.transform, false);
                    Renderer = saved.Renderer; Set(Renderer, "_tilemap", main); Set(Renderer, "_bgTilemap", bg.GetComponent<Tilemap>());
                    var camera = new GameObject("Kitchen camera", typeof(Camera)); camera.transform.SetParent(saved.Root.transform, false); camera.GetComponent<Camera>().orthographic = true; Set(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(saved.Root.transform, main, bg.GetComponent<Tilemap>()); env.RenderingEnabled = true;
                    ReplaceCatalog(new FlatPixelGreyboxCatalog()); overlay = (Tilemap)Get(env, "_overlayTilemap");
                    Set(Renderer, "_envSpriteRenderer", env); Set(Renderer, "_flatPixelModeEnabled", true); Renderer.PlayerEntity = null; Zone = zone; Renderer.SetZone(zone);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty; Frame(); Quiet();
                }
                catch { Dispose(); throw; }
            }
            public void ReplaceCatalog(FlatPixelGreyboxCatalog catalog) { Catalog = catalog; Set(env, "_flatPixelGreybox", catalog); }
            public Tile At(Entity owner) { var cell = Zone.GetEntityCell(owner); return At(cell.X, cell.Y); }
            public Tile At(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, CavesOfOoo.Core.Zone.Height - 1 - y, 0));
            public void Frame() { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Quiet() { Assert.False((bool)Get(Renderer, "_fullDirty")); Assert.Zero(((HashSet<int>)Get(Renderer, "_dirtyCells")).Count); }
            public void Dispose()
            {
                try { saved?.Dispose(); if (grid != null) Object.DestroyImmediate(grid); }
                finally
                {
                    Catalog?.Dispose(); ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var item in perf) item.Key.SetValue(null, item.Value);
                    MessageLog.Restore(messages, announcements, flash, serial); MessageLog.OnMessage = oldMessage; MessageLog.TickProvider = oldTick;
                }
            }
        }
    }
}
