using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>First Tent's open oath court and four unequal functional shelters.
    /// Only logical native cells: an oath is claimed in conversation, never from
    /// geometry or entering this court. Scope is the exact fresh surface address.</summary>
    public sealed class FirstTentCompositionPlan
    {
        public const string ZoneID="Overworld.5.17.0";
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
        public int OathX{get;}public int OathY{get;}
        /// <summary>Selected semantic relationship, stable for this address and seed.</summary>
        public string FormationName{get;}
        private readonly string[,] objects=new string[80,25];
        private readonly bool[,] interiors=new bool[80,25],approaches=new bool[80,25],reserved=new bool[80,25],court=new bool[80,25];
        private static readonly (int x,int y)[] Directions={(0,-1),(-1,0),(1,0),(0,1)};
        public static bool IsSupportedZone(string id)=>id==ZoneID;
        public static FirstTentCompositionPlan Create(string id,int seed)
        {if(!IsSupportedZone(id))throw new ArgumentException("The exact First Tent surface address is required.",nameof(id));return new FirstTentCompositionPlan(seed);}
        private FirstTentCompositionPlan(int seed)
        {
            Rooms=rooms.AsReadOnly();Profile=profile.AsReadOnly();var rng=new Random(FormationSelector.StableIndex(ZoneID+":"+seed,int.MaxValue));
            int formation=FormationSelector.StableIndex(ZoneID+":"+seed+":formation",3);
            FormationName=formation==0?"NorthwardWelcome":formation==1?"SouthwardWelcome":"SaltRoadGathering";
            bool saltSouth=formation==2;
            OathX=30+rng.Next(3);OathY=11;
            // Four poles make an open gathering boundary; they do not form an
            // altar or a wall. The two hosts remain the ordinary native role.
            for(int y=OathY-5;y<=OathY+5;y++)for(int x=OathX-6;x<=OathX+6;x++)court[x,y]=reserved[x,y]=true;
            AddRoom("GuestThreshold",8+rng.Next(3),2+rng.Next(2),12+rng.Next(3),6,true);
            AddRoom("PilgrimRest",10+rng.Next(4),18,16+rng.Next(3),6,false);
            AddRoom("SaltService",54+rng.Next(3),saltSouth?17:2+rng.Next(2),14+rng.Next(3),6,!saltSouth);
            AddRoom("Household",56+rng.Next(3),saltSouth?2:17,10+rng.Next(2),7,saltSouth);
            var guest=rooms[0];var salt=rooms[2];
            profile.Add(new ProfilePlacement("TentRightHost",guest.DoorX+3,guest.DoorY+2));
            profile.Add(new ProfilePlacement("GuestClothPole",guest.DoorX+1,guest.DoorY+3));
            profile.Add(new ProfilePlacement("Well",guest.DoorX+6,guest.DoorY+4));
            profile.Add(new ProfilePlacement("TentRightHost",OathX,OathY-1));
            foreach(var d in new[]{(-6,-5),(6,-5),(-6,5),(6,5)})profile.Add(new ProfilePlacement("GuestClothPole",OathX+d.Item1,OathY+d.Item2));
            profile.Add(new ProfilePlacement("SaltMaster",salt.DoorX-2,salt.DoorY+(saltSouth?-2:2)));
            foreach(var owner in profile)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(InBounds(owner.X+dx,owner.Y+dy))reserved[owner.X+dx,owner.Y+dy]=true;
            // Native population places its main well here after the profile.
            // Its open apron stays separate from the older guest-water well.
            for(int y=10;y<=14;y++)for(int x=38;x<=43;x++)
            {reserved[x,y]=true;if(CanRoute(x,y))approaches[x,y]=true;}
            reserved[40,12]=true;
            // The mapped road receives guests at the west threshold, skirts
            // the open oath court, then reaches water and the salt-service yard.
            Chain(new[]{(0,12),(7,12),(23,14),(OathX,14),(38,14),(43,14),(49,12),(74,12),(79,12)});
            Chain(new[]{(40,0),(44,3),(45,8),(43,10)});
            Chain(new[]{(40,24),(43,21),(46,18),(43,14)});
            Route(OathX,OathY,OathX,14,true);
            foreach(var room in rooms)JoinPublicRoute(room.DoorX,room.DoorY);
            // Walkable examination poles do not need individual paved spurs.
            // Their reserved Sand court already provides ordinary standing access.
            foreach(var owner in profile)if(owner.Blueprint!="GuestClothPole")JoinOwnerFrontage(owner);
            DressShoulders(rng);
            if(formation==1){ReflectNorthSouth();OathY=24-OathY;}
        }
        private void AddRoom(string role,int x,int y,int w,int h,bool south)
        {
            var room=new Room(role,x,y,w,h,south);rooms.Add(room);
            for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)
            {
                bool edge=xx==x||xx==x+w-1||yy==y||yy==y+h-1;bool door=yy==room.DoorY&&Math.Abs(xx-room.DoorX)<=(role=="GuestThreshold"?2:1);
                if(edge&&!door){objects[xx,yy]="TentWall";reserved[xx,yy]=true;}
                else if(!edge)interiors[xx,yy]=true;
                if(door)approaches[xx,yy]=reserved[xx,yy]=true;
            }
            // Concentrated activity, not even scatter. Long shared rest mats,
            // a short salt goods row, and private bedding read from the camera.
            if(role=="PilgrimRest")
            {Put(x+2,y+2,"Bed");Put(x+5,y+2,"Bed");Put(x+8,y+2,"Bed");Put(x+w-3,y+h-3,"Chair");}
            else if(role=="SaltService")
            {Put(x+2,y+2,"Crate");Put(x+4,y+2,"Crate");Put(x+w-3,y+2,"Crate");Put(x+2,y+h-3,"Chair");}
            else if(role=="GuestThreshold")
            {Put(x+2,y+2,"Chair");Put(x+w-3,y+2,"Chair");Put(x+w-3,y+h-3,"Crate");}
            else{Put(x+2,y+2,"Bed");Put(x+5,y+2,"Bed");Put(x+w-3,y+h-3,"Chair");}
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
            if(!CanRoute(sx,sy)||!CanRoute(tx,ty))throw new InvalidOperationException("First Tent route endpoint intersects an owner.");
            var start=(x:sx,y:sy);var target=(x:tx,y:ty);var q=new Queue<(int x,int y)>();var previous=new Dictionary<(int x,int y),(int x,int y)>();q.Enqueue(start);previous[start]=start;
            while(q.Count>0&&!previous.ContainsKey(target))
            {
                var at=q.Dequeue();foreach(var direction in Directions)
                {var next=(x:at.x+direction.x,y:at.y+direction.y);if(!CanRoute(next.x,next.y)||previous.ContainsKey(next))continue;previous[next]=at;q.Enqueue(next);}
            }
            if(!previous.ContainsKey(target))throw new InvalidOperationException("First Tent semantic destinations must connect.");
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
            // A seeded guest shelter can adjoin a monument corner. Route to
            // a real standing cell; never carve its wall to force a left apron.
            foreach(var d in new[]{(-1,0),(0,1),(1,0),(0,-1)})
            {
                int x=owner.X+d.Item1,y=owner.Y+d.Item2;
                if(!CanRoute(x,y)||interiors[x,y])continue;
                JoinPublicRoute(x,y);return;
            }
            throw new InvalidOperationException("First Tent owner has no exterior standing frontage: "+owner.Blueprint);
        }
        private void JoinPublicRoute(int x,int y)
        {
            int bx=-1,by=-1,best=int.MaxValue;
            for(int yy=0;yy<25;yy++)for(int xx=0;xx<80;xx++)
            {
                int distance=Math.Abs(xx-x)+Math.Abs(yy-y);if(distance<2||distance>=best||!approaches[xx,yy]||interiors[xx,yy]||!CanRoute(xx,yy))continue;
                bool doorway=false;foreach(var r in rooms)if(yy==r.DoorY&&Math.Abs(xx-r.DoorX)<=(r.Role=="GuestThreshold"?2:1))doorway=true;
                if(!doorway){best=distance;bx=xx;by=yy;}
            }
            if(bx<0)throw new InvalidOperationException("First Tent has no receiving route.");Route(x,y,bx,by,false);
        }
        private void DressShoulders(Random rng)
        {
            // Three small colonies leave much of the desert open. Rocks are
            // isolated so a decorative ring cannot trap a walkable pocket.
            foreach(var center in new[]{(4,4),(75,5),(74,21)})
            {
                int brush=0,rock=0;
                for(int attempt=0;attempt<140&&(brush<6||rock<2);attempt++)
                {
                    int x=center.Item1+rng.Next(-2,3),y=center.Item2+rng.Next(-2,3);
                    if(!InBounds(x,y)||reserved[x,y]||interiors[x,y]||objects[x,y]!=null)continue;
                    if(brush<6){Put(x,y,"DryBrush");brush++;}
                    else
                    {
                        bool clear=true;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                            if(!InBounds(x+dx,y+dy)||objects[x+dx,y+dy]=="Rock"||objects[x+dx,y+dy]=="TentWall")clear=false;
                        if(clear){Put(x,y,"Rock");rock++;}
                    }
                }
            }
        }
        private void ReflectNorthSouth()
        {
            // Transform the complete semantic graph together. A southern
            // receiving tent brings its host, pole, well and thresholds with it;
            // the native main well remains fixed at the middle row twelve.
            for(int y=0;y<12;y++)for(int x=0;x<80;x++)
            {
                int opposite=24-y;
                var o=objects[x,y];objects[x,y]=objects[x,opposite];objects[x,opposite]=o;
                Swap(interiors,x,y,opposite);Swap(approaches,x,y,opposite);Swap(reserved,x,y,opposite);Swap(court,x,y,opposite);
            }
            for(int i=0;i<rooms.Count;i++)
            {var r=rooms[i];rooms[i]=new Room(r.Role,r.X,25-r.Y-r.Height,r.Width,r.Height,r.DoorY==r.Y);}
            for(int i=0;i<profile.Count;i++)
            {var owner=profile[i];profile[i]=new ProfilePlacement(owner.Blueprint,owner.X,24-owner.Y);}
        }
        private static void Swap(bool[,] grid,int x,int y,int opposite)
        {bool value=grid[x,y];grid[x,y]=grid[x,opposite];grid[x,opposite]=value;}
        public string GroundAt(int x,int y)=>!InBounds(x,y)?null:interiors[x,y]?"StoneFloor":court[x,y]?"Sand":approaches[x,y]?"RoadStone":"Sand";
        public string ObjectAt(int x,int y)=>InBounds(x,y)?objects[x,y]:null;
        public bool IsInterior(int x,int y)=>InBounds(x,y)&&interiors[x,y];
        public bool IsApproach(int x,int y)=>InBounds(x,y)&&approaches[x,y];
        public bool IsReserved(int x,int y)=>InBounds(x,y)&&reserved[x,y];
        public bool IsOathCourt(int x,int y)=>InBounds(x,y)&&court[x,y];
        private static bool InBounds(int x,int y)=>x>=0&&y>=0&&x<80&&y<25;
        public string Signature()
        {
            var s=new StringBuilder(ZoneID).Append('|').Append(FormationName);foreach(var r in rooms)s.Append('|').Append(r.Role).Append(':').Append(r.X).Append(',').Append(r.Y).Append(',').Append(r.Width);
            foreach(var p in profile)s.Append('|').Append(p.Blueprint).Append(':').Append(p.X).Append(',').Append(p.Y);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)s.Append('|').Append(GroundAt(x,y)).Append(':').Append(objects[x,y]??".");return s.ToString();
        }
    }
}
