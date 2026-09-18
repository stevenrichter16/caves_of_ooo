using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Validated presentation metadata. The native zone owns collision,
    /// inventories and saves; this manifest only binds authored models to that state.</summary>
    [Serializable]
    public sealed class Village3DManifest
    {
        public int schemaVersion,artSeed,zoneWidth,zoneHeight;
        public string id,coordinates,paletteTexture;
        public float artOriginX,artWidth;
        public Model[] models;
        public Owner[] owners;
        public Placement[] staticPlacements;
        public Building[] buildings;
        [Serializable] public sealed class Model
        {
            public string id,path,kind,pivot;
            public int triangles;
            public Vector3 boundsCenter,boundsSize;
            public bool rigged;
            public string[] clips,sockets;
        }
        [Serializable] public class Placement
        {
            public string id,modelId,role;
            public Vector3 position,scale;
            public float rotationY;
        }
        [Serializable] public sealed class CellPoint { public int x,y; }
        [Serializable] public sealed class Owner : Placement
        {
            public string ownerId,kind,roomId,visibleWhen;
            public int anchorX,anchorY;
            public Vector3 visualOffset;
            public bool mutable;
            public CellPoint[] footprint;
        }
        [Serializable] public sealed class Building
        { public string id,shellOwnerId,roofOwnerId,doorOwnerId; }
        [NonSerialized] private Dictionary<string,Model> modelIndex;
        [NonSerialized] private Dictionary<string,Owner> ownerIndex;

        public static Village3DManifest Parse(string json, MorrowfastSceneDefinition native=null)
        {
            if(string.IsNullOrWhiteSpace(json))throw new ArgumentException("Village manifest is required.");
            Village3DManifest value;
            try{value=JsonUtility.FromJson<Village3DManifest>(json);}
            catch(Exception e){throw new ArgumentException("Invalid village manifest JSON.",e);}
            if(value==null)throw new ArgumentException("Missing village manifest.");
            value.Validate(native);return value;
        }

        public void Validate(MorrowfastSceneDefinition native=null)
        {
            if(schemaVersion!=1||id!="morrowfast-village3d"||zoneWidth!=Zone.Width||zoneHeight!=Zone.Height
                ||artOriginX!=MorrowfastScenePresenter.OriginX||artWidth!=MorrowfastScenePresenter.ArtWidth)
                throw new ArgumentException("Unsupported village transform or version.");
            if(models==null||models.Length==0||owners==null||staticPlacements==null||buildings==null)
                throw new ArgumentException("Incomplete village manifest.");
            var newModels=new Dictionary<string,Model>(StringComparer.Ordinal);
            foreach(var model in models)
            {
                if(model==null||!SafeId(model.id)||newModels.ContainsKey(model.id)
                    ||model.path!="models/"+model.id+".fbx"||!Finite(model.boundsCenter)||!Positive(model.boundsSize)
                    ||model.triangles<=0||!ValidNames(model.clips)||!ValidNames(model.sockets)
                    ||model.rigged&&(model.clips==null||model.clips.Length==0))
                    throw new ArgumentException("Invalid or duplicate village model.");
                newModels.Add(model.id,model);
            }
            var newOwners=new Dictionary<string,Owner>(StringComparer.Ordinal);
            foreach(var owner in owners)
            {
                if(owner==null||!SafeId(owner.ownerId)||newOwners.ContainsKey(owner.ownerId)
                    ||owner.anchorX<0||owner.anchorX>=Zone.Width||owner.anchorY<0||owner.anchorY>=Zone.Height
                    ||!Finite(owner.visualOffset)||owner.footprint==null
                    ||owner.visibleWhen!=ExpectedVisibility(owner))
                    throw new ArgumentException("Invalid or duplicate village owner.");
                ValidatePlacement(owner,newModels);
                if((owner.position-(Village3DProjection.CellCentre(owner.anchorX,owner.anchorY)+owner.visualOffset)).sqrMagnitude>.000001f)
                    throw new ArgumentException("Village owner anchor disagrees with its placement.");
                var points=new HashSet<int>();
                foreach(var point in owner.footprint)
                    if(point==null||point.x<0||point.y<0||point.x>=Zone.Width||point.y>=Zone.Height||!points.Add(point.y*Zone.Width+point.x))
                        throw new ArgumentException("Invalid village owner footprint.");
                if(native!=null)
                {
                    var source=native.FindOwner(owner.ownerId);
                    if(source==null||source.anchorX!=owner.anchorX||source.anchorY!=owner.anchorY||source.kind!=owner.kind
                        ||source.mutable!=owner.mutable||!SameFootprint(points,source.footprint)
                        ||!string.Equals(source.roomId??"",owner.roomId??"",StringComparison.Ordinal))
                        throw new ArgumentException("Village manifest disagrees with native owner "+owner.ownerId);
                }
                newOwners.Add(owner.ownerId,owner);
            }
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var placement in staticPlacements)
            {
                if(placement==null||!SafeId(placement.id)||!ids.Add(placement.id))throw new ArgumentException("Invalid static placement identity.");
                ValidatePlacement(placement,newModels);
            }
            ids.Clear();
            foreach(var building in buildings)
            {
                if(building==null||!SafeId(building.id)||!ids.Add(building.id)
                    ||!newOwners.TryGetValue(building.shellOwnerId??"",out var shell)||shell.kind!="building-shell"
                    ||!newOwners.TryGetValue(building.roofOwnerId??"",out var roof)||roof.kind!="roof"
                    ||!newOwners.TryGetValue(building.doorOwnerId??"",out var door)||door.kind!="door"
                    ||shell.roomId!=building.id||roof.roomId!=building.id||door.roomId!=building.id)
                    throw new ArgumentException("Invalid village room ownership.");
                if(native!=null)
                {
                    MorrowfastSceneDefinition.BuildingSpec source=null;
                    foreach(var candidate in native.buildings)
                        if(candidate.id==building.id){source=candidate;break;}
                    if(source==null||source.roofId!=building.roofOwnerId||source.doorId!=building.doorOwnerId)
                        throw new ArgumentException("Village manifest disagrees with native room "+building.id);
                }
            }
            if(native!=null&&(newOwners.Count!=native.owners.Length||buildings.Length!=native.buildings.Length))
                throw new ArgumentException("Village kit must cover every native owner and room.");
            modelIndex=newModels;ownerIndex=newOwners;
        }
        // Exterior shell/roof/door stay visible while room contents follow cutaway.
        static string ExpectedVisibility(Owner owner)
            =>!string.IsNullOrEmpty(owner.roomId)&&owner.kind!="building-shell"&&owner.kind!="roof"&&owner.kind!="door"
                ?"room-open":"owner-visible";
        static bool SameFootprint(HashSet<int> points,MorrowfastSceneDefinition.CellPoint[] native)
        {
            if(native==null||points.Count!=native.Length)return false;
            foreach(var point in native)
                if(point==null||!points.Contains(point.y*Zone.Width+point.x))return false;
            return true;
        }
        // Empty optional lists are valid for static meshes; names, when supplied,
        // must be exact usable identifiers rather than ambiguous duplicate entries.
        static bool ValidNames(string[] values)
        {
            if(values==null)return true;
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var value in values)
                if(string.IsNullOrWhiteSpace(value)||!seen.Add(value))return false;
            return true;
        }
        static void ValidatePlacement(Placement p,Dictionary<string,Model> index)
        {
            if(string.IsNullOrEmpty(p.modelId)||!index.ContainsKey(p.modelId)||!Finite(p.position)||!Positive(p.scale)
                ||!Village3DProjection.Finite(p.rotationY))throw new ArgumentException("Invalid village model placement.");
        }
        public Model FindModel(string modelId)
        {if(string.IsNullOrEmpty(modelId))return null;if(modelIndex==null)Validate();return modelIndex.TryGetValue(modelId,out var value)?value:null;}
        public Owner FindOwner(string ownerId)
        {if(string.IsNullOrEmpty(ownerId))return null;if(ownerIndex==null)Validate();return ownerIndex.TryGetValue(ownerId,out var value)?value:null;}
        static bool Finite(Vector3 v)=>Village3DProjection.Finite(v.x)&&Village3DProjection.Finite(v.y)&&Village3DProjection.Finite(v.z);
        static bool Positive(Vector3 v)=>Finite(v)&&v.x>0&&v.y>0&&v.z>0;
        static bool SafeId(string value)
        {if(string.IsNullOrEmpty(value)||value.Length>96)return false;foreach(char c in value)if(!(c>='a'&&c<='z')&&!(c>='0'&&c<='9')&&c!='-'&&c!='_')return false;return true;}
    }
}
