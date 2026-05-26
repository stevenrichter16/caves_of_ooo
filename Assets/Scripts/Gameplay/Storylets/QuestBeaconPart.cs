using CavesOfOoo.Core;

namespace CavesOfOoo.Storylets
{
    /// <summary>
    /// Quest-giver discoverability beacon. On the <c>"Render"</c> event the
    /// ZoneRenderer fires per cell (it reads the event's <c>ColorString</c>
    /// back), this tints the carrying NPC a distinct "quest available" color
    /// while the quest it offers is <b>not yet started</b> (offerable) — so the
    /// player can spot quest-givers among ordinary villagers. Once the quest is
    /// active or completed the override stops and the NPC shows its themed
    /// color again.
    ///
    /// <para>Uses the existing render-event color-mutation hook
    /// (ZoneRenderer.cs ~902-916), so it needs NO overlay tilemap / renderer
    /// wiring. The renderer locks the GLYPH before firing "Render", so this
    /// signals via COLOR only; a floating "!" marker would need a dedicated
    /// overlay layer and is deferred (Docs/QUEST-GIVER-DISCOVERABILITY.md).</para>
    ///
    /// <para>Placed on quest-giver (offerer) NPCs by VillagePopulationBuilder.
    /// The handler is allocation-free and only does dict/set lookups, so it is
    /// safe on the per-cell render path.</para>
    /// </summary>
    public class QuestBeaconPart : Part
    {
        public override string Name => "QuestBeacon";

        /// <summary>The quest id this NPC offers (e.g. "ClearTheWarren").</summary>
        public string Quest;

        /// <summary>Render color while the quest is offerable (not started).
        /// Bright yellow — a recognizable "quest available" highlight.</summary>
        public string AvailableColor = "&Y";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "Render") return true;
            if (string.IsNullOrEmpty(Quest)) return true;

            var sp = StoryletPart.Current;
            if (sp == null) return true;

            // Highlight ONLY while the quest is offerable: neither active nor
            // already completed. (Mirrors IfQuestNotStarted's definition.)
            if (!sp.IsQuestActive(Quest) && !sp.IsQuestCompleted(Quest))
                e.SetParameter("ColorString", AvailableColor);

            return true;
        }
    }
}
