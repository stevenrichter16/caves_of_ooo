using System;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class Village3DManifestTests
    {
        const string Valid = "{\"schemaVersion\":1,\"id\":\"morrowfast-village3d\",\"zoneWidth\":80,\"zoneHeight\":25,\"artOriginX\":21.25,\"artWidth\":37.5,\"models\":[{\"id\":\"barrel-0\",\"path\":\"models/barrel-0.fbx\",\"kind\":\"prop\",\"triangles\":12,\"boundsSize\":{\"x\":1,\"y\":1,\"z\":1}}],\"owners\":[{\"ownerId\":\"barrel\",\"modelId\":\"barrel-0\",\"anchorX\":4,\"anchorY\":5,\"position\":{\"x\":4.5,\"y\":0,\"z\":19.5},\"scale\":{\"x\":1,\"y\":1,\"z\":1},\"visibleWhen\":\"owner-visible\",\"footprint\":[]}],\"staticPlacements\":[],\"buildings\":[]}";
        [Test] public void ValidManifestResolvesModelsAndOwnersWithoutChangingCoordinates()
        {
            var d = Village3DManifest.Parse(Valid);
            Assert.NotNull(d.FindModel("barrel-0")); Assert.NotNull(d.FindOwner("barrel"));
            Assert.IsNull(d.FindModel("not-here")); Assert.IsNull(d.FindOwner(null));
            Assert.AreEqual(new Vector3(4.5f,0,19.5f),d.owners[0].position);
        }
        [TestCase("version")] [TestCase("identity")] [TestCase("width")] [TestCase("height")]
        [TestCase("origin")] [TestCase("art-width")] [TestCase("null-models")] [TestCase("empty-models")]
        [TestCase("null-model")] [TestCase("duplicate-model")] [TestCase("unsafe-model-id")]
        [TestCase("path-traversal")] [TestCase("wrong-extension")] [TestCase("invalid-bounds")]
        [TestCase("null-owners")] [TestCase("null-owner")] [TestCase("duplicate-owner")]
        [TestCase("missing-model")] [TestCase("invalid-cell")] [TestCase("nan-position")]
        [TestCase("mismatched-position")] [TestCase("zero-scale")] [TestCase("negative-scale")]
        [TestCase("infinite-rotation")] [TestCase("bad-footprint")] [TestCase("null-static")]
        [TestCase("bad-static-model")] [TestCase("null-buildings")] [TestCase("orphan-building")]
        public void AdversarialMalformedContractFailsBeforeBinding(string mutation)
        {
            var d=Village3DManifest.Parse(Valid); Assert.DoesNotThrow(()=>d.Validate());
            switch(mutation)
            {
                case "version":d.schemaVersion++;break;
                case "identity":d.id="felling";break;
                case "width":d.zoneWidth++;break;
                case "height":d.zoneHeight++;break;
                case "origin":d.artOriginX=float.NaN;break;
                case "art-width":d.artWidth=80;break;
                case "null-models":d.models=null;break;
                case "empty-models":d.models=Array.Empty<Village3DManifest.Model>();break;
                case "null-model":d.models[0]=null;break;
                case "duplicate-model":d.models=new[]{d.models[0],d.models[0]};break;
                case "unsafe-model-id":d.models[0].id="../escape";break;
                case "path-traversal":d.models[0].path="models/../barrel-0.fbx";break;
                case "wrong-extension":d.models[0].path="models/barrel-0.cs";break;
                case "invalid-bounds":d.models[0].boundsSize=new Vector3(1,float.NaN,1);break;
                case "null-owners":d.owners=null;break;
                case "null-owner":d.owners[0]=null;break;
                case "duplicate-owner":d.owners=new[]{d.owners[0],d.owners[0]};break;
                case "missing-model":d.owners[0].modelId="absent";break;
                case "invalid-cell":d.owners[0].anchorX=-1;break;
                case "nan-position":d.owners[0].position=new Vector3(float.NaN,0,1);break;
                case "mismatched-position":d.owners[0].position+=Vector3.right;break;
                case "zero-scale":d.owners[0].scale=Vector3.zero;break;
                case "negative-scale":d.owners[0].scale=-Vector3.one;break;
                case "infinite-rotation":d.owners[0].rotationY=float.PositiveInfinity;break;
                case "bad-footprint":d.owners[0].footprint=new[]{new Village3DManifest.CellPoint{x=80,y=0}};break;
                case "null-static":d.staticPlacements=null;break;
                case "bad-static-model":d.staticPlacements=new[]{new Village3DManifest.Placement{id="grass",modelId="absent",scale=Vector3.one}};break;
                case "null-buildings":d.buildings=null;break;
                case "orphan-building":d.buildings=new[]{new Village3DManifest.Building{id="house",shellOwnerId="absent",roofOwnerId="absent",doorOwnerId="absent"}};break;
                default:Assert.Fail("Unimplemented mutation");break;
            }
            Assert.Throws<ArgumentException>(()=>d.Validate());
        }
        [TestCase(null)] [TestCase("")] [TestCase("null")] [TestCase("{")] [TestCase("{}")]
        public void EmptyOrMalformedInputCannotCreateAPartialWorld(string json)
            =>Assert.Throws<ArgumentException>(()=>Village3DManifest.Parse(json));
    }
}
