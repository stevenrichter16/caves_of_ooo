using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// H1.4 — a wayfarer who claimed the player's cloth (<see cref="ClothClaims"/>).
    /// When none of the pursuers is left standing in the zone while the guest
    /// lives under the player's cloth, the defence is honoured once: the guest
    /// pays what they carry (<see cref="Gift"/> drams) and Tent-Right standing
    /// rises (canon: standing is "gained by ... defending guests").
    /// Diag: effect/ClaimHonored.
    /// </summary>
    public class ClothClaimantPart : Part
    {
        public const int Gift = 40;
        public const int HonorRep = 5;

        public override string Name => "ClothClaimant";

        /// <summary>Comma-separated pursuer entity IDs. Public for save-reach.</summary>
        public string Pursuers;
        public bool Honored;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "EndTurn" && !Honored) TryHonor();
            return true;
        }

        /// <summary>Honour the defence if it is done. Public for tests.</summary>
        public bool TryHonor()
        {
            var guest = ParentEntity;
            var zone = guest?.SpatialZone;
            if (Honored || zone == null || guest.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(guest)) return false;
            var cloth = guest.GetEffect<UnderTheClothEffect>();
            if (cloth == null || !cloth.HostSworn) return false;
            Entity host = null;
            foreach (var e in zone.GetReadOnlyEntities())
                if (e.ID == cloth.HostId && e.HasTag("Player")) { host = e; break; }
            if (host == null || !TentRightOath.IsSworn()) return false;
            if (!string.IsNullOrEmpty(Pursuers))
                foreach (var e in zone.GetReadOnlyEntities())
                    if (("," + Pursuers + ",").Contains("," + e.ID + ",")
                        && e.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(e)) return false;
            Honored = true;
            TradeSystem.SetDrams(host, TradeSystem.GetDrams(host) + Gift);
            PlayerReputation.Modify(TentRightOath.Faction, HonorRep);
            MessageLog.AddAnnouncement(guest.GetDisplayName() + " says, \"You stood between. Take this; it is all I carry that is mine. Ask me my road on the third day, and I will tell you.\"");
            if (Diag.IsChannelEnabled("effect"))
                Diag.Record("effect", "ClaimHonored", actor: host, target: guest, payload: new { gift = Gift, rep = HonorRep });
            return true;
        }
    }
}
