using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Three quiet stepping-stone materials for the real kitchen court.
    /// Borrows its atlas; owns only cached tiles/sprites. Never creates terrain,
    /// advances state, queries neighbors or supplies art from a hidden cell.</summary>
    public sealed class FlatPixelWorkingCourtCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/working-court-v2";
        readonly Texture2D atlas;
        readonly Tile[] tiles;
        readonly Sprite[] sprites;
        bool disposed;
        public FlatPixelWorkingCourtCatalog() : this(Resources.Load<Texture2D>(ResourcePath)) { }
        public FlatPixelWorkingCourtCatalog(Texture2D borrowedAtlas)
        {
            if(borrowedAtlas == null || borrowedAtlas.width != 48 || borrowedAtlas.height != 24) return;
            atlas=borrowedAtlas; tiles=new Tile[3]; sprites=new Sprite[3];
            for(int i=0;i<3;i++)
            {
                var sprite=Sprite.Create(atlas,new Rect(i*16,0,16,24),new Vector2(.5f,.5f),16,0,SpriteMeshType.FullRect);
                sprite.name="Live16x24 working stones "+i; sprite.hideFlags=HideFlags.HideAndDontSave;
                var tile=ScriptableObject.CreateInstance<Tile>(); tile.name=sprite.name;
                tile.hideFlags=HideFlags.HideAndDontSave; tile.sprite=sprite;
                tile.flags=TileFlags.None; tile.colliderType=Tile.ColliderType.None;
                sprites[i]=sprite; tiles[i]=tile;
            }
        }
        public bool TryResolve(Zone zone,int x,int y,out Tile tile,out Entity floor)
        {
            tile=null; floor=null;
            if(disposed || atlas==null || zone==null || zone.ZoneID!="Overworld.12.11.0"
                || !FlatPixelGreyboxCatalog.IsAdmitted(zone)) return false;
            var cell=zone.GetCell(x,y);
            if(cell==null || cell.ParentZone!=zone || !cell.Explored || !cell.IsVisible) return false;
            var state=zone.TileState.Get(x,y);
            if(state!=null && !state.IsEmpty || CultivatedSoilPart.IsCultivated(zone,cell)) return false;
            bool grass=false;
            for(int i=0;i<cell.Objects.Count;i++)
            {
                var owner=cell.Objects[i];
                if(!Current(zone,cell,owner)) continue;
                var render=owner.GetPart<RenderPart>();
                if(render==null || render.ParentEntity!=owner || !render.Visible) continue;
                if(owner.GetPart<CropPart>()!=null || owner.GetPart<FieldHarvestPart>()!=null
                    || owner.GetPart<LiquidPoolPart>()!=null || owner.GetPart<GasPoolPart>()!=null
                    || owner.BlueprintName=="RoadStone" || owner.BlueprintName=="Floor" || owner.BlueprintName=="CropRow") return false;
                if(owner.BlueprintName=="Grass" && owner.HasTag("Plantable") && PlainTerrain(owner)) grass=true;
                if(!IsFloor(owner)) continue;
                if(floor!=null) { floor=null; return false; }
                floor=owner;
            }
            if(floor==null || !grass) { floor=null; return false; }
            tile=tiles[(x*7+y*11)%3]; return true;
        }
        static bool Current(Zone zone,Cell cell,Entity owner)
        {
            if(owner==null || owner.SpatialZone!=zone || zone.GetEntityCell(owner)!=cell) return false;
            var p=owner.GetPart<PhysicsPart>();
            return p!=null && p.ParentEntity==owner && p.InInventory==null && p.Equipped==null
                && owner.GetPart<SpatialFootprintPart>()==null
                && (owner.GetPart<StackerPart>()==null || owner.GetPart<StackerPart>().ParentEntity==owner && owner.GetPart<StackerPart>().StackCount>0);
        }
        static bool PlainTerrain(Entity owner)
        {
            var p=owner.GetPart<PhysicsPart>();
            return owner.HasTag("Terrain") && !owner.HasTag("Barren") && !owner.HasTag("Creature") && !owner.HasTag("Item") && !owner.HasTag("Player") && !owner.HasTag("Solid")
                && p!=null && p.ParentEntity==owner && !p.Solid && !p.Takeable && p.InInventory==null && p.Equipped==null;
        }
        static bool IsFloor(Entity owner)
        {
            if(owner.BlueprintName!="StoneFloor" || owner.GetProperty("SpreadResident.Role")!="court-floor"
                || owner.GetProperty("ConnectedSpread.Role")!="court-floor" || !PlainTerrain(owner)
                || owner.HasTag("Vegetation") || owner.GetPart<CultivatedSoilPart>()!=null) return false;
            var r=owner.GetPart<RenderPart>();
            if(r==null || r.ParentEntity!=owner || !r.Visible || !string.IsNullOrEmpty(r.VisualID)
                || !string.IsNullOrEmpty(r.VisualVariant) || r.GlyphVariants!="...,.'`") return false;
            int renders=0, physics=0;
            for(int i=0;i<owner.Parts.Count;i++)
            { if(owner.Parts[i] is RenderPart) renders++; if(owner.Parts[i] is PhysicsPart) physics++; }
            return renders==1 && physics==1;
        }
        public void Dispose()
        {
            if(disposed) return; disposed=true;
            if(tiles!=null) foreach(var tile in tiles) DestroyOwned(tile);
            if(sprites!=null) foreach(var sprite in sprites) DestroyOwned(sprite);
        }
        static void DestroyOwned(Object value)
        { if(value==null) return; if(Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
