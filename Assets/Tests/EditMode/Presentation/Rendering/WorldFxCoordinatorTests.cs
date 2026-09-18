using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public class WorldFxCoordinatorTests
    {
        private WorldFxCoordinator _world;
        private AsciiFxRenderer _ascii;
        private Zone _zone;
        private GameObject _root;
        private Tilemap _tiles;
        [SetUp] public void Setup()
        {
            AsciiFxBus.Clear(); SpellFxBus.Clear();
            SpellFxSettings.Mode = SpellFxMode.Full;
            SpellFxSettings.AnimationSpeed = 1f;
            _root = new GameObject("WorldFxTest", typeof(Grid));
            var map = new GameObject("Effects", typeof(Tilemap), typeof(TilemapRenderer));
            map.transform.SetParent(_root.transform);
            _tiles = map.GetComponent<Tilemap>();
            _ascii = new AsciiFxRenderer(_tiles);
            _world = new WorldFxCoordinator(_ascii, _root.transform);
            _zone = new Zone("FxLifecycle");
            foreach (var cell in _zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
            _world.SetZone(_zone);
        }
        [TearDown] public void Teardown()
        {
            _world.Dispose(); UnityEngine.Object.DestroyImmediate(_root);
            SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.AnimationSpeed = 1f;
        }
        private SpellFxSequence Sequence(string id = "MissingArt") => new SpellFxSequence(id, _zone, null,
            new Point(4, 4), new[] { new Point(5, 4), new Point(6, 4) }, new[] { new Point(6, 4) });

        [Test] public void QueuedBlockingWork_BlocksBeforeFirstRendererTick_ThenCompletes()
        {
            SpellFxBus.Emit(Sequence());
            Assert.IsTrue(_world.HasBlockingFx);
            _world.Update(0f, false);
            // Switch mode before enqueueing; switching intentionally cancels in-flight work.
            SpellFxBus.Emit(Sequence()); _world.Update(0f, false);
            Assert.IsTrue(_world.HasBlockingFx);
            Assert.AreEqual(0, SpellFxBus.PendingCount);
            _world.Update(1f, false);
            Assert.IsFalse(_world.HasBlockingFx);
            Assert.AreEqual(WorldFxPlaybackState.Completed, _world.LastPlayback.State);
        }
        [Test] public void LegacyQueue_IsConsumedOnce_AndRequestPoolResets()
        {
            AsciiFxBus.EmitBurst(_zone, 4, 4, AsciiFxTheme.Fire, true, .2f);
            _world.Update(0f);
            Assert.AreEqual(1, _ascii.ActiveBurstCount);
            _ascii.Update(0f);
            Assert.AreEqual(1, _ascii.ActiveBurstCount);
            Assert.AreEqual(0, AsciiFxBus.PendingCount);
            _world.CancelAll();
            AsciiFxBus.EmitParticle(_zone, 4, 4, '*', "&W", .1f);
            var request = AsciiFxBus.Drain()[0];
            Assert.IsFalse(request.BlocksTurnAdvance); Assert.AreEqual(0f, request.Delay);
            Assert.IsNull(request.Path); AsciiFxBus.Release(request);
        }
        [Test] public void MissingArt_FallsBackAndRetainsExactImpactCells()
        {
            var handle = _world.Play(Sequence());
            _world.Update(.2f);
            Assert.AreEqual(WorldFxPlaybackState.Playing, handle.State);
            Assert.Greater(_ascii.ActiveParticleCount, 0);
            Assert.IsNull(_tiles.GetTile(new Vector3Int(6, Zone.Height - 1 - 5, 0)), "No invented burst neighbors");
            _world.Update(1f); Assert.IsFalse(_world.HasBlockingFx);
        }
        [Test] public void FullyHiddenSequence_IsCancelledWithoutWaiting()
        {
            foreach (var cell in _zone.Cells) cell.IsVisible = false;
            var handle = _world.Play(Sequence());
            Assert.AreEqual(WorldFxPlaybackState.Cancelled, handle.State);
            Assert.IsFalse(_world.HasBlockingFx);
        }
        [Test] public void AsciiFallback_ClipsRememberedAndUnexploredCells()
        {
            _zone.GetCell(6, 4).IsVisible = false;
            _zone.GetCell(5, 4).Explored = false;
            _world.Play(Sequence()); _world.Update(.2f);
            Assert.IsNull(_tiles.GetTile(new Vector3Int(6, Zone.Height - 1 - 4, 0)));
            Assert.IsNull(_tiles.GetTile(new Vector3Int(5, Zone.Height - 1 - 4, 0)));
        }
        [Test] public void Off_DiscardsDecorativeQueueAndReleasesWaiting()
        {
            SpellFxBus.Emit(Sequence()); SpellFxSettings.Mode = SpellFxMode.Off;
            Assert.IsFalse(_world.HasBlockingFx); _world.Update(0f);
            Assert.AreEqual(0, SpellFxBus.PendingCount); Assert.AreEqual(0, _world.ActiveSequenceCount);
        }
        private sealed class StateAura : Effect, IAuraProvider
        {
            public override string DisplayName => "State aura";
            public AsciiFxTheme GetAuraTheme() => AsciiFxTheme.Fire;
        }
        [Test] public void Off_RebuildsPersistentStatusIndicatorsFromCurrentState()
        {
            var entity = new Entity();
            var effects = new StatusEffectsPart(); entity.AddPart(effects);
            effects.RestoreEffectsForLoad(new System.Collections.Generic.List<Effect> { new StateAura() });
            _zone.AddEntity(entity, 4, 4);
            SpellFxSettings.Mode = SpellFxMode.Off;
            _world.Update(0f);
            Assert.AreEqual(1, _ascii.ActiveAuraCount);
            Assert.AreEqual(0, _ascii.ActiveParticleCount, "Off does not emit decorative aura particles");
            Assert.IsNotNull(_tiles.GetTile(new Vector3Int(4, Zone.Height - 5, 0)));
            Assert.IsFalse(_world.HasBlockingFx);
        }
        [Test] public void LoadBusReset_InvalidatesPlayingWorkBeforeTheNextRendererFrame()
        {
            var handle = _world.Play(Sequence());
            Assert.IsTrue(_world.HasBlockingFx);
            AsciiFxBus.Clear(); SpellFxBus.Clear();
            Assert.IsFalse(_world.HasBlockingFx);
            _world.Update(0f);
            Assert.AreEqual(WorldFxPlaybackState.Cancelled, handle.State);
            Assert.AreEqual(0, _ascii.ActiveParticleCount);
        }
        [Test] public void ZoneChange_CancelsActiveAndQueuedWork()
        {
            var handle = _world.Play(Sequence()); SpellFxBus.Emit(Sequence());
            _world.SetZone(new Zone("Elsewhere"));
            Assert.AreEqual(WorldFxPlaybackState.Cancelled, handle.State);
            Assert.AreEqual(0, SpellFxBus.PendingCount); Assert.IsFalse(_world.HasBlockingFx);
            Assert.AreEqual(0, _ascii.ActiveParticleCount);
        }
        [Test] public void HiddenUiAndSpriteToggle_CancelInFlightWork()
        {
            var first = _world.Play(Sequence()); _world.Update(0f, true, false);
            Assert.AreEqual(WorldFxPlaybackState.Cancelled, first.State);
            Assert.IsFalse(_world.HasBlockingFx);
            _world.Update(0f, true, true);
            var second = _world.Play(Sequence()); _world.Update(0f, false);
            Assert.AreEqual(WorldFxPlaybackState.Cancelled, second.State);
        }
        [Test] public void LegacyDelayedBlock_HasHardWallClockTimeout()
        {
            AsciiFxBus.EmitBeam(_zone, new[] { new Point(4, 4) }, 1, 0, AsciiFxTheme.Fire, 100f, true, 100f);
            _world.Update(0f); Assert.IsTrue(_world.HasBlockingFx);
            _world.Update(WorldFxPlayback.HardTimeoutSeconds + .1f);
            Assert.IsFalse(_world.HasBlockingFx); Assert.AreEqual(0, _ascii.ActiveBeamCount);
        }
        [Test] public void NewQueuedCast_DoesNotInheritThePreviousFramesHitch()
        {
            AsciiFxBus.EmitBeam(_zone, new[] { new Point(4, 4) }, 1, 0, AsciiFxTheme.Fire, .2f, true);
            _world.Update(10f);
            Assert.AreEqual(1, _ascii.ActiveBeamCount);
            Assert.IsTrue(_world.HasBlockingFx);
            _world.Update(.3f);
            Assert.IsFalse(_world.HasBlockingFx);
        }
        [Test] public void Playback_QueuedTimeoutAndCancellationAreTerminal()
        {
            var handle = new WorldFxPlayback(true); handle.Tick(5f, 0f);
            Assert.AreEqual(WorldFxPlaybackState.TimedOut, handle.State);
            handle.Start(.1f); Assert.AreEqual(WorldFxPlaybackState.TimedOut, handle.State);
            var cancelled = new WorldFxPlayback(true); cancelled.Cancel(); cancelled.Start(1f);
            Assert.AreEqual(WorldFxPlaybackState.Cancelled, cancelled.State);
        }
        [Test] public void SettingsRejectNonFiniteSpeed_AndSpeedChangesPlaybackClock()
        {
            SpellFxSettings.AnimationSpeed = float.NaN; Assert.AreEqual(1f, SpellFxSettings.AnimationSpeed);
            SpellFxSettings.AnimationSpeed = 4f;
            var handle = _world.Play(Sequence()); _world.Update(.2f);
            Assert.AreEqual(WorldFxPlaybackState.Completed, handle.State);
        }
    }
}
