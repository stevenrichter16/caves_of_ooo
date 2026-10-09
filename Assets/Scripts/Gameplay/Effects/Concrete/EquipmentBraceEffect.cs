using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A saved stance against one physical displacement, never a general teleport veto.</summary>
    public sealed class EquipmentBraceEffect : Effect
    {
        public Entity Equipment;
        public Entity Handhold;
        public string ZoneID;
        public int OriginX, OriginY;
        public bool Armed = true;
        public override string DisplayName => "braced";
        public EquipmentBraceEffect() { Duration = 3; }
        public override int GetEffectType() => TYPE_ACTIVITY | TYPE_EQUIPMENT | TYPE_VOLUNTARY;
        public override bool OnStack(Effect incoming) => true;
        public bool IsCurrent(Entity actor, Zone zone)
        {
            var origin = zone?.GetEntityCell(actor);
            return Duration > 0 && Armed && WorldResourceActions.ActorCurrent(actor, zone)
                && origin != null && zone.ZoneID == ZoneID && origin.X == OriginX && origin.Y == OriginY
                && (Equipment?.BlueprintName == "GripfrondWrap"
                    ? EquipmentUtilityActions.Equipped(actor, Equipment, "Handwear") && EquipmentUtilityActions.Handhold(actor, Handhold, zone)
                    : Equipment?.BlueprintName == "IronshodBoots" && EquipmentUtilityActions.Equipped(actor, Equipment, "Feet")
                        && EquipmentUtilityActions.StableFeet(actor, zone));
        }
        public static bool TryAbsorb(Entity actor, Zone zone, bool deferRemoval = false)
        {
            var manager = actor?.GetPart<StatusEffectsPart>();
            var brace = manager?.GetEffect<EquipmentBraceEffect>();
            if (brace == null || !brace.Armed) return false;
            bool valid = brace.IsCurrent(actor, zone);
            // Hooked runs inside the effect list's reverse EndTurn loop. Mark
            // this earlier stance expired there; removing it shifts Hooked and
            // would tick its pull/duration a second time in the same turn.
            if(deferRemoval){brace.Duration=0;brace.Armed=false;}
            else manager.RemoveEffect(brace);
            if (valid)
            {
                MessageLog.Add(actor.GetDisplayName() + " holds firm against the shove; the brace releases.");
                Diag.Record("event", "EquipmentBraceAbsorbed", actor, brace.Equipment);
            }
            return valid;
        }
        public static void CancelOnMovement(Entity actor)
        {
            var manager = actor?.GetPart<StatusEffectsPart>();
            var brace = manager?.GetEffect<EquipmentBraceEffect>();
            if (brace != null && brace.Duration > 0) manager.RemoveEffect(brace);
        }
        public override void OnTurnEnd(Entity target, GameEvent context)
        {
            if (!IsCurrent(target, context?.GetParameter<Zone>("Zone"))) Duration = 0;
            else base.OnTurnEnd(target, context);
        }
    }
}
