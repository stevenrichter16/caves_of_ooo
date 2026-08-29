using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Qud parity — pressing `&gt;` when you are not standing on the
    /// stairs walks you to them. Qud's CmdMoveD does not scold you for
    /// being in the wrong cell; it travels. This is the pure half:
    /// finding the right staircase and deciding when to stop walking.
    ///
    /// <para>The interrupt rule matters as much as the walk. Auto-travel
    /// that keeps going while something hostile closes on you is how a
    /// player loses a character to a convenience feature.</para>
    /// </summary>
    public class StairTravelTests
    {
        [SetUp]
        public void SetUp() => FactionManager.Initialize();

        private static Zone Floored(string id = "Travel")
        {
            var zone = new Zone(id);
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var floor = new Entity { ID = $"f{x}_{y}", BlueprintName = "Floor" };
                    floor.AddPart(new RenderPart { DisplayName = "floor" });
                    zone.AddEntity(floor, x, y);
                }
            return zone;
        }

        private static Entity Stairs(Zone zone, int x, int y, bool down)
        {
            var s = new Entity { ID = $"s{x}_{y}", BlueprintName = down ? "StairsDown" : "StairsUp" };
            s.AddPart(new RenderPart { DisplayName = down ? "stairs down" : "stairs up" });
            if (down) s.AddPart(new StairsDownPart()); else s.AddPart(new StairsUpPart());
            zone.AddEntity(s, x, y);
            return s;
        }

        private static Entity Walker(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "player", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = "you" });
            e.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════
        //   Finding the stairs
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ItFindsTheStairsYouAskedFor()
        {
            var zone = Floored();
            Stairs(zone, 50, 12, down: true);
            Stairs(zone, 20, 4, down: false);
            var you = Walker(zone, 10, 10);

            var down = StairTravel.FindTarget(zone, you, goingDown: true);
            Assert.IsTrue(down.HasValue, "there is a way down in this chunk");
            Assert.AreEqual((50, 12), (down.Value.x, down.Value.y));

            var up = StairTravel.FindTarget(zone, you, goingDown: false);
            Assert.AreEqual((20, 4), (up.Value.x, up.Value.y),
                "and `<` looks for the other kind");
        }

        [Test]
        public void ItPrefersTheNearerStaircase()
        {
            var zone = Floored();
            Stairs(zone, 60, 12, down: true);
            Stairs(zone, 16, 10, down: true);
            var you = Walker(zone, 10, 10);

            var t = StairTravel.FindTarget(zone, you, goingDown: true);
            Assert.AreEqual((16, 10), (t.Value.x, t.Value.y),
                "the one you can reach soonest");
        }

        [Test]
        public void NoStairs_NoTravel()
        {
            // Counter-check: the old behaviour must survive — a chunk
            // with no way down still falls through to the world map /
            // the honest refusal.
            var zone = Floored();
            var you = Walker(zone, 10, 10);
            Assert.IsFalse(StairTravel.FindTarget(zone, you, goingDown: true).HasValue);
        }

        [Test]
        public void UnreachableStairs_AreNotATarget()
        {
            // Walled off entirely: travelling is impossible, so the
            // command must fall through rather than walk into a wall
            // forever.
            var zone = Floored();
            // Full column, edge to edge: a wall that stops at y=1
            // leaves the unfloored border row open and the path simply
            // walks around it (caught by this test failing RED).
            for (int y = 0; y < Zone.Height; y++)
            {
                var w = new Entity { ID = $"w{y}", BlueprintName = "Wall" };
                w.Tags["Solid"] = "";
                w.AddPart(new RenderPart());
                w.AddPart(new PhysicsPart { Solid = true });
                zone.AddEntity(w, 40, y);
            }
            Stairs(zone, 60, 12, down: true);
            var you = Walker(zone, 10, 10);

            Assert.IsFalse(StairTravel.FindTarget(zone, you, goingDown: true).HasValue,
                "no path, no travel");
        }

        // ════════════════════════════════════════════════════════════
        //   Walking, and stopping
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ThePathLeadsAllTheWayThere()
        {
            var zone = Floored();
            Stairs(zone, 20, 10, down: true);
            var you = Walker(zone, 10, 10);

            var steps = StairTravel.PathTo(zone, you, 20, 10);
            Assert.IsNotNull(steps);
            Assert.Greater(steps.Count, 0);

            int x = 10, y = 10;
            foreach (var (dx, dy) in steps) { x += dx; y += dy; }
            Assert.AreEqual((20, 10), (x, y), "the steps arrive");
        }

        [Test]
        public void SomethingHostileStopsYou()
        {
            // The rule that keeps this a convenience and not a way to
            // die: travel halts the moment a hostile is in view.
            var zone = Floored();
            var you = Walker(zone, 10, 10);
            you.Tags["Faction"] = "Villagers";
            var beast = new Entity { ID = "beast", BlueprintName = "Snapjaw" };
            beast.Tags["Creature"] = "";
            beast.Tags["Faction"] = "Snapjaws";
            beast.Statistics["Hitpoints"] = new Stat
            { Owner = beast, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            beast.AddPart(new RenderPart { DisplayName = "snapjaw" });
            zone.AddEntity(beast, 14, 10);

            Assert.IsTrue(StairTravel.ShouldInterrupt(zone, you),
                "you do not stroll past a snapjaw");
        }

        [Test]
        public void AHostileAcrossTheZoneDoesNotStopYou()
        {
            // Counter-check on the NoticeRadius gate (cold-eye fix D):
            // the old rule vetoed travel whenever ANY hostile existed
            // anywhere in the zone, which made the feature unusable in
            // exactly the zones it exists for. A snapjaw 30 cells away
            // has not noticed you; you walk. A revert to the zone-wide
            // scan fails here.
            var zone = Floored();
            var you = Walker(zone, 10, 10);
            you.Tags["Faction"] = "Villagers";
            var beast = FarBeast(zone, 45, 10);

            Assert.IsFalse(StairTravel.ShouldInterrupt(zone, you),
                "a hostile beyond notice range is scenery, not an ambush");
        }

        [Test]
        public void TheNoticeBoundaryIsChebyshev()
        {
            // Boundary pair: distance exactly NoticeRadius interrupts;
            // one cell further does not. Chebyshev, matching how the
            // grid actually plays (diagonals cost one).
            var zone = Floored();
            var you = Walker(zone, 10, 10);
            you.Tags["Faction"] = "Villagers";

            var near = FarBeast(zone, 10 + StairTravel.NoticeRadius, 10);
            Assert.IsTrue(StairTravel.ShouldInterrupt(zone, you),
                "at the boundary you are noticed");

            zone.RemoveEntity(near);
            var far = FarBeast(zone, 10 + StairTravel.NoticeRadius + 1, 10);
            Assert.IsFalse(StairTravel.ShouldInterrupt(zone, you),
                "one past the boundary you are not");
        }

        private static Entity FarBeast(Zone zone, int x, int y)
        {
            var beast = new Entity { ID = "beast@" + x + "," + y, BlueprintName = "Snapjaw" };
            beast.Tags["Creature"] = "";
            beast.Tags["Faction"] = "Snapjaws";
            beast.Statistics["Hitpoints"] = new Stat
            { Owner = beast, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            beast.AddPart(new RenderPart { DisplayName = "snapjaw" });
            zone.AddEntity(beast, x, y);
            return beast;
        }

        [Test]
        public void AnEmptyRoomDoesNotStopYou()
        {
            // Counter-check: the interrupt must not fire on nothing, or
            // the feature never walks a single step.
            var zone = Floored();
            var you = Walker(zone, 10, 10);
            you.Tags["Faction"] = "Villagers";
            Assert.IsFalse(StairTravel.ShouldInterrupt(zone, you));
        }

        [Test]
        public void AFriendlyNeighbourDoesNotStopYou()
        {
            var zone = Floored();
            var you = Walker(zone, 10, 10);
            you.Tags["Faction"] = "Villagers";
            var friend = new Entity { ID = "friend", BlueprintName = "Villager" };
            friend.Tags["Creature"] = "";
            friend.Tags["Faction"] = "Villagers";
            friend.Statistics["Hitpoints"] = new Stat
            { Owner = friend, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            friend.AddPart(new RenderPart { DisplayName = "villager" });
            zone.AddEntity(friend, 13, 10);

            Assert.IsFalse(StairTravel.ShouldInterrupt(zone, you),
                "a neighbour is not an ambush");
        }

        // ════════════════════════════════════════════════════════════
        //   W5.7 — who actually stops you
        // ════════════════════════════════════════════════════════════

        [Test]
        public void AFleeingFrogIsNotAnAmbush()
        {
            // Close-out hypothesis H2 — GinFrog is faction Beasts
            // (faction-hostile to the player) but Passive: it never
            // initiates (BrainPart.cs:47). Before the passive rule, one
            // frog three cells away vetoed stair travel across the
            // whole sima it lives in.
            var zone = Floored();
            var you = Walker(zone, 10, 10);
            you.Tags["Faction"] = "Villagers";
            var frog = FarBeast(zone, 13, 10);
            frog.AddPart(new BrainPart { Passive = true });

            Assert.IsFalse(StairTravel.ShouldInterrupt(zone, you),
                "a thing that only flees is scenery, not company");
        }

        [Test]
        public void APassiveCreatureYouAreFightingStillStopsYou()
        {
            // Counter-check: passivity ends where the fight begins —
            // a passive creature DEFENDING itself against you is a
            // fight like any other.
            var zone = Floored();
            var you = Walker(zone, 10, 10);
            you.Tags["Faction"] = "Villagers";
            var frog = FarBeast(zone, 13, 10);
            var brain = new BrainPart { Passive = true };
            brain.PersonalEnemies.Add(you);
            frog.AddPart(brain);

            Assert.IsTrue(StairTravel.ShouldInterrupt(zone, you),
                "you started this; you do not stroll away mid-fight");
        }

        [Test]
        public void AFarSightedHostileStopsYouAtItsOwnRange()
        {
            // Close-out 🔵 — SightRadius is blueprint-settable; the
            // gate is as wide as THIS hostile's eyes, not the default.
            var zone = Floored();
            var you = Walker(zone, 10, 10);
            you.Tags["Faction"] = "Villagers";
            var watcher = FarBeast(zone, 10 + StairTravel.NoticeRadius + 4, 10);
            watcher.AddPart(new BrainPart { SightRadius = StairTravel.NoticeRadius + 4 });

            Assert.IsTrue(StairTravel.ShouldInterrupt(zone, you),
                "it can see you, so it counts — whatever the default radius says");
        }

        [Test]
        public void TheWalkGoesAroundSomethingParkedOnThePath()
        {
            // Close-out hypothesis H11 — W5.5's encased elders are
            // solid and never move. Planning ignores creatures (they
            // usually move), so the plan can go THROUGH one; the
            // creature-respecting overload must find the way around,
            // because the walker re-plans with it when a step bounces.
            var zone = Floored();
            var you = Walker(zone, 10, 10);
            var elder = new Entity { ID = "elder", BlueprintName = "EncasedElder" };
            elder.Tags["Creature"] = "";
            elder.AddPart(new PhysicsPart { Solid = true });
            elder.AddPart(new RenderPart { DisplayName = "encased elder" });
            elder.Statistics["Hitpoints"] = new Stat
            { Owner = elder, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            zone.AddEntity(elder, 15, 10);

            var detour = StairTravel.PathTo(zone, you, 20, 10, ignoreCreatures: false);
            Assert.IsNotNull(detour, "there is floor all around; the way exists");
            int x = 10, y = 10;
            bool through = false;
            foreach (var (dx, dy) in detour)
            { x += dx; y += dy; if (x == 15 && y == 10) through = true; }
            Assert.IsFalse(through, "the detour respects the elder");
            Assert.AreEqual((20, 10), (x, y), "and still arrives");
        }

    }
}
