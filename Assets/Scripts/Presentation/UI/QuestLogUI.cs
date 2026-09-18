using UnityEngine;
using System.Collections.Generic;
using CavesOfOoo.Core;
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
        public bool NotesVisible { get; private set; }
        public int NotesPage { get; private set; }
        private const int NotesLinesPerPage=34;
        private readonly List<string> _noteLines=new List<string>();
        private sealed class JournalKeys:IInputProbe {public bool GetKeyDown(KeyCode key)=>InputHelper.GetKeyDown(key);}
        private static readonly IInputProbe LiveKeys=new JournalKeys();

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
            NotesVisible=false;NotesPage=0;
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
            _noteLines.Clear();
            AppendNotes(RegionalTravelNotes.Read(StoryletPart.LocalPlayer));
            AppendNotes(RegionalSituationNotes.Read(StoryletPart.LocalPlayer));
            NotesPage=Mathf.Clamp(NotesPage,0,Mathf.Max(0,(_noteLines.Count-1)/NotesLinesPerPage));
        }

        private void AppendNotes(IReadOnlyList<string> notes)
        {
            foreach (string note in notes)
            {
                foreach (string line in WrapJournalText(note, 74)) _noteLines.Add(line);
                _noteLines.Add("");
            }
        }

        /// <summary>Process input while open. Returns true (consumes the
        /// frame's input). Closes on 'q' or Escape. Uses GetKeyDown so the
        /// 'q' that OPENED the log doesn't immediately close it (that press
        /// is consumed by InputHandler the prior frame).</summary>
        public bool HandleInput()=>HandleInput(LiveKeys);
        public bool HandleInput(IInputProbe input)
        {
            if (!_isOpen || input==null) return false;
            if (input.GetKeyDown(KeyCode.Q) || input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return true;
            }
            if(input.GetKeyDown(KeyCode.Tab)){NotesVisible=!NotesVisible;Rebuild();Render();return true;}
            if(NotesVisible)
            {
                int delta=input.GetKeyDown(KeyCode.PageDown)||input.GetKeyDown(KeyCode.RightArrow)?1:
                    input.GetKeyDown(KeyCode.PageUp)||input.GetKeyDown(KeyCode.LeftArrow)?-1:0;
                if(delta!=0){NotesPage+=delta;Rebuild();Render();}
            }
            return true;
        }

        private void Render()
        {
            if (Tilemap == null) return;
            Tilemap.ClearAllTiles();
            if(NotesVisible)
            {
                DrawText(2,1,"===== FIELD NOTES =====",ColTitle);
                if(_noteLines.Count==0)DrawText(2,3,"Ask a scribe or innkeeper for nearby destinations.",ColDim);
                for(int i=0;i<NotesLinesPerPage&&NotesPage*NotesLinesPerPage+i<_noteLines.Count;i++)
                    DrawText(2,3+i,_noteLines[NotesPage*NotesLinesPerPage+i],ColQuest);
                DrawText(2,H-4,"Requests and directions: page "+(NotesPage+1)+" / "+Mathf.Max(1,(_noteLines.Count+NotesLinesPerPage-1)/NotesLinesPerPage),ColDim);
                DrawFooter();return;
            }

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
                    // Same answer as the message log — two surfaces
                    // disagreeing about a quest's name is the bug the
                    // display-name seam exists to remove.
                    DrawText(3, y, StoryletPart.QuestDisplayName(e.QuestId), ColQuest); y++;
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

                            // Q3.4: under the CURRENT stage, list its objectives
                            // as indented done/pending sub-rows.
                            if (row.Status == QuestLogStageStatus.Current
                                && e.CurrentObjectives.Count > 0)
                            {
                                for (int k = 0; k < e.CurrentObjectives.Count && y < H - 4; k++)
                                {
                                    var o = e.CurrentObjectives[k];
                                    char og = o.Done ? GLYPH_DONE : GLYPH_PENDING;
                                    Color oc = o.Done ? ColDone : ColPending;
                                    DrawChar(10, y, og, oc);
                                    string olabel = !string.IsNullOrEmpty(o.Text) ? o.Text
                                        : (!string.IsNullOrEmpty(o.ObjectiveId) ? o.ObjectiveId : "(objective)");
                                    // Live counter for collect/kill-N objectives ("(1/3)").
                                    if (o.HasProgress) olabel += " (" + o.Current + "/" + o.Target + ")";
                                    if (o.Optional) olabel += " (optional)";
                                    // Objectives contain actionable directions; never silently
                                    // discard their return/contact clause at the right edge.
                                    foreach(string line in WrapJournalText(olabel,W-14))
                                    {
                                        if(y>=H-4)break;
                                        DrawText(12,y,line,oc);y++;
                                    }
                                }
                            }
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

        private static IEnumerable<string> WrapJournalText(string text,int width)
        {
            if(string.IsNullOrEmpty(text)){yield return "";yield break;}
            string remaining=text;
            while(remaining.Length>width)
            {
                int cut=remaining.LastIndexOf(' ',width-1,width);if(cut<1)cut=width;
                yield return remaining.Substring(0,cut);remaining=remaining.Substring(cut).TrimStart();
            }
            yield return remaining;
        }

        private void DrawFooter()
        {
            DrawText(2, H - 2, "[Tab] quests / field notes  [PgUp/PgDn] pages  [Q/Esc] close", ColDim);
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
