using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InputHandler
    {
        private readonly List<(Entity owner, int x, int y, string label)> _controllerLabels =
            new List<(Entity, int, int, string)>(64);
        private readonly List<Rect> _controllerLabelBoxes = new List<Rect>(24);
        private Zone _controllerLabelZone;
        private int _controllerLabelTick = -1;
        private bool _controllerHighlighted;
        private int _controllerLabelsDrawn;
        private GUIStyle _controllerHintStyle, _controllerLabelStyle;
        private Camera _controllerHintCamera;

        private void OnGUI()
        {
            _controllerLabelsDrawn = 0;
            if (!NativeGamepadInput.IsConnected || !isActiveAndEnabled || PlayerEntity == null
                || CurrentZone == null || TurnManager == null || ControllerContext() == GamepadInputContext.Menu)
            { _controllerHighlighted = false; return; }
            if (_controllerHintStyle == null)
            {
                _controllerHintStyle = new GUIStyle(GUI.skin.box) {
                    fontSize = 14, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8, 8, 3, 3)
                };
                _controllerHintStyle.normal.textColor = new Color(.85f, .94f, .77f);
                _controllerLabelStyle = new GUIStyle(_controllerHintStyle) { fontSize = 12 };
            }
            if (_controllerHintCamera == null) _controllerHintCamera = Camera.main;
            Rect viewport = _controllerHintCamera != null ? _controllerHintCamera.pixelRect
                : new Rect(0, 0, Screen.width * .7f, Screen.height * .8f);
            string hint = _inputState == InputState.Normal
                ? "LS direction · RT step / wait · A use · X ability · Menu character · View pause"
                : _inputState == InputState.LookMode || _inputState == InputState.ThrowTargeting
                    ? "Sticks / D-pad cursor · A / RT confirm · B back"
                    : "LS direction · A / RT confirm · B cancel";
            if (_controllerTravel != ControllerTravelMode.None) hint = "Travelling / waiting — any input stops";
            GUI.Box(new Rect(viewport.x + 6, Screen.height - viewport.y - 29,
                Mathf.Min(viewport.width - 12, 680), 24), hint, _controllerHintStyle);

            bool highlight = _inputState == InputState.Normal && NativeGamepadInput.IsHighlightHeld;
            if (!highlight) { _controllerHighlighted = false; return; }
            if (!_controllerHighlighted || _controllerLabelZone != CurrentZone || _controllerLabelTick != TurnManager.TickCount)
            {
                _controllerLabels.Clear();
                var seen = new HashSet<Entity>();
                for (int y = 0; y < Zone.Height; y++)
                    for (int x = 0; x < Zone.Width; x++)
                    {
                        var cell = CurrentZone.GetCell(x, y);
                        if (!cell.IsVisible || !cell.Explored) continue;
                        foreach (var owner in cell.Occupants)
                            if (ControllerInteresting(owner) && seen.Add(owner))
                            { _controllerLabels.Add((owner, x, y, owner.GetDisplayName())); }
                    }
                var at = CurrentZone.GetEntityPosition(PlayerEntity);
                _controllerLabels.Sort((a, b) => {
                    int da = (a.x-at.x)*(a.x-at.x) + (a.y-at.y)*(a.y-at.y);
                    int db = (b.x-at.x)*(b.x-at.x) + (b.y-at.y)*(b.y-at.y);
                    return da != db ? da.CompareTo(db) : a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x);
                });
                _controllerLabelZone = CurrentZone; _controllerLabelTick = TurnManager.TickCount;
            }
            _controllerHighlighted = true;
            if (_controllerHintCamera == null) return;
            _controllerLabelBoxes.Clear();
            foreach (var row in _controllerLabels)
            {
                // Revalidate after load/destruction/FOV changes, even at the same tick.
                var cell = CurrentZone.GetCell(row.x, row.y);
                if (cell?.IsVisible != true || !cell.Explored || row.owner.SpatialZone != CurrentZone
                    || row.owner.GetPart<RenderPart>()?.Visible != true || !cell.Occupants.Contains(row.owner)) continue;
                Vector3 screen = _controllerHintCamera.WorldToScreenPoint(new Vector3(row.x + .5f, Zone.Height - row.y - .5f, -.1f));
                if (screen.z <= 0 || !viewport.Contains(new Vector2(screen.x, screen.y))) continue;
                var box = new Rect(Mathf.Clamp(screen.x, viewport.x, Mathf.Max(viewport.x, viewport.xMax - 174)),
                    Mathf.Clamp(Screen.height - screen.y - 24, Screen.height - viewport.yMax, Screen.height - viewport.y - 54), 174, 22);
                bool overlaps = false;
                foreach (var occupied in _controllerLabelBoxes) if (box.Overlaps(occupied)) { overlaps = true; break; }
                if (overlaps) continue;
                _controllerLabelBoxes.Add(box);
                GUI.Box(box, row.label, _controllerLabelStyle);
                _controllerLabelsDrawn++;
                if (_controllerLabelsDrawn >= 24) break;
            }
        }
    }
}
