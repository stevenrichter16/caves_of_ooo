using System;
using System.Collections.Generic;
using System.Linq;
namespace CavesOfOoo.Core
{
    public static partial class SecondExplorationSites
    {
        static void Passage(PacketPlan p)
        {
            var guard = p.Make("SoddenPassageGuard", "cutbank-guard"); var post = p.Make("SoddenPassagePost", "cutbank-post");
            p.At(guard,35,2); p.At(post,36,2);
            if (guard.GetPart<SpreadTerritoryPart>() == null || guard.GetPart<LocalPassagePermitPart>() == null) throw new InvalidOperationException("missing-cutbank-duty");
            p.Configure = () =>
            {
                if (!guard.GetPart<SpreadTerritoryPart>().Configure(p.Zone,post,35,0,45,7,2)) throw new InvalidOperationException("invalid-cutbank-duty");
                guard.GetPart<BrainPart>().Stay(35,2);
            };
        }
        static void Pen(PacketPlan p)
        {
            if (p.Manager.WorldMap.GetPOI(12,10) != null || p.Manager.WorldMap.Tiles[12,10] != BiomeType.Spread) throw new InvalidOperationException("foreign-pen-destination");
            var specs = new List<(string bp,int x,int y,string role)>();
            for(int y=-2;y<=2;y++)for(int x=-2;x<=2;x++)if(Math.Abs(x)==2||Math.Abs(y)==2)
                specs.Add((x==2&&y==0?"FrontierPenGate":"Hedge",x,y,x==2&&y==0?"pen-gate":"pen-hedge"));
            specs.Add(("ReedbackGrazer",0,0,"pen-grazer")); specs.Add(("RipeCropRow",4,0,"pen-forage")); specs.Add(("RipeCropRow",4,2,"pen-reserved-row"));
            (int x,int y)? found=null;
            foreach(var at in from y in Enumerable.Range(5,15) from x in Enumerable.Range(7,66) orderby Math.Abs(x-58)+Math.Abs(y-16),y,x select(x,y))
            {
                bool clear=true;for(int y=-3;y<=3&&clear;y++)for(int x=-3;x<=5;x++)if(!p.Free(at.x+x,at.y+y)){clear=false;break;}
                if(!clear)continue;
                var cells=specs.Where(s=>s.bp!="RipeCropRow").Select(s=>(at.x+s.x,at.y+s.y)).ToArray();
                if(!p.Preserves(cells))continue; found=at;break;
            }
            if(!found.HasValue)throw new InvalidOperationException("no-safe-pen-packet");
            Entity gate=null,grazer=null,food=null,reserve=null;
            foreach(var s in specs)
            {
                var e=p.Make(s.bp,s.role);p.At(e,found.Value.x+s.x,found.Value.y+s.y);
                if(s.role=="pen-gate")gate=e;if(s.role=="pen-grazer")grazer=e;if(s.role=="pen-forage")food=e;if(s.role=="pen-reserved-row")reserve=e;
            }
            var penned=new GrazerPenPart{Gate=gate};grazer.AddPart(penned);
            p.Explain(gate,"The closed pen holds one real grazer. Opening the gate gives it a route to the outside forage row. The second reserved row remains for travellers.");
            p.Configure=()=>
            {
                if(!gate.GetPart<DoorPart>().IsClosed||!grazer.GetPart<SpreadGrazerPart>().ConfigureForage(p.Zone,food,reserve))throw new InvalidOperationException("invalid-pen-role");
                var center=p.Zone.GetEntityPosition(grazer);var gateAt=p.Zone.GetEntityPosition(gate);
                if(Reach(p.Zone,center,gate).Contains((gateAt.x+1,gateAt.y)))throw new InvalidOperationException("closed-pen-diagonal-leak");
            };
        }
        static HashSet<(int x,int y)> Reach(Zone zone,(int x,int y) start,Entity closedGate)
        {
            var seen=new HashSet<(int,int)>();var queue=new Queue<(int x,int y)>();seen.Add(start);queue.Enqueue(start);
            while(queue.Count>0)
            {
                var at=queue.Dequeue();for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
                {
                    if(x==0&&y==0)continue;var c=zone.GetCell(at.x+x,at.y+y);if(c==null||seen.Contains((c.X,c.Y))||c.BlocksMovement()||c.Objects.Contains(closedGate))continue;
                    seen.Add((c.X,c.Y));queue.Enqueue((c.X,c.Y));
                }
            }
            return seen;
        }
        static void CounterStore(PacketPlan p)
        {
            var source=p.Before.FirstOrDefault(e=>e.BlueprintName=="Bones"&&e.GetProperty("SecondExploration.Stamp")=="AbandonedCounter");
            if(source==null)throw new InvalidOperationException("missing-actual-abandoned-counter-stamp");
            var chest=p.Make("CounterStoreChest","counter-store");
            foreach(string bp in new[]{"HealingTonic","SteelBladeComponent","SalvagedTimber","SalvagedTimber"})p.StockNew(chest,bp);
            p.Near(chest,p.Zone.GetEntityPosition(source),8);var at=p.Planned(chest);
            var worker=p.Make("Villager","counter-lockworker");worker.GetPart<RenderPart>().DisplayName="travelling lockworker";
            var brain=worker.GetPart<BrainPart>();brain.Passive=true;brain.Wanders=false;brain.WandersRandomly=false;brain.Staying=true;
            worker.AddPart(new LocksmithServicePart());worker.AddPart(new CivilianAidPart());worker.AddPart(new CivilianEquipmentGiftPart());worker.AddPart(new CivilianCourtesyPart());
            p.Near(worker,at,2);var workerAt=p.Planned(worker);p.Configure=()=>brain.Stay(workerAt.x,workerAt.y);
            p.Explain(worker,"A travelling lockworker pauses beside the old store. Six drams buys an opening; the marked old key or breaking the chest are other ways to reach the same stock.");
            var screen=p.Make("FrontierClothScreen","counter-screen");p.Near(screen,at,3);
            var key=p.Make("CounterStoreKey","counter-key");p.Near(key,p.Zone.GetEntityPosition(source),8);
            p.Explain(source,"A marked old store key lies outside these abandoned walls. Nearby, a cloth screen still provides cover; stripping it yields cord but removes that cover.");
        }
    }
    /// <summary>A closed authored gate pauses this one grazer's finite approach
    /// budget. Opening or physically removing that gate resumes the existing AI.</summary>
    public sealed class GrazerPenPart:Part
    {
        public override string Name=>"GrazerPen";
        public Entity Gate;
        internal bool Closed(Zone zone)=>Gate!=null&&Gate.SpatialZone==zone&&zone.GetEntityCell(Gate)!=null
            &&Gate.GetPart<DoorPart>()?.IsClosed==true&&Gate.GetPart<DestructiblePart>()?.Gone!=true;
    }
}
