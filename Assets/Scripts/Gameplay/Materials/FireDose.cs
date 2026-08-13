namespace CavesOfOoo.Core
{
    /// <summary>
    /// How much heat a fire effect delivers, on a scale calibrated against
    /// the temperatures things actually catch fire at.
    ///
    /// <para><b>The bug this exists to prevent.</b> Every fire ability had
    /// its own hand-picked joule figure — 50, 150, 200, <c>damage * 5</c> —
    /// and none of them had been checked against a
    /// <c>ThermalPart.FlameTemperature</c>. <c>HandleApplyHeat</c> converts
    /// joules to degrees as <c>delta = joules / HeatCapacity</c>, so
    /// igniting something from ambient costs roughly
    /// <c>(FlameTemperature - ambient) x HeatCapacity</c> joules —
    /// <b>hundreds</b>. Flaming Hands delivered <c>damage * 5</c>, i.e.
    /// 5-20, which per-turn ambient decay ate faster than it accumulated:
    /// at level 1 it could not ignite <i>anything</i>, ever, no matter how
    /// many times it was cast. That is what the player reported.</para>
    ///
    /// <para><b>Why it went unnoticed.</b> The scale is asymmetric. Cold
    /// only has to cross 25 degrees (ambient 25 down to a freeze point of
    /// 0), so every cold dose — even -100 — freezes its target in one
    /// cast and looks fine. Heat has to cross 295-475. The doses were
    /// sized for the cold half of the scale and never re-checked against
    /// the hot half.</para>
    ///
    /// <para><b>The anchor.</b> <see cref="TinderIgnition"/> is the dose
    /// that takes dry tinder — a bush, <c>FlameTemperature</c> 320,
    /// <c>HeatCapacity</c> 1.0 — from ambient to alight in ONE
    /// application. Everything else is a multiple of it, so re-tuning the
    /// family means moving one number. Measured against the shipped
    /// blueprints:</para>
    ///
    /// <code>
    /// dose               Bush  Hedge  Tree  Chest  Creature  Wall
    /// Cantrip   (0.5x)      3      3     7      4         8  never
    /// Attack    (1.0x)      1      2     3      2         4  never
    /// Ignition  (2.0x)      1      1     2      1         2  never
    /// Blast     (3.0x)      1      1     1      1         2  never
    /// </code>
    ///
    /// <para>("never" for stone is belt-and-braces: masonry is authored
    /// <c>Combustibility 0</c>, so <c>MaterialPart.HandleTryIgnite</c>
    /// vetoes it whatever its temperature reaches.)</para>
    ///
    /// <para>These are numbers a test pins — see
    /// <c>FireIgnitionTests</c>. If someone re-tunes a dose down far
    /// enough to stop igniting tinder, that test fails rather than the
    /// spell quietly going inert again.</para>
    /// </summary>
    public static class FireDose
    {
        /// <summary>
        /// The anchor: one application lights dry tinder. Derived as
        /// (320 flame - 25 ambient) x 1.0 capacity, rounded up.
        /// </summary>
        public const float TinderIgnition = 300f;

        /// <summary>A flicker. Lights tinder in 3, a tree in 7.</summary>
        public const float Cantrip = TinderIgnition * 0.5f;

        /// <summary>A proper fire attack. Tinder in 1, a tree in 3.</summary>
        public const float Attack = TinderIgnition * 1.0f;

        /// <summary>A spell whose whole purpose is setting things alight.</summary>
        public const float Ignition = TinderIgnition * 2.0f;

        /// <summary>The big one. Everything flammable, at once.</summary>
        public const float Blast = TinderIgnition * 3.0f;

        /// <summary>
        /// What a burning thing radiates to its neighbours each turn,
        /// BEFORE <c>MaterialSimSystem.EmitHeatToAdjacent</c> divides it
        /// across the eight directions — so each neighbour receives an
        /// eighth of this.
        ///
        /// <para>The old figure was <c>Intensity * 30</c>, i.e. 3.75 per
        /// neighbour, which put a bush's equilibrium temperature at 100
        /// degrees against a flame point of 320: <b>fire could not spread
        /// at all</b>, at any intensity, for any duration. At this figure
        /// an adjacent bush catches in about three turns — visible, and
        /// slow enough to walk away from.</para>
        /// </summary>
        public const float SpreadTotal = TinderIgnition * 4f;

        /// <summary>
        /// Per-turn heat a burning thing puts back into ITSELF.
        ///
        /// <para>Ambient decay pulls a burning object back toward room
        /// temperature every turn; if the fire does not at least match
        /// that, it cools below its own flame point and
        /// <c>TryExtinguish</c> puts it out from underneath the effect. On
        /// heavy fuel (a tree, capacity 2.5) the old flat
        /// <c>Intensity * 20</c> lost about 7 degrees a turn.</para>
        ///
        /// <para>Scaled by capacity and by how far above ambient the thing
        /// is sitting, so it offsets decay rather than racing away: a fire
        /// stays lit for as long as it has fuel, and no longer.</para>
        /// </summary>
        public static float SelfSustain(float intensity, float heatCapacity,
            float temperature, float ambient, float decayRate)
        {
            float lostToDecay = (temperature - ambient) * decayRate * heatCapacity;
            // A little over break-even, so intensity still means something:
            // a fiercer fire runs hotter rather than merely holding.
            return lostToDecay + intensity * 20f;
        }
    }
}
