using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
namespace CavesOfOoo.Core
{
    /// <summary>Cold, opt-in substitution of one exact terrain-produced hedge.
    /// Caller supplies current generation-attempt authority; this never edits a
    /// loaded/live passage, relocates stock, or grants a second terrain budget.</summary>
    public static class SpreadExplorationPassage
    {
        internal static bool Eligible(Zone z,Entity e)
        {
            if(z==null||e?.BlueprintName!="Hedge"||e.SpatialZone!=z||z.GetEntityCell(e)==null)return false;
            var p=e.GetPart<PhysicsPart>();var hp=e.GetPart<DestructiblePart>();var at=z.GetEntityPosition(e);
            return p?.ParentEntity==e&&p.Solid&&!p.Takeable&&p.InInventory==null&&p.Equipped==null
                &&hp?.ParentEntity==e&&hp.HP==10&&hp.MaxHP==10&&!hp.Gone&&!hp.Indestructible
                &&e.GetPart<RenderPart>()?.ParentEntity==e&&!e.HasTag("Owned")&&!e.HasTag("QuestItem")&&!e.HasTag("Unique")
                &&!e.HasPart<InventoryPart>()&&!e.HasPart<ContainerPart>()&&!e.HasTag("Creature")&&!e.HasPart<SpatialFootprintPart>()
                &&at.x>=4&&at.y>=4&&at.x<Zone.Width-4&&at.y<Zone.Height-4
                &&!z.GenReservedCells.Contains(at)&&!z.GetCell(at.x,at.y).IsInterior
                &&z.TileState.Get(at.x,at.y)?.IsEmpty!=false;
        }
        public static bool TryPlace(Zone zone,EntityFactory factory,SpreadCompositionBuilder producer,Entity hedge,
            Func<bool> authority,out Entity gate,out Func<bool> finalState)
            =>TryPlace(zone,factory,producer,hedge,authority,false,out gate,out finalState);
        public static bool TryPlace(Zone zone,EntityFactory factory,SpreadCompositionBuilder producer,Entity hedge,
            Func<bool> authority,bool buckled,out Entity gate,out Func<bool> finalState)
        {
            gate=null;finalState=null;
            string blueprint=buckled?"GleanersBuckledWicket":"SpreadFieldGate";
            var receipt=producer?.PassageSource(hedge);
            bool Source()=>Eligible(zone,hedge)&&receipt!=null&&receipt.Zone==zone&&ReferenceEquals(receipt.Factory,factory)
                &&ReferenceEquals(producer.PassageSource(hedge),receipt)&&receipt.IsCurrent;
            if(factory==null||authority==null||!Source()||!authority()||!Source()||!factory.Blueprints.ContainsKey(blueprint))return false;
            var at=zone.GetEntityPosition(hedge);
            var original=new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>());
            var opened=new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>{hedge});
            if(!opened.Place(at.x,at.y))return false;
            bool horizontal=false;Entity[] sides=null;bool found=false;
            foreach(bool crossX in new[]{true,false})
            {
                var first=zone.GetCell(at.x-(crossX?0:1),at.y-(crossX?1:0)).Occupants.Where(e=>e.BlueprintName=="Hedge").ToArray();
                var second=zone.GetCell(at.x+(crossX?0:1),at.y+(crossX?1:0)).Occupants.Where(e=>e.BlueprintName=="Hedge").ToArray();
                if(first.Length!=1||second.Length!=1)continue;
                if(!Layout(zone,hedge,at,crossX,original))continue;
                horizontal=crossX;sides=new[]{first[0],second[0]};found=true;break;
            }
            if(!found)return false;
            var sideProof=SpreadGenerationReceipt.CaptureFinalState(zone,sides);
            bool Ready()=>Source()&&sideProof()&&Layout(zone,hedge,at,horizontal,original);
            if(!authority()||!Ready())return false;
            var made=factory.CreateEntity(blueprint);
            if(!ValidGate(made,false,buckled)||!authority()||!Ready()||!ValidGate(made,false,buckled)
                ||zone.GetReadOnlyEntities().Any(e=>e.ID==made.ID))return false;
            made.GetPart<DoorPart>().QuarterTurns=horizontal?1:0;
            if(!receipt.TryConsume())return false;
            bool removed=false,placed=false,success=false;Func<bool> detached=null,gateProof=null;
            try
            {
                removed=zone.RemoveEntity(hedge);if(!removed)return false;
                detached=SpreadGenerationReceipt.CaptureDetachedState(hedge);
                if(!detached())return false;
                placed=zone.AddEntity(made,at.x,at.y);if(!placed)return false;
                gateProof=SpreadGenerationReceipt.CaptureFinalState(zone,new[]{made});
                bool Final()=>authority()&&producer.OwnsPassageReceipt(receipt)&&sideProof()&&detached()
                    &&!zone.GetReadOnlyEntities().Any(e=>e.ID==hedge.ID)
                    &&gateProof()&&ValidGate(made,true,buckled)&&zone.GetEntityPosition(made)==at
                    &&Layout(zone,made,at,horizontal,original);
                if(!Final())return false;
                gate=made;finalState=Final;success=true;return true;
            }
            finally
            {
                if(!success)
                {
                    // Preserve independent callback replacements/state. The caller
                    // must reject a changed packet instead of accepting it as refusal.
                    if(placed&&gateProof?.Invoke()==true)zone.RemoveEntity(made);
                    if(removed&&detached?.Invoke()==true&&!zone.GetReadOnlyEntities().Any(e=>e.ID==hedge.ID)
                        &&new SpreadWildernessSituationBuilder.Geometry(zone,new HashSet<Entity>()).Place(at.x,at.y))
                        zone.AddEntity(hedge,at.x,at.y);
                }
            }
        }
        internal static bool ValidGate(Entity e,bool placed,bool buckled=false)
        {
            var p=e?.GetPart<PhysicsPart>();var d=e?.GetPart<DoorPart>();var h=e?.GetPart<DestructiblePart>();var r=e?.GetPart<RenderPart>();
            var repair=e?.GetPart<RepairablePart>();var composition=e?.GetPart<CompositionPart>();
            bool fault=buckled
                ? repair?.ParentEntity==e&&repair.RecipeId=="timber-wicket-hinge"&&!repair.Repaired&&repair.RepairedBy==null
                    &&string.IsNullOrEmpty(repair.RepairCauseID)&&composition?.ParentEntity==e&&composition.Contains("Wood")
                    &&e.Parts.Count(part=>part is RepairablePart)==1&&e.Parts.Count(part=>part is CompositionPart)==1
                :repair==null&&composition==null;
            return fault&&e?.BlueprintName==(buckled?"GleanersBuckledWicket":"SpreadFieldGate")&&!string.IsNullOrEmpty(e.ID)&&e.Parts.All(part=>part!=null&&part.ParentEntity==e)
                &&p?.ParentEntity==e&&!p.Solid&&!p.Takeable&&p.InInventory==null&&p.Equipped==null
                &&d?.ParentEntity==e&&d.IsClosed&&string.IsNullOrEmpty(d.OwnerId)&&d.QuarterTurns>=0&&d.QuarterTurns<=3
                &&h?.ParentEntity==e&&h.HP==10&&h.MaxHP==10&&!h.Gone&&!h.Indestructible&&h.Hardness==0&&string.IsNullOrEmpty(h.WreckageBlueprint)
                &&r?.ParentEntity==e&&r.RenderString=="+"&&string.IsNullOrEmpty(r.VisualID)&&string.IsNullOrEmpty(r.VisualVariant)
                &&(placed?e.SpatialZone!=null:e.SpatialZone==null)&&!e.HasTag("Solid")&&!e.HasTag("Creature")&&!e.HasTag("Owned")
                &&!e.HasPart<LockPart>()&&!e.HasPart<InventoryPart>()&&!e.HasPart<ContainerPart>()&&!e.HasPart<SpatialFootprintPart>();
        }
        static bool Layout(Zone z,Entity obstruction,(int x,int y) at,bool horizontal,SpreadWildernessSituationBuilder.Geometry original)
        {
            if(z.GetEntityPosition(obstruction)!=at)return false;
            var g=new SpreadWildernessSituationBuilder.Geometry(z,new HashSet<Entity>{obstruction});
            if(!g.Place(at.x,at.y))return false;
            var a=(x:at.x-(horizontal?1:0),y:at.y-(horizontal?0:1));var b=(x:at.x+(horizontal?1:0),y:at.y+(horizontal?0:1));
            var blocked=new HashSet<(int x,int y)>{at};
            if(!g.Walk(a.x,a.y,blocked)||!g.Walk(b.x,b.y,blocked)||!g.BorderReach[a.x,a.y]||!g.BorderReach[b.x,b.y])return false;
            int cardinal=Distance(g,a,b,blocked,false);
            if(cardinal<8||cardinal>60||Distance(g,a,b,blocked,true)<=3)return false;
            // Use the real physical path API too: actor-aware navigation plans
            // to open a closed door and therefore cannot establish bypass cost.
            var native=FindPath.Search(z,a.x,a.y,b.x,b.y);
            if(!native.Usable||native.Steps.Count<=3||native.Steps.Count>60)return false;
            int x=a.x,y=a.y;foreach(var step in native.Steps){x+=step.dx;y+=step.dy;if(!g.Walk(x,y,blocked))return false;}
            return (x,y)==b&&g.PreservesAgainst(original,new[]{at});
        }
        static int Distance(SpreadWildernessSituationBuilder.Geometry g,(int x,int y) a,(int x,int y) b,HashSet<(int x,int y)> blocked,bool diagonal)
        {
            var seen=new bool[Zone.Width,Zone.Height];var q=new Queue<(int x,int y,int d)>();q.Enqueue((a.x,a.y,0));seen[a.x,a.y]=true;
            while(q.Count>0)
            {
                var p=q.Dequeue();if((p.x,p.y)==b)return p.d;if(p.d>=60)continue;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {if(dx==0&&dy==0||!diagonal&&dx!=0&&dy!=0)continue;int xx=p.x+dx,yy=p.y+dy;if(!g.Walk(xx,yy,blocked)||seen[xx,yy])continue;seen[xx,yy]=true;q.Enqueue((xx,yy,p.d+1));}
            }
            return -1;
        }
    }
}
