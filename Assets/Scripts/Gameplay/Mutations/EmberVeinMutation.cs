using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Grimoire spell: Ember Vein.
    /// Beam spell that scorches creatures in its line for 2d6 each and
    /// fires ApplyHeat(+150J) at every ThermalPart entity in every path
    /// cell. Low-ignition combustibles (crates, oil slicks) ignite
    /// directly from the heat pulse — a row of crates lit by a single
    /// cast is the signature moment. Beam passes through creatures
    /// (TraceBeam only stops on solid blockers) so a creature at range
    /// 2 doesn't shield a prop at range 4.
    /// </summary>
    public class EmberVeinMutation : BaseMutation
    {
        public const string COMMAND = "CommandEmberVein";
        public const int RANGE = 7;
        public const int COOLDOWN = 12;
        private const float ChargeDuration = 0.08f;
        private const float BeamDuration = 0.12f;

        public override string Name => "Ember Vein";
        public override string MutationType => "Mental";
        public override string DisplayName => "Ember Vein";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ActivatedAbilityID = AddMyActivatedAbility(
                DisplayName,
                COMMAND,
                "Grimoire Spells",
                AbilityTargetingMode.DirectionLine,
                RANGE);
        }

        public override void Unmutate(Entity entity)
        {
            RemoveMyActivatedAbility(ActivatedAbilityID);
            ActivatedAbilityID = Guid.Empty;
            base.Unmutate(entity);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != COMMAND)
                return true;

            Zone zone = e.GetParameter<Zone>("Zone");
            Cell sourceCell = e.GetParameter<Cell>("SourceCell");
            System.Random rng = e.GetParameter<System.Random>("RNG") ?? new System.Random();
            int dx = e.GetIntParameter("DirectionX");
            int dy = e.GetIntParameter("DirectionY");

            if (!Cast(zone, sourceCell, dx, dy, rng))
                return true;

            e.SetParameter("BlocksTurnAdvance", true);
            e.Handled = true;
            return false;
        }

        public bool Cast(Zone zone, Cell sourceCell, int dx, int dy, System.Random rng)
        {
            if (ParentEntity == null || zone == null || sourceCell == null || (dx == 0 && dy == 0))
                return false;

            BeamTraceResult trace = SpellTargeting.TraceBeam(
                zone, ParentEntity, sourceCell.X, sourceCell.Y, dx, dy, RANGE);
            if (trace.Path.Count == 0)
                return false;

            AsciiFxBus.EmitChargeOrbit(zone, ParentEntity, radius: 1, duration: ChargeDuration,
                AsciiFxTheme.Fire, blocksTurnAdvance: true);
            AsciiFxBus.EmitBeam(zone, trace.Path, dx, dy, AsciiFxTheme.Fire,
                duration: BeamDuration, blocksTurnAdvance: true, delay: ChargeDuration);

            Point impact = trace.GetImpactPoint();
            if (impact.X >= 0)
            {
                AsciiFxBus.EmitBurst(zone, impact.X, impact.Y, AsciiFxTheme.Fire,
                    blocksTurnAdvance: true, delay: ChargeDuration + BeamDuration);
            }

            // 1. Damage creatures the beam passed through.
            for (int i = 0; i < trace.HitEntities.Count; i++)
            {
                Entity target = trace.HitEntities[i];
                int damage = DiceRoller.Roll("2d6", rng);
                if (damage <= 0)
                    continue;

                MessageLog.Add(
                    ParentEntity.GetDisplayName() + " scorches " +
                    target.GetDisplayName() + " for " + damage + " damage!");
                CombatSystem.ApplyDamage(target, damage, ParentEntity, zone);
            }

            // 2. Per-cell heat pass: +150J to every ThermalPart entity in the
            // beam's path. Reverse-iterate cell.Objects and re-fetch Count each
            // step because a shatter or reaction can mutate the collection
            // mid-loop (spawned steam clouds, removed entities). Matches
            // ConflagrationMutation's iteration-safety pattern.
            for (int p = 0; p < trace.Path.Count; p++)
            {
                Point point = trace.Path[p];
                Cell cell = zone.GetCell(point.X, point.Y);
                if (cell == null)
                    continue;

                for (int i = cell.Objects.Count - 1; i >= 0; i--)
                {
                    if (i >= cell.Objects.Count) continue;
                    Entity entity = cell.Objects[i];
                    if (entity == ParentEntity)
                        continue;

                    if (entity.HasPart<ThermalPart>())
                    {
                        var heatEvent = GameEvent.New("ApplyHeat");
                        heatEvent.SetParameter("Joules", (object)150f);
                        heatEvent.SetParameter("Radiant", (object)false);
                        heatEvent.SetParameter("Source", (object)ParentEntity);
                        heatEvent.SetParameter("Zone", (object)zone);
                        entity.FireEvent(heatEvent);
                        heatEvent.Release();
                    }
                }
            }

            CooldownMyActivatedAbility(ActivatedAbilityID, COOLDOWN);
            return true;
        }
    }
}
