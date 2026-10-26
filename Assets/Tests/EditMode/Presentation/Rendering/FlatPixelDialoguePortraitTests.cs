using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Dialogue borrows the exact disclosed speaker's native art. These
    /// are UI binding/authority tests, not screen-layout or ordinary-input acceptance.</summary>
    public sealed class FlatPixelDialoguePortraitTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        static FieldInfo Field(string name)
        {
            var result = typeof(DialogueUI).GetField(name, I);
            Assert.NotNull(result, "Dialogue needs the explicit native portrait seam: " + name);
            return result;
        }
        static ExaminePortrait Portrait(DialogueUI ui) => (ExaminePortrait)Field("_portrait").GetValue(ui);
        static AnnouncementDocument Document(DialogueUI ui) => (AnnouncementDocument)Field("_portraitDocument").GetValue(ui);
        static void Invoke(DialogueUI ui, string name)
        {
            var method = typeof(DialogueUI).GetMethod(name, I); Assert.NotNull(method, name);
            try { method.Invoke(ui, null); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
        static object Subject(AnnouncementDocument document, string name)
            => typeof(AnnouncementDocument).GetProperty(name, I).GetValue(document);

        static void AssertNative(Fixture f)
        {
            var image = Portrait(f.Ui);
            Assert.AreSame(f.KeeperAtlas, image.Texture, "Borrow the approved Seed Keeper3 resource, not a glyph/model copy.");
            Assert.AreEqual(new Rect(0, 0, 1, 1), image.Uv);
            Assert.AreEqual(Color.white, image.Tint); Assert.False(image.IsModel);
            Assert.True(f.Catalog.TryGetPresentationSubjectSprite(f.Zone, f.Keeper, 10, 10, out var sprite, out var tint));
            Assert.AreSame(sprite.texture, image.Texture); Assert.AreEqual(new Rect(0, 0, 16, 24), sprite.rect);
            Assert.AreEqual(tint, image.Tint);
        }

        [Test] public void OpenBorrowsTheExactKeeper3FrameAndCapturesItsWorldSubject()
        {
            using (var f = new Fixture())
            {
                f.Open(); AssertNative(f);
                var doc = Document(f.Ui); Assert.NotNull(doc);
                Assert.AreSame(f.Keeper, Subject(doc, "Subject"));
                Assert.AreSame(f.Viewer, Subject(doc, "SubjectViewer"));
                Assert.AreSame(f.Zone, Subject(doc, "SubjectZone"));
                Assert.AreSame(f.Zone.GetCell(10, 10), Subject(doc, "SubjectCell"));
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(doc));
                Invoke(f.Ui, "RefreshPortrait"); AssertNative(f);
                Assert.AreSame(doc, Document(f.Ui), "Repainting must not silently recapture a new subject.");
            }
        }

        [TestCase("hidden-cell")] [TestCase("unexplored-cell")] [TestCase("hidden-render")]
        [TestCase("moved")] [TestCase("same-id-replacement")] [TestCase("listener-left")]
        [TestCase("presented-zone-replaced")] [TestCase("managed-graph-replaced")]
        public void RevokedWorldAuthorityClearsThePreviouslyBorrowedImage(string change)
        {
            using (var f = new Fixture())
            {
                OverworldZoneManager manager = null;
                if (change == "managed-graph-replaced")
                { manager = OverworldZoneManager.CreateDetached(f.Factory, 64); manager.SetActiveZone(f.Zone); }
                f.Open(); AssertNative(f);
                if (change == "hidden-cell") f.Zone.GetCell(10, 10).IsVisible = false;
                if (change == "unexplored-cell") f.Zone.GetCell(10, 10).Explored = false;
                if (change == "hidden-render") f.Keeper.GetPart<RenderPart>().Visible = false;
                if (change == "moved") Assert.True(f.Zone.MoveEntity(f.Keeper, 11, 10));
                if (change == "same-id-replacement")
                {
                    Assert.True(f.Zone.RemoveEntity(f.Keeper));
                    var replacement = f.Factory.CreateEntity("SpreadSeedKeeper"); replacement.ID = f.Keeper.ID;
                    Assert.True(f.Zone.AddEntity(replacement, 10, 10));
                }
                if (change == "listener-left") Assert.True(f.Zone.RemoveEntity(f.Viewer));
                if (change == "presented-zone-replaced") f.SetPresentedZone(new Zone(f.Zone.ZoneID));
                if (change == "managed-graph-replaced") manager.CachedZones[f.Zone.ZoneID] = new Zone(f.Zone.ZoneID);
                Invoke(f.Ui, "RefreshPortrait"); Assert.IsNull(Portrait(f.Ui).Texture);
                Assert.AreSame(f.Keeper, ConversationManager.Speaker, "Presentation must not repair or retarget the session.");
            }
        }

        [TestCase("speaker")] [TestCase("listener")] [TestCase("conversation")]
        public void ExactSessionReferenceReplacementCannotRetargetAnOpenPortrait(string change)
        {
            using (var f = new Fixture())
            {
                f.Open(); AssertNative(f);
                if (change == "speaker")
                {
                    var other = f.Factory.CreateEntity("SpreadSeedKeeper");
                    Assert.True(f.Zone.AddEntity(other, 11, 10)); ConversationManager.Speaker = other;
                }
                if (change == "listener")
                {
                    var other = f.NewViewer(); Assert.True(f.Zone.AddEntity(other, 9, 11)); ConversationManager.Listener = other;
                }
                if (change == "conversation") ConversationManager.CurrentConversation = new ConversationData { ID = f.Conversation.ID };
                Invoke(f.Ui, "RefreshPortrait"); Assert.IsNull(Portrait(f.Ui).Texture);
                if (Document(f.Ui) != null) Assert.AreSame(f.Keeper, Subject(Document(f.Ui), "Subject"));
            }
        }

        [Test] public void ChangingNodeInsideTheSameSessionKeepsTheAuthorizedSpeaker()
        {
            using (var f = new Fixture())
            {
                f.Open(); AssertNative(f);
                ConversationManager.CurrentNode = new NodeData { ID = "More", Text = "The same speaker continues." };
                Invoke(f.Ui, "RefreshPortrait"); AssertNative(f);
            }
        }

        [Test] public void ExplicitLowerSpeakerDoesNotBecomeTheCellTopOccupant()
        {
            using (var f = new Fixture())
            {
                var cover = f.Factory.CreateEntity("Warden"); Assert.NotNull(cover);
                cover.GetPart<RenderPart>().RenderLayer = 100;
                Assert.True(f.Zone.AddEntity(cover, 10, 10));
                Assert.AreSame(cover, f.Zone.GetCell(10, 10).GetTopVisibleObject());
                f.Open(); AssertNative(f);
                Assert.AreSame(f.Keeper, Subject(Document(f.Ui), "Subject"));
                Assert.AreSame(cover, f.Zone.GetCell(10, 10).GetTopVisibleObject(), "Dialogue does not reorder world owners.");
            }
        }

        [TestCase("unchanged")] [TestCase("hidden")] [TestCase("session-swapped")]
        public void RepaintValidationRevokesStaleImagesWithoutRetargetingOrReopening(string change)
        {
            using (var f = new Fixture())
            {
                f.Open(); AssertNative(f); var document = Document(f.Ui);
                if (change == "hidden") f.Zone.GetCell(10, 10).IsVisible = false;
                if (change == "session-swapped") ConversationManager.CurrentConversation = new ConversationData { ID = f.Conversation.ID };
                Invoke(f.Ui, "ValidatePortrait");
                if (change == "unchanged")
                { AssertNative(f); Assert.AreSame(document, Document(f.Ui)); }
                else
                {
                    Assert.IsNull(Portrait(f.Ui).Texture);
                    f.Zone.GetCell(10, 10).IsVisible = true; ConversationManager.CurrentConversation = f.Conversation;
                    Invoke(f.Ui, "ValidatePortrait");
                    Assert.IsNull(Portrait(f.Ui).Texture, "A cheap repaint never reacquires previously revoked art.");
                }
            }
        }

        [TestCase("legacy")] [TestCase("unbound")] [TestCase("panel-disabled")]
        public void ExistingGlyphPresentationDoesNotAcquireANativeOrModelPortrait(string mode)
        {
            using (var f = new Fixture())
            {
                string glyph = f.Keeper.GetPart<RenderPart>().RenderString;
                if (mode == "legacy") f.SetFlat(false);
                if (mode == "unbound") Field("WorldPresentation").SetValue(f.Ui, null);
                if (mode == "panel-disabled") f.Ui.UsePortraitPanel = false;
                f.Open(); Invoke(f.Ui, "RefreshPortrait");
                Assert.IsNull(Portrait(f.Ui).Texture);
                Assert.AreEqual(glyph, f.Keeper.GetPart<RenderPart>().RenderString);
                Assert.True(f.Ui.IsOpen); Assert.True(ConversationManager.IsActive);
            }
        }

        [Test] public void CloseDropsCapturedAuthorityAndCannotReacquireWhileClosed()
        {
            using (var f = new Fixture())
            {
                f.Open(); AssertNative(f); f.Ui.Close();
                Assert.False(f.Ui.IsOpen); Assert.IsNull(Portrait(f.Ui).Texture); Assert.IsNull(Document(f.Ui));
                f.InstallSession(); Invoke(f.Ui, "RefreshPortrait");
                Assert.IsNull(Portrait(f.Ui).Texture); Assert.IsNull(Document(f.Ui));
            }
        }

        [TestCase("disable")] [TestCase("destroy")]
        public void PopupLifecycleDoesNotDestroyTheBorrowedWorldAtlas(string action)
        {
            using (var f = new Fixture())
            {
                f.Open(); AssertNative(f);
                if (action == "disable")
                {
                    Invoke(f.Ui, "OnDisable"); Assert.IsNull(Portrait(f.Ui).Texture); Assert.IsNull(Document(f.Ui));
                    Assert.False(f.Ui.IsOpen);
                }
                else Object.DestroyImmediate(f.Ui);
                Assert.True(f.KeeperAtlas != null); Assert.AreEqual(16, f.KeeperAtlas.width); Assert.AreEqual(24, f.KeeperAtlas.height);
                Assert.True(f.Catalog.TryGetPresentationSubjectSprite(f.Zone, f.Keeper, 10, 10, out var sprite, out _));
                Assert.AreSame(f.KeeperAtlas, sprite.texture, "The active world still owns a usable borrowed texture.");
            }
        }

        sealed class CountingDescription : IItemEnhancement
        {
            public int Reads;
            public override string Name => "DialoguePortraitDescriptionProbe";
            public override string GetEffectDescription() { Reads++; return "An examine-only provider."; }
        }
        [Test] public void DialogueOpenAndRefreshNeverRunExamineDescriptionProviders()
        {
            using (var f = new Fixture())
            {
                var probe = new CountingDescription(); f.Keeper.AddPart(probe);
                // Positive control: ordinary explicit examine really invokes this provider.
                Assert.NotNull(ExamineDocumentBuilder.BuildWorld(f.Keeper, f.Zone, f.Zone.GetCell(10, 10), f.Viewer));
                Assert.Greater(probe.Reads, 0); probe.Reads = 0;
                f.Open(); AssertNative(f);
                Invoke(f.Ui, "RefreshPortrait"); Invoke(f.Ui, "RefreshPortrait");
                Assert.AreEqual(0, probe.Reads, "A dialogue portrait is not an implicit Examine action.");
            }
        }

        sealed class Fixture : IDisposable
        {
            readonly List<(FieldInfo field, object value)> conversationState = new List<(FieldInfo, object)>();
            readonly Action<int, int, string> priorCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> priorFull = ZoneRenderHooks.FullDirtyCallback;
            readonly HaulingContentScope content;
            readonly GameObject root;
            readonly Texture2D opening, cast, anchors, states;
            public readonly Texture2D KeeperAtlas;
            public readonly FlatPixelGreyboxCatalog Catalog;
            public readonly Zone Zone = new Zone(ReferenceGladePlan.ZoneID);
            public readonly Entity Viewer, Keeper;
            public readonly ZoneRenderer Presentation;
            public readonly DialogueUI Ui;
            public readonly ConversationData Conversation;
            public EntityFactory Factory => content.Factory;

            public Fixture()
            {
                foreach (string name in new[] { "CurrentConversation", "CurrentNode", "Speaker", "Listener", "PendingTradePartner", "PendingAttackTarget", "_visibleChoices" })
                {
                    var field = typeof(ConversationManager).GetField(name, S); Assert.NotNull(field);
                    conversationState.Add((field, field.GetValue(null)));
                }
                foreach (string name in new[] { "offers", "revision" })
                {
                    var field = typeof(SpreadDiscoveryReports).GetField(name, S); Assert.NotNull(field);
                    conversationState.Add((field, field.GetValue(null)));
                }
                try
                {
                    // Even a missing-seam RED or constructor failure must never
                    // let popup cleanup end a borrowed real conversation.
                    foreach (var row in conversationState)
                        if (row.field.DeclaringType == typeof(ConversationManager))
                            row.field.SetValue(null, row.field.Name == "_visibleChoices" ? new List<ChoiceData>() : null);
                    SpreadDiscoveryReports.ClearOffers();
                    content = new HaulingContentScope(); content.Seed(64);
                    ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
                    opening = Texture(160); cast = Texture(64); anchors = Texture(192); states = Texture(192);
                    KeeperAtlas = Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.KeeperResourcePath);
                    Assert.NotNull(KeeperAtlas); Assert.AreEqual(16, KeeperAtlas.width); Assert.AreEqual(24, KeeperAtlas.height);
                    Catalog = new FlatPixelGreyboxCatalog(opening, cast, anchors, states, null, null, null, null, null, KeeperAtlas);
                    Viewer = NewViewer(); Assert.True(Zone.AddEntity(Viewer, 9, 10));
                    Keeper = Factory.CreateEntity("SpreadSeedKeeper"); Assert.NotNull(Keeper); Assert.True(Zone.AddEntity(Keeper, 10, 10));
                    for (int y = 9; y <= 12; y++) for (int x = 8; x <= 12; x++) Zone.GetCell(x, y).IsVisible = Zone.GetCell(x, y).Explored = true;
                    root = new GameObject("Owned dialogue native portrait test"); root.SetActive(false);
                    Presentation = root.AddComponent<ZoneRenderer>();
                    var environment = root.AddComponent<EnvironmentSpriteRenderer>();
                    typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", I).SetValue(environment, Catalog);
                    environment.FlatPixelGreyboxEnabled = true;
                    typeof(ZoneRenderer).GetField("_envSpriteRenderer", I).SetValue(Presentation, environment);
                    SetFlat(true); SetPresentedZone(Zone); Assert.True(Presentation.FlatPixelModeActive);
                    Ui = root.AddComponent<DialogueUI>(); Ui.PlayerEntity = Viewer; Ui.CurrentZone = Zone;
                    Field("WorldPresentation").SetValue(Ui, Presentation);
                    Conversation = new ConversationData { ID = "NativePortraitFixture" };
                    Conversation.Nodes.Add(new NodeData { ID = "Start", Text = "Good soil remembers the rain." });
                    InstallSession();
                }
                catch { Dispose(); throw; }
            }
            public Entity NewViewer()
            {
                var actor = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "DialoguePortraitViewer" };
                actor.SetTag("Player"); actor.AddPart(new PhysicsPart()); actor.AddPart(new InventoryPart());
                actor.AddPart(new RenderPart { RenderString = "@", Visible = true }); actor.AddPart(new ExaminablePart());
                return actor;
            }
            public void InstallSession()
            {
                typeof(ConversationManager).GetField("_visibleChoices", S).SetValue(null, new List<ChoiceData>());
                ConversationManager.CurrentConversation = Conversation; ConversationManager.CurrentNode = Conversation.Nodes[0];
                ConversationManager.Speaker = Keeper; ConversationManager.Listener = Viewer;
                ConversationManager.PendingTradePartner = null; ConversationManager.PendingAttackTarget = null;
            }
            public void Open() { Ui.Open(); Assert.True(Ui.IsOpen); }
            public void SetPresentedZone(Zone zone) => typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, zone);
            public void SetFlat(bool flat) => typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(Presentation, flat);
            static Texture2D Texture(int width) => new Texture2D(width, 24) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            public void Dispose()
            {
                try
                {
                    if (root != null) Object.DestroyImmediate(root);
                    Catalog?.Dispose();
                    foreach (var texture in new[] { opening, cast, anchors, states }) if (texture != null) Object.DestroyImmediate(texture);
                    content?.Dispose();
                }
                finally
                {
                    foreach (var row in conversationState) row.field.SetValue(null, row.value);
                    ZoneRenderHooks.CellDirtyCallback = priorCell; ZoneRenderHooks.FullDirtyCallback = priorFull;
                }
            }
        }
    }
}
