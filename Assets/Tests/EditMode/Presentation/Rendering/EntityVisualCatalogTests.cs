using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class EntityVisualCatalogTests
    {
        private static Entity Actor(string blueprint, string glyph, string visualID = "")
        {
            var entity = new Entity { ID = "test-" + blueprint, BlueprintName = blueprint };
            entity.AddPart(new RenderPart
            {
                RenderString = glyph,
                VisualID = visualID,
                RenderLayer = 20,
            });
            return entity;
        }

        [Test]
        public void ShippedCatalog_IsValidAndCompleteForTheVerticalSlice()
        {
            Assert.AreEqual(15, EntityVisualCatalog.DefinitionCount);
            CollectionAssert.IsEmpty(EntityVisualCatalog.ValidationIssues);

            foreach (string blueprint in new[]
            {
                "Player", "Villager", "Merchant", "Elder", "Warden",
                "VillageChild", "Snapjaw", "SariSnake", "Wardline",
                "CascadeFather", "GlasspaneFrog", "YellowfootWayfarer",
                "PalimpsestEcho", "MorrowfastFarra", "MorrowfastEdden",
            })
            {
                Assert.IsTrue(EntityVisualCatalog.HasDefinitionForBlueprint(blueprint),
                    blueprint + " must have a data-driven animated visual");
            }
        }

        [Test]
        public void PlayerSheet_LoadsAsBottomPivotSixteenByTwentyFourFrames()
        {
            Assert.IsTrue(EntityVisualCatalog.TryGetAsset(Actor("Player", "@"), out var asset));
            Sprite frame = asset.GetFrame(EntityVisualState.Idle, EntityVisualFacing.South, 0);

            Assert.IsNotNull(frame);
            Assert.AreEqual(16f, frame.rect.width);
            Assert.AreEqual(24f, frame.rect.height);
            Assert.AreEqual(8f, frame.pivot.x);
            Assert.AreEqual(0f, frame.pivot.y);
            Assert.AreEqual(16f, frame.pixelsPerUnit);
        }

        [Test]
        public void HumanoidCastingSheets_LoadDistinctFramesWhileFaunaKeepAttackFallback()
        {
            foreach (string visualID in new[] { "actor.player", "actor.sill_villager", "actor.concord_merchant",
                "actor.sill_elder", "actor.sill_warden", "actor.sill_child", "actor.recension_echo" })
            {
                Assert.IsTrue(EntityVisualCatalog.TryGetAsset(Actor("AuthoredCaster", "@", visualID), out var asset), visualID);
                Assert.IsTrue(asset.HasCastingArt, visualID);
                Sprite cast = asset.GetFrame(EntityVisualState.Cast, EntityVisualFacing.East, 2);
                Assert.AreNotSame(asset.GetFrame(EntityVisualState.Attack, EntityVisualFacing.East, 2).texture, cast.texture);
                Assert.AreEqual(new Vector2(8, 0), cast.pivot);
                Assert.AreEqual(FilterMode.Point, cast.texture.filterMode);
            }
            Assert.IsTrue(EntityVisualCatalog.TryGetAsset(Actor("Snapjaw", "s"), out var fauna));
            Assert.IsFalse(fauna.HasCastingArt);
            Assert.AreSame(fauna.GetFrame(EntityVisualState.Attack, EntityVisualFacing.East, 2),
                fauna.GetFrame(EntityVisualState.Cast, EntityVisualFacing.East, 2));
        }

        [Test]
        public void BlueprintFallback_RejectsAReskinnedGlyph()
        {
            Assert.IsFalse(EntityVisualCatalog.TryGetDefinition(
                Actor("Villager", "g"), out _));
        }

        [Test]
        public void ExplicitVisualID_AllowsAnAuthoredReskin()
        {
            Assert.IsTrue(EntityVisualCatalog.TryGetDefinition(
                Actor("QuestDisguise", "g", "actor.sill_villager"), out var definition));
            Assert.AreEqual("actor.sill_villager", definition.ID);
        }

        [Test]
        public void UnknownExplicitVisualID_DoesNotBypassBlueprintReskinGuard()
        {
            Assert.IsFalse(EntityVisualCatalog.TryGetDefinition(
                Actor("Villager", "g", "actor.typo"), out _));
        }

        [Test]
        public void Validation_ReportsInvalidDimensionsAndDuplicateBlueprints()
        {
            const string json = @"{
              ""Definitions"": [
                { ""ID"": ""one"", ""Sheet"": ""a"", ""Blueprints"": [""B""], ""FrameWidth"": 0 },
                { ""ID"": ""two"", ""Sheet"": ""b"", ""Blueprints"": [""B""] }
              ]
            }";

            var issues = EntityVisualCatalog.ValidateJson(json);
            Assert.IsTrue(issues.Any(issue => issue.Contains("must be positive")));
            Assert.IsTrue(issues.Any(issue => issue.Contains("more than one visual definition")));
        }

        [Test]
        public void Casting_MissingAndUndersizedOptionalArtUseAttackFrames()
        {
            var definition = new EntityVisualDefinition
            {
                ID = "test", AttackFrames = 2, CastFrames = 4,
            };
            var sheet = new Texture2D(64, 384);
            var undersized = new Texture2D(16, 24);
            EntityVisualAsset asset = null;
            try
            {
                asset = new EntityVisualAsset(definition, sheet, undersized);
                Assert.IsFalse(asset.HasCastingArt);
                Assert.AreEqual(2, asset.GetFrameCount(EntityVisualState.Cast));
                Assert.AreSame(asset.GetFrame(EntityVisualState.Attack, EntityVisualFacing.North, 1),
                    asset.GetFrame(EntityVisualState.Cast, EntityVisualFacing.North, 3));
            }
            finally
            {
                DestroyAssetSprites(asset);
                Object.DestroyImmediate(sheet);
                Object.DestroyImmediate(undersized);
            }
        }

        [Test]
        public void Casting_OptionalSheetPreservesExistingStateRowsAndBottomPivot()
        {
            var definition = new EntityVisualDefinition { ID = "test", CastFrames = 3 };
            var sheet = new Texture2D(64, 384);
            var cast = new Texture2D(48, 96);
            EntityVisualAsset asset = null;
            try
            {
                asset = new EntityVisualAsset(definition, sheet, cast);
                Assert.IsTrue(asset.HasCastingArt);
                Sprite frame = asset.GetFrame(EntityVisualState.Cast, EntityVisualFacing.North, 2);
                Assert.AreSame(cast, frame.texture);
                Assert.AreEqual(new Rect(32, 0, 16, 24), frame.rect);
                Assert.AreEqual(new Vector2(8, 0), frame.pivot);
                Assert.AreSame(sheet, asset.GetFrame(EntityVisualState.Hurt, EntityVisualFacing.North, 0).texture);
                Assert.AreEqual(FilterMode.Point, cast.filterMode);
            }
            finally
            {
                DestroyAssetSprites(asset);
                Object.DestroyImmediate(sheet);
                Object.DestroyImmediate(cast);
            }
        }

        private static void DestroyAssetSprites(EntityVisualAsset asset)
        {
            if (asset == null) return;
            var sprites = new System.Collections.Generic.HashSet<Sprite>();
            foreach (EntityVisualState state in System.Enum.GetValues(typeof(EntityVisualState)))
                foreach (EntityVisualFacing facing in System.Enum.GetValues(typeof(EntityVisualFacing)))
                    for (int frame = 0; frame < asset.GetFrameCount(state); frame++)
                        sprites.Add(asset.GetFrame(state, facing, frame));
            foreach (Sprite sprite in sprites)
                Object.DestroyImmediate(sprite);
        }
    }
}
