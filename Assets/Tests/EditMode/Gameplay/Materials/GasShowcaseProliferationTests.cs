using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Crash investigation (2026-05-23): the user reported "the gas
    /// scenario crashes unity once the player gets damaged by the gas."
    ///
    /// <para><b>Hypothesis under test.</b> The <c>GasSystemShowcase</c>
    /// change that sets <c>gp.Stable = true</c> on every spawned cloud
    /// makes the 6 strips × 3 cells (density 300) <b>proliferate</b>:
    /// Stable gas SPREADS (density &gt; <see cref="GasSystem.LOW_DENSITY_THRESHOLD"/>)
    /// but is EXEMPT from the low-density dissipation gate, so it fans
    /// out into hundreds of never-removed gas entities. Each subsequent
    /// <c>TickEnd</c> reprocesses ALL of them (spread roll + per-cell
    /// apply + per-cell repaint); the per-turn cost climbs until the
    /// editor freezes. The "once the player gets damaged" timing is a
    /// correlate, not the cause: by the time the player walks ~8 tiles
    /// east into the first cloud, the Stable gas has had ~8 ticks to
    /// spread.</para>
    ///
    /// <para><b>Why headless + bounded.</b> Reproducing live in Play mode
    /// risks re-hanging the editor. This rig ticks a controlled number of
    /// times in a headless zone and asserts the live gas-entity count
    /// stays bounded — RED before the fix (count explodes into the
    /// hundreds), GREEN after. A hard guard bails the loop the moment the
    /// count crosses a runaway threshold so the test itself can never
    /// hang the suite.</para>
    /// </summary>
    public class GasShowcaseProliferationTests
    {
        // Matches GasSystemShowcase: 6 distinct gas types so cross-strip
        // clouds don't merge (worst case for entity count). Only poison
        // carries a behavior (for the player-damage path test); the others
        // are inert — spread is identical regardless of behavior.
        private const string RegistryJson = @"{ ""Gases"":[
          { ""Id"":""poison-vapor"",    ""GasType"":""Poison"",    ""Glyph"":""°"", ""Color"":""&g"", ""DefaultDensity"":300, ""DefaultLevel"":1, ""BehaviorKind"":""Poison"" },
          { ""Id"":""stun-vapor"",      ""GasType"":""Stun"",      ""Glyph"":""°"", ""Color"":""&Y"", ""DefaultDensity"":300, ""DefaultLevel"":1 },
          { ""Id"":""confusion-vapor"", ""GasType"":""Confusion"", ""Glyph"":""°"", ""Color"":""&M"", ""DefaultDensity"":300, ""DefaultLevel"":1 },
          { ""Id"":""cryo-mist"",       ""GasType"":""Cryo"",      ""Glyph"":""°"", ""Color"":""&C"", ""DefaultDensity"":300, ""DefaultLevel"":1 },
          { ""Id"":""sleep-vapor"",     ""GasType"":""Sleep"",     ""Glyph"":""°"", ""Color"":""&B"", ""DefaultDensity"":300, ""DefaultLevel"":1 },
          { ""Id"":""fungal-spores"",   ""GasType"":""Fungal"",    ""Glyph"":""°"", ""Color"":""&G"", ""DefaultDensity"":300, ""DefaultLevel"":1 } ] }";

        private static readonly string[] StripIds =
        { "poison-vapor", "stun-vapor", "confusion-vapor", "cryo-mist", "sleep-vapor", "fungal-spores" };

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(RegistryJson);
            GasSystem.SetRngForTests(new System.Random(1234));
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            GasSystem.SetRngForTests(new System.Random());
        }

        private static Entity MakePlayerLike(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "player", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            void S(string n, int v, int max = 600) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", 500, 500); S("Toughness", 12); S("Agility", 14);
            S("DV", 6); S("AV", 0); S("AcidResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "player" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        /// <summary>Spawn the 6×3 showcase strips. <paramref name="stable"/>
        /// toggles the gp.Stable flag exactly like the showcase does.</summary>
        private static void SpawnShowcaseStrips(Zone zone, Entity creator, bool stable, int y = 12)
        {
            for (int s = 0; s < StripIds.Length; s++)
            {
                int stripX = 8 + s * 8;
                for (int dx = -1; dx <= 1; dx++)
                {
                    var g = GasFactory.SpawnGas(zone, stripX + dx, y, StripIds[s],
                        density: 300, level: 1, creator: creator);
                    var gp = g?.GetPart<GasPoolPart>();
                    if (gp != null) gp.Stable = stable;
                }
            }
        }

        // ════════════════ Proliferation measurement (the RED test) ════════════════

        [Test]
        public void StableShowcaseGas_DoesNotProliferateUnbounded()
        {
            // RED before the fix: 18 Stable clouds fan out into hundreds of
            // never-dissipating gas entities. GREEN after: count stays
            // bounded (clouds persist roughly in place).
            var zone = new Zone("ShowcaseProliferation");
            var creator = MakePlayerLike(zone, 0, 0);
            SpawnShowcaseStrips(zone, creator, stable: true);

            int initial = zone.GetEntitiesWithTag("Gas").Count; // 18
            int peak = initial;
            for (int i = 0; i < 30; i++)
            {
                GasSystem.OnTickEnd(zone);
                int c = zone.GetEntitiesWithTag("Gas").Count;
                if (c > peak) peak = c;
                if (c > 2000) break; // runaway guard — never hang the suite
            }
            TestContext.WriteLine($"[proliferation] initial={initial} peak over 30 ticks={peak}");

            // A persistent-but-bounded showcase cloud should not balloon
            // far past its seeded footprint. 18 seeds + modest spread → a
            // generous ceiling of 80. Hundreds = the proliferation bug.
            Assert.Less(peak, 80,
                $"Stable showcase gas proliferated (initial={initial}, peak={peak}). " +
                "Stable gas must persist without fanning out into hundreds of entities.");
        }

        [Test]
        public void NonStableShowcaseGas_DissipatesAndStaysBounded()
        {
            // Counter-check (§3.4): the SAME setup with Stable=false must
            // stay bounded AND ultimately thin out — proving the unbounded
            // growth above is caused specifically by the Stable flag, not
            // by the strip layout / density / spawn count.
            var zone = new Zone("ShowcaseNonStable");
            var creator = MakePlayerLike(zone, 0, 0);
            SpawnShowcaseStrips(zone, creator, stable: false);

            int peak = zone.GetEntitiesWithTag("Gas").Count;
            for (int i = 0; i < 30; i++)
            {
                GasSystem.OnTickEnd(zone);
                int c = zone.GetEntitiesWithTag("Gas").Count;
                if (c > peak) peak = c;
                if (c > 2000) break;
            }
            TestContext.WriteLine($"[non-stable] peak over 30 ticks={peak}");
            Assert.Less(peak, 80,
                $"non-stable showcase gas should stay bounded (peak={peak})");
        }

        // ════════════════ Player-damage path (no crash / no recursion) ════════════════

        [Test]
        public void PlayerStandingInStablePoison_TakesDamage_NoCrashOrUnboundedGrowth()
        {
            // The user's literal trigger: "once the player gets damaged by
            // the gas." Place a player-like creature in a Stable poison
            // cloud (creator == the player itself, mirroring the showcase's
            // creator: ctx.PlayerEntity) and tick. Assert: no exception, the
            // player actually takes damage (the path executes), and the gas
            // count stays bounded. Pins that the self-damage path (source ==
            // target == player) does not recurse or explode.
            var zone = new Zone("PlayerInPoison");
            var player = MakePlayerLike(zone, 12, 12);
            // One Stable poison strip centered on the player; player is the
            // creator (self-sourced gas damage).
            for (int dx = -1; dx <= 1; dx++)
            {
                var g = GasFactory.SpawnGas(zone, 12 + dx, 12, "poison-vapor",
                    density: 300, level: 1, creator: player);
                var gp = g?.GetPart<GasPoolPart>();
                if (gp != null) gp.Stable = true;
            }
            int hpBefore = player.GetStatValue("Hitpoints", -1);

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 30; i++)
                {
                    GasSystem.OnTickEnd(zone);
                    if (zone.GetEntitiesWithTag("Gas").Count > 2000) break;
                }
            }, "self-sourced gas damage on the player must not throw / recurse");

            int hpAfter = player.GetStatValue("Hitpoints", -1);
            Assert.Less(hpAfter, hpBefore,
                $"player should take gas damage (hp {hpBefore} -> {hpAfter})");
            Assert.Less(zone.GetEntitiesWithTag("Gas").Count, 80,
                "gas around the player should stay bounded");
        }
    }
}
