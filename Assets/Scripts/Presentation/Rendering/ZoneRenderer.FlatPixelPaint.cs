using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    public partial class ZoneRenderer
    {
        internal Color ApplyFlatLighting(Color color, int x, int y)
            => _lightMap == null ? color : _lightMap.ApplyToColor(color, x, y);

        // Runs before legacy CP437 lookup. The environment pass uses the same
        // validated owner and this exact Render-event result when claiming it.
        private void RenderFlatCellCore(int x, int y, Vector3Int position, Cell cell)
        {
            if (!_envSpriteRenderer.TryResolveFlatPresentation(this, CurrentZone, x, y, out var flat))
            {
                _envSpriteRenderer.ClearFlatPaintReceipt(x, y);
                _tilemap.SetTile(position, null);
                _tileStateTilemap?.SetTile(position, null);
                if (_bgTilemap != null)
                {
                    bool unexplored = FlatPixelFallbackCatalog.IsCurrentGraph(CurrentZone)
                        && cell.ParentZone == CurrentZone && ReferenceEquals(CurrentZone.GetCell(x, y), cell) && !cell.Explored;
                    _bgTilemap.SetTile(position, unexplored ? _envSpriteRenderer.FlatBackgroundTile : null);
                    _bgTilemap.SetTileFlags(position, TileFlags.None);
                    _bgTilemap.SetColor(position, UnexploredColor);
                    _bgTilemap.SetTransformMatrix(position, Matrix4x4.identity);
                }
                return;
            }

            PaintTileStateMarkCore(x, y, position, cell,
                flat.LocalGround && flat.GroundKind == FlatPixelGreyboxGround.Water);
            var tint = !flat.Remembered && flat.Owner != null
                ? ResolveRenderColor(flat.Owner, flat.Owner.GetPart<RenderPart>(), x, y)
                : ApplyFlatLighting(flat.ForegroundColor, x, y);
            _envSpriteRenderer.RecordFlatPaint(CurrentZone, x, y, flat, tint);
            _tilemap.SetTile(position, flat.ForegroundTile);
            _tilemap.SetTileFlags(position, TileFlags.None);
            _tilemap.SetColor(position, flat.NativeForeground ? flat.ForegroundColor : tint);
            _tilemap.SetTransformMatrix(position, Matrix4x4.identity);
            if (_bgTilemap != null)
            {
                _bgTilemap.SetTile(position, flat.GroundTile);
                _bgTilemap.SetTileFlags(position, TileFlags.None);
                _bgTilemap.SetColor(position, flat.LocalGround ? flat.GroundColor : ApplyFlatLighting(flat.GroundColor, x, y));
                _bgTilemap.SetTransformMatrix(position, Matrix4x4.identity);
            }
        }

        // Both presentation modes dispatch one pooled event for the selected
        // visible owner. Catalogs, portraits and neighbor reclaims never do so.
        private Color ResolveRenderColor(Entity owner, RenderPart render, int x, int y)
        {
            string colorString = AsciiWorldRenderPolicy.GetColorOrFallback(render, out string issue);
            LogRenderIssueOnce(owner, issue);
            var renderEvent = GameEvent.New("Render");
            try
            {
                renderEvent.SetParameter("Entity", (object)owner);
                renderEvent.SetParameter("RenderPart", (object)render);
                renderEvent.SetParameter("ColorString", colorString ?? "");
                renderEvent.SetParameter("DetailColor", render.DetailColor ?? "");
                owner.FireEvent(renderEvent);
                string eventColor = renderEvent.GetStringParameter("ColorString", colorString);
                colorString = AsciiWorldRenderPolicy.IsValidColorString(eventColor)
                    ? eventColor : AsciiWorldRenderPolicy.FallbackColorString;
            }
            finally { renderEvent.Release(); }
            return ApplyFlatLighting(QudColorParser.Parse(colorString), x, y);
        }
    }
}
