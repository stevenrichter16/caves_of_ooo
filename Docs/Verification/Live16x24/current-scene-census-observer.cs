// Observational CodeDOM body. No resolver calls, Render events, resource loads,
// world generation, camera/input changes, refreshes, or scene mutations.
// __LABEL__ must be replaced by a fresh label before execution.
var r = UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.ZoneRenderer>();
var input = UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.InputHandler>();
if (r == null || input == null || !r.FlatPixelModeActive || r.Paused || r.CurrentZone == null)
    throw new System.InvalidOperationException("Requires a settled, unpaused flat gameplay scene.");
var z = r.CurrentZone;
var camera = input.CameraFollow.GetComponent<UnityEngine.Camera>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
System.Func<object,string,object> field = (o,n) => o == null ? null : o.GetType().GetField(n,flags).GetValue(o);
var env = field(r,"_envSpriteRenderer");
var main = (UnityEngine.Tilemaps.Tilemap)field(r,"_tilemap");
var bg = (UnityEngine.Tilemaps.Tilemap)field(r,"_bgTilemap");
var overlay = (UnityEngine.Tilemaps.Tilemap)field(env,"_overlayTilemap");
var hazards = (UnityEngine.Tilemaps.Tilemap)field(r,"_tileStateTilemap");
var decoration = (UnityEngine.Tilemaps.Tilemap)field(env,"_flatGroundDecorationTilemap");
var receipts = (System.Array)field(env,"_flatPaintReceipts");
var animated = field(r,"_entityVisualRenderer");
var views = (System.Collections.IDictionary)field(animated,"_views");
if (main == null || receipts == null) throw new System.InvalidOperationException("No existing flat paint receipts.");
var activeBodies = new System.Collections.Generic.Dictionary<CavesOfOoo.Core.Entity,UnityEngine.SpriteRenderer>();
if (views != null) foreach (System.Collections.DictionaryEntry entry in views)
{
    var body = field(entry.Value,"Body") as UnityEngine.SpriteRenderer;
    if ((bool)field(entry.Value,"Flat") && body != null && body.enabled && body.gameObject.activeInHierarchy && body.color.a > 0 && body.sprite != null)
        activeBodies[(CavesOfOoo.Core.Entity)entry.Key] = body;
}
System.Func<UnityEngine.Sprite,string> status = s => s == null ? "none"
    : s.name.StartsWith("Flat world glyph ",System.StringComparison.Ordinal) ? "glyph-fallback"
    : s.name == "Greybox Ground" ? "semantic-solid-ground"
    : s.name.StartsWith("Greybox ",System.StringComparison.Ordinal) ? "greybox-shape"
    : !string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(s.texture)) && s.rect.width == 16 && s.rect.height == 24 ? "authored-16x24"
    : "other-observed-sprite";
System.Func<UnityEngine.Sprite,object> spriteInfo = s => s == null ? null : (object)new {
    name=s.name, texture=s.texture == null ? null : s.texture.name,
    asset=s.texture == null ? null : UnityEditor.AssetDatabase.GetAssetPath(s.texture),
    rect=new[]{s.rect.x,s.rect.y,s.rect.width,s.rect.height}, ppu=s.pixelsPerUnit, classification=status(s)
};
System.Func<UnityEngine.Tilemaps.Tilemap,UnityEngine.Vector3Int,UnityEngine.Sprite> painted = (map,pos) => {
    if (map == null || !map.gameObject.activeInHierarchy) return null;
    var renderer = map.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
    return renderer == null || !renderer.enabled ? null : map.GetSprite(pos);
};
System.Func<CavesOfOoo.Core.Entity,object> ownerInfo = e => {
    if(e == null) return null;
    var render=e.GetPart<CavesOfOoo.Core.RenderPart>();
    var crop=e.GetPart<CavesOfOoo.Core.CropPart>();
    var repair=e.GetPart<CavesOfOoo.Core.RepairablePart>();
    var harvest=e.GetPart<CavesOfOoo.Core.HarvestablePart>();
    var pool=e.GetPart<CavesOfOoo.Core.LiquidPoolPart>();
    return new { id=e.ID, blueprint=e.BlueprintName, display=render == null ? null : render.DisplayName,
        visualID=render == null ? null : render.VisualID, variant=render == null ? null : render.VisualVariant,
        layer=render == null ? (int?)null : render.RenderLayer,
        category=e.HasTag("Player")?"player":e.HasTag("Creature")?"actor":crop!=null?"crop":e.HasTag("Corpse")?"corpse":e.HasTag("Item")?"item":e.HasTag("Wall")?"wall":e.HasTag("Terrain")?"terrain":"prop",
        sourceBlueprint=e.GetProperty("SourceBlueprint"),sourceID=e.GetProperty("SourceID"),
        crop=crop==null?null:(object)new{stage=crop.GrowthStage,moisture=crop.MoistureTicks},
        repaired=repair==null?(bool?)null:repair.Repaired, harvested=harvest==null?(bool?)null:harvest.Harvested,
        liquid=pool==null?null:(object)new{id=pool.LiquidId,volume=pool.Volume},
        hp=e.GetStat("Hitpoints")==null?(int?)null:e.GetStatValue("Hitpoints") };
};
var cells = new System.Collections.Generic.List<object>();
var counts = new System.Collections.Generic.Dictionary<string,int>();
var groundCounts = new System.Collections.Generic.Dictionary<string,int>();
int disclosedOffscreen=0, onscreenCells=0, unmatchedReceipts=0;
for(int y=0;y<CavesOfOoo.Core.Zone.Height;y++) for(int x=0;x<CavesOfOoo.Core.Zone.Width;x++)
{
    var cell=z.GetCell(x,y); if(cell==null || !cell.Explored || !cell.IsVisible) continue;
    var pos=new UnityEngine.Vector3Int(x,CavesOfOoo.Core.Zone.Height-1-y,0);
    var a=camera.WorldToViewportPoint(main.CellToWorld(pos));
    var b=camera.WorldToViewportPoint(main.CellToWorld(pos+new UnityEngine.Vector3Int(1,1,0)));
    bool onscreen=System.Math.Max(a.x,b.x)>0 && System.Math.Min(a.x,b.x)<1 && System.Math.Max(a.y,b.y)>0 && System.Math.Min(a.y,b.y)<1 && a.z>0;
    if(!onscreen){disclosedOffscreen++;continue;} onscreenCells++;
    object receipt=receipts.GetValue(y*CavesOfOoo.Core.Zone.Width+x);
    var selected=field(receipt,"Owner") as CavesOfOoo.Core.Entity;
    bool current=object.ReferenceEquals(field(receipt,"Zone"),z) && object.ReferenceEquals(field(receipt,"Cell"),cell)
        && (selected==null || cell.Occupants.Contains(selected)
            && object.ReferenceEquals(field(receipt,"Render"),selected.GetPart<CavesOfOoo.Core.RenderPart>()));
    if(!current){selected=null;unmatchedReceipts++;}
    var foreground=painted(overlay,pos); string channel="environment-overlay";
    if(foreground==null){foreground=painted(main,pos);channel="main-tilemap";}
    UnityEngine.SpriteRenderer body;
    if(selected!=null && activeBodies.TryGetValue(selected,out body)){foreground=body.sprite;channel="animated-body";}
    var ground=painted(bg,pos); var mark=painted(hazards,pos); var detail=painted(decoration,pos);
    string resolution=!current?"unreceipted":foreground!=null?status(foreground):ground!=null?"ground-only":"no-painted-sprite";
    string key=(selected==null?"<no selected owner>":selected.BlueprintName)+" | "+resolution;
    counts[key]=counts.ContainsKey(key)?counts[key]+1:1;
    string groundKey=ground==null?"none":ground.name+" | "+status(ground);
    groundCounts[groundKey]=groundCounts.ContainsKey(groundKey)?groundCounts[groundKey]+1:1;
    var covered=new System.Collections.Generic.List<object>();
    foreach(var e in cell.Occupants)
    {
        var rp=e.GetPart<CavesOfOoo.Core.RenderPart>();
        var ph=e.GetPart<CavesOfOoo.Core.PhysicsPart>();
        if(e==selected || rp==null || !rp.Visible || rp.ParentEntity!=e
            || ph!=null && (ph.ParentEntity!=e || ph.InInventory!=null || ph.Equipped!=null))continue;
        covered.Add(new{id=e.ID,blueprint=e.BlueprintName,layer=rp.RenderLayer,visualID=rp.VisualID});
    }
    var state=z.TileState.Get(x,y);
    // Ordinary empty/grass ground is counted, without hundreds of repetitive rows.
    bool simple=current && (selected==null || selected.BlueprintName=="Grass" || selected.BlueprintName=="Floor")
        && resolution=="ground-only" && covered.Count==0 && mark==null && detail==null && state==null;
    if(simple)continue;
    cells.Add(new{x,y,receiptCurrent=current,owner=ownerInfo(selected),selectedVisualID=selected==null?null:selected.GetPart<CavesOfOoo.Core.RenderPart>().VisualID,
        resolution,channel,foreground=spriteInfo(foreground),ground=spriteInfo(ground),stateMark=spriteInfo(mark),decoration=spriteInfo(detail),
        coveredOwnersNotArtAssessed=covered,
        tileState=state==null?null:(object)new{coatings=state.Coatings.Select(l=>new{id=l.Id,turns=l.Turns,source=l.ProjectionOwnerId}).ToArray(),heat=state.Heat,cold=state.Cold,charge=state.Charge,cloud=state.Cloud} });
}
string label="__LABEL__";
string output=System.IO.Path.GetFullPath("Docs/Verification/Live16x24/"+label+".json");
if(System.IO.File.Exists(output))throw new System.InvalidOperationException("Census exists; choose a fresh label.");
var playerCell=z.GetEntityCell(r.PlayerEntity);
var report=new{label,observational=true,source="Existing paint receipts, actual current tilemap sprites and active animated bodies; no resolver calls",
    zone=z.ZoneID,seed=input.ZoneManager.WorldSeed,tick=CavesOfOoo.Core.WorldClock.CurrentTick,
    player=new[]{playerCell.X,playerCell.Y},screen=new[]{UnityEngine.Screen.width,UnityEngine.Screen.height},nativeScale=camera.pixelRect.height/(2f*camera.orthographicSize*16f),
    onscreenCells,disclosedOffscreenCellsOmitted=disclosedOffscreen,unmatchedReceipts,
    selectedCellCounts=counts.OrderBy(p=>p.Key).ToDictionary(p=>p.Key,p=>p.Value),groundCellCounts=groundCounts.OrderBy(p=>p.Key).ToDictionary(p=>p.Key,p=>p.Value),cells,
    limits="Current on-screen visible cells only, not six-zone completion or all-owner art coverage. Counts are cells, not unique entities. Covered owners are not classified. Existing pixel snapshots can be stale during movement; use after settling. Actual sprite asset/rect takes precedence over authored VisualID. Ground, hazard marks and foreground are separate. Classification is sprite provenance, not quality acceptance. Does not assess inventory/readers or other states."};
System.IO.File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return new{output,zone=z.ZoneID,onscreenCells,unmatchedReceipts,rows=cells.Count,selectedCellCounts=report.selectedCellCounts};
