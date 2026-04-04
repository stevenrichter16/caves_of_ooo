using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    public enum AsciiFxTheme
    {
        Fire = 0,
        Ice = 1,
        Poison = 2,
        Arcane = 3,
        Lightning = 4,
        WellFouled = 5,
        WellClean = 6,
        WellImproved = 7,
        Campfire = 8,
        OvenBroken = 9,
        OvenWorking = 10,
        OvenImproved = 11
    }

    public enum AsciiFxRequestType
    {
        Projectile = 0,
        Burst = 1,
        AuraStart = 2,
        AuraStop = 3,
        Beam = 4,
        ChargeOrbit = 5,
        RingWave = 6,
        ChainArc = 7
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
        public int DX;
        public int DY;
        public int Radius;
        public int MaxRadius;
        public bool Trail;
        public bool BlocksTurnAdvance;
        public float Duration;
        public float StepDuration;
        public float Delay;
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
            bool blocksTurnAdvance,
            float delay = 0f)
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
                BlocksTurnAdvance = blocksTurnAdvance,
                Delay = delay < 0f ? 0f : delay
            });
        }

        public static void EmitBurst(
            Zone zone,
            int x,
            int y,
            AsciiFxTheme theme,
            bool blocksTurnAdvance,
            float delay = 0f)
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
                BlocksTurnAdvance = blocksTurnAdvance,
                Delay = delay < 0f ? 0f : delay
            });
        }

        public static void EmitBeam(
            Zone zone,
            IReadOnlyList<Point> path,
            int dx,
            int dy,
            AsciiFxTheme theme,
            float duration,
            bool blocksTurnAdvance,
            float delay = 0f)
        {
            if (zone == null || path == null || path.Count == 0 || duration <= 0f)
                return;

            var copy = new List<Point>(path.Count);
            for (int i = 0; i < path.Count; i++)
                copy.Add(path[i]);

            PendingRequests.Enqueue(new AsciiFxRequest
            {
                Type = AsciiFxRequestType.Beam,
                Zone = zone,
                Theme = theme,
                Path = copy,
                DX = dx,
                DY = dy,
                Duration = duration,
                BlocksTurnAdvance = blocksTurnAdvance,
                Delay = delay < 0f ? 0f : delay
            });
        }

        public static void EmitChargeOrbit(
            Zone zone,
            Entity anchor,
            int radius,
            float duration,
            AsciiFxTheme theme,
            bool blocksTurnAdvance,
            float delay = 0f)
        {
            if (zone == null || anchor == null || radius <= 0 || duration <= 0f)
                return;

            PendingRequests.Enqueue(new AsciiFxRequest
            {
                Type = AsciiFxRequestType.ChargeOrbit,
                Zone = zone,
                Anchor = anchor,
                Theme = theme,
                Radius = radius,
                Duration = duration,
                BlocksTurnAdvance = blocksTurnAdvance,
                Delay = delay < 0f ? 0f : delay
            });
        }

        public static void EmitRingWave(
            Zone zone,
            int x,
            int y,
            int maxRadius,
            float stepDuration,
            AsciiFxTheme theme,
            bool blocksTurnAdvance,
            float delay = 0f)
        {
            if (zone == null || !zone.InBounds(x, y) || maxRadius <= 0 || stepDuration <= 0f)
                return;

            PendingRequests.Enqueue(new AsciiFxRequest
            {
                Type = AsciiFxRequestType.RingWave,
                Zone = zone,
                Theme = theme,
                X = x,
                Y = y,
                MaxRadius = maxRadius,
                StepDuration = stepDuration,
                BlocksTurnAdvance = blocksTurnAdvance,
                Delay = delay < 0f ? 0f : delay
            });
        }

        public static void EmitChainArc(
            Zone zone,
            IReadOnlyList<Point> hops,
            AsciiFxTheme theme,
            float hopDuration,
            bool blocksTurnAdvance,
            float delay = 0f)
        {
            if (zone == null || hops == null || hops.Count < 2 || hopDuration <= 0f)
                return;

            var copy = new List<Point>(hops.Count);
            for (int i = 0; i < hops.Count; i++)
                copy.Add(hops[i]);

            PendingRequests.Enqueue(new AsciiFxRequest
            {
                Type = AsciiFxRequestType.ChainArc,
                Zone = zone,
                Theme = theme,
                Path = copy,
                StepDuration = hopDuration,
                BlocksTurnAdvance = blocksTurnAdvance,
                Delay = delay < 0f ? 0f : delay
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
