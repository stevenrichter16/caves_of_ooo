using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>A local flood-remnant lake with dry working fingers and three inhabited
    /// shore relationships. Geometry supplies no fishing or boat travel mechanic.</summary>
    public sealed class TineCompositionPlan
    {
        public const string ZoneID="Overworld.13.7.0",ProfileID="LakesideVillage";
        public sealed class Room
        {
            public readonly string Role;public readonly int X,Y,Width,Height,DoorX,DoorY;
            internal Room(string role,int x,int y,int width,int height,bool south)
            {Role=role;X=x;Y=y;Width=width;Height=height;DoorX=x+width/2;DoorY=south?y+height-1:y;}
        }
        public readonly struct ProfilePlacement
        {
            public readonly string Blueprint;public readonly int X,Y;
            internal ProfilePlacement(string bp,int x,int y){Blueprint=bp;X=x;Y=y;}
        }
        private readonly List<Room> rooms=new List<Room>();
        private readonly List<ProfilePlacement> profile=new List<ProfilePlacement>();
        private readonly Dictionary<string,(int x,int y)> services=new Dictionary<string,(int,int)>(StringComparer.Ordinal);
        public ReadOnlyCollection<Room> Rooms{get;}public ReadOnlyCollection<ProfilePlacement> Profile{get;}
        public string FormationName{get;}public int ScribeX=>services["Scribe"].x;public int ScribeY=>services["Scribe"].y;
        private readonly string[,] objects=new string[80,25];
        private readonly bool[,] interiors=new bool[80,25],approaches=new bool[80,25],reserved=new bool[80,25],wet=new bool[80,25];
        private static readonly (int x,int y)[] Directions={(0,-1),(-1,0),(1,0),(0,1)};
        public static bool IsSupportedZone(string id)=>id==ZoneID;
        public static TineCompositionPlan Create(string id,int seed)
        {if(!IsSupportedZone(id))throw new ArgumentException("The exact Tine surface address is required.",nameof(id));return new TineCompositionPlan(seed);}
        private TineCompositionPlan(int seed)
        {
            Rooms=rooms.AsReadOnly();Profile=profile.AsReadOnly();
            var rng=new Random(FormationSelector.StableIndex(ZoneID+":"+seed,int.MaxValue));
            int formation=FormationSelector.StableIndex(ZoneID+":"+seed+":formation",3),jitter=rng.Next(-1,2);
            FormationName=formation==0?"SouthernReach":formation==1?"EasternCove":"NorthernInlet";
            if(formation==0)
            {
                Lake(50+jitter,21,28,6);
                AddRoom("ScribeRetreat",5,3,14+rng.Next(2),7,true);
                AddRoom("LakeHouse",54,3,15+rng.Next(2),7,true);
                AddRoom("ShoreStore",5,16,14+rng.Next(2),7,false);
                Finger(30,14,3,7);Finger(62,14,3,7);Frame(31,13);Frame(63,13);
            }
            else if(formation==1)
            {
                Lake(68+jitter,12,10,11);
                AddRoom("ScribeRetreat",6,2,15+rng.Next(2),7,true);
                AddRoom("ShoreStore",31,3,16+rng.Next(2),7,true);
                AddRoom("LakeHouse",8,17,15+rng.Next(2),6,false);
                Finger(53,5,15,3);Finger(53,17,14,3);Frame(52,6);Frame(52,18);
            }
            else
            {
                Lake(50+jitter,4,28,5);
                AddRoom("ScribeRetreat",5,15,14+rng.Next(2),7,false);
                AddRoom("ShoreStore",54,15,16+rng.Next(2),7,false);
                AddRoom("LakeHouse",5,3,12+rng.Next(2),7,true);
                Finger(30,5,3,7);Finger(62,5,3,7);Frame(31,12);Frame(63,12);
            }
            // Population's existing main well remains at its native central cell.
            for(int y=10;y<=14;y++)for(int x=38;x<=44;x++)
            {if(wet[x,y]||objects[x,y]!=null)throw new InvalidOperationException("Tine main well needs a dry open apron.");reserved[x,y]=true;}
            // Routes are connections among actual useful places. A wide mapped
            // west/east road threads the dry bank; north/south arrival paths use
            // the real remaining shoreline instead of crossing invisible water.
            Route(0,12,39,12,true);Route(79,12,41,12,true);Route(39,12,41,12,true);
            Route(40,0,39,12,false);Route(40,24,41,12,false);
            foreach(var room in rooms)JoinRoute(room.DoorX,room.DoorY);
            foreach(var owner in profile)
            {
                foreach(var d in Directions)
                {int x=owner.X+d.x,y=owner.Y+d.y;if(!CanRoute(x,y)||interiors[x,y])continue;JoinRoute(x,y);break;}
            }
            DressShore(rng);
        }
        private void Lake(int cx,int cy,int rx,int ry)
        {
            for(int y=1;y<24;y++)for(int x=1;x<79;x++)
            {
                double nx=(x-cx)/(double)rx,ny=(y-cy)/(double)ry;
                if(nx*nx+ny*ny>1)continue;wet[x,y]=reserved[x,y]=true;objects[x,y]="WaterPuddle";
            }
        }
        private void Finger(int x,int y,int width,int depth)
        {
            for(int yy=y;yy<y+depth;yy++)for(int xx=x;xx<x+width;xx++)
            {wet[xx,yy]=false;objects[xx,yy]="Duckboard";reserved[xx,yy]=true;}
        }
        private void Frame(int x,int y)
        {
            if(wet[x,y]||objects[x,y]!=null)throw new InvalidOperationException("A hull work frame needs dry native ground.");
            profile.Add(new ProfilePlacement("BoatFrame",x,y));
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(InBounds(x+dx,y+dy)&&!wet[x+dx,y+dy])reserved[x+dx,y+dy]=true;
        }
        private void AddRoom(string role,int x,int y,int w,int h,bool south)
        {
            var r=new Room(role,x,y,w,h,south);rooms.Add(r);
            for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)
            {
                if(wet[xx,yy])throw new InvalidOperationException("Tine room intrudes into its local lake.");
                bool edge=xx==x||xx==x+w-1||yy==y||yy==y+h-1,door=yy==r.DoorY&&Math.Abs(xx-r.DoorX)<=1;
                if(edge&&!door)Put(xx,yy,"SandstoneWall");else if(!edge)interiors[xx,yy]=true;
                if(door)approaches[xx,yy]=reserved[xx,yy]=true;
            }
            if(role=="ScribeRetreat")
            {Put(x+2,y+2,"Chair");Put(x+w-3,y+2,"Crate");Service("Scribe",r.DoorX,y+3);}
            else if(role=="ShoreStore")
            {Put(x+2,y+2,"Crate");Put(x+4,y+2,"Crate");Put(x+w-3,y+2,"Crate");Put(x+2,y+h-3,"Chair");Service("Merchant",r.DoorX-1,y+3);Service("Quartermaster",r.DoorX+2,y+3);}
            else{Put(x+2,y+2,"Bed");Put(x+5,y+2,"Bed");Put(x+w-3,y+h-3,"Chair");Service("Innkeeper",r.DoorX,y+3);}
        }
        private void Service(string bp,int x,int y)
        {
            if(!interiors[x,y]||objects[x,y]!=null)throw new InvalidOperationException("A native service anchor needs free interior floor.");
            services.Add(bp,(x,y));
            // Leave the actor cell eligible for VillagePopulation's ordinary
            // service placement; its neighbor is protected for conversation.
            reserved[x,y+1]=true;
        }
        /// <summary>Planned native service cells, not actors. The caller must still
        /// validate the current graph and retain ordinary stock/dialogue behavior.</summary>
        public bool TryGetServiceCell(string blueprint,out int x,out int y)
        {x=y=-1;if(blueprint==null||!services.TryGetValue(blueprint,out var at))return false;x=at.x;y=at.y;return true;}
        private void Put(int x,int y,string bp){objects[x,y]=bp;reserved[x,y]=true;}
        private bool CanRoute(int x,int y)
        {
            if(!InBounds(x,y)||wet[x,y]||interiors[x,y]||(objects[x,y]!=null&&objects[x,y]!="Duckboard")||(x==40&&y==12))return false;
            foreach(var owner in profile)if(owner.X==x&&owner.Y==y)return false;return true;
        }
        private void Route(int sx,int sy,int tx,int ty,bool wide)
        {
            if(!CanRoute(sx,sy)||!CanRoute(tx,ty))throw new InvalidOperationException("Tine route endpoint is not dry exterior standing ground.");
            var start=(x:sx,y:sy);var target=(x:tx,y:ty);var q=new Queue<(int x,int y)>();var previous=new Dictionary<(int x,int y),(int x,int y)>();q.Enqueue(start);previous[start]=start;
            while(q.Count>0&&!previous.ContainsKey(target))
            {var at=q.Dequeue();foreach(var d in Directions){var n=(x:at.x+d.x,y:at.y+d.y);if(!CanRoute(n.x,n.y)||previous.ContainsKey(n))continue;previous[n]=at;q.Enqueue(n);}}
            if(!previous.ContainsKey(target))throw new InvalidOperationException("Tine semantic shores must connect.");
            for(var at=target;;at=previous[at])
            {approaches[at.x,at.y]=reserved[at.x,at.y]=true;if(wide&&CanRoute(at.x,at.y+1))approaches[at.x,at.y+1]=reserved[at.x,at.y+1]=true;if(at==start)break;}
        }
        private void JoinRoute(int x,int y)
        {
            int best=int.MaxValue,bx=-1,by=-1;
            for(int yy=0;yy<25;yy++)for(int xx=0;xx<80;xx++)
            {
                int distance=Math.Abs(x-xx)+Math.Abs(y-yy);if(distance<2||distance>=best||!approaches[xx,yy]||!CanRoute(xx,yy))continue;
                bool door=false;foreach(var r in rooms)if(yy==r.DoorY&&Math.Abs(xx-r.DoorX)<=1)door=true;
                if(!door){best=distance;bx=xx;by=yy;}
            }
            if(bx<0)throw new InvalidOperationException("Tine has no dry public route.");Route(x,y,bx,by,false);
        }
        private void DressShore(Random rng)
        {
            // Water has one native owner per wet cell. Reeds live along the dry
            // edge, leaving pool projection/removal unambiguous.
            var bank=new List<(int x,int y)>();
            for(int y=2;y<23;y++)for(int x=2;x<78;x++)
            {
                if(reserved[x,y]||interiors[x,y]||objects[x,y]!=null)continue;
                foreach(var d in Directions)if(wet[x+d.x,y+d.y]){bank.Add((x,y));break;}
            }
            for(int count=0;count<32&&bank.Count>0;count++)
            {int index=rng.Next(bank.Count);var at=bank[index];bank.RemoveAt(index);Put(at.x,at.y,"Reeds");}
            foreach(var center in new[]{(3,3),(75,3),(3,21)})
            {
                int trees=0,bush=0;
                for(int attempt=0;attempt<160&&(trees<2||bush<5);attempt++)
                {
                    int x=center.Item1+rng.Next(-2,3),y=center.Item2+rng.Next(-2,3);
                    if(!InBounds(x,y)||x<1||x>78||y<1||y>23||reserved[x,y]||interiors[x,y]||wet[x,y]||objects[x,y]!=null)continue;
                    if(bush<5){Put(x,y,"Bush");bush++;continue;}
                    bool clear=true;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(!InBounds(x+dx,y+dy)||wet[x+dx,y+dy]||objects[x+dx,y+dy]=="Tree"||objects[x+dx,y+dy]=="SandstoneWall")clear=false;
                    if(clear){Put(x,y,"Tree");trees++;}
                }
            }
        }
        public string GroundAt(int x,int y)=>!InBounds(x,y)?null:interiors[x,y]?"StoneFloor":approaches[x,y]?"RoadStone":"Floor";
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&interiors[x,y];public bool IsApproach(int x,int y)=>InBounds(x,y)&&approaches[x,y];
        public bool IsReserved(int x,int y)=>InBounds(x,y)&&reserved[x,y];public bool IsWet(int x,int y)=>InBounds(x,y)&&wet[x,y];
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<80&&y<25;
        public string Signature()
        {
            var s=new StringBuilder(ZoneID).Append('|').Append(FormationName);
            foreach(var r in rooms)s.Append('|').Append(r.Role).Append(':').Append(r.X).Append(',').Append(r.Y).Append(',').Append(r.Width);
            foreach(var p in profile)s.Append('|').Append(p.Blueprint).Append(':').Append(p.X).Append(',').Append(p.Y);
            foreach(var p in services)s.Append('|').Append(p.Key).Append(':').Append(p.Value.x).Append(',').Append(p.Value.y);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".");return s.ToString();
        }
    }
}
