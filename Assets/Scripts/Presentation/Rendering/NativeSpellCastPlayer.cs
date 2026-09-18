using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// One native actor's reversible cast gesture. Owns its override controller and clock;
    /// the shared controller, imported clip, and gameplay remain untouched.
    /// </summary>
    public sealed class NativeSpellCastPlayer : IDisposable
    {
        private static readonly int InteractState = Animator.StringToHash("Interact");
        private readonly Animator _animator;
        private AnimatorOverrideController _owned;
        private RuntimeAnimatorController _original;
        private AnimationClip _interact;
        private float _originalSpeed, _clipRate, _duration, _elapsed;
        private AnimatorUpdateMode _originalUpdateMode;
        private SpellFxMode _mode;
        private int _clearVersion;
        private bool _active, _disposed;

        public bool IsActive => _active && !_disposed;

        public NativeSpellCastPlayer(Animator animator) { _animator = animator; }

        /// <summary>
        /// Starts or restarts a cast using wall seconds at the current presentation speed. Invalid requests
        /// leave an existing cast alone. Every accepted recast begins at normalized time zero.
        /// </summary>
        public bool TryPlay(AnimationClip clip, float duration)
        {
            if (_disposed || _animator == null || clip == null || !FinitePositive(duration)
                || !FinitePositive(clip.length) || SpellFxSettings.Mode == SpellFxMode.Off) return false;
            // The cast hook already divided its authored duration by the initial settings
            // speed. Recover that base duration once so the animator does not double-scale it.
            float nativeDuration = duration * SpellFxSettings.AnimationSpeed;
            if (!FinitePositive(nativeDuration)) return false;
            float clipRate = clip.length / nativeDuration;
            if (!FinitePositive(clipRate * SpellFxSettings.AnimationSpeed)) return false;

            if (_active && (_clearVersion != AsciiFxBus.ClearVersion || _mode != SpellFxSettings.Mode
                || _animator.runtimeAnimatorController != _owned)) Clear();
            if (!_active)
            {
                var original = _animator.runtimeAnimatorController;
                if (original == null) return false;
                if (_owned == null || _original != original)
                {
                    // Clone an existing override as well: its other borrowed substitutions must survive.
                    var candidate = original is AnimatorOverrideController borrowed
                        ? Object.Instantiate(borrowed) : new AnimatorOverrideController(original);
                    candidate.name = "Native spell cast (owned)";
                    candidate.hideFlags = HideFlags.HideAndDontSave;
                    var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(candidate.overridesCount);
                    candidate.GetOverrides(overrides);
                    AnimationClip interact = null;
                    for (int i = 0; i < overrides.Count; i++)
                        if (overrides[i].Key != null && overrides[i].Key.name == "Interact")
                        { interact = overrides[i].Key; break; }
                    if (interact == null) { DestroyOwned(candidate); return false; }
                    if (_owned != null) DestroyOwned(_owned);
                    _original = original;
                    _owned = candidate;
                    _interact = interact;
                }
                // A completed cast can reuse its controller, but the borrowed actor state
                // may have changed while idle and must be saved anew for this cast.
                _originalSpeed = _animator.speed;
                _originalUpdateMode = _animator.updateMode;
            }

            _owned[_interact] = clip;
            _animator.runtimeAnimatorController = _owned;
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            _clipRate = clipRate;
            _duration = nativeDuration;
            _elapsed = 0f;
            _clearVersion = AsciiFxBus.ClearVersion;
            _mode = SpellFxSettings.Mode;
            _active = true;
            _animator.speed = _clipRate * SpellFxSettings.AnimationSpeed;
            if (Application.isPlaying && _animator.isActiveAndEnabled)
                _animator.Play(InteractState, 0, 0f);
            return true;
        }

        /// <summary>Advances by unscaled wall seconds, applying current presentation speed to both pose and completion.</summary>
        public void Tick(float wallDelta)
        {
            if (!_active) return;
            if (_animator == null || _clearVersion != AsciiFxBus.ClearVersion || _mode != SpellFxSettings.Mode
                || SpellFxSettings.Mode == SpellFxMode.Off || _animator.runtimeAnimatorController != _owned)
            { Clear(); return; }
            float speed = SpellFxSettings.AnimationSpeed;
            _animator.speed = _clipRate * speed;
            _elapsed += WorldFxPlayback.SanitizeDelta(wallDelta) * speed;
            if (_elapsed >= _duration) Clear();
        }

        /// <summary>Restores borrowed animator state, retaining one private override for reuse until Dispose. Safe to repeat.</summary>
        public void Clear()
        {
            if (_active && _owned != null)
            {
                // A different controller means another presentation owner has already taken over.
                if (_animator != null && _animator.runtimeAnimatorController == _owned)
                {
                    _animator.runtimeAnimatorController = _original;
                    _animator.speed = _originalSpeed;
                    _animator.updateMode = _originalUpdateMode;
                }
            }
            _active = false; _elapsed = 0f;
        }

        public void Dispose()
        {
            if (_disposed) return;
            Clear();
            if (_owned != null) DestroyOwned(_owned);
            _owned = null; _original = null; _interact = null;
            _disposed = true;
        }

        private static bool FinitePositive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
        private static void DestroyOwned(Object value)
        { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
