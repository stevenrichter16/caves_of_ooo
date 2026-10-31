using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// H1 (Docs/PLACE-WORK-AND-PROGRESSION.md §H1) — the oath, sworn. Canon
    /// (Lore/Factions/07_TentRight.md VII.4): a player in good standing may
    /// swear the oath and become a host, who must then protect anyone who
    /// claims their hospitality, "even when it is inconvenient, even when the
    /// guest is the player's enemy". Breaking the cloth is the one thing
    /// Tent-Right does not forgive, from either side.
    ///
    /// <para>State is two narrative facts, so it saves with the world:
    /// <see cref="SwornFact"/> and <see cref="OathbreakerFact"/>. The sworn
    /// host learns <c>Hospitality_ExtendCloth</c>; an oathbreaker loses it
    /// and is never named guest or sworn again.</para>
    ///
    /// <para>Diag: effect/OathSworn, effect/Oathbreak.</para>
    /// </summary>
    public static class TentRightOath
    {
        public const string SwornFact = "TentRight.OathSworn";
        public const string OathbreakerFact = "TentRight.Oathbreaker";
        public const string Faction = "TentRight";
        public const string TreeClass = "HospitalitySkill";
        public const string PowerClass = "Hospitality_ExtendCloth";
        public const int SwearingRepGain = 10;

        static int Fact(string key) => NarrativeStatePart.Current?.GetFact(key) ?? 0;

        public static bool IsSworn() => Fact(SwornFact) > 0 && !IsOathbreaker();
        public static bool IsOathbreaker() => Fact(OathbreakerFact) > 0;

        /// <summary>Why the oath cannot be sworn now, or null if it can.</summary>
        public static string RefusalFor(Entity player)
        {
            if (player == null) return "missing_listener";
            if (NarrativeStatePart.Current == null) return "missing_narrative_state";
            if (IsOathbreaker()) return "oathbreaker";
            if (IsSworn()) return "already_sworn";
            if ((int)PlayerReputation.GetAttitude(Faction) < (int)PlayerReputation.Attitude.Liked) return "not_liked";
            if (player.GetPart<SkillsPart>() == null) return "missing_skills";
            return null;
        }

        /// <summary>Swear the oath: the fact, the standing and the power.
        /// Returns null on success, else the refusal reason (nothing changes).</summary>
        public static string Swear(Entity player, Entity host)
        {
            string refusal = RefusalFor(player);
            if (refusal != null) { Record("OathRefused", player, host, refusal); return refusal; }
            var skills = player.GetPart<SkillsPart>();
            if (!skills.HasSkill(TreeClass)) skills.AddSkill(TreeClass, "oath");
            if (!skills.HasSkill(PowerClass) && !skills.AddSkill(PowerClass, "oath"))
            { Record("OathRefused", player, host, "power_refused"); return "power_refused"; }
            NarrativeStatePart.Current.SetFact(SwornFact, 1);
            PlayerReputation.Modify(Faction, SwearingRepGain);
            MessageLog.AddAnnouncement("You are sworn. Whoever takes your hem now is your guest.");
            Record("OathSworn", player, host, null);
            return null;
        }

        /// <summary>The player broke a cloth, as guest or as host. The largest
        /// single loss in the game, the oath withdrawn, and every guest the
        /// player hosts in this zone set loose (guests elsewhere follow on
        /// their next turn).</summary>
        public static void Oathbreak(Entity player, string side)
        {
            if (player == null) return;
            bool first = !IsOathbreaker();
            PlayerReputation.Modify(Faction, UnderTheClothEffect.OathbreakRepLoss);
            NarrativeStatePart.Current?.SetFact(OathbreakerFact, 1);
            var skills = player.GetPart<SkillsPart>();
            var power = skills?.GetSkill(PowerClass);
            if (power != null) skills.RemoveSkill(power, "oathbreak");
            var tree = skills?.GetSkill(TreeClass);
            if (tree != null) skills.RemoveSkill(tree, "oathbreak");
            int loosed = UnderTheClothEffect.Withdraw(player.ID, player.SpatialZone);
            if (first && Fact(SwornFact) > 0) MessageLog.AddAnnouncement("Your oath is withdrawn, and no tent will lift a cloth for you again.");
            Record("Oathbreak", player, null, side, loosed);
        }

        /// <summary>Canon (07 :152): an oathbreaker "is refused hospitality across
        /// all Tent-Right settlements", and in the Beating that exile is death,
        /// only slower. A Tent-Right place's wells refuse them water, both drinking
        /// and filling.</summary>
        public static bool RefusesWater(Entity actor, Zone zone)
        {
            if (actor == null || !actor.HasTag("Player") || !IsOathbreaker()) return false;
            string place = PlaceSiteCatalog.PlaceAt(zone?.ZoneID);
            if (place == null) return false;
            foreach (var p in WorldMapAuthoring.Places) if (p.Name == place) return p.Faction == Faction;
            return false;
        }

        public const string RefusedWaterLine = "A keeper stands at the well rope and does not move. Not for an oathbreaker.";

        static void Record(string kind, Entity player, Entity host, string reason, int loosed = 0)
        {
            if (!Diag.IsChannelEnabled("effect")) return;
            Diag.Record("effect", kind, actor: player, target: host, payload: new { reason, loosed });
        }
    }
}
