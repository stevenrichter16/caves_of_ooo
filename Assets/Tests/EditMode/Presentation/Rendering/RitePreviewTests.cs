using System;
using System.Collections;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class RitePreviewTests
    {
        Entity _caster, _target; Zone _zone; Rites_HangingBolt _rite; GrimoireChargePart _ink;
        [SetUp] public void SetUp()
        {
            AbilityClarityTestSupport.Reset(); _zone = new Zone("clarity");
            _caster = AbilityClarityTestSupport.Actor(); _zone.AddEntity(_caster, 10, 10);
            _rite = new Rites_HangingBolt(); _caster.GetPart<SkillsPart>().AddSkill(_rite);
            _target = Creature("marked kestrel", 12, 10); _target.ApplyEffect(new WetEffect());
            var book = new Entity { ID = "book" }; book.AddPart(new PhysicsPart());
            _ink = new GrimoireChargePart { Charges = 3 }; book.AddPart(_ink); _caster.GetPart<InventoryPart>().AddObject(book);
            for (int x = 4; x <= 17; x++) for (int y = 4; y <= 17; y++)
            { var cell = _zone.GetCell(x, y); cell.IsVisible = cell.Explored = true; }
        }
        [TearDown] public void TearDown() { SkillRegistry.ResetForTests(); ResonanceSystem.ResetForTests(); MessageLog.Clear(); }
        Entity Creature(string name, int x, int y)
        { var e = AbilityClarityTestSupport.Actor(name); e.Tags.Remove("Player"); _zone.AddEntity(e, x, y); return e; }
        string Preview(int dx = 1, int dy = 0, Guid? id = null, Zone zone = null)
            => (string)AbilityClarityTestSupport.Query("RitePreviewBuilder", "Build", _caster, zone ?? _zone, id ?? _rite.ActivatedAbilityID, dx, dy);

        [Test] public void VisibleTargetUsesActualResonanceAndChangingMarksChangesReadout()
        {
            string text = Preview(); StringAssert.Contains("marked kestrel", text); StringAssert.Contains("Wet", text);
            StringAssert.Contains("Spend: Wet", text); StringAssert.Contains("2 turns", text);
            _target.GetPart<StatusEffectsPart>().RemoveEffect(fx => fx is WetEffect);
            StringAssert.Contains("Spend: none", Preview());
        }
        [Test] public void PreviewNeverSpendsResourcesEffectsCooldownHealthOrFx()
        {
            var effects = _target.GetPart<StatusEffectsPart>().GetAllEffects(); var wet = (WetEffect)effects[0];
            var ability = _caster.GetPart<ActivatedAbilitiesPart>().GetAbility(_rite.ActivatedAbilityID);
            int hp = _target.GetStatValue("Hitpoints"), parts = _caster.Parts.Count;
            using (var capture = new SpellFxCapture("read-only probe", _zone, _caster))
            {
                for (int i = 0; i < 5; i++) Assert.IsNotEmpty(Preview());
                foreach (string field in new[] { "_path", "_cells", "_targets", "_reactions" })
                    Assert.Zero(((ICollection)typeof(SpellFxCapture).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(capture)).Count, field);
            }
            Assert.AreEqual(3, _ink.Charges); Assert.AreEqual(hp, _target.GetStatValue("Hitpoints"));
            Assert.Zero(ability.CooldownRemaining); Assert.AreEqual(parts, _caster.Parts.Count);
            Assert.AreSame(wet, _target.GetPart<StatusEffectsPart>().GetAllEffects()[0]); Assert.AreEqual(1f, wet.Moisture);
        }
        [Test] public void VisibleCreatureInterceptionDoesNotRevealFartherCreatureMarks()
        {
            var front = Creature("front companion", 11, 10); front.ApplyEffect(new FrozenEffect(3));
            string text = Preview(); StringAssert.Contains("front companion", text); StringAssert.DoesNotContain("marked kestrel", text);
            _zone.RemoveEntity(front); StringAssert.Contains("marked kestrel", Preview());
        }
        [Test] public void VisibleWallStopsDirectionalPreviewButRemovalRestoresIt()
        {
            var wall = new Entity { ID = "wall" }; wall.Tags.Add("Solid", ""); wall.AddPart(new PhysicsPart { Solid = true });
            _zone.AddEntity(wall, 11, 10); StringAssert.DoesNotContain("marked kestrel", Preview());
            _zone.RemoveEntity(wall); StringAssert.Contains("marked kestrel", Preview());
        }
        [TestCase("fog")] [TestCase("unexplored")] [TestCase("invisible")] [TestCase("foreign-render")]
        [TestCase("carried")] [TestCase("dead")]
        public void HiddenOrUnavailableTargetDoesNotLeakNameOrMarks(string change)
        {
            StringAssert.Contains("marked kestrel", Preview());
            if (change == "fog") _zone.GetEntityCell(_target).IsVisible = false;
            if (change == "unexplored") _zone.GetEntityCell(_target).Explored = false;
            if (change == "invisible") _target.GetPart<RenderPart>().Visible = false;
            if (change == "foreign-render") _target.GetPart<RenderPart>().ParentEntity = _caster;
            if (change == "carried") _target.GetPart<PhysicsPart>().InInventory = _caster;
            if (change == "dead") _target.GetStat("Hitpoints").BaseValue = 0;
            string text = Preview(); StringAssert.DoesNotContain("marked kestrel", text); StringAssert.DoesNotContain("Spend: Wet", text);
        }
        [Test] public void HiddenIntermediateOwnerDoesNotChangeDisclosedVisibleCandidates()
        {
            string before = Preview(); var hidden = Creature("secret owner", 11, 10); hidden.GetPart<RenderPart>().Visible = false;
            hidden.ApplyEffect(new WetEffect()); Assert.AreEqual(before, Preview());
        }
        [Test] public void UnseenIntermediateCellStopsReadingEvenWhenFarCellIsVisible()
        {
            _zone.GetCell(11, 10).IsVisible = false; StringAssert.DoesNotContain("marked kestrel", Preview());
        }
        [TestCase(0, 0)] [TestCase(2, 0)] [TestCase(1, 2)]
        public void NonEightDirectionInputIsNotNormalizedIntoAnUnchosenRead(int dx, int dy)
        { Assert.IsTrue(string.IsNullOrEmpty(Preview(dx, dy))); }
        [Test] public void ForeignAbilityAndForeignZoneCannotReadTargets()
        {
            Assert.IsTrue(string.IsNullOrEmpty(Preview(id: Guid.NewGuid())));
            Assert.IsTrue(string.IsNullOrEmpty(Preview(zone: new Zone(_zone.ZoneID))));
        }
        [Test] public void OrdinarySpellDoesNotMasqueradeAsAnInkedRitePreview()
        {
            var id = _caster.GetPart<ActivatedAbilitiesPart>().AddAbility("Ordinary", "CommandOrdinary", "Skills");
            Assert.IsTrue(string.IsNullOrEmpty(Preview(id: id)));
        }
        [Test] public void RadiusPreviewIncludesVisibleCompanionsButNotHiddenTargets()
        {
            var rite = new Rites_StormAnvil(); _caster.GetPart<SkillsPart>().AddSkill(rite);
            var ally = Creature("near companion", 10, 11); ally.Properties["PartyLeader"] = _caster.ID;
            var hidden = Creature("hidden companion", 10, 9); hidden.GetPart<RenderPart>().Visible = false;
            string text = Preview(0, 0, rite.ActivatedAbilityID);
            StringAssert.Contains("near companion", text); StringAssert.DoesNotContain("hidden companion", text);
            StringAssert.DoesNotContain("reader\nSpend", text);
        }
        [TestCase("visible")] [TestCase("removed")] [TestCase("hidden")]
        public void MorrowfastActualOffAnchorFootprintUsesOnlyVisibleCurrentBlockers(string state)
        {
            // The actual barrel occupies (49,17), but blocks its authored footprint
            // at (49,16). No ordinary object/tag occupies that ray cell.
            _zone.RemoveEntity(_caster); _zone.RemoveEntity(_target);
            _zone = new Zone(MorrowfastSceneRuntime.ZoneID);
            Assert.True(MorrowfastSceneRuntime.Install(_zone, MorrowfastTestWorld.Factory()));
            var owner = MorrowfastSceneRuntime.FindOwner(_zone, "inn-northern-barrel");
            var scene = MorrowfastSceneRuntime.GetState(_zone);
            Assert.NotNull(owner); Assert.NotNull(scene);
            foreach (var entity in _zone.GetAllEntities())
                if (entity != owner && entity != scene.ParentEntity) _zone.RemoveEntity(entity);
            Assert.True(_zone.AddEntity(_caster, 48, 16)); Assert.True(_zone.AddEntity(_target, 50, 16));
            for (int x = 47; x <= 51; x++) for (int y = 15; y <= 18; y++)
            { var cell = _zone.GetCell(x, y); cell.IsVisible = cell.Explored = true; }
            var footprint = _zone.GetCell(49, 16);
            Assert.Zero(footprint.Objects.Count); Assert.AreSame(owner, MorrowfastSceneRuntime.BlockingOwner(footprint));
            Assert.True(footprint.IsSolid(), "Actual line geometry must really see the authored off-anchor blocker.");
            if (state == "removed") _zone.RemoveEntity(owner);
            if (state == "hidden") owner.GetPart<RenderPart>().Visible = false;
            string text = Preview();
            if (state == "visible") StringAssert.DoesNotContain("marked kestrel", text);
            else StringAssert.Contains("marked kestrel", text, "Absent/invisible owners cannot affect the disclosed candidates.");
        }
    }
}
