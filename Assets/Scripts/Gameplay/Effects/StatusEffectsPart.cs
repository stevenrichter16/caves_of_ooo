using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part that manages all status effects on an entity.
    /// Dispatches turn/combat events to each active effect.
    /// Auto-cleans expired effects at end of turn.
    /// </summary>
    public class StatusEffectsPart : Part
    {
        public override string Name => "StatusEffects";

        private readonly List<Effect> _effects = new List<Effect>();

        public override void Initialize()
        {
            // Keep effect handling first, matching Qud's "effects before parts" behavior.
            // This ensures turn/action gating happens before AI or other gameplay parts.
            if (ParentEntity == null)
                return;

            int index = ParentEntity.Parts.IndexOf(this);
            if (index > 0)
            {
                ParentEntity.Parts.RemoveAt(index);
                ParentEntity.Parts.Insert(0, this);
            }
        }

        /// <summary>
        /// Apply an effect to this entity.
        /// Checks CanApply, handles stacking, calls OnApply.
        /// Returns true if the effect was applied (or stacked).
        /// </summary>
        public bool ApplyEffect(Effect effect, Entity source = null, Zone zone = null)
        {
            return ApplyEffectInternal(effect, source, zone, forced: false);
        }

        /// <summary>
        /// Apply an effect while bypassing CanApply checks.
        /// Still supports stacking and emits force-apply lifecycle events.
        /// </summary>
        public bool ForceApplyEffect(Effect effect, Entity source = null, Zone zone = null)
        {
            return ApplyEffectInternal(effect, source, zone, forced: true);
        }

        private bool ApplyEffectInternal(Effect effect, Entity source, Zone zone, bool forced)
        {
            if (effect == null || ParentEntity == null)
                return false;

            if (!forced && !effect.CanBeAppliedTo(ParentEntity))
                return false;

            if (!CheckBeforeApply(effect, source, forced))
                return false;

            // Check for stacking with existing effects of the same type
            Type incomingType = effect.GetType();
            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i].GetType() == incomingType)
                {
                    if (_effects[i].OnStack(effect))
                        return true; // stacking handled, don't add duplicate
                }
            }

            effect.Owner = ParentEntity;

            // Mark the effect as "applied while this owner is the active
            // turn-taker" — true only when TurnManager's CurrentActor is
            // this entity. The first OnTurnEnd will skip this effect and
            // clear the flag, so an effect applied mid-action survives
            // the apply turn rather than evaporating in the very EndTurn
            // that follows.
            //
            // Why query TurnManager.CurrentActor instead of tracking a
            // local "_isOwnerActing" flag: StatusEffectsPart is lazily
            // created by EnsureStatusEffectsPart on the FIRST effect
            // application. If a player's first-ever effect comes from a
            // trap (no prior poison/buff/etc.), the part doesn't exist
            // when ProcessUntilPlayerTurn fires BeginTakeAction — so a
            // local flag would never get set. CurrentActor is the
            // canonical "who's acting" source on the TurnManager itself
            // and is always correct regardless of part-creation order.
            //
            // Coverage:
            //   - Trap step (player stepping during own turn):
            //       CurrentActor == player == ParentEntity → JustApplied=true ✓
            //   - On-hit melee (attacker A hits defender B):
            //       CurrentActor == A, ParentEntity == B → JustApplied=false ✓
            //   - AOE spell (caster C hits enemy E):
            //       CurrentActor == C, ParentEntity == E → JustApplied=false ✓
            //   - Self tonic on own turn:
            //       CurrentActor == player == ParentEntity → JustApplied=true ✓
            //   - Standalone test or no TurnManager:
            //       Active == null → JustApplied=false (legacy behavior) ✓
            var tm = TurnManager.Active;
            effect.JustApplied = tm != null && tm.CurrentActor == ParentEntity;

            if (!effect.Apply(ParentEntity))
                return false;

            _effects.Add(effect);
            effect.Applied(ParentEntity);

            // D2.1 diag hook (Docs/D2-HOOKS-PLAN.md §4 D2.1).
            // Position mirror of D1.2's OnRemove hook in RemoveEffectAt:
            // fires AFTER `effect.Applied(ParentEntity)` (which calls
            // effect.OnApply → MessageLog.Add) and BEFORE SendApplied
            // (which fires the EffectApplied event), so the buffer
            // ordering is [user-visible message] → [diag record] →
            // [downstream listeners]. Symmetric with OnRemove which is
            // [user-visible message] → [diag record] → [SendRemoved].
            //
            // Stack branch at line ~70 returns BEFORE reaching here, so
            // re-application of an already-active effect type does not
            // double-emit (counter-checked by
            // StackingReApplication_DoesNotEmitSecondOnApplyRecord).
            if (Diag.IsChannelEnabled("effect"))
            {
                Diag.Record(
                    category: "effect",
                    kind: "OnApply",
                    target: ParentEntity,
                    actor: source,
                    payload: new
                    {
                        effect = effect.GetType().Name,
                        duration = effect.Duration,
                        justApplied = effect.JustApplied,
                        forced = forced
                    });
            }

            SendApplied(effect, source, zone, forced);
            return true;
        }

        /// <summary>
        /// Remove the first effect of type T. Calls OnRemove.
        /// Returns true if an effect was found and removed.
        /// </summary>
        public bool RemoveEffect<T>() where T : Effect
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i] is T)
                {
                    // Public RemoveEffect → caller is doing external removal
                    // (cure spell, dispel mutation, etc.). Tag the cause so the
                    // EffectRemoved event distinguishes it from save-success
                    // and duration-tick exits.
                    _effects[i].LastRemovalCause = Effect.CAUSE_EXTERNAL;
                    RemoveEffectAt(i);
                    return true;
                }
            }
            return false;
        }

        public bool RemoveEffect(Type effectType)
        {
            if (effectType == null)
                return false;

            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i].GetType() == effectType)
                {
                    _effects[i].LastRemovalCause = Effect.CAUSE_EXTERNAL;
                    RemoveEffectAt(i);
                    return true;
                }
            }
            return false;
        }

        public bool RemoveEffect(Predicate<Effect> filter)
        {
            if (filter == null)
                return false;

            for (int i = 0; i < _effects.Count; i++)
            {
                if (filter(_effects[i]))
                {
                    _effects[i].LastRemovalCause = Effect.CAUSE_EXTERNAL;
                    RemoveEffectAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Remove a specific effect instance. Calls OnRemove.
        /// </summary>
        public bool RemoveEffect(Effect effect)
        {
            int index = _effects.IndexOf(effect);
            if (index < 0)
                return false;
            effect.LastRemovalCause = Effect.CAUSE_EXTERNAL;
            RemoveEffectAt(index);
            return true;
        }

        public bool HasEffect<T>() where T : Effect
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i] is T)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// True when any active effect would block the owner from acting
        /// (AllowAction returns false). Used by InputHandler to detect
        /// the "frozen / stunned / paralyzed" state and route failed
        /// player-input moves through the turn advancer instead of
        /// ignoring them.
        /// </summary>
        public bool IsActionBlocked()
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                if (!_effects[i].AllowAction(ParentEntity))
                    return true;
            }
            return false;
        }

        public T GetEffect<T>() where T : Effect
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i] is T typed)
                    return typed;
            }
            return null;
        }

        public IReadOnlyList<Effect> GetAllEffects() => _effects;

        public int EffectCount => _effects.Count;

        public void RestoreEffectsForLoad(List<Effect> effects)
        {
            _effects.Clear();
            if (effects == null)
                return;

            for (int i = 0; i < effects.Count; i++)
            {
                Effect effect = effects[i];
                if (effect == null)
                    continue;

                effect.Owner = ParentEntity;
                _effects.Add(effect);
            }
        }

        public override void OnAfterLoad(SaveReader reader)
        {
            // Resolve the entity's current zone so IAuraProvider effects can
            // re-emit their visual auras after a save/load. BrainPart owns
            // CurrentZone post-load (LoadBrainPart line ~1452 calls FindZone);
            // entities without a brain (rare for status-effect targets) fall
            // back to the active zone from the SaveReader. Without this, a
            // poisoned/smoldering NPC would load with the effect mechanically
            // intact but no visual cue — caught by
            // SaveSystemAdversarialTests.Adv_PoisonedNpc_VisualAuraResumes_AfterSaveLoad.
            Zone zone = ParentEntity?.GetPart<BrainPart>()?.CurrentZone
                ?? reader?.ZoneManager?.ActiveZone;

            for (int i = 0; i < _effects.Count; i++)
            {
                _effects[i].Owner = ParentEntity;
                TryStartAura(_effects[i], zone);
            }
        }

        /// <summary>
        /// Remove all effects, calling OnRemove on each.
        /// </summary>
        public void RemoveAllEffects()
        {
            for (int i = _effects.Count - 1; i >= 0; i--)
                RemoveEffectAt(i);
        }

        /// <summary>
        /// Get the highest-priority render color override from active effects.
        /// Returns null if no effect overrides color.
        /// First effect with a non-null override wins.
        /// </summary>
        public string GetRenderColorOverride()
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                string color = _effects[i].GetRenderColorOverride();
                if (color != null)
                    return color;
            }
            return null;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeginTakeAction")
                return HandleBeginTakeAction(e);

            // Back-compat: some systems still drive effect start-of-turn on TakeTurn.
            if (e.ID == "TakeTurn")
            {
                if (e.GetParameter<bool>("BeginTakeActionProcessed"))
                    return true;
                return HandleBeginTakeAction(e);
            }

            // Block movement when any active effect denies action. Defense in
            // depth against paths that call MovementSystem.TryMove directly
            // (e.g. InputHandler's player-move path) without first going
            // through the TurnManager's BeginTakeAction gate.
            //
            // Without this, a frozen player could still press a direction
            // key and move — the "X is frozen and cannot act!" log from
            // HandleBeginTakeAction would appear on their skipped turn,
            // but the next input frame would slip through because
            // BeforeMove has no AllowAction consultation upstream.
            if (e.ID == "BeforeMove")
                return HandleBeforeMove(e);

            if (e.ID == "EndTurn")
            {
                HandleEndTurn(e);
                return true;
            }

            if (e.ID == "BeforeTakeDamage")
            {
                HandleBeforeTakeDamage(e);
                return true;
            }

            if (e.ID == "TakeDamage")
            {
                HandleTakeDamage(e);
                return true;
            }

            if (e.ID == "Died")
            {
                RemoveAllEffects();
                return true;
            }

            if (e.ID == "Render")
                return HandleRender(e);

            return true;
        }

        private bool HandleBeforeMove(GameEvent e)
        {
            // Movement-only blocking is via AllowMovement; full action-
            // blocking via AllowAction (its default makes AllowMovement
            // return AllowAction's result, so existing effects like
            // Stunned/Frozen continue to block movement without changes).
            // RootedEffect overrides AllowMovement => false but leaves
            // AllowAction at its true default — actor can still attack
            // / cast / use abilities, just not change cells.
            //
            // We DON'T re-log here because HandleBeginTakeAction already
            // logged on the turn gate; a second "cannot act" per skipped-
            // input key-press would spam the log.
            for (int i = 0; i < _effects.Count; i++)
            {
                if (!_effects[i].AllowMovement(ParentEntity))
                {
                    e.Handled = true;
                    return false;
                }
            }
            return true;
        }

        private bool HandleBeginTakeAction(GameEvent e)
        {
            // Fire OnTurnStart FIRST so damage-tick effects (poison, burn,
            // bleed, acidic, electrified) deal their per-turn damage BEFORE
            // the action-block check. Bodily processes don't pause for
            // mental effects — bleeding still bleeds while you're stunned.
            //
            // Pre-fix, BearTrap's Stun(1) + Bleeding pairing gave the
            // player NO visible feedback during the stunned turn (no
            // "takes X bleed damage" message), making bleeding appear
            // to never tick. Players reported "bleeding finishes the
            // same turn it gets applied" — actually it was still active
            // but ticked silently. This restores the expected roguelike
            // semantic: damage-tick effects always tick at start of turn,
            // and the action-block check only governs whether the entity
            // can take a willed action (move/attack).
            //
            // Reverse iteration so an effect removing itself or another
            // effect during OnTurnStart doesn't skip iterations.
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                if (i < _effects.Count)
                    _effects[i].OnTurnStart(ParentEntity, e);
            }

            // After damage ticks, check if any effect blocks the action
            // (stun, freeze, paralysis). If so, log the skipped-turn
            // message and return false so TurnManager skips the actor's
            // willed action. The damage ticks above have already fired.
            for (int i = 0; i < _effects.Count; i++)
            {
                if (!_effects[i].AllowAction(ParentEntity))
                {
                    string name = ParentEntity.GetDisplayName();
                    string effectName = _effects[i].DisplayName;
                    MessageLog.Add(name + " is " + effectName + " and cannot act!");
                    e.Handled = true;
                    return false;
                }
            }

            return true;
        }

        private void HandleEndTurn(GameEvent e)
        {
            // Tick each effect — but skip any effect that was JustApplied
            // during this same owner-turn cycle. Without this skip, an
            // effect added mid-action (e.g., StunnedEffect(1) from a
            // BearTrap step) would tick its OnTurnEnd in the very EndTurn
            // that wraps the move, decrementing Duration 1 → 0 and being
            // cleaned up before the next turn even starts. The flag is
            // single-shot: cleared on the first tick attempt so the
            // second EndTurn ticks normally.
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                if (i < _effects.Count)
                {
                    var effect = _effects[i];
                    if (effect.JustApplied)
                    {
                        effect.JustApplied = false;
                        continue;
                    }
                    effect.OnTurnEnd(ParentEntity, e);
                }
            }

            // Clean up expired effects
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                if (_effects[i].Duration == 0)
                    RemoveEffectAt(i);
            }
        }

        private void HandleTakeDamage(GameEvent e)
        {
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                if (i < _effects.Count)
                    _effects[i].OnTakeDamage(ParentEntity, e);
            }
        }

        /// <summary>
        /// Tier 2.4: route Phase F's <c>BeforeTakeDamage</c> event to per-effect
        /// dispatch. Each effect's <see cref="Effect.OnBeforeTakeDamage"/>
        /// observes (and may mutate) the incoming <see cref="Damage"/> before
        /// resistance and HP decrement. Iterates oldest-first since the
        /// mutation order is "all effects had a turn"; reverse-iteration would
        /// give newer effects a less direct view of the damage.
        /// </summary>
        private void HandleBeforeTakeDamage(GameEvent e)
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                _effects[i].OnBeforeTakeDamage(ParentEntity, e);
            }
        }

        private bool HandleRender(GameEvent e)
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                if (!_effects[i].Render(e))
                    return false;
            }

            string effectColor = GetRenderColorOverride();
            if (!string.IsNullOrEmpty(effectColor))
                e.SetParameter("ColorString", effectColor);

            return true;
        }

        private void RemoveEffectAt(int index)
        {
            Effect effect = _effects[index];
            _effects.RemoveAt(index);
            effect.Remove(ParentEntity);

            // Diag hook (D1.2): record the OnRemove with effect type, final
            // duration, and the cause string set by the caller (one of
            // CAUSE_DURATION_EXPIRED / CAUSE_SAVE_SUCCEEDED / CAUSE_EXTERNAL).
            // Read BEFORE we null effect.Owner so the payload sees a coherent
            // effect-state snapshot. Hook is a no-op when the "effect"
            // channel is disabled (default-on per AI-OBSERVABILITY.md §3).
            if (Diag.IsChannelEnabled("effect"))
            {
                Diag.Record(
                    category: "effect",
                    kind: "OnRemove",
                    target: ParentEntity,
                    payload: new
                    {
                        effect = effect.GetType().Name,
                        duration = effect.Duration,
                        cause = effect.LastRemovalCause
                    });
            }

            effect.Owner = null;
            SendRemoved(effect);
        }

        private bool CheckBeforeApply(Effect effect, Entity source, bool forced)
        {
            var before = GameEvent.New(forced ? "BeforeForceApplyEffect" : "BeforeApplyEffect");
            before.SetParameter("Target", (object)ParentEntity);
            before.SetParameter("Effect", (object)effect);
            before.SetParameter("EffectType", effect.ClassName);
            before.SetParameter("Forced", forced);
            if (source != null)
                before.SetParameter("Source", (object)source);

            return ParentEntity.FireEventAndRelease(before);
        }

        private void SendApplied(Effect effect, Entity source, Zone zone, bool forced)
        {
            if (forced)
            {
                var forceApplied = GameEvent.New("EffectForceApplied");
                forceApplied.SetParameter("Target", (object)ParentEntity);
                forceApplied.SetParameter("Effect", (object)effect);
                forceApplied.SetParameter("EffectType", effect.ClassName);
                forceApplied.SetParameter("Forced", true);
                if (source != null)
                    forceApplied.SetParameter("Source", (object)source);
                ParentEntity.FireEventAndRelease(forceApplied);
            }

            var applied = GameEvent.New("EffectApplied");
            applied.SetParameter("Target", (object)ParentEntity);
            applied.SetParameter("Effect", (object)effect);
            applied.SetParameter("EffectType", effect.ClassName);
            applied.SetParameter("Forced", forced);
            if (source != null)
                applied.SetParameter("Source", (object)source);
            ParentEntity.FireEventAndRelease(applied);

            TryStartAura(effect, zone);
        }

        private void SendRemoved(Effect effect)
        {
            var removed = GameEvent.New("EffectRemoved");
            removed.SetParameter("Target", (object)ParentEntity);
            removed.SetParameter("Effect", (object)effect);
            removed.SetParameter("EffectType", effect.ClassName);
            // Cause: "duration_expired" by default; effects with save-based
            // recovery overwrite Effect.LastRemovalCause before setting
            // Duration=0; public RemoveEffect overloads set CAUSE_EXTERNAL.
            removed.SetParameter("Cause", effect.LastRemovalCause);
            ParentEntity.FireEventAndRelease(removed);

            TryStopAura(effect);
        }

        private void TryStartAura(Effect effect, Zone zone)
        {
            if (effect == null || zone == null || ParentEntity == null)
                return;

            if (effect is IAuraProvider aura)
                AsciiFxBus.StartAura(zone, ParentEntity, aura.GetAuraTheme());
        }

        private void TryStopAura(Effect effect)
        {
            if (effect == null || ParentEntity == null)
                return;

            if (effect is IAuraProvider aura)
                AsciiFxBus.StopAura(ParentEntity, aura.GetAuraTheme());
        }
    }
}
