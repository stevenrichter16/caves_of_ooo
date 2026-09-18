using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// The single owner of both presentation queues. Simulation has already resolved;
    /// routing, clocks and cancellation never invoke gameplay commands.
    /// </summary>
    public sealed class WorldFxCoordinator : IDisposable
    {
        private readonly AsciiFxRenderer _ascii;
        private readonly SpriteSpellFxRenderer _sprites;
        private readonly NativeSpellFxRenderer _native;
        public EmberSpitAudioPlayer EmberAudio { get; }
        public StarterSpellAudioPlayer StarterAudio { get; }
        private NativeZone3DRenderSurface _nativeSurface;
        private readonly List<WorldFxPlayback> _playbacks = new List<WorldFxPlayback>();
        private readonly List<WorldFxPlayback> _nativePlaybacks = new List<WorldFxPlayback>();
        private Zone _zone;
        private bool _visible = true, _spriteMode = true, _disposed;
        private SpellFxMode _mode = SpellFxMode.Full;
        private float _blockingWallTime;
        private int _clearVersion;
        private int _nativeCancellationVersion;
        public WorldFxPlayback LastPlayback { get; private set; }
        public Action<float, float> CameraAccent { get; set; }
        public int ActiveSequenceCount => _playbacks.Count;
        public SpriteSpellFxRenderer SpriteRenderer => _sprites;
        public NativeSpellFxRenderer NativeRenderer => _native;
        public bool HasBlockingFx => !_disposed && _visible && _clearVersion == AsciiFxBus.ClearVersion && SpellFxSettings.Mode != SpellFxMode.Off &&
            (AsciiFxBus.HasPendingBlocking || SpellFxBus.HasPendingBlocking ||
             _ascii.HasBlockingFx || _sprites.HasBlockingFx || _native.HasBlockingFx || HasBlockingPlayback());

        public WorldFxCoordinator(AsciiFxRenderer ascii, Transform parent = null, NativeSpellFxLibrary nativeLibrary = null)
        {
            _ascii = ascii ?? throw new ArgumentNullException(nameof(ascii));
            _ascii.CoordinatorOwned = true;
            _sprites = new SpriteSpellFxRenderer(parent);
            _native = new NativeSpellFxRenderer(nativeLibrary);
            EmberAudio = new EmberSpitAudioPlayer(parent);
            StarterAudio = new StarterSpellAudioPlayer(parent);
            _clearVersion = AsciiFxBus.ClearVersion;
        }

        /// <summary>The presenter supplies its current borrowed native surface. A
        /// transition cancels the old queue/clock; repeated binding is a no-op.</summary>
        public void SetNativeSurface(NativeZone3DRenderSurface surface)
        {
            if (_disposed || ReferenceEquals(_nativeSurface, surface)) return;
            CancelAll();
            _nativeSurface = surface;
            _native.SetSurface(surface);
            PrepareNative();
        }

        public void SetZone(Zone zone)
        {
            CancelAll();
            _zone = zone;
            _ascii.SetZone(zone);
            _sprites.SetZone(zone);
            _native.SetZone(zone);
            EmberAudio.SetZone(zone);
            StarterAudio.SetZone(zone);
            RebuildAuras();
        }

        public void Update(float wallDelta, bool spritesEnabled = true, bool presentationVisible = true)
        {
            if (_disposed) return;
            wallDelta = WorldFxPlayback.SanitizeDelta(wallDelta);
            if (_clearVersion != AsciiFxBus.ClearVersion) { CancelAll(); RebuildAuras(); }
            if (_visible != presentationVisible || _spriteMode != spritesEnabled || _mode != SpellFxSettings.Mode)
            {
                CancelAll();
                _visible = presentationVisible;
                _spriteMode = spritesEnabled;
                _mode = SpellFxSettings.Mode;
                if (_visible) RebuildAuras();
            }
            _sprites.SetPresentationVisible(_visible && _spriteMode);
            PrepareNative();
            if (!_visible) { DiscardQueues(); _clearVersion = AsciiFxBus.ClearVersion; return; }

            float playbackDelta = wallDelta * SpellFxSettings.AnimationSpeed;
            float nativeDelta = _native.RecoverContactDelta(wallDelta, playbackDelta);
            bool recoveredContact = nativeDelta < playbackDelta;
            bool timedOut = false;
            for (int i = _playbacks.Count - 1; i >= 0; i--)
            {
                var playback = _playbacks[i];
                // Only a recovered native interval uses the shorter presentation
                // step. Raw wall time still drives every handle's hard deadline.
                playback.Tick(wallDelta, recoveredContact && _nativePlaybacks.Contains(playback) ? nativeDelta : playbackDelta);
                timedOut |= playback.State == WorldFxPlaybackState.TimedOut;
                if (playback.IsFinished) { _nativePlaybacks.Remove(playback); _playbacks.RemoveAt(i); }
            }
            // A new request was emitted during this frame. Do not charge it for a
            // preceding frame hitch (or the editor's stale delta after leaving Play).
            // Pending work still blocks input immediately and is accepted below.
            bool alreadyPlaying = _ascii.HasBlockingFx || _sprites.HasBlockingFx || _native.HasBlockingFx || HasBlockingPlayback();
            _blockingWallTime = alreadyPlaying ? _blockingWallTime + wallDelta : 0f;
            if (timedOut || _blockingWallTime >= WorldFxPlayback.HardTimeoutSeconds)
            {
                CancelAll();
                RebuildAuras();
                recoveredContact = false;
                nativeDelta = playbackDelta;
            }

            // Advance already-playing instances before accepting this frame's requests.
            _ascii.Update(playbackDelta);
            _sprites.Update(playbackDelta);
            _native.Update(nativeDelta);
            if (recoveredContact && _native.ActiveCount > 0 && Diag.IsChannelEnabled("effect"))
                Diag.Record("effect", "NativeSpellHitchRecovered", payload: new
                {
                    wallSeconds = wallDelta, requestedPlaybackSeconds = playbackDelta,
                    appliedPlaybackSeconds = nativeDelta, backend = "native",
                    reason = "unseen-contact-crossed"
                });
            ReleaseCancelledNativePlaybacks();
            EmberAudio.Update(wallDelta, playbackDelta, nativeDelta);
            StarterAudio.Update(wallDelta, playbackDelta, nativeDelta);
            RefreshAudioMix();
            // Empty queues are common: avoid allocating an empty batch every frame.
            if (AsciiFxBus.PendingCount > 0)
            {
                var legacy = AsciiFxBus.Drain();
                for (int i = 0; i < legacy.Count; i++)
                {
                    var request = legacy[i];
                    try
                    {
                        if (request != null && (_mode != SpellFxMode.Off || IsStateOrReadout(request)))
                            _ascii.AcceptRequest(request);
                    }
                    finally { AsciiFxBus.Release(request); }
                }
            }
            if (SpellFxBus.PendingCount > 0)
            {
                var spells = SpellFxBus.Drain();
                for (int i = 0; i < spells.Count; i++) Play(spells[i]);
            }
            _ascii.Update(0f);
            _sprites.Update(0f);
            _native.Update(0f);
            ReleaseCancelledNativePlaybacks();
        }

        private void PrepareNative()
        {
            _ascii.CompactAuras = _visible && _spriteMode && _nativeSurface != null && _nativeSurface.IsVisible;
            if (!_visible || !_spriteMode || SpellFxSettings.Mode == SpellFxMode.Off) return;
            _native.Prepare();
            ReleaseCancelledNativePlaybacks();
        }

        public WorldFxPlayback Play(SpellFxSequence sequence)
        {
            var playback = new WorldFxPlayback(sequence != null && sequence.BlocksTurnAdvance);
            LastPlayback = playback;
            if (_disposed || sequence == null || sequence.Zone != _zone || !_visible ||
                SpellFxSettings.Mode == SpellFxMode.Off || !HasVisibleCell(sequence))
            {
                playback.Cancel();
                return playback;
            }
            try
            {
                var definition = SpellFxCatalog.GetOrFallback(sequence.SpellId);
                float duration = _spriteMode ? _native.Play(sequence) : 0f;
                ReleaseCancelledNativePlaybacks();
                var nativeEntry = duration > 0f ? _native.LastEntry : null;
                float contact = _native.LastContactSeconds;
                Point contactCell = nativeEntry != null ? _native.LastContactCell :
                    sequence.Path.Count > 0 ? sequence.Path[sequence.Path.Count-1] : new Point(-1,-1);
                if (duration <= 0f && _spriteMode)
                { duration = _sprites.Play(sequence, definition); contact = _sprites.LastContactSeconds; }
                if (duration <= 0f) duration = PlayAscii(sequence, definition, out contact);
                var facing = sequence.Path.Count > 0 ? sequence.Path[sequence.Path.Count - 1] :
                    sequence.AffectedCells.Count > 0 ? sequence.AffectedCells[0] : sequence.Source;
                EntityVisualHooks.EmitCast(sequence.Caster, sequence.Zone, sequence.SpellId,
                    sequence.Source.X, sequence.Source.Y, facing.X, facing.Y,
                    (nativeEntry != null ? nativeEntry.CastDuration : Mathf.Min(.24f, duration)) / SpellFxSettings.AnimationSpeed);
                playback.Start(duration);
                if (duration > 0 && sequence.SpellId == "Pyromancy_EmberSpit")
                    EmberAudio.Play(sequence, contact, contactCell, nativeEntry != null);
                else if (duration > 0)
                    StarterAudio.Play(sequence, contact, nativeEntry != null);
                RefreshAudioMix();
                if (!playback.IsFinished)
                {
                    _playbacks.Add(playback);
                    if (nativeEntry != null) _nativePlaybacks.Add(playback);
                }
                if (sequence.Intensity > 1f && Visible(sequence.Source))
                    CameraAccent?.Invoke(Mathf.Min(2f, sequence.Intensity - 1f) / 16f, .12f);
            }
            catch (Exception exception)
            {
                // Presentation failure must release a turn even when an asset is malformed.
                playback.Cancel();
                Debug.LogWarning("[SpellFX] Cancelled presentation: " + exception.Message);
            }
            return playback;
        }

        private float PlayAscii(SpellFxSequence sequence, SpellFxDefinition definition, out float contact)
        {
            AsciiFxTheme theme = definition.Theme;
            string color = theme == AsciiFxTheme.Fire ? "&R" : theme == AsciiFxTheme.Ice ? "&C" :
                theme == AsciiFxTheme.Lightning ? "&Y" : theme == AsciiFxTheme.Water ? "&B" :
                theme == AsciiFxTheme.Poison ? "&G" : "&M";
            char glyph = theme == AsciiFxTheme.Fire ? '^' : theme == AsciiFxTheme.Ice ? '*' :
                theme == AsciiFxTheme.Lightning ? '/' : theme == AsciiFxTheme.Water ? '~' :
                theme == AsciiFxTheme.Poison ? ':' : '+';
            bool reduced = SpellFxSettings.Mode == SpellFxMode.Reduced;
            const float cast = .10f;
            Paint(sequence.Source, '+', color, 0f, cast);
            float travel = Mathf.Min(.4f, sequence.Path.Count * .035f);
            for (int i = 0; i < sequence.Path.Count; i++)
                Paint(sequence.Path[i], glyph, color, cast + travel * i / Math.Max(1, sequence.Path.Count), .08f);
            float impact = cast + travel;
            contact = impact;
            for (int i = 0; i < sequence.AffectedCells.Count; i++)
                Paint(sequence.AffectedCells[i], glyph, color, impact, reduced ? .1f : .2f);
            for (int i = 0; i < sequence.Targets.Count; i++)
            {
                var target = sequence.Targets[i];
                if (target == null) continue;
                bool resisted = target.Resisted || (target.Damage == 0 && target.AppliedEffects.Count == 0
                    && target.RejectedEffects.Count > 0);
                Paint(target.Cell, resisted ? '#' : target.Died ? '%' : glyph, color, impact, .2f);
                if (target.MarksConsumed > 0)
                    Paint(target.Cell, (char)('0' + Math.Min(9, target.MarksConsumed)), "&W", impact + .2f, .12f);
            }
            if (!reduced)
                foreach (var outcome in sequence.Reactions)
                {
                    if (outcome == null || outcome.Amount <= 0) continue;
                    if (outcome.Kind == "healing") Paint(outcome.Cell, '+', "&G", impact + .2f, .12f);
                    else if (outcome.Kind == "cleansing") Paint(outcome.Cell, '+', "&C", impact + .2f, .12f);
                    else if (outcome.Kind == "consumed-status") Paint(outcome.Cell, '-', color, impact + .2f, .12f);
                }
            return impact + .32f;
        }

        private void Paint(Point cell, char glyph, string color, float delay, float duration)
        {
            _ascii.AcceptRequest(new AsciiFxRequest { Type = AsciiFxRequestType.Particle, Zone = _zone,
                X = cell.X, Y = cell.Y, Glyph = glyph, ColorString = color, Lifetime = duration, Delay = delay });
        }

        private bool HasVisibleCell(SpellFxSequence sequence)
        {
            if (Visible(sequence.Source)) return true;
            foreach (var p in sequence.Path) if (Visible(p)) return true;
            foreach (var p in sequence.AffectedCells) if (Visible(p)) return true;
            foreach (var t in sequence.Targets) if (t != null && Visible(t.Cell)) return true;
            return false;
        }
        private bool Visible(Point p)
        {
            var cell = _zone?.GetCell(p.X, p.Y);
            return cell != null && cell.Explored && cell.IsVisible;
        }
        private bool HasBlockingPlayback()
        {
            foreach (var playback in _playbacks)
                if (playback.BlocksTurnAdvance && !playback.IsFinished) return true;
            return false;
        }
        private void ReleaseCancelledNativePlaybacks()
        {
            if (_nativeCancellationVersion == _native.CancellationVersion) return;
            _nativeCancellationVersion = _native.CancellationVersion;
            EmberAudio.CancelNative();
            StarterAudio.CancelNative();
            RefreshAudioMix();
            // Native surface/hierarchy loss cancels its handles immediately. Keep
            // unrelated sprite/ASCII work and newly queued actions intact.
            for (int i = 0; i < _nativePlaybacks.Count; i++)
            {
                _nativePlaybacks[i].Cancel();
                _playbacks.Remove(_nativePlaybacks[i]);
            }
            _nativePlaybacks.Clear();
        }
        private static bool IsStateOrReadout(AsciiFxRequest r) => r.Type == AsciiFxRequestType.AuraStart ||
            r.Type == AsciiFxRequestType.AuraStop || (r.Type == AsciiFxRequestType.Particle && char.IsDigit(r.Glyph));

        public void CancelAll()
        {
            CameraAccent?.Invoke(0f, 0f);
            foreach (var playback in _playbacks) playback.Cancel();
            _playbacks.Clear();
            _nativePlaybacks.Clear();
            _ascii.ClearAll();
            _sprites.ClearAll();
            _native.ClearAll();
            EmberAudio.ClearAll();
            StarterAudio.ClearAll();
            RefreshAudioMix();
            _nativeCancellationVersion = _native.CancellationVersion;
            _blockingWallTime = 0f;
            DiscardQueues();
            _clearVersion = AsciiFxBus.ClearVersion;
        }
        private static void DiscardQueues() { AsciiFxBus.Clear(); SpellFxBus.Clear(); }
        private void RefreshAudioMix()
        {
            int voices = EmberAudio.ActiveVoices + StarterAudio.ActiveVoices;
            EmberAudio.SetMixVoiceCount(voices);
            StarterAudio.SetMixVoiceCount(voices);
        }
        private void RebuildAuras()
        {
            if (_zone == null) return;
            foreach (var entity in _zone.GetAllEntities())
            {
                var well = entity.GetPart<WellSitePart>();
                var oven = entity.GetPart<OvenSitePart>();
                var lantern = entity.GetPart<LanternSitePart>();
                if (well != null)
                {
                    var site = SettlementManager.Current?.GetSite(well.SettlementId, well.SiteId);
                    if (site != null) well.StartAuraForStage(site.Stage, _zone);
                }
                if (oven != null)
                {
                    var site = SettlementManager.Current?.GetSite(oven.SettlementId, oven.SiteId);
                    if (site != null) oven.StartAuraForStage(site.Stage, _zone);
                }
                if (lantern != null)
                {
                    var site = SettlementManager.Current?.GetSite(lantern.SettlementId, lantern.SiteId);
                    if (site != null) lantern.StartAuraForStage(site.Stage, _zone);
                }
                var effects = entity.GetPart<StatusEffectsPart>()?.GetAllEffects();
                if (effects == null) continue;
                foreach (var effect in effects)
                    if (effect is IAuraProvider aura)
                        _ascii.AcceptRequest(new AsciiFxRequest { Type = AsciiFxRequestType.AuraStart,
                            Zone = _zone, Anchor = entity, Theme = aura.GetAuraTheme() });
            }
        }
        public void Dispose()
        {
            if (_disposed) return;
            CancelAll();
            _sprites.Dispose();
            _native.Dispose();
            EmberAudio.Dispose();
            StarterAudio.Dispose();
            _ascii.CompactAuras = false;
            _disposed = true;
        }

        /// <summary>Compatibility adapter for standalone ASCII renderer clients/tests.</summary>
        internal static void ConsumeLegacyRequests(AsciiFxRenderer renderer)
        {
            if (AsciiFxBus.PendingCount == 0) return;
            var requests = AsciiFxBus.Drain();
            foreach (var request in requests)
                try { renderer.AcceptRequest(request); }
                finally { AsciiFxBus.Release(request); }
        }
    }
}
