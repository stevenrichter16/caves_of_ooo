using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Imported at the assigned native RED checkpoint; production follows actual failures.
    // Native missing-API RED precedes the direct production API assertions below.
    public sealed class FlatPixelExaminePortraitTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        EntityFactory factory;
        [OneTimeSetUp] public void Load() => factory = DensityReferenceGladeTests.Factory();

        [TestCase(false, 9)] [TestCase(true, 10)]
        public void WorldTorchBorrowsTheExactCurrentStateFrameWithoutAnyPreview(bool lit, int frame)
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var owner = f.Torch(lit); var document = f.World(owner);
                AssertFrame(Get(portrait, document, f.Presentation), f.States, frame);
                AssertNoPreview(portrait);
                Assert.True(f.Catalog.TryResolve(f.Zone, 10, 10, out var world));
                Assert.AreSame(world.ForegroundTile.sprite.texture, Get(portrait, document, f.Presentation).Texture);
            }
        }

        [TestCase("wet")] [TestCase("frozen")] [TestCase("disabled")] [TestCase("empty")]
        public void SameDocumentRefreshReadsCurrentFlameWithoutChangingItsTextOrFuel(string change)
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var owner = f.Torch(true); var document = f.World(owner); string text = document.ToPlainText();
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 10);
                if (change == "wet") Assert.True(owner.ApplyEffect(new WetEffect(1)));
                if (change == "frozen") Assert.True(owner.ApplyEffect(new FrozenEffect(1)));
                if (change == "disabled") owner.GetPart<LightSourcePart>().Enabled = false;
                if (change == "empty") owner.GetPart<FuelPart>().FuelMass = 0;
                float fuel = owner.GetPart<FuelPart>().FuelMass;
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 9);
                Assert.AreEqual(fuel, owner.GetPart<FuelPart>().FuelMass);
                Assert.AreEqual(text, document.ToPlainText(), "Art refresh must not rerun or replace the prose snapshot.");
                if (change == "wet") Assert.True(owner.RemoveEffect<WetEffect>());
                if (change == "frozen") Assert.True(owner.RemoveEffect<FrozenEffect>());
                if (change == "disabled") owner.GetPart<LightSourcePart>().Enabled = true;
                if (change == "empty") owner.GetPart<FuelPart>().FuelMass = 50;
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 10);
                AssertNoPreview(portrait);
            }
        }

        [Test]
        public void ExplicitLowerOccupantPortraitDoesNotBecomeTheCellTopOwner()
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var torch = f.Torch(true); var blocker = f.Add("Warden");
                blocker.GetPart<RenderPart>().RenderLayer = 100;
                Assert.AreSame(blocker, f.Zone.GetCell(10, 10).GetTopVisibleObject());
                var document = f.World(torch);
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 10);
                AssertNoPreview(portrait);
            }
        }

        [TestCase("moved")] [TestCase("removed")] [TestCase("hidden")]
        [TestCase("fog")] [TestCase("unexplored")] [TestCase("replaced-cell")]
        [TestCase("foreign-render")] [TestCase("viewer-left")]
        public void PreviouslyValidWorldPortraitCannotSurviveLossOfCapturedAuthority(string change)
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var owner = f.Torch(true); var document = f.World(owner);
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 10);
                if (change == "moved") Assert.True(f.Zone.MoveEntity(owner, 11, 10));
                if (change == "removed") Assert.True(f.Zone.RemoveEntity(owner));
                if (change == "hidden") owner.GetPart<RenderPart>().Visible = false;
                if (change == "fog") f.Zone.GetCell(10, 10).IsVisible = false;
                if (change == "unexplored") f.Zone.GetCell(10, 10).Explored = false;
                if (change == "replaced-cell") f.Zone.Cells[10, 10] = new Cell(10, 10, f.Zone) { IsVisible = true, Explored = true };
                if (change == "foreign-render") owner.GetPart<RenderPart>().ParentEntity = new Entity();
                if (change == "viewer-left") Assert.True(f.Zone.RemoveEntity(f.Viewer));
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document));
                Assert.IsNull(Get(portrait, document, f.Presentation).Texture);
                AssertNoPreview(portrait);
            }
        }

        [Test]
        public void SameAddressDifferentGraphCannotBorrowTheCurrentFlatPresentation()
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var owner = f.Torch(true); var old = f.World(owner);
                AssertFrame(Get(portrait, old, f.Presentation), f.States, 10);
                var foreign = new Zone(ReferenceGladePlan.ZoneID);
                var other = factory.CreateEntity("Torch"); Assert.True(foreign.AddEntity(other, 10, 10));
                foreign.GetCell(10, 10).Explored = foreign.GetCell(10, 10).IsVisible = true;
                var document = ExamineDocumentBuilder.BuildWorld(other, foreign, foreign.GetCell(10, 10));
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document), "Positive visibility authority is separate from the currently presented graph.");
                Assert.IsNull(Get(portrait, document, f.Presentation).Texture);
                AssertNoPreview(portrait);
            }
        }

        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void GenuineCarriedAndEquippedTorchWorksWhileInventoryOwnsTheCanvas(bool equipped, bool paused)
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var torch = f.Torch(true); Assert.True(f.Zone.RemoveEntity(torch));
                var inventory = f.Viewer.GetPart<InventoryPart>(); Assert.True(inventory.AddObject(torch));
                if (equipped) Assert.True(inventory.Equip(torch, "Hand"));
                f.Presentation.Paused = paused;
                Assert.True(f.Presentation.FlatPixelModeActive);
                var document = ExamineDocumentBuilder.BuildInventory(torch, f.Viewer);
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                int entities = f.Zone.EntityCount, carried = inventory.Objects.Count, worn = inventory.EquippedItems.Count;
                string text = document.ToPlainText(); float fuel = torch.GetPart<FuelPart>().FuelMass;
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 10);
                Assert.IsNull(f.Zone.GetEntityCell(torch));
                Assert.AreEqual(entities, f.Zone.EntityCount); Assert.AreEqual(carried, inventory.Objects.Count);
                Assert.AreEqual(worn, inventory.EquippedItems.Count); Assert.AreEqual(text, document.ToPlainText());
                Assert.AreEqual(fuel, torch.GetPart<FuelPart>().FuelMass);
                Assert.AreSame(f.Viewer, equipped ? torch.GetPart<PhysicsPart>().Equipped : torch.GetPart<PhysicsPart>().InInventory);
                torch.GetPart<LightSourcePart>().Enabled = false;
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 9);
                AssertNoPreview(portrait);
            }
        }

        [TestCase("lost")] [TestCase("transfer")] [TestCase("hidden")] [TestCase("empty-stack")]
        public void CarriedPortraitEndsWithItsActualOwnershipNotTheDocumentReference(string change)
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var torch = f.Torch(true); Assert.True(f.Zone.RemoveEntity(torch));
                var inventory = f.Viewer.GetPart<InventoryPart>(); Assert.True(inventory.AddObject(torch));
                if (torch.GetPart<StackerPart>() == null) torch.AddPart(new StackerPart { StackCount = 1 });
                var document = ExamineDocumentBuilder.BuildInventory(torch, f.Viewer);
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 10);
                if (change == "lost") inventory.Objects.Remove(torch);
                if (change == "transfer") { inventory.Objects.Remove(torch); torch.GetPart<PhysicsPart>().InInventory = new Entity(); }
                if (change == "hidden") torch.GetPart<RenderPart>().Visible = false;
                if (change == "empty-stack") torch.GetPart<StackerPart>().StackCount = 0;
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document));
                Assert.IsNull(Get(portrait, document, f.Presentation).Texture);
                AssertNoPreview(portrait);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedStateAtlasUsesFlatFallbackAndKeepsOtherGroups(bool malformed)
        {
            using (var f = new Fixture(factory, malformed ? 191 : 0))
            using (var portrait = Probe())
            {
                var torch = f.Torch(true); var image = Get(portrait, f.World(torch), f.Presentation);
                Assert.NotNull(image.Texture); Assert.False(image.IsModel);
                Assert.AreNotSame(f.States, image.Texture); AssertNoPreview(portrait);
                var dog = f.Add("PetDog", 11, 10);
                AssertFrame(Get(portrait, f.World(dog), f.Presentation), f.Cast, 1);
            }
        }

        [Test]
        public void RepairAndPoolRefreshUseActualPartsRatherThanAnOldCachedImage()
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var well = f.Add("RepairLinedWell"); var wellDoc = f.World(well);
                well.GetPart<RepairablePart>().Repaired = false;
                AssertFrame(Get(portrait, wellDoc, f.Presentation), f.Anchors, 0);
                well.GetPart<RepairablePart>().Repaired = true;
                AssertFrame(Get(portrait, wellDoc, f.Presentation), f.Anchors, 1);
                var basin = f.Add("SpreadDrawPoint", 11, 10); var pool = basin.GetPart<LiquidPoolPart>();
                pool.Volume = 3; var basinDoc = f.World(basin);
                AssertFrame(Get(portrait, basinDoc, f.Presentation), f.Anchors, 2);
                pool.Volume = 0;
                AssertFrame(Get(portrait, basinDoc, f.Presentation), f.Anchors, 3);
                Assert.AreEqual(0, pool.Volume); Assert.True(well.GetPart<RepairablePart>().Repaired);
                AssertNoPreview(portrait);
            }
        }

        [Test]
        public void ClearingOrDisposingPortraitsCannotDestroySharedWorldFrames()
        {
            using (var f = new Fixture(factory))
            {
                var torch = f.Torch(true); var document = f.World(torch);
                Assert.True(f.Catalog.TryResolve(f.Zone, 10, 10, out var world));
                var tile = world.ForegroundTile; var sprite = tile.sprite;
                var portrait = Probe();
                try
                {
                    for (int i = 0; i < 12; i++)
                    {
                        AssertFrame(Get(portrait, document, f.Presentation), f.States, 10);
                        portrait.Clear(); AssertNoPreview(portrait);
                        Assert.True(sprite != null && tile != null && f.States != null);
                    }
                }
                finally { portrait.Dispose(); }
                Assert.True(f.Catalog.TryResolve(f.Zone, 10, 10, out var again));
                Assert.AreSame(tile, again.ForegroundTile); Assert.AreSame(sprite, again.ForegroundTile.sprite);
            }
        }

        [Test]
        public void PlainAnnouncementAndBareSubjectNeverGainPortraitAuthority()
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                Assert.IsNull(Get(portrait, null, f.Presentation).Texture);
                Assert.IsNull(Get(portrait, new AnnouncementDocument("Notice"), f.Presentation).Texture);
                Assert.IsNull(Get(portrait, new AnnouncementDocument("Bare", subject: f.Torch(true)), f.Presentation).Texture);
                AssertNoPreview(portrait);
            }
        }

        [Test]
        public void InactivePresentationPreservesTheExactLegacyModelRoute()
        {
            using (var legacy = new SpawnRing3DIntegrationFixture())
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                f.Presentation.FlatPixelModeEnabled = false;
                f.SetPresentedZone(legacy.Zone); Assert.False(f.Presentation.FlatPixelModeActive);
                var well = legacy.Add("RepairLinedWell");
                var document = ExamineDocumentBuilder.BuildWorld(well, legacy.Zone, legacy.Zone.GetEntityCell(well), legacy.Player);
                var result = Get(portrait, document, f.Presentation);
                Assert.True(result.IsModel); Assert.AreEqual("repair-cultivation-lined-well-broken", portrait.CurrentModelId);
                Assert.AreEqual(1, Read<int>(portrait, "CaptureCount"));
            }
        }

        [Test]
        public void SwitchingFromLegacyToFlatReleasesPreviewButNotBorrowedAtlas()
        {
            using (var legacy = new SpawnRing3DIntegrationFixture())
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var well = legacy.Add("RepairLinedWell"); var torch = f.Torch(true);
                var oldDoc = ExamineDocumentBuilder.BuildWorld(well, legacy.Zone, legacy.Zone.GetEntityCell(well), legacy.Player);
                var flatDoc = f.World(torch);
                for (int i = 0; i < 2; i++)
                {
                    f.Presentation.FlatPixelModeEnabled = false;
                    f.SetPresentedZone(legacy.Zone); Assert.True(Get(portrait, oldDoc, f.Presentation).IsModel);
                    var oldTarget = Read<RenderTexture>(portrait, "OwnedTarget");
                    var oldCamera = Read<Camera>(portrait, "PreviewCamera");
                    Assert.NotNull(oldTarget); Assert.NotNull(oldCamera);
                    f.SetPresentedZone(f.Zone); f.Presentation.FlatPixelModeEnabled = true;
                    AssertFrame(Get(portrait, flatDoc, f.Presentation), f.States, 10);
                    AssertNoPreview(portrait, i + 1);
                    Assert.True(oldTarget == null && oldCamera == null, "Flat mode must not retain a hidden 3D camera/target.");
                    Assert.True(f.States != null);
                }
            }
        }

        [TestCase("missing")] [TestCase("foreign")]
        public void InvalidStatePartUsesHonestFlatFallbackRatherThanAnotherNativeState(string mutation)
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var owner = f.Torch(true); var document = f.World(owner);
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 10);
                var part = owner.GetPart<TorchLightPart>();
                if (mutation == "missing") owner.RemovePart(part); else part.ParentEntity = new Entity();
                var image = Get(portrait, document, f.Presentation);
                Assert.NotNull(image.Texture); Assert.False(image.IsModel);
                Assert.AreNotSame(f.States, image.Texture); AssertNoPreview(portrait);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void ManagedGraphReplacementRevokesWorldAndInventoryPresentationAuthority(bool carried)
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var manager = new OverworldZoneManager(factory, 64); manager.SetActiveZone(f.Zone);
                Assert.True(f.Presentation.FlatPixelModeActive, "The managed seed64 graph must still be admitted.");
                var owner = f.Torch(true);
                if (carried) { Assert.True(f.Zone.RemoveEntity(owner)); Assert.True(f.Viewer.GetPart<InventoryPart>().AddObject(owner)); }
                var document = carried ? ExamineDocumentBuilder.BuildInventory(owner, f.Viewer) : f.World(owner);
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 10);
                manager.CachedZones[f.Zone.ZoneID] = new Zone(f.Zone.ZoneID);
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document), "Physical authorization alone does not select the presented world graph.");
                Assert.IsNull(Get(portrait, document, f.Presentation).Texture);
                AssertNoPreview(portrait);
            }
        }

        [TestCase("detached")] [TestCase("foreign-zone")]
        public void CarriedSubjectCannotBorrowAFlatZoneFromAnAbsentViewer(string change)
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var owner = f.Torch(true); Assert.True(f.Zone.RemoveEntity(owner));
                Assert.True(f.Viewer.GetPart<InventoryPart>().AddObject(owner));
                var document = ExamineDocumentBuilder.BuildInventory(owner, f.Viewer);
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 10);
                Assert.True(f.Zone.RemoveEntity(f.Viewer));
                if (change == "foreign-zone") Assert.True(new Zone("Overworld.12.10.0").AddEntity(f.Viewer, 9, 10));
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                Assert.IsNull(Get(portrait, document, f.Presentation).Texture);
                AssertNoPreview(portrait);
            }
        }

        [Test]
        public void AuthorizedFootprintCellUsesItsSelectedSubjectInsteadOfRequiringItsAnchor()
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var owner = f.Torch(true); owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
                var document = ExamineDocumentBuilder.BuildWorld(owner, f.Zone, f.Zone.GetCell(11, 10), f.Viewer);
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                AssertFrame(Get(portrait, document, f.Presentation), f.States, 10);
                AssertNoPreview(portrait);
            }
        }

        [Test]
        public void ExactSubjectCatalogQueryReusesBorrowedFramesWithoutWorldPlacement()
        {
            using (var f = new Fixture(factory))
            {
                var owner = f.Torch(true); Assert.True(f.Zone.RemoveEntity(owner));
                Assert.True(f.Catalog.TryGetSubjectSprite(owner, 10, 10, out var frame, out var tint));
                Assert.AreSame(f.States, frame.texture); Assert.AreEqual(Color.white, tint);
                Assert.AreEqual(new Rect(160, 0, 16, 24), frame.rect);
                Assert.IsNull(f.Zone.GetEntityCell(owner));
                owner.GetPart<RenderPart>().Visible = false;
                Assert.False(f.Catalog.TryGetSubjectSprite(owner, 10, 10, out frame, out tint)); Assert.IsNull(frame);
            }
        }

        [TestCase("subject-hidden")] [TestCase("flat-mode-left")] [TestCase("presented-zone-left")]
        public void PopupBindingUsesNativePortraitAndInvalidatesCachedImagesWithoutRecapture(string change)
        {
            using (var f = new Fixture(factory))
            using (var portrait = Probe())
            {
                var owner = f.Torch(true); var document = f.World(owner);
                var ui = f.AddPopup();
                var binding = typeof(AnnouncementUI).GetField("WorldPresentation", I);
                Assert.NotNull(binding, "Popup needs an explicit bootstrap-bound current presentation.");
                binding.SetValue(ui, f.Presentation);
                typeof(AnnouncementUI).GetField("_portraitRenderer", I).SetValue(ui, portrait);
                ui.OpenDocument(document);
                InvokeUi(ui, "RefreshPortrait");
                var imageField = typeof(AnnouncementUI).GetField("_portrait", I);
                AssertFrame((ExaminePortrait)imageField.GetValue(ui), f.States, 10);
                string prose = document.ToPlainText();
                if (change == "subject-hidden") owner.GetPart<RenderPart>().Visible = false;
                if (change == "flat-mode-left") typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(f.Presentation, false);
                if (change == "presented-zone-left") f.SetPresentedZone(new Zone(f.Zone.ZoneID));
                InvokeUi(ui, "ValidatePortrait");
                Assert.IsNull(((ExaminePortrait)imageField.GetValue(ui)).Texture);
                Assert.AreEqual(prose, ui.Document.ToPlainText()); AssertNoPreview(portrait);
            }
        }

        static void InvokeUi(AnnouncementUI ui, string name)
        {
            var method = typeof(AnnouncementUI).GetMethod(name, I);
            Assert.NotNull(method, name + " must be used by the normal popup lifecycle.");
            try { method.Invoke(ui, null); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }

        static ExaminePortrait Get(ExaminePortraitRenderer renderer, AnnouncementDocument document, ZoneRenderer presentation)
            => renderer.GetPortrait(document, presentation);
        static ExaminePortraitRenderer Probe()
            => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), I, null,
                new object[] { (Action<Camera, RenderTexture>)((camera, target) => { }) }, null);
        static T Read<T>(ExaminePortraitRenderer renderer, string name)
            => (T)typeof(ExaminePortraitRenderer).GetProperty(name, I).GetValue(renderer);
        static void AssertNoPreview(ExaminePortraitRenderer renderer, int previousCaptures = 0)
        {
            Assert.IsNull(renderer.CurrentModelId); Assert.IsNull(Read<GameObject>(renderer, "PreviewRoot"));
            Assert.IsNull(Read<Camera>(renderer, "PreviewCamera")); Assert.IsNull(Read<RenderTexture>(renderer, "OwnedTarget"));
            Assert.AreEqual(previousCaptures, Read<int>(renderer, "CaptureCount"));
        }
        static void AssertFrame(ExaminePortrait portrait, Texture2D atlas, int frame)
        {
            Assert.AreSame(atlas, portrait.Texture); Assert.False(portrait.IsModel); Assert.AreEqual(Color.white, portrait.Tint);
            Assert.AreEqual(new Rect(frame * 16f / atlas.width, 0, 16f / atlas.width, 1), portrait.Uv);
        }
        sealed class Fixture : IDisposable
        {
            readonly GameObject root;
            readonly EntityFactory factory;
            readonly Action<int, int, string> previousCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> previousFull = ZoneRenderHooks.FullDirtyCallback;
            public readonly Zone Zone = new Zone(ReferenceGladePlan.ZoneID);
            public readonly Entity Viewer;
            public readonly ZoneRenderer Presentation;
            public readonly FlatPixelGreyboxCatalog Catalog;
            public readonly Texture2D Base, Cast, Anchors, States;
            public Fixture(EntityFactory factory, int stateWidth = 192)
            {
                this.factory = factory;
                try
                {
                    ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
                    Base = Texture(160); Cast = Texture(64); Anchors = Texture(192); States = stateWidth == 0 ? null : Texture(stateWidth);
                    Catalog = new FlatPixelGreyboxCatalog(Base, Cast, Anchors, States);
                    Viewer = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "FlatExamineViewer" };
                    Viewer.SetTag("Player"); Viewer.AddPart(new PhysicsPart()); Viewer.AddPart(new InventoryPart());
                    Viewer.AddPart(new RenderPart { RenderString = "@", Visible = true });
                    Viewer.AddPart(new ExaminablePart());
                    Assert.True(Zone.AddEntity(Viewer, 9, 10));
                    for (int y = 9; y <= 11; y++) for (int x = 9; x <= 12; x++)
                        Zone.GetCell(x, y).IsVisible = Zone.GetCell(x, y).Explored = true;
                    root = new GameObject("Owned flat examine draft fixture"); root.SetActive(false);
                    Presentation = root.AddComponent<ZoneRenderer>();
                    var environment = root.AddComponent<EnvironmentSpriteRenderer>();
                    typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", I).SetValue(environment, Catalog);
                    environment.FlatPixelGreyboxEnabled = true;
                    typeof(ZoneRenderer).GetField("_envSpriteRenderer", I).SetValue(Presentation, environment);
                    typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(Presentation, true);
                    SetPresentedZone(Zone); Assert.True(Presentation.FlatPixelModeActive);
                }
                catch { Dispose(); throw; }
            }
            public AnnouncementUI AddPopup() => root.AddComponent<AnnouncementUI>();
            public void SetPresentedZone(Zone zone) => typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, zone);
            public Entity Add(string blueprint, int x = 10, int y = 10)
            { var owner = factory.CreateEntity(blueprint); Assert.NotNull(owner); Assert.True(Zone.AddEntity(owner, x, y)); return owner; }
            public Entity Torch(bool lit)
            {
                var owner = Add("Torch"); owner.GetPart<LightSourcePart>().Enabled = lit;
                owner.GetPart<ThermalPart>().Temperature = 500; owner.GetPart<FuelPart>().FuelMass = 50;
                return owner;
            }
            public AnnouncementDocument World(Entity owner)
            {
                var result = ExamineDocumentBuilder.BuildWorld(owner, Zone, Zone.GetEntityCell(owner), Viewer);
                Assert.NotNull(result); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(result)); return result;
            }
            static Texture2D Texture(int width) => new Texture2D(width, 24) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            public void Dispose()
            {
                try
                {
                    if (root != null) Object.DestroyImmediate(root);
                    Catalog?.Dispose();
                    if (Base != null) Object.DestroyImmediate(Base); if (Cast != null) Object.DestroyImmediate(Cast);
                    if (Anchors != null) Object.DestroyImmediate(Anchors); if (States != null) Object.DestroyImmediate(States);
                }
                finally { ZoneRenderHooks.CellDirtyCallback = previousCell; ZoneRenderHooks.FullDirtyCallback = previousFull; }
            }
        }
    }
}
