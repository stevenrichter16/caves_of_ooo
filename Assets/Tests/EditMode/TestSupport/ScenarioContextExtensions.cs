using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;

namespace CavesOfOoo.Tests.TestSupport
{
    /// <summary>
    /// Phase 3b — test-only extensions on <see cref="ScenarioContext"/>.
    ///
    /// Lives in the test assembly so that runtime builds don't pick up these
    /// APIs. The point is to express test intent clearly (e.g. "advance 15 turns"
    /// instead of "for-loop fire TakeTurn on every entity").
    /// </summary>
    public static class ScenarioContextExtensions
    {
        /// <summary>
        /// Fire a <c>TakeTurn</c> event on every entity registered with the
        /// context's <see cref="TurnManager"/>, <paramref name="count"/> times.
        /// Returns the same context for fluent chaining into <c>.Verify()</c>
        /// (Phase 3c).
        ///
        /// Semantics (chosen deliberately — see note below):
        /// - Simple tick. Every registered entity gets exactly one TakeTurn per
        ///   advance-step, regardless of Speed stat. Matches how current AI
        ///   tests manually loop TakeTurn. For speed-accurate simulation, use
        ///   <see cref="TurnManager.Tick"/> / <see cref="TurnManager.ProcessUntilPlayerTurn"/>
        ///   directly — those are the production paths and behave exactly like
        ///   the live game.
        /// - Snapshot iteration. Entities are snapshotted per advance-step
        ///   before firing, so mid-step additions/removals don't affect the
        ///   current step. New entities added during step N start ticking on
        ///   step N+1.
        /// - Manually-constructed entities (created with <c>new Entity()</c>
        ///   and placed via <c>Zone.AddEntity</c> without going through
        ///   <c>ScenarioContext.Spawn</c>) are NOT in the TurnManager's list
        ///   and will NOT receive TakeTurn events from this helper. Either
        ///   register them via <c>ctx.Turns.AddEntity(entity)</c> before
        ///   calling AdvanceTurns, or keep firing TakeTurn on them manually.
        /// </summary>
        /// <param name="ctx">The scenario context.</param>
        /// <param name="count">How many TakeTurn cycles to run (default 1).</param>
        /// <returns>The same context, for chaining.</returns>
        public static ScenarioContext AdvanceTurns(this ScenarioContext ctx, int count = 1)
        {
            if (ctx == null) return null;
            if (count <= 0) return ctx;

            for (int step = 0; step < count; step++)
            {
                // Snapshot the entity list so mid-step adds/removes don't skip
                // or double-tick entities within the same advance-step.
                var snapshot = new List<Entity>(ctx.Turns.EntityCount);
                foreach (var entity in ctx.Turns.Entities)
                    snapshot.Add(entity);

                for (int i = 0; i < snapshot.Count; i++)
                    snapshot[i].FireEvent(GameEvent.New("TakeTurn"));
            }

            return ctx;
        }
    }
}
