using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Native source components backed by real zone owners. This adapter
    /// reads gameplay state; it never removes entities, opens doors or grants items.</summary>
    public sealed class MorrowfastScenePresenter : MonoBehaviour
    {
        public const string ShaderResource="SceneArt/Morrowfast/MorrowfastScene";
        public const float PixelsPerCell=40.96f, OriginX=21.25f, ArtWidth=37.5f, ArtHeight=25f;
        // Native player bodies extend half a cell above the row-zero boundary.
        // Constant headroom avoids a zoom jump when arriving from the Felling.
        public const float CameraWorldHeight=25.5f, CameraCenterY=CameraWorldHeight*.5f, CameraAspect=ArtWidth/CameraWorldHeight;
        private sealed class View
        {
            public MorrowfastArtDefinition.Layer Layer;
            public MorrowfastArtDefinition.Owner Spec;
            public SpriteRenderer Sprite;
            public Color32[] Pixels;
            public Vector2 SourcePosition;
            public float SourceDepth,VisibleFog;
            public Entity Owner;
            public bool Ground,Transient;
        }
        private readonly List<View> views=new List<View>();
        private readonly HashSet<Entity> owners=new HashSet<Entity>();
        private readonly Dictionary<string,Entity> currentOwners=new Dictionary<string,Entity>(StringComparer.Ordinal);
        private readonly Dictionary<string,bool> revealedRooms=new Dictionary<string,bool>(StringComparer.Ordinal);
        private readonly Dictionary<string,float> roomFog=new Dictionary<string,float>(StringComparer.Ordinal);
        private readonly Dictionary<string,RectInt> roomBounds=new Dictionary<string,RectInt>(StringComparer.Ordinal);
        private readonly Color32[] fogPixels=new Color32[80*25];
        private MorrowfastArtDefinition definition;
        private GameObject content;
        private Material material;
        private Texture2D fog;
        private MaterialPropertyBlock properties;
        private bool visible=true;
        public Zone CurrentZone { get; private set; }
        public bool IsReady { get; private set; }
        public bool PresentationVisible=>IsReady&&visible;
        public string Failure { get; private set; }
        public int ComponentCount=>definition?.owners.Length??0;
        public int LayerCount=>views.Count;
        public static Vector2 ImageToWorld(Vector2 p)=>new Vector2(OriginX+p.x/PixelsPerCell,ArtHeight-p.y/PixelsPerCell);
        public static Vector2 WorldToImage(Vector2 p)=>new Vector2((p.x-OriginX)*PixelsPerCell,(ArtHeight-p.y)*PixelsPerCell);
        public static bool TryImageToCell(Vector2 p,out int x,out int y)
        {
            x=y=-1;
            if(!Finite(p.x)||!Finite(p.y)||p.x<0||p.x>=1536||p.y<0||p.y>=1024)return false;
            x=Mathf.FloorToInt(OriginX+p.x/PixelsPerCell);y=Mathf.FloorToInt(p.y/PixelsPerCell);return true;
        }
        public static float CameraHalfHeight(float aspect)=>Finite(aspect)&&aspect>0?Mathf.Max(CameraCenterY,ArtWidth*.5f/aspect):CameraCenterY;
        public static float DepthForFootPixel(float y)=>-(y/PixelsPerCell-1)*.001f;
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public static bool IsInsideRoom(Vector2 p,MorrowfastArtDefinition.Point[] polygon)
        {
            if(!Finite(p.x)||!Finite(p.y)||polygon==null||polygon.Length<3)return false;
            bool inside=false;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
            {
                var a=polygon[i];var b=polygon[j];
                if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
            }
            return inside;
        }
        public bool ClaimsCell(int x,int y)=>PresentationVisible&&x>=21&&x<=58&&y>=0&&y<25;
        public bool IsRoomRevealed(string roomId)=>PresentationVisible&&roomId!=null&&revealedRooms.TryGetValue(roomId,out bool revealed)&&revealed;
        public bool IsAuthoredEntity(Entity entity)=>PresentationVisible&&entity!=null
            &&(owners.Contains(entity)||entity.HasTag("MorrowfastAuthoredTerrain")||entity.ID?.StartsWith("morrowfast-terrain:",StringComparison.Ordinal)==true);
        /// <summary>The shared actor renderer should exclude only entities with a
        /// currently drawn source view. Outside art bounds its normal fallback is free.</summary>
        public bool IsRenderedEntity(Entity entity)
        {
            if(!PresentationVisible||entity==null)return false;
            foreach(var view in views)if(ReferenceEquals(view.Owner,entity)&&view.Sprite.enabled&&view.VisibleFog>0)return true;
            return false;
        }
        public void Bind(Zone zone)
        {
            if(ReferenceEquals(CurrentZone,zone)&&IsReady)return;
            Unbind();CurrentZone=zone;
            if(!MorrowfastSceneRuntime.IsActive(zone))return;
            try
            {
                definition=MorrowfastArtDefinition.Load();
                if(definition==null)throw new InvalidOperationException("missing art definition");
                Shader shader=Resources.Load<Shader>(ShaderResource);
                if(shader==null||!shader.isSupported)throw new InvalidOperationException("missing or unsupported source shader");
                fog=new Texture2D(80,25,TextureFormat.RGBA32,false,true)
                {name="Morrowfast native cell fog",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
                material=new Material(shader){name="Morrowfast source art",hideFlags=HideFlags.DontSave};
                material.SetTexture("_SceneFog",fog);properties=new MaterialPropertyBlock();
                content=new GameObject("Morrowfast layered art");content.transform.SetParent(transform,false);content.layer=gameObject.layer;
                MakeSprite("Backing",definition.baseResource,new[]{0,0,1536,1024},3,1);
                var ordered=new List<MorrowfastArtDefinition.Layer>(definition.layers);
                ordered.Sort((a,b)=>a.z!=b.z?a.z.CompareTo(b.z):string.CompareOrdinal(a.id,b.id));
                for(int i=0;i<ordered.Count;i++)
                {
                    var layer=ordered[i];var owner=definition.FindOwner(layer.ownerId);
                    // A traversable bridge is a walking surface; north-bank actors
                    // must not pass behind its pixels due to ordinary foot depth.
                    bool ground=owner.kind=="bridge"||layer.role=="interior"||(layer.role=="contact"&&owner.kind=="building-shell");
                    float depth=ground?.9f-i*.000001f:DepthForFootPixel(owner.sourceFoot[1])-i*.0000001f;
                    var sprite=MakeSprite(layer.id,layer.resource,layer.bounds,ground?3:AnimatedEntityRenderer.BodySortingOrder,depth);
                    views.Add(new View{Layer=layer,Spec=owner,Sprite=sprite,Pixels=sprite.sprite.texture.GetPixels32(),SourcePosition=sprite.transform.position,
                        SourceDepth=depth,Ground=ground,Transient=owner.kind=="npc"||owner.kind=="creature"});
                    if(layer.role=="roof"&&!string.IsNullOrEmpty(layer.roomId))
                    {
                        var b=layer.bounds;Vector2 min=ImageToWorld(new Vector2(b[0],b[1]+b[3]));
                        int left=Mathf.Max(0,Mathf.FloorToInt(min.x)-1),top=Mathf.Max(0,Mathf.FloorToInt(b[1]/PixelsPerCell)-1);
                        int right=Mathf.Min(79,Mathf.CeilToInt(OriginX+(b[0]+b[2])/PixelsPerCell));
                        int bottom=Mathf.Min(24,Mathf.CeilToInt((b[1]+b[3])/PixelsPerCell));
                        roomBounds[layer.roomId]=new RectInt(left,top,right-left+1,bottom-top+1);
                    }
                }
                ResolveSourceDepths();IsReady=true;Refresh();content.SetActive(visible);
            }
            catch(Exception e)
            {
                Unbind();CurrentZone=zone;Failure=e.Message;
                Debug.LogWarning("[MorrowfastScene] Native terrain fallback: "+Failure);
            }
        }
        private SpriteRenderer MakeSprite(string name,string resource,int[] bounds,int order,float depth)
        {
            Sprite sprite=Resources.Load<Sprite>(resource);
            if(sprite==null||sprite.texture==null||!sprite.texture.isReadable||sprite.rect.width!=bounds[2]||sprite.rect.height!=bounds[3]
                ||Mathf.Abs(sprite.pixelsPerUnit-PixelsPerCell)>.0001f||sprite.pivot!=Vector2.zero)
                throw new InvalidOperationException("invalid native sprite: "+resource);
            var child=new GameObject(name);child.transform.SetParent(content.transform,false);child.layer=gameObject.layer;
            Vector2 pos=ImageToWorld(new Vector2(bounds[0],bounds[1]+bounds[3]));child.transform.position=new Vector3(pos.x,pos.y,depth);
            var renderer=child.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;renderer.sortingOrder=order;return renderer;
        }
        /// <summary>Preserve the source's alpha-overlap precedence at intact anchors,
        /// while disjoint objects retain native actor-compatible foot depth.</summary>
        private void ResolveSourceDepths()
        {
            for(int i=0;i<views.Count;i++)
            {
                var current=views[i];if(current.Ground)continue;
                for(int j=0;j<i;j++)
                {
                    var prior=views[j];
                    if(!prior.Ground&&prior.SourceDepth<=current.SourceDepth&&Overlaps(prior,current))
                        current.SourceDepth=prior.SourceDepth-.0000001f;
                }
                Vector3 p=current.Sprite.transform.position;p.z=current.SourceDepth;current.Sprite.transform.position=p;
            }
        }
        private static bool Overlaps(View a,View b)
        {
            int[] aa=a.Layer.bounds,bb=b.Layer.bounds;
            int left=Mathf.Max(aa[0],bb[0]),top=Mathf.Max(aa[1],bb[1]),right=Mathf.Min(aa[0]+aa[2],bb[0]+bb[2]),bottom=Mathf.Min(aa[1]+aa[3],bb[1]+bb[3]);
            for(int y=top;y<bottom;y++)for(int x=left;x<right;x++)
                if(a.Pixels[(aa[3]-1-y+aa[1])*aa[2]+x-aa[0]].a!=0&&b.Pixels[(bb[3]-1-y+bb[1])*bb[2]+x-bb[0]].a!=0)return true;
            return false;
        }
        public void Refresh()
        {
            if(!IsReady||CurrentZone==null)return;
            owners.Clear();currentOwners.Clear();revealedRooms.Clear();roomFog.Clear();
            float remembered=ZoneRenderer.RememberedBrightnessFor(CurrentZone.AmbientLevel);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                byte value=FogByte(CurrentZone.GetCell(x,y),remembered);fogPixels[(24-y)*80+x]=new Color32(value,value,value,255);
            }
            fog.SetPixels32(fogPixels);fog.Apply(false,false);
            string playerRoom=null;
            foreach(var entity in CurrentZone.GetReadOnlyEntities())
                if(entity.HasTag("Player")){var cell=CurrentZone.GetEntityCell(entity);if(cell!=null)playerRoom=MorrowfastSceneRuntime.GetRoomAt(CurrentZone,cell.X,cell.Y);break;}
            foreach(var room in definition.rooms)
            {
                revealedRooms[room.id]=room.id==playerRoom||MorrowfastSceneRuntime.IsRoofLifted(CurrentZone,room.roofId);
                float observed=0;
                if(roomBounds.TryGetValue(room.id,out var bounds))
                    for(int y=bounds.yMin;y<bounds.yMax;y++)for(int x=bounds.xMin;x<bounds.xMax;x++)
                        if(x==bounds.xMin||x==bounds.xMax-1||y==bounds.yMin||y==bounds.yMax-1)
                            observed=Mathf.Max(observed,FogByte(CurrentZone.GetCell(x,y),remembered)/255f);
                roomFog[room.id]=observed;
            }
            foreach(var spec in definition.owners)
            {
                Entity owner=MorrowfastSceneRuntime.FindOwner(CurrentZone,spec.id);currentOwners[spec.id]=owner;
                if(owner!=null)owners.Add(owner);
            }
            foreach(var view in views)
            {
                currentOwners.TryGetValue(view.Spec.id,out view.Owner);
                Cell cell=view.Owner!=null?CurrentZone.GetEntityCell(view.Owner):null;
                bool shown=cell!=null&&MorrowfastSceneRuntime.IsPresent(CurrentZone,view.Spec.id);
                string roomId=view.Layer.roomId??view.Spec.roomId;
                bool roomOpen=roomId!=null&&revealedRooms.TryGetValue(roomId,out bool open)&&open;
                if(view.Layer.role=="roof"&&roomOpen)shown=false;
                if((view.Layer.role=="interior"||view.Spec.visibleWhen=="room-open")&&!roomOpen)shown=false;
                if(view.Layer.role=="door"&&MorrowfastSceneRuntime.IsDoorOpen(CurrentZone,view.Spec.id))shown=false;
                Vector2Int authored=MorrowfastSceneRuntime.GetAuthoredAnchor(view.Spec.id);
                int dx=cell!=null?cell.X-authored.x:0,dy=cell!=null?cell.Y-authored.y:0;
                bool displaced=dx!=0||dy!=0;
                // Contact collars contain the original ground: they stay out of a
                // moved actor/item's view rather than dragging a patch of old paving.
                if(displaced&&view.Layer.role=="contact")shown=false;
                if(cell!=null&&(cell.X<21||cell.X>58))shown=false;
                view.VisibleFog=FogByte(cell,remembered)/255f;
                if(view.Transient&&(cell==null||!cell.IsVisible))view.VisibleFog=0;
                if((view.Layer.role=="roof"||view.Layer.role=="shell")&&roomId!=null&&roomFog.TryGetValue(roomId,out float structureFog))
                    view.VisibleFog=structureFog;
                view.Sprite.enabled=shown;
                view.Sprite.transform.position=new Vector3(view.SourcePosition.x+dx,view.SourcePosition.y-dy,view.SourceDepth-dy*.001f);
                properties.Clear();properties.SetVector("_OwnerFog",new Vector4(view.Ground?0:1,view.VisibleFog,0,0));view.Sprite.SetPropertyBlock(properties);
            }
        }
        private static byte FogByte(Cell cell,float remembered)=>cell==null||!cell.Explored?(byte)0:cell.IsVisible?(byte)255:(byte)Mathf.RoundToInt(remembered*255);
        public void SetPresentationVisible(bool value){visible=value;if(content!=null)content.SetActive(value);}
        public bool TryPickWorld(Vector2 point,out Entity owner,out int x,out int y)=>TryPickWorld(point,out owner,out x,out y,out _,out _);
        public bool TryPickWorld(Vector2 point,out Entity owner,out int x,out int y,out int sortingOrder,out float depth)
        {
            owner=null;x=y=-1;sortingOrder=0;depth=0;
            if(!PresentationVisible||!Finite(point.x)||!Finite(point.y))return false;
            View hit=null;
            foreach(var view in views)
            {
                if(!view.Sprite.enabled||view.Owner==null||view.Layer.role=="interior")continue;
                Vector2 pixel=(point-(Vector2)view.Sprite.transform.position)*PixelsPerCell;
                int width=view.Layer.bounds[2],height=view.Layer.bounds[3];
                if(pixel.x<0||pixel.y<0||pixel.x>=width||pixel.y>=height)continue;
                float visibleFog=view.Ground?FogByte(CurrentZone.GetCell(Mathf.FloorToInt(point.x),24-Mathf.FloorToInt(point.y)),0)/255f:view.VisibleFog;
                if(visibleFog<.999f||view.Pixels[Mathf.FloorToInt(pixel.y)*width+Mathf.FloorToInt(pixel.x)].a==0)continue;
                if(hit==null||view.Sprite.sortingOrder>hit.Sprite.sortingOrder||(view.Sprite.sortingOrder==hit.Sprite.sortingOrder&&view.Sprite.transform.position.z<hit.Sprite.transform.position.z))hit=view;
            }
            if(hit==null)return false;
            var cell=CurrentZone.GetEntityCell(hit.Owner);if(cell==null)return false;
            owner=hit.Owner;x=cell.X;y=cell.Y;sortingOrder=hit.Sprite.sortingOrder;depth=hit.Sprite.transform.position.z;return true;
        }
        public bool TryPickImage(Vector2 point,out int x,out int y,out Entity owner)=>TryPickWorld(ImageToWorld(point),out owner,out x,out y);
        public void Unbind()
        {
            IsReady=false;Failure=null;definition=null;views.Clear();owners.Clear();currentOwners.Clear();revealedRooms.Clear();roomFog.Clear();roomBounds.Clear();
            if(content!=null){content.SetActive(false);DestroyOwned(content);}if(material!=null)DestroyOwned(material);if(fog!=null)DestroyOwned(fog);
            content=null;material=null;fog=null;CurrentZone=null;
        }
        private static void DestroyOwned(UnityEngine.Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy()=>Unbind();
    }
}
