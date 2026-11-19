using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>How strictly a station kind is checked. Strict is the forge's rule (the station must be a ready, parented, placed, unfaulted owner of the current zone);
    /// PresenceOnly is the alchemy still's older rule (a provider Part on an object in the 3x3 is enough). PresenceOnly stays until a later slice proves the difference is unreachable in shipped play.</summary>
    public enum ReadinessMode { Strict = 0, PresenceOnly = 1 }

    /// <summary>Implemented by a Part that makes its owner a station of one or more kinds ("forge", "cold-work", "still"...). A station is found by capability,
    /// not by concrete Part type, so a new bench kind needs no edit to the scanners. Stateless and pure.</summary>
    public interface IStationCapability
    {
        IReadOnlyList<string> Kinds { get; }
        /// <summary>Chebyshev reach in cells; 1 for every shipped station (the 3x3 box).</summary>
        int Reach { get; }
        ReadinessMode Readiness { get; }
    }

    /// <summary>Implemented by any Part that owns a physical veto on its owner's readiness (the rig payload today; fuel, loose state later). Pure: no mutation, no events.</summary>
    public interface IStationReadinessGuard
    {
        bool TryBlock(Entity station, Zone zone, out string code);
    }

    /// <summary>The permission seam. With no guard on the station, use is allowed, so every shipped station keeps today's open behaviour. Enforcement semantics
    /// (terms, fees, gifts) arrive with the service-terms slice after its independent critique. Pure.</summary>
    public interface IStationAccessGuard
    {
        bool TryDeny(Entity actor, Entity station, Zone zone, string kind, string command, out string code, out string reason);
    }

    /// <summary>A station found near an actor, with the reason it is not ready (null when ready).</summary>
    public readonly struct StationCandidate
    {
        public readonly Entity Station;
        public readonly string BlockCode;
        public StationCandidate(Entity station, string blockCode) { Station = station; BlockCode = blockCode; }
        public bool Ready => BlockCode == null;
    }
}
