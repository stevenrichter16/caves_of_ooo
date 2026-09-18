using System;

namespace CavesOfOoo.Rendering
{
    public enum WorldFxPlaybackState { Queued, Playing, Completed, Cancelled, TimedOut }

    /// <summary>Bounded presentation lifetime. Elapsed wall time includes delayed work.</summary>
    public sealed class WorldFxPlayback
    {
        public const float HardTimeoutSeconds = 5f;
        public WorldFxPlaybackState State { get; private set; } = WorldFxPlaybackState.Queued;
        public bool BlocksTurnAdvance { get; }
        public bool IsFinished => State == WorldFxPlaybackState.Completed || State == WorldFxPlaybackState.Cancelled
            || State == WorldFxPlaybackState.TimedOut;
        private float _elapsed, _played, _duration;
        public WorldFxPlayback(bool blocksTurnAdvance) { BlocksTurnAdvance = blocksTurnAdvance; }
        public void Start(float duration)
        {
            if (IsFinished) return;
            _duration = float.IsNaN(duration) || float.IsInfinity(duration) ? HardTimeoutSeconds : Math.Max(0f, duration);
            State = _duration > 0 ? WorldFxPlaybackState.Playing : WorldFxPlaybackState.Completed;
        }
        public void Tick(float wallDelta, float playbackDelta)
        {
            if (IsFinished) return;
            _elapsed += SanitizeDelta(wallDelta);
            _played += SanitizeDelta(playbackDelta);
            if (_elapsed >= HardTimeoutSeconds) State = WorldFxPlaybackState.TimedOut;
            else if (State == WorldFxPlaybackState.Playing && _played >= _duration) State = WorldFxPlaybackState.Completed;
        }
        public void Cancel() { if (!IsFinished) State = WorldFxPlaybackState.Cancelled; }
        public static float SanitizeDelta(float delta)
            => float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0f ? 0f : delta;
    }
}
