using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    public class SpellFxSequenceTests
    {
        [SetUp] public void Setup() { SpellFxBus.Clear(); AsciiFxBus.Clear(); ResonanceSystem.ResetForTests(); ResonanceSystem.EnsureInitialized(); }
        [TearDown] public void Cleanup() { SpellFxBus.Clear(); AsciiFxBus.Clear(); ResonanceSystem.ResetForTests(); }

        private static Entity Actor(Zone zone, string id, int x, int y, int hp = 100)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        [Test] public void SequenceCopiesCollectionsAndDrainsInOrder()
        {
            var path = new List<Point> { new Point(3, 4) };
            var sequence = new SpellFxSequence("first", new Zone(), null, new Point(2, 4), path: path);
            path[0] = new Point(50, 50);
            SpellFxBus.Emit(sequence);
            SpellFxBus.Emit(new SpellFxSequence("second", sequence.Zone, null, sequence.Source));
            Assert.IsTrue(SpellFxBus.HasPendingBlocking);
            var drained = SpellFxBus.Drain();
            Assert.AreEqual("first", drained[0].SpellId);
            Assert.AreEqual("second", drained[1].SpellId);
            Assert.AreEqual(3, drained[0].Path[0].X);
            Assert.IsFalse(SpellFxBus.HasPendingBlocking);
            Assert.IsEmpty(SpellFxBus.Drain());
        }

        [Test] public void KindleCapturesDeadTargetAndStopsAtFirstBodyWithoutLegacyDuplicate()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            var victim = Actor(zone, "victim", 7, 5, 1);
            Actor(zone, "behind", 9, 5);
            var skill = new Pyromancy_Kindle();
            var ctx = new SkillEventContext { Attacker = caster, Zone = zone, Rng = new Random(3), DirectionX = 1 };
            Assert.IsTrue(skill.OnCommand(ctx));
            var sequence = SpellFxBus.Drain()[0];
            Assert.AreEqual("Pyromancy_Kindle", sequence.SpellId);
            Assert.AreEqual(2, sequence.Path.Count);
            Assert.AreEqual(7, sequence.Path[1].X);
            Assert.AreEqual("victim", sequence.Targets[0].TargetId);
            Assert.AreEqual(7, sequence.Targets[0].Cell.X);
            Assert.IsTrue(sequence.Targets[0].Died);
            Assert.IsTrue(ctx.BlocksTurnAdvance);
            Assert.IsFalse(AsciiFxBus.Drain().Exists(r => r.Type == AsciiFxRequestType.Projectile || r.Type == AsciiFxRequestType.Beam || r.Type == AsciiFxRequestType.ChargeOrbit));
        }

        [Test] public void RejectedCastDoesNotEmitOrBlock()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            var ctx = new SkillEventContext { Attacker = caster, Zone = zone, Rng = new Random(3) };
            Assert.IsFalse(new Pyromancy_Kindle().OnCommand(ctx));
            Assert.IsFalse(ctx.BlocksTurnAdvance);
            Assert.IsEmpty(SpellFxBus.Drain());
        }

        [Test] public void JetBlastCapturesOriginalAndResolvedMovementCells()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            Actor(zone, "victim", 6, 5);
            var ctx = new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1 };
            Assert.IsTrue(new Hydromancy_JetBlast().OnCommand(ctx));
            var sequence = SpellFxBus.Drain()[0];
            var target = sequence.Targets[0];
            Assert.AreEqual(6, target.Cell.X);
            Assert.AreEqual(7, target.FinalCell.X);
            Assert.IsTrue(target.Moved);
            Assert.AreEqual(2, target.Damage);
            Assert.IsTrue(new List<Point>(sequence.AffectedCells).Exists(p => p.X == 7 && p.Y == 4));
        }

        [Test] public void FullyResistedDamageIsResolvedAndCosmeticCaptureUsesNoGameplayRandom()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            var victim = Actor(zone, "victim", 6, 5);
            victim.Statistics["HeatResistance"] = new Stat { Owner = victim, Name = "HeatResistance", BaseValue = 100, Min = -100, Max = 100 };
            var random = new Random(37);
            var reference = new Random(37);
            // Ember Spit uses no damage dice; a wet target also avoids ignition randomness.
            victim.ApplyEffect(new WetEffect(1f), caster, zone);
            Assert.IsTrue(new Pyromancy_EmberSpit().OnCommand(new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1, Rng = random }));
            var sequence = SpellFxBus.Drain()[0];
            Assert.AreEqual(0, sequence.Targets[0].Damage);
            Assert.IsTrue(sequence.Targets[0].Resisted);
            Assert.AreEqual(reference.Next(), random.Next());
        }

        [Test] public void DamageResistanceCountercheck_DryTargetStillTakesDamage()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            Actor(zone, "victim", 6, 5);
            Assert.IsTrue(new Pyromancy_EmberSpit().OnCommand(new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1, Rng = new Random(37) }));
            var sequence = SpellFxBus.Drain()[0];
            Assert.Greater(sequence.Targets[0].Damage, 0);
            Assert.IsFalse(sequence.Targets[0].Resisted);
        }

        [Test] public void DisposedOrNestedCaptureCannotLeakResultsAcrossSequences()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            var target = Actor(zone, "target", 6, 5);
            using (var abandoned = new SpellFxCapture("abandoned", zone, caster))
                SpellFxCapture.AffectCell(zone, 20, 20);
            Assert.IsEmpty(SpellFxBus.Drain());
            using (var outer = new SpellFxCapture("outer", zone, caster))
            {
                SpellFxCapture.AffectCell(zone, 7, 5);
                using (var inner = new SpellFxCapture("inner", zone, target, blocking: false))
                {
                    SpellFxCapture.Target(zone, caster);
                    CombatSystem.ApplyDamage(caster, 3, target, zone);
                    inner.Commit();
                }
                outer.Commit();
            }
            var sequences = SpellFxBus.Drain();
            Assert.AreEqual(2, sequences.Count);
            Assert.AreEqual("inner", sequences[0].SpellId);
            Assert.AreEqual(3, sequences[0].Targets[0].Damage);
            Assert.AreEqual("outer", sequences[1].SpellId);
            Assert.IsEmpty(sequences[1].Targets);
        }

        [Test] public void DrenchLobCapturesItsActualFirstBodyEndpointAndBurstGeometry()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            Actor(zone, "victim", 7, 5);
            Actor(zone, "splash", 7, 7);
            Assert.IsTrue(new Hydromancy_DrenchLob().OnCommand(new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1 }));
            var sequence = SpellFxBus.Drain()[0];
            Assert.AreEqual(2, sequence.Path.Count);
            Assert.AreEqual(7, sequence.Path[1].X);
            Assert.AreEqual(25, sequence.AffectedCells.Count);
            Assert.AreEqual(2, sequence.Targets.Count);
            Assert.IsFalse(new List<Point>(sequence.AffectedCells).Exists(p => p.X > 9));
        }

        [Test] public void OverloadStopsAtAnActualNonConductorAfterTheFirstHit()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            var conductor = Actor(zone, "conductor", 6, 5);
            conductor.ApplyEffect(new WetEffect(0.8f), caster, zone);
            Actor(zone, "dry", 8, 5);
            var beyond = Actor(zone, "beyond", 9, 5);
            beyond.ApplyEffect(new WetEffect(0.8f), caster, zone);
            Assert.IsTrue(new Galvanism_Overload().OnCommand(new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1, Rng = new Random(2) }));
            var sequence = SpellFxBus.Drain()[0];
            Assert.AreEqual(3, sequence.Path.Count);
            Assert.AreEqual(8, sequence.Path[2].X);
            Assert.AreEqual(1, sequence.Targets.Count);
            Assert.AreEqual("conductor", sequence.Targets[0].TargetId);
            Assert.AreEqual(100, beyond.GetStatValue("Hitpoints"));
        }

        [Test] public void FourRetortsAreNonblockingAndRepeatedEventsShareAnAccent()
        {
            var zone = new Zone();
            var attacker = Actor(zone, "attacker", 5, 5);
            var defender = Actor(zone, "defender", 6, 5);
            var ctx = new SkillEventContext { Attacker = attacker, Defender = defender, Zone = zone };
            var skills = new BaseSkillPart[] { new Pyromancy_ScorchRetort(), new Cryomancy_FrostRetort(), new Galvanism_ShockRetort(), new Corrosion_AcidRetort() };
            foreach (var skill in skills)
            {
                skill.OnDefenderAfterAttackMissed(ctx);
                skill.OnDefenderAfterAttackMissed(ctx);
            }
            Assert.IsFalse(SpellFxBus.HasPendingBlocking);
            var sequences = SpellFxBus.Drain();
            Assert.AreEqual(4, sequences.Count);
            foreach (var sequence in sequences)
            {
                Assert.AreEqual(6, sequence.Source.X);
                Assert.AreEqual("attacker", sequence.Targets[0].TargetId);
                Assert.Greater(sequence.Targets[0].Damage, 0);
            }
        }

        [TestCase(typeof(Rites_StormAnvil))]
        [TestCase(typeof(Rites_RenderedSteam))]
        [TestCase(typeof(Rites_HangingBolt))]
        [TestCase(typeof(Rites_Fulmination))]
        [TestCase(typeof(Rites_ShatteredRime))]
        [TestCase(typeof(Rites_StillHeart))]
        [TestCase(typeof(Rites_VerdigrisBloom))]
        [TestCase(typeof(Rites_HollowCoin))]
        [TestCase(typeof(Rites_SunderingWord))]
        [TestCase(typeof(Rites_BloodletterLedger))]
        public void ColdRitesReportZeroActualMarksAndOneInkSpent(Type type)
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            var inventory = new InventoryPart { MaxWeight = 500 };
            caster.AddPart(inventory);
            var book = new Entity { ID = "book" };
            var ink = new GrimoireChargePart { Charges = 10, MaxCharges = 10 };
            book.AddPart(ink); inventory.AddObject(book);
            Actor(zone, "target", 6, 5);
            var rite = (ConsumingRiteSkillBase)Activator.CreateInstance(type);
            caster.AddPart(rite);
            Assert.IsTrue(rite.OnCommand(new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1, Rng = new Random(7) }));
            var sequences = SpellFxBus.Drain();
            Assert.AreEqual(1, sequences.Count);
            Assert.AreEqual(type.Name, sequences[0].SpellId);
            Assert.AreEqual(0, sequences[0].MarksConsumed);
            Assert.AreEqual(1f, sequences[0].Intensity);
            Assert.AreEqual(9, ink.Charges);
            Assert.IsNotEmpty(sequences[0].Targets);
        }

        [Test] public void ScaldingVeilRefusalSpendsNothingThenWetCastReportsActualResonance()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            var inventory = new InventoryPart { MaxWeight = 500 };
            caster.AddPart(inventory);
            var book = new Entity { ID = "book" };
            var ink = new GrimoireChargePart { Charges = 10, MaxCharges = 10 };
            book.AddPart(ink); inventory.AddObject(book);
            var rite = new Rites_ScaldingVeil(); caster.AddPart(rite);
            var ctx = new SkillEventContext { Attacker = caster, Zone = zone };
            Assert.IsFalse(rite.OnCommand(ctx));
            Assert.AreEqual(10, ink.Charges);
            Assert.IsEmpty(SpellFxBus.Drain());
            caster.ApplyEffect(new WetEffect(0.8f), caster, zone);
            Assert.IsTrue(rite.OnCommand(ctx));
            var sequence = SpellFxBus.Drain()[0];
            Assert.AreEqual(1, sequence.MarksConsumed);
            Assert.AreEqual(1, sequence.Targets[0].MarksConsumed);
            Assert.Greater(sequence.Intensity, 1f);
            Assert.AreEqual(9, ink.Charges);
        }

        [Test] public void EveryRegisteredMagicActiveUsesTheCaptureSpine()
        {
            SkillRegistry.ResetForTests();
            int count = 0;
            var schools = new HashSet<string> { "Pyromancy", "Cryomancy", "Galvanism", "Hydromancy", "Corrosion", "Spellcraft", "Rites" };
            foreach (var tree in SkillRegistry.GetAllSkills())
            {
                if (!schools.Contains(tree.Name)) continue;
                foreach (var power in tree.Powers)
                {
                    var type = typeof(BaseSkillPart).Assembly.GetType("CavesOfOoo.Skills." + power.Class);
                    Assert.IsNotNull(type, power.Class);
                    var skill = (BaseSkillPart)Activator.CreateInstance(type);
                    if (skill.DeclareActivatedAbility(null) == null) continue;
                    Assert.IsInstanceOf<SpellSkillPart>(skill, power.Class);
                    count++;
                }
            }
            Assert.AreEqual(49, count, "content coverage is tied to actual registered castable classes");
            SkillRegistry.ResetForTests();
        }

        [Test] public void NullContextAndNullCasterDoNotStartASequence()
        {
            var spells = new SpellSkillPart[] { new Pyromancy_Kindle(), new Hydromancy_JetBlast(), new Spellcraft_WardGleam(), new Rites_StormAnvil() };
            foreach (var spell in spells)
            {
                Assert.IsFalse(spell.OnCommand(null));
                Assert.IsFalse(spell.OnCommand(new SkillEventContext { Zone = new Zone() }));
            }
            Assert.IsEmpty(SpellFxBus.Drain());
        }

        [Test] public void ConeBlockedAtItsFirstCellNeverIncludesCellsBehindTheWall()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            var wall = new Entity { ID = "wall" }; wall.Tags["Solid"] = ""; zone.AddEntity(wall, 6, 5);
            Actor(zone, "behind", 7, 5);
            // Jet Blast's existing ground-only success policy remains unchanged even when the ground walk is empty.
            Assert.IsTrue(new Hydromancy_JetBlast().OnCommand(new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1 }));
            var sequence = SpellFxBus.Drain()[0];
            Assert.IsEmpty(sequence.Path);
            Assert.IsEmpty(sequence.Targets);
            Assert.IsFalse(new List<Point>(sequence.AffectedCells).Exists(p => p.X > 5));
        }

        [Test] public void ProjectileAtZoneEdgeHasNoPathAndIsFree()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", Zone.Width - 1, 5);
            var ctx = new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1, Rng = new Random(2) };
            Assert.IsFalse(new Pyromancy_Kindle().OnCommand(ctx));
            Assert.IsFalse(ctx.BlocksTurnAdvance);
            Assert.IsEmpty(SpellFxBus.Drain());
        }

        [Test] public void SingleTargetGripPathStopsAtVictimBeforeFartherWall()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            Actor(zone, "victim", 6, 5);
            var wall = new Entity { ID = "wall" }; wall.Tags["Solid"] = ""; zone.AddEntity(wall, 8, 5);
            Assert.IsTrue(new Cryomancy_RimeGrip().OnCommand(new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1 }));
            var sequence = SpellFxBus.Drain()[0];
            Assert.AreEqual(1, sequence.Path.Count);
            Assert.AreEqual(6, sequence.Path[0].X);
            Assert.IsFalse(new List<Point>(sequence.AffectedCells).Exists(p => p.X == 8));
        }

        [Test] public void MaterialResultsCopyTheResolvedClampedStrengthAndRefreshDuration()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            zone.TileState.WriteCoating(6, 5, "water", 20);
            using (var capture = new SpellFxCapture("test", zone, caster))
            {
                ZoneTileStateSystem.AddCharge(zone, 6, 5, 100, caster);
                ZoneTileStateSystem.WriteCoating(zone, 6, 5, "water", 2, caster);
                capture.Commit();
            }
            var sequence = SpellFxBus.Drain()[0];
            Assert.AreEqual(zone.TileState.Charge(6, 5), sequence.Reactions[0].Amount);
            Assert.AreEqual("charge", sequence.Reactions[0].Value);
            Assert.AreEqual(20, sequence.Reactions[1].Amount);
            zone.TileState.AddCharge(6, 5, -100);
            zone.TileState.RemoveCoating(6, 5, "water");
            Assert.Greater(sequence.Reactions[0].Amount, 0);
            Assert.AreEqual(20, sequence.Reactions[1].Amount);
        }

        [Test] public void LethalStructuralHitRetainsDestroyedSceneryCell()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            var scenery = new Entity { ID = "barrel", BlueprintName = "barrel" };
            scenery.AddPart(new RenderPart { DisplayName = "barrel" });
            scenery.AddPart(new DestructiblePart { HP = 1, MaxHP = 1 });
            zone.AddEntity(scenery, 6, 5);
            using (var capture = new SpellFxCapture("test", zone, caster))
            {
                DestructionSystem.RouteDamage(scenery, new Damage(20), caster, zone);
                capture.Commit();
            }
            var target = SpellFxBus.Drain()[0].Targets[0];
            Assert.AreEqual("barrel", target.TargetId);
            Assert.AreEqual(6, target.Cell.X);
            Assert.IsTrue(target.Died);
            Assert.AreEqual(20, target.Damage, "structural HP preserves the existing un-clamped resolved damage");
            Assert.IsFalse(target.Moved);
        }

        [Test] public void CalmReportsPacificationAndAlreadyPeacefulNoOpSeparately()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            var victim = Actor(zone, "victim", 6, 5);
            var brain = new BrainPart(); victim.AddPart(brain);
            var ctx = new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1, Rng = new Random(5) };
            var calm = new Spellcraft_Calm();
            Assert.IsTrue(calm.OnCommand(ctx));
            var applied = SpellFxBus.Drain()[0].Targets[0];
            CollectionAssert.Contains(applied.AppliedEffects, "Pacified");
            Assert.IsEmpty(applied.RejectedEffects);
            Assert.IsTrue(brain.HasGoal<NoFightGoal>());
            Assert.IsTrue(calm.OnCommand(ctx), "the already-peaceful bolt still consumes its cast");
            var repeated = SpellFxBus.Drain()[0].Targets[0];
            CollectionAssert.Contains(repeated.RejectedEffects, "Pacified");
            Assert.IsEmpty(repeated.AppliedEffects);
            Assert.IsTrue(brain.HasGoal<NoFightGoal>());
            CollectionAssert.Contains(applied.AppliedEffects, "Pacified", "first snapshot remains unchanged");
        }

        [Test] public void CalmWithoutABrainRecordsNoPacificationButStillConsumesCast()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            Actor(zone, "victim", 6, 5);
            Assert.IsTrue(new Spellcraft_Calm().OnCommand(new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1, Rng = new Random(5) }));
            var target = SpellFxBus.Drain()[0].Targets[0];
            CollectionAssert.Contains(target.RejectedEffects, "Pacified");
            Assert.IsEmpty(target.AppliedEffects);
            Assert.AreEqual(0, target.Damage);
        }

        [TestCase(99, 1)]
        [TestCase(90, 4)]
        [TestCase(100, 0)]
        public void BloodletterRecordsActualCappedHealing(int before, int expected)
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            caster.GetStat("Hitpoints").BaseValue = before;
            var inventory = new InventoryPart { MaxWeight = 500 }; caster.AddPart(inventory);
            var book = new Entity { ID = "book" };
            var ink = new GrimoireChargePart { Charges = 10, MaxCharges = 10 };
            book.AddPart(ink); inventory.AddObject(book);
            var victim = Actor(zone, "victim", 6, 5, 1);
            victim.ApplyEffect(new WetEffect(0.8f), caster, zone);
            var rite = new Rites_BloodletterLedger(); caster.AddPart(rite);
            Assert.IsTrue(rite.OnCommand(new SkillEventContext { Attacker = caster, Zone = zone, DirectionX = 1, Rng = new Random(7) }));
            var sequence = SpellFxBus.Drain()[0];
            var healing = new List<SpellFxCellResult>(sequence.Reactions).Find(r => r.Kind == "healing");
            Assert.IsNotNull(healing);
            Assert.AreEqual(expected, healing.Amount);
            Assert.AreEqual("Hitpoints", healing.Value);
            Assert.AreEqual(5, healing.Cell.X);
            Assert.AreEqual(expected, caster.GetStatValue("Hitpoints") - before);
            Assert.IsTrue(sequence.Targets[0].Died, "healing still pays out over the corpse");
            Assert.AreEqual(9, ink.Charges);
        }

        [Test] public void WardGleamRecordsEachActualEquipmentCleanseAtTheCasterAndRefusesWhenClean()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            caster.AddPart(new ActivatedAbilitiesPart());
            var skills = new SkillsPart(); caster.AddPart(skills);
            var inventory = new InventoryPart(); caster.AddPart(inventory);
            skills.AddSkill(new Spellcraft_WardGleam());
            var sword = new Entity { ID = "sword", BlueprintName = "Sword" };
            sword.AddPart(new RenderPart { DisplayName = "sword" });
            sword.AddPart(new MaterialPart { MaterialID = "Wood", MaterialTagsRaw = "Wood,Organic,Flammable" });
            Assert.IsTrue(sword.ApplyEffect(new AcidicEffect(0.8f)));
            Assert.IsTrue(sword.ApplyEffect(new CharredEffect()));
            inventory.Objects.Add(sword); inventory.Equip(sword, "Hand");
            Assert.IsTrue(skills.TryRouteSkillCommand("CommandWardGleam", zone, new Random(4)));
            var sequence = SpellFxBus.Drain()[0];
            var cleansed = new List<SpellFxCellResult>(sequence.Reactions).FindAll(r => r.Kind == "cleansing");
            Assert.AreEqual(2, cleansed.Count);
            Assert.AreEqual("Acidic", cleansed[0].Value);
            Assert.AreEqual("Charred", cleansed[1].Value);
            foreach (var result in cleansed)
            {
                Assert.AreEqual(1, result.Amount);
                Assert.AreEqual(5, result.Cell.X); Assert.AreEqual(5, result.Cell.Y);
            }
            Assert.IsFalse(sword.HasEffect<AcidicEffect>()); Assert.IsFalse(sword.HasEffect<CharredEffect>());
            var ability = caster.GetPart<ActivatedAbilitiesPart>().AbilityList[0];
            Assert.AreEqual(Spellcraft_WardGleam.COOLDOWN, ability.CooldownRemaining);
            ability.CooldownRemaining = 0;
            Assert.IsFalse(skills.TryRouteSkillCommand("CommandWardGleam", zone, new Random(4)));
            Assert.AreEqual(0, ability.CooldownRemaining);
            Assert.IsEmpty(SpellFxBus.Drain());
        }

        [Test] public void PyroclasmReportsConsumedBurningDurationBeforeLethalDamageAndRejectsWithoutBurning()
        {
            var zone = new Zone();
            var caster = Actor(zone, "caster", 5, 5);
            var victim = Actor(zone, "victim", 6, 5, 1);
            victim.ApplyEffect(new BurningEffect(1f, caster, new Random(4)), caster, zone);
            victim.GetEffect<BurningEffect>().Duration = 7;
            var ctx = new SkillEventContext { Attacker = caster, Zone = zone, Rng = new Random(4) };
            var spell = new Pyromancy_Pyroclasm();
            Assert.IsTrue(spell.OnCommand(ctx));
            var sequence = SpellFxBus.Drain()[0];
            var consumed = new List<SpellFxCellResult>(sequence.Reactions).Find(r => r.Kind == "consumed-status");
            Assert.IsNotNull(consumed);
            Assert.AreEqual("Burning", consumed.Value); Assert.AreEqual(7, consumed.Amount);
            Assert.AreEqual(6, consumed.Cell.X); Assert.AreEqual(5, consumed.Cell.Y);
            Assert.IsFalse(victim.HasEffect<BurningEffect>());
            Assert.IsFalse(spell.OnCommand(ctx));
            Assert.IsFalse(ctx.BlocksTurnAdvance);
            Assert.IsEmpty(SpellFxBus.Drain());
            Assert.AreEqual(7, consumed.Amount, "resolved consumption survives target removal");
        }
    }
}
