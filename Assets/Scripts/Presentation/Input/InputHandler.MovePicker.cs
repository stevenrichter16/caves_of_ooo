using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// The cell picker behind a set-down kit's single "move" row. The cursor starts on the kit and moves with the arrow keys (or the mouse) within <see cref="HeldFixtureActions.MoveReach"/>
    /// of the player; its border is green where the kit fits and red where it does not, decided by the same <see cref="HeldFixtureActions.CanMoveTo"/> the move itself runs, so the colour is
    /// never a promise the move breaks. Enter on a green cell moves it as one action; Enter on a red one says why and keeps the picker open; Escape leaves it. It reuses the cursor, range
    /// and mouse handling of the throw and vault cursors and the same awaiting-direction state, so the hotbar, controller and renderer plumbing is shared.
    /// </summary>
    public partial class InputHandler
    {
        Entity _moveFixture, _moveActor; Zone _moveZone;
        bool IsMoveTargeting => _moveFixture != null && _worldCursorState.Active && _worldCursorState.Mode == WorldCursorMode.PlaceTarget;

        /// <summary>Starts the picker on the kit. False (with a message) when the kit cannot be moved from here, so the caller can fall back to where it was.</summary>
        bool BeginMoveTargeting(Entity fixture)
        {
            var zone = CurrentZone; var origin = zone == null || PlayerEntity == null ? null : zone.GetEntityCell(PlayerEntity); var cell = zone?.GetEntityCell(fixture);
            if (fixture == null || origin == null || cell == null || !HeldFixtureActions.IsPlayerHeld(fixture) || fixture.GetPart<PackableFixturePart>() == null)
            { MessageLog.Add("That is no longer something you can move."); return false; }
            _moveFixture = fixture; _moveActor = PlayerEntity; _moveZone = zone;
            _worldCursorState.Activate(WorldCursorMode.PlaceTarget, zone, cell.X, cell.Y, origin.X, origin.Y, HeldFixtureActions.MoveReach, true);
            RefreshMoveValidity();
            _lastLookMousePosition = Input.mousePosition; _inputState = InputState.AwaitingDirection;
            SyncHotbarState();
            MessageLog.Add("Move " + fixture.GetDisplayName() + ": pick a cell within " + HeldFixtureActions.MoveReach + " of you with the arrow keys. Green = it fits, red = it does not. Enter moves it, Escape cancels.");
            return true;
        }

        void ExitMoveTargeting()
        {
            _worldCursorState.Deactivate(); ZoneRenderer?.ClearWorldCursor();
            _moveFixture = null; _moveActor = null; _moveZone = null;
            _inputState = InputState.Normal; SyncHotbarState();
        }

        /// <summary>Recolours the border for the cell under the cursor.</summary>
        void RefreshMoveValidity()
        {
            if (_moveFixture == null) return;
            _worldCursorState.SetPlaceValid(HeldFixtureActions.CanMoveTo(_moveActor, _moveFixture, _moveZone, _worldCursorState.X, _worldCursorState.Y, out _));
            ZoneRenderer?.SetWorldCursorState(_worldCursorState, PlayerEntity);
        }

        void HandleMoveTargeting()
        {
            if (PlayerEntity != _moveActor || CurrentZone != _moveZone || !IsMoveTargeting) { ExitMoveTargeting(); return; }
            if (InputHelper.GetKeyDown(KeyCode.Escape)) { MessageLog.Add("You leave it where it is."); ExitMoveTargeting(); return; }
            if (GetDirectionKeyDown(out int dx, out int dy)) { MoveThrowCursor(dx, dy); RefreshMoveValidity(); _lastLookMousePosition = Input.mousePosition; return; }
            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.KeypadEnter)) { ConfirmMoveTarget(); return; }
            if (Input.GetMouseButtonDown(0)) { if (TryUpdateThrowCursorFromMouse(false)) { RefreshMoveValidity(); ConfirmMoveTarget(); } return; }
            if (TryUpdateThrowCursorFromMouse(true)) RefreshMoveValidity();
        }

        void ConfirmMoveTarget()
        {
            int x = _worldCursorState.X, y = _worldCursorState.Y;
            if (!HeldFixtureActions.CanMoveTo(_moveActor, _moveFixture, _moveZone, x, y, out string code))
            {
                // Red: say why, and stay so another cell can be chosen.
                MessageLog.Add(HeldFixtureActions.Reason(code, PackableFixtureRegistry.Get(_moveFixture?.GetPart<PackableFixturePart>()?.Kind)));
                return;
            }
            var fixture = _moveFixture; var zone = _moveZone; var command = HeldFixtureActions.MoveCommand(zone, fixture, x, y);
            ExitMoveTargeting();
            var result = InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(fixture, command), PlayerEntity, zone);
            if (result.Success) { EndTurnAndProcess(); RequestZoneRedraw("World.MoveFixture"); }
        }
    }
}
