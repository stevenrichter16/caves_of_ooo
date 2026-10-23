using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace CavesOfOoo.Rendering
{
    public partial class ZoneRenderer
    {
        [SerializeField, FormerlySerializedAs("Paused")]
        private bool _paused;
        [SerializeField, Tooltip("Opt-in rough16x24 presentation for the real opening. Earlier art is retained.")]
        private bool _flatPixelModeEnabled;
        private FlatPixelWorldProjection _flatProjection;
        private CameraFollow _flatCamera;
        private bool _flatModeWasActive;

        /// <summary>Fullscreen screens borrow the main canvas. Change grid metrics
        /// immediately, before their first paint and before restoring the world camera.</summary>
        public bool Paused
        {
            get => _paused;
            set { if (_paused == value) return; _paused = value; SyncFlatPixelMode(); }
        }

        /// <summary>Opt-in greybox request. Admission is restricted to the real
        /// reference glade; leaving it restores the previously selected art.</summary>
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
            && FlatPixelGreyboxCatalog.IsAdmitted(CurrentZone);

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
            if (active && _flatProjection == null && _tilemap.GetComponentInParent<Grid>() != null)
                _flatProjection = new FlatPixelWorldProjection(_tilemap, _flatCamera, ResetProjectedViews);
            if (active) _flatProjection?.Apply(true, Paused);
            else { _flatProjection?.Dispose(); _flatProjection = null; }
            if (_envSpriteRenderer != null) _envSpriteRenderer.FlatPixelGreyboxEnabled = active;
            _entityVisualRenderer?.SetPresentationVisible(!Paused && !active
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
