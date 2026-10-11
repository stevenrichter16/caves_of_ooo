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
        /// <summary>Zero-based quest page, independent of the field-note page.</summary>
        public int QuestPage { get; private set; }
        public int QuestPageCount { get; private set; } = 1;
        private const int QuestLinesPerPage = 36;
        private readonly List<JournalRow> _questRows = new List<JournalRow>();
        private readonly struct JournalRow
        {
            public readonly int X, MarkerX;
            public readonly string Text;
            public readonly Color Color;
            public readonly char Marker;
            public JournalRow(int x, string text, Color color, int markerX = -1, char marker = '\0')
            { X = x; Text = text; Color = color; MarkerX = markerX; Marker = marker; }
        }
        private const int NotesLinesPerPage=34;
        private readonly List<string> _noteLines=new List<string>();
        private sealed class JournalKeys:IInputProbe {public bool GetKeyDown(KeyCode key)=>InputHelper.GetKeyDown(key);}
        private static readonly IInputProbe LiveKeys=new JournalKeys();

        // Status markers — plain ASCII, color-coded. CRITICAL (PlayMode
        // verification, 2026-05-23): all glyphs here render via
        // CP437TilesetGenerator.GetTextTile (the narrow TEXT atlas), NOT
        // GetTile. The GAME atlas overrides letters with entity glyphs
        // ('T' = tree, 's' = marlback, ...) — UI text drawn through GetTile
        // shows trees/marlbacks mid-word. The TEXT atlas has no such
        // overrides (it's the path Sidebar/Hotbar use for legible text),
        // so letters AND these ASCII markers render correctly. Status is
        // conveyed by marker + color (green/yellow/grey).
        private const char GLYPH_DONE = '*';     // done    (green)
        private const char GLYPH_CURRENT = '>';  // current (yellow)
        private const char GLYPH_PENDING = '-';
        private const char GLYPH_REFUSED = '-';  // pending (grey)

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
            NotesVisible=false;NotesPage=0;QuestPage=0;
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
            var player = StoryletPart.LocalPlayer;
            var zone = player?.SpatialZone;
            var manager = WorldLocationContext.For(zone);
            // Current player graph supplies identity; opening a journal never
            // resolves a destination or manufactures an old world's key.
            string worldKey = zone != null && manager?.CachedZones != null
                && manager.CachedZones.TryGetValue(zone.ZoneID, out var current) && ReferenceEquals(current, zone)
                && zone.GetEntityCell(player) != null ? manager.Exploration?.WorldKey : null;
            AppendNotes(SpreadDiscoveryNotes.Read(player, worldKey));
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
            // ES.3: renounce lost acts aloud from the journal; only acts whose giver is gone are touched.
            if(!NotesVisible&&input.GetKeyDown(KeyCode.R)){if((StoryletPart.Current?.RenounceLost(StoryletPart.LocalPlayer)??0)>0){Rebuild();Render();}return true;}
            int delta=input.GetKeyDown(KeyCode.PageDown)||input.GetKeyDown(KeyCode.RightArrow)?1:
                input.GetKeyDown(KeyCode.PageUp)||input.GetKeyDown(KeyCode.LeftArrow)?-1:0;
            if(delta!=0)
            {
                if(NotesVisible) NotesPage+=delta; else QuestPage+=delta;
                Rebuild();Render();
            }
            return true;
        }

        private void Render()
        {
            if (!NotesVisible) BuildQuestRows();
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

            DrawText(2, 1, "===== QUEST LOG =====", ColTitle);
            DrawText(2, 2, "closure  " + _snapshot.ClosureClosed + " closed  " + _snapshot.ClosureRefused
                + " refused  " + _snapshot.ClosureOpen + " open", ColDim);
            int first = QuestPage * QuestLinesPerPage;
            for (int i = 0; i < QuestLinesPerPage && first + i < _questRows.Count; i++)
            {
                var row = _questRows[first + i];
                if (row.MarkerX >= 0) DrawChar(row.MarkerX, 4 + i, row.Marker, row.Color);
                DrawText(row.X, 4 + i, row.Text, row.Color);
            }
            DrawText(2, H - 4, "Quests: page " + (QuestPage + 1) + " / " + QuestPageCount, ColDim);
            DrawFooter();
        }

        // Build the entire presentation before selecting a page. Truncating
        // during layout used to permanently hide later objectives and history.
        // This runs only for explicit journal input/open/rebuild, never Update.
        private void BuildQuestRows()
        {
            _questRows.Clear();
            if (_snapshot.ActiveCount == 0 && _snapshot.CompletedCount == 0
                && _snapshot.RefusedCount == 0 && _snapshot.UnspokenCount == 0)
                AddQuestText(2, "You have no quests yet.", ColDim);
            else
            {
                AddQuestText(2, "ACTIVE", ColHeader);
                if (_snapshot.ActiveCount == 0) AddQuestText(4, "(none)", ColDim);
                foreach (var entry in _snapshot.Active)
                {
                    AddQuestText(3, StoryletPart.QuestDisplayName(entry.QuestId), ColQuest);
                    if (entry.Stages.Count == 0) AddQuestText(6, "stage " + (entry.CurrentStageIndex + 1), ColCurrent);
                    for (int j = 0; j < entry.Stages.Count; j++)
                    {
                        var stage = entry.Stages[j];
                        bool done = stage.Status == QuestLogStageStatus.Done;
                        bool current = stage.Status == QuestLogStageStatus.Current;
                        Color color = done ? ColDone : current ? ColCurrent : ColPending;
                        char marker = done ? GLYPH_DONE : current ? GLYPH_CURRENT : GLYPH_PENDING;
                        AddQuestText(8, string.IsNullOrEmpty(stage.StageId) ? "(stage " + (j + 1) + ")" : stage.StageId, color, 6, marker);
                        if (!current) continue;
                        foreach (var objective in entry.CurrentObjectives)
                        {
                            string text = !string.IsNullOrEmpty(objective.Text) ? objective.Text
                                : !string.IsNullOrEmpty(objective.ObjectiveId) ? objective.ObjectiveId : "(objective)";
                            if (objective.HasProgress) text += " (" + objective.Current + "/" + objective.Target + ")";
                            if (objective.Optional) text += " (optional)";
                            AddQuestText(12, text, objective.Done ? ColDone : ColPending, 10, objective.Done ? GLYPH_DONE : GLYPH_PENDING);
                        }
                    }
                    AddQuestText(2, "", ColDim);
                }
                AddQuestSection("COMPLETED", _snapshot.Completed, ColDim, GLYPH_DONE);
                AddQuestSection("UNSPOKEN", _snapshot.Unspoken, ColPending, GLYPH_PENDING);
                AddQuestSection("REFUSED", _snapshot.Refused, ColDim, GLYPH_REFUSED);
            }
            QuestPageCount = Mathf.Max(1, (_questRows.Count + QuestLinesPerPage - 1) / QuestLinesPerPage);
            QuestPage = Mathf.Clamp(QuestPage, 0, QuestPageCount - 1);
        }

        private void AddQuestSection(string label, IReadOnlyList<string> entries, Color color, char marker)
        {
            if (entries.Count == 0) return;
            AddQuestText(2, "", ColDim);
            AddQuestText(2, label, ColHeader);
            foreach (string text in entries) AddQuestText(8, text, color, 6, marker);
        }

        private void AddQuestText(int x, string text, Color color, int markerX = -1, char marker = '\0')
        {
            foreach (string line in WrapJournalText(text, W - x - 2))
            {
                _questRows.Add(new JournalRow(x, line, color, markerX, marker));
                markerX = -1;
            }
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
            string hint = NativeGamepadInput.IsConnected
                ? "[Y] quests / notes  [LB/RB] pages  [B] close"
                : "[Tab] quests / notes  [PgUp/PgDn] pages  [Q/Esc] close";
            DrawText(2, H - 2, (!NotesVisible && _snapshot.LostCount > 0 ? "[R] renounce lost  " : "") + hint, ColDim);
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
