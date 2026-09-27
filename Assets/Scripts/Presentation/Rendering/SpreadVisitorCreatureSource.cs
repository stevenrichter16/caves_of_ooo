using System;
using System.Collections.Generic;
namespace CavesOfOoo.Rendering
{
    /// <summary>Exact original anatomy and semantic palette contracts for the29
    /// missing receiving-biome bodies. This table grants no gameplay authority.</summary>
    public static class SpreadVisitorCreatureSource
    {
        public sealed class Spec
        {
            public readonly string Blueprint, Id, Glyph, RigFamily;
            public readonly string[] Bones, Sockets;
            public readonly float MaxHeight, MaxWidth;
            internal Spec(string bp, string id, string glyph, string rig, string[] bones, string[] sockets, float height, float width)
            { Blueprint=bp; Id=id; Glyph=glyph; RigFamily=rig; Bones=bones; Sockets=sockets; MaxHeight=height; MaxWidth=width; }
        }
        public static readonly string[] Clips={"Idle","Walk","Interact","Attack","Hit"};
        public static readonly string[] Palette={"#082C28","#103E36","#1A4B40","#26594A","#A0A77C","#CBC697","#647353","#235D25","#40872C","#65AE3D","#435A53","#62786C","#819489","#16883B","#45CB4B","#A0E772","#207838","#D2D3B4","#B77B43","#403D28","#756C40","#A39456","#C4B877","#243E39","#C84332","#151C1C","#37476B","#D99B43","#95CBCB","#866C98"};
        public static readonly Spec[] Specs={
            new Spec("CaveBat","spread-visitor-cave-bat","b","avian",new[]{"Root","Body","Head","Wing.L","Leg.L","Wing.R","Leg.R"},Array.Empty<string>(),0.85f,1.12f),
            new Spec("CaveSlime","spread-visitor-cave-slime","j","gel",new[]{"Root","Body"},Array.Empty<string>(),0.85f,0.7f),
            new Spec("CaveBear","spread-visitor-cave-bear","B","quadruped",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R"},Array.Empty<string>(),0.85f,0.8f),
            new Spec("PaleStalker","spread-visitor-pale-stalker","p","quadruped",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R","Tail.0","Tail.1","Tail.2"},Array.Empty<string>(),0.85f,0.8f),
            new Spec("DesertProwler","spread-visitor-desert-prowler","D","quadruped",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R","Tail.0","Tail.1","Tail.2"},Array.Empty<string>(),0.85f,0.8f),
            new Spec("BrittleHound","spread-visitor-brittle-hound","b","quadruped",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R","Tail.0","Tail.1","Tail.2"},Array.Empty<string>(),0.85f,0.8f),
            new Spec("JungleStalker","spread-visitor-jungle-stalker","J","quadruped",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R","Tail.0","Tail.1","Tail.2"},Array.Empty<string>(),0.85f,0.8f),
            new Spec("Scorpion","spread-visitor-scorpion","x","arachnid",new[]{"Root","Body","Head","Leg.L0","Leg.L1","Leg.L2","Leg.L3","Arm.L","Leg.R0","Leg.R1","Leg.R2","Leg.R3","Arm.R","Tail.0","Tail.1","Tail.2","Tail.3","Tail.4"},Array.Empty<string>(),0.85f,1.12f),
            new Spec("GlassScorpion","spread-visitor-glass-scorpion","s","arachnid",new[]{"Root","Body","Head","Leg.L0","Leg.L1","Leg.L2","Leg.L3","Arm.L","Leg.R0","Leg.R1","Leg.R2","Leg.R3","Arm.R","Tail.0","Tail.1","Tail.2","Tail.3","Tail.4"},Array.Empty<string>(),0.85f,1.12f),
            new Spec("SandWurm","spread-visitor-sand-wurm","W","serpent",new[]{"Root","Body","Segment.0","Segment.1","Segment.2","Segment.3","Segment.4","Segment.5","Segment.6","Head"},Array.Empty<string>(),0.85f,1.12f),
            new Spec("DuneLurker","spread-visitor-dune-lurker","d","grasping",new[]{"Root","Body","Head","Arm.L0","Arm.L1","Arm.R0","Arm.R1"},Array.Empty<string>(),0.85f,1.12f),
            new Spec("CanopyStrangler","spread-visitor-canopy-strangler","C","rooted",new[]{"Root","Body","Tendril.0","Tendril.1","Tendril.2","Tendril.3"},Array.Empty<string>(),0.85f,0.7f),
            new Spec("SkeletalSentry","spread-visitor-skeletal-sentry","Z","humanoid",new[]{"Root","Spine","Head","Arm.L","Hand.L","Leg.L","Arm.R","Hand.R","Leg.R"},new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},1.25f,0.74f),
            new Spec("StoneGolem","spread-visitor-stone-golem","G","humanoid",new[]{"Root","Spine","Head","Arm.L","Hand.L","Leg.L","Arm.R","Hand.R","Leg.R"},new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},1.25f,0.74f),
            new Spec("ObsidianBrute","spread-visitor-obsidian-brute","O","humanoid",new[]{"Root","Spine","Head","Arm.L","Hand.L","Leg.L","Arm.R","Hand.R","Leg.R"},new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},1.25f,0.74f),
            new Spec("AncientGuardian","spread-visitor-ancient-guardian","H","humanoid",new[]{"Root","Spine","Head","Arm.L","Hand.L","Leg.L","Arm.R","Hand.R","Leg.R"},new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},1.25f,0.74f),
            new Spec("VaultSentinel","spread-visitor-vault-sentinel","V","humanoid",new[]{"Root","Spine","Head","Arm.L","Hand.L","Leg.L","Arm.R","Hand.R","Leg.R"},new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},1.25f,0.74f),
            new Spec("BrassHusk","spread-visitor-brass-husk","H","humanoid",new[]{"Root","Spine","Head","Arm.L","Hand.L","Leg.L","Arm.R","Hand.R","Leg.R"},new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},1.25f,0.74f),
            new Spec("IceWight","spread-visitor-ice-wight","W","humanoid",new[]{"Root","Spine","Head","Arm.L","Hand.L","Leg.L","Arm.R","Hand.R","Leg.R"},new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},1.25f,0.74f),
            new Spec("CharredHusk","spread-visitor-charred-husk","H","humanoid",new[]{"Root","Spine","Head","Arm.L","Hand.L","Leg.L","Arm.R","Hand.R","Leg.R"},new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},1.25f,0.74f),
            new Spec("SleepingTroll","spread-visitor-sleeping-troll","T","humanoid",new[]{"Root","Spine","Head","Arm.L","Hand.L","Leg.L","Arm.R","Hand.R","Leg.R"},new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},1.25f,0.74f),
            new Spec("SporeShambler","spread-visitor-spore-shambler","f","humanoid",new[]{"Root","Spine","Head","Arm.L","Hand.L","Leg.L","Arm.R","Hand.R","Leg.R"},new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},1.25f,0.74f),
            new Spec("Reedfrog","spread-visitor-reedfrog","f","frog",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R"},Array.Empty<string>(),0.85f,0.7f),
            new Spec("Bandfrog","spread-visitor-bandfrog","f","frog",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R"},Array.Empty<string>(),0.85f,0.7f),
            new Spec("GinFrog","spread-visitor-gin-frog","f","frog",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R"},Array.Empty<string>(),0.85f,0.7f),
            new Spec("SummitSinger","spread-visitor-summit-singer","f","frog",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R"},Array.Empty<string>(),0.85f,0.7f),
            new Spec("SunStriker","spread-visitor-sun-striker","l","lizard",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R","Tail.0","Tail.1","Tail.2","Tail.3","Tail.4"},Array.Empty<string>(),0.85f,0.7f),
            new Spec("BrocchiniaSentinel","spread-visitor-brocchinia-sentinel","l","lizard",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R","Tail.0","Tail.1","Tail.2","Tail.3","Tail.4"},Array.Empty<string>(),0.85f,0.7f),
            new Spec("PrickleBrowGecko","spread-visitor-prickle-brow-gecko","l","lizard",new[]{"Root","Body","Head","LegFront.L","LegRear.L","LegFront.R","LegRear.R","Tail.0","Tail.1","Tail.2","Tail.3","Tail.4"},Array.Empty<string>(),0.85f,0.7f),
        };
        private static readonly Dictionary<string,Spec> ById=new Dictionary<string,Spec>(StringComparer.Ordinal);
        private static readonly Dictionary<string,Spec> ByBlueprint=new Dictionary<string,Spec>(StringComparer.Ordinal);
        static SpreadVisitorCreatureSource()
        { foreach(var s in Specs){ById.Add(s.Id,s);ByBlueprint.Add(s.Blueprint,s);} }
        public static bool IsPaletteCoordinate(float x,float y)
        {
            float swatch=x*30-.5f;
            return !float.IsNaN(swatch)&&!float.IsInfinity(swatch)&&!float.IsNaN(y)&&!float.IsInfinity(y)&&Math.Abs(y-.5f)<=.00001f
                &&swatch>=-.00001f&&swatch<=29.00001f&&Math.Abs(swatch-Math.Round(swatch))<=.00001;
        }
        public static Spec Find(string id)=>id!=null&&ById.TryGetValue(id,out var s)?s:null;
        public static Spec ForBlueprint(string blueprint)=>blueprint!=null&&ByBlueprint.TryGetValue(blueprint,out var s)?s:null;
    }
}
