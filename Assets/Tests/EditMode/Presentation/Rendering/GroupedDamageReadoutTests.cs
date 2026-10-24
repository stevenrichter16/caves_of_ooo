using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Real damage receipts must remain readable after the coordinator submits
    /// glyphs. Two neighboring hits are not one larger damage value.
    /// </summary>
    public sealed class GroupedDamageReadoutTests
    {
        GameObject root;
        Tilemap tiles;
        AsciiFxRenderer ascii;
        WorldFxCoordinator world;
        Zone zone;
        SpellFxMode previousMode;
        float previousSpeed, previousSound;

        [SetUp]
        public void Setup()
        {
            previousMode = SpellFxSettings.Mode;
            previousSpeed = SpellFxSettings.AnimationSpeed;
            previousSound = SpellFxSettings.SoundVolume;
            AsciiFxBus.Clear();
            SpellFxBus.Clear();
            SpellFxSettings.Mode = SpellFxMode.Full;
            SpellFxSettings.AnimationSpeed = 1;
            SpellFxSettings.SoundVolume = 0;
            root = new GameObject("Grouped damage fixture", typeof(Grid));
            root.GetComponent<Grid>().cellSize = new Vector3(1, 1.5f, 0);
            var map = new GameObject("Submitted readout tiles", typeof(Tilemap), typeof(TilemapRenderer));
            map.transform.SetParent(root.transform);
            tiles = map.GetComponent<Tilemap>();
            ascii = new AsciiFxRenderer(tiles);
            world = new WorldFxCoordinator(ascii, root.transform);
            zone = new Zone("GroupedDamageReadouts");
            foreach (var cell in zone.Cells)
            {
                cell.IsVisible = true;
                cell.Explored = true;
            }
            world.SetZone(zone);
        }

        [TearDown]
        public void Cleanup()
        {
            world?.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
            AsciiFxBus.Clear();
            SpellFxBus.Clear();
            SpellFxSettings.Mode = previousMode;
            SpellFxSettings.AnimationSpeed = previousSpeed;
            SpellFxSettings.SoundVolume = previousSound;
        }

        [Test]
        public void AdjacentActualDamageValuesRemainSeparateInsteadOfReadingAsTwelve()
        {
            var left = Actor(39, 20);
            var right = Actor(40, 20);
            CombatSystem.ApplyDamage(left, new Damage(1), source: null, zone: zone);
            CombatSystem.ApplyDamage(right, new Damage(2), source: null, zone: zone);
            int version = zone.EntityVersion;
            Assert.False(AsciiFxBus.HasPendingBlocking);

            world.Update(0);

            Assert.AreEqual(49, left.GetStatValue("Hitpoints"));
            Assert.AreEqual(48, right.GetStatValue("Hitpoints"));
            var one = SingleGlyph('1');
            var two = SingleGlyph('2');
            Assert.AreEqual(39, one.x, "The first receipt stays aligned with its owner.");
            Assert.AreEqual(40, two.x, "The second receipt stays aligned with its owner.");
            Assert.That(one.y, Is.InRange(15, 19));
            Assert.That(two.y, Is.InRange(15, 19));
            Assert.True(one.y != two.y || Math.Abs(one.x - two.x) >= 2,
                "Different damage receipts need a blank column between them on a shared row; adjacent '1' and '2' currently read as a false '12'.");
            Assert.False(world.HasBlockingFx);
            Assert.Zero(world.ActiveSequenceCount);
            Assert.AreEqual(version, zone.EntityVersion, "Readout layout cannot mutate the world.");
        }

        [Test]
        public void OneActualTwelveDamageHitRemainsOneContiguousNumber()
        {
            var target = Actor(20, 12);
            CombatSystem.ApplyDamage(target, new Damage(12), source: null, zone: zone);
            int version = zone.EntityVersion;

            world.Update(0);

            Assert.AreEqual(38, target.GetStatValue("Hitpoints"));
            var one = SingleGlyph('1');
            var two = SingleGlyph('2');
            Assert.AreEqual(one.y, two.y, "Digits from a single receipt share one row.");
            Assert.AreEqual(one.x + 1, two.x, "A genuine twelve must remain contiguous.");
            Assert.That(one.x, Is.InRange(19, 20));
            Assert.That(one.y, Is.InRange(7, 11));
            Assert.False(world.HasBlockingFx);
            Assert.Zero(world.ActiveSequenceCount);
            Assert.AreEqual(version, zone.EntityVersion);
        }

        [TestCase(SpellFxMode.Full)]
        [TestCase(SpellFxMode.Reduced)]
        [TestCase(SpellFxMode.Off)]
        public void DamageReadoutsRemainNonblockingInEveryFxMode(SpellFxMode mode)
        {
            SpellFxSettings.Mode = mode;
            world.Update(0);
            var target = Actor(20, 12);
            CombatSystem.ApplyDamage(target, new Damage(12), source: null, zone: zone);
            world.Update(0);
            var one = SingleGlyph('1');
            var two = SingleGlyph('2');
            Assert.AreEqual(one + Vector2Int.right, two);
            Assert.False(world.HasBlockingFx);
            Assert.False(AsciiFxBus.HasPendingBlocking);
            Assert.Zero(world.ActiveSequenceCount);
            world.Update(.61f);
            Assert.Zero(GlyphPositions('1').Count);
            Assert.Zero(GlyphPositions('2').Count);
            Assert.AreEqual(38, target.GetStatValue("Hitpoints"));
        }

        [Test]
        public void RepeatedActualOneDamageHitsRemainDistinctReceipts()
        {
            var target = Actor(20, 20);
            CombatSystem.ApplyDamage(target, new Damage(1), source: null, zone: zone);
            CombatSystem.ApplyDamage(target, new Damage(1), source: null, zone: zone);
            world.Update(0);
            var ones = GlyphPositions('1');
            Assert.AreEqual(2, ones.Count, "Status duplicate coalescing must not erase another real hit.");
            Assert.AreEqual(20, ones[0].x);
            Assert.AreEqual(20, ones[1].x);
            Assert.AreNotEqual(ones[0].y, ones[1].y);
            Assert.AreEqual(48, target.GetStatValue("Hitpoints"));
        }

        [Test]
        public void LaterReceiptRetainsItsLaneWhenAnOlderReceiptExpires()
        {
            var target = Actor(20, 20);
            CombatSystem.ApplyDamage(target, new Damage(1), source: null, zone: zone);
            world.Update(0);
            world.Update(.2f);
            CombatSystem.ApplyDamage(target, new Damage(2), source: null, zone: zone);
            world.Update(0);
            var two = SingleGlyph('2');
            world.Update(.41f);
            Assert.Zero(GlyphPositions('1').Count);
            Assert.AreEqual(two + Vector2Int.down, SingleGlyph('2'),
                "A surviving receipt only makes its own one-row half-life rise; it must not jump into the expired lane.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AdjacentDamageAndEventWordBothRemainComplete(bool numberFirst)
        {
            var guarded = Actor(20, 20);
            var damaged = Actor(21, 20);
            if (numberFirst)
                CombatSystem.ApplyDamage(damaged, new Damage(12), source: null, zone: zone);
            AsciiFxBus.EmitEventText(zone, guarded, "Guarded", "&C");
            if (!numberFirst)
                CombatSystem.ApplyDamage(damaged, new Damage(12), source: null, zone: zone);
            world.Update(0);
            var word = SingleGlyph('G');
            AssertWordAt("Guarded", word);
            var one = SingleGlyph('1');
            var two = SingleGlyph('2');
            Assert.AreEqual(one + Vector2Int.right, two);
            Assert.True(one.y != word.y || two.x < word.x - 1 || one.x > word.x + 7,
                "The complete damage receipt and word share the same padded collision rules.");
            Assert.False(world.HasBlockingFx);
        }

        [TestCase("hidden")]
        [TestCase("unexplored")]
        [TestCase("render-hidden")]
        public void HiddenDamageOriginDoesNotLeakIntoVisibleRowAbove(string fault)
        {
            var target = Actor(20, 12);
            if (fault == "hidden") zone.GetCell(20, 12).IsVisible = false;
            if (fault == "unexplored") zone.GetCell(20, 12).Explored = false;
            if (fault == "render-hidden") target.GetPart<RenderPart>().Visible = false;
            Assert.True(zone.GetCell(20, 11).IsVisible);
            ascii.ClipToFieldOfView = false;
            CombatSystem.ApplyDamage(target, new Damage(8), source: null, zone: zone);
            world.Update(0);
            Assert.AreEqual(42, target.GetStatValue("Hitpoints"), "Presentation must not suppress real damage.");
            Assert.Zero(GlyphPositions('8').Count, "Visible destination glyph cells are not authority to reveal hidden damage.");
        }

        [TestCase(false, "origin-hidden")]
        [TestCase(true, "origin-hidden")]
        [TestCase(false, "removed")]
        [TestCase(true, "removed")]
        [TestCase(false, "render-hidden")]
        [TestCase(true, "render-hidden")]
        public void LivingReceiptsRecheckQueuedAndActiveOwners(bool admitted, string fault)
        {
            var target = Actor(20, 12);
            CombatSystem.ApplyDamage(target, new Damage(8), source: null, zone: zone);
            if (admitted) { world.Update(0); SingleGlyph('8'); }
            if (fault == "origin-hidden") zone.GetCell(20, 12).IsVisible = false;
            if (fault == "removed") Assert.True(zone.RemoveEntity(target));
            if (fault == "render-hidden") target.GetPart<RenderPart>().Visible = false;
            ascii.ClipToFieldOfView = false;
            world.Update(0);
            Assert.Zero(GlyphPositions('8').Count);
            zone.GetCell(20, 12).IsVisible = true;
            target.GetPart<RenderPart>().Visible = true;
            world.Update(0);
            Assert.Zero(GlyphPositions('8').Count, "Retired receipts must not reappear when visibility returns.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RealLethalReceiptUsesOnlyItsFiniteDisclosedOrigin(bool hideOrigin)
        {
            var target = Actor(20, 12);
            target.GetStat("Hitpoints").BaseValue = 3;
            CombatSystem.ApplyDamage(target, new Damage(10), source: null, zone: zone);
            Assert.Null(zone.GetEntityCell(target), "Exercise actual removal before queue admission.");
            if (hideOrigin) zone.GetCell(20, 12).IsVisible = false;
            ascii.ClipToFieldOfView = false;
            world.Update(0);
            Assert.AreEqual(hideOrigin ? 0 : 1, GlyphPositions('3').Count,
                "Visible lethal damage retains its clamped receipt after removal; a hidden contact does not.");
            Assert.Zero(GlyphPositions('1').Count);
            Assert.Zero(GlyphPositions('0').Count);
            world.Update(.61f);
            Assert.Zero(GlyphPositions('3').Count);
            Assert.False(world.HasBlockingFx);
        }

        [TestCase(0, 0)]
        [TestCase(79, 0)]
        [TestCase(0, 24)]
        [TestCase(79, 24)]
        public void CompleteActualNumberFitsAtEveryWorldCorner(int x, int y)
        {
            var target = Actor(x, y);
            CombatSystem.ApplyDamage(target, new Damage(12), source: null, zone: zone);
            world.Update(0);
            var one = SingleGlyph('1');
            var two = SingleGlyph('2');
            Assert.AreEqual(one + Vector2Int.right, two);
            Assert.That(one.x, Is.InRange(0, Zone.Width - 2));
            Assert.That(one.y, Is.InRange(0, Zone.Height - 1));
            Assert.AreEqual(38, target.GetStatValue("Hitpoints"));
        }

        [TestCase("clear")]
        [TestCase("zone")]
        [TestCase("presentation")]
        public void LifecycleCancellationClearsActiveAndQueuedDamage(string change)
        {
            var target = Actor(20, 12);
            CombatSystem.ApplyDamage(target, new Damage(1), source: null, zone: zone);
            world.Update(0);
            SingleGlyph('1');
            CombatSystem.ApplyDamage(target, new Damage(2), source: null, zone: zone);
            if (change == "clear") { AsciiFxBus.Clear(); world.Update(0); }
            else if (change == "zone") world.SetZone(new Zone("OtherReadoutZone"));
            else world.Update(0, true, false);
            Assert.Zero(GlyphPositions('1').Count);
            Assert.Zero(GlyphPositions('2').Count);
            Assert.Zero(AsciiFxBus.PendingCount);
            Assert.False(world.HasBlockingFx);
        }

        [TestCase(.25f)]
        [TestCase(4f)]
        public void DamageReadingTimeUsesWallTimeRatherThanDecorativeSpeed(float speed)
        {
            SpellFxSettings.AnimationSpeed = speed;
            var target = Actor(20, 12);
            CombatSystem.ApplyDamage(target, new Damage(8), source: null, zone: zone);
            world.Update(0);
            var initial = SingleGlyph('8');
            world.Update(.31f);
            Assert.AreEqual(initial + Vector2Int.down, SingleGlyph('8'));
            world.Update(.3f);
            Assert.Zero(GlyphPositions('8').Count);
        }

        void AssertWordAt(string text, Vector2Int start)
        {
            for (int i = 0; i < text.Length; i++)
                Assert.AreSame(CP437TilesetGenerator.GetUiTile(text[i]),
                    tiles.GetTile(new Vector3Int(start.x + i, Zone.Height - 1 - start.y, 0)),
                    "Complete word character " + i);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SameIdReplacementDoesNotInheritAFormerLivingOwnersReceipt(bool admitted)
        {
            var original = Actor(20, 12);
            CombatSystem.ApplyDamage(original, new Damage(8), source: null, zone: zone);
            if (admitted) { world.Update(0); SingleGlyph('8'); }
            Assert.True(zone.RemoveEntity(original));
            var replacement = Actor(20, 12, original.ID);
            world.Update(0);
            Assert.Zero(GlyphPositions('8').Count);
            CombatSystem.ApplyDamage(replacement, new Damage(2), source: null, zone: zone);
            world.Update(0);
            SingleGlyph('2');
            Assert.Zero(GlyphPositions('8').Count);
            Assert.AreEqual(48, replacement.GetStatValue("Hitpoints"));
        }

        [Test]
        public void WarmSubmittedReadoutLayoutAllocatesNothingOrMutatesTheWorld()
        {
            var guarded = Actor(20, 20);
            var damaged = Actor(21, 20);
            AsciiFxBus.EmitEventText(zone, guarded, "Guarded", "&C");
            CombatSystem.ApplyDamage(damaged, new Damage(8), source: null, zone: zone);
            world.Update(0);
            AssertWordAt("Guarded", SingleGlyph('G'));
            var number = SingleGlyph('8');
            int version = zone.EntityVersion;
            // Measure the actual warmed renderer/layout, not unrelated audio or
            // coordinator diagnostics. Zero delta holds the finite labels alive.
            for (int i = 0; i < 20; i++) ascii.Update(0, 0);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) ascii.Update(0, 0);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Zero(allocated);
            Assert.AreEqual(number, SingleGlyph('8'));
            AssertWordAt("Guarded", SingleGlyph('G'));
            Assert.AreEqual(version, zone.EntityVersion);
            Assert.AreEqual(42, damaged.GetStatValue("Hitpoints"));
        }

        [Test]
        public void EventWordDoesNotPaintOverADisclosedNeighboringActor()
        {
            var owner = Actor(39, 20);
            var neighbor = Actor(40, 19);
            AsciiFxBus.EmitEventText(zone, owner, "Stunned", "&C");
            world.Update(0);
            Assert.Zero(ascii.ActiveEventTextCount,
                "All three same-column lanes lie on or beyond the nearer actor; omit the whole word rather than imply its condition.");
            Assert.Zero(GlyphPositions('S').Count);
            Assert.Null(tiles.GetTile(new Vector3Int(40, Zone.Height - 1 - 19, 0)),
                "Stunned must still never replace the neighboring visible actor's world cell.");
            Assert.AreSame(zone.GetCell(40, 19), zone.GetEntityCell(neighbor));
        }

        [TestCase("cell-hidden")]
        [TestCase("render-hidden")]
        public void HiddenNeighborDoesNotChangeEventWordPlacement(string hidden)
        {
            var owner = Actor(39, 20);
            AsciiFxBus.EmitEventText(zone, owner, "Stunned", "&C");
            world.Update(0);
            var baseline = SingleGlyph('S');
            Assert.AreEqual(new Vector2Int(36, 19), baseline);
            world.CancelAll();
            var neighbor = Actor(40, 19);
            if (hidden == "cell-hidden") zone.GetCell(40, 19).IsVisible = false;
            else neighbor.GetPart<RenderPart>().Visible = false;
            AsciiFxBus.EmitEventText(zone, owner, "Stunned", "&C");
            world.Update(0);
            Assert.AreEqual(baseline, SingleGlyph('S'),
                "Moving a label around an undisclosed actor would reveal hidden occupancy.");
            AssertWordAt("Stunned", baseline);
        }

        [Test]
        public void ReusedDamageRequestCannotCarryAnOldOwnerValueOrLethalAllowance()
        {
            var target = Actor(20, 12);
            target.GetStat("Hitpoints").BaseValue = 3;
            CombatSystem.ApplyDamage(target, new Damage(10), source: null, zone: zone);
            var emitted = AsciiFxBus.Drain();
            AsciiFxRequest number = null;
            foreach (var request in emitted)
                if (request.Type == AsciiFxRequestType.FloatingNumber) number = request;
            Assert.NotNull(number);
            Assert.AreEqual(3, number.Number);
            Assert.AreSame(target, number.Anchor);
            Assert.NotNull(number.OriginCell);
            Assert.True(number.AllowOriginOnly);
            foreach (var request in emitted)
                if (request != number) AsciiFxBus.Release(request);
            // Release the numeric request last to exercise that exact pooled
            // instance when a later ordinary particle is rented.
            AsciiFxBus.Release(number);
            AsciiFxBus.EmitParticle(zone, 5, 5, '*', "&R", 1);
            var reused = AsciiFxBus.Drain();
            try
            {
                Assert.AreEqual(1, reused.Count);
                Assert.AreSame(number, reused[0]);
                Assert.AreEqual(AsciiFxRequestType.Particle, reused[0].Type);
                Assert.Zero(reused[0].Number);
                Assert.Null(reused[0].Anchor);
                Assert.Null(reused[0].OriginCell);
                Assert.Null(reused[0].Text);
                Assert.False(reused[0].AllowOriginOnly);
            }
            finally { foreach (var request in reused) AsciiFxBus.Release(request); }
        }

        // Post-review hypotheses: these cases cover gaps beyond the initial
        // combat, fog, lifetime and collision regression pass.
        [Test]
        public void DelayedOwnerReceiptWaitsThenCanBeCancelledBeforeAnyGlyphAppears()
        {
            var target = Actor(20, 12);
            AsciiFxBus.EmitFloatingNumber(zone, target, 12, "&R", delay: .2f);
            world.Update(0);
            Assert.AreEqual(1, ascii.ActiveNumberCount);
            Assert.Zero(GlyphPositions('1').Count);
            world.Update(.1f);
            Assert.Zero(GlyphPositions('1').Count);
            world.Update(.11f);
            Assert.AreEqual(SingleGlyph('1') + Vector2Int.right, SingleGlyph('2'));
            world.CancelAll();
            AsciiFxBus.EmitFloatingNumber(zone, target, 8, "&R", delay: .2f);
            world.Update(.1f);
            Assert.AreEqual(1, ascii.ActiveNumberCount);
            Assert.Zero(GlyphPositions('8').Count);
            world.CancelAll();
            world.Update(.3f);
            Assert.Zero(ascii.ActiveNumberCount);
            Assert.Zero(AsciiFxBus.PendingCount);
            Assert.Zero(GlyphPositions('8').Count);
            Assert.AreEqual(50, target.GetStatValue("Hitpoints"), "The delayed presentation API cannot manufacture damage.");
        }

        [Test]
        public void ReplacedCapturedCellRetiresTheReceiptEvenWhenBothCellsAreVisible()
        {
            var target = Actor(20, 12);
            CombatSystem.ApplyDamage(target, new Damage(8), source: null, zone: zone);
            world.Update(0);
            SingleGlyph('8');
            var old = zone.GetCell(20, 12);
            var replacement = new Cell(20, 12, zone) { Explored = true, IsVisible = true };
            replacement.Objects.AddRange(old.Objects);
            zone.Cells[20, 12] = replacement;
            world.Update(0);
            Assert.Zero(GlyphPositions('8').Count,
                "Matching coordinates and visible occupants cannot authorize a stale captured Cell reference.");
            zone.Cells[20, 12] = old;
            world.Update(0);
            Assert.Zero(GlyphPositions('8').Count, "Restoring a cell does not resurrect retired transient feedback.");
        }

        [Test]
        public void LethalOriginReceiptDoesNotAttachToALivingSameIdReplacement()
        {
            var original = Actor(20, 12);
            original.GetStat("Hitpoints").BaseValue = 3;
            CombatSystem.ApplyDamage(original, new Damage(10), source: null, zone: zone);
            Assert.Null(zone.GetEntityCell(original));
            var replacement = Actor(20, 12, original.ID);
            world.Update(0);
            Assert.Zero(GlyphPositions('3').Count,
                "The origin-only lethal allowance must not visually attribute an old death to a replacement actor.");
            CombatSystem.ApplyDamage(replacement, new Damage(2), source: null, zone: zone);
            world.Update(0);
            SingleGlyph('2');
            Assert.Zero(GlyphPositions('3').Count);
            Assert.AreEqual(48, replacement.GetStatValue("Hitpoints"));
        }

        [Test]
        public void ForeignRenderAndLaterCarriedCustodyRetireLivingOwnerFeedback()
        {
            var target = Actor(20, 12);
            var foreign = new Entity { ID = "readout-custodian" };
            var render = target.GetPart<RenderPart>();
            CombatSystem.ApplyDamage(target, new Damage(8), source: null, zone: zone);
            render.ParentEntity = foreign;
            world.Update(0);
            Assert.Zero(GlyphPositions('8').Count);
            render.ParentEntity = target;
            CombatSystem.ApplyDamage(target, new Damage(9), source: null, zone: zone);
            world.Update(0);
            SingleGlyph('9');
            // Deliberately retain the stale ground list entry: present custody
            // must win even before the caller cleans that list up.
            target.GetPart<PhysicsPart>().InInventory = foreign;
            world.Update(0);
            Assert.Zero(GlyphPositions('9').Count);
            target.GetPart<PhysicsPart>().InInventory = null;
            world.Update(0);
            Assert.Zero(GlyphPositions('8').Count);
            Assert.Zero(GlyphPositions('9').Count);
            Assert.AreEqual(33, target.GetStatValue("Hitpoints"));
        }

        [Test]
        public void MovingDiagonalOwnerCannotJoinAnotherActorsCompleteNumber()
        {
            var left = Actor(20, 20);
            var right = Actor(22, 19);
            CombatSystem.ApplyDamage(left, new Damage(12), source: null, zone: zone);
            CombatSystem.ApplyDamage(right, new Damage(3), source: null, zone: zone);
            world.Update(0);
            Assert.AreEqual(new Vector2Int(22, 18), SingleGlyph('3'));
            Assert.True(zone.MoveEntity(right, 22, 20));
            world.Update(0);
            var one = SingleGlyph('1');
            var three = SingleGlyph('3');
            Assert.AreEqual(one + Vector2Int.right, SingleGlyph('2'));
            Assert.AreEqual(22, three.x, "Movement must keep the receipt at its own current owner column.");
            Assert.AreNotEqual(one.y, three.y,
                "The moved owner's '3' must not turn its neighbor's genuine '12' into a false '123'.");
            world.Update(.31f);
            Assert.AreEqual(SingleGlyph('1') + Vector2Int.right, SingleGlyph('2'));
            Assert.AreNotEqual(SingleGlyph('1').y, SingleGlyph('3').y);
            Assert.AreEqual(38, left.GetStatValue("Hitpoints"));
            Assert.AreEqual(47, right.GetStatValue("Hitpoints"));
        }

        [Test]
        public void ThreeNearbyReceiptLanesStayBoundedWhenAFourthRealHitArrives()
        {
            var target = Actor(20, 20);
            for (int damage = 1; damage <= 4; damage++)
                CombatSystem.ApplyDamage(target, new Damage(damage), source: null, zone: zone);
            world.Update(0);
            var one = SingleGlyph('1');
            var two = SingleGlyph('2');
            var three = SingleGlyph('3');
            Assert.AreNotEqual(one.y, two.y);
            Assert.AreNotEqual(one.y, three.y);
            Assert.AreNotEqual(two.y, three.y);
            Assert.AreEqual(3, ascii.ActiveNumberCount);
            Assert.Zero(GlyphPositions('4').Count, "An exhausted local lane budget omits the whole newest receipt, never fragments it.");
            Assert.AreEqual(40, target.GetStatValue("Hitpoints"), "All four real hits still affect simulation.");
            Assert.False(world.HasBlockingFx);
            world.Update(.61f);
            Assert.Zero(ascii.ActiveNumberCount);
            CombatSystem.ApplyDamage(target, new Damage(4), source: null, zone: zone);
            world.Update(0);
            SingleGlyph('4');
            Assert.AreEqual(36, target.GetStatValue("Hitpoints"));
        }

        // Attribution follow-up: a distant label cannot sit beyond an unrelated
        // disclosed actor within its glyph columns and appear to describe it.
        [TestCase("visible", false)]
        [TestCase("cell-hidden", true)]
        [TestCase("unexplored", true)]
        [TestCase("render-hidden", true)]
        [TestCase("outside-span", true)]
        [TestCase("covered", true)]
        [TestCase("foreign-render", true)]
        [TestCase("carried", true)]
        public void CrowdedWordCannotClaimANearerActorWhileUndisclosedOccupancyDoesNotMoveIt(
            string competingActor, bool expectedWord)
        {
            var owner = Actor(40, 20);
            QueueCrowdedBleeding(owner);
            world.Update(0);
            var baseline = SingleGlyph('B');
            Assert.AreEqual(new Vector2Int(37, 15), baseline);
            AssertWordAt("Bleeding", baseline);
            world.CancelAll();

            var neighbor = Actor(competingActor == "outside-span" ? 45 : 41, 17);
            neighbor.SetTag("Player");
            if (competingActor == "cell-hidden") zone.GetCell(41, 17).IsVisible = false;
            if (competingActor == "unexplored") zone.GetCell(41, 17).Explored = false;
            if (competingActor == "render-hidden") neighbor.GetPart<RenderPart>().Visible = false;
            if (competingActor == "covered")
            {
                var cover = new Entity { ID = "non-actor-corridor-cover" };
                cover.AddPart(new RenderPart { Visible = true, RenderLayer = 100 });
                Assert.True(zone.AddEntity(cover, 41, 17));
                Assert.AreSame(cover, zone.GetCell(41, 17).GetTopVisibleObject());
            }
            if (competingActor == "foreign-render" || competingActor == "carried")
            {
                var custodian = new Entity { ID = "competing-actor-custodian" };
                if (competingActor == "foreign-render") neighbor.GetPart<RenderPart>().ParentEntity = custodian;
                else neighbor.GetPart<PhysicsPart>().InInventory = custodian;
                Assert.AreSame(neighbor, zone.GetCell(41, 17).GetTopVisibleObject());
                Assert.AreSame(zone.GetCell(41, 17), zone.GetEntityCell(neighbor));
            }
            // World reveal cannot make a hidden competitor affect layout.
            ascii.ClipToFieldOfView = false;
            QueueCrowdedBleeding(owner);
            int version = zone.EntityVersion;
            world.Update(0);

            Assert.AreEqual(new Vector2Int(40, 19), SingleGlyph('1'));
            Assert.AreEqual(new Vector2Int(40, 17), SingleGlyph('2'));
            Assert.AreEqual(2, ascii.ActiveNumberCount);
            Assert.AreEqual(expectedWord ? 1 : 0, ascii.ActiveEventTextCount);
            if (expectedWord)
            {
                Assert.AreEqual(baseline, SingleGlyph('B'));
                AssertWordAt("Bleeding", baseline);
            }
            else Assert.Zero(GlyphPositions('B').Count,
                "Bleeding at row15 would appear above the closer Player41,17, although its true recipient is40,20.");
            Assert.AreEqual(version, zone.EntityVersion);
            Assert.AreEqual(44, owner.GetStatValue("Hitpoints"));
            Assert.False(world.HasBlockingFx);
        }

        [Test]
        public void ACompetingActorWithTheSameIdIsNotTheExactReadoutRecipient()
        {
            var owner = Actor(40, 20, "duplicate-readout-id");
            var neighbor = Actor(41, 18, owner.ID);
            Assert.AreNotSame(owner, neighbor);
            Assert.AreEqual(owner.ID, neighbor.ID);
            Assert.AreSame(zone.GetCell(40, 20), zone.GetEntityCell(owner));
            Assert.AreSame(zone.GetCell(41, 18), zone.GetEntityCell(neighbor));
            QueueCrowdedBleeding(owner);
            world.Update(0);
            Assert.AreEqual(2, ascii.ActiveNumberCount);
            Assert.Zero(ascii.ActiveEventTextCount,
                "Only reference identity exempts the recipient; equal string IDs do not exempt a nearer actor.");
            Assert.Zero(GlyphPositions('B').Count);
        }

        [Test]
        public void ACompleteDamageNumberAlsoCannotFloatBeyondANearerActor()
        {
            var owner = Actor(40, 20);
            var neighbor = Actor(41, 18);
            CombatSystem.ApplyDamage(owner, new Damage(8), source: null, zone: zone);
            CombatSystem.ApplyDamage(owner, new Damage(12), source: null, zone: zone);
            world.Update(0);
            Assert.AreEqual(new Vector2Int(40, 19), SingleGlyph('8'));
            Assert.AreEqual(1, ascii.ActiveNumberCount);
            Assert.Zero(GlyphPositions('1').Count);
            Assert.Zero(GlyphPositions('2').Count,
                "A whole twelve is omitted when the remaining lanes would imply damage to the nearer actor.");
            Assert.AreEqual(30, owner.GetStatValue("Hitpoints"));
            Assert.AreEqual(50, neighbor.GetStatValue("Hitpoints"));
        }

        [Test]
        public void AttributionUsesTheRecipientsCurrentCellRatherThanItsCapturedOrigin()
        {
            var owner = Actor(40, 20);
            QueueCrowdedBleeding(owner);
            world.Update(0);
            Assert.AreEqual(new Vector2Int(37, 15), SingleGlyph('B'));
            var neighbor = Actor(41, 21);
            Assert.True(zone.MoveEntity(owner, 40, 22));
            world.Update(0);
            Assert.AreEqual(new Vector2Int(40, 21), SingleGlyph('1'));
            Assert.AreEqual(new Vector2Int(40, 19), SingleGlyph('2'));
            Assert.Zero(ascii.ActiveEventTextCount,
                "The new candidate row17 must test through21 to current recipient22, not stop at captured row20.");
            Assert.Zero(GlyphPositions('B').Count);
            Assert.AreSame(zone.GetCell(41, 21), zone.GetEntityCell(neighbor));
            Assert.True(zone.GetCell(40, 20).IsVisible, "Origin visibility remains independently valid.");
        }

        [Test]
        public void EnteringTheAttributionCorridorRetiresTheWordWithoutDelayedReplay()
        {
            var owner = Actor(40, 20);
            var neighbor = Actor(45, 18);
            QueueCrowdedBleeding(owner);
            world.Update(0);
            Assert.AreEqual(new Vector2Int(37, 15), SingleGlyph('B'));
            Assert.True(zone.MoveEntity(neighbor, 41, 18));
            world.Update(0);
            Assert.Zero(GlyphPositions('B').Count);
            Assert.Zero(ascii.ActiveEventTextCount);
            Assert.True(zone.MoveEntity(neighbor, 45, 18));
            world.Update(0);
            Assert.Zero(GlyphPositions('B').Count, "Leaving the corridor cannot resurrect a retired event.");
            AsciiFxBus.EmitEventText(zone, owner, "Bleeding", "&R");
            world.Update(0);
            Assert.AreEqual(new Vector2Int(37, 15), SingleGlyph('B'),
                "A genuinely new event is still allowed when its local lanes become truthful again.");
        }

        [Test]
        public void HalfLifeRiseCannotRelocateAWordPastAnUnrelatedActor()
        {
            var owner = Actor(40, 20);
            Actor(41, 18);
            AsciiFxBus.EmitEventText(zone, owner, "Bleeding", "&R");
            world.Update(0);
            Assert.AreEqual(new Vector2Int(37, 19), SingleGlyph('B'),
                "The initial label lies between its own recipient and the other actor and is truthful.");
            world.Update(.56f);
            Assert.Zero(ascii.ActiveEventTextCount,
                "The rise reaches the other actor's row; higher lanes must not move the word beyond that actor.");
            Assert.Zero(GlyphPositions('B').Count);
            Assert.AreEqual(50, owner.GetStatValue("Hitpoints"));
        }

        [Test]
        public void TopEdgeCandidatesCheckTheSignedIntervalBelowTheirRecipient()
        {
            var owner = Actor(40, 0);
            var neighbor = Actor(41, 2);
            AsciiFxBus.EmitEventText(zone, owner, "Bleeding", "&R");
            world.Update(0);
            var start = SingleGlyph('B');
            Assert.AreEqual(new Vector2Int(37, 1), start,
                "Rows5 and3 lie beyond the actor at2; row1 is the nearby truthful lane below recipient0.");
            AssertWordAt("Bleeding", start);
            Assert.AreSame(zone.GetCell(41, 2), zone.GetEntityCell(neighbor));
            Assert.AreEqual(1, ascii.ActiveEventTextCount);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FiniteLethalReceiptUsesItsOriginForTheSameAttributionRule(bool hideNeighbor)
        {
            var lower = Actor(39, 20);
            var upper = Actor(39, 18);
            var owner = Actor(40, 20);
            owner.GetStat("Hitpoints").BaseValue = 12;
            var neighbor = Actor(41, 18);
            if (hideNeighbor) zone.GetCell(41, 18).IsVisible = false;
            // Two earlier real receipts consume the lethal number's lower lanes.
            CombatSystem.ApplyDamage(lower, new Damage(11), source: null, zone: zone);
            CombatSystem.ApplyDamage(upper, new Damage(22), source: null, zone: zone);
            CombatSystem.ApplyDamage(owner, new Damage(99), source: null, zone: zone);
            Assert.Null(zone.GetEntityCell(owner));
            ascii.ClipToFieldOfView = false;
            world.Update(0);
            AssertWordAt("11", new Vector2Int(39, 19));
            AssertWordAt("22", new Vector2Int(39, 17));
            Assert.AreEqual(hideNeighbor ? 3 : 2, ascii.ActiveNumberCount);
            if (hideNeighbor) AssertWordAt("12", new Vector2Int(40, 15));
            else
            {
                Assert.Null(tiles.GetTile(new Vector3Int(40, Zone.Height - 1 - 15, 0)));
                Assert.Null(tiles.GetTile(new Vector3Int(41, Zone.Height - 1 - 15, 0)));
            }
            Assert.AreEqual(0, owner.GetStatValue("Hitpoints"), "Lethal receipt remains clamped to real remaining HP.");
            Assert.AreEqual(50, neighbor.GetStatValue("Hitpoints"));
            world.Update(.61f);
            Assert.Zero(ascii.ActiveNumberCount);
        }

        [TestCase(0, 7)]
        [TestCase(79, 72)]
        public void AttributionChecksTheWholeClampedWordWidthAtWorldEdges(int ownerX, int competingX)
        {
            var owner = Actor(ownerX, 20);
            var neighbor = Actor(competingX, 18);
            QueueCrowdedBleeding(owner);
            world.Update(0);
            Assert.AreEqual(new Vector2Int(ownerX, 19), SingleGlyph('1'));
            Assert.AreEqual(new Vector2Int(ownerX, 17), SingleGlyph('2'));
            Assert.Zero(ascii.ActiveEventTextCount,
                "The far end of the clamped eight-cell word must not sit beyond an unrelated actor.");
            Assert.Zero(GlyphPositions('B').Count);
            Assert.AreSame(zone.GetCell(competingX, 18), zone.GetEntityCell(neighbor));
        }

        [Test]
        public void WarmCrowdedAttributionChecksAllocateNothingAndKeepValidLanesStable()
        {
            var owner = Actor(40, 20);
            Actor(45, 18); // Just outside the complete Bleeding span.
            QueueCrowdedBleeding(owner);
            world.Update(0);
            AssertWordAt("Bleeding", new Vector2Int(37, 15));
            int version = zone.EntityVersion;
            for (int i = 0; i < 20; i++) ascii.Update(0, 0);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) ascii.Update(0, 0);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Zero(allocated);
            AssertWordAt("Bleeding", new Vector2Int(37, 15));
            Assert.AreEqual(new Vector2Int(40, 19), SingleGlyph('1'));
            Assert.AreEqual(new Vector2Int(40, 17), SingleGlyph('2'));
            Assert.AreEqual(version, zone.EntityVersion);
            Assert.AreEqual(47, owner.GetStatValue("Hitpoints"));
        }

        void QueueCrowdedBleeding(Entity owner)
        {
            CombatSystem.ApplyDamage(owner, new Damage(1), source: null, zone: zone);
            CombatSystem.ApplyDamage(owner, new Damage(2), source: null, zone: zone);
            AsciiFxBus.EmitEventText(zone, owner, "Bleeding", "&R");
        }


        Entity Actor(int x, int y, string id = null)
        {
            var result = new Entity { ID = id ?? Guid.NewGuid().ToString("N") };
            result.SetTag("Creature");
            result.AddPart(new PhysicsPart());
            result.AddPart(new RenderPart { Visible = true, DisplayName = "readout subject" });
            result.Statistics["Hitpoints"] = new Stat
            {
                Owner = result, Name = "Hitpoints", BaseValue = 50, Max = 50
            };
            Assert.True(zone.AddEntity(result, x, y));
            return result;
        }

        Vector2Int SingleGlyph(char glyph)
        {
            var positions = GlyphPositions(glyph);
            Assert.AreEqual(1, positions.Count, "Exactly one submitted '" + glyph + "' is expected.");
            return positions[0];
        }

        List<Vector2Int> GlyphPositions(char glyph)
        {
            var positions = new List<Vector2Int>();
            // Accept either existing gameplay glyphs or pure UI glyphs: this
            // regression concerns receipt grouping, not a font implementation.
            var gameplay = CP437TilesetGenerator.GetTile(glyph);
            var ui = CP437TilesetGenerator.GetUiTile(glyph);
            for (int y = 0; y < Zone.Height; y++)
            for (int x = 0; x < Zone.Width; x++)
            {
                var tile = tiles.GetTile(new Vector3Int(x, Zone.Height - 1 - y, 0));
                if (tile == gameplay || tile == ui)
                    positions.Add(new Vector2Int(x, y));
            }
            return positions;
        }
    }
}
