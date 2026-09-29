using System;
using System.Collections.Generic;
using System.Linq;
namespace CavesOfOoo.Core
{
    /// <summary>Cold composition of one optional hauling shortcut from the exact
    /// original load and twelve terrain Hedges. No actor, stock, new material or
    /// runtime rewrite authority; saved graphs never replay this helper.</summary>
    public static class SpreadExplorationHauling
    {
        const int HedgeCount=12,HalfWall=6,MaxCenters=32;
        static readonly (int x,int y)[] Axes={(0,1),(1,0),(0,-1),(-1,0)};
        internal static bool EligibleLoad(Zone z,Entity e)
        {
            var p=e?.GetPart<PhysicsPart>();var h=e?.GetPart<HandlingPart>();
            return z!=null&&e!=null&&(e.BlueprintName=="FallenBeam"||e.BlueprintName=="HaulBarrel")
                &&e.SpatialZone==z&&z.GetEntityCell(e)!=null&&p?.ParentEntity==e&&p.Solid&&!p.Takeable
                &&p.InInventory==null&&p.Equipped==null&&h?.ParentEntity==e&&!h.Carryable&&h.MinLiftStrength==0
                &&h.Weight>0&&h.Weight<=144&&e.Parts.All(v=>v!=null&&v.ParentEntity==e)
                &&!e.HasPart<DraggedPart>()&&!e.HasPart<DragPart>()&&!e.HasPart<SpatialFootprintPart>()
                &&!e.HasPart<InventoryPart>()&&!e.HasPart<ContainerPart>()&&!e.HasTag("Creature")
                &&!e.HasTag("Owned")&&!e.HasTag("QuestItem")&&!e.HasTag("Unique")
                &&!z.GenReservedCells.Contains(z.GetEntityPosition(e))&&!z.GetEntityCell(e).IsInterior
                &&z.TileState.Get(z.GetEntityPosition(e).x,z.GetEntityPosition(e).y)?.IsEmpty!=false;
        }
        /// <summary>Relocates at most thirteen exact original owners in one cold
        /// attempt. The caller's authority is rechecked around every mutation;
        /// the returned proof pins the final packet and both useful routes.</summary>
        public static bool TryPlace(Zone zone,SpreadCompositionBuilder terrain,HaulablePropBuilder producer,
            Func<bool> authority,out Entity load,out Func<bool> finalState)
        {
            load=null;finalState=null;var source=producer?.SourceReceipt;
            bool Source()=>source!=null&&source.Zone==zone&&source.Owners.Count==1&&producer.OwnsSourceReceipt(source)
                &&source.IsCurrent&&EligibleLoad(zone,source.Owners[0])&&terrain?.SourceZone==zone
                &&terrain.Plan?.Formation==Formation.Hedgerow&&terrain.CapturePassageSources;
            if(authority==null||!Source())return false;
            var initialOwners=new HashSet<Entity>(zone.GetReadOnlyEntities());
            var initialState=SpreadGenerationReceipt.CaptureFinalState(zone,initialOwners.Where(e=>!DoorPart.IsBareGround(e)));
            if(!authority()||!Source()||!initialOwners.SetEquals(zone.GetReadOnlyEntities())||!initialState())return false;
            var heavy=source.Owners[0];var candidates=terrain.PassageSources.Where(r=>r.Zone==zone&&r.Factory==source.Factory&&r.Owners.Count==1
                &&r.IsCurrent&&SpreadExplorationPassage.Eligible(zone,r.Owners[0])).ToArray();
            if(candidates.Length<HedgeCount)return false;
            var design=Find(zone,heavy,candidates);if(design==null)return false;design.Plan=terrain.Plan;
            var receipts=design.Hedges.Select(e=>terrain.PassageSource(e)).Concat(new[]{source}).ToArray();
            var owners=design.Hedges.Concat(new[]{heavy}).ToArray();var selected=new HashSet<Entity>(owners);
            var originalAt=owners.Select(zone.GetEntityPosition).ToArray();var expected=originalAt.ToArray();
            var destinations=design.Wall.Concat(new[]{design.Center}).ToArray();
            var allOwners=new HashSet<Entity>(zone.GetReadOnlyEntities());
            var unchanged=SpreadGenerationReceipt.CaptureFinalState(zone,allOwners.Where(e=>!selected.Contains(e)&&!DoorPart.IsBareGround(e)));
            bool Others()=>allOwners.SetEquals(zone.GetReadOnlyEntities())&&unchanged();
            var originalGeometry=new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>());
            bool Owned()=>receipts.All(r=>r!=null&&r.MatchesOwnedState())
                &&owners.Select((e,i)=>zone.GetEntityPosition(e)==expected[i]).All(v=>v);
            bool Identity()=>terrain.SourceZone==zone&&terrain.Plan==design.Plan&&producer.OwnsSourceReceipt(source)
                &&receipts.Take(HedgeCount).All(terrain.OwnsPassageReceipt);
            if(!authority()||!Source()||!Others()||!receipts.All(r=>r.IsCurrent)||!design.Fits(zone,selected,originalGeometry))return false;
            foreach(var r in receipts)if(!r.TryConsume())return false;
            bool success=false;
            try
            {
                for(int i=0;i<owners.Length;i++)
                {
                    if(!authority()||!Identity()||!Owned()||!Others())return false;
                    if(expected[i]==destinations[i])continue;
                    if(!zone.MoveEntity(owners[i],destinations[i].x,destinations[i].y))return false;
                    expected[i]=destinations[i];
                    if(!authority()||!Identity()||!Owned()||!Others())return false;
                }
                var packet=SpreadGenerationReceipt.CaptureFinalState(zone,owners);
                bool Final()=>authority()&&Identity()&&packet()&&EligibleLoad(zone,heavy)
                    &&design.Fits(zone,selected,originalGeometry);
                if(!Final()||!Others())return false;load=heavy;finalState=Final;success=true;return true;
            }
            finally
            {
                if(!success)
                {
                    // An independent changed/replaced/transferred owner is not
                    // ours to reclaim. Only clean owned moves can be restored.
                    for(int i=owners.Length-1;i>=0;i--)
                    {
                        if(expected[i]==originalAt[i]||!receipts[i].MatchesOwnedState()||zone.GetEntityPosition(owners[i])!=expected[i])continue;
                        var clean=new HashSet<Entity>(owners.Where((e,j)=>receipts[j].MatchesOwnedState()&&zone.GetEntityPosition(e)==expected[j]));
                        if(new SpreadWildernessSituationBuilder.Geometry(zone,clean).Place(originalAt[i].x,originalAt[i].y)
                            &&zone.MoveEntity(owners[i],originalAt[i].x,originalAt[i].y))expected[i]=originalAt[i];
                    }
                }
            }
        }
        sealed class Design
        {
            internal SpreadCompositionPlan Plan;internal (int x,int y) Center,Axis;internal (int x,int y)[] Wall;internal Entity[] Hedges;
            internal (int x,int y) At(int u,int v)=>(Center.x+u*Axis.y+v*Axis.x,Center.y-u*Axis.x+v*Axis.y);
            internal IEnumerable<(int x,int y)> Required()
            {
                for(int i=-HalfWall;i<=HalfWall;i++)yield return At(i,0);
                for(int y=-4;y<=-1;y++)for(int x=-2;x<=2;x++)yield return At(x,y);
                yield return At(0,1);
            }
            internal bool Fits(Zone zone,HashSet<Entity> ignored,SpreadWildernessSituationBuilder.Geometry original)
            {
                var g=new SpreadWildernessSituationBuilder.Geometry(zone,ignored);
                if(!Required().All(p=>g.Place(p.x,p.y)))return false;
                var blocked=new HashSet<(int x,int y)>(Wall){Center};
                int detour=Distance(g,At(0,-1),At(0,1),blocked);
                blocked.Remove(Center);blocked.Add(At(0,-2));int clear=Distance(g,At(0,-3),At(0,1),blocked);
                return detour>=14&&detour<=60&&clear>=0&&clear<=6&&detour>=4+clear+6
                    &&g.Flood(new HashSet<(int x,int y)>(Wall){Center},null,int.MaxValue,null)[At(0,-1).x,At(0,-1).y]
                    &&g.PreservesAgainst(original,Wall.Concat(new[]{Center}).ToArray())
                    &&g.PreservesAgainst(original,Wall.Concat(new[]{At(0,-2)}).ToArray());
            }
        }
        static Design Find(Zone zone,Entity load,SpreadGenerationReceipt[] receipts)
        {
            var hedges=receipts.Select(r=>r.Owners[0]).ToArray();var all=new HashSet<Entity>(hedges){load};
            var admission=new SpreadWildernessSituationBuilder.Geometry(zone,all);
            var original=new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>());
            var at=zone.GetEntityPosition(load);int centers=0;
            var points=from y in Enumerable.Range(4,Zone.Height-8) from x in Enumerable.Range(4,Zone.Width-8)
                orderby Math.Abs(x-at.x)+Math.Abs(y-at.y),y,x select(x,y);
            foreach(var point in points)
            {
                var choices=new List<Design>();
                foreach(var axis in Axes)
                {
                    var d=new Design{Center=point,Axis=axis};
                    if(new[]{d.At(-7,-4),d.At(7,-4),d.At(-7,4),d.At(7,4)}.Any(p=>p.x<4||p.y<4||p.x>=Zone.Width-4||p.y>=Zone.Height-4))continue;
                    var required=d.Required().ToArray();if(!required.All(p=>admission.Place(p.x,p.y)))continue;
                    // Owners currently in required cells must belong to this
                    // same bounded packet, including ones vacating the shoulder.
                    var forced=hedges.Where(e=>required.Contains(zone.GetEntityPosition(e))).ToArray();if(forced.Length>HedgeCount)continue;
                    var chosen=forced.Concat(hedges.Except(forced).OrderBy(e=>Math.Abs(zone.GetEntityPosition(e).x-point.x)+Math.Abs(zone.GetEntityPosition(e).y-point.y)).ThenBy(e=>zone.GetEntityPosition(e).y).ThenBy(e=>zone.GetEntityPosition(e).x)).Take(HedgeCount).ToList();
                    if(chosen.Count!=HedgeCount)continue;
                    d.Wall=Enumerable.Range(-HalfWall,HalfWall*2+1).Where(i=>i!=0).Select(i=>d.At(i,0)).ToArray();d.Hedges=new Entity[HedgeCount];
                    for(int i=0;i<HedgeCount;i++){var existing=chosen.FirstOrDefault(e=>zone.GetEntityPosition(e)==d.Wall[i]);if(existing!=null){d.Hedges[i]=existing;chosen.Remove(existing);}}
                    for(int i=0;i<HedgeCount;i++)if(d.Hedges[i]==null){d.Hedges[i]=chosen[0];chosen.RemoveAt(0);}
                    choices.Add(d);
                }
                if(choices.Count==0)continue;if(++centers>MaxCenters)return null;
                foreach(var d in choices)
                {
                    var selected=new HashSet<Entity>(d.Hedges){load};
                    if(d.Fits(zone,selected,original))return d;
                }
            }
            return null;
        }
        // Equal-cost eight-direction movement matches actual native steps; no
        // diagonal corner rule is invented for these single-cell owners.
        static int Distance(SpreadWildernessSituationBuilder.Geometry g,(int x,int y) start,(int x,int y) end,HashSet<(int x,int y)> blocked)
        {
            if(!g.Walk(start.x,start.y,blocked)||!g.Walk(end.x,end.y,blocked))return -1;
            var seen=new bool[Zone.Width,Zone.Height];var q=new Queue<(int x,int y,int d)>();q.Enqueue((start.x,start.y,0));seen[start.x,start.y]=true;
            while(q.Count>0){var p=q.Dequeue();if((p.x,p.y)==end)return p.d;if(p.d>=60)continue;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int x=p.x+dx,y=p.y+dy;if((dx==0&&dy==0)||!g.Walk(x,y,blocked)||seen[x,y])continue;seen[x,y]=true;q.Enqueue((x,y,p.d+1));}}
            return -1;
        }
    }
}
