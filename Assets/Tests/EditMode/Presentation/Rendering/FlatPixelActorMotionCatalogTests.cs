using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelActorMotionCatalogTests
    {
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private readonly List<FlatPixelActorMotionCatalog> _catalogs = new List<FlatPixelActorMotionCatalog>();
        private Texture2D _base, _cast, _motion;

        [SetUp]
        public void SetUp()
        {
            _base = Texture(160, 24);
            _cast = Texture(64, 24);
            _motion = Texture(256, 24);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var catalog in _catalogs) catalog.Dispose();
            _catalogs.Clear();
            foreach (var texture in _textures) if (texture != null) Object.DestroyImmediate(texture);
            _textures.Clear();
        }

        [TestCase("Player", 0, 0)]
        [TestCase("PetDog", 1, 1)]
        [TestCase("MarlbackScrabbler", 2, 2)]
        [TestCase("MarlbackGleaner", 3, 3)]
        public void ExactActorsUseTheirOwnIdleAndFourBottomAnchoredMotionPoses(string blueprint, int actorIndex, int idleIndex)
        {
            var catalog = Catalog(_base, _cast, _motion);
            Assert.That(catalog.TryGetAsset(Actor(blueprint), out var asset), Is.True);
            var idleTexture = actorIndex == 0 ? _base : _cast;
            foreach (EntityVisualFacing facing in Enum.GetValues(typeof(EntityVisualFacing)))
            {
                AssertFrame(asset.GetFrame(EntityVisualState.Idle, facing, 0), idleTexture, idleIndex);
                AssertFrame(asset.GetFrame(EntityVisualState.Walk, facing, 0), _motion, actorIndex * 4);
                AssertFrame(asset.GetFrame(EntityVisualState.Walk, facing, 1), _motion, actorIndex * 4 + 1);
                AssertFrame(asset.GetFrame(EntityVisualState.Attack, facing, 0), _motion, actorIndex * 4 + 2);
                AssertFrame(asset.GetFrame(EntityVisualState.Hurt, facing, 0), _motion, actorIndex * 4 + 3);
            }
            Assert.That(asset.Definition.MoveDuration, Is.EqualTo(.10f));
            Assert.That(asset.Definition.AttackDuration, Is.EqualTo(.16f));
            Assert.That(asset.Definition.HurtDuration, Is.EqualTo(.14f));
            Assert.That(asset.GetFrameCount(EntityVisualState.Idle), Is.EqualTo(1));
            Assert.That(asset.GetFrameCount(EntityVisualState.Walk), Is.EqualTo(2));
            Assert.That(asset.GetFrameCount(EntityVisualState.Attack), Is.EqualTo(2));
            Assert.That(asset.GetFrameCount(EntityVisualState.Hurt), Is.EqualTo(2));
        }

        [Test]
        public void AttackAndHurtRecoverToSharedIdleWhileUnauthoredCastingStaysIdle()
        {
            var catalog = Catalog(_base, _cast, _motion);
            Assert.That(catalog.TryGetAsset(Actor("PetDog"), out var asset), Is.True);
            var idle = asset.GetFrame(EntityVisualState.Idle, EntityVisualFacing.South, 0);
            Assert.That(asset.HasCastingArt, Is.False);
            Assert.That(asset.GetFrameCount(EntityVisualState.Cast), Is.EqualTo(1));
            foreach (EntityVisualFacing facing in Enum.GetValues(typeof(EntityVisualFacing)))
            {
                Assert.That(asset.GetFrame(EntityVisualState.Attack, facing, 1), Is.SameAs(idle));
                Assert.That(asset.GetFrame(EntityVisualState.Hurt, facing, 1), Is.SameAs(idle));
                Assert.That(asset.GetFrame(EntityVisualState.Cast, facing, 0), Is.SameAs(idle));
                Assert.That(asset.GetFrame(EntityVisualState.Cast, facing, 99), Is.SameAs(idle));
                Assert.That(asset.GetFrame(EntityVisualState.Attack, facing, 0), Is.Not.SameAs(idle));
                Assert.That(asset.GetFrame(EntityVisualState.Hurt, facing, 0), Is.Not.SameAs(idle));
                for (int frame = 0; frame < 2; frame++)
                    Assert.That(asset.GetFrame(EntityVisualState.Walk, facing, frame),
                        Is.SameAs(asset.GetFrame(EntityVisualState.Walk, EntityVisualFacing.South, frame)),
                        "This compact artwork is one pose per state, not four authored directions.");
            }
        }

        [TestCase("Villager")]
        [TestCase("Warden")]
        [TestCase("Cook")]
        [TestCase("SeedKeeper")]
        [TestCase("MarlbackCorpse")]
        [TestCase("MarlbackScrabblerElite")]
        [TestCase("petdog")]
        [TestCase("")]
        public void UnauthoredOwnersDoNotBorrowOtherActorsMotion(string blueprint)
        {
            var catalog = Catalog(_base, _cast, _motion);
            var owner = Actor(blueprint);
            owner.GetPart<RenderPart>().VisualID = "actor.player";
            Assert.That(catalog.TryGetAsset(owner, out var asset), Is.False);
            Assert.That(asset, Is.Null);
            Assert.That(catalog.TryGetAsset(Actor("Player"), out _), Is.True);
        }

        [TestCase("Player", "missing-tag")]
        [TestCase("PetDog", "missing-tag")]
        [TestCase("MarlbackScrabbler", "missing-tag")]
        [TestCase("MarlbackGleaner", "missing-tag")]
        [TestCase("Player", "hidden")]
        [TestCase("PetDog", "hidden")]
        [TestCase("Player", "foreign-render")]
        [TestCase("PetDog", "foreign-render")]
        [TestCase("Player", "no-render")]
        [TestCase("PetDog", "no-render")]
        public void EligibilityIsRecheckedEvenAfterTheSpeciesAssetIsCached(string blueprint, string mutation)
        {
            var catalog = Catalog(_base, _cast, _motion);
            var owner = Actor(blueprint);
            Assert.That(catalog.TryGetAsset(owner, out var original), Is.True);
            switch (mutation)
            {
                case "missing-tag": owner.Tags.Remove(blueprint == "Player" ? "Player" : "Creature"); break;
                case "hidden": owner.GetPart<RenderPart>().Visible = false; break;
                case "foreign-render": owner.GetPart<RenderPart>().ParentEntity = new Entity(); break;
                case "no-render": owner.RemovePart(owner.GetPart<RenderPart>()); break;
            }
            Assert.That(catalog.TryGetAsset(owner, out var rejected), Is.False);
            Assert.That(rejected, Is.Null);
            Assert.That(catalog.TryGetAsset(Actor(blueprint), out var unaffected), Is.True);
            Assert.That(unaffected, Is.SameAs(original));
        }

        [Test]
        public void PlayerRoleIsExplicitAndDoesNotRequireThePlayerBlueprintOrCreatureTag()
        {
            var catalog = Catalog(_base, _cast, _motion);
            var owner = Actor("QuestTraveler");
            owner.Tags.Remove("Creature");
            Assert.That(catalog.TryGetAsset(owner, out _), Is.False);
            owner.Tags["Player"] = "";
            Assert.That(catalog.TryGetAsset(owner, out var asset), Is.True);
            AssertFrame(asset.GetFrame(EntityVisualState.Idle, EntityVisualFacing.South, 0), _base, 0);
            Assert.That(catalog.TryGetAsset(null, out var absent), Is.False);
            Assert.That(absent, Is.Null);
        }

        [TestCase("base", 0, 0)]
        [TestCase("base", 159, 24)]
        [TestCase("base", 160, 23)]
        [TestCase("cast", 0, 0)]
        [TestCase("cast", 63, 24)]
        [TestCase("cast", 64, 25)]
        public void MissingOrMalformedIdleGroupDoesNotDisableTheOtherActors(string group, int width, int height)
        {
            var replacement = width == 0 ? null : Texture(width, height);
            var catalog = Catalog(group == "base" ? replacement : _base,
                group == "cast" ? replacement : _cast, _motion);
            Assert.That(catalog.TryGetAsset(Actor("Player"), out var player), Is.EqualTo(group != "base"));
            foreach (var blueprint in new[] { "PetDog", "MarlbackScrabbler", "MarlbackGleaner" })
                Assert.That(catalog.TryGetAsset(Actor(blueprint), out _), Is.EqualTo(group != "cast"), blueprint);
            if (group == "base") Assert.That(player, Is.Null);
        }

        [TestCase(0, 0)]
        [TestCase(255, 24)]
        [TestCase(257, 24)]
        [TestCase(256, 23)]
        [TestCase(256, 25)]
        public void MissingOrMalformedMotionLeavesAllOwnersOnStaticArt(int width, int height)
        {
            var catalog = Catalog(_base, _cast, width == 0 ? null : Texture(width, height));
            foreach (var blueprint in new[] { "Player", "PetDog", "MarlbackScrabbler", "MarlbackGleaner" })
            {
                Assert.That(catalog.TryGetAsset(Actor(blueprint), out var asset), Is.False, blueprint);
                Assert.That(asset, Is.Null);
            }
            Assert.That(Catalog(_base, _cast, _motion).TryGetAsset(Actor("Player"), out _), Is.True);
        }

        [Test]
        public void RepeatedOwnersAndFacingsReuseExactlyFourAssetsAndTwentySprites()
        {
            var catalog = Catalog(_base, _cast, _motion);
            var assets = new HashSet<EntityVisualAsset>();
            var sprites = new HashSet<Sprite>();
            for (int repeat = 0; repeat < 8; repeat++)
                foreach (var blueprint in new[] { "Player", "PetDog", "MarlbackScrabbler", "MarlbackGleaner" })
                {
                    Assert.That(catalog.TryGetAsset(Actor(blueprint), out var asset), Is.True);
                    assets.Add(asset);
                    foreach (var sprite in Sprites(asset)) sprites.Add(sprite);
                }
            Assert.That(assets.Count, Is.EqualTo(4));
            Assert.That(sprites.Count, Is.EqualTo(20));
            Assert.That(sprites.Contains(null), Is.False);
        }

        [Test]
        public void DisposalReleasesAllOwnedSpritesButPreservesBorrowedTexturesAndPeerCatalog()
        {
            var catalog = Catalog(_base, _cast, _motion);
            var peer = Catalog(_base, _cast, _motion);
            var owned = new HashSet<Sprite>();
            foreach (var blueprint in new[] { "Player", "PetDog", "MarlbackScrabbler", "MarlbackGleaner" })
            {
                Assert.That(catalog.TryGetAsset(Actor(blueprint), out var asset), Is.True);
                foreach (var sprite in Sprites(asset)) owned.Add(sprite);
            }
            Assert.That(peer.TryGetAsset(Actor("PetDog"), out var survivor), Is.True);
            catalog.Dispose(); catalog.Dispose();
            Assert.That(owned.Count, Is.EqualTo(20));
            foreach (var sprite in owned) Assert.That(sprite == null, Is.True, "Owned Sprite must be released.");
            foreach (var texture in new[] { _base, _cast, _motion }) Assert.That(texture != null, Is.True);
            foreach (var sprite in Sprites(survivor)) Assert.That(sprite != null, Is.True);
            Assert.That(catalog.TryGetAsset(Actor("Player"), out var unavailable), Is.False);
            Assert.That(unavailable, Is.Null);
        }

        [Test]
        public void BorrowedTextureSettingsAreNeverChangedBySlicingOrDisposal()
        {
            foreach (var texture in new[] { _base, _cast, _motion })
            { texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Repeat; }
            var catalog = Catalog(_base, _cast, _motion);
            Assert.That(catalog.TryGetAsset(Actor("PetDog"), out _), Is.True);
            catalog.Dispose();
            foreach (var texture in new[] { _base, _cast, _motion })
            {
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Bilinear));
                Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
            }
        }

        [Test]
        public void ShippedMotionResourceHasNativeImportSettingsAndDefaultCatalogUsesIt()
        {
            var resource = Resources.Load<Texture2D>("Sprites/Live16x24/opening-motion");
            Assert.That(resource, Is.Not.Null, "Static fallback cannot prove the motion artwork is shipped.");
            Assert.That(resource.width, Is.EqualTo(256));
            Assert.That(resource.height, Is.EqualTo(24));
            Assert.That(resource.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(resource.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(resource.mipmapCount, Is.EqualTo(1));
            var baseResource = Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath);
            var castResource = Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CastResourcePath);
            Assert.That(baseResource, Is.Not.Null);
            Assert.That(castResource, Is.Not.Null);
            var catalog = new FlatPixelActorMotionCatalog(); _catalogs.Add(catalog);
            var actors = new[] { "Player", "PetDog", "MarlbackScrabbler", "MarlbackGleaner" };
            for (int index = 0; index < actors.Length; index++)
            {
                Assert.That(catalog.TryGetAsset(Actor(actors[index]), out var asset), Is.True, actors[index]);
                AssertFrame(asset.GetFrame(EntityVisualState.Idle, EntityVisualFacing.West, 0),
                    index == 0 ? baseResource : castResource, index);
                AssertFrame(asset.GetFrame(EntityVisualState.Hurt, EntityVisualFacing.West, 0), resource, index * 4 + 3);
            }
        }

        private Texture2D Texture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _textures.Add(texture); return texture;
        }

        private FlatPixelActorMotionCatalog Catalog(Texture2D baseAtlas, Texture2D castAtlas, Texture2D motionAtlas)
        {
            var catalog = new FlatPixelActorMotionCatalog(baseAtlas, castAtlas, motionAtlas);
            _catalogs.Add(catalog); return catalog;
        }

        private static Entity Actor(string blueprint)
        {
            var owner = new Entity { ID = "motion-catalog-" + Guid.NewGuid().ToString("N"), BlueprintName = blueprint };
            owner.Tags[blueprint == "Player" ? "Player" : "Creature"] = "";
            owner.AddPart(new RenderPart { RenderString = "@", RenderLayer = 20 });
            return owner;
        }

        private static IEnumerable<Sprite> Sprites(EntityVisualAsset asset)
        {
            foreach (EntityVisualState state in Enum.GetValues(typeof(EntityVisualState)))
                foreach (EntityVisualFacing facing in Enum.GetValues(typeof(EntityVisualFacing)))
                    for (int frame = 0; frame < asset.GetFrameCount(state); frame++)
                        yield return asset.GetFrame(state, facing, frame);
        }

        private static void AssertFrame(Sprite frame, Texture2D texture, int index)
        {
            Assert.That(frame, Is.Not.Null);
            Assert.That(frame.texture, Is.SameAs(texture));
            Assert.That(frame.rect, Is.EqualTo(new Rect(index * 16, 0, 16, 24)));
            Assert.That(frame.pivot, Is.EqualTo(new Vector2(8, 0)));
            Assert.That(frame.pixelsPerUnit, Is.EqualTo(16));
        }
    }
}
