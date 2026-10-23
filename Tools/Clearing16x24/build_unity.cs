// Run through Unity MCP execute_code in Edit mode after build_kit.py.
// Rebuilds only the dedicated clearing assets/scene; preserves all approved art.
if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before authoring the clearing.");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
 if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve all unsaved scenes before rebuilding.");
string kit = "Assets/Art/Clearing16x24";
string scenePath = "Assets/Scenes/Studies/SeedKeeperClearing16x24.unity";
string[] ids = {"clay-oven","stone-well","striped-seed-shelter","sorting-table","drying-rack","woven-basket","seed-sack","broken-wall","fence-horizontal","fence-vertical","tilled-bed","emberwheat","marlroot","grass-tuft","stool","firewood"};
string[] actors={"Assets/Art/SeedKeeper3/seed-keeper-3.png","Assets/Art/Cook16x24/cook-16x24.png","Assets/Art/Tatterjay16x24/tatterjay.png","Assets/Art/ReedbackGrazer16x24/03-ridge.png","Assets/Art/BeetleJar16x24/beetle-jar.png"};
foreach(string path in actors) if(AssetDatabase.LoadAssetAtPath<Sprite>(path)==null) throw new InvalidOperationException("Missing approved sprite: "+path);
var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SeedKeeper3/SeedKeeper16x24.mat");
if(material==null)throw new InvalidOperationException("Approved shared unlit material is missing.");
foreach (string id in ids) if (!System.IO.File.Exists("ArtSource/Clearing16x24/kit/"+id+".png")) throw new InvalidOperationException("Missing source: "+id);
System.IO.Directory.CreateDirectory(kit);
foreach (string id in ids) System.IO.File.Copy("ArtSource/Clearing16x24/kit/"+id+".png",kit+"/"+id+".png",true);
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
foreach (string id in ids)
{
 string path=kit+"/"+id+".png";
 var imp=(TextureImporter)AssetImporter.GetAtPath(path);
 imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;
 imp.spritePixelsPerUnit=16;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;
 imp.textureCompression=TextureImporterCompression.Uncompressed;imp.wrapMode=TextureWrapMode.Clamp;
 imp.npotScale=TextureImporterNPOTScale.None;imp.alphaIsTransparency=true;imp.sRGBTexture=true;imp.isReadable=false;
 foreach(string platform in new[]{"Standalone","Android","iPhone","WebGL"})imp.ClearPlatformTextureSettings(platform);
 var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);
 settings.spriteAlignment=9;settings.spritePivot=new Vector2(.5f,0);settings.spriteMeshType=SpriteMeshType.FullRect;
 settings.spriteExtrude=0;settings.spriteGenerateFallbackPhysicsShape=false;imp.SetTextureSettings(settings);imp.SaveAndReimport();
}
foreach(string id in ids)
{
 var go=new GameObject(id);var r=go.AddComponent<SpriteRenderer>();
 r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(kit+"/"+id+".png");r.sharedMaterial=material;r.color=Color.white;
 PrefabUtility.SaveAsPrefabAsset(go,kit+"/"+id+".prefab");UnityEngine.Object.DestroyImmediate(go);
}
var tiles=new Dictionary<string,UnityEngine.Tilemaps.Tile>();
foreach(string id in ids){
 string path=kit+"/"+id+".asset";var tile=AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(path);
 if(tile==null){tile=ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();AssetDatabase.CreateAsset(tile,path);}
 tile.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(kit+"/"+id+".png");tile.color=Color.white;tile.colliderType=UnityEngine.Tilemaps.Tile.ColliderType.None;
 EditorUtility.SetDirty(tile);AssetDatabase.SaveAssetIfDirty(tile);tiles.Add(id,tile);
}
var atlas=AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(kit+"/Clearing.spriteatlas");
if(atlas==null){atlas=new UnityEngine.U2D.SpriteAtlas();AssetDatabase.CreateAsset(atlas,kit+"/Clearing.spriteatlas");}
UnityEditor.U2D.SpriteAtlasExtensions.Remove(atlas,UnityEditor.U2D.SpriteAtlasExtensions.GetPackables(atlas));
UnityEditor.U2D.SpriteAtlasExtensions.Add(atlas,ids.Select(id=>AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(kit+"/"+id+".png")).Concat(actors.Select(p=>AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p))).ToArray());
var pack=UnityEditor.U2D.SpriteAtlasExtensions.GetPackingSettings(atlas);pack.enableRotation=false;pack.enableTightPacking=false;pack.padding=2;UnityEditor.U2D.SpriteAtlasExtensions.SetPackingSettings(atlas,pack);
var tex=UnityEditor.U2D.SpriteAtlasExtensions.GetTextureSettings(atlas);tex.filterMode=FilterMode.Point;tex.generateMipMaps=false;tex.readable=false;UnityEditor.U2D.SpriteAtlasExtensions.SetTextureSettings(atlas,tex);
var platformSettings=UnityEditor.U2D.SpriteAtlasExtensions.GetPlatformSettings(atlas,"DefaultTexturePlatform");platformSettings.maxTextureSize=512;platformSettings.textureCompression=TextureImporterCompression.Uncompressed;platformSettings.format=TextureImporterFormat.RGBA32;UnityEditor.U2D.SpriteAtlasExtensions.SetPlatformSettings(atlas,platformSettings);
foreach(string platform in new[]{"Standalone","Android","iPhone","WebGL"}) {var p=UnityEditor.U2D.SpriteAtlasExtensions.GetPlatformSettings(atlas,platform);p.overridden=false;UnityEditor.U2D.SpriteAtlasExtensions.SetPlatformSettings(atlas,p);}
UnityEditor.U2D.SpriteAtlasExtensions.SetIncludeInBuild(atlas,true);EditorUtility.SetDirty(atlas);AssetDatabase.SaveAssetIfDirty(atlas);
UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(new[]{atlas},EditorUserBuildSettings.activeBuildTarget);
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
// NewScene may unload assets held only by this authoring stack. Resolve tile owners
// after the transition; SetTile(null) otherwise silently produces empty saved maps.
foreach(string id in ids)tiles[id]=AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(kit+"/"+id+".asset");
material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SeedKeeper3/SeedKeeper16x24.mat");
var cameraGo=new GameObject("Main Camera");cameraGo.tag="MainCamera";cameraGo.transform.position=new Vector3(0,0,-10);
var camera=cameraGo.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6.25f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(3,47,35,255);camera.allowHDR=false;camera.allowMSAA=false;
var data=cameraGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=false;data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
var pixel=cameraGo.AddComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();pixel.assetsPPU=16;pixel.refResolutionX=320;pixel.refResolutionY=200;pixel.gridSnapping=UnityEngine.Rendering.Universal.PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;pixel.cropFrame=UnityEngine.Rendering.Universal.PixelPerfectCamera.CropFrame.None;
var serializedPixel=new SerializedObject(pixel);serializedPixel.FindProperty("m_FilterMode").intValue=1;serializedPixel.ApplyModifiedPropertiesWithoutUndo();
var light=new GameObject("Main Light (unlit artwork)").AddComponent<Light>();light.type=LightType.Directional;light.intensity=0;
var grid=new GameObject("16x24 Clearing Grid").AddComponent<Grid>();grid.cellSize=new Vector3(1,1.5f,0);grid.transform.position=new Vector3(-10,-6,0);
var placed=new List<object>();
var tilemaps=new Dictionary<int,UnityEngine.Tilemaps.Tilemap>();
Action<string,float,float,int> place=(id,x,y,layer)=>{
 var asset=ids.Contains(id)?kit+"/"+id+".png":actors[Array.IndexOf(new[]{"seed-keeper","cook","tatterjay","grazer","beetle-jar"},id)];
 if(ids.Contains(id)){
  UnityEngine.Tilemaps.Tilemap map;
  if(!tilemaps.TryGetValue(layer,out map)){
   var layerGo=new GameObject("Environment layer "+layer);layerGo.transform.SetParent(grid.transform,false);
   map=layerGo.AddComponent<UnityEngine.Tilemaps.Tilemap>();map.tileAnchor=new Vector3(.5f,0,0);
   var render=layerGo.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();render.sharedMaterial=material;render.sortingOrder=layer;render.mode=UnityEngine.Tilemaps.TilemapRenderer.Mode.Chunk;
   tilemaps.Add(layer,map);
  }
  if(tiles[id]==null)throw new InvalidOperationException("Tile asset was unloaded: "+id);
  var cell=new Vector3Int((int)x,(int)y,0);map.SetTile(cell,tiles[id]);EditorUtility.SetDirty(map);
  if(!map.HasTile(cell))throw new InvalidOperationException("Tile placement failed: "+id);
  placed.Add(new{id,x,y,asset,layer});return;
 }
 var go=new GameObject(id+" ["+x+","+y+"]");go.transform.SetParent(grid.transform,false);go.transform.localPosition=new Vector3(x+.5f,y*1.5f,0);
 var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(asset);sr.sharedMaterial=material;sr.color=Color.white;sr.sortingOrder=layer;sr.spriteSortPoint=SpriteSortPoint.Pivot;
 if(sr.sprite==null)throw new InvalidOperationException("Missing sprite: "+asset);
 placed.Add(new{id,x,y,asset,layer});
};
// Workplaces frame open center and south approaches. All placements preserve native scale.
place("striped-seed-shelter",4,5,0);place("sorting-table",4,5,2);place("seed-keeper",4,4,10);
place("seed-sack",2,5,2);place("seed-sack",6,5,2);place("woven-basket",6,4,10);
place("clay-oven",15,5,0);place("drying-rack",17,5,0);place("sorting-table",16,4,10);place("cook",14,4,10);
place("stool",13,5,2);place("firewood",18,5,2);place("beetle-jar",9,4,10);place("stone-well",10,4,10);
for(int y=1;y<=2;y++)for(int x=2;x<=5;x++){place("tilled-bed",x,y,-20);place(x<4?"emberwheat":"marlroot",x,y,20-y);}
place("woven-basket",6,1,20);place("stool",7,2,18);
for(int x=14;x<=17;x++)place("fence-horizontal",x,1,30);
place("fence-vertical",18,2,20);place("fence-vertical",18,3,10);place("grazer",16,2,15);place("tatterjay",12,3,15);
foreach(var at in new[]{new Vector2(1,6),new Vector2(18,6),new Vector2(1,3),new Vector2(8,1),new Vector2(19,1)})place("broken-wall",at.x,at.y,8);
foreach(var at in new[]{new Vector2(2,6),new Vector2(8,6),new Vector2(11,6),new Vector2(13,6),new Vector2(18,4),new Vector2(1,1),new Vector2(7,1),new Vector2(10,1),new Vector2(13,1),new Vector2(15,3),new Vector2(17,3),new Vector2(19,3),new Vector2(7,4),new Vector2(3,3),new Vector2(6,3),new Vector2(1,5)})place("grass-tuft",at.x,at.y,-10);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath);
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
return new{scene=scenePath,placementsCount=placed.Count,environmentTilemaps=tilemaps.Count,grid=new[]{1f,1.5f},referenceResolution=new[]{320,200},shader=material.shader.name,atlas=AssetDatabase.GetAssetPath(atlas),placements=placed};
