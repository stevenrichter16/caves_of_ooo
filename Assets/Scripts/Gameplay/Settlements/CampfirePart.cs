using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part attached to campfire entities that provides:
    /// - Color flicker between red and yellow (the campfire itself looks alive)
    /// - Ember aura (dots that pop on surrounding cells and vanish)
    /// - Proximity ambient message (one-shot per zone visit)
    /// </summary>
    public class CampfirePart : Part
    {
        public override string Name => "Campfire";

        private int _renderFrameCounter;
        private bool _proximityMessageShown;
        private bool _auraStarted;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "Render")
                return HandleRender(e);
            if (e.ID == "EndTurn")
                return HandleEndTurn(e);
            return true;
        }

        private bool HandleRender(GameEvent e)
        {
            _renderFrameCounter++;

            // Flicker between red and yellow to simulate burning
            // Mostly red, with yellow flashes every ~5 frames
            if (_renderFrameCounter % 5 == 0)
                e.SetParameter("ColorString", "&Y");
            else if (_renderFrameCounter % 13 == 0)
                e.SetParameter("ColorString", "&W");

            return true;
        }

        private bool HandleEndTurn(GameEvent e)
        {
            if (_proximityMessageShown || ParentEntity == null)
                return true;

            Zone zone = SettlementRuntime.ActiveZone;
            if (zone == null)
                return true;

            Cell fireCell = zone.GetEntityCell(ParentEntity);
            if (fireCell == null)
                return true;

            Entity player = FindPlayer(zone);
            if (player == null)
                return true;

            Cell playerCell = zone.GetEntityCell(player);
            if (playerCell == null)
                return true;

            if (IsAdjacent(fireCell.X, fireCell.Y, playerCell.X, playerCell.Y))
            {
                _proximityMessageShown = true;
                MessageLog.Add("The campfire crackles warmly.");
            }

            return true;
        }

        /// <summary>
        /// Start the ember aura. Stops any existing aura first so the request
        /// is always fresh — this matters when called after the renderer's zone
        /// is set (e.g. from RefreshActiveZonePresentation).
        /// </summary>
        public void StartAura(Zone zone)
        {
            Debug.Log($"[Campfire/Debug] StartAura called. zone={zone?.ZoneID ?? "null"}, entity={ParentEntity?.BlueprintName ?? "null"}, alreadyStarted={_auraStarted}");

            if (zone == null || ParentEntity == null)
            {
                Debug.LogWarning($"[Campfire/Debug] StartAura early-out: zone={zone != null}, entity={ParentEntity != null}");
                return;
            }

            if (_auraStarted)
            {
                Debug.Log("[Campfire/Debug] Stopping old aura before restart");
                AsciiFxBus.StopAura(ParentEntity, AsciiFxTheme.Campfire);
                _auraStarted = false;
            }

            Debug.Log($"[Campfire/Debug] Emitting AuraStart for Campfire theme on zone {zone.ZoneID}");
            AsciiFxBus.StartAura(zone, ParentEntity, AsciiFxTheme.Campfire);
            _auraStarted = true;
        }

        /// <summary>
        /// Stop the ember aura. Called on zone teardown.
        /// </summary>
        public void StopAura()
        {
            if (!_auraStarted || ParentEntity == null)
                return;

            AsciiFxBus.StopAura(ParentEntity, AsciiFxTheme.Campfire);
            _auraStarted = false;
        }

        public void ResetProximityMessage()
        {
            _proximityMessageShown = false;
        }

        private static Entity FindPlayer(Zone zone)
        {
            if (zone == null)
                return null;

            var entities = zone.GetAllEntities();
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].HasTag("Player"))
                    return entities[i];
            }
            return null;
        }

        private static bool IsAdjacent(int x1, int y1, int x2, int y2)
        {
            int dx = x1 - x2;
            int dy = y1 - y2;
            if (dx < 0) dx = -dx;
            if (dy < 0) dy = -dy;
            return dx <= 1 && dy <= 1 && (dx + dy > 0);
        }
    }
}
