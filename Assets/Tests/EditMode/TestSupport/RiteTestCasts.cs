using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Migration M4: the rites are skills. These extensions keep the
    /// mutation-era <c>rite.Cast(...)</c> call sites verbatim across the
    /// Magic test suites while routing through the dispatcher with the
    /// InputHandler's parameter shapes. The cooldown is zeroed before
    /// each cast — the old direct-Cast path never checked its own
    /// cooldown either.
    /// </summary>
    internal static class RiteTestCasts
    {
        public static bool Cast(this ConsumingRiteSkillBase rite,
            Zone zone, int dx, int dy)
        {
            var caster = rite.ParentEntity;
            if (caster == null) return false;

            var abilities = caster.GetPart<ActivatedAbilitiesPart>();
            if (abilities != null)
                for (int i = 0; i < abilities.AbilityList.Count; i++)
                    if (abilities.AbilityList[i].Command == rite.CommandName)
                        abilities.AbilityList[i].CooldownRemaining = 0;

            var cmd = GameEvent.New(rite.CommandName);
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("RNG", (object)new System.Random(7));
            if (zone != null)
                cmd.SetParameter("SourceCell", (object)zone.GetEntityCell(caster));
            cmd.SetParameter("DirectionX", dx);
            cmd.SetParameter("DirectionY", dy);
            caster.FireEvent(cmd);
            bool handled = cmd.Handled;
            cmd.Release();
            return handled;
        }

        /// <summary>SelfCentered mutation shape — the cell argument was
        /// always the caster's own cell; the skill spine centers on the
        /// caster's position itself.</summary>
        public static bool Cast(this ConsumingRiteSkillBase rite,
            Zone zone, Cell sourceCell)
            => Cast(rite, zone, 0, 0);

        /// <summary>Self shape (ScaldingVeil).</summary>
        public static bool Cast(this ConsumingRiteSkillBase rite, Zone zone)
            => Cast(rite, zone, 0, 0);
    }
}
