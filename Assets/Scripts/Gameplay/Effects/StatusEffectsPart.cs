using System;
using System.Collections.Generic;

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
            if (!effect.Apply(ParentEntity))
                return false;

            _effects.Add(effect);
            effect.Applied(ParentEntity);
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
            for (int i = 0; i < _effects.Count; i++)
                _effects[i].Owner = ParentEntity;
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
            // Same scan as HandleBeginTakeAction — if any effect returns
            // AllowAction=false, block the move. We DON'T re-log here
            // because HandleBeginTakeAction already logged on the turn
            // gate; a second "cannot act" per skipped-input key-press
            // would spam the log.
            for (int i = 0; i < _effects.Count; i++)
            {
                if (!_effects[i].AllowAction(ParentEntity))
                {
                    e.Handled = true;
                    return false;
                }
            }
            return true;
        }

        private bool HandleBeginTakeAction(GameEvent e)
        {
            // Check if any effect blocks action (stun, paralysis)
            for (int i = 0; i < _effects.Count; i++)
            {
                if (!_effects[i].AllowAction(ParentEntity))
                {
                    // Log that turn is skipped
                    string name = ParentEntity.GetDisplayName();
                    string effectName = _effects[i].DisplayName;
                    MessageLog.Add(name + " is " + effectName + " and cannot act!");
                    e.Handled = true;
                    return false; // block the turn
                }
            }

            // Fire OnTurnStart for each effect (poison damage, etc.)
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                if (i < _effects.Count)
                    _effects[i].OnTurnStart(ParentEntity, e);
            }

            return true;
        }

        private void HandleEndTurn(GameEvent e)
        {
            // Tick each effect
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                if (i < _effects.Count)
                    _effects[i].OnTurnEnd(ParentEntity, e);
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

            return ParentEntity.FireEvent(before);
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
                ParentEntity.FireEvent(forceApplied);
            }

            var applied = GameEvent.New("EffectApplied");
            applied.SetParameter("Target", (object)ParentEntity);
            applied.SetParameter("Effect", (object)effect);
            applied.SetParameter("EffectType", effect.ClassName);
            applied.SetParameter("Forced", forced);
            if (source != null)
                applied.SetParameter("Source", (object)source);
            ParentEntity.FireEvent(applied);

            TryStartAura(effect, zone);
        }

        private void SendRemoved(Effect effect)
        {
            var removed = GameEvent.New("EffectRemoved");
            removed.SetParameter("Target", (object)ParentEntity);
            removed.SetParameter("Effect", (object)effect);
            removed.SetParameter("EffectType", effect.ClassName);
            ParentEntity.FireEvent(removed);

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
