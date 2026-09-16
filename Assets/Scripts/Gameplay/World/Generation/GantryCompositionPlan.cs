using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Gantry's exchange, records office, caravan rest and homes.
    /// Native cells and role relationships precede geometry. Three seeded
    /// formations share public crossings without duplicating a hard-coded town.</summary>
    public sealed class GantryCompositionPlan
    {
        public const string ZoneID="Overworld.7.8.0";
        public const string ProfileID="CrossroadsExchange";
        public sealed class Room
        {
            public readonly string Role;public readonly int X,Y,Width,Height,DoorX,DoorY;
            internal Room(string role,int x,int y,int w,int h,bool south)
            {Role=role;X=x;Y=y;Width=w;Height=h;DoorX=x+w/2;DoorY=south?y+h-1:y;}
        }
        public readonly struct ProfilePlacement
        {
            public readonly string Blueprint;public readonly int X,Y;
            internal ProfilePlacement(string bp,int x,int y){Blueprint=bp;X=x;Y=y;}
        }
        private readonly List<Room> rooms=new List<Room>();
        private readonly List<ProfilePlacement> profile=new List<ProfilePlacement>();
        public ReadOnlyCollection<Room> Rooms{get;}
        public ReadOnlyCollection<ProfilePlacement> Profile{get;}

        /// <summary>Selected semantic relationship, stable for this address and seed.</summary>
        public string FormationName{get;}
        private readonly string[,] objects=new string[80,25];
        private readonly bool[,] interiors=new bool[80,25],approaches=new bool[80,25],reserved=new bool[80,25],court=new bool[80,25];
        private static readonly (int x,int y)[] Directions={(0,-1),(-1,0),(1,0),(0,1)};
        public static bool IsSupportedZone(string id)=>id==ZoneID;
        public static GantryCompositionPlan Create(string id,int seed)
        {if(!IsSupportedZone(id))throw new ArgumentException("The exact Gantry surface address is required.",nameof(id));return new GantryCompositionPlan(seed);}
        private readonly Dictionary<string,(int x,int y)> serviceCells=new Dictionary<string,(int x,int y)>();
        private GantryCompositionPlan(int seed)
        {
            Rooms=rooms.AsReadOnly();Profile=profile.AsReadOnly();var rng=new Random(FormationSelector.StableIndex(ZoneID+":"+seed,int.MaxValue));
            int formation=FormationSelector.StableIndex(ZoneID+":"+seed+":formation",3);
            FormationName=formation==0?"CrossingExchange":formation==1?"CaravanForecourt":"RegistryCrossing";
            // Semantic roles change corners, facing the common crossing.
            // Width follows the activity rather than a shared lot size.
            string[][] roles={new[]{"ExchangeFloor","RegistryOffice","CaravanRest","Household"},new[]{"CaravanRest","RegistryOffice","ExchangeFloor","Household"},new[]{"RegistryOffice","ExchangeFloor","Household","CaravanRest"}};
            for(int i=0;i<4;i++)
            {
                string role=roles[formation][i];int width=role=="ExchangeFloor"?18:role=="CaravanRest"?19:role=="RegistryOffice"?14:11;
                int x=(i%2==0?8:53)+rng.Next(3),y=i<2?2:17;
                AddRoom(role,x,y,width,6+(i<2?1:0),i<2);
            }
            var office=rooms.Find(r=>r.Role=="RegistryOffice");var rest=rooms.Find(r=>r.Role=="CaravanRest");
            profile.Add(new ProfilePlacement("GantryRegistrar",office.X+3,office.Y+3));
            profile.Add(new ProfilePlacement("TentRightHost",rest.DoorX-3,rest.DoorY+(rest.Y<12?2:-2)));
            profile.Add(new ProfilePlacement("GuestClothPole",rest.DoorX+3,rest.DoorY+(rest.Y<12?2:-2)));
            profile.Add(new ProfilePlacement("GantryWayboard",34,10));
            foreach(var owner in profile)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(InBounds(owner.X+dx,owner.Y+dy))reserved[owner.X+dx,owner.Y+dy]=true;
            // The native main well arrives at (40,12). Leave a generous open
            // gathering apron without paving a line through the well itself.
            for(int y=10;y<=14;y++)for(int x=37;x<=43;x++)reserved[x,y]=court[x,y]=true;
            Chain(new[]{(0,12),(6,12),(28,13),(36,14),(44,14),(50,12),(76,12),(79,12)});
            Chain(new[]{(40,0),(43,4),(44,9),(44,14),(43,20),(40,24)});
            foreach(var room in rooms)JoinPublicRoute(room.DoorX,room.DoorY);
            foreach(var owner in profile)JoinOwnerFrontage(owner);
            SetService("Merchant","ExchangeFloor",7,3);SetService("Quartermaster","ExchangeFloor",11,3);
            SetService("Scribe","RegistryOffice",9,3);SetService("Innkeeper","CaravanRest",13,3);
            DressShoulders(rng);
        }
        private void SetService(string blueprint,string role,int dx,int dy)
        {
            var room=rooms.Find(r=>r.Role==role);int x=room.X+dx,y=room.Y+dy;
            if(objects[x,y]!=null||reserved[x,y]||!interiors[x,y])throw new InvalidOperationException("Gantry service anchor is occupied: "+blueprint);
            serviceCells.Add(blueprint,(x,y));
            // A reserved standing neighbor belongs to access, not the actor.
            for(int yy=y-1;yy<=y+1;yy++)for(int xx=x-1;xx<=x+1;xx++)
                if((xx!=x||yy!=y)&&objects[xx,yy]==null&&interiors[xx,yy])reserved[xx,yy]=true;
        }
        /// <summary>Preferred native service owner position. Consumers must still
        /// validate the current graph; these coordinates never recreate an actor.</summary>
        public bool TryGetServiceCell(string blueprint,out int x,out int y)
        {if(blueprint!=null&&serviceCells.TryGetValue(blueprint,out var at)){x=at.x;y=at.y;return true;}x=y=-1;return false;}
        private void AddRoom(string role,int x,int y,int w,int h,bool south)
        {
            var room=new Room(role,x,y,w,h,south);rooms.Add(room);
            for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)
            {
                bool edge=xx==x||xx==x+w-1||yy==y||yy==y+h-1;bool door=yy==room.DoorY&&(role=="ExchangeFloor"?xx>x&&xx<x+w-1:Math.Abs(xx-room.DoorX)<=1);
                if(edge&&!door){objects[xx,yy]=role=="CaravanRest"?"TentWall":"GantryTimberWall";reserved[xx,yy]=true;}
                else if(!edge)interiors[xx,yy]=true;
                if(door)approaches[xx,yy]=reserved[xx,yy]=true;
            }
            if(role=="CaravanRest")
            {Put(x+2,y+2,"Bed");Put(x+5,y+2,"Bed");Put(x+8,y+2,"Bed");Put(x+w-3,y+h-2,"Chair");}
            else if(role=="ExchangeFloor")
            {Put(x+3,y+2,"GantryExchangeCounter");Put(x+4,y+2,"GantryExchangeCounter");Put(x+3,y+h-3,"Crate");Put(x+w-3,y+2,"Crate");}
            else if(role=="RegistryOffice")
            {Put(x+3,y+2,"GantryRegistryDesk");Put(x+w-3,y+2,"Crate");Put(x+2,y+h-2,"Chair");}
            else{Put(x+2,y+2,"Bed");Put(x+5,y+2,"Bed");Put(x+w-3,y+h-2,"Chair");}
        }
        private void Put(int x,int y,string blueprint){objects[x,y]=blueprint;reserved[x,y]=true;}
        private bool CanRoute(int x,int y)
        {
            if(!InBounds(x,y)||objects[x,y]!=null||(x==40&&y==12))return false;
            foreach(var owner in profile)if(owner.X==x&&owner.Y==y)return false;
            return true;
        }
        private void Route(int sx,int sy,int tx,int ty,bool wide)
        {
            if(!CanRoute(sx,sy)||!CanRoute(tx,ty))throw new InvalidOperationException("Gantry route endpoint intersects an owner.");
            var start=(x:sx,y:sy);var target=(x:tx,y:ty);var q=new Queue<(int x,int y)>();var previous=new Dictionary<(int x,int y),(int x,int y)>();q.Enqueue(start);previous[start]=start;
            while(q.Count>0&&!previous.ContainsKey(target))
            {
                var at=q.Dequeue();foreach(var direction in Directions)
                {var next=(x:at.x+direction.x,y:at.y+direction.y);if(!CanRoute(next.x,next.y)||previous.ContainsKey(next))continue;previous[next]=at;q.Enqueue(next);}
            }
            if(!previous.ContainsKey(target))throw new InvalidOperationException("Gantry semantic destinations must connect.");
            for(var at=target;;at=previous[at])
            {
                approaches[at.x,at.y]=reserved[at.x,at.y]=true;
                if(wide&&CanRoute(at.x,at.y+1))approaches[at.x,at.y+1]=reserved[at.x,at.y+1]=true;
                if(at==start)break;
            }
        }
        private void Chain((int x,int y)[] points)
        {for(int i=1;i<points.Length;i++)Route(points[i-1].x,points[i-1].y,points[i].x,points[i].y,true);}
        private void JoinOwnerFrontage(ProfilePlacement owner)
        {
            // A seeded service can adjoin a working fixture. Route to
            // a real standing cell; never carve its wall to force a left apron.
            foreach(var d in new[]{(-1,0),(0,1),(1,0),(0,-1)})
            {
                int x=owner.X+d.Item1,y=owner.Y+d.Item2;
                if(!CanRoute(x,y))continue;
                JoinPublicRoute(x,y);return;
            }
            throw new InvalidOperationException("Gantry owner has no standing frontage: "+owner.Blueprint);
        }
        private void JoinPublicRoute(int x,int y)
        {
            int bx=-1,by=-1,best=int.MaxValue;
            for(int yy=0;yy<25;yy++)for(int xx=0;xx<80;xx++)
            {
                int distance=Math.Abs(xx-x)+Math.Abs(yy-y);if(distance<2||distance>=best||!approaches[xx,yy]||interiors[xx,yy]||!CanRoute(xx,yy))continue;
                bool doorway=false;foreach(var r in rooms)if(yy==r.DoorY&&(r.Role=="ExchangeFloor"?xx>=r.X&&xx<r.X+r.Width:Math.Abs(xx-r.DoorX)<=1))doorway=true;
                if(!doorway){best=distance;bx=xx;by=yy;}
            }
            if(bx<0)throw new InvalidOperationException("Gantry has no receiving route.");Route(x,y,bx,by,false);
        }
        private void DressShoulders(Random rng)
        {
            // Three small colonies leave the roadside grass open. Rocks are
            // isolated so a decorative ring cannot trap a walkable pocket.
            foreach(var center in new[]{(4,4),(75,5),(74,21)})
            {
                int brush=0,rock=0;
                for(int attempt=0;attempt<140&&(brush<6||rock<2);attempt++)
                {
                    int x=center.Item1+rng.Next(-2,3),y=center.Item2+rng.Next(-2,3);
                    if(!InBounds(x,y)||reserved[x,y]||interiors[x,y]||objects[x,y]!=null)continue;
                    if(brush<6){Put(x,y,"Bush");brush++;}
                    else
                    {
                        bool clear=true;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                            if(!InBounds(x+dx,y+dy)||objects[x+dx,y+dy]=="Rock"||objects[x+dx,y+dy]=="GantryTimberWall")clear=false;
                        if(clear){Put(x,y,"Rock");rock++;}
                    }
                }
            }
        }
        public string GroundAt(int x,int y)=>!InBounds(x,y)?null:interiors[x,y]?"StoneFloor":approaches[x,y]?"RoadStone":"Grass";
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&interiors[x,y];
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approaches[x,y];
        public bool IsReserved(int x,int y)=>InBounds(x,y)&&reserved[x,y];
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<80&&y<25;
        public string Signature()
        {
            var s=new StringBuilder(ZoneID).Append('|').Append(FormationName);foreach(var r in rooms)s.Append('|').Append(r.Role).Append(':').Append(r.X).Append(',').Append(r.Y).Append(',').Append(r.Width);
            foreach(var p in profile)s.Append('|').Append(p.Blueprint).Append(':').Append(p.X).Append(',').Append(p.Y);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".");return s.ToString();
        }
    }
}
