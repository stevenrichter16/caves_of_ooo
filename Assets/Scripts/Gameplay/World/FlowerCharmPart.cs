using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The bleed-wilt instrument, owned by the flower itself.
    ///
    /// <para>Canon frames the conjured festival-flower as the Thinning's
    /// most intimate symptom — "Festival-flowers wilt before noon"
    /// (Lore/History/09_Magic.md:157). This part reads the zone's
    /// <see cref="Zone.UrquBleedLevel"/> the first time the flower
    /// stands through a turn and shortens its <see cref="LifespanPart"/>
    /// accordingly. Today nothing writes bleed above 0, so every flower
    /// gets the full season — the instrument is wired and dormant, which
    /// is thematically correct: the Thinning has not escalated at game
    /// start.</para>
    ///
    /// <para><b>Why the entity owns the formula.</b> The first cut put it
    /// in <c>SpreadFormationBuilder.FlowerMeadow</c>, which meant only the
    /// wilderness formation's flowers wilted; the same blueprint placed by
    /// the FestivalField stamp (through the generic <c>LandmarkBuilder</c>)
    /// never did. One object, two behaviours, keyed on which builder
    /// happened to place it — the cold-eye pass caught it
    /// (Docs/FELLING-W1-W2-PLAN.md §2.3). Now the flower knows how to
    /// wilt wherever it is put, and the numbers are content
    /// (<c>Objects.json</c>), not constants in a builder.</para>
    ///
    /// <para><b>Why lazily, on the first EndTurn.</b> There is no
    /// "placed in a zone" hook on an entity — <c>ObjectCreated</c> fires
    /// before it has one — but <c>EndTurn</c> carries the Zone. Rooting
    /// on the first tick costs at most one turn of default lifespan,
    /// invisible to the player.</para>
    ///
    /// <para><see cref="Rooted"/> is a PUBLIC field on purpose: the save
    /// system round-trips public fields, and a private flag would reset
    /// on load and re-root — handing every saved flower a fresh season.</para>
    /// </summary>
    public class FlowerCharmPart : Part
    {
        public override string Name => "FlowerCharm";

        /// <summary>Lifespan on a clean zone. A duration, not a lore claim.</summary>
        public int BaseDuration = 200;

        /// <summary>Turns shaved per unit of zone bleed. Content-feel;
        /// diag'd so it is auditable the day W7 writes bleed.</summary>
        public int WiltPerBleedUnit = 150;

        /// <summary>Never below this — canon's Consume ending says charms
        /// "still bloom, briefly, in the few unconsumed pockets": a wilting
        /// charm blooms short, it does not fail to bloom.</summary>
        public int MinDuration = 10;

        /// <summary>True once the zone's bleed has been read and applied.
        /// Public so it survives save/load (see class doc).</summary>
        public bool Rooted;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "EndTurn" || Rooted) return true;

            var zone = e.GetParameter<Zone>("Zone");
            if (zone == null) return true;   // no zone context yet — try next tick

            Rooted = true;
            int duration = ComputeDuration(zone.UrquBleedLevel);

            var lifespan = ParentEntity?.GetPart<LifespanPart>();
            if (lifespan != null) lifespan.TurnsRemaining = duration;

            // Only the instrument speaking is worth a record: at bleed 0
            // nothing wilted and eighty flowers would say so eighty times.
            if (duration < BaseDuration && Diag.IsChannelEnabled("event"))
            {
                Diag.Record("event", "CharmWilted", payload: new
                {
                    zoneID = zone.ZoneID,
                    bleedLevel = zone.UrquBleedLevel,
                    baseDuration = BaseDuration,
                    wiltPerUnit = WiltPerBleedUnit,
                    finalDuration = duration,
                });
            }
            return true;
        }

        /// <summary>The wilt formula: base minus bleed×rate, floored.
        /// Bleed below zero reads as zero — nothing lengthens the season.</summary>
        public int ComputeDuration(float bleedLevel)
        {
            if (bleedLevel < 0f || float.IsNaN(bleedLevel)) bleedLevel = 0f;
            // Compare in float BEFORE the int cast: a pathological bleed
            // (or WiltPerBleedUnit) would overflow the cast to int.MinValue
            // and turn "wilt completely" into a wrapped-around bonus.
            float shaved = bleedLevel * WiltPerBleedUnit;
            if (shaved >= BaseDuration - MinDuration) return MinDuration;
            return BaseDuration - (int)shaved;
        }
    }
}
