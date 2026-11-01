using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Waysites in the flat pixel world (Docs/CHUNK-ACTIVITY.md §3.4): every owner a real waysite
    /// places, in every biome that has them, draws native art on its real ground (LocalGround), never
    /// a glyph and never a black box: a black-tinted glyph backing is a ground tile too, so "has a
    /// ground tile" cannot tell them apart. Generated as a fresh game generates them, located through the
    /// WaysitePlaced records, resolved through the world catalog the renderer uses.
    /// </summary>
    public sealed class WaysitePresentationTests
    {
        const int Seed = 64, ZonesPerBiome = 24;
        DensityLootTestScope content; FlatPixelGreyboxCatalog world;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope(); world = new FlatPixelGreyboxCatalog();
            Diag.ResetAll(); Diag.SetChannel("worldgen", true);
        }
        [TearDown] public void Cleanup() { Diag.SetChannel("worldgen", false); world?.Dispose(); content?.Dispose(); }

        [TestCase(BiomeType.Spread)] [TestCase(BiomeType.Sodden)] [TestCase(BiomeType.Beating)]
        [TestCase(BiomeType.Stump)] [TestCase(BiomeType.Grovelands)]
        public void EveryWaysiteOwnerDrawsNativeArtOnItsGround(BiomeType biome)
        {
            var manager = OverworldZoneManager.CreateDetached(content.Factory, Seed, enableExploration: true);
            var kinds = WaysiteCatalog.For(biome).ToDictionary(k => k.Id);
            var seen = new HashSet<string>(); var failures = new List<string>(); int owners = 0, zones = 0;
            for (int x = 0; x < WorldMap.Width && zones < ZonesPerBiome; x++)
                for (int y = 0; y < WorldMap.Height && zones < ZonesPerBiome; y++)
                {
                    string id = WorldMap.ToZoneID(x, y, 0);
                    if (WorldMapAuthoring.BiomeAt(x, y) != biome || !WaysitePlan.IsEligible(manager, id)) continue;
                    Diag.ResetAll(); Diag.SetChannel("worldgen", true);
                    var zone = manager.GetZone(id); zones++;
                    manager.SetActiveZone(zone);
                    foreach (var c in zone.Cells) c.IsVisible = c.Explored = true;
                    foreach (var r in DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "WaysitePlaced", Limit = 50 }).Records)
                    {
                        var kind = kinds[Field(r.PayloadJson, "kind")]; int ax = int.Parse(Field(r.PayloadJson, "ax")), ay = int.Parse(Field(r.PayloadJson, "ay"));
                        seen.Add(kind.Id);
                        for (int sy = 0; sy < kind.Stamp.Height; sy++)
                            for (int sx = 0; sx < kind.Stamp.Rows[sy].Length; sx++)
                            {
                                char ch = kind.Stamp.Rows[sy][sx];
                                if (ch == '.' || !kind.Stamp.Legend.TryGetValue(ch, out var marker) || marker == "interior") continue;
                                owners++;
                                int cx = ax + sx, cy = ay + sy;
                                string what = kind.Id + "@" + id + " " + cx + "," + cy + " " + string.Join("+", zone.GetCell(cx, cy).Objects.Select(o => o.BlueprintName));
                                if (!world.TryResolvePresentation(zone, cx, cy, out var v)) { failures.Add(what + ": unresolved"); continue; }
                                if (!v.NativeForeground) failures.Add(what + ": glyph (" + v.Owner?.BlueprintName + ")");
                                else if (!v.LocalGround) failures.Add(what + ": not on its real ground (" + v.Owner?.BlueprintName + ")");
                            }
                    }
                }
            CollectionAssert.AreEquivalent(kinds.Keys, seen, biome + ": every kind was placed somewhere in " + zones + " zones");
            Assert.Greater(owners, 0);
            CollectionAssert.IsEmpty(failures, biome + ": " + owners + " owners checked");
        }

        static string Field(string json, string name)
            => System.Text.RegularExpressions.Regex.Match(json, "\"" + name + "\":\"?([^\",}]*)").Groups[1].Value;

        [Test] public void AGlyphOwnerIsReportedAsOne()
        {
            // Counter-check: the probe can fail. A blueprint with no native art anywhere stays a glyph.
            var manager = OverworldZoneManager.CreateDetached(content.Factory, Seed, enableExploration: true);
            var zone = new Zone("Overworld.10.11.0"); manager.SetActiveZone(zone);
            foreach (var c in zone.Cells) c.IsVisible = c.Explored = true;
            zone.AddEntity(content.Factory.CreateEntity("Grass"), 10, 10);
            var odd = new Entity { BlueprintName = "WaysiteProbeGlyph" };
            odd.AddPart(new RenderPart { RenderString = "Q", ColorString = "&M", RenderLayer = 5 });   // no approved frame claims it
            zone.AddEntity(odd, 10, 10);
            Assert.True(world.TryResolvePresentation(zone, 10, 10, out var v));
            Assert.False(v.NativeForeground && ReferenceEquals(v.Owner, odd), "an owner with no art is reported as a glyph");
        }
    }
}
