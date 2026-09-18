using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Kindle Flame — the weakest fire in the game: a gentle warmth
    /// pressed into an adjacent cell's low-flashpoint scenery. Port of
    /// <c>KindleFlameMutation</c>; taught by KindleFlameGrimoire,
    /// buyable in Pyromancy.
    ///
    /// <para>Verbatim: cooldown 2, <see cref="FireDose.Cantrip"/> (150J)
    /// to every non-creature ThermalPart occupant of the chosen cell
    /// whose FlameTemperature is under 250° (dry tinder — a torch can't
    /// light a stone). A cast that warms NOTHING is refused and free,
    /// exactly as the mutation only charged its cooldown when something
    /// was affected.</para>
    /// </summary>
    public class Pyromancy_KindleFlame : SpellSkillPart
    {
        public override string Name => nameof(Pyromancy_KindleFlame);

        public const int COOLDOWN = 2;
        public const float MAX_FLASHPOINT = 250f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Kindle Flame",
                Command = "CommandKindleFlame",
                Class = "Pyromancy",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }
            if (ctx.TargetCell == null) { EmitSkillRejectedDiag(ctx, "no_target_cell"); return false; }

            bool affectedAny = false;
            var targets = MultiCellAbilityQueries.SnapshotOccupants(new[] { ctx.TargetCell }, reverse: true);
            foreach (var entity in targets)
            {
                if (ctx.Zone.GetEntityCell(entity) == null) continue;
                if (entity.HasTag("Creature"))
                    continue;

                ThermalPart thermal = entity.GetPart<ThermalPart>();
                if (thermal == null || thermal.FlameTemperature >= MAX_FLASHPOINT)
                    continue;

                var heatEvent = GameEvent.New("ApplyHeat");
                heatEvent.SetParameter("Joules", (object)FireDose.Cantrip);
                heatEvent.SetParameter("Radiant", (object)false);
                heatEvent.SetParameter("Source", (object)ctx.Attacker);
                heatEvent.SetParameter("Zone", (object)ctx.Zone);
                SpellFxCapture.Target(ctx.Zone, entity);
                entity.FireEvent(heatEvent);
                heatEvent.Release();
                affectedAny = true;
            }

            if (!affectedAny)
            {
                EmitSkillRejectedDiag(ctx, "nothing_kindleable");
                return false; // free — nothing there could catch
            }
            return true;
        }
    }
}
