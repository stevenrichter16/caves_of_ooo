// Read-only presentation observer plus actual end-of-frame screenshot; no gameplay events.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(!EditorApplication.isPlaying || scene.path!="Assets/Scenes/Studies/SeedKeeperClearing16x24.unity")throw new InvalidOperationException("Wrong Play scene");
var sprites=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
var maps=UnityEngine.Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsSortMode.None);
int cells=0;foreach(var map in maps)foreach(var cell in map.cellBounds.allPositionsWithin)if(map.HasTile(cell))cells++;
if(cells!=56 || sprites.Length!=5)throw new InvalidOperationException("Missing clearing artwork");
ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Docs/Verification/Clearing16x24/clearing-game-final.png"));
return new{scene=scene.path,playing=EditorApplication.isPlaying,environmentCells=cells,tilemaps=maps.Length,actors=sprites.Length,
 atlasTextures=sprites.Select(x=>x.sprite.texture.name).Distinct().ToArray(),materials=sprites.Select(x=>x.sharedMaterial.name).Distinct().ToArray(),
 batches=UnityEditor.UnityStats.batches,drawCalls=UnityEditor.UnityStats.drawCalls,triangles=UnityEditor.UnityStats.triangles,
 cameraSize=Camera.main.orthographicSize,cameraTilt=Camera.main.transform.eulerAngles.ToString(),
 bootstraps=UnityEngine.Object.FindObjectsByType<CavesOfOoo.GameBootstrap>(FindObjectsSortMode.None).Length,
 screenshot="Docs/Verification/Clearing16x24/clearing-game-final.png"};
