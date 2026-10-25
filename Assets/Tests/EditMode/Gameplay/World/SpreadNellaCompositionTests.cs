using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Cold Nella geometry and authoritative complete stamp; no live-layout rewrite.</summary>
    public sealed class SpreadNellaCompositionTests
    {
        const string Nella = "Overworld.11.8.0", Role = SpreadExplorationResidents.RoleKey;
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        static readonly SpreadExplorationTopology[] Topologies = { SpreadExplorationTopology.OffsetLanes,
            SpreadExplorationTopology.BrokenEnclosures, SpreadExplorationTopology.BankCrossing };
        static readonly (string bp, string role, int x, int y)[] Stamp = {
            ("SpreadSeedKeeper","resident",0,0), ("CandyCarrotCrop","crop",-2,-1), ("CandyCarrotCrop","crop",-1,-1),
            ("EmberwheatCrop","crop",-2,1), ("EmberwheatCrop","crop",-1,1), ("Signpost","sign",2,0),
            ("MarlrootCrop","reserve-crop",-2,-3), ("PitchpodCrop","reserve-crop",-1,-3),
            ("ConnectedReserveTray","reserve-tray",2,-2), ("Signpost","reserve-sign",1,-3) };
        static readonly (int x, int y)[] Gaps = { (0,-1), (0,1) };
        sealed class Baseline
        {
            internal int Seed; internal SpreadExplorationTopology Topology; internal string Hash; readonly byte[] rows;
            internal Baseline(int seed, SpreadExplorationTopology topology, string hash, string mask)
            { Seed=seed; Topology=topology; Hash=hash; rows=Convert.FromBase64String(mask); }
            internal bool Row(int x,int y) { int i=y*Zone.Width+x;return (rows[i/8]&(1<<(i%8)))!=0; }
        }
        // Exact native pre-change output, captured by root before any production edit.
        // Full input receipt: nella-composition-original-plans.json.gz under Verification/Live16x24.
        static readonly Baseline[] Before = {
            new Baseline(64,SpreadExplorationTopology.OffsetLanes,"025e128ab032ce9a811db1d6724640b1c89ca26b4131b068f21ce94d6776585d","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAECybQGADdogAQCAFmgDgE2CJAEAgBJsAQAowaIBAMAUaAOAbAGQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAoG4YFAADDJgAAbBkmBAAEQjYAAEADIgEAbMEiAAAoGqIBAGxDNAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(64,SpreadExplorationTopology.BrokenEnclosures,"c984c24fb1847f45d36d59d9434bc551746bb3cf9918ce920cd32363094b03f6","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAECyLQCRlgQAFACAFigAg4AAAAYBgBIsAFKwAYCEAMAUKQDTIAGAggCAFAUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIgk0AAAHDpgAA2IQkAIAFQjYAAMAGaQAAbcGiAACYhmkAAGxDtAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(64,SpreadExplorationTopology.BankCrossing,"1186c446fac1b4d6adb36c257b796125be9262801e99d6aad63185d50757dd7a","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABh2yQAAAAAAACATEAmAIAgAQYAgADAEACwYYg0AIBFQQYAICmYIgCALcE0ALAtyBYAAAAAAACAYEA2AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA2IZBWwIAAAAAANgmQMoEAAAAAABAIkFLBgAAAAAA2KIhQgYAAAAAAMiQaYgCAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(65,SpreadExplorationTopology.OffsetLanes,"d6185b972d35b45dc0a81a29ee3c10d512140e2df5464c77d98c89c8f3f40aef","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMC2bQOAbdu2AQDANmwDgGzDlgEAQDRMA4Btw7YBAMA2ZAOAbMO2AQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAsG7YFAGzDFgAAbBs2BQBswzYAAGwbtgUAbMI0AABsG7YFAGzDNgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(65,SpreadExplorationTopology.BrokenEnclosures,"baa5a19109e627f7feda58220ace4b14024397d433a2a3dd998daf08f44f8724","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMC2LQDbtgXAtgHAFi0A27AFwIIBQBQNANkwBcCGAcAWJQDbsAXAhAHAFikAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAYhm0AgG3DlgAA2IZtAIBtw7YAANiGbQCAbcK0AADYhm0AgG3DtgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(65,SpreadExplorationTopology.BankCrossing,"9d7503462e6038df798e17cff29c53bdb6b54e3cec7fc42a7d0661fefefbfb0c","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIBt2zYAAAAAAACAbdE2ALBt2yIAAG3RNgAwbdg2AIBt0SYAsGXYNACAbdE2ALBt2DYAAAAAAAAwaNg2AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA2LZB2wYAAAAAANg2IdsGAAAAAACYtmGbBgAAAAAA2LZh2wYAAAAAANi2bdsGAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(1729,SpreadExplorationTopology.OffsetLanes,"77137eae83572b9c6cb39669bb87ee4fbb3d8a458f1ac44c3bad7a954b1dfbd8","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAECiJQOAaBq0AQDAAigBACjDhgEAgBRsA4BhgLIBAEAWTAMAJUCyAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAsEoYFAGjCMgAAbAGiBAAoQiYAAEQThgUAbEA0AAAsG4IEACRDMgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(1729,SpreadExplorationTopology.BrokenEnclosures,"cae6d7591954dd70323b296cc65cbe223b4215caa19ef666d6da40f251bbc160","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAECiJQDBsATAJAHAAikAC7ABwIYBgBQtANKgBICGAUAWDABbsATAggHAEiwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACIgkUAgGnCsgAA2AQpAAApQqYAAMAGDQCAbUA0AACIhkEAACVDsgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(1729,SpreadExplorationTopology.BankCrossing,"38eece3bf0b6368139f708391670e055ce51f8877cc7df30eef16fad0ecb7676","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgERYAAAAAAACASZE0ALBByyYAgEXRFgCgDJgmAABNQCYAsGzQIgCAaVAQAJAN0DAAAAAAAAAgIdgGAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA0IZh2QAAAAAAAECiYBICAAAAAAAQhmHKBAAAAAAA2IIgWgYAAAAAAFCmIVsGAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(0,SpreadExplorationTopology.OffsetLanes,"3875a067395066da34283c3645fcd265e4a9b5bc02feeef66bf134ef3783ea7a","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMC0bQOAZdm2AQDAMmQDgG3DtgEAwDZsA4Btw7YBAMAWbAOAbcOkAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABsG7YFAGzCNgAAbBu2BQBswzYAAGQLtgUAbMM2AABsG7YFAGzDJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(0,SpreadExplorationTopology.BrokenEnclosures,"cfbf13c87936543110e90ed57d9e91a3fc2990a6b2c589bf498d58639fa76b31","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMC0LQDbtgXAtgHAEiUA27AFgIIBwBYtANowBcCGAcAWLADDsAXAhgHAFi0AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADYhm0AgG3CtgAAWIZtAIBtw7YAANCGbQCAbcO2AADYhm0AgG3DpAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(0,SpreadExplorationTopology.BankCrossing,"59ccaa52053213cc38cfb0a5d1853edc9be11865952f340c192fb35a6d76c496","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIBt2zIAAAAAAACAadE2ALBtmyIAgG3RNgAwbdg2AIBN0BIAsG3INgCALVE2ALBt2DYAAAAAAACwbdg2AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA2LZh2wYAAAAAANi2YdsGAAAAAADItmHTBgAAAAAA2LYh2wYAAAAAAMi2bNsGAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(1,SpreadExplorationTopology.OffsetLanes,"3da67095b65f5a49f904dbb14bc4306c81473f9a0717ed2d3bed141458bd055b","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMCyaQAAYcKGAABANmgAgGWCFAAAgCZoAIAkg5IAAMAGYAOAbcEEAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABEALAEAGTBNgAADAAmAABAwzIAAAAAtgUAKEM0AAAAEKYBAAzCFAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(1,SpreadExplorationTopology.BrokenEnclosures,"6440d83506b16af95e7453a2439152e36e2b660208c603d8e7adbfb3573012b7","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMCyKQBZsgCAEgFAFikA0KAEQIYAgAYpAJkwBYCGAcAGIADboAUAhACABCUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADQAGgAgGTBtgAACABlAIBBw7IAAAAALQCAKUM0AAAAhGUAAA3ClAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(1,SpreadExplorationTopology.BankCrossing,"108fe1ed340c7f07940df80ff8a3f97de734524257b82fa6b69e2e49d0996dd3","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIBo2yAAAAAAAAAAJcEQAKAsWRYAgCVRJAAwLZg2AIAM0AYAoGUIFACAZAEwAJBk2BYAAAAAAACwbJASAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAULJgiQYAAAAAAJAmYJEAAAAAAABYtiHSAgAAAAAA0KZhQAYAAAAAAFiyaFkCAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(2,SpreadExplorationTopology.OffsetLanes,"d02171ea7bb58f62814b26f046e4687accdfbb033739d7ed39c546272c383801","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMC2bQKAbdO2AQDANmwDgG3DpgAAQDZsA4BNw7YBAEA2aAKAbcO2AQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABkG7YFAGzDNgAATBu2BQBswzYAAGgbtgUAbIM2AABsG7YFAEzDNgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(2,SpreadExplorationTopology.BrokenEnclosures,"94b91e023257c1d6c4dff06c66c4edfb884ab7708e391382496c559fdc48f4eb","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMC2LQDbtAXAtgHAFi0A27AFwIYBQBYtANuQBcCGAUAWKQCZsAWAhgHABg0AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADQhm0AgG3DtgAA2IZtAIBtw7YAANiGbQCAbYO2AADYhm0AAE3DtgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Baseline(2,SpreadExplorationTopology.BankCrossing,"8e4d721a0d09615ad81dca242dd3d78a88856c28c20e30841ae7c25595448662","AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIBl2zYAAAAAAACAbNE2ALBtyzYAAG3RNgCQbdg2AABt0TYAsG2QNgCAbdE2ALBt2BYAAAAAAACwbdg2AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA2LZh2wYAAAAAANi2YdsGAAAAAACYtmHZBgAAAAAA2LZhWwYAAAAAAJi2adsEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        };
        static readonly (int seed,string id,Formation form,SpreadExplorationTopology topology,string hash)[] Outside = {
            (64,"Overworld.11.8.0",Formation.FieldStrips,SpreadExplorationTopology.Legacy,"0a0d18999d450ea7baba514f3e09492517e99ea363066f2c4fc5b0d969a58ba0"),
            (64,"Overworld.10.8.0",Formation.FieldStrips,SpreadExplorationTopology.OffsetLanes,"97e891c099b3b0b8376b98b178c6e527a6288c04d12a9f07612ebdea99706d46"),
            (64,"Overworld.12.8.0",Formation.FieldStrips,SpreadExplorationTopology.BrokenEnclosures,"a916b70237312bf108b79944e0d88272a3e254edf621ddf6a124a178c894f712"),
            (64,"Overworld.11.8.0",Formation.Fallow,SpreadExplorationTopology.BankCrossing,"a8297f9a75b8b37727b4af56b34292b947cdd87decdbc0f4020e4a65a2df5caa"),
            (64,"Overworld.11.9.0",Formation.Fallow,SpreadExplorationTopology.OffsetLanes,"7d3027ee1afeec9f8eb9ed440f0d39ae46779c9c9f22d05a6e9e8ad8c634e4dd"),
            (64,"Overworld.12.11.0",Formation.Hedgerow,SpreadExplorationTopology.BrokenEnclosures,"b0c84f747045a65bb06ea99a2cfd23ac07072573206ce9fa8ee484a0057b03ca"),
            (65,"Overworld.11.8.0",Formation.FieldStrips,SpreadExplorationTopology.Legacy,"8145b8b979a4d232470c669e36f68e3472d9b07db5e749b11a50624475d5d1af"),
            (65,"Overworld.10.8.0",Formation.FieldStrips,SpreadExplorationTopology.OffsetLanes,"9a8db26fa412ecf11a8a9639e5de2209f5100204be33a2abb4b4a42c390672ef"),
            (65,"Overworld.12.8.0",Formation.FieldStrips,SpreadExplorationTopology.BrokenEnclosures,"80b70419cb35035357f5c1e9b8740f793881f90a64c3d6a260282221b08581ab"),
            (65,"Overworld.11.8.0",Formation.Fallow,SpreadExplorationTopology.BankCrossing,"2459d30af242d278d3eacc42a6786c08e2de01e1316274aa394fb33f417a58b1"),
            (65,"Overworld.11.9.0",Formation.Fallow,SpreadExplorationTopology.OffsetLanes,"3fc20285a93d119c50d2a364a93a77b2eba852feea296564ea7994e2ff4f8e48"),
            (65,"Overworld.12.11.0",Formation.Hedgerow,SpreadExplorationTopology.BrokenEnclosures,"98d67fc060c513722443e6469dfac7f0b4da1bc60e4d7a4a8f97ea86d96a4c05"),
        };
        static SpreadCompositionPlan Plan(int seed, SpreadExplorationTopology topology)
            => SpreadCompositionPlan.Create(Nella,seed,Formation.FieldStrips,topology);
        static Baseline Old(int seed,SpreadExplorationTopology topology)=>Before.Single(p=>p.Seed==seed&&p.Topology==topology);
        static IEnumerable<(int x,int y)> Cells(SpreadCompositionPlan.Parcel p)
        { for(int y=p.Y;y<p.Y+p.Height;y++)for(int x=p.X;x<p.X+p.Width;x++)yield return(x,y); }
        static bool In(int x,int y)=>x>=0&&x<Zone.Width&&y>=0&&y<Zone.Height;
        static int Distance((int x,int y) a,(int x,int y) b)=>Math.Max(Math.Abs(a.x-b.x),Math.Abs(a.y-b.y));
        static (int x,int y) At((int x,int y) p,int x,int y,int turn)=>turn==0?(p.x+x,p.y+y):(p.x-y,p.y+x);
        static string Hash(string text)
        { using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant(); }
        static string Snapshot(SpreadCompositionPlan p,bool omitRows)
        {
            var b=new StringBuilder();b.Append(p.Formation).Append('|').Append(p.Condition).Append('|').Append(p.FocalX).Append(',').Append(p.FocalY)
                .Append('|').Append(p.WestY).Append(',').Append(p.EastY).Append(',').Append(p.NorthX).Append(',').Append(p.SouthX).Append('|');
            for(int i=0;i<p.ParcelCount;i++){var r=p.GetParcel(i);b.Append(r.X).Append(',').Append(r.Y).Append(',').Append(r.Width).Append(',').Append(r.Height).Append(';');}
            b.Append('|');for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {string owner=p.ObjectAt(x,y);if(omitRows&&owner=="CropRow")owner=null;b.Append(p.GroundAt(x,y)).Append(',').Append(p.IsApproach(x,y)?'1':'0').Append(p.IsWater(x,y)?'1':'0').Append(',').Append(owner??"-").Append(';');}
            return b.ToString();
        }
        sealed class Frontage
        {
            readonly object value; readonly Type type;
            internal Frontage(object value){this.value=value;type=value.GetType();}
            int Read(string name){var f=type.GetField(name,All);Assert.NotNull(f,"Missing immutable frontage field "+name);return (int)f.GetValue(value);}
            internal int Primary=>Read("PrimaryParcelIndex");internal int X=>Read("X");internal int Y=>Read("Y");internal int Rotation=>Read("Rotation");
            internal bool Bay(int x,int y){var m=type.GetMethod("ContainsServiceBay",All);Assert.NotNull(m);return (bool)m.Invoke(value,new object[]{x,y});}
        }
        static Frontage Front(SpreadCompositionPlan p,bool required=true)
        {
            var m=typeof(SpreadCompositionPlan).GetMethod("TryGetNellaFrontage",All);Assert.NotNull(m,"The mask and complete resident stamp need one pure scoped frontage descriptor.");
            object[] args={null};bool exists=(bool)m.Invoke(p,args);Assert.AreEqual(required,exists,p.ZoneID+" "+p.Formation+" "+p.Topology);
            return exists?new Frontage(args[0]):null;
        }
        static int Span(IEnumerable<int> values){var a=values.Distinct().ToArray();return a.Length==0?0:a.Max()-a.Min()+1;}

        [TestCase(64)] [TestCase(65)] [TestCase(1729)] [TestCase(1)]
        public void RemoteFieldsKeepSubstantialDepthAndUnequalCoherentSpans(int seed)
        {
            foreach(var topology in Topologies)
            {
                var p=Plan(seed,topology);var before=Old(seed,topology);var front=Front(p);var main=p.GetParcel(front.Primary);
                var center=(main.X+main.Width/2,main.Y+main.Height/2);
                int secondary=Enumerable.Range(0,p.ParcelCount).Where(i=>i!=front.Primary)
                    .OrderByDescending(i=>{var parcel=p.GetParcel(i);return Distance(center,(parcel.X+parcel.Width/2,parcel.Y+parcel.Height/2));}).First();
                var profiles=new HashSet<string>();
                for(int i=0;i<p.ParcelCount;i++)
                {
                    if(i==front.Primary)continue;
                    var parcel=p.GetParcel(i);
                    var old=Cells(parcel).Where(c=>before.Row(c.x,c.y)).ToArray();
                    var now=Cells(parcel).Where(c=>p.ObjectAt(c.x,c.y)=="CropRow").ToArray();
                    int depth=Span(now.Select(c=>c.y)),width=now.Select(c=>c.x).Distinct().Count();
                    int retainedDepth=Math.Min(i==secondary?4:3,Span(old.Select(c=>c.y)));
                    Assert.AreEqual(retainedDepth,depth,
                        topology+" parcel "+i+": distant fields must have substantial three/four-row depth, not one-row crumbs.");
                    // Original Roll holes in the discarded depth are not width clipping.
                    int availableWidth=old.Where(c=>(parcel.Y<12?c.y-parcel.Y:parcel.Y+parcel.Height-1-c.y)<=retainedDepth)
                        .Select(c=>c.x).Distinct().Count();
                    Assert.GreaterOrEqual(width,availableWidth-(i==secondary?4:6),
                        topology+" parcel "+i+": keep a coherent span of the rows actually available in the retained depth.");
                    Assert.Greater(width,2);Assert.Less(now.Length,old.Length,"Remote fields still need bounded breaks from the old repeated rectangles.");
                    profiles.Add(width+":"+depth);
                }
                Assert.GreaterOrEqual(profiles.Count,2,"Remote remnants must not all be the same clipped mini-field.");
                Assert.AreEqual(p.Signature(),Plan(seed,topology).Signature());
            }
        }
        [Test]
        public void RemoteWidthClippingKeepsTheWholeExistingPairAtTheCutEdge()
        {
            var p=Plan(2,SpreadExplorationTopology.BrokenEnclosures);var before=Old(2,SpreadExplorationTopology.BrokenEnclosures);
            var front=Front(p);Assert.Greater(p.GetParcel(front.Primary).X,40,"Exercise left-anchored remnants beside the right-side primary.");
            Assert.True(before.Row(18,4)&&before.Row(19,4),"Both members of the original pair exist before clipping.");
            Assert.AreEqual("CropRow",p.ObjectAt(18,4));
            Assert.AreEqual("CropRow",p.ObjectAt(19,4),"Clipping the outer furrow group must not split the next original pair.");
            for(int i=0;i<p.ParcelCount;i++)
            {
                if(i==front.Primary)continue;var parcel=p.GetParcel(i);
                foreach(var c in Cells(parcel))
                    if((c.x-parcel.X)%3==1&&before.Row(c.x,c.y)&&before.Row(c.x+1,c.y))
                        Assert.AreEqual(p.ObjectAt(c.x,c.y)=="CropRow",p.ObjectAt(c.x+1,c.y)=="CropRow",
                            "Keep or remove each available original pair together: "+c.x+","+c.y);
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(64)] [TestCase(65)] [TestCase(1729)]
        public void OnlyPrimaryFurrowHolesMayGainRowsAndEveryOtherPlanCellAndPortStayExact(int seed)
        {
            foreach(var topology in Topologies)
            {
                var p=Plan(seed,topology);var before=Old(seed,topology);var front=Front(p);var primary=p.GetParcel(front.Primary);
                Assert.AreEqual(before.Hash,Hash(Snapshot(p,true)),"Ground, routes, nonrow owners and original RNG-derived metadata must remain exact.");
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                    if(p.ObjectAt(x,y)=="CropRow"&&!before.Row(x,y))
                        Assert.True(primary.Contains(x,y)&&!primary.Boundary(x,y)&&(x-primary.X)%3!=0
                            &&!front.Bay(x,y)&&!p.IsApproach(x,y)&&!p.IsLawn(x,y)&&!p.IsWater(x,y),
                            "New cut rows are permitted only in the original primary furrow grid, never its gaps/bay/routes or other parcels: "+x+","+y);
                var reached=Reach((0,p.WestY),(x,y)=>p.ObjectAt(x,y)!="Tree"&&p.ObjectAt(x,y)!="Hedge"&&!p.IsWater(x,y));
                foreach(var port in new[]{(79,p.EastY),(p.NorthX,0),(p.SouthX,24)})Assert.True(reached.Contains(port));
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                    if(p.ObjectAt(x,y)!="Tree"&&p.ObjectAt(x,y)!="Hedge"&&!p.IsWater(x,y))Assert.True(reached.Contains((x,y)));
            }
        }
        [TestCase(64)] [TestCase(65)] [TestCase(1)]
        public void SharedFrontageHasAPlanSafeCompleteStampAndPreservesDeepPrimaryFurrows(int seed)
        {
            int completedHoles=0;
            foreach(var topology in Topologies)
            {
                var p=Plan(seed,topology);var old=Old(seed,topology);string before=p.Signature();var f=Front(p);
                Assert.That(f.Primary,Is.InRange(0,p.ParcelCount-1));Assert.That(f.Rotation,Is.InRange(0,1));
                var primary=p.GetParcel(f.Primary);Assert.Less(primary.Y,12,"Use an upper field's working edge.");
                Assert.LessOrEqual(Math.Abs(f.Y-(primary.Y+primary.Height)),3,"Prefer the actual lower edge, with bounded plan-safe fallback.");
                int bay=0;for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(f.Bay(x,y))bay++;
                Assert.That(bay,Is.InRange(12,35),"A service bay must not become a blanket primary-field deletion.");
                foreach(var offset in Stamp.Select(s=>(s.x,s.y)).Concat(Gaps))
                {
                    var at=At((f.X,f.Y),offset.x,offset.y,f.Rotation);
                    Assert.True(In(at.x,at.y));Assert.False(p.IsApproach(at.x,at.y)||p.IsLawn(at.x,at.y)||p.IsWater(at.x,at.y));
                    Assert.Greater(Distance(at,(40,12)),6);Assert.True(f.Bay(at.x,at.y),"Mask bay covers the same complete planned stamp.");
                }
                var wholeGroups=new HashSet<int>();
                foreach(var group in Cells(primary).Where(c=>old.Row(c.x,c.y)).GroupBy(c=>(c.x-primary.X)/3))
                    if(group.All(c=>p.ObjectAt(c.x,c.y)=="CropRow"))wholeGroups.Add(group.Key);
                Assert.True(wholeGroups.Any(g=>wholeGroups.Contains(g+1)),"Keep two neighboring original furrow groups at their available depth, including baseline holes.");
                foreach(var c in Cells(primary))
                    if(!primary.Boundary(c.x,c.y)&&(c.x-primary.X)%3!=0&&!f.Bay(c.x,c.y)
                        &&!p.IsApproach(c.x,c.y)&&!p.IsLawn(c.x,c.y)&&!p.IsWater(c.x,c.y))
                    {
                        Assert.AreEqual("CropRow",p.ObjectAt(c.x,c.y),"Complete the principal field's existing furrows at full available depth outside its small bay.");
                        if(!old.Row(c.x,c.y))completedHoles++;
                    }
                Assert.AreEqual(before,p.Signature(),"Reading the descriptor must be pure.");
            }
            Assert.Greater(completedHoles,0,"This continuity check must exercise real prior holes, not only retained rows.");
        }
        [TestCase(64)] [TestCase(65)]
        public void LegacyOtherAddressesAndOtherFormationsKeepTheirWholePlanAndNoFrontage(int seed)
        {
            foreach(var row in Outside.Where(r=>r.seed==seed))
            {
                var p=SpreadCompositionPlan.Create(row.id,seed,row.form,row.topology);
                Assert.AreEqual(row.hash,Hash(Snapshot(p,false)),row.id+" "+row.form+" "+row.topology);
                Assert.IsNull(Front(p,false));
            }
        }
        static IEnumerable<(int x,int y)> Neighbors((int x,int y) p)
        {yield return(p.x-1,p.y);yield return(p.x+1,p.y);yield return(p.x,p.y-1);yield return(p.x,p.y+1);}
        static HashSet<(int x,int y)> Reach((int x,int y) start,Func<int,int,bool> walk)
        {
            var seen=new HashSet<(int x,int y)>();var queue=new Queue<(int x,int y)>();queue.Enqueue(start);
            while(queue.Count>0){var p=queue.Dequeue();if(!In(p.x,p.y)||!walk(p.x,p.y)||!seen.Add(p))continue;foreach(var n in Neighbors(p))queue.Enqueue(n);}return seen;
        }
        static bool Walk(Zone z,int x,int y)=>In(x,y)&&!z.GetCell(x,y).BlocksMovement()&&z.TileState.Get(x,y)?.IsEmpty!=false
            &&!z.GetCell(x,y).Occupants.Any(e=>e.HasTag("Creature")||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>());
        static Entity One(Zone z,string role)=>z.GetReadOnlyEntities().Single(e=>e.GetProperty(Role)==role);
        static (int x,int y) Origin(Zone z)=>z.GetEntityPosition(One(z,"resident"));
        static int Rotation(Zone z)
        {var at=Origin(z);var sign=z.GetEntityPosition(One(z,"sign"));if(sign==(at.x+2,at.y))return 0;Assert.AreEqual((at.x,at.y+2),sign);return 1;}
        static void AssertStamp(Zone z,int seed,string worldKey)
        {
            var owners=z.GetReadOnlyEntities().Where(e=>e.GetProperty(Role)!=null).ToArray();Assert.AreEqual(10,owners.Length);
            var origin=Origin(z);int rotation=Rotation(z);var reached=Reach((40,12),(x,y)=>Walk(z,x,y));
            foreach(var s in Stamp)
            {
                var p=At(origin,s.x,s.y,rotation);var e=owners.Single(o=>o.BlueprintName==s.bp&&o.GetProperty(Role)==s.role&&z.GetEntityPosition(o)==p);
                Assert.NotNull(e);Assert.False(z.GenReservedCells.Contains(p));Assert.Greater(Distance(p,(40,12)),6);
                Assert.GreaterOrEqual(Neighbors(p).Count(reached.Contains),2,s.role+" needs two actually reachable standing cells.");
            }
            foreach(var gap in Gaps){var p=At(origin,gap.x,gap.y,rotation);Assert.True(reached.Contains(p));Assert.True(z.GetCell(p.x,p.y).Objects.Any(e=>e.HasTag("Plantable")));}
            var claim=One(z,"resident").GetPart<LocalGatheringClaimPart>();Assert.True(claim.Configured);Assert.AreEqual(seed,claim.WorldSeed);Assert.AreEqual(worldKey,claim.WorldKey);
            Assert.AreSame(One(z,"reserve-tray"),claim.Tray);Assert.IsEmpty(claim.Permissions);Assert.AreNotSame(claim.FirstSoil,claim.SecondSoil);
            foreach(var soil in new[]{claim.FirstSoil,claim.SecondSoil})
            {Assert.NotNull(soil.GetPart<CultivatedSoilPart>());Assert.AreEqual(One(z,"resident").ID,soil.GetProperty(SpreadExplorationResidents.ReserveBedKey));Assert.AreEqual(1,z.GetEntityCell(soil).Objects.Count(e=>e.GetProperty(Role)=="reserve-crop"));}
            foreach(var e in owners.Where(e=>e.GetProperty(Role)=="crop")){var c=e.GetPart<CropPart>();Assert.Zero(c.GrowthStage);Assert.Zero(c.MoistureTicks);Assert.False(c.HarvestAtMaturity);}
            var reserve=owners.Where(e=>e.GetProperty(Role)=="reserve-crop").Select(e=>e.GetPart<CropPart>()).ToArray();
            Assert.AreEqual(1,reserve.Count(c=>c.GrowthStage==2));Assert.AreEqual(1,reserve.Count(c=>c.GrowthStage==0));
            foreach(var c in reserve){Assert.True(c.HarvestAtMaturity);Assert.AreEqual(2,c.YieldCount);Assert.AreEqual(1,c.SeedYieldCount);}
        }
        static Zone Generate(HaulingContentScope scope,int seed,out OverworldZoneManager manager)
        {
            scope.Seed(unchecked(seed^FormationSelector.StableIndex(Nella,int.MaxValue)));
            manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var z=manager.GetZone(Nella);
            Assert.NotNull(z);Assert.AreEqual(2,manager.Exploration.DispositionFor(Nella));return z;
        }
        [TestCase(64)] [TestCase(65)]
        public void ActualColdWorldCommitsTheCompleteBoundPlotAtItsSharedFieldFrontage(int seed)
        {
            using(var scope=new HaulingContentScope())
            {
                var z=Generate(scope,seed,out var manager);AssertStamp(z,seed,manager.Exploration.WorldKey);
                var p=Plan(seed,manager.Exploration.Entries.Single(e=>e.ZoneID==Nella).Topology);var f=Front(p);
                Assert.LessOrEqual(Distance(Origin(z),(f.X,f.Y)),2,"The accepted scene should not leave the worksite far from its retained field bay.");
            }
        }
        sealed class NoRandom:Random
        {public override int Next()=>throw new InvalidOperationException("Composition consumed caller RNG.");public override int Next(int max)=>Next();public override int Next(int min,int max)=>Next();public override double NextDouble()=>Next();}
        sealed class Site:IDisposable
        {
            internal readonly HaulingContentScope Scope=new HaulingContentScope();internal EntityFactory Factory=>Scope.Factory;
            internal readonly Zone Zone=new Zone(Nella);internal readonly SpreadCompositionBuilder Terrain;
            internal Entity[] Owners;internal Func<bool> Final;
            internal Site(int seed=64){Scope.Seed(seed);Terrain=new SpreadCompositionBuilder(seed){FormationOverride=Formation.FieldStrips,Topology=SpreadExplorationTopology.OffsetLanes};Assert.True(Terrain.BuildZone(Zone,Factory,new NoRandom()));}
            internal bool Place(Func<bool> authority=null)=>SpreadExplorationResidents.TryPlaceConnected(Zone,Factory,Terrain,SpreadExplorationFamily.SeedKeepersPlot,
                authority??(()=>true),out Owners,out Final,true,"0123456789abcdef0123456789abcdef");
            public void Dispose()=>Scope.Dispose();
        }
        [TestCase(false,64)] [TestCase(true,64)] [TestCase(false,1)]
        public void PreferredFrontageUsesNativeFitsAndAnOccupiedFrontageFallsBackWithoutMovingItsSource(bool block,int seed)
        {
            using(var f=new Site(seed))
            {
                var front=Front(f.Terrain.Plan);Entity obstruction=null;
                if(seed==1)Assert.Greater(front.X,40,"Exercise the actual right-side preferred frontage.");
                if(block){obstruction=f.Factory.CreateEntity("StoneWall");Assert.True(f.Zone.AddEntity(obstruction,front.X,front.Y));}
                var original=f.Zone.GetReadOnlyEntities().ToArray();Assert.True(f.Place());Assert.True(f.Final());AssertStamp(f.Zone,seed,"0123456789abcdef0123456789abcdef");
                foreach(var e in original)Assert.NotNull(f.Zone.GetEntityCell(e));
                if(block){Assert.AreSame(obstruction,f.Zone.GetCell(front.X,front.Y).Objects.Single(e=>ReferenceEquals(e,obstruction)));Assert.AreNotEqual((front.X,front.Y),Origin(f.Zone));}
                else Assert.AreEqual((front.X,front.Y),Origin(f.Zone),"With no population interference use the complete plan-safe preferred origin.");
                Assert.False(f.Place(),"Never duplicate the stamp after acceptance.");
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void AuthorityRefusalBeforeOrAfterFirstPublicationRestoresEveryOriginalOwner(bool afterPublication)
        {
            using(var f=new Site())
            {
                var original=f.Zone.GetReadOnlyEntities().ToArray();bool observed=false;
                Assert.False(f.Place(()=>{bool published=f.Zone.GetReadOnlyEntities().Any(e=>e.GetProperty(Role)!=null);observed|=published;return afterPublication&&!published;}));
                Assert.AreEqual(afterPublication,observed);CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities());Assert.IsNull(f.Owners);Assert.IsNull(f.Final);
                Assert.False(original.Any(e=>e.HasPart<CultivatedSoilPart>()),"Refused publication cannot mint prepared beds or rights.");
            }
        }
        [TestCase(0,3)] [TestCase(65,2)] [TestCase(64,1)]
        public void EachConditionRetainsItsExactFiniteGleaningBudgetAndHarvestsOnlyOnce(int seed,int budget)
        {
            using(var scope=new HaulingContentScope())
            {
                foreach(var topology in Topologies)
                {
                    var z=new Zone(Nella);var terrain=new SpreadCompositionBuilder(seed){FormationOverride=Formation.FieldStrips,Topology=topology};
                    Assert.True(terrain.BuildZone(z,scope.Factory,new NoRandom()));var rows=z.GetReadOnlyEntities().Where(e=>e.HasPart<FieldHarvestPart>()).ToArray();Assert.AreEqual(budget,rows.Length);
                    Assert.Greater(z.GetReadOnlyEntities().Count(e=>e.BlueprintName=="CropRow"),20);
                    var player=scope.Factory.CreateEntity("Player");var target=rows[0];var at=z.GetEntityPosition(target);Assert.True(z.AddEntity(player,at.x,at.y));
                    var oldFactory=HarvestablePart.Factory;try
                    {
                        HarvestablePart.Factory=scope.Factory;int before=Units(player,"Emberwheat");
                        Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(target,"Harvest"),player,z).Success);
                        Assert.True(target.GetPart<FieldHarvestPart>().Harvested);Assert.NotNull(z.GetEntityCell(target));Assert.AreEqual(before+1,Units(player,"Emberwheat"));
                        Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(target,"Harvest"),player,z).Success);Assert.AreEqual(before+1,Units(player,"Emberwheat"));
                    }finally{HarvestablePart.Factory=oldFactory;}
                }
            }
        }
        static int Units(Entity actor,string bp)=>actor.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName==bp).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        [Test]
        public void CachedAcceptedPlotKeepsExactDepletedOwnersAndPermissionWithoutReplayingTheColdPlan()
        {
            using(var scope=new HaulingContentScope())
            {
                var z=Generate(scope,64,out var manager);var keeper=One(z,"resident");var claim=keeper.GetPart<LocalGatheringClaimPart>();
                var player=scope.Factory.CreateEntity("Player");Assert.True(z.AddEntity(player,40,12));manager.SetActiveZone(z);
                // Saved-state setup, not a claim of ordinary purchase acceptance.
                claim.Permissions.Add(new ReserveAccessRecord{Player=player,State=ReserveAccessState.Granted});
                var crop=z.GetReadOnlyEntities().First(e=>e.GetProperty(Role)=="reserve-crop");Assert.True(z.RemoveEntity(crop));
                var pack=keeper.GetPart<InventoryPart>();var spent=pack.Objects[0];Assert.True(pack.RemoveObject(spent));
                var owners=z.GetReadOnlyEntities().ToArray();var positions=owners.Select(z.GetEntityPosition).ToArray();
                manager.UnloadZone(Nella);Assert.AreSame(z,manager.GetZone(Nella));CollectionAssert.AreEquivalent(owners,z.GetReadOnlyEntities());
                for(int i=0;i<owners.Length;i++)Assert.AreEqual(positions[i],z.GetEntityPosition(owners[i]));
                Assert.AreEqual(ReserveAccessState.Granted,claim.GetState(player));Assert.IsNull(z.GetEntityCell(crop));Assert.False(pack.Objects.Contains(spent));
                var rebuild=new SpreadCompositionBuilder(64){Topology=SpreadExplorationTopology.OffsetLanes};Assert.False(rebuild.BuildZone(z,scope.Factory,new NoRandom()));
                CollectionAssert.AreEquivalent(owners,z.GetReadOnlyEntities());
            }
        }
    }
}
