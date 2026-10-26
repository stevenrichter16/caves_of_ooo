var r=UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.ZoneRenderer>();
var z=r.CurrentZone; var c=z.GetEntityCell(r.PlayerEntity);
var owners=new System.Collections.Generic.List<object>();
foreach(var e in z.GetReadOnlyEntities()) {
 if(e.HasTag("Player")||e.BlueprintName=="Grass"||e.BlueprintName=="Floor")continue;
 var p=z.GetEntityCell(e);var crop=e.GetPart<CavesOfOoo.Core.CropPart>();
 owners.Add(new { id=e.ID,blueprint=e.BlueprintName,role=e.GetProperty("ConnectedSpread.Role")??e.GetProperty("SpreadResident.Role")??e.GetProperty("SpreadWorksite.Role"),x=p.X,y=p.Y,visible=p.IsVisible,stage=crop==null?-1:crop.GrowthStage,moisture=crop==null?-1:crop.MoistureTicks,composted=crop!=null && crop.Composted,ticksPerStage=crop==null?-1:crop.TicksPerStage,ticksInStage=crop==null?-1:crop.TicksInStage,growthWetTickRemainder=crop==null?-1:crop.GrowthWetTickRemainder,contents=e.GetPart<CavesOfOoo.Core.ContainerPart>()?.Contents.Count, repair=e.GetPart<CavesOfOoo.Core.RepairablePart>()?.Repaired, kitchenState=e.GetPart<CavesOfOoo.Core.KitchenBatchPart>()?.State, healingSlot=e.GetPart<CavesOfOoo.Core.FieldMedicinePart>()?.FindCarriedMedicine()?.BlueprintName, hp=e.GetStatValue("Hitpoints"), reserveState=e.GetPart<CavesOfOoo.Core.LocalGatheringClaimPart>()?.GetState(r.PlayerEntity).ToString() });
}
string label="__LABEL__";
var inventory=new System.Collections.Generic.List<object>();
foreach(var item in r.PlayerEntity.GetPart<CavesOfOoo.Core.InventoryPart>().Objects)
 inventory.Add(new {id=item.ID,blueprint=item.BlueprintName,count=item.GetPart<CavesOfOoo.Core.StackerPart>()?.StackCount,displayName=item.GetDisplayName(),brewForm=item.GetPart<CavesOfOoo.Core.BrewItemPart>()?.Form,brewEffects=item.GetPart<CavesOfOoo.Core.BrewItemPart>()?.EffectsRaw,healing=item.GetPart<CavesOfOoo.Core.TonicPart>()?.Healing});
var coatings=new System.Collections.Generic.List<object>();
for(int y=0;y<CavesOfOoo.Core.Zone.Height;y++)for(int x=0;x<CavesOfOoo.Core.Zone.Width;x++) {
 var state=z.TileState.Get(x,y);if(state==null)continue;
 coatings.Add(new{x,y,visible=z.GetCell(x,y).IsVisible,explored=z.GetCell(x,y).Explored,layers=state.Coatings.Select(l=>new{id=l.Id,turns=l.Turns,projectionOwner=l.ProjectionOwnerId}).ToArray(),cold=state.Cold,heat=state.Heat,charge=state.Charge});
}
var receipt=new {label,seed=UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.InputHandler>().ZoneManager.WorldSeed,tick=CavesOfOoo.Core.WorldClock.CurrentTick,drams=CavesOfOoo.Core.TradeSystem.GetDrams(r.PlayerEntity),zone=z.ZoneID,flat=r.FlatPixelModeActive,player=new[]{c==null?-1:c.X,c==null?-1:c.Y},hp=r.PlayerEntity.GetStatValue("Hitpoints"),editorReportedScreenSize=new[]{UnityEngine.Screen.width,UnityEngine.Screen.height},owners,inventory,coatings,observational=true};
string root=System.IO.Path.GetFullPath("Docs/Verification/Live16x24");
if(System.IO.File.Exists(System.IO.Path.Combine(root,label+".json"))||System.IO.File.Exists(System.IO.Path.Combine(root,label+".png")))throw new System.InvalidOperationException("Capture exists");
System.IO.File.WriteAllText(System.IO.Path.Combine(root,label+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(receipt,Newtonsoft.Json.Formatting.Indented));
UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(root,label+".png"));
return new {label,zone=z.ZoneID,player=new[]{c==null?-1:c.X,c==null?-1:c.Y},hp=r.PlayerEntity.GetStatValue("Hitpoints")};
