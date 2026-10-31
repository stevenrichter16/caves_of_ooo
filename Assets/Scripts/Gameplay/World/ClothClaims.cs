using System.Globalization;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// H1.4 — the claim. Canon (Lore/Factions/07_TentRight.md VII.4): a sworn
    /// host "experiences the obligation when someone claims their hospitality
    /// at an inconvenient moment", and cannot refuse. On entering open Beating
    /// ground (no POI, tier at most 3), a sworn player is sometimes reached
    /// by a hunted wayfarer with pursuers a few strides behind. The wayfarer
    /// takes the hem at once: the cloth goes on with the player as sworn host,
    /// the pursuers cannot touch the guest, and the host stands between them.
    /// Defending the guest is honoured (<see cref="ClothClaimantPart"/>).
    ///
    /// <para>Bounded like <see cref="WorldTravellers"/>: one saved roll per
    /// zone, at most <see cref="MaximumClaims"/> claims a game, rolls from a
    /// hash of seed and zone, so no gameplay RNG is consumed.
    /// Diag: worldgen/ClothClaimRoll.</para>
    /// </summary>
    public static class ClothClaims
    {
        public const int MaximumClaims = 3;
        public const int ChanceOneIn = 4;
        const string CountKey = "ClothClaimCount";
        const string RollPrefix = "ClothClaimRoll:";

        public static bool OnZoneEntered(Entity player, Zone zone)
        {
            if (!WorldAmbience.IsPresentPlayer(player, zone) || !TentRightOath.IsSworn()) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.Factory == null || !ReferenceEquals(manager.ActiveZone, zone)) return false;
            var (x, y, depth) = WorldMap.FromZoneID(zone.ZoneID);
            if (!WorldMapAuthoring.InBounds(x, y) || depth != 0 || WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Beating
                || WorldMapAuthoring.TierAt(x, y) > 3 || manager.WorldMap.GetPOI(x, y) != null) return false;
            string seed = manager.WorldSeed.ToString(CultureInfo.InvariantCulture);
            string canonical = WorldMap.ToZoneID(x, y, 0);
            string rollKey = RollPrefix + seed + ":" + canonical;
            int count = player.GetIntProperty(CountKey);
            if (count < 0 || count >= MaximumClaims || player.IntProperties.ContainsKey(rollKey)) return false;
            player.IntProperties[rollKey] = 1;
            uint sample = WorldRemarks.Hash(seed + ":clothclaim:" + canonical);
            if (sample % ChanceOneIn != 0) { Record(player, zone, "miss"); return false; }
            var at = zone.GetEntityCell(player);
            if (at == null || !FindCell(zone, at.X, at.Y, 1, 2, out int gx, out int gy)) { Record(player, zone, "no-room"); return false; }

            string id = "clothclaim:" + seed + ":" + canonical;
            var guest = manager.Factory.CreateEntity("Villager");
            if (guest == null) { Record(player, zone, "factory-refused"); return false; }
            guest.ID = id;
            var render = guest.GetPart<RenderPart>(); if (render != null) render.DisplayName = "hunted wayfarer";
            var talk = guest.GetPart<ConversationPart>(); if (talk != null) talk.ConversationID = "ClothClaimant_1";
            guest.RemovePart(guest.GetPart<TraderPart>());
            if (!zone.AddEntity(guest, gx, gy)) { Record(player, zone, "placement-refused"); return false; }
            var guestBrain = guest.GetPart<BrainPart>(); if (guestBrain != null) guestBrain.CurrentZone = zone;

            int pursuers = WorldMapAuthoring.TierAt(x, y) >= 3 ? 3 : 2;
            var claimant = new ClothClaimantPart();
            for (int i = 0; i < pursuers; i++)
            {
                if (!FindCell(zone, at.X, at.Y, 6 + i, 9, out int px, out int py)) break;
                var p = manager.Factory.CreateEntity("DesertBandit");
                if (p == null) break;
                p.ID = id + ":p" + i;
                var pr = p.GetPart<RenderPart>(); if (pr != null) pr.DisplayName = "pursuer";
                if (!zone.AddEntity(p, px, py)) continue;
                var brain = p.GetPart<BrainPart>();
                if (brain != null) { brain.CurrentZone = zone; brain.PersonalEnemies.Add(guest); }
                claimant.Pursuers = claimant.Pursuers == null ? p.ID : claimant.Pursuers + "," + p.ID;
            }
            guest.AddPart(claimant);
            // A host cannot refuse: the cloth goes on before the player can act.
            guest.ApplyEffect(new UnderTheClothEffect(player, sworn: true), player, null);
            MessageLog.AddAnnouncement("A hunted wayfarer reaches you and takes the hem of your cloth. \"Guest-right. I claim it.\"");
            player.IntProperties[CountKey] = count + 1;
            Record(player, zone, "claimed", id);
            return true;
        }

        static bool FindCell(Zone zone, int ox, int oy, int min, int max, out int x, out int y)
        {
            for (int r = min; r <= max; r++)
                for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                {
                    if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != r) continue;
                    x = ox + dx; y = oy + dy;
                    if (WorldTravellers.StandingCell(zone, x, y)) return true;
                }
            x = y = -1; return false;
        }

        static void Record(Entity player, Zone zone, string reason, string id = null)
        {
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "ClothClaimRoll", actor: player, payload: new { zone = zone.ZoneID, reason, entityId = id });
        }
    }
}
