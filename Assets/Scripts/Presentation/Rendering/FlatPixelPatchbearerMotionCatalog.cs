using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Local Patchbearer poses with the approved full/empty idle pair.
    /// Textures are borrowed; the ten bottom-anchored sprites belong to this catalog.</summary>
    public sealed class FlatPixelPatchbearerMotionCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/north-patchbearer-motion";
        private readonly EntityVisualAsset[] _assets = new EntityVisualAsset[2];
        private readonly List<Sprite> _sprites = new List<Sprite>(10);
        private readonly Texture2D _works, _motion;
        private bool _disposed;

        public FlatPixelPatchbearerMotionCatalog()
            : this(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.WorksResourcePath),
                Resources.Load<Texture2D>(ResourcePath)) { }

        public FlatPixelPatchbearerMotionCatalog(Texture2D worksAtlas, Texture2D motionAtlas)
        {
            _works = worksAtlas; _motion = motionAtlas;
            if (_works == null || _works.width != 144 || _works.height != 24
                || _motion == null || _motion.width != 128 || _motion.height != 24) return;
            for (int state = 0; state < 2; state++)
            {
                var definition = new EntityVisualDefinition
                {
                    ID = "live16x24.motion.MarlbackPatchbearer." + (state == 0 ? "full" : "empty"),
                    Blueprints = new[] { "MarlbackPatchbearer" },
                    FrameWidth = 16, FrameHeight = 24, PixelsPerUnit = 16,
                    PivotX = .5f, PivotY = 0,
                    IdleFrames = 1, WalkFrames = 2, AttackFrames = 2, HurtFrames = 2, CastFrames = 0,
                    MoveDuration = .10f, AttackDuration = .16f, HurtDuration = .14f
                };
                int offset = state * 4;
                _assets[state] = new EntityVisualAsset(definition, Frame(_works, 7 + state),
                    Frame(_motion, offset), Frame(_motion, offset + 1),
                    Frame(_motion, offset + 2), Frame(_motion, offset + 3));
            }
        }

        // Identity-only policy for the cell observer. It must not inspect hidden stock.
        internal static bool IsLocalActor(Zone zone, Entity owner)
            => owner?.BlueprintName == "MarlbackPatchbearer" && FlatPixelGreyboxCatalog.IsAdmitted(zone);

        internal bool CanRefresh(EntityVisualAsset before, EntityVisualAsset after)
            => !_disposed && before != null && after != null
                && (ReferenceEquals(before, _assets[0]) || ReferenceEquals(before, _assets[1]))
                && (ReferenceEquals(after, _assets[0]) || ReferenceEquals(after, _assets[1]));

        public bool TryGetAsset(Zone zone, Entity owner, out EntityVisualAsset asset)
        {
            asset = null;
            if (_disposed || _works == null || _motion == null || _assets[0] == null || !IsLocalActor(zone, owner)
                || !FlatPixelFallbackCatalog.IsCurrentGraph(zone) || owner.SpatialZone != zone) return false;
            var cell = zone.GetEntityCell(owner);
            // Authority and disclosure precede the state query. An undisclosed
            // owner cannot reveal its carried supplies through this adapter.
            if (cell == null || cell.ParentZone != zone || !ReferenceEquals(zone.GetCell(cell.X, cell.Y), cell)
                || !cell.IsVisible || !cell.Explored || !cell.Objects.Contains(owner)
                || !ReferenceEquals(cell.GetTopVisibleObject(), owner)
                || !FlatPixelOpeningArtCatalog.TryGetPatchbearerIdleFrame(owner, out int idle)) return false;
            asset = _assets[idle - 7];
            return asset != null;
        }

        private Sprite Frame(Texture2D texture, int frame)
        {
            var sprite = Sprite.Create(texture, new Rect(frame * 16, 0, 16, 24), new Vector2(.5f, 0),
                16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 Patchbearer " + texture.name + " " + frame;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            _sprites.Add(sprite); return sprite;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var sprite in _sprites)
            {
                if (sprite == null) continue;
                if (Application.isPlaying) Object.Destroy(sprite); else Object.DestroyImmediate(sprite);
            }
            _sprites.Clear(); Array.Clear(_assets, 0, _assets.Length);
        }
    }
}
