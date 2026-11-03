using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>Projects the actual rendered menu text into a readable browser
    /// reader. Menu navigation still runs through the game's existing controllers.</summary>
    public static class PhoneMenuMirror
    {
        [Serializable] public sealed class Row { public string text, color; }
        [Serializable] private sealed class View
        {
            public int revision;
            public string mode, title, hp, location, name, ability, modal;
            public bool boot, dead, naming;
            public List<Row> rows = new List<Row>();
            public List<string> messages;
        }
        internal static int Revision { get; private set; }
        internal static void MarkDirty() { if (PhoneInput.Enabled) Revision++; }
        private static readonly List<SkillScreenText> Labels = new List<SkillScreenText>(128);

        public static void Publish(InputHandler input, string mode, string title, bool boot, bool dead, string name)
        {
            if (!PhoneInput.Enabled) return;
            var view = new View {
                revision = PhoneInput.Gate.Revision, mode = mode, title = title, boot = boot, dead = dead,
                ability = input.PhoneAbilityName,
                naming = name != null, name = name ?? "",
                hp = input.PlayerEntity == null ? "" : "HP " + input.PlayerEntity.GetStatValue("Hitpoints"),
                location = input.CurrentZone?.ZoneID ?? "",
                messages = MessageLog.GetRecent(5)
            };
            if (mode == "Menu" || mode == "Skills")
            {
                if (input.InventoryUI?.AppendPhoneRows(view.rows) == true) view.modal = input.InventoryUI.PhoneModalKey;
                else if (input.SkillsScreenUI?.AppendPhoneRows(view.rows) == true) view.modal = input.SkillsScreenUI.PhoneModalKey;
                else
                {
                    Labels.Clear(); input.GetComponentsInChildren(false, Labels);
                    foreach (var label in Labels)
                        if (label.isActiveAndEnabled && !string.IsNullOrWhiteSpace(label.Content))
                            view.rows.Add(new Row { text = label.Content, color = "#" + ColorUtility.ToHtmlStringRGB(label.color) });
                }
                if (view.rows.Count == 0)
                {
                    AddTilemap(view.rows, input.ZoneRenderer?.CenteredPopupFgTilemap);
                    if (view.rows.Count == 0) AddTilemap(view.rows, input.ZoneRenderer?.PopupFgTilemap);
                }
            }
            if (mode == "Targeting" && input.PhoneUsesCursor && input.PhoneLookSnapshot != null)
            {
                var look = input.PhoneLookSnapshot;
                view.rows.Add(new Row { text = look.Header, color = "#e7c878" });
                view.rows.Add(new Row { text = look.Summary, color = "#e5e4cf" });
                foreach (var line in look.DetailLines) view.rows.Add(new Row { text = line, color = "#e5e4cf" });
            }
            if (mode == "Targeting" && !input.PhoneUsesCursor)
            {
                string selection = PhoneInput.TrySelectedDirection(out int dx, out int dy)
                    ? (dy < 0 ? "north" : dy > 0 ? "south" : "") + (dx < 0 ? "west" : dx > 0 ? "east" : "") : "none";
                view.rows.Add(new Row { text = "Direction: " + selection + ". Choose a direction, then confirm.", color = "#e7c878" });
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            CooPhoneView(JsonUtility.ToJson(view));
#endif
        }

        private static void AddTilemap(List<Row> rows, Tilemap map)
        {
            if (map == null) return;
            var text = new StringBuilder(80);
            for (int y = 44; y >= 0; y--)
            {
                text.Clear(); Color ink = Color.white;
                for (int x = 0; x < 80; x++)
                {
                    var at = new Vector3Int(x, y, 0);
                    var tile = map.GetTile(at);
                    char glyph = ' ';
                    if (tile != null && tile.name.StartsWith("UI_", StringComparison.Ordinal)
                        && int.TryParse(tile.name.Substring(3), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                    {
                        // Borders are presentation; preserve printable content and list markers.
                        if (code >= 32 && code < 127) { glyph = (char)code; ink = map.GetColor(at); }
                        else if (code == 16 || code == 26) glyph = '>';
                        else if (code == 17 || code == 27) glyph = '<';
                        else if (code == 7) glyph = '*';
                    }
                    text.Append(glyph);
                }
                string line = text.ToString().Trim();
                if (line.Length > 0) rows.Add(new Row { text = line, color = "#" + ColorUtility.ToHtmlStringRGB(ink) });
            }
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void CooPhoneView(string json);
#endif
    }
}
