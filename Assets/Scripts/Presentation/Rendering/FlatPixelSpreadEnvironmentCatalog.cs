using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Approved turf, packed road and vegetation for ordinary Spread.
    /// Borrows immutable art/selector and owns cached views only. Opening sites,
    /// settlements, saved lairs, memory and gameplay remain separately authoritative.</summary>
    public sealed class FlatPixelSpreadEnvironmentCatalog : IDisposable
    {
        const int Width=16, Height=24;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] turf, road, hedges, tree, bush, flowers;
        bool disposed;
        static readonly Dictionary<string,Vector2Int> addresses=BuildAddresses();
        static Dictionary<string,Vector2Int> BuildAddresses()
        {
            var result=new Dictionary<string,Vector2Int>(StringComparer.Ordinal);
            for(int x=0;x<WorldMap.Width;x++)for(int y=0;y<WorldMap.Height;y++)
            {
                string id=WorldMap.ToZoneID(x,y);
                // Reserve addresses even when their mutable authority has been revoked.
                if(SpreadCompositionPlan.IsWildernessZone(id)&&id!=ReferenceGladePlan.ZoneID
                    &&id!="Overworld.11.9.0"&&id!="Overworld.11.8.0"
                    &&id!="Overworld.12.10.0"&&id!="Overworld.12.11.0")result.Add(id,new Vector2Int(x,y));
            }
            return result;
        }
        public FlatPixelSpreadEnvironmentCatalog(FlatPixelFallbackCatalog borrowedSelector)
            :this(Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath),
                Resources.Load<Texture2D>(FlatPixelHedgeCatalog.ResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.VegetationResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath),
                Resources.Load<Texture2D>(FlatPixelEastKitchenCatalog.ResourcePath),borrowedSelector){ }

        /// <summary>Independent optional groups: Sill turf0–3 of9, opening road16/17 of21,
        /// sixteen hedge masks, north Tree0 of4, base Bush5 of10. Null/wrong-sized
        /// injection never reloads Resources. No soil frame is used as ordinary grass.</summary>
        public FlatPixelSpreadEnvironmentCatalog(Texture2D turf9,Texture2D ground21,Texture2D hedge16,
            Texture2D vegetation4,Texture2D base10,FlatPixelFallbackCatalog borrowedSelector)
            :this(turf9,ground21,hedge16,vegetation4,base10,null,borrowedSelector){ }

        /// <summary>Optional approved CharmFlowers frame14 of the18-frame East atlas.
        /// Older explicit injection stays unavailable; no hidden Resources reload.</summary>
        public FlatPixelSpreadEnvironmentCatalog(Texture2D turf9,Texture2D ground21,Texture2D hedge16,
            Texture2D vegetation4,Texture2D base10,Texture2D eastKitchen18,FlatPixelFallbackCatalog borrowedSelector)
        {
            selector=borrowedSelector;if(selector==null)return;
            turf=Frames(turf9,9,0,4);road=Frames(ground21,21,16,2);hedges=Frames(hedge16,16,0,16);
            tree=Frames(vegetation4,4,0,1);bush=Frames(base10,10,5,1);flowers=Frames(eastKitchen18,18,14,1);
        }
        public static bool IsSupportedZone(Zone zone)
        {
            if(zone?.ZoneID==null||!addresses.TryGetValue(zone.ZoneID,out var p))return false;
            var manager=WorldLocationContext.For(zone);var map=manager?.WorldMap;
            return manager?.CachedZones!=null&&manager.CachedZones.TryGetValue(zone.ZoneID,out var current)&&ReferenceEquals(zone,current)
                &&map?.Tiles!=null&&map.POIs!=null&&map.Tiles.GetLength(0)==WorldMap.Width&&map.Tiles.GetLength(1)==WorldMap.Height
                &&map.POIs.GetLength(0)==WorldMap.Width&&map.POIs.GetLength(1)==WorldMap.Height
                &&map.GetBiome(p.x,p.y)==BiomeType.Spread&&map.GetPOI(p.x,p.y)==null&&!LairStacks.HasSavedColumn(manager,zone.ZoneID);
        }
        public bool TryResolveGround(Zone zone,int x,int y,out Tile tile,out Entity floor)
        {
            tile=null;floor=null;if(!ReadyCell(zone,x,y,out var cell))return false;
            var state=zone.TileState.Get(x,y);bool fixture=false;
            Entity grass=null,packed=null;
            foreach(var owner in cell.Occupants)
            {
                // Hidden/foreign/carried props cannot disclose themselves through a lower floor.
                if(!Placed(zone,cell,owner)||!VisiblePhysicalOwner(owner))continue;
                if(owner.BlueprintName=="Grass")
                {if(grass!=null||!Floor(owner,true))return false;grass=owner;}
                else if(owner.BlueprintName=="RoadStone")
                {if(packed!=null||!Floor(owner,false))return false;packed=owner;}
                else if(IsEligibleFixture(owner))fixture=true;
                else if(CompetingGround(owner))return false;
            }
            // Hazard marks remain on the independent higher tile-state layer. This
            // exception represents real ground under exact fixtures, never Water.
            if(state!=null&&!state.IsEmpty&&!fixture)return false;
            floor=packed??grass;if(floor==null)return false;
            tile=packed!=null?road==null?null:road[(x+y)&1]:turf==null?null:turf[VariantAt(x,y)];
            if(tile!=null)return true;floor=null;return false;
        }
        public bool TryResolveForeground(Zone zone,int x,int y,Entity owner,out Tile tile)
        {
            tile=null;if(!ReadyCell(zone,x,y,out _)||!Selected(zone,x,y,owner)||!EligibleBody(owner))return false;
            switch(owner.BlueprintName)
            {
                case "Tree":tile=tree==null?null:tree[0];break;
                case "Bush":tile=bush==null?null:bush[0];break;
                case "CharmFlowers":tile=flowers==null?null:flowers[0];break;
                case "Hedge":
                    if(hedges==null)return false;int mask=0;
                    if(HedgeAt(zone,x,y-1))mask|=1;if(HedgeAt(zone,x+1,y))mask|=2;
                    if(HedgeAt(zone,x,y+1))mask|=4;if(HedgeAt(zone,x-1,y))mask|=8;
                    tile=hedges[mask];break;
            }
            return tile!=null;
        }
        /// <summary>Current disclosed floor or selected vegetation; caller still authorizes its
        /// exact document. A covering actor does not replace the lower floor's identity.
        /// Hedge portraits use isolated frame0 and cannot reveal surrounding topology.</summary>
        public bool TryGetSubjectSprite(Zone zone,Entity owner,int x,int y,out Sprite sprite)
        {
            sprite=null;if(owner==null||!ReadyCell(zone,x,y,out _))return false;
            if(owner.BlueprintName=="Grass"||owner.BlueprintName=="RoadStone")
            {
                if(!TryResolveGround(zone,x,y,out var material,out var floor)||!ReferenceEquals(floor,owner))return false;
                sprite=material.sprite;return true;
            }
            // The reader separately authorizes its exact subject. A Player may
            // cover these non-solid flowers without replacing their portrait.
            if(owner.BlueprintName=="CharmFlowers")
            {
                if(flowers==null||!EligibleFlowers(owner)||!Placed(zone,zone.GetCell(x,y),owner))return false;
                sprite=flowers[0].sprite;return true;
            }
            if(!Selected(zone,x,y,owner))return false;
            if(owner.BlueprintName=="Hedge")
            {if(hedges==null||!EligibleBody(owner))return false;sprite=hedges[0].sprite;return true;}
            if(TryResolveForeground(zone,x,y,owner,out var body)){sprite=body.sprite;return true;}
            return false;
        }
        bool ReadyCell(Zone zone,int x,int y,out Cell cell)
        {
            cell=null;if(disposed||selector==null||!IsSupportedZone(zone))return false;cell=zone.GetCell(x,y);
            return cell!=null&&cell.ParentZone==zone&&ReferenceEquals(zone.GetCell(cell.X,cell.Y),cell)
                &&cell.X==x&&cell.Y==y&&cell.Explored&&cell.IsVisible;
        }
        bool Selected(Zone zone,int x,int y,Entity owner)
            =>owner!=null&&selector.TryResolve(zone,x,y,out var v)&&!v.Remembered&&ReferenceEquals(v.Owner,owner);
        bool HedgeAt(Zone zone,int x,int y)
        {
            var cell=zone.GetCell(x,y);return cell!=null&&cell.Explored&&cell.IsVisible
                &&selector.TryResolve(zone,x,y,out var v)&&!v.Remembered&&v.Owner?.BlueprintName=="Hedge"&&EligibleBody(v.Owner);
        }
        static bool Floor(Entity owner,bool grass)
        {
            if(!Plain(owner)||!owner.HasTag("Terrain")||owner.HasTag("Wall")||owner.HasTag("Solid")||owner.HasTag("Barren")
                ||Owned<PhysicsPart>(owner).Solid||ForeignMeaning(owner))return false;
            var r=Owned<RenderPart>(owner);
            if(grass)return owner.HasTag("Vegetation")&&owner.HasTag("Plantable")&&r.RenderString=="."
                &&r.ColorString=="&g"&&r.BackgroundColor=="^g"&&r.GlyphVariants=="...,.'`"&&r.RenderLayer==0;
            return !owner.HasTag("Vegetation")&&r.RenderString=="="&&r.ColorString=="&y"
                &&string.IsNullOrEmpty(r.BackgroundColor)&&string.IsNullOrEmpty(r.GlyphVariants)&&r.RenderLayer==0;
        }
        static bool EligibleBody(Entity owner)
        {
            if(owner?.BlueprintName=="CharmFlowers")return EligibleFlowers(owner);
            if(owner==null||!Plain(owner)||ForeignMeaning(owner)||!owner.HasTag("Vegetation")||owner.HasTag("Wall"))return false;
            string role=owner.BlueprintName;if(role!="Hedge"&&role!="Tree"&&role!="Bush")return false;
            var render=Owned<RenderPart>(owner);var structure=Owned<DestructiblePart>(owner);var thermal=Owned<ThermalPart>(owner);
            bool low=role=="Bush";
            if(render.RenderString!=(low?";":role=="Tree"?"T":"#")||render.ColorString!="&G"||render.RenderLayer!=(low?0:1)
                ||!string.IsNullOrEmpty(render.BackgroundColor)||!string.IsNullOrEmpty(render.GlyphVariants)
                ||Owned<PhysicsPart>(owner).Solid==low||structure==null||structure.Gone||structure.HP<=0
                ||Owned<MaterialPart>(owner)?.MaterialID!=(role=="Tree"?"Wood":"Plant")
                ||thermal==null||!float.IsFinite(thermal.Temperature)||!float.IsFinite(thermal.FlameTemperature)||!float.IsFinite(thermal.FreezeTemperature))return false;
            return low?owner.HasTag("Terrain")&&!owner.HasTag("Solid"):!owner.HasTag("Terrain");
        }
        static bool EligibleFlowers(Entity owner)
        {
            if(owner?.BlueprintName!="CharmFlowers"||!Plain(owner)||ForeignMeaning(owner)
                ||!owner.HasTag("Terrain")||!owner.HasTag("Vegetation")||owner.HasTag("Wall")||owner.HasTag("Solid")
                ||Owned<PhysicsPart>(owner).Solid)return false;
            var render=Owned<RenderPart>(owner);
            return render.RenderString=="*"&&render.ColorString=="&M"&&render.RenderLayer==0
                &&string.IsNullOrEmpty(render.BackgroundColor)&&string.IsNullOrEmpty(render.GlyphVariants);
        }
        static bool Plain(Entity owner)
        {
            if(owner==null||owner.HasTag("Creature")||owner.HasTag("Player")||owner.HasTag("Item"))return false;
            var render=Owned<RenderPart>(owner);var physics=Owned<PhysicsPart>(owner);
            if(render==null||!render.Visible||physics==null||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
                ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)
                ||!string.IsNullOrEmpty(render.DetailColor)||!string.IsNullOrEmpty(render.TileColor))return false;
            if(owner.HasPart<StackerPart>()){var s=Owned<StackerPart>(owner);if(s==null||s.StackCount<=0)return false;}
            if(owner.HasPart<StatusEffectsPart>()&&Owned<StatusEffectsPart>(owner)==null)return false;
            if(!owner.HasPart<SpatialFootprintPart>())return true;
            var footprint=Owned<SpatialFootprintPart>(owner);
            return footprint!=null&&footprint.Offsets.Length==1&&footprint.Offsets[0]==Vector2Int.zero;
        }
        static bool Placed(Zone zone,Cell cell,Entity owner)
        {
            if(owner==null||owner.SpatialZone!=zone)return false;var anchor=zone.GetEntityCell(owner);
            if(anchor==null||!anchor.Objects.Contains(owner))return false;var footprint=owner.GetPart<SpatialFootprintPart>();
            return footprint==null?anchor==cell:footprint.ParentEntity==owner&&zone.IsFootprintCurrent(owner)
                &&SpatialQuery.DistanceToCell(zone,owner,cell.X,cell.Y)==0;
        }
        static bool VisiblePhysicalOwner(Entity owner)
        {
            var r=Owned<RenderPart>(owner);var p=Owned<PhysicsPart>(owner);var stack=owner.GetPart<StackerPart>();
            return r!=null&&r.Visible&&p!=null&&p.InInventory==null&&p.Equipped==null
                &&(stack==null||stack.ParentEntity==owner&&stack.StackCount>0);
        }
        static bool CompetingGround(Entity owner)
            =>owner.HasPart<CropPart>()||owner.HasPart<FieldHarvestPart>()||owner.HasPart<CultivatedSoilPart>()
                ||owner.HasPart<LiquidPoolPart>()||owner.HasPart<GasPoolPart>()||owner.HasPart<TileStateSourcePart>()||owner.HasPart<TriggerOnStepPart>()
                ||owner.HasTag("WellGroundMarker")||owner.HasTag("OvenGroundMarker")||owner.HasTag("LanternGroundMarker")
                ||owner.HasTag("Terrain")&&!owner.HasTag("Wall")
                &&!(owner.BlueprintName=="Bush"&&EligibleBody(owner))&&!EligibleFlowers(owner);
        // Shared by the ordinary-Spread body and lower-ground paths. Existing
        // opening contracts are intentionally unchanged. No rendering/allocation here.
        internal static bool IsEligibleFixture(Entity owner)
        {
            if(owner==null||(owner.BlueprintName!="CopperPipe"&&owner.BlueprintName!="PeatBog"&&owner.BlueprintName!="SteamVent"))return false;
            if(!Plain(owner)||!owner.HasTag("Terrain")||owner.HasTag("Solid")||owner.HasTag("Wall")
                ||owner.HasPart<Body>()||owner.HasPart<BrainPart>()||owner.HasPart<ContainerPart>()
                ||owner.HasPart<DoorPart>()||owner.HasPart<RepairablePart>()||owner.HasPart<HarvestablePart>()
                ||owner.HasPart<CropPart>()||owner.HasPart<FieldHarvestPart>()||owner.HasPart<CultivatedSoilPart>()
                ||owner.HasPart<TriggerOnStepPart>()||owner.HasPart<WellPart>()||owner.HasPart<ForgePart>()
                ||owner.HasPart<AlchemyStillPart>()||owner.HasPart<GasPoolPart>()||owner.GetStat("Hitpoints")!=null)return false;
            // Reject duplicate or foreign parts instead of borrowing another owner's identity.
            for(int i=0;i<owner.Parts.Count;i++)
            {
                if(owner.Parts[i].ParentEntity!=owner)return false;
                for(int j=0;j<i;j++)if(owner.Parts[i].GetType()==owner.Parts[j].GetType())return false;
            }
            if(owner.BlueprintName=="SteamVent")return FlatPixelSharedSteamVentCatalog.IsEligibleBody(owner);
            if(!FlatPixelNellaCornerCatalog.IsEligibleFixtureBody(owner))return false;
            var render=Owned<RenderPart>(owner);
            if(render.RenderLayer!=1||!string.IsNullOrEmpty(render.BackgroundColor))return false;
            if(owner.BlueprintName=="CopperPipe")return !owner.HasPart<LiquidPoolPart>()&&!owner.HasPart<TileStateSourcePart>();
            var source=Owned<TileStateSourcePart>(owner);
            return source!=null&&source.CoatingTurns==4&&source.Heat==0&&source.Cold==0&&source.Charge==0
                &&!source.RadiatesEnergy&&string.IsNullOrEmpty(source.Residue);
        }
        static bool ForeignMeaning(Entity owner)
            =>owner.HasPart<CropPart>()||owner.HasPart<FieldHarvestPart>()||owner.HasPart<CultivatedSoilPart>()||owner.HasPart<HarvestablePart>()
                ||owner.HasPart<RepairablePart>()||owner.HasPart<DoorPart>()||owner.HasPart<ContainerPart>()||owner.HasPart<BrainPart>()||owner.HasPart<Body>()
                ||owner.HasPart<LiquidPoolPart>()||owner.HasPart<GasPoolPart>()||owner.HasPart<TileStateSourcePart>()||owner.HasPart<TriggerOnStepPart>()
                ||owner.HasPart<WellPart>()||owner.HasPart<ForgePart>()||owner.HasPart<AlchemyStillPart>()||owner.HasPart<CampfirePart>()||owner.HasPart<TorchLightPart>()
                ||owner.HasTag("WellGroundMarker")||owner.HasTag("OvenGroundMarker")||owner.HasTag("LanternGroundMarker");
        static T Owned<T>(Entity owner) where T:Part
        {
            T found=null;for(int i=0;i<owner.Parts.Count;i++)if(owner.Parts[i] is T part)
            {if(found!=null||part.ParentEntity!=owner)return null;found=part;}return found;
        }
        static int VariantAt(int x,int y)
        {uint value=unchecked((uint)x*0x9e3779b1u^(uint)y*0x85ebca77u);value^=value>>16;value=unchecked(value*0x7feb352du);value^=value>>15;return(value&15u)<12u?0:1+(int)((value>>4)%3u);}
        static Tile[] Frames(Texture2D source,int atlasCount,int start,int count)
        {
            if(source==null||source.width!=atlasCount*Width||source.height!=Height)return null;var result=new Tile[count];
            for(int i=0;i<count;i++)
            {
                var sprite=Sprite.Create(source,new Rect((start+i)*Width,0,Width,Height),new Vector2(.5f,.5f),Width,0,SpriteMeshType.FullRect);
                sprite.name="Live16x24 Spread "+source.name+" "+(start+i);sprite.hideFlags=HideFlags.HideAndDontSave;
                var tile=ScriptableObject.CreateInstance<Tile>();tile.name=sprite.name;tile.hideFlags=HideFlags.HideAndDontSave;
                tile.sprite=sprite;tile.flags=TileFlags.None;tile.colliderType=Tile.ColliderType.None;result[i]=tile;
            }
            return result;
        }
        public void Dispose(){if(disposed)return;disposed=true;Release(turf);Release(road);Release(hedges);Release(tree);Release(bush);Release(flowers);}
        static void Release(Tile[] tiles){if(tiles==null)return;foreach(var tile in tiles)if(tile!=null){Destroy(tile.sprite);Destroy(tile);}}
        static void Destroy(Object value){if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
    }
}
