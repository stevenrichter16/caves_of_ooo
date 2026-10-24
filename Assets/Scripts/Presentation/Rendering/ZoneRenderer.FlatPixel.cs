using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace CavesOfOoo.Rendering
{
    public partial class ZoneRenderer
    {
        [SerializeField, FormerlySerializedAs("Paused")]
        private bool _paused;
        [SerializeField, Tooltip("Continuous flat16x24 world presentation. Native regional art remains scoped; earlier presentation is retained.")]
        private bool _flatPixelModeEnabled;
        private FlatPixelWorldProjection _flatProjection;
        private CameraFollow _flatCamera;
        private bool _flatModeWasActive;
        private Zone _flatGraphZone;
        private bool _flatGraphCurrent;

        /// <summary>Fullscreen screens borrow the main canvas. Change grid metrics
        /// immediately, before their first paint and before restoring the world camera.</summary>
        public bool Paused
        {
            get => _paused;
            set { if (_paused == value) return; _paused = value; SyncFlatPixelMode(); }
        }

        /// <summary>Continuous flat-world request. Local native artwork does not
        /// determine projection; explicitly leaving this mode restores earlier art.</summary>
        public bool FlatPixelModeEnabled
        {
            get => _flatPixelModeEnabled;
            set
            {
                if (_flatPixelModeEnabled == value) return;
                _flatPixelModeEnabled = value;
                SyncFlatPixelMode();
                SyncVillagePresentation(!Paused && _envSpriteRenderer != null && _envSpriteRenderer.RenderingEnabled);
            }
        }
        /// <summary>Zone ownership survives fullscreen UI; only world visibility
        /// and grid metrics pause. Sprite-disable explicitly leaves this mode.</summary>
        public bool FlatPixelModeActive => _flatPixelModeEnabled
            && _envSpriteRenderer != null && _envSpriteRenderer.RenderingEnabled
            && CurrentZone != null;

        /// <summary>Handle an authorized subject using the active flat world art.
        /// A rejected subject is still handled here, so it cannot fall through to
        /// an older model illustration. Sprites and textures remain borrowed.</summary>
        public bool TryGetFlatExaminePortrait(AnnouncementDocument document, out ExaminePortrait portrait)
        {
            portrait = default;
            if (!FlatPixelModeActive) return false;
            if (!ExaminePortraitRenderer.IsCurrentPresentationSubject(document, this)) return true;
            var cell = document.SubjectCell ?? CurrentZone.GetEntityCell(document.SubjectViewer);
            if (_envSpriteRenderer.TryGetFlatSubjectSprite(CurrentZone, document.Subject, cell?.X ?? 0, cell?.Y ?? 0,
                out var sprite, out var tint) && sprite != null && sprite.texture != null)
            {
                var texture = sprite.texture;
                var rect = sprite.rect;
                portrait = new ExaminePortrait(texture,
                    new Rect(rect.x / texture.width, rect.y / texture.height,
                        rect.width / texture.width, rect.height / texture.height), tint, false);
            }
            return true;
        }

        /// <summary>Both fresh and loaded bootstraps bind after creating the camera,
        /// before their first SnapToPlayer. No global camera setting is overwritten.</summary>
        public void BindFlatPixelCamera(CameraFollow follow)
        {
            if (_flatCamera == follow && _flatProjection != null) return;
            _flatProjection?.Dispose();
            _flatProjection = null;
            _flatCamera = follow;
            SyncFlatPixelMode();
        }

        private void SyncFlatPixelMode()
        {
            if (_tilemap == null) return;
            bool active = FlatPixelModeActive;
            bool current = active && FlatPixelFallbackCatalog.IsCurrentGraph(CurrentZone);
            if (_flatGraphZone != CurrentZone || _flatGraphCurrent != current)
            {
                _flatGraphZone = CurrentZone; _flatGraphCurrent = current;
                _envSpriteRenderer?.ClearFlatPaintReceipts();
                _fullDirty = true;
            }
            if (active && _flatProjection == null && _tilemap.GetComponentInParent<Grid>() != null)
                _flatProjection = new FlatPixelWorldProjection(_tilemap, _flatCamera, ResetProjectedViews);
            if (active) _flatProjection?.Apply(true, Paused);
            else { _flatProjection?.Dispose(); _flatProjection = null; }
            if (_envSpriteRenderer != null) _envSpriteRenderer.FlatPixelGreyboxEnabled = active;
            _entityVisualRenderer?.SetFlatPixelMode(active);
            _entityVisualRenderer?.SetPresentationVisible(!Paused
                && (_envSpriteRenderer == null || _envSpriteRenderer.RenderingEnabled));
            if (_flatModeWasActive != active)
            {
                _flatModeWasActive = active;
                if (Paused) _envSpriteRenderer?.NotifyMainTilemapCleared();
                else _envSpriteRenderer?.ReleaseAllClaims();
                _animatedEnvRenderer?.ClearAll();
                _fineWaterTilemap?.ClearAllTiles();
                _fullDirty = true;
                _nativeGeometryFullDirty = true;
            }
            if (_campfireEmberRenderer != null) _campfireEmberRenderer.gameObject.SetActive(!Paused && !active);
            if (_ambientMotesRenderer != null) _ambientMotesRenderer.gameObject.SetActive(!Paused && !active);
        }

        private void ResetProjectedViews()
        {
            _entityVisualRenderer?.ClearViews();
            _worldCursorRenderer?.Clear();
            _worldFxCoordinator?.CancelAll();
            ClearWorldAffordance();
            _fullDirty = true;
        }

        private void HideLegacyForFlatMode()
        {
            _village3DPresenter?.SetPresentationVisible(false);
            _spawnRing3DPresenter?.SetPresentationVisible(false);
            _fellingScenePresenter?.SetPresentationVisible(false);
            _fellingDressingPresenter?.SetPresentationVisible(false);
            _morrowfastScenePresenter?.SetPresentationVisible(false);
            _village3DPresenter?.Bind(null, _mainCamera);
            _spawnRing3DPresenter?.Bind(null, _mainCamera);
            _fellingScenePresenter?.Bind(null);
            _fellingDressingPresenter?.Bind(null);
            _morrowfastScenePresenter?.Bind(null);
            if (_tileStateNativeVisible) { _tileStateNativeVisible = false; _fullDirty = true; }
            _worldFxCoordinator?.SetNativeSurface(null);
        }
    }
}
