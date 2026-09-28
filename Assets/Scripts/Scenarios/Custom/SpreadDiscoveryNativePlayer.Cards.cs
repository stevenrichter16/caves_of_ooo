using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    // Separate visual/source cards. All setup transfers are explicit, all source
    // owners and stock come from the actual ordinary manager pipeline.
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _cards;
        static readonly string[] CardChecks={"ordinary_start","cargo_actual_source","cargo_native_layout","cargo_native_cache_reader","shelter_actual_source","shelter_native_layout","shelter_native_cache_reader","shelter_native_bypass","cards_finish"};
        const string CardsIntent="Ordinary seed64 starter; disclosed original-player transfers to actual cold-generated cargo/shelter view lanes. Fresh commit receipt binds exact existing owner IDs/positions; current approved body/gear, native Look/Examine and screenshots. Optional real pair clue is separately observed.";
        const string CardsLimits="Staged layout/readability cards, not an ordinary journey, loot acquisition, combat or permanent safe-route guarantee. One native shelter crossing begins at a disclosed border transfer. No source/stock/AI/gear changes. Current guard visibility is measured; bounds are submitted/source geometry, not pixel occlusion. Independent image review required. Optional pair clue may be absent.";
        IEnumerator RunCards()
        {
            yield return SituationCard("cargo",new[]{"Overworld.6.9.0"});
            yield return SituationCard("shelter",new[]{"Overworld.11.2.0","Overworld.10.7.0","Overworld.8.9.0"});
            yield return OptionalPairClueCard();
            Check("cards_finish",State=="Normal"&&Player.GetStatValue("Hitpoints")==40&&!DevMode.Enabled&&!DebugInvincibility.IsEnabled(Player));yield return Capture("cards-99-finished");
        }
        IEnumerator SituationCard(string kind,string[] candidates)
        {
            Zone source=null;Entity[] owners=null;Entity cache=null;Cell view=null;Diag.Entry commit=default;
            foreach(string id in candidates)
            {
                if(Manager.CachedZones.ContainsKey(id)||SpreadWildernessSituationPlan.Select(Manager,id,Manager.Wayhouse?.ZoneID)!=kind)continue;
                string marker=Mark("fresh-card-source-"+kind);var z=Manager.GetZone(id);var rows=Window(marker);
                var record=rows.LastOrDefault(r=>r.Category=="worldgen"&&r.Kind=="SpreadWildernessSituationCommitted"&&JObject.Parse(r.PayloadJson)["zone"]?.Value<string>()==id&&JObject.Parse(r.PayloadJson)["kind"]?.Value<string>()==kind);
                if(string.IsNullOrEmpty(record.TraceId)){_notes.Add("CARD SOURCE REFUSED "+id+" "+kind);continue;}
                var payload=JObject.Parse(record.PayloadJson);var ids=payload["owners"]?.Values<string>().ToArray();Require(ids!=null&&(kind=="cargo"?ids.Length==1:ids.Length>=2&&ids.Length<=3)&&ids.Distinct().Count()==ids.Length,"bounded exact commit owner IDs");
                var actual=ids.Select(ownerId=>z.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==ownerId)).ToArray();Require(actual.All(e=>e!=null&&e.SpatialZone==z)&&actual[0].ID==record.TargetId,"fresh commit binds final returned owners/cache");
                var after=(JArray)payload["after"];Require(after!=null&&after.Count==actual.Length,"exact after-placement records");
                for(int i=0;i<actual.Length;i++){var at=z.GetEntityPosition(actual[i]);Require(at.x==after[i]["Item1"].Value<int>()&&at.y==after[i]["Item2"].Value<int>(),"actual current committed anchor");}
                Require((actual[0].BlueprintName=="Crate"||actual[0].BlueprintName=="Sack")&&actual[0].GetPart<ContainerPart>()!=null&&actual.Skip(1).All(e=>e.BlueprintName=="MarlbackScrabbler"&&e.GetStatValue("Hitpoints")==15),"actual existing stock and guard family");
                var chosen=CardViewCell(z,actual,kind);if(chosen==null){_notes.Add("CARD VIEW REFUSED "+id+" "+kind);continue;}
                source=z;owners=actual;cache=actual[0];view=chosen;commit=record;break;
            }
            Require(source!=null&&cache!=null&&view!=null,"bounded actual "+kind+" source and legal view lane");string preserved=CardSourceSignature(source,owners);int tick=Tick,energy=Energy;string gear=Gear(Player);
            _observations.Add(new{phase=kind+"_fresh_generation",receipt=commit,owners=owners.Select(e=>new{id=e.ID,blueprint=e.BlueprintName,position=source.GetEntityPosition(e),hp=e.GetStatValue("Hitpoints"),gear=e.GetPart<InventoryPart>()==null?null:Gear(e),contents=e.GetPart<ContainerPart>()?.Contents.Select(c=>c.ID+":"+c.BlueprintName+":"+Units(c)).ToArray()}).ToArray()});
            yield return Transfer(source,view,"actual "+kind+" layout card view lane");Check(kind+"_actual_source",CardSourceSignature(source,owners)==preserved&&Tick==tick&&Energy==energy&&Gear(Player)==gear);
            yield return LookCardOwner(cache);var visual=owners.Select(CardVisual).ToArray();_observations.Add(new{phase=kind+"_live_model_visibility",visual});
            Check(kind+"_native_layout",owners.All(e=>Zone.GetEntityCell(e)?.IsVisible==true)&&CardSourceSignature(source,owners)==preserved&&Tick==tick&&Energy==energy);yield return Capture("cards-"+kind+"-layout");
            yield return OpenLookCardOwner(cache);yield return MenuAction("Examine");yield return ReadPages("cards-"+kind+"-cache-reader");yield return CloseNormal();
            Check(kind+"_native_cache_reader",_readerText.Contains(cache.GetDisplayName())&&CardSourceSignature(source,owners)==preserved&&Tick==tick&&Energy==energy&&Gear(Player)==gear);
            yield return Capture("cards-"+kind+"-returned-layout");yield return IdleTiming(kind+"-card-idle");if(kind=="shelter")yield return ShelterBypass(source,owners);
        }
        // Static planning admits only the real current native footprint. Once walking,
        // recompute after every paid action; no cached path moves through a changed guard.
        bool CardPassable(Zone zone,Cell cell,Entity[] guards)=>CardPassable(zone,cell,guards,Threats(zone));
        bool CardPassable(Zone zone,Cell cell,Entity[] guards,Entity[] threats)
            =>cell!=null&&Safe(zone,cell,ThreatClearance,threats)&&zone.CanPlaceFootprint(Player,cell.X,cell.Y)
                &&guards.All(e=>e.SpatialZone==zone&&e.GetStatValue("Hitpoints")>0&&e.GetPart<BrainPart>()!=null
                    &&SpatialQuery.DistanceToCell(zone,e,cell.X,cell.Y)>e.GetPart<BrainPart>().SightRadius);
        List<Cell> CardPath(Zone zone,IEnumerable<Cell> starts,Func<Cell,bool> goal,Entity[] guards)
        {
            var queue=new Queue<Cell>();var seen=new HashSet<Cell>();var parent=new Dictionary<Cell,Cell>();var threats=Threats(zone);
            foreach(var c in starts)if(c!=null&&seen.Add(c)&&CardPassable(zone,c,guards,threats))queue.Enqueue(c);
            while(queue.Count>0)
            {
                var at=queue.Dequeue();if(goal(at)){var result=new List<Cell>{at};while(parent.TryGetValue(at,out var prior)){at=prior;result.Add(at);}result.Reverse();return result;}
                foreach(var d in Steps.Take(4)){var c=zone.GetCell(at.X+d.x,at.Y+d.y);if(c==null||!seen.Add(c)||!CardPassable(zone,c,guards,threats))continue;parent[c]=at;queue.Enqueue(c);}
            }
            return null;
        }
        string CardInventorySignature(Zone zone,Entity[] owners)=>string.Join("|",owners.OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":registered="+zone.GetReadOnlyEntities().Count(x=>x.ID==e.ID&&ReferenceEquals(x,e)&&e.SpatialZone==zone)+":"+(e.GetPart<InventoryPart>()==null?"":Gear(e))+":"+string.Join(",",e.GetPart<ContainerPart>()?.Contents.Select(c=>c.ID+":"+c.BlueprintName+":"+Units(c)+":"+c.GetPart<PhysicsPart>()?.InInventory?.ID)??Enumerable.Empty<string>())));
        IEnumerator ShelterBypass(Zone source,Entity[] owners)
        {
            var guards=owners.Skip(1).ToArray();var cache=source.GetEntityCell(owners[0]);bool vertical=true;
            var route=CardPath(source,Enumerable.Range(0,Zone.Width).OrderBy(x=>Math.Abs(x-cache.X)).Select(x=>source.GetCell(x,0)),c=>c.Y==Zone.Height-1,guards);
            if(route==null){vertical=false;route=CardPath(source,Enumerable.Range(0,Zone.Height).OrderBy(y=>Math.Abs(y-cache.Y)).Select(y=>source.GetCell(0,y)),c=>c.X==Zone.Width-1,guards);}
            Require(route!=null&&route.Count>1,"actual opposite-border shelter bypass");
            string stock=CardInventorySignature(source,owners),gear=Gear(Player);int hp=Player.GetStatValue("Hitpoints"),tick=Tick,actions=_localInputs;var cacheAt=source.GetEntityPosition(owners[0]);
            yield return Transfer(source,route[0],"shelter bypass start border only");yield return Capture("cards-shelter-bypass-start");
            var walk=new List<object>();string marker=Mark("native-shelter-bypass");BeginTiming("shelter-native-bypass");
            try
            {
                for(int n=0;!(vertical?At.Y==Zone.Height-1:At.X==Zone.Width-1);n++)
                {
                    Require(n<160&&CardPassable(source,At,guards),"bounded live outside-sight bypass");var path=CardPath(source,new[]{At},c=>vertical?c.Y==Zone.Height-1:c.X==Zone.Width-1,guards);Require(path!=null&&path.Count>1,"fresh actual remaining shelter bypass");
                    var next=path[1];Require(CardPassable(source,next,guards),"current bypass step admission");yield return StepTo(next.X,next.Y);
                    walk.Add(new{x=At.X,y=At.Y,tick=Tick,guards=guards.Select(e=>new{id=e.ID,position=source.GetEntityPosition(e),hp=e.GetStatValue("Hitpoints"),target=e.GetPart<BrainPart>().Target?.ID}).ToArray()});
                    Require(Zone==source&&Player.GetStatValue("Hitpoints")==hp,"actual bypass remains local and unharmed");
                    if(n==8)yield return Capture("cards-shelter-bypass-moving");
                }
            }
            finally{EndTiming();}
            var rows=Window(marker);bool noContact=!rows.Any(e=>e.Category=="damage"&&(e.ActorId==Player.ID||e.TargetId==Player.ID));
            Check("shelter_native_bypass",_localInputs>actions&&Tick>tick&&noContact&&CardInventorySignature(source,owners)==stock&&Gear(Player)==gear&&Player.GetStatValue("Hitpoints")==hp&&source.GetEntityPosition(owners[0])==cacheAt&&guards.All(e=>e.GetStatValue("Hitpoints")==15&&e.GetPart<BrainPart>().Target!=Player));
            _observations.Add(new{phase="actual_shelter_bypass",vertical,paidInputs=_localInputs-actions,walk,marker,rows,bound="Only start-border placement is a disclosed transfer. Every crossing step is real paid native input. Exact selected source owner/count/stock/gear identities retained; unrelated ambient owners are not frozen; guard positions may change through ordinary AI. One crossing, not a permanent safety claim."});yield return Capture("cards-shelter-bypass-complete");
        }
        Cell CardViewCell(Zone zone,Entity[] owners,string kind)
        {
            var cache=zone.GetEntityCell(owners[0]);var plan=SpreadCompositionPlan.Create(zone.ZoneID,Manager.WorldSeed);var candidates=new List<Cell>();var threats=Threats(zone);
            for(int y=1;y<Zone.Height-1;y++)for(int x=1;x<Zone.Width-1;x++)
            {
                var c=zone.GetCell(x,y);int distance=Math.Max(Math.Abs(x-cache.X),Math.Abs(y-cache.Y));if(distance<2||distance>(kind=="cargo"?4:14)||!Safe(zone,c,ThreatClearance,threats))continue;
                if(kind=="cargo"&&!c.Occupants.Any(e=>e.BlueprintName=="RoadStone"))continue;
                if(owners.Skip(1).Any(e=>SpatialQuery.DistanceToCell(zone,e,x,y)<=e.GetPart<BrainPart>().SightRadius))continue;
                if(!owners.All(e=>{var at=zone.GetEntityCell(e);return AIHelpers.HasLineOfSight(zone,x,y,at.X,at.Y);}))continue;candidates.Add(c);
            }
            return candidates.OrderBy(c=>plan.IsApproach(c.X,c.Y)?0:1).ThenBy(c=>Math.Max(Math.Abs(c.X-cache.X),Math.Abs(c.Y-cache.Y))).ThenBy(c=>c.Y*Zone.Width+c.X).FirstOrDefault();
        }
        string CardSourceSignature(Zone zone,IEnumerable<Entity> owners)=>SourceGraph(zone)+";cards="+string.Join("|",owners.OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+zone.GetEntityPosition(e)+":"+Stats(e)+":"+(e.GetPart<InventoryPart>()==null?"":Gear(e))+":"+string.Join(",",e.GetPart<ContainerPart>()?.Contents.Select(c=>c.ID+":"+c.BlueprintName+":"+Units(c)+":"+c.GetPart<PhysicsPart>()?.InInventory?.ID)??Enumerable.Empty<string>())));
        IEnumerator LookCardOwner(Entity owner)
        {
            Require(State=="Normal"&&Zone.GetEntityCell(owner)?.IsVisible==true,"actual visible card owner before native Look");yield return Tap(Key.L);Require(State=="LookMode","native card Look");var cursor=(WorldCursorState)Field(_input,"_worldCursorState");var at=Zone.GetEntityPosition(owner);
            for(int n=0;cursor.X!=at.x||cursor.Y!=at.y;n++){Require(n<32,"bounded actual card look cursor");yield return Tap(cursor.X<at.x?Key.D:cursor.X>at.x?Key.A:cursor.Y<at.y?Key.S:Key.W);}yield return null;
        }
        IEnumerator OpenLookCardOwner(Entity owner)
        {
            Require(State=="LookMode","existing card Look focus");yield return Tap(Key.Enter);Require(State=="WorldActionMenuOpen","native card menu");string choose=WorldInteractionSystem.PickTargetCommandPrefix+owner.ID;
            var actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");if((_input.WorldActionMenuUI.SelectedCellIsPile||!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,owner))&&!actions.Any(a=>a.Command==choose)&&actions.Any(a=>a.Command==WorldInteractionSystem.PickCellCommand))yield return MenuAction(WorldInteractionSystem.PickCellCommand);
            actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");if(actions.Any(a=>a.Command==choose))yield return MenuAction(choose);Require(ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,owner)&&!_input.WorldActionMenuUI.SelectedCellIsPile,"exact current selected card owner");
        }
        object CardVisual(Entity owner)
        {
            var presenter=_input.ZoneRenderer.SpawnRing3D;SpreadBiomeStyleEvidence proof=default;GameObject root=null;string model=null;Require(presenter!=null&&presenter.TryGetApprovedStyle(owner,out proof)&&presenter.TryGetEntityView(owner,out root,out model)&&presenter.IsRenderedEntity(owner),"actual approved submitted card owner model");
            Bounds bounds;
            if(proof.Batched)
            {
                var recipe=SpawnRing3DRecipes.Resolve(Zone,owner,(SpawnRing3DCatalog)Field(presenter,"definition"));var prefab=(GameObject)typeof(SpawnRing3DPresenter).GetMethod("PrefabFor",Private).Invoke(presenter,new object[]{recipe});
                Require(prefab!=null&&prefab.GetComponent<MeshFilter>()?.sharedMesh==proof.ExpectedMesh,"exact adopted root-mesh static source for card bounds");var b=proof.ExpectedMesh.bounds;var matrix=Matrix4x4.TRS(recipe.Position,Quaternion.Euler(0,recipe.QuarterTurns*90,0),Vector3.one)*Matrix4x4.TRS(Vector3.zero,prefab.transform.localRotation,prefab.transform.localScale);bounds=new Bounds(matrix.MultiplyPoint3x4(b.center),Vector3.zero);
                foreach(float x in new[]{b.min.x,b.max.x})foreach(float y in new[]{b.min.y,b.max.y})foreach(float z in new[]{b.min.z,b.max.z})bounds.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(x,y,z)));
            }
            else{var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy).ToArray();Require(renderers.Length>0,"current submitted independent card body");bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);}
            var camera=presenter.WorldCamera;Require(camera!=null,"actual native card world camera");float minX=1,minY=1,maxX=0,maxY=0,depth=float.PositiveInfinity;foreach(float x in new[]{bounds.min.x,bounds.max.x})foreach(float y in new[]{bounds.min.y,bounds.max.y})foreach(float z in new[]{bounds.min.z,bounds.max.z}){var v=camera.WorldToViewportPoint(new Vector3(x,y,z));minX=Math.Min(minX,v.x);minY=Math.Min(minY,v.y);maxX=Math.Max(maxX,v.x);maxY=Math.Max(maxY,v.y);depth=Math.Min(depth,v.z);}
            Require(depth>0&&minX>=0&&minY>=0&&maxX<=1&&maxY<=1&&maxX>minX&&maxY>minY,"card owner geometry inside actual viewport");
            var inv=owner.GetPart<InventoryPart>();if(inv!=null)foreach(var item in inv.EquippedItems.Values.Distinct())Require(presenter.TryGetApprovedEquipmentStyle(owner,item,out _),"actual card guard equipment style");
            return new{id=owner.ID,blueprint=owner.BlueprintName,model,visible=Zone.GetEntityCell(owner).IsVisible,batched=proof.Batched,expectedMesh=proof.ExpectedMesh.name,submittedMesh=proof.SubmittedMesh.name,pieces=proof.PieceCount,minX,minY,maxX,maxY,depth,guardTarget=owner.GetPart<BrainPart>()?.Target?.ID,bound="Exact current style/fragment proof and geometry bounds; no pixel occlusion guarantee."};
        }
        IEnumerator OptionalPairClueCard()
        {
            string id=Manager.RareEncounters?.PairZoneID;if(string.IsNullOrEmpty(id)||Manager.CachedZones.ContainsKey(id)){_notes.Add("OPTIONAL PAIR CLUE NOT VERIFIED: no fresh selected graph");yield break;}
            var source=Manager.GetZone(id);var sign=source.GetReadOnlyEntities().SingleOrDefault(e=>e.BlueprintName=="Signpost"&&e.GetProperty(SpreadRareEncounterBuilder.PairClueKey)==id);if(sign==null){_notes.Add("OPTIONAL PAIR CLUE NOT VERIFIED: optional source absent");yield break;}
            var pair=source.GetReadOnlyEntities().Where(e=>e.GetProperty(SpreadRareEncounterBuilder.SourceKey)==id&&e.HasTag("Creature")).ToArray();Require(pair.Length==2&&pair.All(e=>e.GetPart<BrainPart>()!=null),"actual pair guards for optional sign");var at=source.GetEntityCell(sign);Cell view=null;
            foreach(var d in Steps){var c=source.GetCell(at.X+d.x,at.Y+d.y);if(c!=null&&Safe(source,c,ThreatClearance)&&pair.All(e=>SpatialQuery.DistanceToCell(source,e,c.X,c.Y)>e.GetPart<BrainPart>().SightRadius)){view=c;break;}}
            if(view==null){_notes.Add("OPTIONAL PAIR CLUE NOT VERIFIED: no current outside-sight reading cell");yield break;}string graph=CardSourceSignature(source,new[]{sign}.Concat(pair));int tick=Tick,energy=Energy;
            yield return Transfer(source,view,"optional actual physical pair clue");yield return LookCardOwner(sign);var visual=CardVisual(sign);yield return Capture("cards-pair-clue-approach");yield return OpenLookCardOwner(sign);yield return MenuAction("Examine");yield return ReadPages("cards-pair-clue-reader");yield return CloseNormal();
            bool verified=graph==CardSourceSignature(source,new[]{sign}.Concat(pair))&&Tick==tick&&Energy==energy&&_readerText.Contains("ditch-cutters");_observations.Add(new{phase="optional_pair_clue",verified,sign=sign.ID,source=id,visual,pair=pair.Select(e=>e.ID).ToArray(),bound="Actual optional historical sign approached by disclosed original-player transfer; no fight/ordinary journey claim."});Require(verified,"optional current pair clue read is free and source preserving");
        }
    }
}
