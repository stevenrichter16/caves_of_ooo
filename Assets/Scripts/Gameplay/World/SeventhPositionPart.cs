using CavesOfOoo.Diagnostics;
using Unity.Profiling;

namespace CavesOfOoo.Core
{
    /// <summary>Local, non-intentional slippage at one empty point. Called
    /// after the ending player's status cleanup, so waiting works and the
    /// brief nonstacking aftereffect expires normally after leaving.</summary>
    public sealed class SeventhPositionPart : Part
    {
        public override string Name => "SeventhPosition";
        public const int ExposureDuration = 2;
        private static readonly ProfilerMarker Marker = new ProfilerMarker("COO.Turns.FellingExposure");

        /// <summary>Checks only the ending player's occupied cell. NPC ends,
        /// absent actors and ordinary cells have no exposure or world effects.</summary>
        public static void OnPlayerTurnEnded(Entity actor, Zone zone)
        {
            if (actor == null || !actor.HasTag("Player") || zone == null) return;
            using (Marker.Auto())
            {
                var cell = zone.GetEntityCell(actor); if (cell == null) return;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var part = cell.Objects[i].GetPart<SeventhPositionPart>();
                    if (part == null) continue;
                    part.TryAffectStandingPlayer(actor, zone);
                    return; // Multiple malformed markers still mean one point.
                }
            }
        }

        /// <summary>Revalidates live membership before applying. Respects
        /// existing confusion and normal immunity/veto hooks; never forces it.</summary>
        /// <summary>ES.4: the empty seventh offers its two enactments underfoot; once one is
        /// enacted the exposure ends — the near and far edges of the circle agree.</summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
                EndingSpine.AddActions(e.GetParameter<InventoryActionList>("Actions"), CavesOfOoo.Storylets.StoryletPart.LocalPlayer);
            return true;
        }

        public bool TryAffectStandingPlayer(Entity actor, Zone zone)
        {
            if (EndingSpine.Enacted(actor) != 0) return false;
            if (actor == null || !actor.HasTag("Player") || zone == null || ParentEntity == null
                || actor.GetStatValue("Hitpoints", 0) <= 0) return false;
            var cell = zone.GetEntityCell(actor);
            if (cell == null || zone.GetEntityCell(ParentEntity) != cell
                || !cell.Objects.Contains(actor) || !cell.Objects.Contains(ParentEntity)) return false;
            var effects = actor.GetPart<StatusEffectsPart>();
            if (effects?.HasEffect<ConfusedEffect>() == true) return false;
            var exposure = new ConfusedEffect(ExposureDuration);
            if (!actor.ApplyEffect(exposure, source: ParentEntity, zone: zone)) return false;
            // This application follows, rather than precedes, owner EndTurn
            // cleanup. Keeping its mid-action flag would add a third action.
            exposure.JustApplied = false;
            MessageLog.Add("At the empty position, near and far slip out of agreement.");
            if (Diag.IsChannelEnabled("event")) Diag.Record("event", "FellingExposure", target: actor,
                payload: new { zone = zone.ZoneID, x = cell.X, y = cell.Y, duration = ExposureDuration });
            return true;
        }
    }
}
