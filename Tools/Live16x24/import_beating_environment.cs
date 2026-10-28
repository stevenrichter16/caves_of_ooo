// Execute only after editable Beating environment source verification, with Play stopped.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play first");
var target="Assets/Resources/Sprites/Live16x24/beating-materials.png";
var source="ArtSource/Live16x24/beating-environment/beating-materials.png";
var meta=System.IO.File.ReadAllBytes(target+".meta");
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(target);
if(importer.filterMode!=UnityEngine.FilterMode.Point||importer.mipmapEnabled||importer.textureCompression!=UnityEditor.TextureImporterCompression.Uncompressed)throw new System.InvalidOperationException("Unexpected prior import settings");
var sourceBytes=System.IO.File.ReadAllBytes(source);System.IO.File.WriteAllBytes(target,sourceBytes);
UnityEditor.AssetDatabase.ImportAsset(target,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var texture=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(target);
if(texture==null||texture.width!=544||texture.height!=24||texture.filterMode!=UnityEngine.FilterMode.Point||texture.mipmapCount!=1)throw new System.InvalidOperationException("Unexpected native import");
if(!meta.SequenceEqual(System.IO.File.ReadAllBytes(target+".meta")))throw new System.InvalidOperationException("Import changed pre-existing metadata; preserve original bytes before proceeding");
if(!sourceBytes.SequenceEqual(System.IO.File.ReadAllBytes(target)))throw new System.InvalidOperationException("Import changed PNG source bytes");
using(var sha=System.Security.Cryptography.SHA256.Create()){return new{target,width=texture.width,height=texture.height,png_sha256=System.BitConverter.ToString(sha.ComputeHash(sourceBytes)).Replace("-","").ToLowerInvariant(),metadataUnchanged=true,filter=texture.filterMode.ToString(),mipmaps=texture.mipmapCount};}
