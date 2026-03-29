using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    public enum AsciiFxTheme
    {
        Fire = 0,
        Ice = 1,
        Poison = 2
    }

    public enum AsciiFxRequestType
    {
        Projectile = 0,
        Burst = 1,
        AuraStart = 2,
        AuraStop = 3
    }

    public class AsciiFxRequest
    {
        public AsciiFxRequestType Type;
        public AsciiFxTheme Theme;
        public Zone Zone;
        public Entity Anchor;
        public List<Point> Path;
        public int X;
        public int Y;
        public bool Trail;
        public bool BlocksTurnAdvance;
    }

    /// <summary>
    /// Pure C# request bus for transient ASCII world FX.
    /// Gameplay code emits requests here; the renderer consumes and visualizes them.
    /// </summary>
    public static class AsciiFxBus
    {
        private static readonly Queue<AsciiFxRequest> PendingRequests = new Queue<AsciiFxRequest>();

        public static void EmitProjectile(
            Zone zone,
            IReadOnlyList<Point> path,
            AsciiFxTheme theme,
            bool trail,
            bool blocksTurnAdvance)
        {
            if (zone == null || path == null || path.Count == 0)
                return;

            var copy = new List<Point>(path.Count);
            for (int i = 0; i < path.Count; i++)
                copy.Add(path[i]);

            PendingRequests.Enqueue(new AsciiFxRequest
            {
                Type = AsciiFxRequestType.Projectile,
                Zone = zone,
                Theme = theme,
                Path = copy,
                Trail = trail,
                BlocksTurnAdvance = blocksTurnAdvance
            });
        }

        public static void EmitBurst(
            Zone zone,
            int x,
            int y,
            AsciiFxTheme theme,
            bool blocksTurnAdvance)
        {
            if (zone == null || !zone.InBounds(x, y))
                return;

            PendingRequests.Enqueue(new AsciiFxRequest
            {
                Type = AsciiFxRequestType.Burst,
                Zone = zone,
                Theme = theme,
                X = x,
                Y = y,
                BlocksTurnAdvance = blocksTurnAdvance
            });
        }

        public static void StartAura(Zone zone, Entity anchor, AsciiFxTheme theme)
        {
            if (zone == null || anchor == null)
                return;

            PendingRequests.Enqueue(new AsciiFxRequest
            {
                Type = AsciiFxRequestType.AuraStart,
                Zone = zone,
                Anchor = anchor,
                Theme = theme
            });
        }

        public static void StopAura(Entity anchor, AsciiFxTheme theme)
        {
            if (anchor == null)
                return;

            PendingRequests.Enqueue(new AsciiFxRequest
            {
                Type = AsciiFxRequestType.AuraStop,
                Anchor = anchor,
                Theme = theme
            });
        }

        public static List<AsciiFxRequest> Drain()
        {
            var drained = new List<AsciiFxRequest>(PendingRequests.Count);
            while (PendingRequests.Count > 0)
                drained.Add(PendingRequests.Dequeue());
            return drained;
        }

        public static void Clear()
        {
            PendingRequests.Clear();
        }
    }
}
