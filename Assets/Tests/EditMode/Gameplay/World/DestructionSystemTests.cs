using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Breaking things that are not alive
    /// (<c>Docs/OBJECT-INTERACTION-PLAN.md</c> §4).
    ///
    /// <para>The load-bearing property this file exists to protect is that
    /// object destruction is a <b>separate path</b> from creature death.
    /// <c>CombatSystem.HandleDeath</c> awards kill XP, drops equipment off
    /// body parts, rolls a death-loot table, emits a blood splatter, fires
    /// <c>Died</c> (which spawns a corpse) and broadcasts to nearby NPCs.
    /// A smashed barrel must touch none of it.</para>
    ///
    /// <para>The second property is that destructibility is <b>opt-in</b>,
    /// per Qud's <c>Breakable</c> gate. Everything without a
    /// <c>DestructiblePart</c> is immune, so a staircase is safe by
    /// omission rather than by anyone remembering to protect it.</para>
    /// </summary>
    public class DestructionSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
        }

        private static Entity Breakable(string id = "barrel", int hp = 10, int hardness = 0)
        {
            var e = new Entity { ID = id, BlueprintName = "HaulBarrel" };
            e.AddPart(new PhysicsPart { Solid = true, Takeable = false });
            e.AddPart(new DestructiblePart { HP = hp, MaxHP = hp, Hardness = hardness });
            return e;
        }

        private static Entity Creature(string id = "npc")
        {
            var e = new Entity { ID = id, BlueprintName = "Villager" };
            e.Tags["Creature"] = "";
            e.AddPart(new PhysicsPart { Solid = true });
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            return e;
        }

        private static Entity LooseItem(string id)
        {
            var e = new Entity { ID = id, BlueprintName = "Bone" };
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            return e;
        }

        private static IReadOnlyList<Diag.Entry> Records(string kind)
            => DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = kind, Limit = 50 }).Records;

        // ════════════════════════════════════════════════════════
        // Opt-in: almost nothing is breakable
        // ════════════════════════════════════════════════════════

        [Test]
        public void AnObjectWithoutTheePart_IsNotBreakable()
        {
            var wall = new Entity { ID = "wall", BlueprintName = "Wall" };
            wall.AddPart(new PhysicsPart { Solid = true });

            Assert.IsFalse(DestructionSystem.IsBreakable(wall));
            Assert.AreEqual(DestroyVerdict.NotBreakable,
                DestructionSystem.Damage(wall, 999, null, new Zone("Z")));
        }

        [Test]
        public void AnObjectWithThePart_IsBreakable()
        {
            // Counter-check to the above — "immune by default" must not
            // mean "immune always", or the feature does not exist.
            Assert.IsTrue(DestructionSystem.IsBreakable(Breakable()));
        }

        [Test]
        public void AnIndestructibleObject_SurvivesAnythingAndSaysSo()
        {
            // The staircase case. Belt-and-braces on top of the opt-in
            // default, for things that need the Part but must never break.
            var stairs = Breakable("stairs");
            stairs.GetPart<DestructiblePart>().Indestructible = true;
            var zone = new Zone("Z");
            zone.AddEntity(stairs, 5, 5);

            Assert.AreEqual(DestroyVerdict.Indestructible,
                DestructionSystem.Damage(stairs, 9999, null, zone));
            Assert.AreEqual(DestroyVerdict.Indestructible,
                DestructionSystem.Destroy(stairs, null, zone, "test"),
                "the outright-destroy primitive must respect it too, not just the HP path");
            Assert.AreEqual((5, 5), zone.GetEntityPosition(stairs), "still there");
            Assert.IsFalse(DestructionSystem.IsBreakable(stairs));
        }

        [Test]
        public void ALivingThingIsNeverDestroyedThisWay()
        {
            // The whole point of the separate path: creatures go through
            // CombatSystem, which handles corpses, XP and faction fallout.
            var npc = Creature();
            npc.AddPart(new DestructiblePart { HP = 1, MaxHP = 1 });
            var zone = new Zone("Z");
            zone.AddEntity(npc, 5, 5);

            Assert.AreEqual(DestroyVerdict.Living, DestructionSystem.Damage(npc, 999, null, zone));
            Assert.AreEqual(DestroyVerdict.Living, DestructionSystem.Destroy(npc, null, zone, "test"));
            Assert.AreEqual((5, 5), zone.GetEntityPosition(npc));
        }

        // ════════════════════════════════════════════════════════
        // The HP pool
        // ════════════════════════════════════════════════════════

        [Test]
        public void DamageShortOfLethal_LeavesItStanding()
        {
            var barrel = Breakable(hp: 10);
            var zone = new Zone("Z");
            zone.AddEntity(barrel, 5, 5);

            Assert.AreEqual(DestroyVerdict.Damaged, DestructionSystem.Damage(barrel, 4, null, zone));
            Assert.AreEqual(6, barrel.GetPart<DestructiblePart>().HP);
            Assert.AreEqual((5, 5), zone.GetEntityPosition(barrel));
        }

        [Test]
        public void ReachingZero_DestroysAndRemovesIt()
        {
            var barrel = Breakable(hp: 10);
            var zone = new Zone("Z");
            zone.AddEntity(barrel, 5, 5);

            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(barrel, 10, null, zone));
            Assert.Less(zone.GetEntityPosition(barrel).x, 0, "removed from the zone");
        }

        [Test]
        public void HardnessBlunts_ButNeverFullyAbsorbs()
        {
            // A stone wall shrugs off a fist — but if hardness could reduce
            // a hit to zero the object would be unbreakable by arithmetic
            // rather than by design, which is what Indestructible is for.
            var stone = Breakable(hp: 100, hardness: 5);
            var zone = new Zone("Z");
            zone.AddEntity(stone, 5, 5);

            DestructionSystem.Damage(stone, 8, null, zone);
            Assert.AreEqual(97, stone.GetPart<DestructiblePart>().HP, "8 - 5 hardness = 3");

            DestructionSystem.Damage(stone, 1, null, zone);
            Assert.AreEqual(96, stone.GetPart<DestructiblePart>().HP,
                "a hit under the hardness still does 1, never 0");
        }

        // ════════════════════════════════════════════════════════
        // Consequences
        // ════════════════════════════════════════════════════════

        [Test]
        public void BreakingAContainer_SpillsItsContentsWhereItStood()
        {
            var chest = Breakable("chest", hp: 5);
            var container = new ContainerPart();
            var sword = LooseItem("sword");
            var coin = LooseItem("coin");
            container.Contents.Add(sword);
            container.Contents.Add(coin);
            chest.AddPart(container);

            var zone = new Zone("Z");
            zone.AddEntity(chest, 7, 3);

            DestructionSystem.Damage(chest, 5, null, zone);

            Assert.AreEqual((7, 3), zone.GetEntityPosition(sword), "contents land where it stood");
            Assert.AreEqual((7, 3), zone.GetEntityPosition(coin));
            Assert.IsEmpty(container.Contents, "and are no longer inside it");
        }

        [Test]
        public void BreakingAnEmptyContainer_SpillsNothingAndDoesNotCrash()
        {
            // Counter-check on the spill loop's reverse iteration and its
            // empty case.
            var chest = Breakable("chest", hp: 5);
            chest.AddPart(new ContainerPart());
            var zone = new Zone("Z");
            zone.AddEntity(chest, 7, 3);

            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(chest, 5, null, zone));
        }

        [Test]
        public void ABreakableThingLeavesWreckageWhenItSaysTo()
        {
            // A broken wall should read as broken, not as pristine floor.
            // Without an injected factory this degrades to "simply gone"
            // rather than throwing — the CorpsePart.Factory convention.
            var wall = Breakable("wall", hp: 5);
            wall.GetPart<DestructiblePart>().WreckageBlueprint = "Rubble";
            var zone = new Zone("Z");
            zone.AddEntity(wall, 5, 5);

            DestructionSystem.EntityFactoryRef = null;
            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(wall, 5, null, zone),
                "a missing factory must not break destruction");
        }

        // ════════════════════════════════════════════════════════
        // The event contract (Qud parity)
        // ════════════════════════════════════════════════════════

        [Test]
        public void BeforeDestroy_CanVetoIt()
        {
            var vault = Breakable("vault", hp: 1);
            vault.AddPart(new VetoDestructionPart());
            var zone = new Zone("Z");
            zone.AddEntity(vault, 5, 5);

            Assert.AreEqual(DestroyVerdict.Vetoed, DestructionSystem.Damage(vault, 99, null, zone));
            Assert.AreEqual((5, 5), zone.GetEntityPosition(vault), "a vetoed destroy leaves it standing");
        }

        [Test]
        public void DestroyedFiresOnTheWayOut()
        {
            var barrel = Breakable(hp: 1);
            var witness = new WitnessDestroyedPart();
            barrel.AddPart(witness);
            var zone = new Zone("Z");
            zone.AddEntity(barrel, 5, 5);

            DestructionSystem.Damage(barrel, 1, null, zone);
            Assert.IsTrue(witness.Saw, "the Destroyed notification must fire");
        }

        // ════════════════════════════════════════════════════════
        // Observability
        // ════════════════════════════════════════════════════════

        [Test]
        public void DamageAndDestructionAreBothQueryable()
        {
            var barrel = Breakable(hp: 10);
            var zone = new Zone("Z");
            zone.AddEntity(barrel, 5, 5);

            DestructionSystem.Damage(barrel, 3, null, zone);
            Assert.AreEqual(1, Records("ObjectDamaged").Count);
            Assert.AreEqual(0, Records("ObjectDestroyed").Count,
                "a survivable hit is not a destruction");

            DestructionSystem.Damage(barrel, 99, null, zone);
            Assert.AreEqual(1, Records("ObjectDestroyed").Count);
        }

        [Test]
        public void ARefusalIsRecordedWithItsReason()
        {
            var stairs = Breakable("stairs");
            stairs.GetPart<DestructiblePart>().Indestructible = true;
            DestructionSystem.Damage(stairs, 99, null, new Zone("Z"));

            var refusals = Records("ObjectRefused");
            Assert.AreEqual(1, refusals.Count);
            StringAssert.Contains("indestructible", refusals[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════
        // Reaching it from the interact menu
        // ════════════════════════════════════════════════════════

        private static InventoryActionList ActionsFor(Entity target)
        {
            var list = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", (object)list);
            target.FireEventAndRelease(e);
            return list;
        }

        [Test]
        public void ABreakableThing_OffersABreakRow()
        {
            // Bumping only reaches things that block you. A barrel you can
            // walk around needs the interact key, or "attack anything" is
            // only true of obstacles.
            var actions = ActionsFor(Breakable());
            Assert.IsTrue(actions.Actions.Exists(
                a => a.Command == DestructiblePart.BreakCommand), "expected a Break row");
        }

        [Test]
        public void AnIndestructibleThing_OffersNoBreakRow()
        {
            // Counter-check: the row must not simply always be there. A row
            // that appears and then refuses is a worse experience than no
            // row at all.
            var stairs = Breakable("stairs");
            stairs.GetPart<DestructiblePart>().Indestructible = true;

            Assert.IsFalse(ActionsFor(stairs).Actions.Exists(
                a => a.Command == DestructiblePart.BreakCommand));
        }

        // ════════════════════════════════════════════════════════
        // How hard you hit a thing
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// A striker whose blow is fully deterministic: "6d1" always rolls 6,
        /// since a one-sided die can only come up 1. Lets these tests compare
        /// exact numbers instead of ranges.
        /// </summary>
        private static Entity Striker(int strength = 16, string weaponDamage = null)
        {
            var e = new Entity { ID = "striker", BlueprintName = "Player" };
            e.Statistics["Strength"] = new Stat
            { Owner = e, Name = "Strength", BaseValue = strength, Min = 1, Max = 40 };
            var inv = new InventoryPart();
            e.AddPart(inv);
            if (weaponDamage != null)
            {
                var axe = new Entity { ID = "axe", BlueprintName = "Axe" };
                axe.AddPart(new MeleeWeaponPart { BaseDamage = weaponDamage });
                inv.EquippedItems["Hand"] = axe;
            }
            return e;
        }

        [Test]
        public void AnUnarmedBlow_StillDoesSomething()
        {
            // Bare hands on a hedgerow must never come out as 0, or the
            // player would stand there swinging at a bush forever.
            var blow = DestructionSystem.ComputeStructuralBlow(
                Striker(strength: 4), new System.Random(1));
            Assert.GreaterOrEqual(blow, 1);
        }

        [Test]
        public void AWieldedWeapon_HitsHarderThanAFist()
        {
            // Counter-check to the above: if the weapon were being ignored,
            // both branches would return the same unarmed figure and this
            // would fail.
            var armed = DestructionSystem.ComputeStructuralBlow(
                Striker(weaponDamage: "6d1"), new System.Random(1));
            var barehanded = DestructionSystem.ComputeStructuralBlow(
                Striker(), new System.Random(1));

            Assert.AreEqual(6, armed, "6d1 is deterministic; Str 16 is a +0 modifier");
            Assert.Greater(armed, barehanded);
        }

        [Test]
        public void StrengthAddsToTheBlow()
        {
            // Same weapon, different arms. Without this the axe would swing
            // identically for a weakling and a giant.
            var strong = DestructionSystem.ComputeStructuralBlow(
                Striker(strength: 26, weaponDamage: "6d1"), new System.Random(1));
            Assert.AreEqual(11, strong, "6 damage + a +5 Strength modifier");
        }

        [Test]
        public void ANullAttacker_StillYieldsAUsableBlow()
        {
            // A falling rock, a scripted demolition — damage with no-one
            // behind it must not throw or come out as 0.
            Assert.GreaterOrEqual(
                DestructionSystem.ComputeStructuralBlow(null, new System.Random(1)), 1);
        }

        // ════════════════════════════════════════════════════════
        // Reach
        // ════════════════════════════════════════════════════════

        [Test]
        public void YouCanOnlyBreakWhatYouCanReach()
        {
            // The interact menu is reachable from look mode, whose cursor
            // can sit anywhere on the map. Without a reach check the player
            // could demolish a wall across the zone by pointing at it.
            var zone = new Zone("Z");
            var player = Creature("player");
            zone.AddEntity(player, 5, 5);

            var near = Breakable("near");
            zone.AddEntity(near, 6, 5);
            var diagonal = Breakable("diagonal");
            zone.AddEntity(diagonal, 4, 6);
            var far = Breakable("far");
            zone.AddEntity(far, 12, 5);

            Assert.IsTrue(DestructionSystem.IsWithinStrikeReach(player, near, zone), "adjacent");
            Assert.IsTrue(DestructionSystem.IsWithinStrikeReach(player, diagonal, zone), "diagonal counts");
            Assert.IsFalse(DestructionSystem.IsWithinStrikeReach(player, far, zone), "across the zone");
        }

        [Test]
        public void ReachIsFalseRatherThanThrowingOnMissingPlacement()
        {
            var zone = new Zone("Z");
            var player = Creature("player");
            zone.AddEntity(player, 5, 5);
            var orphan = Breakable("orphan"); // never placed

            Assert.IsFalse(DestructionSystem.IsWithinStrikeReach(player, orphan, zone));
            Assert.IsFalse(DestructionSystem.IsWithinStrikeReach(null, orphan, zone));
            Assert.IsFalse(DestructionSystem.IsWithinStrikeReach(player, null, zone));
            Assert.IsFalse(DestructionSystem.IsWithinStrikeReach(player, orphan, null));
        }

        // ════════════════════════════════════════════════════════
        // Narration
        // ════════════════════════════════════════════════════════

        [Test]
        public void OnlyTheePlayersOwnBlowIsNarrated()
        {
            // Damage() is also the per-turn path for a burning hedgerow.
            // "You strike the hedgerow." once a turn while it smoulders is
            // both untrue and noise — BurningEffect logs its own line.
            var zone = new Zone("Z");
            var barrel = Breakable(hp: 50);
            zone.AddEntity(barrel, 5, 5);

            var player = Creature("player");
            player.Tags["Player"] = "";
            DestructionSystem.Damage(barrel, 1, player, zone);
            Assert.IsTrue(MessageLog.GetRecent(5).Exists(m => m.Contains("You strike")),
                "a deliberate swing is narrated");

            MessageLog.Clear();
            DestructionSystem.Damage(barrel, 1, null, zone);
            Assert.IsFalse(MessageLog.GetRecent(5).Exists(m => m.Contains("You strike")),
                "a fire tick is not");
        }

        // ════════════════════════════════════════════════════════
        // Degenerate
        // ════════════════════════════════════════════════════════

        [Test]
        public void NullsAndMissingZonesAreSafe()
        {
            Assert.AreEqual(DestroyVerdict.NoTarget, DestructionSystem.Damage(null, 5, null, null));
            Assert.AreEqual(DestroyVerdict.NoTarget, DestructionSystem.Destroy(null, null, null, "x"));
            Assert.IsFalse(DestructionSystem.IsBreakable(null));

            // A breakable entity that was never placed in a zone.
            var orphan = Breakable(hp: 1);
            Assert.AreEqual(DestroyVerdict.Destroyed,
                DestructionSystem.Damage(orphan, 1, null, null));
        }

        /// <summary>Refuses every destruction, to exercise the veto.</summary>
        private sealed class VetoDestructionPart : Part
        {
            public override string Name => "VetoDestruction";
            public override bool HandleEvent(GameEvent e)
                => e.ID != "BeforeDestroy";
        }

        /// <summary>Records that the Destroyed notification arrived.</summary>
        private sealed class WitnessDestroyedPart : Part
        {
            public override string Name => "WitnessDestroyed";
            public bool Saw;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Destroyed") Saw = true;
                return true;
            }
        }
    }
}
