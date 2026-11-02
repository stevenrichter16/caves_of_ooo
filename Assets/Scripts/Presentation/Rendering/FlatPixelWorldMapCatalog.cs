using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Bounded live Spread cartography. Reuses approved turf; does not
    /// change map knowledge, POI symbols, traversal, memory or source textures.</summary>
    public sealed class FlatPixelWorldMapCatalog : IDisposable
    {
        // Map symbols retain their authored tint; quiet the continuous substrate.
        public static readonly Color GroundTint = new Color(.65f,.65f,.65f,1f);
        readonly Tile[] turf;
        bool disposed;
        public FlatPixelWorldMapCatalog() : this(Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath)) { }
        public FlatPixelWorldMapCatalog(Texture2D source)
        {
            if (source == null || source.width != 144 || source.height != 24) return;
            turf = new Tile[4];
            for (int i = 0; i < turf.Length; i++)
            {
                var sprite = Sprite.Create(source,new Rect(i*16,0,16,24),new Vector2(.5f,.5f),16,0,SpriteMeshType.FullRect);
                sprite.name = "Live16x24 WorldMap Spread " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; turf[i] = tile;
            }
        }
        public bool TryResolveGround(Zone zone,int x,int y,out Tile tile,out Entity floor)
        {
            tile=null;floor=null;
            if (disposed || turf==null || zone?.ZoneID!=WorldMap.WorldMapZoneID) return false;
            var manager=WorldLocationContext.For(zone);var map=manager?.WorldMap;
            if (manager?.CachedZones==null || !manager.CachedZones.TryGetValue(zone.ZoneID,out var current)
                || !ReferenceEquals(current,zone) || !Dimensions(map?.Tiles) || !Dimensions(map?.POIs) || !Dimensions(map?.Visited)) return false;
            var cell=zone.GetCell(x,y);
            if(cell==null || cell.ParentZone!=zone || !cell.Explored || !cell.IsVisible) return false;
            var state=zone.TileState.Get(x,y);if(state!=null && !state.IsEmpty)return false;
            var (wx,wy)=WorldMap.ZoneCellToWorldCell(x,y);
            if(!map.InBounds(wx,wy) || map.GetBiome(wx,wy)!=BiomeType.Spread)return false;
            var poi=map.GetPOI(wx,wy);
            // Hidden sinkholes MUST be indistinguishable from ordinary terrain.
            bool disclosedSite = poi!=null && (poi.Type!=POIType.Sinkhole || map.IsVisited(wx,wy));
            var appearance = disclosedSite ? WorldMapZoneBuilder.GetPOIRender(poi)
                : WorldMapZoneBuilder.GetBiomeRender(BiomeType.Spread);
            foreach(var candidate in cell.Occupants)
            {
                var render=candidate?.GetPart<RenderPart>();
                if(render==null || !render.Visible || render.ParentEntity!=candidate || candidate.SpatialZone!=zone
                    || zone.GetEntityCell(candidate)!=cell || !cell.Objects.Contains(candidate))continue;
                var physics=candidate.GetPart<PhysicsPart>();
                if(physics!=null && (physics.InInventory!=null || physics.Equipped!=null))continue;
                if(candidate.BlueprintName!="WorldMapCell" && !candidate.HasPart<WorldMapCellPart>())continue;
                if(floor!=null || !Canonical(candidate,wx,wy,appearance.glyph,appearance.color,appearance.displayName)) { floor=null;return false; }
                floor=candidate;
            }
            if(floor==null)return false;
            uint hash=unchecked((uint)(wx*374761393+wy*668265263));hash=(hash^(hash>>13))*1274126177u;
            tile=turf[(hash>>16)&3];return true;
        }
        static bool Dimensions(Array values) => values!=null && values.Rank==2
            && values.GetLength(0)==WorldMap.Width && values.GetLength(1)==WorldMap.Height;
        public static bool IsPlainTerrain(Entity owner) => owner?.GetPart<RenderPart>() is RenderPart render
            && render.RenderString=="," && render.ColorString=="&g" && render.DisplayName=="the Spread";
        static bool Canonical(Entity owner,int wx,int wy,string glyph,string color,string name)
        {
            if(owner.BlueprintName!="WorldMapCell" || !owner.HasTag("WorldMapCell") || owner.Parts.Count!=3)return false;
            RenderPart render=null;PhysicsPart physics=null;WorldMapCellPart map=null;
            foreach(var part in owner.Parts)
            {
                if(part.ParentEntity!=owner)return false;
                if(part is RenderPart r && render==null)render=r;
                else if(part is PhysicsPart p && physics==null)physics=p;
                else if(part is WorldMapCellPart m && map==null)map=m;
                else return false;
            }
            return render!=null && physics!=null && map!=null && map.WorldX==wx && map.WorldY==wy
                && !physics.Solid && !physics.Takeable && physics.Weight==0 && physics.InInventory==null && physics.Equipped==null
                && render.Visible && render.RenderLayer==0 && render.RenderString==glyph && render.ColorString==color
                && render.DisplayName==name && string.IsNullOrEmpty(render.BackgroundColor)
                && string.IsNullOrEmpty(render.GlyphVariants) && string.IsNullOrEmpty(render.VisualVariant)
                && string.IsNullOrEmpty(render.VisualID);
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;if(turf==null)return;
            foreach(var tile in turf) { if(tile==null)continue;DestroyOwned(tile.sprite);DestroyOwned(tile); }
        }
        static void DestroyOwned(Object value) { if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value); }
    }
}
