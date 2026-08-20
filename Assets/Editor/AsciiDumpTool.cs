using System.IO;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.EditorTools
{
    /// <summary>
    /// Headless look-pass tool (W2.8). The W1.1b lesson made "look at
    /// the picture" a mandatory gate for terrain work; this makes the
    /// picture producible without a GUI editor:
    ///
    ///   Unity -batchmode -projectPath ... -executeMethod
    ///     CavesOfOoo.EditorTools.AsciiDumpTool.Run -quit
    ///
    /// Zone ids come from ASCII_DUMP_ZONES (semicolon-separated env
    /// var), output goes to ASCII_DUMP_OUT (default
    /// /tmp/claude_ascii_dump.txt). Top render layer wins per cell —
    /// the same rule the real renderer uses.
    /// </summary>
    public static class AsciiDumpTool
    {
        public static void Run()
        {
            string zones = System.Environment.GetEnvironmentVariable("ASCII_DUMP_ZONES")
                ?? "Overworld.8.16.0";
            string outPath = System.Environment.GetEnvironmentVariable("ASCII_DUMP_OUT")
                ?? "/tmp/claude_ascii_dump.txt";

            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            FactionManager.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Factions.json")));

            var mgr = new OverworldZoneManager(factory, worldSeed: 42);
            var sb = new StringBuilder();
            foreach (string id in zones.Split(';'))
            {
                var zone = mgr.GetZone(id.Trim());
                sb.Append("=== ").Append(id.Trim()).Append(" ===\n");
                if (zone == null) { sb.Append("(failed to generate)\n"); continue; }
                for (int y = 0; y < Zone.Height; y++)
                {
                    for (int x = 0; x < Zone.Width; x++)
                    {
                        var cell = zone.GetCell(x, y);
                        string glyph = " ";
                        int best = int.MinValue;
                        for (int i = 0; i < cell.Objects.Count; i++)
                        {
                            var r = cell.Objects[i]?.GetPart<RenderPart>();
                            if (r == null || string.IsNullOrEmpty(r.RenderString)) continue;
                            if (r.RenderLayer >= best) { best = r.RenderLayer; glyph = r.RenderString; }
                        }
                        sb.Append(glyph);
                    }
                    sb.Append('\n');
                }
            }
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log("[AsciiDumpTool] wrote " + outPath);
        }
    }
}
