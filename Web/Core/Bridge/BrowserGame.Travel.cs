using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Web
{
    public partial class BrowserGame
    {
        bool OnWorldMap => WorldMap.IsWorldMapZoneID(zone.ZoneID);

        (int x,int y,int depth) WorldCoordinates()
        {
            if(!OnWorldMap)return WorldMap.FromZoneID(zone.ZoneID);
            var cell=zone.GetEntityCell(player);
            var (x,y)=WorldMap.ZoneCellToWorldCell(cell.X,cell.Y);
            return (x,y,0);
        }

        bool CanEnterWorldMap()
        {
            var (x,y,depth)=WorldCoordinates();
            return !OnWorldMap&&depth==0&&zones.WorldMap.InBounds(x,y);
        }

        // Explicit travel buttons and stair fallback share the native guarded API.
        // It owns saved surface position and map-step clock cost; we pay one turn
        // only after a successful transition, just as the native input handler does.
        void Travel(string destination)
        {
            if(destination!="world"&&destination!="local") {error="Choose world travel or an area to enter.";return;}
            if(destination=="world"&&!CanEnterWorldMap())
            {error=OnWorldMap?"You are already on the world map.":"Return to the surface before travelling on the world map.";return;}
            if(destination=="local"&&!OnWorldMap) {error="Enter an area from the world map.";return;}
            var result=WorldMapTraversal.TryWorldMapVertical(player,zone,destination=="local",zones);
            if(!result.Success) {error=result.ErrorReason??"That route is not available.";return;}
            EnterZone(result);EndTurn();
        }

        (string glyph,string color,string name) DisclosedMapAppearance(int x,int y)
        {
            var map=zones.WorldMap;var poi=map.GetPOI(x,y);
            return poi!=null&&(poi.Type!=POIType.Sinkhole||map.IsVisited(x,y))
                ?WorldMapZoneBuilder.GetPOIRender(poi):WorldMapZoneBuilder.GetBiomeRender(map.GetBiome(x,y));
        }

        object MapLocation(int x,int y)
        {
            var (wx,wy)=WorldMap.ZoneCellToWorldCell(x,y);
            if(!zones.WorldMap.InBounds(wx,wy))return null;
            // Read the live terrain owner, including when the player covers it.
            foreach(var e in zone.GetCell(x,y).Occupants)
                if(e.GetPart<WorldMapCellPart>() is WorldMapCellPart marker)
                    return new {name=e.GetDisplayName(),worldX=marker.WorldX,worldY=marker.WorldY,
                        biome=zones.WorldMap.GetBiome(wx,wy).ToString(),visited=zones.WorldMap.IsVisited(wx,wy)};
            return null;
        }

        List<object> WorldMapCells()
        {
            var cells=new List<object>(WorldMap.Width*WorldMap.Height);
            for(int wy=0;wy<WorldMap.Height;wy++)for(int wx=0;wx<WorldMap.Width;wx++)
            {
                var (x,y)=WorldMap.WorldCellToZoneCell(wx,wy);var cell=zone.GetCell(x,y);
                if(!cell.Explored)continue;
                Entity terrain=null,top=null;
                foreach(var e in cell.Occupants)
                {
                    if(e.HasPart<WorldMapCellPart>())terrain=e;
                    var r=e.GetPart<RenderPart>();if(r==null||!r.Visible)continue;
                    if(!cell.IsVisible&&!e.HasPart<WorldMapCellPart>())continue;
                    if(top==null||r.RenderLayer>=top.GetPart<RenderPart>().RenderLayer)top=e;
                }
                var render=top?.GetPart<RenderPart>();var ground=terrain?.GetPart<RenderPart>();
                var biome=zones.WorldMap.GetBiome(wx,wy);var plain=WorldMapZoneBuilder.GetBiomeRender(biome);
                cells.Add(new {x,y,v=cell.IsVisible,g=render?.ResolveGlyph(x,y).ToString()??" ",c=render?.ColorString??"&y",
                    art=top==terrain?null:Art(top,x,y),name=ground?.DisplayName??plain.displayName,biome=biome.ToString(),
                    visited=zones.WorldMap.IsVisited(wx,wy),mapGround=MapGroundArt(biome,wx,wy),mapTint=MapGroundTint(biome),
                    mapGlyph=ground?.RenderString??plain.glyph,mapColor=ground?.ColorString??plain.color,
                    landmark=ground!=null&&ground.DisplayName!=plain.displayName,hostile=false,solid=false});
            }
            return cells;
        }

        static string MapGroundArt(BiomeType biome,int x,int y)
        {
            string atlas=biome switch {BiomeType.Spread=>"sill-environment",BiomeType.Beating=>"beating-materials",
                BiomeType.Sodden=>"sodden-flood-water",BiomeType.Grovelands=>"grovelands-ground",
                BiomeType.Stump=>"stump-materials",BiomeType.Overwrit=>"overwrit-materials",_=>null};
            if(atlas==null)return null;
            // Native FlatPixelWorldMapCatalog's deterministic frame choice.
            uint hash=unchecked((uint)(x*374761393+y*668265263));hash=unchecked((hash^(hash>>13))*1274126177u);
            uint frame=(hash>>16)%(biome==BiomeType.Sodden?8u:4u);
            return atlas+":"+(biome==BiomeType.Sodden?frame*16:frame);
        }
        static double MapGroundTint(BiomeType biome)=>biome==BiomeType.Stump ? 0.25 :
            biome==BiomeType.Beating||biome==BiomeType.Overwrit ? 0.45 : 0.65;

        object JourneyView()
        {
            var places=new List<object>();var (currentX,currentY,_)=WorldCoordinates();
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
            {
                var visited=zones.WorldMap.IsVisited(x,y);var poi=zones.WorldMap.GetPOI(x,y);
                bool disclosed=poi!=null&&(poi.Type!=POIType.Sinkhole||visited);
                if(!visited&&!disclosed&&(x!=currentX||y!=currentY))continue;
                var appearance=DisclosedMapAppearance(x,y);
                places.Add(new {x,y,name=appearance.name,glyph=appearance.glyph,color=appearance.color,
                    biome=zones.WorldMap.GetBiome(x,y).ToString(),visited,current=x==currentX&&y==currentY});
            }
            return new {places};
        }
    }
}
