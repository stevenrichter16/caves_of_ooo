using System;

namespace CavesOfOoo.Core
{
    /// <summary>Presentation-only cue identities. No cue grants information or changes simulation state.</summary>
    public enum WorldAudioCue { StepSoft, StepStone, StepWater, Swing, Hit, Pickup, Equip, DoorOpen, DoorClose, Use, UiConfirm, UiCancel, Page, SwingBlade, SwingBlunt, SwingNatural, HitCutting, HitPiercing, HitBlunt, HitNatural, WorkForge, WorkRepairWood, WorkRepairStone, WorkRepairMetal, HarvestPlant, WaterFill, WaterDrink, WaterPour, CookPrepare }

    /// <summary>Small committed-action seam for sounds that cannot be inferred from generic damage or log text.
    /// Subscribers must be additive. Presentation failures never change a committed action's result.</summary>
    public static class WorldAudioHooks
    {
        public static Action<WorldAudioCue, Entity, Zone, int, int> Callback { get; set; }
        public static void Emit(WorldAudioCue cue, Entity actor, Zone zone, int x, int y)
        {
            if (Callback == null || actor == null || zone == null || !zone.InBounds(x,y)) return;
            try { Callback.Invoke(cue,actor,zone,x,y); }
            catch (Exception) { /* Optional presentation must never fail gameplay. */ }
        }
    }
}
