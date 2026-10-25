using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Cold north composition only. Native generation/command evidence,
    /// not an ordinary-input or visual acceptance run.</summary>
    public sealed class NorthWorksCompositionTests
    {
        const string North = "Overworld.11.9.0";
        static readonly SpreadExplorationTopology[] Topologies = {
            SpreadExplorationTopology.OffsetLanes, SpreadExplorationTopology.BrokenEnclosures,
            SpreadExplorationTopology.BankCrossing };
        static SpreadCompositionPlan Plan(string id, int seed, SpreadExplorationTopology topology)
            => SpreadCompositionPlan.Create(id, seed, Formation.None, topology);

        // Native Unity before-change receipts: north-composition-native-baseline.json
        // and north-composition-counterpins-before.json, under Docs/Verification/Live16x24.
        // These literals do not call or copy the proposed subtraction policy.
        sealed class Baseline
        {
            internal readonly int Seed, LongBandCells;
            internal readonly SpreadExplorationTopology Topology;
            internal readonly string NonHedgeHash;
            readonly byte[] hedges;
            internal Baseline(int seed, SpreadExplorationTopology topology, string hash, string mask, int longBandCells)
            { Seed = seed; Topology = topology; NonHedgeHash = hash; hedges = Convert.FromBase64String(mask); LongBandCells = longBandCells; }
            internal bool WasHedge(int x, int y)
            { int i = y * Zone.Width + x; return (hedges[i / 8] & (1 << (i % 8))) != 0; }
        }
        static readonly Baseline[] Before = {
            new Baseline(64, SpreadExplorationTopology.OffsetLanes, "7b31aadf4bd4a47fd862bf51d13323c93ad8f10fac68b1f03885dae7cfa90ecc", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4P/wD8AA/8AHACAAAAgAAAAABAAgAAAIAAAAAAQAAAAACAAAAAAEAAAAAAgAAAAABADgf/AAwADA/wcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP4P8A8A8Mf/AAAAAAAIAAIAAAAAAAAACAACAAAAAAAAAAgAAgCAAAAAAAAIAAIAgAAA/v//AAD+//8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 125),
            new Baseline(64, SpreadExplorationTopology.BrokenEnclosures, "d8a496b0978c1fe6c24e814a1050d020e8fedeea2ae163b010a70217de321bad", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4P9wgP8AD+D/ByAAQIAAAAggAAQgAECAAAAIIAAEAABAgAAAACAABAAAQIAAAAAgAAQgAECA//AP4AcH4B9wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPwPAAAAAADAAQAAAAABQAAAAAEAAAAAAUAAAAABAAAAAAFAAAAAAQAAAAABQAAAAAEA/P//AMD///8BAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 118),
            new Baseline(64, SpreadExplorationTopology.BankCrossing, "e5a1e68bb149995851bd59c1af240aea087ca88f3399ade8f8ed00e543c2bed6", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwP/wfwAAAAAAAEAAAEAACP///wBAAABAAAgAAIAAAAAAQAAIAACAAAAAAEAACAAAgABAAABAAAAAAAAAwP/wcAAAAAAAAAAAAAAA8P/w/wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAM8OMPAAAAAAAABAAAAAgAAAAAAAQAAAAIAAAAAAAAAAAACAAAAAAAAAAAAAgAAAAAAAQAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 79),
            new Baseline(65, SpreadExplorationTopology.OffsetLanes, "bd8b68af2a7f5e32ed08317c04f73408e4813710bdeafd3163644b3436ae59a0", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPwPD8AA/w8HAAAAAAgAAAAABAAAAAAIAAAAAAQAIAAACEAAAAAEACAAAABAAAAABADgfwAAwA/H/wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP4QDw8ADsf/AAACAAAIAAAAgAAAAgAACAAAAIAAAAAAAAgAAgCAAAAAAAAIAAIAgAAA/v//DwD+//8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 102),
            new Baseline(65, SpreadExplorationTopology.BrokenEnclosures, "61554bb29e85a1b4af15364fb1b55ee5be161b48619c058ff5d629c99640a6d2", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4P8PAPwADwD/ByAAQAAAAAggAAQgAEAAAAAIIAAEIABAgAAACCAAACAAQIAAAAggAAAgAAAAwwgP4MAH4B8PAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPyADwHAD+D/AQAEAAABAAAAAAEABAAAAQAAAAABAAAAAAFAAAAAAQAAAAABQAAAAAEA/P//AcD///8BAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 96),
            new Baseline(65, SpreadExplorationTopology.BankCrossing, "ff31df1f35f232778488a3c0949e8d8e0551f9cbe59668c2a634cf59fc1fa5bc", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwP8PfwAAAAAAAEAAAEAA8P///wBAAABAAAAAAIAAQAAAQAAIAACAAEAAAEAACAAAgABAAAAAAAgAAIAAwP8BDwAIAACAAAAAAAAACP8I/wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADwD+PwDwAAAAAABAAAAAAAAAAAAAQAAAAAAAAAAAAEAAAACAAAAAAABAAAAAgAAAAAAAQAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 85),
            new Baseline(0, SpreadExplorationTopology.OffsetLanes, "812603c83e2aba8e59f915340af08b6b0784ae0f57449a0af535c4314c6925f5", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA//DwDw/w8AACAAAAhAAAAABAAgAAAIQAAAAAQAIAAACEAAAAAEACAAAAhAAAAABAAAf/wPwP/H/wcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4f8A8A/sfwAAAAAAAIAAAAgAAAAAAACAAAAIAAAAIAAAgAAgCAAAACAAAIAAIAgAAAAPAADwDw//8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 102),
            new Baseline(0, SpreadExplorationTopology.BrokenEnclosures, "e07c695e9b2c2a56a1bc2fb0a05c9fe6fa461605647d777fdf7d8860338dbef4", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA9/gA/wDwDwByAAQAAAAAggAAAgAEAAAAAIIAAAIABAAAAACAAABCAAQAAAAAgAAAQAAECA//gP4McHAB9/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAyP8AHAf+DwAQAAAAABQAAAAAEAAAAAAUAAAAABAAQAAAFAAAAAAQAEAAABQAAAAAEAAPAAAcDw//8BAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 56),
            new Baseline(0, SpreadExplorationTopology.BankCrossing, "40849c7d9bcce6ec753c18d50a67f046dea8129e640efc8551544dca243c9d1e", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA//fwAAAAAAAEAAAAAA8A//DwBAAAAAAAAAAAAAQAAAAAAAAACAAEAAAAAAAAAAgAAAAABAAAgAAIAAAP/xfwAIAACAAAAAAAAACPAA/wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD88OP/DwAAAAAABAAAAAAAAAAAAAQAAAAAAAAAAAAEAAAACAAAAAAABAAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 78),
            new Baseline(-1, SpreadExplorationTopology.OffsetLanes, "f8689e00f150b59370d5b46f6bf08f3b89df35df1affeda7b59bf3e9357fa9e6", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4P/wDwAPAP8AACAAAAgAAAAABAAgAAAIAAAAAAQAIAAACEAAAAAAACAAAAhAAAAAAADgf/AAwP8A/wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4f/wAA/sAAAAACAAAAAAIAgAAAAgAAAAACAIAAAAIAAAgAAgAAAAACAAAIAAIAAAAAAP8PDwD+Dw8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 86),
            new Baseline(-1, SpreadExplorationTopology.BrokenEnclosures, "d4661b2cd61ae5463e3faaeb655d7d4105f4a19ecc4465cb9ec6bff9bd83c87b", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4P9wAA8PAOAAACAAQIAAAAgAAAAgAECAAAAIAAAAIABAAAAAACAABCAAQAAAAAAgAAQgAECA//gA4MAH4B9wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAyP/wDAf+AAAQAEAAAAQAAAAAEABAAAAEAAAAABAAQAAAFAAAAAAQAEAAABQAAAAAEAAP8PAcD/Dw8BAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 72),
            new Baseline(-1, SpreadExplorationTopology.BankCrossing, "db2619b395bee79aeb459a22528a0bdd63b6a1cb4cce0a659004df608488fc62", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwP/wDwAAAAAAAEAAAEAAAP8P/wBAAABAAAAAAIAAQAAAAAAAAAAAAEAAAAAAAAAAAABAAABAAAgAAIAAwP/wcAAIAACAAAAAAAAA+PDw/wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD8/+D/DwAAAAAABAAAAAgAAAAAAAQAAAAIAAAAAAAEAAAACAAAAAAABAAAAAgAAAAAAAQAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 89),
            new Baseline(2147483647, SpreadExplorationTopology.OffsetLanes, "50f8a8d168dc22e53d5ecb6bb60ea281096b7104958ed7708fbb6ad7168124ab", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4A//AMCP//8AACAAAAhAAAAABAAgAAAIQAAAAAQAAAAACEAAAAAEAAAAAAhAAAAABADgD/wPwMDADwcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAQ8A8A/gfwAAACAAAIAAIAAAAAAgAACAACAAAAAAIAAAAAAgCAAAACAAAAAAIAgAAAAP//DwAO//8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 89),
            new Baseline(2147483647, SpreadExplorationTopology.BrokenEnclosures, "fc31b8391010f750bad53135d7dde6e0df352b972f14b12e532cef13dc366aad", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA4A9/AP//D+DwByAAAIAAAAggAAQgAACAAAAIIAAEAABAAAAACCAAAAAAQAAAAAggAAAgAECA8PAAAMcA4A9/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPCA8AEAfgDwAQAEAAABAAAAAAEABAAAAQAAAAABAAQAAABAAAAAAQAEAAAAQAAAAAEAAP//AcAP//8BAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 54),
            new Baseline(2147483647, SpreadExplorationTopology.BankCrossing, "3f8a9d2f64dca1c24636c0121446f65035e9bd914b45a98f893453ad31222f23", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwA//AAAAAAAAAEAAAEAA8P///wBAAABAAAAAAIAAAAAAAAAIAAAAAAAAAAAACAAAAABAAABAAAAAAIAAwA/xfwAAAACAAAAAAAAA8PDw/wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADw8APjDwAAAAAABAAAAAgAAAAAAAQAAAAIAAAAAAAEAAAACAAAAAAABAAAAAgAAAAAAAQAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", 59),
        };
        static readonly (int seed, string id, SpreadExplorationTopology topology, string hash)[] OutsidePins = {
            (64, "Overworld.11.9.0", SpreadExplorationTopology.Legacy, "2264749cd247168a621b613a61242ff719a561fb72c0d4594031b7d0441fa387"),
            (64, "Overworld.10.9.0", SpreadExplorationTopology.Legacy, "cb362bf75f9c6019d5f1f38738bbd0ab49f56c37f2c45e27dcc136dc59a99248"),
            (64, "Overworld.12.11.0", SpreadExplorationTopology.Legacy, "ebe29edc2975a7f310e3cd9b0ca4f8083c6d67fdb475c46a26279d0314d9bcaf"),
            (64, "Overworld.10.9.0", SpreadExplorationTopology.OffsetLanes, "b09c1632eaa911edf674427cd757a0585d9ca11b1a5b12911510a7f149a363fd"),
            (64, "Overworld.12.11.0", SpreadExplorationTopology.OffsetLanes, "346e20ea1afa5b9de0e12854f62b97a6451a1f46ec455be3080052b9d28c631e"),
            (64, "Overworld.10.9.0", SpreadExplorationTopology.BrokenEnclosures, "0963db6d39848399629aba805f829e657d18dfb130853f78346b2950d5b52cd1"),
            (64, "Overworld.12.11.0", SpreadExplorationTopology.BrokenEnclosures, "6505f8efb1a666ebd0a73932f3dabe9f06a425bde873e3938537e8c08a63275e"),
            (64, "Overworld.10.9.0", SpreadExplorationTopology.BankCrossing, "891dccdf56ec3250ca52d9f8f5819d5db8b3db21c6b95343b76dca645dba9671"),
            (64, "Overworld.12.11.0", SpreadExplorationTopology.BankCrossing, "5142e31b14b5ee03eda43690fddfd702e312fe1426273321f366e09b56596315"),
            (65, "Overworld.11.9.0", SpreadExplorationTopology.Legacy, "0b3209862d89378cffc7d9a18d72c50b04b0e7742cd60a19abef5776b5fdb0a3"),
            (65, "Overworld.10.9.0", SpreadExplorationTopology.Legacy, "936d1320a48a81370b74d46d28c1d302346b8b332f6694ae5c7c898562545c0b"),
            (65, "Overworld.12.11.0", SpreadExplorationTopology.Legacy, "5b79e66adc09e0d74f8b1c061e4b49ac7a174466ce2fec9f7ca2db065ae4d31b"),
            (65, "Overworld.10.9.0", SpreadExplorationTopology.OffsetLanes, "1b5367577cc64a763ad8e161951ba4bcf85d6fb80e1ff818979704edbb7b34a1"),
            (65, "Overworld.12.11.0", SpreadExplorationTopology.OffsetLanes, "3504afe373c9cdfd8851aac3ed375313a971002444b0700a66f5eb2db7fa0669"),
            (65, "Overworld.10.9.0", SpreadExplorationTopology.BrokenEnclosures, "4e629d3cbeeda9600e89ac2f6a57a7663607a3dec7f7745db0cb4b16e791cb95"),
            (65, "Overworld.12.11.0", SpreadExplorationTopology.BrokenEnclosures, "4d9e7fad638500ae8d3d889939455ebb70641ed5a91113aa12a096f48c052300"),
            (65, "Overworld.10.9.0", SpreadExplorationTopology.BankCrossing, "1e4444d2fb0d29a2144b8f19cfd5188a27701d264f6073ad722b424b466d7947"),
            (65, "Overworld.12.11.0", SpreadExplorationTopology.BankCrossing, "66669b028ecd9bc6ea1510e96f90ec021ceba2d22a5a563d2e941609f0914b65"),
            (0, "Overworld.11.9.0", SpreadExplorationTopology.Legacy, "73446be096c0ff0b4d4ecc812363fa93710adff62dbd5c2035ed2f6b00362bda"),
            (0, "Overworld.10.9.0", SpreadExplorationTopology.Legacy, "294f73d97f7c447a8a4d2d75e566d497a3e74c62c409e7d7194bc1c9095ba495"),
            (0, "Overworld.12.11.0", SpreadExplorationTopology.Legacy, "82f6517993008444576ca1d57aa630d8ac60dfebccea15e2179b7aa26d131b11"),
            (0, "Overworld.10.9.0", SpreadExplorationTopology.OffsetLanes, "e53d2380b25835205badbbfd27e29c0ad671fd56daa3cfba7ee8fa861bdd7ec4"),
            (0, "Overworld.12.11.0", SpreadExplorationTopology.OffsetLanes, "01eb40c79c86e8d92cf5b02db4e17209ed5f8ee1bf45890cb8edc6b1782d4b61"),
            (0, "Overworld.10.9.0", SpreadExplorationTopology.BrokenEnclosures, "aedf76d01d35218387505c8d04ff7e618a60e64efaed11f87fac1265e7ffc989"),
            (0, "Overworld.12.11.0", SpreadExplorationTopology.BrokenEnclosures, "58ec75c709bd80d5ec5cb88e97e1c9a56b20a1106b8c1cbe6b494989009c9a40"),
            (0, "Overworld.10.9.0", SpreadExplorationTopology.BankCrossing, "d9296ad955a9322f29f5f53513fb2a71fdf6d8d675595565b0dd9629c2beaf75"),
            (0, "Overworld.12.11.0", SpreadExplorationTopology.BankCrossing, "1c4d91ed59855d9b60a7bf896c91950b386784b22af52014c57bbd1247726500"),
            (-1, "Overworld.11.9.0", SpreadExplorationTopology.Legacy, "a0d8e05ecb6dc9573cfb639fe1bb9071a388eab49899823261a4254908120eef"),
            (-1, "Overworld.10.9.0", SpreadExplorationTopology.Legacy, "8ffa5becf61a1f23b5dc5da75f0d7cc1bccf3a139dd49d4181a5091a92860de8"),
            (-1, "Overworld.12.11.0", SpreadExplorationTopology.Legacy, "abdfd3660b82baffdded713f909663491b393d62fc309e710787d34f6ca01e9a"),
            (-1, "Overworld.10.9.0", SpreadExplorationTopology.OffsetLanes, "68963cd048e0893f406bed1c02551baf08a08a50c9057bd8909ff50e4acf3b59"),
            (-1, "Overworld.12.11.0", SpreadExplorationTopology.OffsetLanes, "2ea475c9cbbc6655c964f3e01b575b481518971a0ad585d362b8b1699d70d87c"),
            (-1, "Overworld.10.9.0", SpreadExplorationTopology.BrokenEnclosures, "d9053e0dab7113198b746d25b288dccae1f18d3cbfadb11c1c3d27636e1cc876"),
            (-1, "Overworld.12.11.0", SpreadExplorationTopology.BrokenEnclosures, "e0a83f53880d77f8004e101eedf25d6aa2ea5508e4435182681c867714b7642c"),
            (-1, "Overworld.10.9.0", SpreadExplorationTopology.BankCrossing, "5953dcb3b9325c033b30986b41282e3298b2baf38405af4dcd29a8d111772933"),
            (-1, "Overworld.12.11.0", SpreadExplorationTopology.BankCrossing, "0362bf5e90b7ac781a9acc316a2c260b8c9f277609c9abed3fe56f2f4972531e"),
            (2147483647, "Overworld.11.9.0", SpreadExplorationTopology.Legacy, "e8bd570244c0f85030c5f6332b4d14df6782f13c801aef7f724df444e9e35949"),
            (2147483647, "Overworld.10.9.0", SpreadExplorationTopology.Legacy, "21ed50f2ed0408a261a9ce8977762224f7dde179bb07449f0b1360cead2739b9"),
            (2147483647, "Overworld.12.11.0", SpreadExplorationTopology.Legacy, "19c97aae2618c5febeb458fc4979d45375f7b43751d2cad64054964a57c721a5"),
            (2147483647, "Overworld.10.9.0", SpreadExplorationTopology.OffsetLanes, "d19406e25a93a589c20059d9f8018097a96da494f11221c56b39815e22613c6e"),
            (2147483647, "Overworld.12.11.0", SpreadExplorationTopology.OffsetLanes, "5b0f776c27800d1b5905420b161f869fd7685cb2e12f642e881f4fe0ae608530"),
            (2147483647, "Overworld.10.9.0", SpreadExplorationTopology.BrokenEnclosures, "3c34ad1aed6719f141814a0d4f569647dac772245e106cccf313244c8fdf44b8"),
            (2147483647, "Overworld.12.11.0", SpreadExplorationTopology.BrokenEnclosures, "8d543964e6e0fc46f889fa65ed33b9922d745bb2945a5161e86fd12d9217960f"),
            (2147483647, "Overworld.10.9.0", SpreadExplorationTopology.BankCrossing, "0880826f86337da4f328b78f9f22977fbadc240a760d93ae97f849809ac1d0be"),
            (2147483647, "Overworld.12.11.0", SpreadExplorationTopology.BankCrossing, "3d215eea44a40ad597a7053a9788c631b43e47b74300350daae6b9e757721731"),
        };
        static Baseline Old(int seed, SpreadExplorationTopology topology) => Before.Single(b => b.Seed == seed && b.Topology == topology);
        static string Hash(string value)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant(); }
        static string NonHedgeHash(SpreadCompositionPlan p)
        {
            var text = new StringBuilder();
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
            {
                string bp = p.ObjectAt(x, y);
                text.Append(p.GroundAt(x, y)).Append('|').Append(p.IsApproach(x, y) ? '1' : '0')
                    .Append(p.IsWater(x, y) ? '1' : '0').Append('|').Append(bp == "Hedge" ? null : bp).Append('\n');
            }
            return Hash(text.ToString());
        }
        static int Count(SpreadCompositionPlan p, string bp)
        {
            int count = 0;
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) if (p.ObjectAt(x, y) == bp) count++;
            return count;
        }
        static int LongBandCells(SpreadCompositionPlan p)
        {
            int total = 0;
            for (int y = 0; y < Zone.Height; y++)
            {
                int run = 0;
                for (int x = 0; x <= Zone.Width; x++)
                    if (x < Zone.Width && p.ObjectAt(x, y) == "Hedge") run++;
                    else { if (run >= 8) total += run; run = 0; }
            }
            return total;
        }
        static bool HasNewBroadBreak(SpreadCompositionPlan p, SpreadCompositionPlan.Parcel parcel, Baseline old)
        {
            for (int side = 0; side < 4; side++)
            {
                int run = 0, length = side < 2 ? parcel.Width : parcel.Height;
                for (int i = 0; i < length; i++)
                {
                    int x = side < 2 ? parcel.X + i : side == 2 ? parcel.X : parcel.X + parcel.Width - 1;
                    int y = side < 2 ? side == 0 ? parcel.Y : parcel.Y + parcel.Height - 1 : parcel.Y + i;
                    // Natural old gaps and approach corridors must not satisfy this new composition assertion.
                    bool removed = old.WasHedge(x, y) && p.ObjectAt(x, y) == null
                        && !p.IsApproach(x, y) && !p.IsLawn(x, y) && !p.IsWater(x, y);
                    run = removed ? run + 1 : 0;
                    if (run >= 4) return true;
                }
            }
            return false;
        }
        [TestCase(64)] [TestCase(65)] [TestCase(0)] [TestCase(-1)] [TestCase(int.MaxValue)]
        public void NorthKeepsSubstantialUnequalFragmentsInsteadOfRepeatedLongRectangles(int seed)
        {
            var counts = new HashSet<int>(); var signatures = new HashSet<string>();
            foreach (var topology in Topologies)
            {
                var p = Plan(North, seed, topology); var old = Old(seed, topology);
                Assert.AreEqual(Formation.Fallow, p.Formation); Assert.AreEqual("overgrown crofts", p.Landscape);
                int count = Count(p, "Hedge");
                Assert.That(count, Is.InRange(40, 125), topology + ": retain cover, but remove the repeated full bands.");
                Assert.GreaterOrEqual(Enumerable.Range(0, p.ParcelCount).Count(i => HasNewBroadBreak(p, p.GetParcel(i), old)), 2,
                    topology + ": existing gates or random old holes are not new broad hedge breaks.");
                Assert.Less(LongBandCells(p), old.LongBandCells, topology + ": reduce actual long horizontal hedge runs.");
                Assert.AreEqual(p.Signature(), Plan(North, seed, topology).Signature(), "Repeated queries and generation must be deterministic.");
                counts.Add(count); signatures.Add(p.Signature());
            }
            Assert.GreaterOrEqual(counts.Count, 2, "Topology must retain a different amount of surviving cover.");
            Assert.AreEqual(3, signatures.Count, "Do not flatten three native topologies into one layout.");
        }
        static bool In(int x, int y) => x >= 0 && y >= 0 && x < Zone.Width && y < Zone.Height;
        static IEnumerable<(int x, int y)> Neighbors((int x, int y) p)
        { yield return (p.x - 1, p.y); yield return (p.x + 1, p.y); yield return (p.x, p.y - 1); yield return (p.x, p.y + 1); }
        static HashSet<(int x, int y)> Reach((int x, int y) start, Func<int, int, bool> walk)
        {
            var seen = new HashSet<(int x, int y)>(); var queue = new Queue<(int x, int y)>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue(); if (!In(p.x, p.y) || !walk(p.x, p.y) || !seen.Add(p)) continue;
                foreach (var next in Neighbors(p)) queue.Enqueue(next);
            }
            return seen;
        }
        [TestCase(64)] [TestCase(65)] [TestCase(0)] [TestCase(-1)] [TestCase(int.MaxValue)]
        public void OnlyOldHedgesMayDisappearAndEveryRouteAndOpenCellRemainsConnected(int seed)
        {
            var legacy = Plan(North, seed, SpreadExplorationTopology.Legacy);
            foreach (var topology in Topologies)
            {
                var p = Plan(North, seed, topology); var old = Old(seed, topology);
                Assert.AreEqual(old.NonHedgeHash, NonHedgeHash(p), topology + ": trees, shrubs, ground, water and every approach cell must remain exact.");
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                    if (p.ObjectAt(x, y) == "Hedge") Assert.True(old.WasHedge(x, y), "No new or relocated hedge at " + x + "," + y);
                Assert.AreEqual((legacy.WestY, legacy.EastY, legacy.NorthX, legacy.SouthX), (p.WestY, p.EastY, p.NorthX, p.SouthX));
                Assert.AreEqual(p.WestY, Plan("Overworld.10.9.0", seed, topology).EastY);
                Assert.AreEqual(p.EastY, Plan("Overworld.12.9.0", seed, topology).WestY);
                Assert.AreEqual(p.NorthX, Plan("Overworld.11.8.0", seed, topology).SouthX);
                Assert.AreEqual(p.SouthX, Plan("Overworld.11.10.0", seed, topology).NorthX);
                bool Open(int x, int y) => p.ObjectAt(x, y) != "Hedge" && p.ObjectAt(x, y) != "Tree" && !p.IsWater(x, y);
                var reached = Reach((0, p.WestY), Open);
                foreach (var port in new[] { (79, p.EastY), (p.NorthX, 0), (p.SouthX, 24) }) Assert.True(reached.Contains(port), topology + ": disconnected port " + port);
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                    if (Open(x, y)) Assert.True(reached.Contains((x, y)), topology + ": disconnected open cell " + x + "," + y);
            }
        }
        [TestCase(64)] [TestCase(65)] [TestCase(0)] [TestCase(-1)] [TestCase(int.MaxValue)]
        public void LegacyAndBothOutsideSitesKeepTheirExactNativeBeforeSignatures(int seed)
        {
            foreach (var pin in OutsidePins.Where(p => p.seed == seed))
            {
                string actual = Hash(Plan(pin.id, seed, pin.topology).Signature());
                Assert.AreEqual(pin.hash, actual, pin.id + " " + seed + " " + pin.topology);
                TestContext.WriteLine("UNCHANGED " + pin.id + " " + seed + " " + pin.topology + " " + actual);
            }
        }
        [TestCase("Overworld.10.9.0", 64, Formation.Fallow, "7e8af67ca3bb18afec2fcead80c61e3219882ea54021544360345e59c63a8eac")]
        [TestCase("Overworld.10.9.0", 65, Formation.Fallow, "457fcdd476007dc511394a34dfeb67bd350a3378dfc50eb878b89040031b2628")]
        [TestCase(North, 64, Formation.Hedgerow, "88f19f255397a0f5f77fd460b4ab09e18a4a4b430df2f47d19ccb5f714999a50")]
        [TestCase(North, 65, Formation.Hedgerow, "261f544aa5885a4b766a33de69f56a3276a3515097ee135f111815bc173a7e6e")]
        public void SameGrammarOutsideAndWrongGrammarInsideStayByteExact(string id, int seed, Formation formation, string before)
        {
            var p = SpreadCompositionPlan.Create(id, seed, formation, SpreadExplorationTopology.BrokenEnclosures);
            Assert.AreEqual(formation, p.Formation);
            if (formation == Formation.Fallow) Assert.AreEqual("overgrown crofts", p.Landscape);
            Assert.AreEqual(before, Hash(p.Signature()));
        }
        static Zone Generate(HaulingContentScope scope, int seed, out OverworldZoneManager manager)
        {
            scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(North, int.MaxValue)));
            manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            var z = manager.GetZone(North); Assert.NotNull(z); Assert.AreEqual(2, manager.Exploration.DispositionFor(North));
            return z;
        }
        static Entity Role(Zone z, string bp, string role) => z.GetReadOnlyEntities().Single(e => e.BlueprintName == bp && e.GetProperty("SpreadWorksite.Role") == role);
        static bool Walk(Zone z, int x, int y) => In(x, y) && !z.GetCell(x, y).BlocksMovement()
            && z.TileState.Get(x, y)?.IsEmpty != false && !z.GetCell(x, y).Occupants.Any(e => e.HasTag("Creature")
                || e.HasPart<TriggerOnStepPart>() || e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>());
        [TestCase(64)] [TestCase(65)]
        public void ActualColdNorthRetainsFiniteForageAndReachableStill(int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                var z = Generate(scope, seed, out var manager); var reach = Reach((40, 12), (x, y) => Walk(z, x, y));
                var still = Role(z, "AlchemyStill", "still");
                Assert.AreSame(still, still.GetPart<AlchemyStillPart>()?.ParentEntity);
                foreach (var owner in new[] { still, Role(z, "StoneburrPatch", "binding-forage"), Role(z, "FrostLichenPatch", "cold-forage") })
                {
                    Assert.AreSame(z.GetCell(z.GetEntityPosition(owner).x, z.GetEntityPosition(owner).y), z.GetEntityCell(owner));
                    Assert.GreaterOrEqual(Neighbors(z.GetEntityPosition(owner)).Count(reach.Contains), 1, owner.BlueprintName + ": actual safe approach from arrival.");
                    Assert.AreSame(owner, owner.GetPart<PhysicsPart>()?.ParentEntity); Assert.False(owner.GetPart<PhysicsPart>().Takeable);
                }
                foreach (var pair in new[] { (bp: "StoneburrPatch", role: "binding-forage", product: "StoneburrSeed"), (bp: "FrostLichenPatch", role: "cold-forage", product: "FrostLichen") })
                {
                    var owner = Role(z, pair.bp, pair.role); var harvest = owner.GetPart<HarvestablePart>();
                    Assert.NotNull(harvest); Assert.AreSame(owner, harvest.ParentEntity); Assert.False(harvest.Harvested);
                    Assert.AreEqual(pair.product, harvest.YieldBlueprint); Assert.AreEqual(100, harvest.YieldChance);
                    Assert.That(harvest.YieldMin, Is.GreaterThanOrEqualTo(1)); Assert.That(harvest.YieldMax, Is.GreaterThanOrEqualTo(harvest.YieldMin));
                }
                Assert.AreSame(z, manager.GetZone(North));
            }
        }
        [Test]
        public void ActualHarvestAndRemovedHedgeRemainGoneAcrossCachedReads()
        {
            using (var scope = new HaulingContentScope())
            {
                var z = Generate(scope, 64, out var manager); var source = Role(z, "StoneburrPatch", "binding-forage");
                var reach = Reach((40, 12), (x, y) => Walk(z, x, y));
                var standing = Neighbors(z.GetEntityPosition(source)).First(reach.Contains);
                var player = scope.Factory.CreateEntity("Player"); player.GetPart<InventoryPart>().MaxWeight = 500;
                Assert.True(z.AddEntity(player, standing.x, standing.y)); manager.SetActiveZone(z);
                var oldFactory = HarvestablePart.Factory;
                try
                {
                    HarvestablePart.Factory = scope.Factory;
                    Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(source, "Harvest"), player, z).Success);
                }
                finally { HarvestablePart.Factory = oldFactory; }
                Assert.True(source.GetPart<HarvestablePart>().Harvested); Assert.Null(z.GetEntityCell(source));
                Assert.True(player.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "StoneburrSeed"));
                var hedge = z.GetReadOnlyEntities().First(e => e.BlueprintName == "Hedge"); Assert.True(z.RemoveEntity(hedge));
                var owners = z.GetReadOnlyEntities().ToArray();
                var positions = owners.ToDictionary(e => e, z.GetEntityPosition);
                var goods = player.GetPart<InventoryPart>().Objects.ToArray();
                int disposition = manager.Exploration.DispositionFor(North);
                manager.UnloadZone(North);
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreSame(z, manager.GetZone(North)); CollectionAssert.AreEquivalent(owners, z.GetReadOnlyEntities());
                    foreach (var owner in owners) Assert.AreEqual(positions[owner], z.GetEntityPosition(owner));
                    CollectionAssert.AreEquivalent(goods, player.GetPart<InventoryPart>().Objects);
                    Assert.Null(z.GetEntityCell(source)); Assert.Null(z.GetEntityCell(hedge)); Assert.True(source.GetPart<HarvestablePart>().Harvested);
                    Assert.AreEqual(disposition, manager.Exploration.DispositionFor(North));
                }
            }
        }
    }
}
