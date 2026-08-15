namespace CavesOfOoo.Core
{
    /// <summary>
    /// One-shot farming-access grant for LOADED games. The starter kit
    /// (Watering Grimoire + seeds) and the spawn plot are granted at
    /// new-game bootstrap only — and no other source of seeds or the
    /// grimoire exists (no trade stock, no drops, produce doesn't yield
    /// seeds), so a save made before the crops feature shipped could
    /// NEVER access farming: a silent dead-end that contradicts the
    /// project's RPG identity (persistent characters carry forward into
    /// new features — Docs/PROJECT-IDENTITY.md). Audit finding SM7-F4,
    /// see <c>Docs/CROPS-WATERING-GRIMOIRE.md §5 SM7c</c>.
    ///
    /// <para><b>One-shot contract:</b> the grant fires at most once per
    /// character, pinned by <see cref="GrantedProperty"/> on the player
    /// entity (Properties round-trip through the save graph — the same
    /// mechanism knowledge-grimoires rely on). Two belt-and-suspenders
    /// guards protect saves made after the kit shipped but before this
    /// pin existed: a player already carrying any Seed item or the
    /// WateringGrimoire, or already knowing Conjure Rain, is treated as
    /// having access and only gets pinned, never re-granted.</para>
    /// </summary>
    public static class FarmingAccessGrant
    {
        /// <summary>Player-entity property marking the kit as granted.
        /// Set by GameBootstrap on BOTH the new-game grant and the
        /// load-path grant; checked before ever granting again.</summary>
        public const string GrantedProperty = "FarmingKitGranted";

        /// <summary>
        /// True when the loaded player should receive the one-shot
        /// farming kit + spawn plot: not yet pinned AND no existing
        /// farming access. Null-safe (false for null).
        /// </summary>
        public static bool ShouldGrantOnLoad(Entity player)
        {
            if (player == null) return false;
            if (player.Properties.ContainsKey(GrantedProperty)) return false;
            return !HasFarmingAccess(player);
        }

        /// <summary>
        /// True when the player can already exercise the farming loop:
        /// carries any Seed-part item or the WateringGrimoire, or knows
        /// Conjure Rain.
        /// </summary>
        public static bool HasFarmingAccess(Entity player)
        {
            if (player == null) return false;

            var inv = player.GetPart<InventoryPart>();
            if (inv != null)
            {
                for (int i = 0; i < inv.Objects.Count; i++)
                {
                    var item = inv.Objects[i];
                    if (item == null) continue;
                    if (item.GetPart<SeedPart>() != null) return true;
                    if (item.BlueprintName == "WateringGrimoire") return true;
                }
            }

            var skills = player.GetPart<CavesOfOoo.Skills.SkillsPart>();
            if (skills != null && skills.HasSkill("Hydromancy_ConjureRain"))
                return true;

            return false;
        }

        /// <summary>Pin the player as granted. Idempotent, null-safe.</summary>
        public static void MarkGranted(Entity player)
        {
            if (player == null) return;
            player.Properties[GrantedProperty] = "true";
        }
    }
}
