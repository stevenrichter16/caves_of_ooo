using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Scenarios
{
    /// <summary>
    /// Dispatches a pending scenario to the live game when <see cref="GameBootstrap.OnAfterBootstrap"/>
    /// fires. The pending scenario type is set by an editor menu click (see
    /// <c>ScenarioMenuItems.cs</c> in the Editor assembly), then Play mode is entered,
    /// which triggers GameBootstrap.Start → OnAfterBootstrap → this runner.
    ///
    /// Design notes:
    /// - The pending scenario is a <see cref="Type"/> rather than an instance. This
    ///   avoids keeping live references across Play-mode enter/exit domain reloads.
    /// - <see cref="LastLaunched"/> is preserved across runs (via static state) so a
    ///   "re-run last scenario" menu shortcut (<c>Cmd+Shift+R</c>) can fire the same
    ///   scenario without navigating the menu.
    /// - If the scenario's <c>Apply</c> throws, we log the error but don't let it
    ///   abort bootstrap — the game continues as a normal Play session.
    /// </summary>
    public static class ScenarioRunner
    {
        /// <summary>
        /// Scenario type set by a menu click before entering Play mode. Cleared after
        /// the scenario is applied (one-shot).
        /// </summary>
        public static Type PendingScenario;

        /// <summary>
        /// The most recently-applied scenario type. Survives across Play mode exits
        /// so <c>Re-run Last</c> can re-launch it without re-navigating the menu.
        /// Null until the first successful launch in the current Editor session.
        /// </summary>
        public static Type LastLaunched;

        /// <summary>
        /// Set <see cref="PendingScenario"/> and enter Play mode in one call.
        /// This is what editor menu items invoke. Safe to call if Play mode is
        /// already running — the scenario will be picked up on the NEXT Play-mode
        /// entry (fresh bootstrap). Mid-play application is a future extension.
        /// </summary>
        public static void Launch<T>() where T : IScenario, new()
            => Launch(typeof(T));

        /// <summary>Non-generic Launch — used by the Re-run Last shortcut.</summary>
        public static void Launch(Type scenarioType)
        {
            if (scenarioType == null)
            {
                Debug.LogWarning("[ScenarioRunner] Launch called with null type — ignoring.");
                return;
            }
            if (!typeof(IScenario).IsAssignableFrom(scenarioType))
            {
                Debug.LogError($"[ScenarioRunner] {scenarioType.Name} does not implement IScenario.");
                return;
            }

            PendingScenario = scenarioType;

#if UNITY_EDITOR
            // Enter Play mode if not already playing. If already playing, the user
            // must exit and re-enter for the scenario to apply (Phase 1 behavior).
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.isPlaying = true;
            }
            else
            {
                Debug.LogWarning(
                    $"[ScenarioRunner] Play mode already active — '{scenarioType.Name}' will apply on NEXT Play-mode entry. " +
                    "Stop Play mode (Cmd+P) and start again to trigger this scenario.");
            }
#else
            Debug.LogWarning("[ScenarioRunner] Launch called outside Editor — PendingScenario set but Play mode can't be auto-entered.");
#endif
        }

        /// <summary>
        /// Subscribe to <see cref="GameBootstrap.OnAfterBootstrap"/> very early in
        /// runtime initialization. This runs automatically before any scene loads,
        /// so when GameBootstrap's Start fires, we're already subscribed.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Subscribe()
        {
            // Defensive: unsubscribe first in case of repeated domain reloads during dev.
            GameBootstrap.OnAfterBootstrap -= HandleAfterBootstrap;
            GameBootstrap.OnAfterBootstrap += HandleAfterBootstrap;
        }

        private static void HandleAfterBootstrap(Zone zone, EntityFactory factory, Entity player, TurnManager turns)
        {
            if (PendingScenario == null) return;

            var pending = PendingScenario;
            PendingScenario = null; // clear immediately so a throw can't loop

            IScenario instance;
            try
            {
                instance = (IScenario)Activator.CreateInstance(pending);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScenarioRunner] Could not instantiate {pending.Name}: {ex.Message}");
                return;
            }

            var ctx = new ScenarioContext(zone, factory, player, turns);
            try
            {
                instance.Apply(ctx);
                LastLaunched = pending;
                Debug.Log($"[ScenarioRunner] Applied scenario: {pending.Name}");
                MessageLog.Add($"[Scenario] {pending.Name} applied.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScenarioRunner] {pending.Name}.Apply threw: {ex}");
                MessageLog.Add($"[Scenario] FAILED: {pending.Name} — see console.");
            }
        }
    }
}
