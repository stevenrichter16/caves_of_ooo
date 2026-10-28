// Execute only after editable Overwrit environment source verification, with Play stopped.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play first");
var target="Assets/Resources/Sprites/Live16x24/overwrit-materials.png";
var source="ArtSource/Live16x24/overwrit-environment/overwrit-materials.png";
var meta=System.IO.File.ReadAllBytes(target+".meta");
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(target);
if(importer.filterMode!=UnityEngine.FilterMode.Point||importer.mipmapEnabled||importer.textureCompression!=UnityEditor.TextureImporterCompression.Uncompressed)throw new System.InvalidOperationException("Unexpected prior import settings");
var backup=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"coo-overwrit-environment-import-"+System.DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffff"));
System.IO.Directory.CreateDirectory(backup);
System.IO.File.WriteAllBytes(System.IO.Path.Combine(backup,"overwrit-materials.png.meta"),meta);
System.IO.File.Copy(target,System.IO.Path.Combine(backup,"overwrit-materials.png"));
var sourceBytes=System.IO.File.ReadAllBytes(source);System.IO.File.WriteAllBytes(target,sourceBytes);
bool metadataRewritten=false;
try{UnityEditor.AssetDatabase.ImportAsset(target,UnityEditor.ImportAssetOptions.ForceSynchronousImport);}
finally{if(!System.IO.File.Exists(target+".meta")||!meta.SequenceEqual(System.IO.File.ReadAllBytes(target+".meta"))){System.IO.File.WriteAllBytes(target+".meta",meta);metadataRewritten=true;}}
if(metadataRewritten)throw new System.InvalidOperationException("Import rewrote metadata; exact original bytes restored. PNG and metadata backup: "+backup);
var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(target);
if(texture==null||texture.width!=128||texture.height!=24||texture.filterMode!=UnityEngine.FilterMode.Point||texture.mipmapCount!=1)throw new System.InvalidOperationException("Unexpected native import");
if(!sourceBytes.SequenceEqual(System.IO.File.ReadAllBytes(target)))throw new System.InvalidOperationException("Import changed PNG source bytes");
using(var sha=System.Security.Cryptography.SHA256.Create()){return new{target,width=texture.width,height=texture.height,png_sha256=System.BitConverter.ToString(sha.ComputeHash(sourceBytes)).Replace("-","").ToLowerInvariant(),metadataUnchanged=true,backup,filter=texture.filterMode.ToString(),mipmaps=texture.mipmapCount};}
