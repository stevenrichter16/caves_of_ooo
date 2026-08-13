using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Adversarial sweep for object destruction and the status matrix
    /// (<c>ADVERSARIAL_TESTING.md</c>; <c>CLAUDE.md</c> §Adversarial test
    /// sweep).
    ///
    /// <para>Surfaces probed, from the bug-class taxonomy:
    /// <b>state atomicity</b> (a container's contents must not be half
    /// spilled), <b>save/load reach</b> (a half-broken wall must stay half
    /// broken), <b>anti-exploit gates</b> (Indestructible must not be
    /// reachable around), <b>cross-actor flows</b> (who struck whom),
    /// <b>boundary inputs</b> (zero/negative/enormous damage),
    /// <b>duplicate operations</b> (destroying the same thing twice), and
    /// <b>iterator-vs-mutation</b> (spilling while a listener also
    /// mutates).</para>
    ///
    /// <para>Not probed, and deliberately so: there is no parser here, and
    /// no RNG-gated behaviour beyond the damage roll, which
    /// <c>DestructionSystemTests</c> pins with one-sided dice.</para>
    /// </summary>
    public class DestructionAdversarialTests
    {
        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
            DestructionSystem.EntityFactoryRef = null;
        }

        private static Entity Breakable(string id = "barrel", int hp = 10, int hardness = 0)
        {
            var e = new Entity { ID = id, BlueprintName = "HaulBarrel" };
            e.AddPart(new PhysicsPart { Solid = true, Takeable = false });
            e.AddPart(new DestructiblePart { HP = hp, MaxHP = hp, Hardness = hardness });
            return e;
        }

        private static Entity Item(string id)
        {
            var e = new Entity { ID = id, BlueprintName = "Bone" };
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            return e;
        }

        // ════════════════════════════════════════════════════════
        // Save/load reach
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_AHalfBrokenWallStaysHalfBrokenAcrossASave()
        {
            // This is an RPG — the player saves and reloads constantly. If
            // HP does not round-trip, every wall silently heals to full on
            // load and chipping through one becomes impossible in practice
            // while looking fine in every unit test.
            var wall = Breakable("wall", hp: 40, hardness: 3);
            wall.GetPart<DestructiblePart>().WreckageBlueprint = "Rubble";
            DestructionSystem.Damage(wall, 20, null, new Zone("Z"));
            Assert.AreEqual(23, wall.GetPart<DestructiblePart>().HP, "precondition: damaged");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(wall);
            var part = loaded.GetPart<DestructiblePart>();

            Assert.IsNotNull(part, "the Part itself must survive");
            Assert.AreEqual(23, part.HP, "structural HP");
            Assert.AreEqual(40, part.MaxHP, "max HP");
            Assert.AreEqual(3, part.Hardness, "hardness");
            Assert.AreEqual("Rubble", part.WreckageBlueprint, "wreckage");
        }

        [Test]
        public void Adversarial_IndestructibleSurvivesASave()
        {
            // The flag is the softlock guard. If it round-tripped as false,
            // a reloaded save would let the player destroy a staircase —
            // and the failure would only show up in someone's playthrough.
            var stairs = Breakable("stairs");
            stairs.GetPart<DestructiblePart>().Indestructible = true;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(stairs);

            Assert.IsTrue(loaded.GetPart<DestructiblePart>().Indestructible);
            Assert.IsFalse(DestructionSystem.IsBreakable(loaded));
        }

        // ════════════════════════════════════════════════════════
        // State atomicity
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_AVetoedDestroyDoesNotSpillTheContents()
        {
            // The ordering trap: if the spill ran before the veto check, a
            // refused destroy would still have emptied the chest — the
            // object survives and its contents are on the floor next to it.
            var chest = Breakable("chest", hp: 1);
            var container = new ContainerPart();
            container.Contents.Add(Item("sword"));
            chest.AddPart(container);
            chest.AddPart(new RefusePart());

            var zone = new Zone("Z");
            zone.AddEntity(chest, 4, 4);

            Assert.AreEqual(DestroyVerdict.Vetoed, DestructionSystem.Damage(chest, 99, null, zone));
            Assert.AreEqual(1, container.Contents.Count, "contents stay inside a vetoed chest");
            Assert.AreEqual((4, 4), zone.GetEntityPosition(chest));
        }

        [Test]
        public void Adversarial_SpilledContentsAreNoLongerFlaggedAsCarried()
        {
            // PhysicsPart.InInventory is the back-reference the inventory
            // and pickup code trusts. An item on the floor still claiming to
            // be inside a destroyed chest is a duplication bug waiting to
            // happen — pick it up and two systems both think they own it.
            var chest = Breakable("chest", hp: 1);
            var container = new ContainerPart();
            var sword = Item("sword");
            sword.GetPart<PhysicsPart>().InInventory = chest;
            container.Contents.Add(sword);
            chest.AddPart(container);

            var zone = new Zone("Z");
            zone.AddEntity(chest, 4, 4);
            DestructionSystem.Damage(chest, 1, null, zone);

            Assert.IsNull(sword.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual((4, 4), zone.GetEntityPosition(sword));
        }

        [Test]
        public void Adversarial_ContentsWithANullEntryDoNotStrandTheRest()
        {
            // Iterator-vs-mutation: the spill loop walks backwards while
            // removing. A null hole in the middle must not abort the loop
            // or skip a neighbour.
            var chest = Breakable("chest", hp: 1);
            var container = new ContainerPart();
            container.Contents.Add(Item("a"));
            container.Contents.Add(null);
            container.Contents.Add(Item("b"));
            chest.AddPart(container);

            var zone = new Zone("Z");
            zone.AddEntity(chest, 4, 4);

            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(chest, 1, null, zone));
            Assert.AreEqual(1, container.Contents.Count, "only the null hole is left behind");
            Assert.IsNull(container.Contents[0]);
        }

        // ════════════════════════════════════════════════════════
        // Duplicate operations
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_DestroyingTheSameThingTwiceIsNotDoubleSpill()
        {
            // A listener on Destroyed could re-enter, and two damage sources
            // in one turn could both take it below zero. Neither may spill
            // the contents twice — that is item duplication.
            var chest = Breakable("chest", hp: 1);
            var container = new ContainerPart();
            container.Contents.Add(Item("sword"));
            chest.AddPart(container);

            var zone = new Zone("Z");
            zone.AddEntity(chest, 4, 4);

            DestructionSystem.Destroy(chest, null, zone, "first");
            DestructionSystem.Destroy(chest, null, zone, "second");

            var atCell = new List<Entity>(zone.GetCell(4, 4).Objects);
            Assert.AreEqual(1, atCell.FindAll(e => e != null && e.ID == "sword").Count,
                "the sword must exist exactly once");
        }

        [Test]
        public void Adversarial_DestroyingTwiceDoesNotLeaveTwoPilesOfRubble()
        {
            // Angle-B finding: Qud's Destroy() opens with an IsInGraveyard()
            // check (XRL.World/GameObject.cs:3306-3311) and CoO had no
            // equivalent. Without it a second destroy re-fires the event and
            // spawns the wreckage AGAIN — one wall, two piles of rubble.
            var wall = Breakable("wall", hp: 1);
            wall.GetPart<DestructiblePart>().WreckageBlueprint = "Rubble";
            var witness = new CountDestroyedPart();
            wall.AddPart(witness);

            var zone = new Zone("Z");
            zone.AddEntity(wall, 4, 4);

            DestructionSystem.Destroy(wall, null, zone, "first");
            DestructionSystem.Destroy(wall, null, zone, "second");

            Assert.AreEqual(1, witness.Count, "Destroyed must fire exactly once");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "ObjectDestroyed", Limit = 10 }).Records.Count,
                "and be recorded exactly once");
        }

        [Test]
        public void Adversarial_AListenerThatReEntersOnDestroyedDoesNotRecurse()
        {
            // The guard is set BEFORE the notification fires, specifically so
            // that a listener calling Destroy again from inside the handler
            // finds it already set instead of recursing forever.
            var barrel = Breakable(hp: 1);
            var zone = new Zone("Z");
            zone.AddEntity(barrel, 4, 4);
            barrel.AddPart(new ReentrantDestroyPart { Zone = zone });

            Assert.AreEqual(DestroyVerdict.Destroyed,
                DestructionSystem.Destroy(barrel, null, zone, "first"));
        }

        [Test]
        public void Adversarial_DamagingAnAlreadyDestroyedThingDoesNotResurrectIt()
        {
            var barrel = Breakable(hp: 1);
            var zone = new Zone("Z");
            zone.AddEntity(barrel, 4, 4);

            DestructionSystem.Damage(barrel, 1, null, zone);
            Assert.Less(zone.GetEntityPosition(barrel).x, 0, "precondition: gone");

            // Second hit on the corpse-of-an-object. Must not put it back in
            // the zone via any code path.
            DestructionSystem.Damage(barrel, 1, null, zone);
            Assert.Less(zone.GetEntityPosition(barrel).x, 0, "still gone");
        }

        // ════════════════════════════════════════════════════════
        // Anti-exploit
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_IndestructibleIsNotReachableAroundByAnyEntryPoint()
        {
            // Every public way in must respect it, or the softlock guard is
            // only as strong as its least-guarded caller.
            var stairs = Breakable("stairs", hp: 5);
            stairs.GetPart<DestructiblePart>().Indestructible = true;
            var zone = new Zone("Z");
            zone.AddEntity(stairs, 4, 4);

            var fire = new Damage(999);
            fire.AddAttribute("Fire");

            Assert.IsFalse(DestructionSystem.IsBreakable(stairs), "IsBreakable");
            Assert.AreEqual(DestroyVerdict.Indestructible,
                DestructionSystem.Damage(stairs, 999, null, zone), "Damage");
            Assert.AreEqual(DestroyVerdict.Indestructible,
                DestructionSystem.Destroy(stairs, null, zone, "x"), "Destroy");
            Assert.AreEqual(0,
                DestructionSystem.RouteDamage(stairs, fire, null, zone), "RouteDamage");
            Assert.AreEqual(5, stairs.GetPart<DestructiblePart>().HP, "untouched");
            Assert.AreEqual((4, 4), zone.GetEntityPosition(stairs));
        }

        [Test]
        public void Adversarial_HardnessCannotBeOutrunByRepeatedTinyHits()
        {
            // The clamp guarantees every hit does at least 1, which is what
            // stops hardness making a thing unbreakable by arithmetic. The
            // counter-property matters too: 1-per-hit must not become
            // hardness-worth-per-hit through some sign error.
            var stone = Breakable("stone", hp: 10, hardness: 100);
            var zone = new Zone("Z");
            zone.AddEntity(stone, 4, 4);

            for (int i = 0; i < 9; i++)
                DestructionSystem.Damage(stone, 1, null, zone);

            Assert.AreEqual(1, stone.GetPart<DestructiblePart>().HP,
                "nine minimum hits off ten HP");
            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(stone, 1, null, zone));
        }

        // ════════════════════════════════════════════════════════
        // Boundary inputs
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ZeroAndNegativeDamageStillCountAsAHit()
        {
            // Hardness clamping means "at least 1" — a 0 or negative roll
            // arriving from a resistance calculation must land on the same
            // side of that clamp, not heal the target.
            var barrel = Breakable(hp: 10);
            var zone = new Zone("Z");
            zone.AddEntity(barrel, 4, 4);

            DestructionSystem.Damage(barrel, 0, null, zone);
            Assert.AreEqual(9, barrel.GetPart<DestructiblePart>().HP);

            DestructionSystem.Damage(barrel, -50, null, zone);
            Assert.AreEqual(8, barrel.GetPart<DestructiblePart>().HP, "a negative roll never heals");
        }

        [Test]
        public void Adversarial_EnormousDamageDoesNotOverflowIntoSurvival()
        {
            var barrel = Breakable(hp: 10);
            var zone = new Zone("Z");
            zone.AddEntity(barrel, 4, 4);

            Assert.AreEqual(DestroyVerdict.Destroyed,
                DestructionSystem.Damage(barrel, int.MaxValue, null, zone));
        }

        [Test]
        public void Adversarial_AZeroHpThingIsDestroyedByTheNextHitNotIgnored()
        {
            // Boundary: a thing authored at HP 0, or left at 0 by some other
            // system, must not sit in the world indefinitely.
            var husk = Breakable(hp: 0);
            var zone = new Zone("Z");
            zone.AddEntity(husk, 4, 4);

            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(husk, 1, null, zone));
        }

        // ════════════════════════════════════════════════════════
        // Wreckage
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_WreckageIsNotSpawnedForAnUnplacedObject()
        {
            // Position is read before removal and is (-1,-1) for something
            // never added to a zone. Spawning rubble at (-1,-1) would either
            // throw or quietly corrupt the cell grid.
            var wall = Breakable("wall", hp: 1);
            wall.GetPart<DestructiblePart>().WreckageBlueprint = "Rubble";

            Assert.AreEqual(DestroyVerdict.Destroyed,
                DestructionSystem.Damage(wall, 1, null, new Zone("Z")));
        }

        // ════════════════════════════════════════════════════════
        // Cross-actor
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TheDiagRecordNamesWhoStruckWhat()
        {
            // "Who broke my chest?" is a real question in a game with NPCs
            // and area spells. The record has to carry the actor, not just
            // the fact that something happened.
            var chest = Breakable("chest", hp: 20);
            var npc = new Entity { ID = "thug", BlueprintName = "Bandit" };
            npc.Tags["Creature"] = "";
            var zone = new Zone("Z");
            zone.AddEntity(chest, 4, 4);

            DestructionSystem.Damage(chest, 5, npc, zone);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "damage", Kind = "ObjectDamaged", Limit = 5 }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("thug", records[0].ActorId);
            Assert.AreEqual("chest", records[0].TargetId);
        }

        // ════════════════════════════════════════════════════════
        // The status matrix under pressure
        // ════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_AnEffectRefusedByMaterialIsNotAppliedByStacking()
        {
            // Stacking is checked INSIDE StatusEffectsPart, after
            // CanBeAppliedTo. The matrix sits outside it, so a second
            // application must be refused just as flatly as the first —
            // otherwise "apply twice" would be a way around the gate.
            var wall = new Entity { ID = "wall", BlueprintName = "StoneWall" };
            wall.AddPart(new PhysicsPart { Solid = true });
            wall.AddPart(new MaterialPart { MaterialTagsRaw = "Stone,Mineral" });

            Assert.IsFalse(ObjectStatusMatrix.TryApply(new BurningEffect(), wall, null, null));
            Assert.IsFalse(ObjectStatusMatrix.TryApply(new BurningEffect(), wall, null, null));
            Assert.IsNull(wall.GetPart<StatusEffectsPart>()?.GetEffect<BurningEffect>());
        }

        [Test]
        public void Adversarial_MaterialTagMatchingIsExactNotSubstring()
        {
            // "Metalwork" must not read as "Metal" and make a workbench
            // conduct lightning. HashSet.Contains is exact, and this pins it.
            var bench = new Entity { ID = "bench", BlueprintName = "Workbench" };
            bench.AddPart(new PhysicsPart());
            bench.AddPart(new MaterialPart { MaterialTagsRaw = "Metalwork,Organics" });

            Assert.AreEqual(ObjectStatusVerdict.WrongMaterial,
                ObjectStatusMatrix.Evaluate(new ElectrifiedEffect(), bench), "Metalwork != Metal");
            Assert.AreEqual(ObjectStatusVerdict.WrongMaterial,
                ObjectStatusMatrix.Evaluate(new BurningEffect(), bench), "Organics != Organic");
        }

        [Test]
        public void Adversarial_AnObjectThatIsAlsoTaggedCreatureUsesTheCreaturePath()
        {
            // The mimic case, from the other side: something carrying BOTH
            // a DestructiblePart and the Creature tag must not be routable
            // through the object path by any entry point, or killing it
            // would skip its XP, loot and corpse.
            var mimic = Breakable("mimic", hp: 5);
            mimic.Tags["Creature"] = "";
            var zone = new Zone("Z");
            zone.AddEntity(mimic, 4, 4);

            Assert.IsFalse(DestructionSystem.IsBreakable(mimic));
            Assert.AreEqual(DestroyVerdict.Living, DestructionSystem.Damage(mimic, 99, null, zone));
            Assert.AreEqual(DestroyVerdict.Living, DestructionSystem.Destroy(mimic, null, zone, "x"));
            Assert.AreEqual(ObjectStatusVerdict.Applies,
                ObjectStatusMatrix.Evaluate(new BleedingEffect(), mimic),
                "and it can still bleed, because it is alive");
            Assert.AreEqual((4, 4), zone.GetEntityPosition(mimic));
        }

        [Test]
        public void Adversarial_TheBreakRowDisappearsWhenTheFlagIsFlippedAtRuntime()
        {
            // Indestructible is a field, not a construction-time decision —
            // a quest could set it mid-game. The row must follow, or the
            // player is offered an action that silently refuses.
            var door = Breakable("door");
            var part = door.GetPart<DestructiblePart>();

            Assert.IsTrue(HasBreakRow(door), "breakable to begin with");
            part.Indestructible = true;
            Assert.IsFalse(HasBreakRow(door), "and not once sealed");
            part.Indestructible = false;
            Assert.IsTrue(HasBreakRow(door), "and back again");
        }

        private static bool HasBreakRow(Entity target)
        {
            var list = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", (object)list);
            target.FireEventAndRelease(e);
            return list.Actions.Exists(a => a.Command == DestructiblePart.BreakCommand);
        }

        /// <summary>Refuses every destruction, to exercise the veto.</summary>
        private sealed class RefusePart : Part
        {
            public override string Name => "Refuse";
            public override bool HandleEvent(GameEvent e) => e.ID != "BeforeDestroy";
        }

        /// <summary>Counts how many times the Destroyed notification fired.</summary>
        private sealed class CountDestroyedPart : Part
        {
            public override string Name => "CountDestroyed";
            public int Count;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Destroyed") Count++;
                return true;
            }
        }

        /// <summary>Destroys its owner again from inside the notification.</summary>
        private sealed class ReentrantDestroyPart : Part
        {
            public override string Name => "ReentrantDestroy";
            public Zone Zone;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Destroyed")
                    DestructionSystem.Destroy(ParentEntity, null, Zone, "reentrant");
                return true;
            }
        }
    }
}
