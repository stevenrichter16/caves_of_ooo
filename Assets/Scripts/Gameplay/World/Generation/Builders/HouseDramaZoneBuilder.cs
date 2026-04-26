using System.Collections.Generic;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Spawns the living NPC roles of an assigned House Drama into a village zone.
    /// Priority 4500 — runs after VillagePopulationBuilder (4000) so drama NPCs
    /// layer on top of the baseline village population without disrupting it.
    ///
    /// Dead roles (FoundationalDead, LostDead) are skipped — they have no live entity.
    /// Each spawned NPC receives a HouseDramaPart (drama identity) and has its
    /// ConversationPart.ConversationID set to "Drama_{DramaID}_{Role}".
    /// </summary>
    public class HouseDramaZoneBuilder : IZoneBuilder
    {
        public string Name => "HouseDramaZoneBuilder";
        public int Priority => 4500;

        private readonly string _dramaId;

        // Role → fallback blueprint when NpcRoleData.BlueprintOverride is absent.
        private static readonly Dictionary<string, string> RoleBlueprints = new Dictionary<string, string>
        {
            { "DiminishedHead",  "Elder"    },
            { "RisingInheritor", "Villager" },
            { "NamedAntagonist", "Merchant" },
            { "SilencedHelper",  "Scribe"   },
        };

        private static readonly HashSet<string> DeadRoles = new HashSet<string>
        {
            "FoundationalDead",
            "LostDead",
        };

        public HouseDramaZoneBuilder(string dramaId)
        {
            _dramaId = dramaId;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (string.IsNullOrEmpty(_dramaId)) return true;

            var data = HouseDramaLoader.Get(_dramaId);
            if (data == null)
            {
                Debug.LogWarning($"[HouseDramaZoneBuilder] Drama '{_dramaId}' not found in loader.");
                return true;
            }

            if (!HouseDramaRuntime.IsDramaActive(_dramaId))
            {
                HouseDramaRuntime.RegisterDrama(data);
                HouseDramaRuntime.ActivateDrama(_dramaId);
            }

            if (data.NpcRoles == null || data.NpcRoles.Count == 0)
                return true;

            var interiorCells = GatherInteriorCells(zone);
            var openCells = GatherOpenCells(zone);

            foreach (var role in data.NpcRoles)
            {
                if (string.IsNullOrEmpty(role.Id) || string.IsNullOrEmpty(role.Role))
                    continue;
                if (DeadRoles.Contains(role.Role))
                    continue;
                if (!role.Alive)
                    continue;

                string blueprint = ResolveBlueprint(role, factory);
                if (string.IsNullOrEmpty(blueprint))
                {
                    Debug.LogWarning($"[HouseDramaZoneBuilder] No blueprint for role '{role.Role}' (NpcId: {role.Id}) in drama '{_dramaId}'; skipping.");
                    continue;
                }

                Entity npc = PlaceNPCInInterior(zone, factory, rng, interiorCells, openCells, blueprint);
                if (npc == null) continue;

                var part = new HouseDramaPart
                {
                    DramaID = _dramaId,
                    NpcRole = role.Role,
                    NpcId   = role.Id,
                };
                npc.AddPart(part);

                var conv = npc.GetPart<ConversationPart>();
                if (conv != null)
                    conv.ConversationID = $"Drama_{_dramaId}_{role.Role}";
            }

            Debug.Log($"[HouseDramaZoneBuilder] Seeded drama '{_dramaId}' into zone '{zone.ZoneID}'.");
            return true;
        }

        private string ResolveBlueprint(NpcRoleData role, EntityFactory factory)
        {
            if (!string.IsNullOrEmpty(role.BlueprintOverride) &&
                factory.Blueprints.ContainsKey(role.BlueprintOverride))
                return role.BlueprintOverride;

            if (RoleBlueprints.TryGetValue(role.Role, out var fallback) &&
                factory.Blueprints.ContainsKey(fallback))
                return fallback;

            return null;
        }

        private Entity PlaceNPCInInterior(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> interiorCells, List<(int x, int y)> openCells, string blueprint)
        {
            if (interiorCells.Count > 0)
            {
                int idx = rng.Next(interiorCells.Count);
                var (x, y) = interiorCells[idx];
                Entity npc = TryCreateEntity(factory, blueprint);
                if (npc != null)
                {
                    zone.AddEntity(npc, x, y);
                    interiorCells.RemoveAt(idx);
                    openCells.Remove((x, y));
                    return npc;
                }
            }

            if (openCells.Count > 0)
            {
                int idx = rng.Next(openCells.Count);
                var (x, y) = openCells[idx];
                Entity npc = TryCreateEntity(factory, blueprint);
                if (npc != null)
                {
                    zone.AddEntity(npc, x, y);
                    openCells.RemoveAt(idx);
                    return npc;
                }
            }

            return null;
        }

        private static Entity TryCreateEntity(EntityFactory factory, string blueprint)
        {
            if (factory == null || string.IsNullOrEmpty(blueprint) || !factory.Blueprints.ContainsKey(blueprint))
                return null;
            return factory.CreateEntity(blueprint);
        }

        private static List<(int x, int y)> GatherInteriorCells(Zone zone)
        {
            var cells = new List<(int x, int y)>();
            zone.ForEachCell((cell, x, y) =>
            {
                if (!cell.IsPassable()) return;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].BlueprintName == "StoneFloor")
                    {
                        cells.Add((x, y));
                        break;
                    }
                }
            });
            return cells;
        }

        private static List<(int x, int y)> GatherOpenCells(Zone zone)
        {
            var cells = new List<(int x, int y)>();
            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsPassable())
                    cells.Add((x, y));
            });
            return cells;
        }
    }
}
