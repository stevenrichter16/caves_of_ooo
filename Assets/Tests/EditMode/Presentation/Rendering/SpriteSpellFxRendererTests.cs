using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SpriteSpellFxRendererTests
    {
        private GameObject _root;
        private SpriteSpellFxRenderer _renderer;
        private Zone _zone;
        [SetUp] public void SetUp()
        {
            SpellFxSettings.Mode = SpellFxMode.Full;
            _root = new GameObject("Spell FX tests");
            _renderer = new SpriteSpellFxRenderer(_root.transform);
            _zone = new Zone("spell-fx-tests");
            _renderer.SetZone(_zone);
        }
        [TearDown] public void TearDown()
        {
            _renderer.Dispose();
            Object.DestroyImmediate(_root);
            SpellFxSettings.Mode = SpellFxMode.Full;
        }
        private SpellFxSequence Sequence(string id = "Pyromancy_Kindle", bool blocking = true)
            => new SpellFxSequence(id, _zone, null, new Point(5, 5),
                new[] { new Point(6, 5), new Point(7, 5) }, new[] { new Point(7, 5) },
                new[] { new SpellFxTargetResult("victim", new Point(7, 5), new Point(7, 5), 4) },
                blocksTurnAdvance: blocking);
        private void Visible(int x, int y)
        { _zone.GetCell(x, y).IsVisible = true; _zone.GetCell(x, y).Explored = true; }
        private void Reveal()
        {
            for (int y = 0; y < Zone.Height; y++)
                for (int x = 0; x < Zone.Width; x++) Visible(x, y);
        }
        [Test] public void HiddenAndWrongZone_DoNotQueueOrBlock()
        {
            Assert.AreEqual(0f, _renderer.Play(Sequence(), SpellFxCatalog.Find("Pyromancy_Kindle")));
            Assert.AreEqual(0, _renderer.ActiveCount);
            Visible(5, 5);
            Assert.Greater(_renderer.Play(Sequence(), SpellFxCatalog.Find("Pyromancy_Kindle")), 0f);
            Assert.IsTrue(_renderer.HasBlockingFx);
            _renderer.SetZone(new Zone("different"));
            Assert.AreEqual(0f, _renderer.Play(Sequence(), SpellFxCatalog.Find("Pyromancy_Kindle")));
            Assert.IsFalse(_renderer.HasBlockingFx);
        }
        [Test] public void UnexploredCell_NeverQueuesAndSpritesUseTheWorldCameraLayer()
        {
            _zone.GetCell(5,5).IsVisible = true;
            Assert.AreEqual(0f, _renderer.Play(Sequence(),SpellFxCatalog.Find("Pyromancy_Kindle")));
            Visible(5,5);
            Assert.Greater(_renderer.Play(Sequence(),SpellFxCatalog.Find("Pyromancy_Kindle")),0f);
            _renderer.Update(0f);
            const int worldLayer = 8; // GameplayRenderLayers.WorldLayer camera contract.
            foreach (var view in _root.GetComponentsInChildren<SpriteRenderer>()) Assert.AreEqual(worldLayer,view.gameObject.layer);
        }
        [Test] public void ScheduledChargeBlocksImmediately_CompletionAndPoolReuseResetEverything()
        {
            Reveal();
            float duration = _renderer.Play(Sequence(), SpellFxCatalog.Find("Pyromancy_Kindle"));
            Assert.IsTrue(_renderer.HasBlockingFx);
            _renderer.Update(0f);
            Assert.Greater(_renderer.ActiveSpriteCount, 0);
            _renderer.Update(duration + .01f);
            Assert.AreEqual(0, _renderer.ActiveCount);
            Assert.IsFalse(_renderer.HasBlockingFx);
            Assert.Greater(_renderer.PoolCount, 0);
            int allocated = _renderer.AllocatedSpriteCount;
            _renderer.Play(Sequence(blocking: false), SpellFxCatalog.Find("Pyromancy_Kindle"));
            _renderer.Update(0f);
            Assert.IsFalse(_renderer.HasBlockingFx);
            Assert.AreEqual(allocated, _renderer.AllocatedSpriteCount);
            _renderer.ClearAll();
            Assert.AreEqual(_renderer.AllocatedSpriteCount, _renderer.PoolCount);
            foreach (var view in _root.GetComponentsInChildren<SpriteRenderer>())
            {
                Assert.IsFalse(view.enabled); Assert.IsNull(view.sprite);
                Assert.AreEqual(Color.white, view.color);
                Assert.AreEqual(Vector3.one, view.transform.localScale);
            }
        }
        [Test] public void LargeImpactFragments_NeverOverhangAnUnseenCell()
        {
            Visible(7, 5);
            var definition = SpellFxCatalog.Find("Pyromancy_Kindle");
            _renderer.Play(Sequence(), definition);
            _renderer.Update(definition.CastDuration + definition.ChargeDuration + .06f);
            var visible = _root.GetComponentsInChildren<SpriteRenderer>().Where(v => v.enabled).ToArray();
            Assert.IsNotEmpty(visible);
            foreach (SpriteRenderer view in visible)
            {
                Assert.GreaterOrEqual(view.bounds.min.x, 7f - .001f);
                Assert.LessOrEqual(view.bounds.max.x, 8f + .001f);
                Assert.GreaterOrEqual(view.bounds.min.y, Zone.Height - 6f - .001f);
                Assert.LessOrEqual(view.bounds.max.y, Zone.Height - 5f + .001f);
            }
            _renderer.ClearAll(); Reveal();
            _renderer.Play(Sequence(), definition);
            _renderer.Update(definition.CastDuration + definition.ChargeDuration + .06f);
            Assert.Greater(_root.GetComponentsInChildren<SpriteRenderer>().Count(v => v.enabled), visible.Length);
        }
        [Test] public void HiddenTarget_DoesNotPaintItsVisibleNeighbors()
        {
            Visible(5,5); Visible(7,4);
            var definition = SpellFxCatalog.Find("Pyromancy_Kindle");
            _renderer.Play(Sequence(), definition);
            _renderer.Update(definition.CastDuration + definition.ChargeDuration + .06f);
            Assert.IsFalse(_root.GetComponentsInChildren<SpriteRenderer>().Any(v => v.enabled));
        }
        [Test] public void OffMissingArtAndHiddenPresentation_ReleaseAllWork()
        {
            Reveal(); var sequence = Sequence();
            var definition = SpellFxCatalog.Find(sequence.SpellId);
            _renderer.Play(sequence, definition); _renderer.Update(0f);
            SpellFxSettings.Mode = SpellFxMode.Off; _renderer.Update(0f);
            Assert.AreEqual(0, _renderer.ActiveCount); Assert.IsFalse(_renderer.HasBlockingFx);
            Assert.AreEqual(0f, _renderer.Play(sequence, definition));
            SpellFxSettings.Mode = SpellFxMode.Full;
            Assert.AreEqual(0f, _renderer.Play(sequence, new SpellFxDefinition { ID = "missing", DetailSheet = "absent", ImpactSheet = "absent" }));
            Assert.Greater(_renderer.Play(sequence, definition), 0f);
            _renderer.SetPresentationVisible(false);
            Assert.AreEqual(0, _renderer.ActiveCount); Assert.IsFalse(_renderer.HasBlockingFx);
        }
        [Test] public void ReducedMode_EnforcesIndependentSpriteBudget()
        {
            Reveal(); SpellFxSettings.Mode = SpellFxMode.Reduced;
            var cells = new List<Point>();
            for (int y = 3; y < 14; y++) for (int x = 4; x < 15; x++) cells.Add(new Point(x,y));
            var sequence = new SpellFxSequence("Cryomancy_GlacialWall", _zone, null, new Point(5,5), affectedCells: cells);
            float duration = _renderer.Play(sequence, SpellFxCatalog.Find(sequence.SpellId));
            _renderer.Update(.25f);
            Assert.Greater(_renderer.ActiveSpriteCount, 0);
            Assert.LessOrEqual(_renderer.ActiveSpriteCount, SpriteSpellFxRenderer.ReducedSpriteBudget);
            Assert.Greater(_renderer.DroppedSpriteCount, 0);
            _renderer.Update(duration);
            Assert.IsFalse(_renderer.HasBlockingFx);
        }
        [Test] public void Aftermath_UsesActualReactionMaterialAndIgnoresZeroWrites()
        {
            Reveal(); var cell = new Point(7,5);
            SpellFxSequence Reaction(int amount) => new SpellFxSequence("Pyromancy_Kindle", _zone, null, new Point(5,5),
                affectedCells:new[]{cell}, reactions:new[]{new SpellFxCellResult(cell,"reaction","steam",amount)});
            var definition = SpellFxCatalog.Find("Pyromancy_Kindle");
            _renderer.Play(Reaction(1),definition);
            _renderer.Update(definition.CastDuration + definition.ChargeDuration + definition.ImpactDuration + .01f);
            var active = _root.GetComponentsInChildren<SpriteRenderer>().Where(v=>v.enabled).ToArray();
            Assert.AreEqual(1,active.Length);
            StringAssert.Contains("water_detail",active[0].sprite.texture.name);
            _renderer.ClearAll();
            _renderer.Play(Reaction(0),definition);
            _renderer.Update(definition.CastDuration + definition.ChargeDuration + definition.ImpactDuration + .01f);
            Assert.IsFalse(_root.GetComponentsInChildren<SpriteRenderer>().Any(v=>v.enabled));
        }
        [Test] public void RitePayoff_EchoesFollowActualTargetResonance()
        {
            Reveal(); var cell = new Point(7,5);
            SpellFxSequence Rite(float resonance) => new SpellFxSequence("Rites_BloodletterLedger",_zone,null,new Point(5,5),
                targets:new[]{new SpellFxTargetResult("victim",cell,cell,resonance:resonance)});
            var definition=SpellFxCatalog.Find("Rites_BloodletterLedger");
            _renderer.Play(Rite(1f),definition); int cold=_renderer.ActiveCount;
            _renderer.ClearAll(); _renderer.Play(Rite(2f),definition); Assert.AreEqual(cold+1,_renderer.ActiveCount);
            _renderer.ClearAll(); _renderer.Play(Rite(3f),definition); Assert.AreEqual(cold+2,_renderer.ActiveCount);
        }
        [Test] public void EffectOnlyRejection_UsesResistedArtWhileMixedSuccessKeepsImpactArt()
        {
            Reveal();
            var cell = new Point(7,5);
            var definition = SpellFxCatalog.Find("Cryomancy_Frostbind");
            void Check(int damage, string[] applied, string[] rejected, bool expectedResisted)
            {
                _renderer.ClearAll();
                var sequence = new SpellFxSequence(definition.ID,_zone,null,new Point(5,5),
                    affectedCells:new[]{cell},targets:new[]{new SpellFxTargetResult("victim",cell,cell,
                        damage:damage,appliedEffects:applied,rejectedEffects:rejected)});
                _renderer.Play(sequence,definition);
                _renderer.Update(definition.CastDuration + definition.ChargeDuration + .01f);
                var impacts = _root.GetComponentsInChildren<SpriteRenderer>()
                    .Where(v=>v.enabled && v.sprite.texture.name.EndsWith("_impact")).ToArray();
                Assert.AreEqual(9,impacts.Length);
                // Resisted is the bottom 32px atlas row; successful impact is the top row.
                foreach (var view in impacts)
                    Assert.AreEqual(expectedResisted,view.sprite.rect.y < 32f,
                        "damage="+damage+", applied="+applied.Length+", rejected="+rejected.Length);
            }
            Check(0,new string[0],new[]{"Frozen"},true);
            Check(3,new string[0],new[]{"Frozen"},false);
            Check(0,new[]{"Chilled"},new[]{"Frozen"},false);
            Check(0,new string[0],new string[0],false);
        }
        [Test] public void ImpactFlash_ZeroKeepsColorSteadyAndHigherStrengthAddsAnOpeningAccent()
        {
            Reveal();
            float savedFlash = SpellFxSettings.FlashIntensity;
            var cell = new Point(7,5);
            var definition = SpellFxCatalog.Find("Cryomancy_Frostbind");
            var sequence = new SpellFxSequence(definition.ID,_zone,null,new Point(5,5),
                targets:new[]{new SpellFxTargetResult("victim",cell,cell,damage:3)});
            Color ImpactColor()
            {
                var impacts = _root.GetComponentsInChildren<SpriteRenderer>()
                    .Where(v=>v.enabled && v.sprite.texture.name.EndsWith("_impact")).ToArray();
                Assert.AreEqual(9,impacts.Length);
                foreach (var impact in impacts) Assert.AreEqual(impacts[0].color,impact.color);
                return impacts[0].color;
            }
            try
            {
                SpellFxSettings.FlashIntensity = 0f;
                _renderer.Play(sequence,definition);
                _renderer.Update(definition.CastDuration + definition.ChargeDuration + .001f);
                Color zeroOpening = ImpactColor();
                _renderer.Update(definition.ImpactDuration / 6f + .005f);
                Assert.AreEqual(Color.white,zeroOpening);
                Assert.AreEqual(zeroOpening,ImpactColor(),"Zero flash must not modulate impact brightness across frames.");
                _renderer.ClearAll();
                SpellFxSettings.FlashIntensity = 1f;
                _renderer.Play(sequence,definition);
                _renderer.Update(definition.CastDuration + definition.ChargeDuration + .001f);
                Color accent = ImpactColor();
                Assert.Greater(accent.r,zeroOpening.r);
                Assert.LessOrEqual(accent.r,1.15f,"The optional accent remains restrained.");
                Assert.AreEqual(1f,accent.a);
                _renderer.Update(definition.ImpactDuration / 6f + .005f);
                Assert.AreEqual(Color.white,ImpactColor(),"The accent must end after the opening frame.");
            }
            finally { SpellFxSettings.FlashIntensity = savedFlash; }
        }
        [Test] public void LargeImpact_RechecksHiddenAnchorForEveryVisibleFragment()
        {
            Reveal(); var cell = new Point(7,5);
            var definition=SpellFxCatalog.Find("Cryomancy_Frostbind");
            var sequence=new SpellFxSequence(definition.ID,_zone,null,new Point(5,5),
                targets:new[]{new SpellFxTargetResult("victim",cell,cell,damage:3)});
            int ImpactCount() => _root.GetComponentsInChildren<SpriteRenderer>()
                .Count(v=>v.enabled && v.sprite.texture.name.EndsWith("_impact"));
            _renderer.Play(sequence,definition);
            _renderer.Update(definition.CastDuration + definition.ChargeDuration + .001f);
            Assert.AreEqual(9,ImpactCount());
            _zone.GetCell(cell.X,cell.Y).IsVisible=false;
            _renderer.Update(0f);
            Assert.AreEqual(0,ImpactCount(),"Visible neighbors must not retain fragments from an anchor that became hidden.");
            _zone.GetCell(cell.X,cell.Y).IsVisible=true;
            _renderer.Update(0f);
            Assert.AreEqual(9,ImpactCount(),"Revealing the live anchor restores the remaining animation.");
        }
        [TestCase("healing","Hitpoints","binding_detail",SpellFxPrimitive.Wave)]
        [TestCase("cleansing","Acidic","binding_detail",SpellFxPrimitive.Charge)]
        [TestCase("cleansing","Charred","binding_detail",SpellFxPrimitive.Charge)]
        [TestCase("consumed-status","Burning","fire_detail",SpellFxPrimitive.Wave)]
        public void NonDamageAftermath_UsesResolvedOutcomeAndSkipsZeroAmounts(
            string kind,string value,string sheetName,SpellFxPrimitive primitive)
        {
            Reveal(); var cell = new Point(7,5);
            var definition=SpellFxCatalog.Find("Pyromancy_Kindle");
            SpellFxSequence Outcome(int amount) => new SpellFxSequence(definition.ID,_zone,null,new Point(5,5),
                reactions:new[]{new SpellFxCellResult(cell,kind,value,amount)});
            _renderer.Play(Outcome(1),definition);
            _renderer.Update(definition.CastDuration + definition.ChargeDuration + definition.ImpactDuration + .001f);
            var visible=_root.GetComponentsInChildren<SpriteRenderer>().Where(v=>v.enabled).ToArray();
            Assert.AreEqual(1,visible.Length);
            StringAssert.Contains(sheetName,visible[0].sprite.texture.name);
            Assert.AreEqual(144f-((int)primitive+1)*16f,visible[0].sprite.rect.y);
            if(kind=="consumed-status")
            {
                Assert.AreEqual(80f,visible[0].sprite.rect.x,"Consumed fuel starts with a contracting wave.");
                _renderer.Update(definition.AftermathDuration*.5f);
                visible=_root.GetComponentsInChildren<SpriteRenderer>().Where(v=>v.enabled).ToArray();
                Assert.AreEqual(1,visible.Length);
                Assert.AreEqual(32f,visible[0].sprite.rect.y,"Consumption leaves the soot flipbook, not a fresh flame.");
            }
            _renderer.ClearAll();
            _renderer.Play(Outcome(0),definition);
            _renderer.Update(definition.CastDuration + definition.ChargeDuration + definition.ImpactDuration + .001f);
            Assert.IsFalse(_root.GetComponentsInChildren<SpriteRenderer>().Any(v=>v.enabled));
        }
        [Test] public void NonDamageAftermath_CoexistsWithMaterialResultsInTheSameCell()
        {
            Reveal(); var cell=new Point(7,5); var definition=SpellFxCatalog.Find("Pyromancy_Kindle");
            var sequence=new SpellFxSequence(definition.ID,_zone,null,new Point(5,5),reactions:new[]{
                new SpellFxCellResult(cell,"healing","Hitpoints",4),
                new SpellFxCellResult(cell,"cleansing","Acidic",1),
                new SpellFxCellResult(cell,"reaction","steam",1)});
            _renderer.Play(sequence,definition);
            _renderer.Update(definition.CastDuration + definition.ChargeDuration + definition.ImpactDuration + .001f);
            var visible=_root.GetComponentsInChildren<SpriteRenderer>().Where(v=>v.enabled).ToArray();
            Assert.AreEqual(3,visible.Length);
            Assert.AreEqual(2,visible.Count(v=>v.sprite.texture.name.Contains("binding_detail")));
            Assert.AreEqual(1,visible.Count(v=>v.sprite.texture.name.Contains("water_detail")));
        }
        [Test] public void ConsumedMarks_ChangeRitePlaybackOnlyWhenActuallyConsumed()
        {
            Reveal(); var cell = new Point(7,5);
            SpellFxSequence Rite(int marks) => new SpellFxSequence("Rites_BloodletterLedger", _zone, null, new Point(5,5),
                affectedCells: new[] {cell}, targets: new[] {new SpellFxTargetResult("target",cell,cell,marksConsumed:marks)}, marksConsumed:marks);
            _renderer.Play(Rite(0),SpellFxCatalog.Find("Rites_BloodletterLedger"));
            int cold = _renderer.ActiveCount;
            _renderer.ClearAll();
            _renderer.Play(Rite(3),SpellFxCatalog.Find("Rites_BloodletterLedger"));
            Assert.AreEqual(cold + 3, _renderer.ActiveCount);
        }
    }
}
