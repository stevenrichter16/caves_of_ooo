using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Elemental abilities finding the things they are aimed at.
    ///
    /// <para>Reported from play: fire bolt and ember spit did nothing to a
    /// tree standing directly in front of the player, nothing to a bush,
    /// and a shock spell "found no conductors" when fired into water.
    /// Three separate creature-only filters, none of them the
    /// <c>LineTargeting</c> path fixed earlier:</para>
    /// <list type="number">
    /// <item><c>SkillLine.Collect</c> — collected only creatures, AND
    /// checked <c>cell.IsSolid()</c> BEFORE looking at the cell's
    /// contents. A tree carries the Solid tag, so the walk broke out
    /// having never considered it: "line_blocked", tree untouched.</item>
    /// <item><c>FlamingHandsMutation</c> — <c>GetObjectsWithTag("Creature")</c>,
    /// so a tree got "blasts the empty space with flames!".</item>
    /// <item><c>Galvanism_Overload</c> — creature-only scan, so a brine
    /// pool was skipped entirely.</item>
    /// </list>
    /// </summary>
    public class ElementalTargetingTests
    {
        private static Entity Actor()
        {
            var e = new Entity { ID = "player", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            return e;
        }

        private static Entity Creature(string id)
        {
            var e = new Entity { ID = id, BlueprintName = "Snapjaw" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            return e;
        }

        /// <summary>A tree: solid, breakable, flammable.</summary>
        private static Entity Tree()
        {
            var e = new Entity { ID = "tree", BlueprintName = "Tree" };
            e.Tags["Solid"] = "";
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new DestructiblePart { HP = 30, MaxHP = 30 });
            e.AddPart(new MaterialPart { MaterialTagsRaw = "Organic,Plant,Wood" });
            return e;
        }

        /// <summary>A bush: breakable and flammable, but not solid.</summary>
        private static Entity Bush()
        {
            var e = new Entity { ID = "bush", BlueprintName = "Bush" };
            e.AddPart(new DestructiblePart { HP = 6, MaxHP = 6 });
            e.AddPart(new MaterialPart { MaterialTagsRaw = "Organic,Plant" });
            return e;
        }

        /// <summary>A pool: no Physics, no HP, but very conductive.</summary>
        private static Entity BrinePool()
        {
            var e = new Entity { ID = "brine", BlueprintName = "BrinePool" };
            e.AddPart(new MaterialPart { MaterialTagsRaw = "Liquid,Water,Conductor" });
            return e;
        }

        private static Entity Coin()
        {
            var e = new Entity { ID = "coin", BlueprintName = "GoldCoin" };
            e.AddPart(new PhysicsPart { Takeable = true });
            return e;
        }

        // ════════════════════════════════════════════════════════
        // What counts as a target
        // ════════════════════════════════════════════════════════

        [Test]
        public void ATreeABushAndAPoolAreAllValidElementalTargets()
        {
            var actor = Actor();
            Assert.IsTrue(AbilityTargeting.IsElementalTarget(Tree(), actor), "tree");
            Assert.IsTrue(AbilityTargeting.IsElementalTarget(Bush(), actor), "bush");
            Assert.IsTrue(AbilityTargeting.IsElementalTarget(BrinePool(), actor),
                "a pool has no HP and no Physics, but conducting is the point of it");
            Assert.IsTrue(AbilityTargeting.IsElementalTarget(Creature("s"), actor), "creature");
        }

        [Test]
        public void ThingsWithNoPhysicalPresenceAreNotTargets()
        {
            // Counter-check: if the rule admitted everything, the first
            // test would pass for the wrong reason and a fire bolt would
            // "hit" a dropped coin.
            var actor = Actor();
            Assert.IsFalse(AbilityTargeting.IsElementalTarget(Coin(), actor), "a coin");
            Assert.IsFalse(AbilityTargeting.IsElementalTarget(null, actor), "null");
            Assert.IsFalse(AbilityTargeting.IsElementalTarget(actor, actor), "the caster");
        }

        [Test]
        public void TheWeaponSkillRuleStaysCreatureOnly()
        {
            // Deliberate divergence: "backstab a wall" has no meaning to
            // define, so the melee skills keep the stricter rule. If this
            // ever starts returning true for a tree, someone has merged
            // the two rules by accident.
            var actor = Actor();
            Assert.IsFalse(AbilityTargeting.IsCreatureTarget(Tree(), actor));
            Assert.IsTrue(AbilityTargeting.IsCreatureTarget(Creature("s"), actor));
        }

        // ════════════════════════════════════════════════════════
        // The line walk — the reported bug
        // ════════════════════════════════════════════════════════

        [Test]
        public void ABoltFindsTheTreeStandingDirectlyInFrontOfYou()
        {
            // The exact reported symptom. Before the fix, cell.IsSolid()
            // was tested BEFORE the cell's contents, so the walk broke
            // out having never looked at the tree.
            var zone = new Zone("Z");
            var actor = Actor();
            var tree = Tree();
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(tree, 6, 5);

            var line = CavesOfOoo.Skills.SkillLine.Collect(zone, actor, 5, 5, 1, 0, 4, out bool blocked);

            Assert.AreEqual(1, line.Count, "the tree must be a target");
            Assert.AreSame(tree, line[0]);
            Assert.IsTrue(blocked, "and it still stops the line — you cannot shoot through a tree");
        }

        [Test]
        public void ABoltFindsABushAndCarriesOnPastIt()
        {
            // A bush is not solid, so unlike the tree it must NOT stop the
            // line — the counter-check that proves the stop rule is still
            // reading solidity rather than just "hit something".
            var zone = new Zone("Z");
            var actor = Actor();
            var bush = Bush();
            var behind = Creature("behind");
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(bush, 6, 5);
            zone.AddEntity(behind, 7, 5);

            var line = CavesOfOoo.Skills.SkillLine.Collect(zone, actor, 5, 5, 1, 0, 4, out bool blocked);

            Assert.AreEqual(2, line.Count, "bush and the creature behind it");
            Assert.IsFalse(blocked);
        }

        [Test]
        public void ACreatureSharingACellWithSceneryIsTakenFirst()
        {
            // Single-target powers take line[0]. A monster standing in a
            // bush is what the player is aiming at, not the bush.
            var zone = new Zone("Z");
            var actor = Actor();
            var bush = Bush();
            var monster = Creature("m");
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(bush, 6, 5);
            zone.AddEntity(monster, 6, 5);

            var line = CavesOfOoo.Skills.SkillLine.Collect(zone, actor, 5, 5, 1, 0, 4, out _);

            Assert.AreSame(monster, line[0], "aim at the thing that can hit back");
        }

        [Test]
        public void ALineOfNothingIsStillEmpty()
        {
            // Counter-check on the whole change: widening the filter must
            // not start reporting phantom targets in empty cells.
            var zone = new Zone("Z");
            var actor = Actor();
            zone.AddEntity(actor, 5, 5);

            var line = CavesOfOoo.Skills.SkillLine.Collect(zone, actor, 5, 5, 1, 0, 4, out bool blocked);

            Assert.AreEqual(0, line.Count);
            Assert.IsFalse(blocked);
        }

        [Test]
        public void ADroppedCoinInThePathIsNotHit()
        {
            var zone = new Zone("Z");
            var actor = Actor();
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(Coin(), 6, 5);

            Assert.AreEqual(0, CavesOfOoo.Skills.SkillLine.Collect(zone, actor, 5, 5, 1, 0, 4, out _).Count);
        }

        // ════════════════════════════════════════════════════════
        // Water conducts
        // ════════════════════════════════════════════════════════

        [Test]
        public void WaterConductsAShock()
        {
            // Reported: the electric spell did not hit water. Two causes —
            // the creature-only scan, and (for a plain puddle) the matrix,
            // since WaterPuddle is authored "Liquid,Water" with no
            // Conductor tag at all.
            var brine = BrinePool();
            var puddle = new Entity { ID = "puddle", BlueprintName = "WaterPuddle" };
            puddle.AddPart(new MaterialPart { MaterialTagsRaw = "Liquid,Water" });

            Assert.AreEqual(ObjectStatusVerdict.Applies,
                ObjectStatusMatrix.Evaluate(new ElectrifiedEffect(), brine), "brine");
            Assert.AreEqual(ObjectStatusVerdict.Applies,
                ObjectStatusMatrix.Evaluate(new ElectrifiedEffect(), puddle), "plain water");
        }

        [Test]
        public void OilDoesNotConduct()
        {
            // Counter-check: "Liquid" alone must not qualify, or every
            // puddle of anything would conduct and the material rules
            // would stop meaning anything.
            var oil = new Entity { ID = "oil", BlueprintName = "OilSeep" };
            oil.AddPart(new MaterialPart { MaterialTagsRaw = "Liquid,Oil,Flammable,Organic" });

            Assert.AreEqual(ObjectStatusVerdict.WrongMaterial,
                ObjectStatusMatrix.Evaluate(new ElectrifiedEffect(), oil));
            Assert.AreEqual(ObjectStatusVerdict.Applies,
                ObjectStatusMatrix.Evaluate(new BurningEffect(), oil), "but it certainly burns");
        }
    }
}
