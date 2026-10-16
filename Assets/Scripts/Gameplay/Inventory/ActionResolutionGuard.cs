using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>Transient main-thread stability boundary. It is never serialized.
    /// Command scopes cover synchronous callbacks; native schedulers hold a debt
    /// from committed return through FX and the owner's actual closing turn.</summary>
    public static class ActionResolutionGuard
    {
        private static int _depth;
        private static readonly HashSet<Entity> ResolvingActors = new HashSet<Entity>();
        private static readonly HashSet<Entity> PendingActors = new HashSet<Entity>();
        public static bool IsUnstable => _depth > 0 || PendingActors.Count > 0;
        public static IDisposable Enter() { _depth++; return new Scope(); }
        public static void HoldPendingAction(Entity actor) { if (actor != null) PendingActors.Add(actor); }
        // Explicit settlement also covers death/input destruction. Do not infer
        // settlement from HP: death callbacks may still be changing the world.
        public static void SettlePendingAction(Entity actor) { if (actor != null) PendingActors.Remove(actor); }
        /// <summary>Exclusive paid action for one actor; independent receipt/pickup
        /// commands remain legal. A pending native action cannot launch another.</summary>
        internal static IDisposable TryEnterAction(Entity actor)
        {
            if (actor == null || PendingActors.Contains(actor) || !ResolvingActors.Add(actor)) return null;
            _depth++;
            return new ActionScope(actor);
        }
        private sealed class ActionScope : IDisposable
        {
            private Entity _actor;
            public ActionScope(Entity actor) { _actor = actor; }
            public void Dispose()
            {
                if (_actor == null) return;
                ResolvingActors.Remove(_actor); _actor = null; _depth--;
            }
        }
        private sealed class Scope : IDisposable
        {
            private bool _disposed;
            public void Dispose() { if (_disposed) return; _disposed = true; _depth--; }
        }
    }
}
