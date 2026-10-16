using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    public enum ThrowAimKind { Automatic, Entity, Ground }

    /// <summary>Intent owns a physical entity and one occupied contact, never a render proxy.</summary>
    public sealed class ThrowIntent
    {
        public readonly ThrowAimKind Kind;
        public readonly Entity Target;
        public readonly int X, Y;
        private ThrowIntent(ThrowAimKind kind, Entity target, int x, int y)
        { Kind=kind; Target=target; X=x; Y=y; }
        public static ThrowIntent AtCell(int x,int y) => new ThrowIntent(ThrowAimKind.Automatic,null,x,y);
        public static ThrowIntent AtEntity(Entity target,int x,int y) => new ThrowIntent(ThrowAimKind.Entity,target,x,y);
        public static ThrowIntent Ground(int x,int y) => new ThrowIntent(ThrowAimKind.Ground,null,x,y);
    }

    public readonly struct ThrowProfile
    {
        public readonly double Bottom, Height;
        public readonly bool Intercepts, Sealed;
        public double Top => Bottom+Height;
        public ThrowProfile(double bottom,double height,bool intercepts,bool sealedCover)
        { Bottom=bottom;Height=height;Intercepts=intercepts;Sealed=sealedCover; }
        public static ThrowProfile For(Entity entity)
        {
            if(entity==null)return default;
            var physics=entity.GetPart<PhysicsPart>();
            bool solid=entity.HasTag("Solid") || physics?.Solid==true
                || entity.GetPart<DoorPart>()?.IsClosed==true || entity.GetPart<SealedLibraryBarrierPart>()?.IsClosed==true;
            bool sealedCover=solid && (entity.HasTag("Wall") || entity.GetPart<DoorPart>()!=null
                || entity.GetPart<SealedLibraryBarrierPart>()!=null);
            bool medium=entity.HasPart<GasPoolPart>() || entity.HasPart<LiquidPoolPart>();
            bool creature=entity.HasTag("Creature");
            bool terrain=entity.HasTag("Terrain") || entity.HasTag("Wall");
            bool tangible=creature || solid || (!terrain && (physics!=null || entity.GetStat("Hitpoints")!=null
                || entity.HasPart<MaterialPart>() || entity.HasPart<ThermalPart>()))
                || entity.GetPart<DestructiblePart>()?.Gone==false && entity.HasPart<SpatialFootprintPart>();
            double height=creature?1.6 : solid?2.4 : physics?.Takeable==true?.15:1.0;
            // Presets may be declared as content tags without changing art or mass.
            if(entity.HasTag("ThrowLowCover"))height=.65;
            if(entity.HasTag("ThrowTallVegetation"))height=2.0;
            if(physics!=null && Finite(physics.ThrowHeight) && physics.ThrowHeight>=0 && physics.ThrowHeight<=32)
                height=physics.ThrowHeight;
            double bottom=physics!=null && Finite(physics.ThrowBase) && physics.ThrowBase>=0 && physics.ThrowBase<=32 ? physics.ThrowBase:0;
            bool intercepts=!medium && tangible && height>0;
            if(physics?.ThrowIntercept==0)intercepts=false;
            else if(physics?.ThrowIntercept==1)intercepts=height>0;
            // Open apertures have no collision interval even if an old saved
            // physics profile still describes the closed door panel.
            if(entity.GetPart<DoorPart>()?.IsClosed==false || entity.GetPart<SealedLibraryBarrierPart>()?.IsClosed==false)
                intercepts=false;
            return new ThrowProfile(bottom,height,intercepts,sealedCover || solid && height>=2.4);
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }

    /// <summary>Resolved centerline sample in logical cell coordinates plus physical height.
    /// Presentation may consume this copy; it never decides collision.</summary>
    public readonly struct ThrowFlightPoint
    {
        public readonly double X,Y,Height;
        public ThrowFlightPoint(double x,double y,double height){X=x;Y=y;Height=height;}
    }

    public sealed class ThrowTraceResult : LineTraceResult
    {
        public ThrowIntent Intent;
        public Cell BurstCell, RestCell;
        public bool Uncertain, AmbiguousContact;
        public readonly List<Point> VisibilityCells=new List<Point>();
        public readonly List<ThrowFlightPoint> FlightPath=new List<ThrowFlightPoint>();
        public double ContactFraction, ContactHeight;
    }

    /// <summary>Centerline XY supercover with a swept vertical radius. No RNG or Unity physics.
    /// Known-only traces stop before unknown cells and never inspect hidden owner profiles.</summary>
    public static class ThrowTrajectory
    {
        public const double Radius=.05, Epsilon=.00001;
        readonly struct Span
        {
            public readonly int X,Y; public readonly double Enter,Exit; public readonly bool Main;
            public Span(int x,int y,double enter,double exit,bool main=true){X=x;Y=y;Enter=enter;Exit=exit;Main=main;}
        }

        public static bool IsKnownOwner(Cell cell,Entity entity) => cell!=null && cell.Explored && cell.IsVisible
            && entity!=null && entity.GetPart<RenderPart>()?.Visible==true && cell.Occupants.Contains(entity);

        public static ThrowIntent ResolveIntent(Zone zone,Entity actor,int x,int y,bool knownOnly=false)
        {
            var cell=zone?.GetCell(x,y);Entity chosen=null;bool ambiguous=false;
            if(cell==null || knownOnly && (!cell.Explored || !cell.IsVisible))return ThrowIntent.Ground(x,y);
            for(int i=0;i<cell.Occupants.Count;i++)
            {
                var e=cell.Occupants[i];if(e==actor || knownOnly && !IsKnownOwner(cell,e))continue;
                if(!ThrowProfile.For(e).Intercepts)continue;
                if(chosen==null || (e.HasTag("Creature") && !chosen.HasTag("Creature"))
                    || e.HasTag("Creature")==chosen.HasTag("Creature") && string.CompareOrdinal(e.ID,chosen.ID)<0)
                {chosen=e;ambiguous=false;}
                else if(e!=chosen && e.HasTag("Creature")==chosen.HasTag("Creature") && e.ID==chosen.ID)ambiguous=true;
            }
            return chosen==null || ambiguous?ThrowIntent.Ground(x,y):ThrowIntent.AtEntity(chosen,x,y);
        }

        public static bool ValidateIntent(Zone zone,Entity actor,ThrowIntent intent,bool requireVisible,out string reason)
        {
            reason=null;
            if(zone==null || actor==null || intent==null || !zone.InBounds(intent.X,intent.Y))
            {reason="Choose a valid cell.";return false;}
            if(intent.Kind!=ThrowAimKind.Entity)return true;
            if(intent.Target==null || intent.Target==actor){reason="Choose another physical target or aim at the ground.";return false;}
            var cell=zone.GetCell(intent.X,intent.Y);bool present=false;
            for(int i=0;i<cell.Occupants.Count;i++)if(cell.Occupants[i]==intent.Target){present=true;break;}
            if(!present || requireVisible && !IsKnownOwner(cell,intent.Target) || !ThrowProfile.For(intent.Target).Intercepts)
            {reason="That target is no longer available there.";return false;}
            return true;
        }

        public static ThrowTraceResult Trace(Zone zone,Entity actor,Entity item,ThrowIntent intent,int maxRange,bool knownOnly=false)
        {
            var result=new ThrowTraceResult{Intent=intent};var start=zone?.GetEntityCell(actor);
            if(start==null || intent==null || !zone.InBounds(intent.X,intent.Y) || maxRange<0
                || AIHelpers.ChebyshevDistance(start.X,start.Y,intent.X,intent.Y)>maxRange)return result;
            if(intent.Kind==ThrowAimKind.Automatic)intent=ResolveIntent(zone,actor,intent.X,intent.Y,knownOnly);
            result.Intent=intent;
            if(!ValidateIntent(zone,actor,intent,knownOnly,out _))return result;
            result.ImpactCell=start;result.LastTraversableCell=CanRestItem(zone,item,start,actor,knownOnly)?start:null;
            if(intent.X==start.X && intent.Y==start.Y && intent.Kind==ThrowAimKind.Ground)
            {result.RestCell=result.BurstCell=result.LastTraversableCell;return result;}
            var launch=ThrowProfile.For(actor);double z0=launch.Bottom+.65*launch.Height;
            var aim=ThrowProfile.For(intent.Target);double z1=intent.Kind==ThrowAimKind.Entity?aim.Bottom+.5*aim.Height:Radius;
            var spans=Supercover(start.X,start.Y,intent.X,intent.Y);
            foreach(var span in spans)result.VisibilityCells.Add(new Point(span.X,span.Y));
            double best=double.PositiveInfinity,unknown=double.PositiveInfinity;Entity hit=null;Cell contact=null;bool ambiguousHit=false;
            for(int n=0;n<spans.Count;n++)
            {
                var span=spans[n];var cell=zone.GetCell(span.X,span.Y);
                if(knownOnly && (!cell.Explored || !cell.IsVisible)){unknown=Math.Min(unknown,span.Enter);continue;}
                if(span.Enter>unknown+Epsilon || span.Enter>best+Epsilon)continue;
                for(int i=0;i<cell.Occupants.Count;i++)
                {
                    var e=cell.Occupants[i];if(e==actor || e==item || knownOnly && !IsKnownOwner(cell,e))continue;
                    var profile=ThrowProfile.For(e);if(!profile.Intercepts)continue;
                    if(!Overlap(span,z0,z1,profile,out double t))continue;
                    bool tied=Math.Abs(t-best)<=Epsilon;
                    bool identityConflict=tied && hit!=null && hit!=e && Priority(e,intent)==Priority(hit,intent) && e.ID==hit.ID;
                    if(t<best-Epsilon || tied && Prefer(e,cell,hit,contact,intent))
                    {best=t;hit=e;contact=cell;ambiguousHit=identityConflict;}
                    else if(identityConflict)ambiguousHit=true;
                }
            }
            // Equality is uncertain too: inspecting a known neighboring corner
            // must not disclose whether the unobserved side contains cover.
            bool uncertain=unknown<=best+Epsilon;double stop=uncertain?unknown:best;
            result.Uncertain=uncertain && !double.IsPositiveInfinity(unknown);
            if(result.Uncertain){hit=null;contact=null;}
            foreach(var span in spans)
            {
                if(span.Enter>stop+Epsilon)break;
                var cell=zone.GetCell(span.X,span.Y);
                if(result.Uncertain && span.Enter>=unknown-Epsilon)break;
                // The diagonal cell begins exactly when a corner-side barrier
                // is contacted. It is beyond that contact, not a resting cell.
                if(contact!=null && span.Main && cell!=contact && span.Enter>=best-Epsilon)continue;
                if(span.X!=start.X || span.Y!=start.Y)
                {
                    if(span.Main || cell==contact)result.Path.Add(new Point(span.X,span.Y));
                }
                if(span.Main)
                {
                    result.ImpactCell=cell;
                    if(CanRestItem(zone,item,cell,actor,knownOnly))result.LastTraversableCell=cell;
                }
            }
            result.AmbiguousContact=hit!=null && ambiguousHit;
            result.HitEntity=!result.AmbiguousContact && IsTargetable(hit)?hit:null;
            if(contact!=null)
            {
                result.ImpactCell=contact;result.ContactFraction=best;result.ContactHeight=z0+(z1-z0)*best;
                result.BlockedBySolid=ThrowProfile.For(hit).Sealed || !CanRest(contact,actor,knownOnly);
                if(result.Path.Count==0 || result.Path[result.Path.Count-1].X!=contact.X || result.Path[result.Path.Count-1].Y!=contact.Y)
                    result.Path.Add(new Point(contact.X,contact.Y));
            }
            result.RestCell=CanRestItem(zone,item,result.ImpactCell,actor,knownOnly)?result.ImpactCell:result.LastTraversableCell;
            result.BurstCell=result.RestCell;
            double dx=intent.X-start.X,dy=intent.Y-start.Y,lengthSquared=dx*dx+dy*dy;
            for(int i=0;i<result.Path.Count;i++)
            {
                var point=result.Path[i];
                double t=lengthSquared==0?1:((point.X-start.X)*dx+(point.Y-start.Y)*dy)/lengthSquared;
                t=Math.Max(0,Math.Min(Math.Min(1,stop),t));
                if(i==result.Path.Count-1 && contact!=null)t=best;
                result.FlightPath.Add(new ThrowFlightPoint(start.X+dx*t,start.Y+dy*t,z0+(z1-z0)*t));
            }
            return result;
        }

        static bool IsTargetable(Entity entity) => entity!=null && (!(entity.HasTag("Wall") || entity.HasTag("Terrain"))
            || entity.HasPart<SpatialFootprintPart>() && entity.GetPart<DestructiblePart>()?.Gone==false);

        public static bool CanRest(Cell cell,Entity actor,bool knownOnly=false)
        {
            if(cell==null || knownOnly && (!cell.Explored || !cell.IsVisible))return false;
            for(int i=0;i<cell.Occupants.Count;i++)
            {
                var e=cell.Occupants[i];if(e==actor || e.HasTag("Creature") || knownOnly && !IsKnownOwner(cell,e))continue;
                if(e.GetPart<DoorPart>()?.IsClosed==false || e.GetPart<SealedLibraryBarrierPart>()?.IsClosed==false)continue;
                if(e.HasTag("Solid") || e.GetPart<PhysicsPart>()?.Solid==true || e.GetPart<DoorPart>()?.IsClosed==true
                    || e.GetPart<SealedLibraryBarrierPart>()?.IsClosed==true)return false;
            }
            return true;
        }
        public static bool CanRestItem(Zone zone,Entity item,Cell cell,Entity actor,bool knownOnly=false)
        {
            if(zone==null || item==null || cell==null)return false;
            var occupied=zone.GetOccupiedCells(item,cell.X,cell.Y);if(occupied.Count==0)return false;
            foreach(var part in occupied)if(!CanRest(part,actor,knownOnly))return false;
            return true;
        }
        static int Priority(Entity entity,ThrowIntent intent)
            => ThrowProfile.For(entity).Sealed?0:entity==intent.Target?1:entity.HasTag("Creature")?2:3;
        static bool Prefer(Entity entity,Cell cell,Entity current,Cell currentCell,ThrowIntent intent)
        {
            if(current==null)return true;
            int a=Priority(entity,intent),b=Priority(current,intent);if(a!=b)return a<b;
            int id=string.CompareOrdinal(entity.ID,current.ID);if(id!=0)return id<0;
            return cell.Y<currentCell.Y || cell.Y==currentCell.Y && cell.X<currentCell.X;
        }
        static bool Overlap(Span span,double z0,double z1,ThrowProfile profile,out double t)
        {
            double low=profile.Bottom-Radius-Epsilon,high=profile.Top+Radius+Epsilon,dz=z1-z0;
            t=span.Enter;if(Math.Abs(dz)<Epsilon)return z0>=low && z0<=high;
            double first=(low-z0)/dz,last=(high-z0)/dz;if(first>last){double swap=first;first=last;last=swap;}
            t=Math.Max(span.Enter,first);return t<=Math.Min(span.Exit,last)+Epsilon;
        }
        static List<Span> Supercover(int sx,int sy,int tx,int ty)
        {
            var spans=new List<Span>();int x=sx,y=sy,dx=tx-sx,dy=ty-sy,stepX=Math.Sign(dx),stepY=Math.Sign(dy);
            double nextX=dx==0?double.PositiveInfinity:.5/Math.Abs(dx),nextY=dy==0?double.PositiveInfinity:.5/Math.Abs(dy);
            double deltaX=dx==0?double.PositiveInfinity:1.0/Math.Abs(dx),deltaY=dy==0?double.PositiveInfinity:1.0/Math.Abs(dy),enter=0;
            for(int guard=0;guard<Zone.Width+Zone.Height+2;guard++)
            {
                double exit=Math.Min(1,Math.Min(nextX,nextY));spans.Add(new Span(x,y,enter,exit));if(exit>=1)break;
                bool crossX=nextX<=nextY+Epsilon,crossY=nextY<=nextX+Epsilon;
                if(crossX && crossY)
                {spans.Add(new Span(x+stepX,y,exit,exit,false));spans.Add(new Span(x,y+stepY,exit,exit,false));}
                if(crossX){x+=stepX;nextX+=deltaX;}if(crossY){y+=stepY;nextY+=deltaY;}enter=exit;
            }
            return spans;
        }
    }
}
