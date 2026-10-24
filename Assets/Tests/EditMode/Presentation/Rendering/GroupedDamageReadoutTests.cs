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
            var start = SingleGlyph('S');
            AssertWordAt("Stunned", start);
            Assert.AreEqual(36, start.x, "Keep the readout aligned with its actual owner.");
            Assert.That(start.y, Is.InRange(15, 19));
            Assert.False(start.y == 19 && start.x <= 40 && start.x + 7 > 40,
                "Stunned must not replace the neighboring visible actor's world cell.");
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
