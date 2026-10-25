using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    public sealed class RiposteVisualFeedbackTests
    {
        FiftySecondCombatFixture fixture;
        Entity attacker, defender;
        MeleeWeaponPart incomingWeapon, counterWeapon;
        readonly List<VisualReceipt> visuals = new List<VisualReceipt>();
        readonly List<string> audio = new List<string>();
        Action<WorldAudioCombatReceipt> oldAudio;
        MeleeStrikeProvenance counterDamage;
        Rolls rolls;
        static readonly FieldInfo Reservation = typeof(RiposteAllowancePart).GetField("Reserved", BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp] public void Setup()
        {
            oldAudio = WorldAudioCombat.Callback;
            fixture = new FiftySecondCombatFixture();
            attacker = fixture.Actor(x: 11);
            defender = fixture.Actor(new LongBlades_Deflect());
            FiftySecondCombatFixture.Learn(defender, new LongBlades_Riposte());
            incomingWeapon = BladeDefense.FindLongBlade(attacker);
            counterWeapon = BladeDefense.FindLongBlade(defender);
            incomingWeapon.Attributes = "Cutting";
            incomingWeapon.PenBonus = counterWeapon.PenBonus = 3;
            defender.GetPart<RenderPart>().VisualFacing = EntityVisualFacing.North;
            Assert.NotNull(Reservation, "Reservation is an internal contract, inspected explicitly rather than accessed from the test assembly.");
            visuals.Clear(); audio.Clear(); counterDamage = default;
            EntityVisualHooks.AttackCallback = CaptureVisual;
            WorldAudioCombat.Callback = receipt => audio.Add(receipt.Cue.ToString());
            defender.AddPart(new ComposableBladeAdversarialTests.DamageProbe
            {
                Callback = e => { if (e.ID == "DamageDealt") counterDamage = e.GetParameter<Damage>("Damage").Provenance; }
            });
        }

        [TearDown] public void Cleanup()
        {
            try { fixture?.Dispose(); }
            finally { WorldAudioCombat.Callback = oldAudio; visuals.Clear(); audio.Clear(); }
        }

        // The optional visual boundary will contain observer exceptions. Capture
        // facts here and assert AFTER combat; NUnit failures inside this callback
        // would otherwise be swallowed and could make a broken hook look green.
        void CaptureVisual(Entity actor, Entity target, Zone zone)
        {
            var budget = actor.GetPart<RiposteAllowancePart>();
            visuals.Add(new VisualReceipt
            {
                Actor = actor, Target = target, Zone = zone,
                Facing = actor.GetPart<RenderPart>().VisualFacing,
                Provenance = MeleeStrikeScope.Current,
                Spent = budget != null && budget.Spent,
                Reserved = budget != null && (bool)Reservation.GetValue(budget),
                TargetPresent = zone.GetEntityCell(target) != null,
                TargetHp = target.GetStatValue("Hitpoints"),
                HitDraws = rolls?.HitDraws ?? -1,
                PercentDraws = rolls?.PercentDraws ?? -1
            });
        }

        [TestCase(false)] [TestCase(true)]
        public void OrdinaryFullParryIssuesOneFacingCounterCueBeforeItsHitOrMiss(bool counterMisses)
        {
            if (counterMisses) counterWeapon.HitBonus = -100;
            rolls = new Rolls(0, 0);
            Assert.True(CombatSystem.PerformMeleeAttack(attacker, defender, fixture.Zone, rolls));
            Assert.AreEqual(2, visuals.Count, "The ordinary swing is present, but its real issued Riposte must also emit exactly one Attack cue.");
            Assert.AreSame(attacker, visuals[0].Actor); Assert.AreSame(defender, visuals[0].Target);
            var counter = visuals[1];
            Assert.AreSame(defender, counter.Actor); Assert.AreSame(attacker, counter.Target); Assert.AreSame(fixture.Zone, counter.Zone);
            Assert.AreEqual(EntityVisualFacing.East, counter.Facing);
            Assert.True(counter.TargetPresent); Assert.AreEqual(1000, counter.TargetHp, "Cue must precede the counter's damage/removal.");
            Assert.True(counter.Spent); Assert.True(counter.Reserved);
            Assert.AreEqual(1, counter.HitDraws); Assert.AreEqual(2, counter.PercentDraws, "The counter is issued after its real proc and before its accuracy draw.");
            Assert.AreEqual(MeleeStrikeOrigin.ReactiveCounter, counter.Provenance.Origin);
            Assert.AreEqual(visuals[0].Provenance.ActionId, counter.Provenance.ActionId);
            Assert.AreEqual(rolls.Incoming.StrikeId, counter.Provenance.ParentStrikeId);
            Assert.AreEqual(rolls.Incoming.RootStrikeId, counter.Provenance.RootStrikeId);
            Assert.AreEqual(rolls.Incoming.Depth + 1, counter.Provenance.Depth);
            Assert.AreEqual(1, counter.Provenance.CounterDepth); Assert.True(counter.Provenance.SuppressFollowups);
            if (!counterMisses) Assert.AreEqual(counterDamage.StrikeId, counter.Provenance.StrikeId);
            Assert.AreEqual(counterMisses ? 1000 : 999, attacker.GetStatValue("Hitpoints"));
            Assert.AreEqual(1000, defender.GetStatValue("Hitpoints"));
            // Existing landed Cutting damage rolls its Bleeding chance after
            // damage. Misses return before that draw; the cue still sees only2.
            Assert.AreEqual(2, rolls.HitDraws); Assert.AreEqual(counterMisses ? 2 : 3, rolls.PercentDraws);
            Assert.True(defender.GetPart<RiposteAllowancePart>().Spent);
            Assert.False((bool)Reservation.GetValue(defender.GetPart<RiposteAllowancePart>()));
            Assert.False(MeleeStrikeScope.IsResolving);
            CollectionAssert.AreEqual(counterMisses ? new[] { "SwingBlade", "Parry", "SwingBlade" }
                : new[] { "SwingBlade", "Parry", "SwingBlade", "HitCutting" }, audio);
        }

        [Test] public void FailedCounterChanceKeepsOnlyTheOrdinarySwingAndDoesNotChangeCounterFacing()
        {
            rolls = new Rolls(0, 99);
            Assert.True(CombatSystem.PerformMeleeAttack(attacker, defender, fixture.Zone, rolls));
            Assert.AreEqual(1, visuals.Count); Assert.AreSame(attacker, visuals[0].Actor);
            Assert.AreEqual(EntityVisualFacing.North, defender.GetPart<RenderPart>().VisualFacing);
            Assert.AreEqual(1000, attacker.GetStatValue("Hitpoints")); Assert.AreEqual(1000, defender.GetStatValue("Hitpoints"));
            Assert.AreEqual(1, rolls.HitDraws); Assert.AreEqual(2, rolls.PercentDraws);
            var budget = defender.GetPart<RiposteAllowancePart>(); Assert.False(budget.Spent); Assert.False((bool)Reservation.GetValue(budget));
            Assert.False(MeleeStrikeScope.IsResolving);
            CollectionAssert.AreEqual(new[] { "SwingBlade", "Parry" }, audio);
        }

        [Test] public void ThrowingCounterObserverCannotAbortTheStrikeOrChangeRngAllowanceAndScope()
        {
            int throws = 0;
            EntityVisualHooks.AttackCallback = (actor, target, zone) =>
            {
                CaptureVisual(actor, target, zone);
                if (actor == defender) { throws++; throw new InvalidOperationException("optional visual observer"); }
            };
            rolls = new Rolls(0, 0);
            Assert.DoesNotThrow(() => CombatSystem.PerformMeleeAttack(attacker, defender, fixture.Zone, rolls));
            Assert.AreEqual(1, throws); Assert.AreEqual(2, visuals.Count);
            Assert.AreEqual(999, attacker.GetStatValue("Hitpoints")); Assert.AreEqual(1000, defender.GetStatValue("Hitpoints"));
            Assert.AreEqual(2, rolls.HitDraws); Assert.AreEqual(3, rolls.PercentDraws);
            Assert.True(defender.GetPart<RiposteAllowancePart>().Spent);
            Assert.False((bool)Reservation.GetValue(defender.GetPart<RiposteAllowancePart>())); Assert.False(MeleeStrikeScope.IsResolving);
            Assert.AreEqual(visuals[1].Provenance.StrikeId, counterDamage.StrikeId);
            CollectionAssert.AreEqual(new[] { "SwingBlade", "Parry", "SwingBlade", "HitCutting" }, audio);
        }

        [Test] public void CounterObserverCannotReenterThroughTheActualAllowanceGate()
        {
            int reentries = 0; bool unchanged = false;
            EntityVisualHooks.AttackCallback = (actor, target, zone) =>
            {
                CaptureVisual(actor, target, zone);
                if (actor != defender) return;
                reentries++;
                int hits = rolls.HitDraws, percentages = rolls.PercentDraws;
                LongBlades_Riposte.TryCounter(actor, target, zone, rolls, rolls.Incoming);
                unchanged = hits == rolls.HitDraws && percentages == rolls.PercentDraws;
            };
            rolls = new Rolls(0, 0);
            Assert.True(CombatSystem.PerformMeleeAttack(attacker, defender, fixture.Zone, rolls));
            Assert.AreEqual(1, reentries); Assert.True(unchanged); Assert.AreEqual(2, visuals.Count);
            Assert.True(visuals[1].Spent); Assert.True(visuals[1].Reserved);
            Assert.AreEqual(999, attacker.GetStatValue("Hitpoints")); Assert.AreEqual(2, rolls.HitDraws); Assert.AreEqual(3, rolls.PercentDraws);
            Assert.False((bool)Reservation.GetValue(defender.GetPart<RiposteAllowancePart>())); Assert.False(MeleeStrikeScope.IsResolving);
        }

        [TestCase("parried")] [TestCase("armor-stopped")]
        public void IssuedCounterStillCuesWhenItsContactCausesNoDamage(string outcome)
        {
            if (outcome == "parried")
            {
                incomingWeapon.Attributes = "Cutting LongBlades";
                FiftySecondCombatFixture.Learn(attacker, new LongBlades_Deflect());
            }
            else attacker.GetPart<ArmorPart>().AV = 999;
            rolls = new Rolls(0, 0, 0);
            Assert.True(CombatSystem.PerformMeleeAttack(attacker, defender, fixture.Zone, rolls));
            Assert.AreEqual(2, visuals.Count); Assert.AreSame(defender, visuals[1].Actor);
            Assert.AreEqual(1000, attacker.GetStatValue("Hitpoints")); Assert.AreEqual(1000, defender.GetStatValue("Hitpoints"));
            Assert.AreEqual(2, rolls.HitDraws); Assert.AreEqual(outcome == "parried" ? 3 : 2, rolls.PercentDraws);
            Assert.True(defender.GetPart<RiposteAllowancePart>().Spent); Assert.True(counterDamage.IsEmpty);
        }

        [Test] public void LethalCounterFacesAndNotifiesBeforeItsTargetLeavesTheWorld()
        {
            attacker.GetStat("Hitpoints").BaseValue = 1;
            counterWeapon.BaseDamage = "1d1+8";
            rolls = new Rolls(0, 0);
            Assert.True(CombatSystem.PerformMeleeAttack(attacker, defender, fixture.Zone, rolls));
            Assert.AreEqual(2, visuals.Count); var cue = visuals[1];
            Assert.True(cue.TargetPresent); Assert.AreEqual(1, cue.TargetHp); Assert.AreEqual(EntityVisualFacing.East, cue.Facing);
            Assert.True(CombatSystem.IsDeathHandled(attacker)); Assert.IsNull(fixture.Zone.GetEntityCell(attacker));
            Assert.AreEqual(0, attacker.GetStatValue("Hitpoints")); Assert.AreEqual(1000, defender.GetStatValue("Hitpoints"));
            Assert.AreEqual(2, rolls.HitDraws); Assert.AreEqual(3, rolls.PercentDraws);
            Assert.False((bool)Reservation.GetValue(defender.GetPart<RiposteAllowancePart>())); Assert.False(MeleeStrikeScope.IsResolving);
        }

        [TestCase("disarm")] [TestCase("move")] [TestCase("stun")] [TestCase("remove")] [TestCase("skill")]
        public void PostProcInvalidationStillCancelsBeforeAnyCounterCueOrAccuracyRoll(string fault)
        {
            rolls = new Rolls(0, 0) { OnPercent = draw =>
            {
                if (draw != 2) return;
                if (fault == "disarm") counterWeapon.ParentEntity.GetPart<PhysicsPart>().Equipped = null;
                if (fault == "move") fixture.Zone.MoveEntity(attacker, 15, 10);
                if (fault == "stun") defender.ApplyEffect(new StunnedEffect());
                if (fault == "remove") fixture.Zone.RemoveEntity(attacker);
                if (fault == "skill") defender.GetPart<SkillsPart>().RemoveSkill(defender.GetPart<LongBlades_Riposte>());
            } };
            Assert.True(CombatSystem.PerformMeleeAttack(attacker, defender, fixture.Zone, rolls));
            Assert.AreEqual(1, visuals.Count); Assert.AreSame(attacker, visuals[0].Actor);
            Assert.AreEqual(1, rolls.HitDraws); Assert.AreEqual(2, rolls.PercentDraws);
            var budget = defender.GetPart<RiposteAllowancePart>(); Assert.False(budget.Spent); Assert.False((bool)Reservation.GetValue(budget));
            Assert.AreEqual(EntityVisualFacing.North, defender.GetPart<RenderPart>().VisualFacing); Assert.False(MeleeStrikeScope.IsResolving);
        }

        [TestCase(false)] [TestCase(true)]
        public void OrdinaryMultihandAndCommittedMeleeRetainOneOuterCue(bool committed)
        {
            var player = fixture.Factory.CreateEntity("Player"); Assert.True(fixture.Zone.AddEntity(player, 20, 10));
            var dagger = fixture.Factory.CreateEntity("Dagger"); Assert.True(player.GetPart<InventoryPart>().AddObject(dagger)); Assert.True(InventorySystem.Equip(player, dagger));
            player.Statistics["MultiWeaponSkillBonus"] = new Stat { Name = "MultiWeaponSkillBonus", BaseValue = 100 };
            var target = fixture.Target(21, 10); target.Statistics["DV"] = new Stat { Name = "DV", BaseValue = 1000 };
            rolls = new Rolls(0);
            Assert.True(committed ? CombatSystem.PerformCommittedMeleeAttack(player, target, fixture.Zone, rolls)
                : CombatSystem.PerformMeleeAttack(player, target, fixture.Zone, rolls));
            Assert.AreEqual(committed ? 1 : 2, rolls.HitDraws, "Prove actual primary and natural offhand strikes rather than only counting an outer event.");
            Assert.AreEqual(1, visuals.Count); Assert.AreSame(player, visuals[0].Actor); Assert.AreSame(target, visuals[0].Target);
            Assert.AreEqual(1000, target.GetStatValue("Hitpoints"));
        }

        [Test] public void VetoAndNonOptedDirectStrikeDoNotAcquireAnExtraCue()
        {
            var veto = new VetoMelee(); attacker.AddPart(veto); rolls = new Rolls(0, 0);
            Assert.False(CombatSystem.PerformMeleeAttack(attacker, defender, fixture.Zone, rolls)); Assert.IsEmpty(visuals); Assert.Zero(rolls.HitDraws); Assert.Zero(rolls.PercentDraws);
            Assert.True(attacker.RemovePart(veto)); defender.GetPart<SkillsPart>().RemoveSkill(defender.GetPart<LongBlades_Riposte>());
            var result = CombatSystem.PerformSingleAttack(attacker, defender, incomingWeapon, true, fixture.Zone, rolls);
            Assert.AreEqual(MeleeStrikeOutcome.FullParry, result.Outcome); Assert.IsEmpty(visuals);
            Assert.AreEqual(1, rolls.HitDraws); Assert.AreEqual(1, rolls.PercentDraws);
        }

        [Test] public void NullCancellationNeverInvokesTheOptedVisualBoundary()
        {
            rolls = new Rolls(0);
            var result = CombatSystem.PerformSingleAttack(null, defender, incomingWeapon, true, fixture.Zone, rolls, emitAttackVisual: true);
            Assert.AreEqual(MeleeStrikeOutcome.Cancelled, result.Outcome); Assert.IsEmpty(visuals);
            Assert.Zero(rolls.HitDraws); Assert.Zero(rolls.PercentDraws); Assert.False(MeleeStrikeScope.IsResolving);
        }

        [TestCase(true, 11, false)] [TestCase(true, 9, true)] [TestCase(false, 11, false)]
        public void ActualCounterDrivesExistingPlayerPoseAndHiddenEventsNeverReplay(bool disclosed, int attackerX, bool mirror)
        {
            defender.SetTag("Player"); defender.BlueprintName = "Player";
            if (attackerX != 11) Assert.True(fixture.Zone.MoveEntity(attacker, attackerX, 10));
            for (int y = 8; y <= 12; y++) for (int x = 8; x <= 12; x++)
                fixture.Zone.GetCell(x, y).Explored = fixture.Zone.GetCell(x, y).IsVisible = true;
            using (var presentation = new MotionFixture(fixture.Zone))
            {
                Assert.NotNull(presentation.Body); Assert.AreEqual(1, presentation.Actors.ActiveViewCount);
                if (!disclosed) fixture.Zone.GetEntityCell(defender).IsVisible = false;
                rolls = new Rolls(0, 0);
                Assert.True(CombatSystem.PerformMeleeAttack(attacker, defender, fixture.Zone, rolls));
                Assert.AreEqual(2, visuals.Count); Assert.AreEqual(999, attacker.GetStatValue("Hitpoints"));
                presentation.Tick(.01f);
                if (disclosed)
                {
                    Assert.NotNull(presentation.Body);
                    Assert.AreSame(Resources.Load<Texture2D>(FlatPixelActorMotionCatalog.ResourcePath), presentation.Body.sprite.texture);
                    Assert.AreEqual(new Rect(32, 0, 16, 24), presentation.Body.sprite.rect);
                    Assert.AreEqual(mirror, presentation.Body.flipX);
                    presentation.Tick(.20f); presentation.AssertIdle();
                }
                else
                {
                    Assert.Zero(presentation.Actors.ActiveViewCount);
                    fixture.Zone.GetEntityCell(defender).IsVisible = true;
                    presentation.Renderer.MarkCellDirty(10, 10, "Actual reveal"); presentation.Frame(); presentation.Tick(.01f);
                    Assert.AreEqual(1, presentation.Actors.ActiveViewCount); presentation.AssertIdle();
                }
            }
        }

        sealed class VetoMelee : Part
        { public override bool HandleEvent(GameEvent e) => e.ID != "BeforeMeleeAttack"; }

        sealed class NativeGraph : CameraCleanupFixture
        {
            public ZoneRenderer Presentation => Renderer;
            public AnimatedEntityRenderer Actors => Read<AnimatedEntityRenderer>(Renderer, "_entityVisualRenderer");
            public void EnableFlat(Zone zone)
            {
                Write(Renderer, "_flatPixelModeEnabled", true); Renderer.SetZone(zone);
                ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
            }
            public void Frame() => Lifecycle("LateUpdate");
        }

        sealed class MotionFixture : IDisposable
        {
            readonly HotbarSaveFixture globals;
            readonly NativeGraph graph;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly EntityInteractionVisualHandler oldInteraction = EntityVisualHooks.InteractionCallback;
            readonly EntitySelfUseVisualHandler oldSelfUse = EntityVisualHooks.SelfUseCallback;
            public ZoneRenderer Renderer => graph.Presentation;
            public AnimatedEntityRenderer Actors => graph.Actors;
            public SpriteRenderer Body => Actors.transform.Find("Visual_Player")?.Find("Body")?.GetComponent<SpriteRenderer>();
            public MotionFixture(Zone zone)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous))
                { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    globals = new HotbarSaveFixture(true, false);
                    graph = new NativeGraph(); graph.SetupCameraCleanup(); graph.EnableFlat(zone); graph.Frame();
                    Assert.True(Renderer.FlatPixelModeActive);
                }
                catch { Dispose(); throw; }
            }
            public void Frame() => graph.Frame();
            public void Tick(float seconds) => Actors.AdvancePresentation(seconds);
            public void AssertIdle()
            {
                Assert.NotNull(Body); Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath), Body.sprite.texture);
                Assert.AreEqual(new Rect(0, 0, 16, 24), Body.sprite.rect);
            }
            public void Dispose()
            {
                try { graph?.CleanupCameraFixture(); }
                finally
                {
                    globals?.Dispose(); EntityVisualHooks.InteractionCallback = oldInteraction; EntityVisualHooks.SelfUseCallback = oldSelfUse;
                    bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                }
            }
        }

        sealed class VisualReceipt
        {
            public Entity Actor, Target;
            public Zone Zone;
            public EntityVisualFacing Facing;
            public MeleeStrikeProvenance Provenance;
            public bool Spent, Reserved, TargetPresent;
            public int TargetHp, HitDraws, PercentDraws;
        }

        sealed class Rolls : Random
        {
            readonly Queue<int> percentages;
            public int HitDraws, PercentDraws;
            public MeleeStrikeProvenance Incoming;
            public Action<int> OnPercent;
            public Rolls(params int[] values) { percentages = new Queue<int>(values); }
            public override int Next(int maxValue)
            {
                if (maxValue != 100) return 0;
                PercentDraws++;
                if (PercentDraws == 1) Incoming = MeleeStrikeScope.Current;
                OnPercent?.Invoke(PercentDraws);
                return percentages.Count > 0 ? percentages.Dequeue() : 99;
            }
            public override int Next(int minValue, int maxValue)
            {
                if (maxValue == 21) { HitDraws++; return 10; }
                return minValue;
            }
        }
    }
}
