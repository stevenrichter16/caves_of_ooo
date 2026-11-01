using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Glyph → pixel, rank 1 ground (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md): Morrowfast's authored land, which
    /// flat mode drew as glyphs because its painted scene is a legacy presenter. The runtime names its
    /// TepuiStone by role (MorrowfastSceneRuntime.NameTerrain), and each role takes approved art:
    /// "grove turf" the Grovelands forest floor, "flagstone floor" the Sill room floor, "house wall"
    /// the Sill masonry. Collision and opacity stay exactly as authored.
    /// </summary>
    public sealed class FlatPixelMorrowfastGroundTests
    {
        static EntityFactory factory;
        OverworldZoneManager manager; Zone zone; FlatPixelGreyboxCatalog world;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
        }
        [SetUp] public void Setup()
        {
            manager = OverworldZoneManager.CreateDetached(factory, 64); world = new FlatPixelGreyboxCatalog();
            zone = manager.GetZone(MorrowfastSceneRuntime.ZoneID); manager.SetActiveZone(zone);
            foreach (var c in zone.Cells) { c.Explored = true; c.IsVisible = true; }
            Assume.That(MorrowfastSceneRuntime.IsActive(zone), "the painted place is installed");
        }
        [TearDown] public void Cleanup() { world?.Dispose(); }

        /// <summary>Uncovered cells whose only owner is authored terrain named for the role.</summary>
        (int x, int y, Entity terrain) Find(string name, bool solid)
        {
            foreach (var c in zone.Cells)
            {
                if (c.Objects.Count != 1) continue;
                var t = c.Objects[0];
                if (t.BlueprintName == "TepuiStone" && t.HasTag(MorrowfastSceneRuntime.TerrainTag)
                    && t.GetPart<RenderPart>().DisplayName == name && t.GetPart<PhysicsPart>().Solid == solid) return (c.X, c.Y, t);
            }
            Assert.Fail("no bare " + name); return default;
        }
        FlatPixelGreyboxCell At(int x, int y) { Assert.True(world.TryResolvePresentation(zone, x, y, out var v)); return v; }
        static Texture2D Art(string path) { var t = Resources.Load<Texture2D>(path); Assert.NotNull(t, path); return t; }

        [Test] public void GroveTurfDrawsTheApprovedForestFloor()
        {
            var (x, y, _) = Find(MorrowfastSceneRuntime.GroundName, false);
            var v = At(x, y);
            Assert.True(v.LocalGround, "Morrowfast's turf is no longer a glyph field");
            Assert.AreSame(Art(FlatPixelGrovelandsMaterialsCatalog.GroundResourcePath), v.GroundTile.sprite.texture);
            Assert.False(v.BlocksMovement);
        }

        [Test] public void FlagstoneFloorsDrawTheApprovedRoomFloor()
        {
            var (x, y, _) = Find(MorrowfastSceneRuntime.FloorName, false);
            var v = At(x, y);
            Assert.True(v.LocalGround);
            Assert.AreSame(Art(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath), v.GroundTile.sprite.texture);
            Assert.That(v.GroundTile.sprite.rect.x / 16, Is.InRange(4, 7), "Sill floor frames 4-7 of 9");
        }

        [Test] public void HouseWallsDrawTheApprovedMasonryAndStillBlock()
        {
            var (x, y, wall) = Find(MorrowfastSceneRuntime.WallName, true);
            var v = At(x, y);
            Assert.AreSame(wall, v.Owner); Assert.True(v.NativeForeground, "the house wall is no longer a glyph");
            Assert.AreSame(Art(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath), v.ForegroundTile.sprite.texture);
            Assert.AreEqual(8 * 16, v.ForegroundTile.sprite.rect.x, "Sill masonry, frame 8 of 9");
            Assert.True(v.BlocksMovement, "art never changes collision");
            Assert.True(world.TryGetPresentationSubjectSprite(zone, wall, x, y, out var portrait, out _));
            Assert.AreSame(v.ForegroundTile.sprite, portrait, "the reader shows the same wall");
        }

        [Test] public void ARenamedOrForeignTerrainKeepsItsGlyph()
        {
            var (x, y, turf) = Find(MorrowfastSceneRuntime.GroundName, false);
            Assume.That(At(x, y).LocalGround);
            turf.GetPart<RenderPart>().DisplayName = "something else";
            Assert.False(At(x, y).LocalGround, "counter-check: only the runtime's own named roles");
            turf.GetPart<RenderPart>().DisplayName = MorrowfastSceneRuntime.GroundName;
            turf.Tags.Remove(MorrowfastSceneRuntime.TerrainTag);
            Assert.False(At(x, y).LocalGround, "counter-check: only the runtime's authored terrain");
        }

        [Test] public void WithoutItsInstalledSceneTheAddressIsNotAdmitted()
        {
            var bare = new Zone(MorrowfastSceneRuntime.ZoneID);
            var turf = factory.CreateEntity("TepuiStone"); turf.SetTag(MorrowfastSceneRuntime.TerrainTag);
            turf.GetPart<RenderPart>().DisplayName = MorrowfastSceneRuntime.GroundName;
            Assert.True(bare.AddEntity(turf, 10, 10));
            Assert.False(MorrowfastSceneRuntime.IsActive(bare));
            Assert.False(FlatPixelSettlementGroundCatalog.IsSupportedZone(bare), "counter-check: the address alone is not the place");
        }

        [Test] public void MemoryKeepsTheSameMaterialDimmed()
        {
            var (x, y, wall) = Find(MorrowfastSceneRuntime.WallName, true);
            var seen = At(x, y).ForegroundTile;
            zone.GetCell(x, y).IsVisible = false;
            var memory = At(x, y);
            Assert.True(memory.Remembered);
            Assert.AreSame(seen.sprite.texture, memory.ForegroundTile?.sprite.texture, "a remembered house wall stays masonry, not a glyph");
        }

        [Test] public void EveryResidentWearsTheirOwnBody()
        {
            var residents = zone.GetAllEntities().Where(e => e.BlueprintName.StartsWith("Morrowfast") && e.HasTag("Creature")).ToList();
            Assert.AreEqual(8, residents.Count, "the town's eight named people");
            foreach (var r in residents)
            {
                var (x, y) = zone.GetEntityPosition(r);
                var v = At(x, y);
                if (!ReferenceEquals(v.Owner, r)) continue;   // covered by something drawn above them
                Assert.True(v.NativeForeground, r.BlueprintName + " is no longer an '@'");
                string resource = "morrowfast_" + r.BlueprintName.Substring("Morrowfast".Length).ToLowerInvariant();
                Assert.AreSame(Art("Sprites/Environment/" + resource), v.ForegroundTile.sprite.texture, r.BlueprintName);
            }
            var farra = residents.Single(r => r.BlueprintName == "MorrowfastFarra");
            Assume.That(farra.GetPart<RenderPart>().VisualID, Is.EqualTo("actor.morrowfast_farra"), "her authored visual identity");
            var (fx, fy) = zone.GetEntityPosition(farra);
            Assume.That(ReferenceEquals(At(fx, fy).Owner, farra));
            farra.GetPart<RenderPart>().VisualID = "actor.someone_else";
            Assert.False(At(fx, fy).NativeForeground, "counter-check: only her own authored visual is canon");
        }

        [Test] public void TheTownReadsAsTownNotGlyphs()
        {
            int land = 0, drawn = 0;
            foreach (var c in zone.Cells)
            {
                var t = c.Objects.FirstOrDefault(o => o.HasTag(MorrowfastSceneRuntime.TerrainTag) && o.BlueprintName == "TepuiStone");
                if (t == null) continue; land++;
                var v = At(c.X, c.Y);
                if (v.LocalGround || ReferenceEquals(v.Owner, t) && v.NativeForeground) drawn++;
            }
            Assert.Greater(land, 1800);
            Assert.Greater(drawn, land * 9 / 10, "nine in ten of Morrowfast's land cells draw native ground or walls");
        }
    }
}
