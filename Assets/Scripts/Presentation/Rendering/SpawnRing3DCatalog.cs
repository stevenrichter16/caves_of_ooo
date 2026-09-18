using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Art coverage only. Actual cells, entity ownership, collision and
    /// saved mutations are resolved by the native zone, never by this catalog.</summary>
    [Serializable]
    public sealed class SpawnRing3DCatalog
    {
        public const string CoordinateContract="Unity X east,Y height,Z north; 1 unit per native cell; source Blender X east,Y north,Z up";
        public int schemaVersion,zoneWidth,zoneHeight;
        public string id,coordinates,paletteTexture;
        public Model[] models;
        public BlueprintBinding[] blueprints;
        public FellingOwner[] fellingOwners;
        public WaterBinding tileState;
        public string[] zones,materialSlots;
        public EquipmentBinding externalEquipment;
        [Serializable] public sealed class Model
        {
            public string id,path,kind,rigFamily,materialFamily,sourceBlueprint;
            public bool rigged;
            public Vector3 boundsCenter,boundsSize;
            public int triangles;
            public string[] clips,sockets;
        }
        [Serializable] public sealed class BlueprintBinding
        {public string blueprint,role,resolver;public string[] models;}
        [Serializable] public sealed class FellingOwner
        {public string componentId,modelId;public bool mutable;}
        [Serializable] public sealed class WaterBinding
        {public string waterModel;public float height;}
        [Serializable] public sealed class EquipmentBinding
        {public string library;public string[] models;}
        [NonSerialized] Dictionary<string,Model> modelIndex;
        [NonSerialized] Dictionary<string,BlueprintBinding> blueprintIndex;
        [NonSerialized] Dictionary<string,FellingOwner> fellingIndex;
        [NonSerialized] HashSet<string> zoneIndex;
        static readonly string[] RequiredBlueprints={
            "AshBed","BrinePool","Bush","Campfire","CascadeFather","CaveHermit",
            "Chest","ChoirIronVein","ChoirTendril","CompostCache","CompostRow","CopperPipe",
            "Crate","DescentLedge","DryBrush","FallenBeam","FellingBarePosition","FellingSceneProp",
            "Floor","FruitingBody","GlasspaneFrog","GlowQuartzVein","GrainRidge","Grass","Grib",
            "GroveRedGrowth","GroveSeep","GroveSign","HaulBarrel","HelmwoodFrog","HollowLog",
            "MawToad","MillStone","Mogu","Mosshulk","MushroomRing","MycelialColumn",
            "Nam","OreCache","PeatBog","Player","Rock","Rotling",
            "Sack","SariSnake","SeventhPosition","Shambler","Sien","SinkholeLip",
            "SkySari","Snapjaw","SnapjawWarlord","Sopp","SprayPool","StairsDown",
            "StairsUp","SteamVent","StoneCoffer","StrongBox","TarSeep","TepuiStone",
            "TepuiWall","Tepuibone","TepuiboneVein","Tree","VineWall","Wall",
            "Wardline","WaterPuddle","WineLeafSundew","WoodenBarrel","WovenBasket","YellowfootWayfarer"
        };
        static readonly string[] RequiredZones={"Overworld.2.5.0","Overworld.2.6.0","Overworld.2.7.0","Overworld.3.5.0","Overworld.3.7.0","Overworld.4.5.0","Overworld.4.6.0","Overworld.4.7.0"};
        static readonly string[] EquipmentIds={"equipment-blade","equipment-club","equipment-staff","equipment-shield","equipment-pack","equipment-helmet"};
        static readonly string[] ClipNames={"Idle","Walk","Interact","Attack","Hit"};
        static readonly string[] HumanoidSockets={"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"};
        static readonly Dictionary<string,string> ActorRigs=new Dictionary<string,string>(StringComparer.Ordinal)
        {
            {"CascadeFather","frog"},
            {"CaveHermit","humanoid"},
            {"ChoirTendril","rooted"},
            {"GlasspaneFrog","frog"},
            {"Grib","humanoid"},
            {"HelmwoodFrog","frog"},
            {"MawToad","frog"},
            {"Mogu","humanoid"},
            {"Mosshulk","fungal"},
            {"Nam","humanoid"},
            {"Player","humanoid"},
            {"Rotling","fungal"},
            {"SariSnake","serpent"},
            {"Shambler","fungal"},
            {"Sien","humanoid"},
            {"SkySari","avian"},
            {"Snapjaw","humanoid"},
            {"SnapjawWarlord","humanoid"},
            {"Sopp","humanoid"},
            {"Wardline","serpent"},
            {"YellowfootWayfarer","tortoise"}
        };
        static readonly HashSet<string> GroundBlueprints=new HashSet<string>(new[]{"Floor","Grass","TepuiStone","WaterPuddle"},StringComparer.Ordinal);
        public static SpawnRing3DCatalog Parse(string json,FellingSceneDefinition native=null)
        {
            if(string.IsNullOrWhiteSpace(json))throw new ArgumentException("Spawn-ring catalog JSON is required.");
            SpawnRing3DCatalog result;
            try{result=JsonUtility.FromJson<SpawnRing3DCatalog>(json);}
            catch(Exception error){throw new ArgumentException("Invalid spawn-ring catalog JSON.",error);}
            if(result==null)throw new ArgumentException("Missing spawn-ring catalog.");
            result.Validate(native);return result;
        }
        public void Validate(FellingSceneDefinition native=null)
        {
            Require(schemaVersion==1&&id=="spawn-ring-3d"&&zoneWidth==Zone.Width&&zoneHeight==Zone.Height,"Unsupported spawn-ring identity, dimensions or version.");
            Require(coordinates==CoordinateContract&&paletteTexture=="textures/SpawnRingPalette.png","Unexpected spawn-ring coordinate/palette contract.");
            Require(models!=null&&models.Length>0&&blueprints!=null&&fellingOwners!=null&&tileState!=null&&externalEquipment!=null,"Incomplete spawn-ring catalog.");
            Require(SameNames(zones,RequiredZones)&&SameNames(materialSlots,new[]{"SpawnRingPalette","SpawnRingWater"}),"Unexpected spawn-ring zones or material slots.");
            Require(externalEquipment.library==Village3DLibrary.ResourcePath&&SameNames(externalEquipment.models,EquipmentIds),"Unsupported external equipment contract.");
            native=native??FellingSceneDefinition.Load();Require(native!=null,"Native Felling definition is required.");native.Validate();
            var modelMap=new Dictionary<string,Model>(StringComparer.Ordinal);
            foreach(var m in models)
            {
                Require(m!=null&&SafeId(m.id)&&!modelMap.ContainsKey(m.id),"Invalid or duplicate spawn-ring model identity.");
                Require(m.path=="models/"+m.id+".fbx"&&Finite(m.boundsCenter)&&Positive(m.boundsSize)&&m.triangles>0,"Invalid model path or measured geometry: "+m.id);
                Require(m.materialFamily=="ring-palette"||m.materialFamily=="ring-water","Unknown material family: "+m.id);
                Require(m.kind=="entity"||m.kind=="ground"||m.kind=="actor"||m.kind=="felling-component"||m.kind=="tile-overlay","Unknown model kind: "+m.id);
                if(m.rigged)
                {
                    Require(m.kind=="actor"&&(m.rigFamily=="humanoid"||m.rigFamily=="frog"||m.rigFamily=="tortoise"||m.rigFamily=="serpent"||m.rigFamily=="avian"||m.rigFamily=="fungal"||m.rigFamily=="rooted"),"Invalid actor rig: "+m.id);
                    Require(SameNames(m.clips,ClipNames)&&SameNames(m.sockets,m.rigFamily=="humanoid"?HumanoidSockets:Array.Empty<string>()),"Invalid rig clip/socket contract: "+m.id);
                }
                else Require(m.kind!="actor"&&m.rigFamily=="none"&&SameNames(m.clips,Array.Empty<string>())&&SameNames(m.sockets,Array.Empty<string>()),"Static model declares a rig: "+m.id);
                modelMap.Add(m.id,m);
            }
            var blueprintMap=new Dictionary<string,BlueprintBinding>(StringComparer.Ordinal);
            var usedModels=new HashSet<string>(StringComparer.Ordinal);
            foreach(var b in blueprints)
            {
                Require(b!=null&&!string.IsNullOrWhiteSpace(b.blueprint)&&!blueprintMap.ContainsKey(b.blueprint),"Invalid or duplicate blueprint binding.");
                string expected=b.blueprint=="FellingSceneProp"?"felling-component":ActorRigs.ContainsKey(b.blueprint)?"actor":GroundBlueprints.Contains(b.blueprint)?"ground":"entity";
                Require(b.role==expected&&b.models!=null,"Unexpected blueprint role: "+b.blueprint);
                if(b.blueprint=="FellingSceneProp")
                    Require(b.models.Length==0&&b.resolver=="FellingSceneProp.ComponentId","Felling requires native component-owner resolution.");
                else
                {
                    Require(b.models.Length>0&&string.IsNullOrEmpty(b.resolver),"Missing models or unknown resolver: "+b.blueprint);
                    var choices=new HashSet<string>(StringComparer.Ordinal);
                    foreach(string modelId in b.models)
                    {
                        Require(!string.IsNullOrEmpty(modelId)&&choices.Add(modelId)&&modelMap.TryGetValue(modelId,out _),"Missing or duplicated model choice: "+b.blueprint);
                        var m=modelMap[modelId];Require(m.kind==b.role,"Blueprint/model role mismatch: "+b.blueprint);
                        if(ActorRigs.TryGetValue(b.blueprint,out string rig))Require(m.rigged&&m.rigFamily==rig,"Species rig mismatch: "+b.blueprint);
                        usedModels.Add(modelId);
                    }
                }
                blueprintMap.Add(b.blueprint,b);
            }
            Require(blueprintMap.Count==RequiredBlueprints.Length,"Spawn-ring authored blueprint coverage is incomplete or unexpected.");
            foreach(string required in RequiredBlueprints)Require(blueprintMap.ContainsKey(required),"Missing required ring blueprint: "+required);
            var ownerMap=new Dictionary<string,FellingOwner>(StringComparer.Ordinal);
            foreach(var owner in fellingOwners)
            {
                Require(owner!=null&&SafeId(owner.componentId)&&!ownerMap.ContainsKey(owner.componentId),"Invalid or duplicate Felling owner binding.");
                var source=native.FindLayer(owner.componentId);
                Require(source!=null&&source.mutable==owner.mutable,"Felling owner/policy disagrees with native source: "+owner.componentId);
                Require(!string.IsNullOrEmpty(owner.modelId)&&modelMap.TryGetValue(owner.modelId,out var m)&&m.kind=="felling-component","Missing Felling component model: "+owner.componentId);
                ownerMap.Add(owner.componentId,owner);usedModels.Add(owner.modelId);
            }
            Require(ownerMap.Count==native.layers.Length,"Every native Felling component needs its own binding.");
            Require(!string.IsNullOrEmpty(tileState.waterModel)&&modelMap.TryGetValue(tileState.waterModel,out var water)&&water.kind=="tile-overlay"&&water.materialFamily=="ring-water","Missing native tile-water model.");
            Require(Village3DProjection.Finite(tileState.height)&&tileState.height>0&&tileState.height<.1f,"Invalid tile-water offset.");
            usedModels.Add(tileState.waterModel);
            Require(usedModels.Count==modelMap.Count,"Catalog contains unreferenced model assets.");
            // Publish indices only when the complete authored contract validates.
            modelIndex=modelMap;blueprintIndex=blueprintMap;fellingIndex=ownerMap;
            zoneIndex=new HashSet<string>(zones,StringComparer.Ordinal);
        }
        public Model FindModel(string modelId)
        {if(string.IsNullOrEmpty(modelId))return null;if(modelIndex==null)Validate();return modelIndex.TryGetValue(modelId,out var value)?value:SpreadVoxelLibrary.Load()?.Find(modelId)?.Spec ?? SoddenVoxelLibrary.Load()?.Find(modelId)?.Spec ?? BeatingVoxelLibrary.Load()?.Find(modelId)?.Spec ?? StumpVoxelLibrary.Load()?.Find(modelId)?.Spec ?? OverwritVoxelLibrary.Load()?.Find(modelId)?.Spec ?? GinmereVoxelLibrary.Load()?.Find(modelId)?.Spec ?? CathedralVoxelLibrary.Load()?.Find(modelId)?.Spec ?? StillleafVoxelLibrary.Load()?.Find(modelId)?.Spec ?? OlderdeepVoxelLibrary.Load()?.Find(modelId)?.Spec ?? WellmeetVoxelLibrary.Load()?.Find(modelId)?.Spec ?? CinderholdVoxelKitLibrary.Load()?.Find(modelId)?.Spec ?? SumpholdVoxelKitLibrary.Load()?.Find(modelId)?.Spec ?? DrownedLedgerVoxelKitLibrary.Load()?.Find(modelId)?.Spec ?? MarrowstyeVoxelKitLibrary.Load()?.Find(modelId)?.Spec ?? FirstTentVoxelKitLibrary.Load()?.Find(modelId)?.Spec ?? LastCounterVoxelKitLibrary.Load()?.Find(modelId)?.Spec ?? GantryVoxelKitLibrary.Load()?.Find(modelId)?.Spec ?? TineVoxelKitLibrary.Load()?.Find(modelId)?.Spec ?? QuillholdVoxelKitLibrary.Load()?.Find(modelId)?.Spec ?? TallyVoxelKitLibrary.Load()?.Find(modelId)?.Spec;}
        public BlueprintBinding FindBlueprint(string blueprint)
        {if(string.IsNullOrEmpty(blueprint))return null;if(blueprintIndex==null)Validate();return blueprintIndex.TryGetValue(blueprint,out var value)?value:null;}
        public FellingOwner FindFellingOwner(string componentId)
        {if(string.IsNullOrEmpty(componentId))return null;if(fellingIndex==null)Validate();return fellingIndex.TryGetValue(componentId,out var value)?value:null;}
        public bool SupportsZone(string zoneId)
        {if(string.IsNullOrEmpty(zoneId))return false;if(zoneIndex==null)Validate();return zoneIndex.Contains(zoneId)||(GrovelandsCompositionPlan.IsWildernessZone(zoneId) || SpreadCompositionPlan.IsWildernessZone(zoneId) || SoddenCompositionPlan.IsWildernessZone(zoneId) || BeatingCompositionPlan.IsWildernessZone(zoneId) || StumpCompositionPlan.IsWildernessZone(zoneId) || OverwritCompositionPlan.IsWildernessZone(zoneId) || GinmereCompositionPlan.IsSupportedZone(zoneId) || CathedralCompositionPlan.IsSupportedZone(zoneId) || StillleafCompositionPlan.IsSupportedZone(zoneId) || OlderdeepCompositionPlan.IsSupportedZone(zoneId) || WellmeetCompositionPlan.IsSupportedZone(zoneId) || CinderholdCompositionPlan.IsSupportedZone(zoneId) || SumpholdCompositionPlan.IsSupportedZone(zoneId) || DrownedLedgerCompositionPlan.IsSupportedZone(zoneId) || MarrowstyeCompositionPlan.IsSupportedZone(zoneId) || FirstTentCompositionPlan.IsSupportedZone(zoneId) || LastCounterCompositionPlan.IsSupportedZone(zoneId) || GantryCompositionPlan.IsSupportedZone(zoneId) || TineCompositionPlan.IsSupportedZone(zoneId) || QuillholdCompositionPlan.IsSupportedZone(zoneId) || TallyCompositionPlan.IsSupportedZone(zoneId));}
        static bool SameNames(string[] values,string[] expected)
        {
            if(values==null||values.Length!=expected.Length)return false;
            var names=new HashSet<string>(StringComparer.Ordinal);
            foreach(string value in values)if(string.IsNullOrWhiteSpace(value)||!names.Add(value))return false;
            return names.SetEquals(expected);
        }
        static bool SafeId(string value)
        {if(string.IsNullOrEmpty(value)||value.Length>96)return false;foreach(char c in value)if(!(c>='a'&&c<='z')&&!(c>='0'&&c<='9')&&c!='-'&&c!='_')return false;return true;}
        static bool Finite(Vector3 v)=>Village3DProjection.Finite(v.x)&&Village3DProjection.Finite(v.y)&&Village3DProjection.Finite(v.z);
        static bool Positive(Vector3 v)=>Finite(v)&&v.x>0&&v.y>0&&v.z>0;
        static void Require(bool condition,string message){if(!condition)throw new ArgumentException(message);}
    }
}
