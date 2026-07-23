using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Docs/THROWN-MUTATION-COMBAT-PLAN.md SM5b/D13. Generic retaliation
    /// hook inside <see cref="CombatSystem.ApplyDamage"/> -- closes the gap
    /// where thrown-weapon and mutation damage never provoked hostility
    /// (BrainPart.SetPersonallyHostile previously had exactly one live call
    /// site: InputHandler.ExecuteAttackOnNPC's pre-swing melee call, which
    /// fires even on a miss and never runs for thrown/mutation damage at
    /// all). This hook fires at damage-landed time instead (post-veto,
    /// post-resistance) -- complementary to, not a replacement for,
    /// InputHandler's existing call, since SetPersonallyHostile/HashSet.Add
    /// is idempotent.
    ///
    /// V1-scoped: only source.HasTag("Player") triggers retaliation, mirroring
    /// today's only live trigger. NPC-on-NPC incidental damage (mutation AoE
    /// catching a bystander) is deliberately NOT covered -- see plan doc §5.
    /// </summary>
    public class RetaliationHookTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        [Test]
        public void ApplyDamage_PlayerDamagesNeutralNpc_NpcBecomesPersonallyHostileToPlayer()
        {
            var zone = new Zone("RetaliationZone");
            var player = CreateCreature("player", hp: 20);
            player.Tags["Player"] = "";
            var npc = CreateCreatureWithBrain("neutral-npc", hp: 20);

            zone.AddEntity(player, 5, 5);
            zone.AddEntity(npc, 6, 5);

            var brain = npc.GetPart<BrainPart>();
            Assert.IsFalse(brain.IsPersonallyHostileTo(player),
                "Precondition: NPC must not already be hostile to the player.");

            CombatSystem.ApplyDamage(npc, 5, player, zone);

            Assert.IsTrue(brain.IsPersonallyHostileTo(player),
                "A landed player hit must make the NPC personally hostile in response.");
        }

        [Test]
        public void ApplyDamage_PlayerDamagesNeutralNpc_SurvivesToNextTurn_StaysHostile()
        {
            // A mutation-bolt that doesn't kill leaves the survivor hostile
            // for its next AI turn -- the retaliation must outlive the hit
            // itself, not just be a transient flag during ApplyDamage.
            var zone = new Zone("RetaliationZone.Survives");
            var player = CreateCreature("player", hp: 20);
            player.Tags["Player"] = "";
            var npc = CreateCreatureWithBrain("neutral-npc", hp: 20);

            zone.AddEntity(player, 5, 5);
            zone.AddEntity(npc, 6, 5);

            CombatSystem.ApplyDamage(npc, 5, player, zone);

            Assert.Greater(npc.GetStatValue("Hitpoints", 0), 0, "NPC must survive this hit.");
            Assert.IsTrue(npc.GetPart<BrainPart>().IsPersonallyHostileTo(player));
        }

        [Test]
        public void ApplyDamage_PlayerLandsFatalHit_NpcIsHostileBeforeItDies()
        {
            // Order matters: the retaliation hook must fire before/alongside
            // damage landing, not get skipped because the target died. Fire
            // a lethal hit and confirm the BrainPart (still reachable pre-
            // corpse) recorded the hostility.
            var zone = new Zone("RetaliationZone.Fatal");
            var player = CreateCreature("player", hp: 20);
            player.Tags["Player"] = "";
            var npc = CreateCreatureWithBrain("neutral-npc", hp: 5);

            zone.AddEntity(player, 5, 5);
            zone.AddEntity(npc, 6, 5);

            var brain = npc.GetPart<BrainPart>();

            CombatSystem.ApplyDamage(npc, 999, player, zone);

            Assert.LessOrEqual(npc.GetStatValue("Hitpoints", 0), 0, "This hit must be lethal.");
            Assert.IsTrue(brain.IsPersonallyHostileTo(player),
                "Retaliation must register even on a killing blow, before the corpse/death handling.");
        }

        // ===== Counter-checks (CLAUDE.md §3.4 — every positive assertion
        // pairs with an identical-setup-but-flag-flipped negative) =====

        [Test]
        public void ApplyDamage_NpcDamagesNpc_DoesNotCreateRetaliation_V1PlayerOnlyGuard()
        {
            // Counter-check for the first test: flip the source from Player
            // to a non-Player NPC. V1 is explicitly scoped to player-sourced
            // damage only -- an NPC-on-NPC hit (e.g. incidental mutation
            // splash) must NOT add a PersonalEnemies entry.
            var zone = new Zone("RetaliationZone.NpcSource");
            var attackerNpc = CreateCreature("attacker-npc", hp: 20);
            var defenderNpc = CreateCreatureWithBrain("defender-npc", hp: 20);

            zone.AddEntity(attackerNpc, 5, 5);
            zone.AddEntity(defenderNpc, 6, 5);

            CombatSystem.ApplyDamage(defenderNpc, 5, attackerNpc, zone);

            Assert.IsFalse(defenderNpc.GetPart<BrainPart>().IsPersonallyHostileTo(attackerNpc),
                "NPC-on-NPC damage must not trigger retaliation in V1 (player-only guard).");
        }

        [Test]
        public void ApplyDamage_NullSource_DoesNotCrashOrRetaliate()
        {
            // Counter-check: environmental/DoT damage with source=null
            // (BleedingEffect, PoisonedEffect, etc. all pass null) must not
            // throw and must not retaliate against a null attacker.
            var zone = new Zone("RetaliationZone.NullSource");
            var npc = CreateCreatureWithBrain("neutral-npc", hp: 20);
            zone.AddEntity(npc, 6, 5);

            Assert.DoesNotThrow(() => CombatSystem.ApplyDamage(npc, 5, null, zone));
            Assert.AreEqual(0, npc.GetPart<BrainPart>().PersonalEnemies.Count);
        }

        [Test]
        public void ApplyDamage_PlayerSelfDamage_DoesNotAddSelfHostility()
        {
            // Counter-check: source == target must not add self-hostility,
            // even though no current production path reaches this (self-
            // damage mutations bypass ApplyDamage entirely per the plan
            // doc's §3.1 verification) -- per CLAUDE.md §3.4, pair every
            // positive assertion with its flip regardless of whether a live
            // path exercises it today.
            var zone = new Zone("RetaliationZone.SelfDamage");
            var player = CreateCreatureWithBrain("player", hp: 20);
            player.Tags["Player"] = "";
            zone.AddEntity(player, 5, 5);

            CombatSystem.ApplyDamage(player, 5, player, zone);

            Assert.IsFalse(player.GetPart<BrainPart>().IsPersonallyHostileTo(player));
        }

        // ===== Helpers =====

        private static Entity CreateCreature(string name, int hp)
        {
            var entity = new Entity { BlueprintName = name };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ArmorPart());
            return entity;
        }

        private static Entity CreateCreatureWithBrain(string name, int hp)
        {
            var entity = CreateCreature(name, hp);
            entity.Tags["Faction"] = "Monsters";
            entity.AddPart(new BrainPart());
            return entity;
        }
    }
}
