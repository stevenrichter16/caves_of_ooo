using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// The four opening actors' compact motion frames. Textures are borrowed;
    /// only the twenty bottom-anchored sprites are owned here. Zone admission,
    /// top-owner selection and fog visibility remain the renderer's responsibility.
    /// </summary>
    public sealed class FlatPixelActorMotionCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/opening-motion";
        private const int Width = 16, Height = 24;
        private readonly EntityVisualAsset[] _assets = new EntityVisualAsset[4];
        private readonly List<Sprite> _sprites = new List<Sprite>(20);
        private bool _disposed;

        public FlatPixelActorMotionCatalog()
            : this(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CastResourcePath),
                Resources.Load<Texture2D>(ResourcePath)) { }

        /// <summary>
        /// Missing motion keeps all actors static. Missing base or cast artwork
        /// disables only that group. No supplied texture settings are changed.
        /// </summary>
        public FlatPixelActorMotionCatalog(Texture2D baseAtlas, Texture2D castAtlas, Texture2D motionAtlas)
        {
            if (!ValidAtlas(motionAtlas, 16)) return;
            if (ValidAtlas(baseAtlas, 10))
                _assets[0] = BuildAsset("Player", 0, baseAtlas, 0, motionAtlas);
            if (!ValidAtlas(castAtlas, 4)) return;
            _assets[1] = BuildAsset("PetDog", 1, castAtlas, 1, motionAtlas);
            _assets[2] = BuildAsset("MarlbackScrabbler", 2, castAtlas, 2, motionAtlas);
            _assets[3] = BuildAsset("MarlbackGleaner", 3, castAtlas, 3, motionAtlas);
        }

        /// <summary>
        /// Returns an existing asset without allocation. Owner eligibility is
        /// checked on every call, including after a species asset was cached.
        /// </summary>
        public bool TryGetAsset(Entity owner, out EntityVisualAsset asset)
        {
            asset = null;
            if (_disposed) return false;
            int index = PortableActorIndex(owner);
            if (index < 0) return false;
            asset = _assets[index];
            return asset != null;
        }

        /// <summary>The existing four semantic actor identities, independent of
        /// atlas availability. World/portrait callers still own current-graph,
        /// custody, cell and disclosure checks; this grants no such authority.</summary>
        public static bool IsPortableActor(Entity owner) => PortableActorIndex(owner) >= 0;

        private static int PortableActorIndex(Entity owner)
        {
            if (owner == null) return -1;
            var render = owner.GetPart<RenderPart>();
            if (render == null || render.ParentEntity != owner || !render.Visible) return -1;
            if (owner.HasTag("Player")) return 0;
            if (!owner.HasTag("Creature")) return -1;
            switch (owner.BlueprintName)
            {
                case "PetDog": return 1;
                case "MarlbackScrabbler": return 2;
                case "MarlbackGleaner": return 3;
                default: return -1;
            }
        }

        private static bool ValidAtlas(Texture2D atlas, int frames)
            => atlas != null && atlas.width == Width * frames && atlas.height == Height;

        private EntityVisualAsset BuildAsset(string blueprint, int actor, Texture2D idleAtlas, int idleFrame,
            Texture2D motionAtlas)
        {
            var definition = new EntityVisualDefinition
            {
                ID = "live16x24.motion." + blueprint,
                Blueprints = new[] { blueprint },
                FrameWidth = Width, FrameHeight = Height, PixelsPerUnit = Width,
                PivotX = .5f, PivotY = 0,
                IdleFrames = 1, WalkFrames = 2, AttackFrames = 2, HurtFrames = 2, CastFrames = 0,
                MoveDuration = .10f, AttackDuration = .16f, HurtDuration = .14f,
            };
            return new EntityVisualAsset(definition,
                Frame(idleAtlas, idleFrame, blueprint + " idle"),
                Frame(motionAtlas, actor * 4, blueprint + " walk A"),
                Frame(motionAtlas, actor * 4 + 1, blueprint + " walk B"),
                Frame(motionAtlas, actor * 4 + 2, blueprint + " attack"),
                Frame(motionAtlas, actor * 4 + 3, blueprint + " hurt"));
        }

        private Sprite Frame(Texture2D atlas, int frame, string name)
        {
            var sprite = Sprite.Create(atlas, new Rect(frame * Width, 0, Width, Height),
                new Vector2(.5f, 0), Width, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 " + name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            _sprites.Add(sprite);
            return sprite;
        }

        /// <summary>Release all renderer views before disposing their sprites.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var sprite in _sprites)
            {
                if (sprite == null) continue;
                if (Application.isPlaying) Object.Destroy(sprite);
                else Object.DestroyImmediate(sprite);
            }
            _sprites.Clear();
            Array.Clear(_assets, 0, _assets.Length);
        }
    }
}
