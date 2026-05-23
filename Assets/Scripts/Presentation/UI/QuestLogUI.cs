using UnityEngine;
using UnityEngine.Tilemaps;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Q1 (Docs/QUEST-LOG-UI.md) — full-screen ASCII quest log overlay,
    /// opened by the 'q' key. Renders active quests with per-stage status
    /// decoration (Qud-parity: √ done / ► current / · pending) plus a
    /// completed-quests section, drawn on the shared CP437 tilemap.
    ///
    /// <para>Mirrors <see cref="InventoryUI"/>'s draw + open/close shape.
    /// <see cref="InputHandler"/> drives open/close (it owns the
    /// <c>InputState</c>, the camera UI-view switch, and
    /// <c>ZoneRenderer.Paused</c>); this component owns only the snapshot
    /// + rendering + the in-overlay close key. State comes from the world
    /// singleton <see cref="StoryletPart.Current"/> via
    /// <see cref="QuestLogStateBuilder"/>, so no per-open wiring is
    /// needed.</para>
    /// </summary>
    public class QuestLogUI : MonoBehaviour
    {
        /// <summary>The shared CP437 tilemap (assigned by GameBootstrap,
        /// same instance InventoryUI draws to).</summary>
        public Tilemap Tilemap;

        // Fullscreen UI grid — matches InventoryUI (W=80, H=45) and the
        // CameraFollow.SetUIView dimensions InputHandler applies on open.
        private const int W = 80;
        private const int H = 45;

        private bool _isOpen;
        public bool IsOpen => _isOpen;

        private QuestLogSnapshot _snapshot;

        // Status markers — plain ASCII, color-coded. CRITICAL (PlayMode
        // verification, 2026-05-23): all glyphs here render via
        // CP437TilesetGenerator.GetTextTile (the narrow TEXT atlas), NOT
        // GetTile. The GAME atlas overrides letters with entity glyphs
        // ('T' = tree, 's' = snapjaw, ...) — UI text drawn through GetTile
        // shows trees/snapjaws mid-word. The TEXT atlas has no such
        // overrides (it's the path Sidebar/Hotbar use for legible text),
        // so letters AND these ASCII markers render correctly. Status is
        // conveyed by marker + color (green/yellow/grey).
        private const char GLYPH_DONE = '*';     // done    (green)
        private const char GLYPH_CURRENT = '>';  // current (yellow)
        private const char GLYPH_PENDING = '-';  // pending (grey)

        private static readonly Color ColTitle = new Color(1f, 0.9f, 0.4f);
        private static readonly Color ColHeader = new Color(0.6f, 0.85f, 1f);
        private static readonly Color ColQuest = Color.white;
        private static readonly Color ColDone = new Color(0.4f, 0.85f, 0.4f);
        private static readonly Color ColCurrent = new Color(1f, 0.85f, 0.3f);
        private static readonly Color ColPending = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color ColDim = new Color(0.5f, 0.5f, 0.5f);

        public void Open()
        {
            _isOpen = true;
            Rebuild();
            Render();
        }

        public void Close()
        {
            _isOpen = false;
            // Clear our glyphs (incl. rows 25-44 of the UI grid that the
            // zone re-render won't repaint); InputHandler.CloseQuestLog
            // restores the game camera view + un-pauses the renderer.
            if (Tilemap != null) Tilemap.ClearAllTiles();
        }

        /// <summary>Rebuild the snapshot from the world StoryletPart.</summary>
        public void Rebuild()
        {
            _snapshot = QuestLogStateBuilder.Build(StoryletPart.Current);
        }

        /// <summary>Process input while open. Returns true (consumes the
        /// frame's input). Closes on 'q' or Escape. Uses GetKeyDown so the
        /// 'q' that OPENED the log doesn't immediately close it (that press
        /// is consumed by InputHandler the prior frame).</summary>
        public bool HandleInput()
        {
            if (!_isOpen) return false;
            if (InputHelper.GetKeyDown(KeyCode.Q) || InputHelper.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return true;
            }
            return true;
        }

        private void Render()
        {
            if (Tilemap == null) return;
            Tilemap.ClearAllTiles();

            int y = 1;
            DrawText(2, y, "===== QUEST LOG =====", ColTitle);
            y += 2;

            if (_snapshot.ActiveCount == 0 && _snapshot.CompletedCount == 0)
            {
                DrawText(2, y, "You have no quests yet.", ColDim);
                DrawFooter();
                return;
            }

            // ── Active ──
            DrawText(2, y, "ACTIVE", ColHeader); y++;
            if (_snapshot.ActiveCount == 0)
            {
                DrawText(4, y, "(none)", ColDim); y++;
            }
            else
            {
                for (int i = 0; i < _snapshot.Active.Count && y < H - 4; i++)
                {
                    var e = _snapshot.Active[i];
                    DrawText(3, y, e.QuestId, ColQuest); y++;
                    if (e.Stages.Count == 0)
                    {
                        // Unresolved blueprint (content removed) — show index.
                        DrawText(6, y, "stage " + (e.CurrentStageIndex + 1), ColCurrent);
                        y++;
                    }
                    else
                    {
                        for (int j = 0; j < e.Stages.Count && y < H - 4; j++)
                        {
                            var row = e.Stages[j];
                            char g; Color c;
                            switch (row.Status)
                            {
                                case QuestLogStageStatus.Done: g = GLYPH_DONE; c = ColDone; break;
                                case QuestLogStageStatus.Current: g = GLYPH_CURRENT; c = ColCurrent; break;
                                default: g = GLYPH_PENDING; c = ColPending; break;
                            }
                            DrawChar(6, y, g, c);
                            string label = string.IsNullOrEmpty(row.StageId)
                                ? "(stage " + (j + 1) + ")" : row.StageId;
                            DrawText(8, y, label, c);
                            y++;
                        }
                    }
                    y++; // blank line between quests
                }
            }

            // ── Completed ──
            if (_snapshot.CompletedCount > 0 && y < H - 4)
            {
                y++;
                DrawText(2, y, "COMPLETED", ColHeader); y++;
                for (int i = 0; i < _snapshot.Completed.Count && y < H - 4; i++)
                {
                    DrawChar(6, y, GLYPH_DONE, ColDone);
                    DrawText(8, y, _snapshot.Completed[i], ColDim);
                    y++;
                }
            }

            DrawFooter();
        }

        private void DrawFooter()
        {
            DrawText(2, H - 2, "[q] or [Esc] to close", ColDim);
        }

        private void DrawChar(int x, int y, char c, Color color)
        {
            if (Tilemap == null || x < 0 || x >= W || y < 0 || y >= H) return;
            var pos = new Vector3Int(x, H - 1 - y, 0);
            // TEXT atlas (no entity-glyph overrides) — see GLYPH_* comment.
            var tile = CP437TilesetGenerator.GetTextTile(c);
            if (tile == null) return;
            Tilemap.SetTile(pos, tile);
            Tilemap.SetTileFlags(pos, TileFlags.None);
            Tilemap.SetColor(pos, color);
        }

        private void DrawText(int x, int y, string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            for (int i = 0; i < text.Length; i++)
            {
                int cx = x + i;
                if (cx >= W) break;
                DrawChar(cx, y, text[i], color);
            }
        }
    }
}
