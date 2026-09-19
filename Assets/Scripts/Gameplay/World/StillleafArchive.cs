using System;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The Stillleaf Archive, the first middle-game chain
    /// (Docs/MIDGAME-STILLLEAF-ARCHIVE.md). SA.1 supplies what the sealed
    /// library was missing: the keeper's key (an item whose id matches the
    /// door's lock) and the one contested record inside the vault.
    ///
    /// <para>The register is placed on fresh generation of Stillleaf's floor
    /// only. A persisted latch on the door outlives the item, so a taken or
    /// destroyed register is never replaced — the record is finite. Existing
    /// saves whose floor was generated earlier are not migrated.</para>
    /// </summary>
    public static class StillleafArchive
    {
        public const string KeyBlueprint = "StillleafKey";
        public const string RegisterBlueprint = "StillleafRegister";
        public const string RegisterId = "stillleaf-archive:register";
        private const string Installed = "StillleafRegisterInstalled";

        /// <summary>Place the register on the vault floor, as far from the door
        /// as the enclosure allows. Returns false (with a diag reason) when the
        /// zone is not Stillleaf's floor, the vault is absent, content is
        /// missing, or the register was already installed once.</summary>
        public static bool TryInstallVault(Zone zone, EntityFactory factory)
        {
            if (zone?.ZoneID != SealedLibraryBuilder.ZoneID || factory == null) return Refuse(zone, "not_stillleaf_floor");
            Entity door = null;
            foreach (var e in zone.GetReadOnlyEntities())
                if (e.BlueprintName == "SealedLibraryDoor") { door = e; break; }
            if (door == null) return Refuse(zone, "no_vault");
            if (door.GetIntProperty(Installed) == 1) return Refuse(zone, "already_installed");
            if (!factory.Blueprints.ContainsKey(RegisterBlueprint)) return Refuse(zone, "missing_register_blueprint");

            var d = zone.GetEntityPosition(door);
            Cell seat = null; int best = -1;
            foreach (var e in zone.GetReadOnlyEntities())
            {
                if (e.BlueprintName != "SealedLibraryFloor") continue;
                var c = zone.GetEntityCell(e);
                if (c == null || c.BlocksMovement()) continue;
                bool bare = true;
                foreach (var o in c.Objects) if (o.BlueprintName != "SealedLibraryFloor") { bare = false; break; }
                if (!bare) continue;
                int distance = Math.Abs(c.X - d.x) + Math.Abs(c.Y - d.y);
                // Deterministic: deepest cell, ties broken by position.
                if (distance > best || (distance == best && seat != null && (c.X < seat.X || (c.X == seat.X && c.Y < seat.Y))))
                { best = distance; seat = c; }
            }
            if (seat == null) return Refuse(zone, "no_free_archive_floor");

            var register = factory.CreateEntity(RegisterBlueprint);
            if (register?.GetPart<PhysicsPart>() == null || register.GetPart<ExaminablePart>() == null)
                return Refuse(zone, "register_incomplete");
            register.ID = RegisterId;
            if (!zone.AddEntity(register, seat.X, seat.Y)) return Refuse(zone, "placement_failed");
            door.SetIntProperty(Installed, 1);
            Diag.Record("worldgen", "StillleafRegisterPlaced", target: register,
                payload: new { zone = zone.ZoneID, x = seat.X, y = seat.Y, fromDoor = best });
            return true;
        }

        private static bool Refuse(Zone zone, string reason)
        {
            Diag.Record("worldgen", "StillleafRegisterRefused", payload: new { zone = zone?.ZoneID, reason });
            return false;
        }
    }
}
