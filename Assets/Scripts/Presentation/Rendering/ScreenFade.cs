using UnityEngine;
using UnityEngine.UI;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Full-screen fade overlay for zone transitions.
    /// Creates a UI Canvas with a black Image that fades in/out.
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        private Image _overlay;
        private float _fadeDuration = 0.2f;
        private float _fadeTimer;
        private bool _fadingIn;  // true = fading to black
        private bool _fadingOut; // true = fading from black to clear
        private System.Action _onFadeInComplete;

        private void Awake()
        {
            // Create overlay canvas that renders on top of everything
            var canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(transform, false);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var imgObj = new GameObject("FadeOverlay");
            imgObj.transform.SetParent(canvasObj.transform, false);
            _overlay = imgObj.AddComponent<Image>();
            _overlay.color = new Color(0f, 0f, 0f, 0f);
            _overlay.raycastTarget = false;

            // Stretch to fill screen
            var rt = _overlay.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Fade to black, call onComplete when fully black, then fade back.
        /// </summary>
        public void FadeTransition(float duration, System.Action onFullyBlack)
        {
            _fadeDuration = duration;
            _fadeTimer = 0f;
            _fadingIn = true;
            _fadingOut = false;
            _onFadeInComplete = onFullyBlack;
        }

        /// <summary>
        /// Immediately set to black and fade out over duration.
        /// Use after an instant zone transition to smooth the visual.
        /// </summary>
        public void FadeFromBlack(float duration = 0.3f)
        {
            _fadeDuration = duration;
            _fadeTimer = 0f;
            _fadingIn = false;
            _fadingOut = true;
            _onFadeInComplete = null;
            _overlay.color = new Color(0f, 0f, 0f, 1f);
        }

        private void Update()
        {
            if (_fadingIn)
            {
                _fadeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_fadeTimer / _fadeDuration);
                _overlay.color = new Color(0f, 0f, 0f, t);

                if (t >= 1f)
                {
                    _fadingIn = false;
                    _onFadeInComplete?.Invoke();
                    _onFadeInComplete = null;

                    // Start fading out
                    _fadeTimer = 0f;
                    _fadingOut = true;
                }
            }
            else if (_fadingOut)
            {
                _fadeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_fadeTimer / _fadeDuration);
                _overlay.color = new Color(0f, 0f, 0f, 1f - t);

                if (t >= 1f)
                {
                    _fadingOut = false;
                    _overlay.color = new Color(0f, 0f, 0f, 0f);
                }
            }
        }
    }
}
