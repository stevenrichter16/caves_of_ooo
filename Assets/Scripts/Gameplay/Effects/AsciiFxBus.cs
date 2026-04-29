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
        OvenImproved = 11,
        LanternDark = 12,
        LanternLit = 13,
        LanternBright = 14,
        Earth = 15,
        Water = 16,
        Holy = 17,
        ThrownObject = 18
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
        ChainArc = 7,
        ColumnRise = 8,
        Particle = 9
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
        public int Height;
        public float LingerDuration;
        public char Glyph;
        public string ColorString;
        public float Lifetime;
        public float MoveInterval;

        /// <summary>
        /// Wipe all fields back to defaults. Called by
        /// <see cref="AsciiFxBus.Rent"/> before handing a pooled instance
        /// to a caller, so two consecutive rentals never see leaked state
        /// from the previous request. Tier-B Fix #1.
        /// </summary>
        public void Reset()
        {
            Type = default;
            Theme = default;
            Zone = null;
            Anchor = null;
            Path = null;
            X = 0;
            Y = 0;
            DX = 0;
            DY = 0;
            Radius = 0;
            MaxRadius = 0;
            Trail = false;
            BlocksTurnAdvance = false;
            Duration = 0f;
            StepDuration = 0f;
            Delay = 0f;
            Height = 0;
            LingerDuration = 0f;
            Glyph = '\0';
            ColorString = null;
            Lifetime = 0f;
            MoveInterval = 0f;
        }
    }

    /// <summary>
    /// Pure C# request bus for transient ASCII world FX.
    /// Gameplay code emits requests here; the renderer consumes and visualizes them.
    /// </summary>
    public static class AsciiFxBus
    {
        private static readonly Queue<AsciiFxRequest> PendingRequests = new Queue<AsciiFxRequest>();

        /// <summary>
        /// Object pool for <see cref="AsciiFxRequest"/>. Each emit path
        /// rents from this pool instead of allocating a fresh request;
        /// <see cref="AsciiFxRenderer"/> calls <see cref="Release"/> after
        /// processing each drained request. Bounded at <see cref="MaxPoolSize"/>
        /// so a one-time burst (zone load, mass FX) doesn't grow the pool
        /// indefinitely.
        ///
        /// <para>Tier-B Fix #1 in PERF-COMBAT-INVESTIGATION.md. Replaces
        /// per-emit class allocations (~3-6 per damage number, plus
        /// projectile / aura / beam allocs) with steady-state zero
        /// allocation under typical combat load.</para>
        /// </summary>
        private static readonly Stack<AsciiFxRequest> Pool = new Stack<AsciiFxRequest>(64);
        private const int MaxPoolSize = 256;

        /// <summary>
        /// Rent a fully-reset <see cref="AsciiFxRequest"/> from the pool,
        /// or allocate a fresh one if the pool is empty. Caller fills the
        /// fields it cares about and Enqueues the result.
        /// </summary>
        private static AsciiFxRequest Rent()
        {
            if (Pool.Count > 0)
            {
                var req = Pool.Pop();
                req.Reset();
                return req;
            }
            return new AsciiFxRequest();
        }

        /// <summary>
        /// Return a consumed <see cref="AsciiFxRequest"/> to the pool.
        /// Bounded — drops requests on the floor if the pool is full so a
        /// burst of FX doesn't permanently inflate memory. Called by
        /// <c>AsciiFxRenderer.ConsumeRequests</c> after the request has
        /// been turned into a renderer-side instance.
        /// </summary>
        public static void Release(AsciiFxRequest request)
        {
            if (request == null) return;
            if (Pool.Count >= MaxPoolSize) return;
            // Don't reset here — Rent does it on retrieval. Skipping the
            // reset on Release lets us drop the request on the floor with
            // a single ref-decrement when the pool is full.
            Pool.Push(request);
        }

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

            var req = Rent();
            req.Type = AsciiFxRequestType.Projectile;
            req.Zone = zone;
            req.Theme = theme;
            req.Path = copy;
            req.Trail = trail;
            req.BlocksTurnAdvance = blocksTurnAdvance;
            req.Delay = delay < 0f ? 0f : delay;
            PendingRequests.Enqueue(req);
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

            var req = Rent();
            req.Type = AsciiFxRequestType.Burst;
            req.Zone = zone;
            req.Theme = theme;
            req.X = x;
            req.Y = y;
            req.BlocksTurnAdvance = blocksTurnAdvance;
            req.Delay = delay < 0f ? 0f : delay;
            PendingRequests.Enqueue(req);
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

            var req = Rent();
            req.Type = AsciiFxRequestType.Beam;
            req.Zone = zone;
            req.Theme = theme;
            req.Path = copy;
            req.DX = dx;
            req.DY = dy;
            req.Duration = duration;
            req.BlocksTurnAdvance = blocksTurnAdvance;
            req.Delay = delay < 0f ? 0f : delay;
            PendingRequests.Enqueue(req);
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

            var req = Rent();
            req.Type = AsciiFxRequestType.ChargeOrbit;
            req.Zone = zone;
            req.Anchor = anchor;
            req.Theme = theme;
            req.Radius = radius;
            req.Duration = duration;
            req.BlocksTurnAdvance = blocksTurnAdvance;
            req.Delay = delay < 0f ? 0f : delay;
            PendingRequests.Enqueue(req);
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

            var req = Rent();
            req.Type = AsciiFxRequestType.RingWave;
            req.Zone = zone;
            req.Theme = theme;
            req.X = x;
            req.Y = y;
            req.MaxRadius = maxRadius;
            req.StepDuration = stepDuration;
            req.BlocksTurnAdvance = blocksTurnAdvance;
            req.Delay = delay < 0f ? 0f : delay;
            PendingRequests.Enqueue(req);
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

            var req = Rent();
            req.Type = AsciiFxRequestType.ChainArc;
            req.Zone = zone;
            req.Theme = theme;
            req.Path = copy;
            req.StepDuration = hopDuration;
            req.BlocksTurnAdvance = blocksTurnAdvance;
            req.Delay = delay < 0f ? 0f : delay;
            PendingRequests.Enqueue(req);
        }

        public static void EmitColumnRise(
            Zone zone,
            int x,
            int y,
            int height,
            float stepDuration,
            float lingerDuration,
            AsciiFxTheme theme,
            bool blocksTurnAdvance,
            float delay = 0f)
        {
            if (zone == null || !zone.InBounds(x, y) || height <= 0 || stepDuration <= 0f)
                return;

            var req = Rent();
            req.Type = AsciiFxRequestType.ColumnRise;
            req.Zone = zone;
            req.Theme = theme;
            req.X = x;
            req.Y = y;
            req.Height = height;
            req.StepDuration = stepDuration;
            req.LingerDuration = lingerDuration;
            req.BlocksTurnAdvance = blocksTurnAdvance;
            req.Delay = delay < 0f ? 0f : delay;
            PendingRequests.Enqueue(req);
        }

        public static void EmitParticle(
            Zone zone,
            int x,
            int y,
            char glyph,
            string colorString,
            float lifetime,
            int dy = 0,
            float moveInterval = 0f,
            float delay = 0f)
        {
            if (zone == null || !zone.InBounds(x, y) || lifetime <= 0f)
                return;

            var req = Rent();
            req.Type = AsciiFxRequestType.Particle;
            req.Zone = zone;
            req.X = x;
            req.Y = y;
            req.Glyph = glyph;
            req.ColorString = colorString;
            req.Lifetime = lifetime;
            req.DY = dy;
            req.MoveInterval = moveInterval;
            req.Delay = delay < 0f ? 0f : delay;
            PendingRequests.Enqueue(req);
        }

        public static void EmitFloatingNumber(
            Zone zone,
            int x,
            int y,
            int number,
            string colorString,
            float delay = 0f)
        {
            if (zone == null || number <= 0)
                return;

            string digits = number.ToString();
            int startX = x - (digits.Length - 1) / 2;

            for (int i = 0; i < digits.Length; i++)
            {
                int px = startX + i;
                if (!zone.InBounds(px, y)) continue;

                EmitParticle(zone, px, y - 1, digits[i], colorString,
                    lifetime: 0.6f, dy: -1, moveInterval: 0.15f, delay: delay);
            }
        }

        public static void StartAura(Zone zone, Entity anchor, AsciiFxTheme theme)
        {
            if (zone == null || anchor == null)
                return;

            var req = Rent();
            req.Type = AsciiFxRequestType.AuraStart;
            req.Zone = zone;
            req.Anchor = anchor;
            req.Theme = theme;
            PendingRequests.Enqueue(req);
        }

        public static void StopAura(Entity anchor, AsciiFxTheme theme)
        {
            if (anchor == null)
                return;

            var req = Rent();
            req.Type = AsciiFxRequestType.AuraStop;
            req.Anchor = anchor;
            req.Theme = theme;
            PendingRequests.Enqueue(req);
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
            // Recycle dropped requests back into the pool so a Clear() that
            // happens mid-combat doesn't permanently shrink the pool.
            while (PendingRequests.Count > 0)
                Release(PendingRequests.Dequeue());
        }
    }
}
