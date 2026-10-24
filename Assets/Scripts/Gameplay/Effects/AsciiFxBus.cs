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
        Particle = 9,
        EventText = 10,
        FloatingNumber = 11
    }

    public class AsciiFxRequest
    {
        public AsciiFxRequestType Type;
        public AsciiFxTheme Theme;
        public Zone Zone;
        public Entity Anchor;
        public List<Point> Path;
        public List<ThrowFlightPoint> ThrowFlight;
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
        public string Text;
        public string ColorString;
        public float Lifetime;
        public float MoveInterval;
        /// <summary>One complete positive damage receipt; Text is its invariant
        /// decimal representation. Zero for nonnumeric requests.</summary>
        public int Number;
        /// <summary>Exact disclosed cell captured at emission, not a coordinate
        /// permission that can transfer to a replacement cell or world.</summary>
        public Cell OriginCell;
        /// <summary>A visible lethal-at-emission receipt may briefly remain at
        /// its origin after that exact owner is removed. Never grants authority
        /// to follow a removed living owner or a replacement entity.</summary>
        public bool AllowOriginOnly;

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
            ThrowFlight = null;
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
            Text = null;
            ColorString = null;
            Lifetime = 0f;
            MoveInterval = 0f;
            Number = 0;
            OriginCell = null;
            AllowOriginOnly = false;
        }
    }

    /// <summary>
    /// Pure C# request bus for transient ASCII world FX.
    /// Gameplay code emits requests here; the renderer consumes and visualizes them.
    /// </summary>
    public static class AsciiFxBus
    {
        /// <summary>Maximum width of one reusable floating event label.</summary>
        public const int MaxEventTextLength = 24;
        private const int MaxPendingEventTexts = 64;
        private const float EventTextLifetime = 1.1f;
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

        /// <summary>
        /// Pool ceiling — beyond this, Release drops requests on the
        /// floor and Rent allocates fresh. Bumped from 256 → 1024 in
        /// the scaling-prep audit (S2). A damage hit now emits one
        /// complete numeric receipt; beams emit 1, chain arcs emit
        /// hop-count, projectiles emit per-step trail. Sustained
        /// 5+ enemy combat with 50+ active FX easily blew past 256.
        /// </summary>
        private const int MaxPoolSize = 1024;

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
            // Queued/active presentation may be cancelled during a world change.
            // Pooled requests must not retain that world's owners or origin cells.
            request.Reset();
            if (Pool.Count >= MaxPoolSize) return;
            Pool.Push(request);
        }

        public static void EmitThrow(Zone zone, ThrowTraceResult trace)
        {
            if(trace==null)return;
            EmitProjectile(zone,trace.Path,AsciiFxTheme.ThrownObject,true,true,0f,trace.FlightPath);
        }

        public static void EmitProjectile(
            Zone zone,
            IReadOnlyList<Point> path,
            AsciiFxTheme theme,
            bool trail,
            bool blocksTurnAdvance,
            float delay = 0f,
            IReadOnlyList<ThrowFlightPoint> throwFlight = null)
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
            if(throwFlight!=null && throwFlight.Count==path.Count)
            {
                req.ThrowFlight=new List<ThrowFlightPoint>(throwFlight.Count);
                for(int i=0;i<throwFlight.Count;i++)req.ThrowFlight.Add(throwFlight[i]);
            }
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

        /// <summary>Readouts reveal only a currently visible physical owner. Also
        /// checked by the renderer: queued text must not outlive visibility.</summary>
        public static bool CanShowEventText(Zone zone, Entity anchor)
        {
            if (zone == null || anchor == null || anchor.SpatialZone != zone) return false;
            var render = anchor.GetPart<RenderPart>();
            if (render == null || render.ParentEntity != anchor || !render.Visible) return false;
            var physics = anchor.GetPart<PhysicsPart>();
            if (physics != null && (physics.ParentEntity != anchor
                || physics.InInventory != null || physics.Equipped != null)) return false;
            var cell = zone.GetEntityCell(anchor);
            return IsReadoutOriginVisible(zone, cell) && cell.Objects.Contains(anchor);
        }

        internal static bool IsReadoutOriginVisible(Zone zone, Cell origin)
        {
            if (zone == null || origin == null || origin.ParentZone != zone
                || !ReferenceEquals(zone.GetCell(origin.X, origin.Y), origin)
                || !origin.Explored || !origin.IsVisible) return false;
            var manager = WorldLocationContext.For(zone);
            return manager == null || zone.ZoneID != null && manager.CachedZones != null
                && manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                && ReferenceEquals(current, zone);
        }

        /// <summary>One brief presentation-only message at a visible owner. No
        /// simulation state, RNG, action cost or turn blocking. Identical pending
        /// labels coalesce; event groups and text width are bounded independently
        /// of decorative requests. A null zone uses the owner's physical zone.</summary>
        public static void EmitEventText(Zone zone, Entity anchor, string text, string colorString)
        {
            zone ??= anchor?.SpatialZone;
            if (!CanShowEventText(zone, anchor) || string.IsNullOrWhiteSpace(text)) return;
            for (int i = 0; i < text.Length; i++)
                if (char.IsControl(text[i])) return;
            text = text.Trim();
            if (text.Length > MaxEventTextLength) text = text.Substring(0, MaxEventTextLength);
            int count = 0;
            foreach (var pending in PendingRequests)
            {
                if (pending.Type != AsciiFxRequestType.EventText && pending.Type != AsciiFxRequestType.FloatingNumber) continue;
                if (++count >= MaxPendingEventTexts || (pending.Type == AsciiFxRequestType.EventText
                    && pending.Zone == zone && pending.Anchor == anchor && pending.Text == text)) return;
            }
            var cell = zone.GetEntityCell(anchor);
            var req = Rent();
            req.Type = AsciiFxRequestType.EventText;
            req.Zone = zone; req.Anchor = anchor; req.X = cell.X; req.Y = cell.Y;
            req.OriginCell = cell;
            req.Text = text;
            req.ColorString = colorString != null && colorString.Length == 2 && colorString[0] == '&'
                && "kKrRgGbBcCmMwWyY".IndexOf(colorString[1]) >= 0 ? colorString : "&W";
            req.Lifetime = EventTextLifetime;
            PendingRequests.Enqueue(req);
        }

        /// <summary>One noncoalescing damage receipt at its real visible owner.
        /// The lethal allowance is captured while that owner still belongs to
        /// the disclosed cell, before HandleDeath removes it.</summary>
        public static void EmitFloatingNumber(Zone zone, Entity anchor, int number,
            string colorString, bool allowOriginOnly = false, float delay = 0f)
        {
            if (number <= 0 || !CanShowEventText(zone, anchor)) return;
            var hp = anchor.GetStat("Hitpoints");
            QueueFloatingNumber(zone, anchor, zone.GetEntityCell(anchor), number, colorString,
                allowOriginOnly && hp != null && hp.BaseValue <= 0, delay);
        }

        /// <summary>Compatibility for standalone coordinate callers. Still one
        /// whole number, authorized only by its exact visible captured origin.</summary>
        public static void EmitFloatingNumber(Zone zone, int x, int y, int number,
            string colorString, float delay = 0f)
        {
            var origin = zone?.GetCell(x, y);
            if (number <= 0 || !IsReadoutOriginVisible(zone, origin)) return;
            QueueFloatingNumber(zone, null, origin, number, colorString, true, delay);
        }

        private static void QueueFloatingNumber(Zone zone, Entity anchor, Cell origin,
            int number, string colorString, bool allowOriginOnly, float delay)
        {
            int count = 0;
            foreach (var pending in PendingRequests)
                if ((pending.Type == AsciiFxRequestType.EventText || pending.Type == AsciiFxRequestType.FloatingNumber)
                    && ++count >= MaxPendingEventTexts) return;
            var req = Rent();
            req.Type = AsciiFxRequestType.FloatingNumber;
            req.Zone = zone; req.Anchor = anchor; req.OriginCell = origin;
            req.X = origin.X; req.Y = origin.Y;
            req.Number = number;
            req.Text = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            req.ColorString = colorString;
            req.AllowOriginOnly = allowOriginOnly;
            req.Lifetime = .6f;
            req.Delay = float.IsNaN(delay) || float.IsInfinity(delay) ? 0f : System.Math.Max(0f, delay);
            PendingRequests.Enqueue(req);
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

        public static int PendingCount => PendingRequests.Count;
        /// <summary>Invalidates active presentation when a load or new world clears the bus.</summary>
        public static int ClearVersion { get; private set; }
        public static bool HasPendingBlocking
        {
            get
            {
                foreach (var request in PendingRequests)
                    if (request != null && request.BlocksTurnAdvance) return true;
                return false;
            }
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
            unchecked { ClearVersion++; }
            // Recycle dropped requests back into the pool so a Clear() that
            // happens mid-combat doesn't permanently shrink the pool.
            while (PendingRequests.Count > 0)
                Release(PendingRequests.Dequeue());
        }
    }
}
