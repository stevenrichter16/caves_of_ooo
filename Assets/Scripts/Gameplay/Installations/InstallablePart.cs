namespace CavesOfOoo.Core
{
    /// <summary>
    /// Implemented by a Part that must follow its owner when it is installed somewhere new (Docs/MACHINE-ARCHITECTURE-SPEC.md C11). The v1 verbs do not
    /// need it (a box has nothing to rebind), so no shipped Part implements it yet; it is the seam a station kind with positional state would use.
    /// </summary>
    public interface IRebindable
    {
        /// <summary>False with a reason when the owner cannot be installed at the cell.</summary>
        bool CanInstallAt(Entity actor, Zone zone, int x, int y, out string reason);
        /// <summary>Re-points the Part at its new cell after a committed install. A failure rolls the whole install back.</summary>
        void Rebind(Zone zone, Cell cell);
    }

    /// <summary>
    /// The physical state of something that can be installed and taken up again (spec S19). It is independent of <c>PhysicsPart.Takeable</c>: a
    /// carried kit is <see cref="Loose"/>, a set-down one is <see cref="Installed"/>. Primitives only, reflection-saved by field name; an older save simply
    /// has none of it and its objects are not installable. A <see cref="Loose"/> state vetoes station readiness through the existing guard, so no station is edited.
    /// </summary>
    public sealed class InstallablePart : Part, IStationReadinessGuard
    {
        public const string Fixture = "Fixture", Portable = "Portable", Installed = "Installed", Loose = "Loose";
        /// <summary><see cref="Fixture"/> (not meant to be moved) or <see cref="Portable"/>.</summary>
        public string Mode = Fixture;
        /// <summary><see cref="Installed"/> or <see cref="Loose"/>.</summary>
        public string State = Installed;

        public bool IsLoose => State == Loose;

        public bool TryBlock(Entity station, Zone zone, out string code)
        {
            code = null;
            if (State != Loose) return false;
            code = "loose";
            return true;
        }
    }

    /// <summary>The install events, fired on the fixture. Before-events may veto (return false from a handler); after-events are observation only.</summary>
    public static class InstallEvents
    {
        public const string BeforeInstall = "BeforeInstall", Installed = "Installed", BeforeUninstall = "BeforeUninstall", Uninstalled = "Uninstalled";

        /// <summary>Fires a veto-able event on the fixture; false means a Part refused. Parameters: Actor, Station, Zone, X, Y, Origin.</summary>
        internal static bool Allow(string id, Entity actor, Entity station, Zone zone, int x, int y, string origin)
        {
            var e = GameEvent.New(id);
            e.SetParameter("Actor", (object)actor); e.SetParameter("Station", (object)station); e.SetParameter("Zone", (object)zone);
            e.SetParameter("X", x); e.SetParameter("Y", y); e.SetParameter("Origin", origin ?? "");
            return station.FireEventAndRelease(e);
        }

        /// <summary>Fires an observation-only event after commit.</summary>
        internal static void Notify(string id, Entity actor, Entity station, Zone zone, int x, int y, string origin)
        {
            var e = GameEvent.New(id);
            e.SetParameter("Actor", (object)actor); e.SetParameter("Station", (object)station); e.SetParameter("Zone", (object)zone);
            e.SetParameter("X", x); e.SetParameter("Y", y); e.SetParameter("Origin", origin ?? "");
            station.FireEventAndRelease(e);
        }
    }
}
