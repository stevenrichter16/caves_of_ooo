using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Glyph → pixel replacement, Prompt 1 (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md): what still
    /// draws as an ASCII glyph in the flat 16x24 view.
    ///
    /// <para><b>World pass.</b> Generates seed 64: all surface zones, sinkhole stacks to depth 3,
    /// and cave floors 1–3 under every fourth surface zone. Every non-terrain owner is resolved
    /// through the real <see cref="FlatPixelGreyboxCatalog"/>. An owner counts as native when the
    /// catalog draws it natively, or when the animated actor catalogs (the portable actors and the
    /// patchbearer) give it a body.</para>
    ///
    /// <para><b>Blueprint pass.</b> Every creature, item and prop blueprint is placed in a neutral
    /// wilderness zone and resolved the same way, so blueprints the seed never places are still
    /// counted.</para>
    ///
    /// <para>Writes <c>Library/ArtCensus/glyph-census.txt</c>. Explicit: run it deliberately.</para>
    /// </summary>
    [Explicit("whole-world census; run deliberately")]
    public sealed class GlyphCensusBenchTests
    {
        const int Seed = 64;

        sealed class Row
        {
            public string Blueprint, Kind, Glyph, Color, Sprite;
            public int Count, Native;
            public HashSet<string> Zones = new HashSet<string>();
            public Dictionary<string, int> GlyphBy = new Dictionary<string, int>();
        }

        /// <summary>Where a glyph owner stands: biome and depth, so triage can tell a cave band
        /// from a surface biome.</summary>
        static string Place(string zoneId)
        {
            if (zoneId == null || !zoneId.StartsWith("Overworld.")) return "other";
            var (x, y, d) = WorldMap.FromZoneID(zoneId);
            string biome = WorldMapAuthoring.InBounds(x, y) ? WorldMapAuthoring.BiomeAt(x, y).ToString() : "?";
            return biome + "/" + (d == 0 ? "surface" : "depth" + d);
        }

        static EntityFactory LoadFactory()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            return f;
        }

        static string KindOf(Entity e)
        {
            if (e.HasTag("Creature")) return e.GetPart<ConversationPart>() != null ? "npc" : "creature";
            if (e.GetPart<PhysicsPart>()?.Takeable == true) return "item";
            return "prop";
        }

        static string SpriteFor(string blueprint)
        {
            string snake = System.Text.RegularExpressions.Regex.Replace(blueprint, "(?<=[a-z0-9])([A-Z])", "_$1").ToLowerInvariant();
            foreach (var folder in new[] { "Environment", "Actors", "Live16x24", "Items" })
                foreach (var name in new[] { snake, snake.Replace('_', '-') })
                    if (Resources.Load<Texture2D>("Sprites/" + folder + "/" + name) != null) return folder + "/" + name;
            return null;
        }

        static bool Native(FlatPixelGreyboxCatalog world, Zone zone, Entity owner)
        {
            if (FlatPixelActorMotionCatalog.IsPortableActor(owner) || owner.BlueprintName == "MarlbackPatchbearer") return true;
            var (x, y) = zone.GetEntityPosition(owner);
            return world.TryResolvePresentation(zone, x, y, out var v) && ReferenceEquals(v.Owner, owner) && v.NativeForeground;
        }

        static void Visible(Zone zone)
        {
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) { var c = zone.GetCell(x, y); c.Explored = true; c.IsVisible = true; }
        }

        static void Note(Dictionary<string, Row> rows, Entity e, bool native, string zoneId)
        {
            if (!rows.TryGetValue(e.BlueprintName, out var r))
            {
                var render = e.GetPart<RenderPart>();
                rows[e.BlueprintName] = r = new Row { Blueprint = e.BlueprintName, Kind = KindOf(e), Glyph = render?.RenderString, Color = render?.ColorString, Sprite = SpriteFor(e.BlueprintName) };
            }
            r.Count++; if (native) r.Native++;
            else { string where = Place(zoneId); r.GlyphBy[where] = r.GlyphBy.TryGetValue(where, out int n) ? n + 1 : 1; }
            if (!native && zoneId != null && r.Zones.Count < 6) r.Zones.Add(zoneId);
        }

        [Test] public void WhatStillDrawsAsAGlyph()
        {
            var factory = LoadFactory();
            var manager = OverworldZoneManager.CreateDetached(factory, Seed);
            var world = new FlatPixelGreyboxCatalog();
            var rows = new Dictionary<string, Row>();
            var ids = new List<string>();
            for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++)
            {
                if (!WorldMapAuthoring.InBounds(x, y)) continue;
                ids.Add(WorldMap.ToZoneID(x, y, 0));
                var poi = manager.WorldMap.GetPOI(x, y);
                bool deep = poi?.Type == POIType.Sinkhole || (x + y * WorldMap.Width) % 4 == 0;
                if (deep) for (int d = 1; d <= 3; d++) ids.Add(WorldMap.ToZoneID(x, y, d));
            }
            int zones = 0;
            foreach (var id in ids)
            {
                Zone zone;
                try { zone = manager.GetZone(id); } catch { continue; }
                if (zone == null) continue;
                manager.SetActiveZone(zone); Visible(zone); zones++;
                foreach (var e in zone.GetAllEntities())
                {
                    if (e == null || e.HasTag("Terrain") || e.GetPart<RenderPart>()?.Visible != true) continue;
                    Note(rows, e, Native(world, zone, e), id);
                }
            }

            // Blueprint pass: everything that could appear, placed alone in quiet country.
            var neutral = manager.GetZone("Overworld.11.6.0"); manager.SetActiveZone(neutral); Visible(neutral);
            var bpRows = new Dictionary<string, Row>();
            foreach (var name in factory.Blueprints.Keys.OrderBy(n => n))
            {
                Entity e;
                try { e = factory.CreateEntity(name); } catch { continue; }
                if (e == null || e.HasTag("Terrain") || e.HasTag("Player") || e.GetPart<RenderPart>() == null) continue;
                if (rows.ContainsKey(name)) continue;
                int cx = -1, cy = -1;
                for (int y = 3; y < Zone.Height - 3 && cx < 0; y++) for (int x = 3; x < Zone.Width - 3; x++)
                {
                    var c = neutral.GetCell(x, y);
                    if (!c.BlocksMovement() && c.Objects.All(o => o.HasTag("Terrain"))) { cx = x; cy = y; break; }
                }
                if (cx < 0 || !neutral.AddEntity(e, cx, cy)) continue;
                Visible(neutral);
                Note(bpRows, e, Native(world, neutral, e), null);
                neutral.RemoveEntity(e);
            }
            world.Dispose();

            var report = new StringBuilder($"GLYPH CENSUS seed {Seed}: {zones} zones\n\n");
            void Section(string title, IEnumerable<Row> list)
            {
                var glyph = list.Where(r => r.Native < r.Count).OrderBy(r => r.Kind).ThenByDescending(r => r.Count - r.Native).ToList();
                report.AppendLine($"== {title}: {glyph.Count} blueprints draw as glyphs ({glyph.Sum(r => r.Count - r.Native)} owners)");
                foreach (var r in glyph)
                {
                    report.AppendLine($"  {r.Kind,-8} {r.Blueprint,-32} glyph={r.Glyph}{r.Color} owners={r.Count - r.Native}/{r.Count} sprite={(r.Sprite ?? "-")} zones={string.Join(" ", r.Zones)}");
                    if (r.GlyphBy.Count > 0)
                        report.AppendLine("           glyph at: " + string.Join(", ", r.GlyphBy.OrderByDescending(kv => kv.Value).Take(6).Select(kv => kv.Key + "=" + kv.Value)));
                }
                report.AppendLine();
            }
            Section("WORLD (seen in seed 64)", rows.Values);
            Section("BLUEPRINTS NOT SEEN IN SEED 64", bpRows.Values);
            string dir = Path.Combine(Application.dataPath, "..", "Library", "ArtCensus");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "glyph-census.txt"), report.ToString());
            TestContext.WriteLine(report.ToString());
            Assert.Greater(zones, 400, "the census walked the world");
        }
    }
}
