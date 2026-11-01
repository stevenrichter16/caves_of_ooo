using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// P5 B6 — a colony answers as one. Canon (sarisarinama_bestiary_design.md): the Sima
    /// Pricklebrow's "swarm response makes them cheap living perimeter alarms"; the
    /// Sima-Steward's "defensive group response if their nesting cell is attacked: full
    /// colony swarms the attacker".
    ///
    /// <para>When a member is struck by a creature from outside the colony, every member of
    /// the same blueprint within <see cref="Radius"/> becomes personally hostile to the
    /// attacker. Only the strike raises the alarm: walking past a colony is safe. One call
    /// per tick, however many members are struck.</para>
    ///
    /// <para>Diag (ai): ColonyAlarm.</para>
    /// </summary>
    public sealed class ColonyPart : Part
    {
        public override string Name => "Colony";

        public int Radius = 8;
        public string Call = "The colony shrieks and turns on you!";
        public int LastAlarmTick = -1;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "TakeDamage" && e.GetParameter<Entity>("Source") is Entity source) Alarm(source);
            return true;
        }

        /// <summary>Raise the colony against <paramref name="attacker"/>. Returns how many
        /// members took it up.</summary>
        public int Alarm(Entity attacker)
        {
            var self = ParentEntity;
            var zone = self?.SpatialZone;
            if (zone == null || attacker == null || attacker == self || attacker.BlueprintName == self.BlueprintName
                || !attacker.HasTag("Creature")) return 0;
            int tick = WorldClock.CurrentTick;
            int raised = 0;
            foreach (var other in zone.GetAllEntities())
            {
                if (other.BlueprintName != self.BlueprintName || other.GetPart<ColonyPart>() == null
                    || other.GetStatValue("Hitpoints") <= 0 || SpatialQuery.Distance(zone, self, other) > Radius) continue;
                var brain = other.GetPart<BrainPart>();
                if (brain == null || brain.IsPersonallyHostileTo(attacker)) continue;
                brain.SetPersonallyHostile(attacker, alertAllies: false);
                raised++;
            }
            if (raised > 0 && LastAlarmTick != tick)
            {
                LastAlarmTick = tick;
                if (attacker.HasTag("Player")) MessageLog.Add(Call);
                if (Diag.IsChannelEnabled("ai")) Diag.Record("ai", "ColonyAlarm", self, attacker, new { raised, radius = Radius });
            }
            return raised;
        }
    }
}
