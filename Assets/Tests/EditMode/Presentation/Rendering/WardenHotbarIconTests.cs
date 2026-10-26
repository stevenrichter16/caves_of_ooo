using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Tests-first: only existing APIs and planned appended ordinals are referenced.
    // HUD pictograms are 16x16; the world-cell 16x24 contract does not apply here.
    public sealed class WardenHotbarIconTests
    {
        static readonly string[] Sources = { "LongBlades_EnGarde", "LongBlades_Lunge", "LongBlades_FollowThrough" };
        static readonly string[] Names = { "EnGarde", "Lunge", "FollowThrough" };
        // Captured before production edits, 2026-10-25. SHA256 of bottom-up RGBA
        // bytes decoded from the seven existing masks and their original palette.
        // HudSkillIconAtlas.cs source SHA256: 696ab7b0cfef77d62f7058a992ad78e23b747b3f45d35f1109b227398793c4fd.
        static readonly string[] OriginalHashes =
        {
            "d069ff04d78cbba62e515a295d2e036b7ddf68dee10950dffc5099b3426c732a",
            "699af45a7e76da7345bc70ebfe1ce989c3986015994f7b30de6a62c3dd6220bc",
            "519fc79321de93de48210358d807aab00213c774cebd286ca85c5dc7c5200cde",
            "a30cbe3705a7193f9b3b5fc4fa0c60d4f9e665d8e37345f4b7a4417151fc9337",
            "8a4bc9abacdcbfb296f2435b37514347ec3d9f76166e678e5c652d848db04219",
            "4b4f731b4d5d5a181e138ba7c59f950419f7cf1ef7b7ec48abcc52588d0ab797",
            "1291b62e386fabbcea61436682413310194750c2ce5cf3ee358ab92c61ea5056"
        };
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        readonly List<Action> restore = new List<Action>();
        GameObject root;
        Tilemap fg, bg;
        Camera camera;
        GameplayHotbarRenderer renderer;
        Entity player;
        ActivatedAbilitiesPart abilities;
        Texture2D borrowedAtlas;
        Tile[] borrowedTiles;

        [SetUp] public void Setup()
        {
            SnapshotStatics(typeof(MessageLog));
            SnapshotStatics(typeof(HotbarStateBuilder));
            SnapshotStatics(typeof(PerformanceDiagnostics));
            SnapshotStatics(typeof(SkillRegistry));
            Snapshot(typeof(AnatomyFactory).GetField("_types", Static));
            Snapshot(typeof(BodyPart).GetField("_nextID", Static));
            OwnCache(typeof(HudPixelFont), "atlas", "primitives", "tiles");
            OwnCache(typeof(HudCardFrame), "texture", "tile");
            foreach (string name in new[] { "_buffer", "_writeIndex", "_filledCount", "_droppedCount" })
                Snapshot(typeof(Diag).GetField(name, Static));
            MessageLog.OnMessage = null;
            MessageLog.TickProvider = null;
            borrowedAtlas = (Texture2D)AtlasField("atlas").GetValue(null);
            borrowedTiles = (Tile[])AtlasField("tiles").GetValue(null);
            // Detach, never destroy, cached objects potentially borrowed by another renderer.
            AtlasField("atlas").SetValue(null, null);
            AtlasField("tiles").SetValue(null, null);

            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            var builds = JsonUtility.FromJson<StartingBuildFile>(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Builds/StartingBuilds.json")));
            player = factory.CreateEntity("Player");
            var result = StartingBuildService.Apply(player, factory, builds.Builds.Single(b => b.Id == "warden"));
            Assert.True(result.Success, string.Join("; ", result.Errors));
            Assert.AreEqual("LongSword", StartingBuildService.PrimaryHandWeapon(player)?.BlueprintName);
            abilities = player.GetPart<ActivatedAbilitiesPart>();
            for (int i = 0; i < 3; i++) Assert.AreEqual(Sources[i], Ability(i).SourcePowerClass);

            root = new GameObject("Warden hotbar fixture");
            root.AddComponent<Grid>().cellSize = new Vector3(.6f, 1, 0);
            fg = Child("icons and text").AddComponent<Tilemap>();
            bg = Child("card backgrounds").AddComponent<Tilemap>();
            camera = Child("owned HUD camera").AddComponent<Camera>();
            camera.orthographic = true; camera.aspect = 8;
            renderer = new GameplayHotbarRenderer(fg, bg);
        }

        [TearDown] public void Cleanup()
        {
            try
            {
                if (root != null) Object.DestroyImmediate(root);
                ResetOwnedAtlas();
                AtlasField("atlas").SetValue(null, borrowedAtlas);
                AtlasField("tiles").SetValue(null, borrowedTiles);
            }
            finally
            {
                for (int i = restore.Count - 1; i >= 0; i--) restore[i]();
                restore.Clear();
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void FactoryWardenAbilityUsesItsExactAppendedIcon(int slot)
        {
            var ability = Ability(slot);
            ability.DisplayName = "An unrelated display name";
            var snapshot = HotbarStateBuilder.Build(player, slot, null);
            Assert.AreEqual(slot + 7, (int)snapshot.Slots[slot].Icon,
                "The real Warden ability must stop using the generic diamond.");
            Assert.AreEqual(Names[slot], snapshot.Slots[slot].Icon.ToString());
            Assert.AreEqual(snapshot.Slots[slot].Icon, HudHotbarPresentation.IconFor(Sources[slot]));
            Assert.AreEqual(0, ability.CooldownRemaining);
        }

        [TestCase(null)] [TestCase("")] [TestCase("Unrelated_Lunge")]
        [TestCase("longblades_lunge")] [TestCase("LongBlades_LungeExtra")] [TestCase("CommandLunge")]
        public void MatchingDisplayNameCannotGrantAnIconToAnUnknownSource(string source)
        {
            var ability = Ability(1);
            ability.DisplayName = "Lunge"; ability.SourcePowerClass = source;
            var snapshot = HotbarStateBuilder.Build(player, 1, null);
            Assert.AreEqual(SkillHudIcon.Fallback, snapshot.Slots[1].Icon);
            renderer.Render(snapshot, camera);
            Assert.AreSame(HudSkillIconAtlas.GetTile(SkillHudIcon.Fallback), fg.GetTile(IconPosition(1)));
        }

        [TestCase(8f)] [TestCase(10f)]
        public void ActualSubmittedThreeIconsAreDistinctAndFitTheExistingTextBands(float aspect)
        {
            camera.aspect = aspect;
            root.GetComponent<Grid>().cellSize = new Vector3(GameplayHotbarLayout.ComputeCellWidth(aspect), 1, 0);
            renderer.Render(HotbarStateBuilder.Build(player, 0, null), camera);
            var hashes = new HashSet<string>();
            for (int i = 0; i < 3; i++)
            {
                var pos = IconPosition(i); var tile = fg.GetTile<Tile>(pos);
                Assert.NotNull(tile);
                Assert.AreNotSame(HudSkillIconAtlas.GetTile(SkillHudIcon.Fallback), tile, Sources[i]);
                Assert.AreSame(HudSkillIconAtlas.GetTile((SkillHudIcon)(7 + i)), tile);
                AssertNative(tile.sprite);
                Assert.True(hashes.Add(Hash(tile.sprite)), "Distinct actions need distinct submitted pixels.");
                var bounds = Placed(pos);
                Assert.AreEqual(1.75f, bounds.size.x, .001f);
                Assert.AreEqual(1.75f, bounds.size.y, .001f);
                Assert.Greater(bounds.min.y, RowBounds(i, 1).Max(b => b.max.y) + .05f);
                Assert.Greater(bounds.min.y, RowBounds(i, 0).Select(b => b.max.y).DefaultIfEmpty(float.NegativeInfinity).Max() + .05f);
                Assert.Less(bounds.max.y, RowBounds(i, 4).Min(b => b.min.y) - .05f);
                Assert.Greater(bounds.min.x, i * 8 * GameplayHotbarLayout.ComputeCellWidth(aspect));
                Assert.Less(bounds.max.x, (i + 1) * 8 * GameplayHotbarLayout.ComputeCellWidth(aspect));
                Assert.AreEqual(Color.white, fg.GetColor(pos));
            }
        }

        [TestCase(940)] [TestCase(1280)]
        public void FactoryWardenEnGardeOccupiesTwoCompleteNameRowsInsideItsCard(int hotbarPixelWidth)
        {
            // Geometry for the sidebar and full-width 1280x800 layouts. This is
            // submitted-tile/bounds coverage, not a GPU or physical-screen capture.
            camera.aspect = hotbarPixelWidth / 120f;
            float cellWidth = GameplayHotbarLayout.ComputeCellWidth(camera.aspect);
            root.GetComponent<Grid>().cellSize = new Vector3(cellWidth, 1, 0);
            Assert.AreEqual("En Garde", Ability(0).DisplayName, "Use the real starting-build label.");
            var snapshot = HotbarStateBuilder.Build(player, 0, null);
            renderer.Render(snapshot, camera);

            AssertNameRow(0, 1, 3, "En");
            AssertNameRow(0, 0, 1, "Garde");
            AssertNameRow(1, 1, 1, "Lunge");
            AssertNameRow(1, 0, 0, "");
            AssertNameRow(2, 1, 1, "Follow");
            AssertNameRow(2, 0, 0, "Through");
            foreach (var bounds in RowBounds(0, 0).Concat(RowBounds(0, 1)))
            {
                Assert.Greater(bounds.min.x, 0f);
                Assert.Less(bounds.max.x, 8 * cellWidth);
                Assert.Greater(bounds.min.y, 0f);
                Assert.Less(bounds.max.y, 2f, "Name rows stay below the icon band.");
            }
            Assert.AreSame(HudSkillIconAtlas.GetTile(SkillHudIcon.EnGarde), fg.GetTile(IconPosition(0)));
            Assert.AreEqual("En Garde", snapshot.Slots[0].DisplayName);
            StringAssert.Contains("En Garde", snapshot.SummaryText);
            Assert.AreEqual("READY", HudHotbarPresentation.StateLabel(snapshot.Slots[0]));
            Assert.False(player.HasEffect<EnGardeEffect>(), "Wrapping must not execute the ability.");
        }

        [Test]
        public void ShortLabelClearsOldSecondRowAndRestoringEnGardeRebuildsBothRows()
        {
            var ability = Ability(0);
            string originalName = ability.DisplayName;
            int hp = player.GetStatValue("Hitpoints");
            renderer.Render(HotbarStateBuilder.Build(player, 0, null), camera);
            AssertNameRow(0, 1, 3, "En");
            AssertNameRow(0, 0, 1, "Garde");
            var otherFirst = Row(2, 1); var otherSecond = Row(2, 0);
            var icon = fg.GetTile(IconPosition(0));

            // Change presentation only, keeping the same real ability and icon.
            ability.DisplayName = "Lunge";
            renderer.Render(HotbarStateBuilder.Build(player, 0, null), camera);
            AssertNameRow(0, 1, 1, "Lunge");
            AssertNameRow(0, 0, 0, "");
            Assert.AreSame(icon, fg.GetTile(IconPosition(0)));
            CollectionAssert.AreEqual(otherFirst, Row(2, 1));
            CollectionAssert.AreEqual(otherSecond, Row(2, 0));

            ability.DisplayName = originalName;
            renderer.Render(HotbarStateBuilder.Build(player, 0, null), camera);
            AssertNameRow(0, 1, 3, "En");
            AssertNameRow(0, 0, 1, "Garde");
            Assert.AreSame(icon, fg.GetTile(IconPosition(0)));
            Assert.AreEqual(Sources[0], ability.SourcePowerClass);
            Assert.Zero(ability.CooldownRemaining);
            Assert.AreEqual(hp, player.GetStatValue("Hitpoints"));
            Assert.False(player.HasEffect<EnGardeEffect>());
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void CooldownAndPendingChangeStateWithoutReplacingNamesOrIdentity(int slot)
        {
            renderer.Render(HotbarStateBuilder.Build(player, slot, null), camera);
            var icon = fg.GetTile(IconPosition(slot));
            Assert.AreNotSame(HudSkillIconAtlas.GetTile(SkillHudIcon.Fallback), icon);
            var first = Row(slot, 1); var second = Row(slot, 0);
            int hp = player.GetStatValue("Hitpoints");
            var stock = player.GetPart<InventoryPart>().Objects.ToArray();
            Ability(slot).CooldownRemaining = 12;
            var cooldown = HotbarStateBuilder.Build(player, slot, null);
            Assert.AreEqual("CD12", HudHotbarPresentation.StateLabel(cooldown.Slots[slot]));
            Assert.False(cooldown.Slots[slot].Usable);
            renderer.Render(cooldown, camera);
            Assert.AreSame(icon, fg.GetTile(IconPosition(slot)));
            CollectionAssert.AreEqual(first, Row(slot, 1)); CollectionAssert.AreEqual(second, Row(slot, 0));
            Ability(slot).CooldownRemaining = 0;
            var pending = HotbarStateBuilder.Build(player, slot, Ability(slot));
            Assert.AreEqual("AIM", HudHotbarPresentation.StateLabel(pending.Slots[slot]));
            renderer.Render(pending, camera);
            Assert.AreSame(icon, fg.GetTile(IconPosition(slot)));
            CollectionAssert.AreEqual(first, Row(slot, 1)); CollectionAssert.AreEqual(second, Row(slot, 0));
            Assert.AreEqual("CD12", HudHotbarPresentation.StateLabel(cooldown.Slots[slot]), "Retained snapshots stay immutable.");
            var ready = HotbarStateBuilder.Build(player, slot, null);
            Assert.AreEqual("READY", HudHotbarPresentation.StateLabel(ready.Slots[slot]));
            renderer.Render(ready, camera);
            Assert.AreSame(icon, fg.GetTile(IconPosition(slot)));
            Assert.AreEqual(hp, player.GetStatValue("Hitpoints"));
            CollectionAssert.AreEqual(stock, player.GetPart<InventoryPart>().Objects);
            Assert.False(player.HasEffect<EnGardeEffect>(), "Painting the guard icon must not activate its effect.");
        }

        [Test] public void QuietFramesDoNoWorkAndExactSourceChangeRepaintsOnlyItsCard()
        {
            var snapshot = HotbarStateBuilder.Build(player, 0, null);
            renderer.Render(snapshot, camera);
            var fgSentinel = new Vector3Int(17, 4, 0); var bgSentinel = new Vector3Int(16, 0, 0);
            fg.SetColor(fgSentinel, Color.magenta); bg.SetColor(bgSentinel, Color.green);
            PerformanceDiagnostics.BeginFrame(0, false, false);
            renderer.Render(HotbarStateBuilder.Build(player, 0, null), camera);
            PerformanceDiagnostics.EndFrame(0);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.HotbarRenderCount);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.TilemapClearCount);
            var oldIcon = fg.GetTile(IconPosition(0));
            Ability(0).SourcePowerClass = Sources[1]; // Same display name, new exact source.
            PerformanceDiagnostics.BeginFrame(0, false, false);
            renderer.Render(HotbarStateBuilder.Build(player, 0, null), camera);
            PerformanceDiagnostics.EndFrame(0);
            Assert.AreEqual(1, PerformanceDiagnostics.LastCompletedFrameSnapshot.HotbarRenderCount);
            Assert.AreNotSame(oldIcon, fg.GetTile(IconPosition(0)));
            Assert.AreSame(HudSkillIconAtlas.GetTile((SkillHudIcon)8), fg.GetTile(IconPosition(0)));
            Assert.AreEqual(Color.magenta, fg.GetColor(fgSentinel)); Assert.AreEqual(Color.green, bg.GetColor(bgSentinel));
        }

        [Test] public void ExplicitClearAndAbilityRemovalEraseOldIconsAndRestoreUnchangedSnapshot()
        {
            var snapshot = HotbarStateBuilder.Build(player, 0, null);
            renderer.Render(snapshot, camera);
            var icon = fg.GetTile(IconPosition(0));
            Assert.AreNotSame(HudSkillIconAtlas.GetTile(SkillHudIcon.Fallback), icon);
            renderer.Clear(); Assert.Zero(fg.GetUsedTilesCount()); Assert.Zero(bg.GetUsedTilesCount());
            renderer.Render(snapshot, camera); Assert.AreSame(icon, fg.GetTile(IconPosition(0)));
            Assert.True(abilities.RemoveAbility(Ability(0).ID));
            var empty = HotbarStateBuilder.Build(player, 0, null);
            renderer.Render(empty, camera);
            Assert.False(empty.Slots[0].Occupied); Assert.IsNull(fg.GetTile(IconPosition(0)));
            Assert.AreSame(HudSkillIconAtlas.GetTile((SkillHudIcon)8), fg.GetTile(IconPosition(1)));
        }

        [Test] public void OriginalSevenDecodedPixelRegionsAndOrdinalsRemainByteExact()
        {
            var names = new[] { "Fallback", "EmberSpit", "FlamingHands", "JetBlast", "GroundSurge", "RimeGrip", "Calm" };
            for (int i = 0; i < 7; i++)
            {
                var icon = (SkillHudIcon)i; var tile = HudSkillIconAtlas.GetTile(icon);
                Assert.AreEqual(names[i], icon.ToString()); AssertNative(tile.sprite);
                Assert.AreEqual(OriginalHashes[i], Hash(tile.sprite), names[i]);
                Assert.AreSame(tile, HudSkillIconAtlas.GetTile(icon));
            }
            Assert.AreSame(HudSkillIconAtlas.GetTile(SkillHudIcon.Fallback), HudSkillIconAtlas.GetTile((SkillHudIcon)(-1)));
            Assert.AreSame(HudSkillIconAtlas.GetTile(SkillHudIcon.Fallback), HudSkillIconAtlas.GetTile((SkillHudIcon)999));
        }

        [Test] public void OwnedAtlasResetRecreatesIdenticalNewIconsAndDoesNotDestroyWorldOrTextBanks()
        {
            var world = CP437TilesetGenerator.GetTile('@'); var text = HudPixelFont.GetTile('A');
            var old = HudSkillIconAtlas.GetTile((SkillHudIcon)7); var texture = old.sprite.texture;
            Assert.AreNotSame(HudSkillIconAtlas.GetTile(SkillHudIcon.Fallback), old);
            string hash = Hash(old.sprite);
            ResetOwnedAtlas();
            Assert.True(old == null); Assert.True(texture == null);
            var rebuilt = HudSkillIconAtlas.GetTile((SkillHudIcon)7);
            Assert.AreEqual(hash, Hash(rebuilt.sprite)); Assert.AreSame(rebuilt, HudSkillIconAtlas.GetTile((SkillHudIcon)7));
            Assert.True(world != null); Assert.True(text != null);
            Assert.AreSame(world, CP437TilesetGenerator.GetTile('@')); Assert.AreSame(text, HudPixelFont.GetTile('A'));
            renderer.Render(HotbarStateBuilder.Build(player, 0, null), camera);
            Assert.AreSame(rebuilt, fg.GetTile(IconPosition(0)));
        }

        void AssertNameRow(int slot, int y, int firstColumn, string label)
        {
            // Explicit expected columns come from the desired visible layout,
            // rather than calling the wrapping/centering implementation again.
            var expected = new TileBase[8];
            for (int i = 0; i < label.Length; i++) expected[firstColumn + i] = HudPixelFont.GetTile(label[i]);
            CollectionAssert.AreEqual(expected, Row(slot, y), $"slot {slot}, name row {y}: {label}");
        }

        ActivatedAbility Ability(int slot) => abilities.GetAbilityBySlot(slot);
        static Vector3Int IconPosition(int slot) => new Vector3Int(slot * 8 + 3, 2, 0);
        GameObject Child(string name) { var go = new GameObject(name); go.transform.SetParent(root.transform, false); return go; }
        TileBase[] Row(int slot, int y) => Enumerable.Range(slot * 8, 8).Select(x => fg.GetTile(new Vector3Int(x, y, 0))).ToArray();
        IEnumerable<Bounds> RowBounds(int slot, int y)
        { for (int x = slot * 8; x < slot * 8 + 8; x++) { var p = new Vector3Int(x, y, 0); if (fg.GetSprite(p) != null) yield return Placed(p); } }
        Bounds Placed(Vector3Int p)
        {
            var bounds = fg.GetSprite(p).bounds;
            var matrix = fg.orientationMatrix * fg.GetTransformMatrix(p);
            var center = fg.GetCellCenterLocal(p);
            var placed = new Bounds(center + matrix.MultiplyPoint3x4(bounds.min), Vector3.zero);
            placed.Encapsulate(center + matrix.MultiplyPoint3x4(bounds.max)); return placed;
        }
        static void AssertNative(Sprite sprite)
        {
            Assert.AreEqual(16, sprite.rect.width); Assert.AreEqual(16, sprite.rect.height);
            Assert.AreEqual(16, sprite.pixelsPerUnit); Assert.AreEqual(new Vector2(8, 8), sprite.pivot);
            Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, sprite.texture.wrapMode);
            Assert.AreEqual(1, sprite.texture.mipmapCount);
            Assert.Greater(sprite.texture.GetPixels((int)sprite.rect.x, (int)sprite.rect.y, 16, 16).Count(p => p.a > .5f), 10);
        }
        static string Hash(Sprite sprite)
        {
            var rect = sprite.rect; var pixels = sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            var bytes = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            { Color32 p = pixels[i]; bytes[i * 4] = p.r; bytes[i * 4 + 1] = p.g; bytes[i * 4 + 2] = p.b; bytes[i * 4 + 3] = p.a; }
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        static FieldInfo AtlasField(string name) => typeof(HudSkillIconAtlas).GetField(name, Static);
        static void ResetOwnedAtlas() => typeof(HudSkillIconAtlas).GetMethod("Release", Static).Invoke(null, null);

        // Restore exact private queue provenance, retained snapshots and diagnostic
        // counters. No save files, registry replacement or unrelated scene teardown.
        void OwnCache(Type type, params string[] names)
        {
            var fields = names.Select(name => type.GetField(name, Static)).ToArray();
            var previous = fields.Select(field => field.GetValue(null)).ToArray();
            foreach (var field in fields) field.SetValue(null, null);
            restore.Add(() =>
            {
                type.GetMethod("Release", Static).Invoke(null, null);
                for (int i = 0; i < fields.Length; i++) fields[i].SetValue(null, previous[i]);
            });
        }
        void SnapshotStatics(Type type)
        { foreach (var field in type.GetFields(Static)) if (!field.IsLiteral) Snapshot(field); }
        void Snapshot(FieldInfo field)
        {
            Assert.NotNull(field);
            object value = field.GetValue(null);
            if (value is PerformanceFrameSnapshot frame)
            {
                var copy = new PerformanceFrameSnapshot(frame);
                restore.Add(() => typeof(PerformanceFrameSnapshot).GetMethod("CopyFrom", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(frame, new object[] { copy }));
            }
            else if (value is Array array)
            { var copy = (Array)array.Clone(); restore.Add(() => Array.Copy(copy, array, copy.Length)); }
            else if (value is IDictionary dictionary)
            {
                var copy = new List<DictionaryEntry>();
                foreach (DictionaryEntry entry in dictionary) copy.Add(entry);
                restore.Add(() => { dictionary.Clear(); foreach (var entry in copy) dictionary.Add(entry.Key, entry.Value); });
            }
            else if (value is IList list)
            { var copy = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var item in copy) list.Add(item); }); }
            else if (value is IEnumerable enumerable && value.GetType().IsGenericType
                && value.GetType().GetGenericTypeDefinition() == typeof(Queue<>))
            {
                var copy = enumerable.Cast<object>().ToArray();
                restore.Add(() => { value.GetType().GetMethod("Clear").Invoke(value, null);
                    foreach (var item in copy) value.GetType().GetMethod("Enqueue").Invoke(value, new[] { item }); });
            }
            if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
        }
    }
}
