using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Cold scene composition and service preservation; native image review remains separate.</summary>
    public sealed class EastMeadowCompositionTests
    {
        const string East = "Overworld.12.10.0";
        static readonly int[] Seeds = { 0, 1, 64, 65, 1729, int.MinValue, int.MaxValue, 200893250 };
        sealed class Original
        {
            internal readonly int Seed;
            internal readonly SpreadExplorationTopology Topology;
            internal readonly string FixedHash;
            internal readonly HashSet<(int x, int y)> Flowers = new HashSet<(int x, int y)>();
            internal Original(int seed, SpreadExplorationTopology topology, string fixedHash, string mask)
            {
                Seed = seed; Topology = topology; FixedHash = fixedHash;
                byte[] bits = Convert.FromBase64String(mask);
                for (int i = 0; i < 2000; i++) if ((bits[i / 8] & (1 << (i % 8))) != 0) Flowers.Add((i % 80, i / 80));
            }
        }
        // Literal original native receipts, before runtime edits, from
        // Docs/Verification/Live16x24/east-meadow-original-plans.json.
        // Base64 is only a compact 80x25 coordinate bitset, not a placement algorithm.
        static readonly Original[] Originals = {
            new Original(0, SpreadExplorationTopology.OffsetLanes, "e9698725f80e7c2515618b72b0be8aeee660dd97158af94566334c7f5146bc1b",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgB4AAAAAAAAAAKgLAAAAAAA+AABoHQAAAAAAWwAAHxwAQAUAAEsAAEVUAWAcAABPAAAf5OkCNQAALgAAAvw3IzcAAAAAAAAAwKMeAAAAAAAAAACAHwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA5AgAAAAAAAAAAPgMAAAAAAAAAgO/HAAAAAAAAAIBnQwMAAAAAAAAAa8IGAAAAAAAAAPyDBwAAAAAAAABY1wQAAAAAAAAAYD8HAAAAAAAAAAA6BAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(0, SpreadExplorationTopology.BrokenEnclosures, "f1c606f5ab6ff380ca34d4c7eefffd6fa45729a5ec2af9b6a429b49c24037222",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgB4AAAAAAAAAAKgLAAAAAAA+AABoHQAAAAAAWwAAHx8AQAAAAEMAAAVXAXAYAABHAAAf5eEOMAAABgAAAvwjNzAAAAAAAAAAwLsYAAAAAAAAAACAGAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPgAAAAAAAAAAgO/DAAAAAAAAAIBnYwMAAAAAAAAAa8IGAAAAAAAAAPyDBwAAAAAAAABY1wQAAAAAAAAAYD8HAAAAAAAAAAA6BAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(0, SpreadExplorationTopology.BankCrossing, "b8e65092eb8a1c10f38939b673b7a5af17c23c0ae49c3fed6ad7bb2dd908a125",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgB4AAAAAAAAAAKgLAAAAAAA+AABoHQAAAAAAWwAAHxEAQAUAAEsAAEVRAUAcAABPAACf4ekGNQAALgAAAvA3BzcAAAAAAAAAwIMeAAAAAAAAAACAHwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA5AAAAAAAAAAAAPhEAAAAAAAAAgO/RAAAAAAAAAIBnYQMAAAAAAAAAa9AGAAAAAAAAAPyRBwAAAAAAAABY0QQAAAAAAAAAADAHAAAAAAAAAAAwBAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(1, SpreadExplorationTopology.OffsetLanes, "64f4c865fe0aa09b089415ccde16b3757d30b5e1e85d9b758eccc1a75f3e61ae",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgB0AAAAAAAAAAFgHAAAAAAA+AACUDgAAAAAAZwAALywAoAYAAEcAAAqoAaAcAABPAABv2NYBOgAAHAAARvw7IzsAAAAAAADc62MtAAAAAAAARL7iDwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA2AQAAAAAAAAAAPQcAAAAAAAAAgN/HAAAAAAAAAICbhwMAAAAAAAAAl8EFAAAAAAAAAP5HCwAAAAAAAACk6wgAAAAAAAAAgD8LAAAAAAAAAAA1AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(1, SpreadExplorationTopology.BrokenEnclosures, "84f2e9749c76d1728dc9043caa300899c740e4224d30b963897e084594730e8c",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgB0AAAAAAAAAAFgHAAAAAAA+AACUDgAAAAAAZwAADy8AoAAAAEcAAAqrAbAYAABHAAAP2sINOAAABAAABv8jOzgAAAAAAADc43coAAAAAAAARoD+CAAAAAAAAAAAJwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPQAAAAAAAAAAgN/jAAAAAAAAAICbgwMAAAAAAAAAl+EFAAAAAAAAAP5jCwAAAAAAAACk6wgAAAAAAAAAgD8LAAAAAAAAAAA1AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(1, SpreadExplorationTopology.BankCrossing, "bcc9c788cf78da4b3c8a09e91cb4f695c8a2ba53ec71a9cb1eb416d8d84fb33e",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgB0AAAAAAAAAAFgHAAAAAAA+AACUDgAAAAAAZwAALyEAgAYAAEcAAIqhAYAcAABPAABv0NYFOgAAHAAAxvE7AzsAAAAAAIDQ60ctAAAAAAAAQL7GDwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA2AQAAAAAAAAAAPQEAAAAAAAAAgN/hAAAAAAAAAICbkQMAAAAAAAAAl+EFAAAAAAAAAP5hCwAAAAAAAACk4QgAAAAAAAAAADALAAAAAAAAAAAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(64, SpreadExplorationTopology.OffsetLanes, "4de0c65d5815024f59799fe4fb16cde2571658bcfc72f293d3e9a8517e39fd11",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEA2AAAAAAA2AADYXgAAAAAAZQAAdLQAgA0AAH8AAHR0BmBtAABsAABc3NpjPgAANgAAAABwY74AAAAAAAAAAADEAAAAAAAAAAAAXAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAID/AAAAAAAAAADAfgEAAAAAAAAAoNVHAAAAAAAAAMC/xAAAAAAAAADA98cBAAAAAAAAAOXCAwAAAAAAAACqHQMAAAAAAAAA8NkBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(64, SpreadExplorationTopology.BrokenEnclosures, "278dd38d203fc2b03cdea6cc48520e9f087e18ec05c06de3d4514c1752b44751",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAAAAAAAEA2AAAAAAA2AADYXgAAAAAAZQAAFLYAgAgAAEcAABR3AnBoAABEAAAc38JvOAAABgAAAD1ie7gAAAAAAAA/YzHAAAAAAAAAfsA7WAAAAAAAAAAAHBgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADAfgAAAAAAAAAAoNVjAAAAAAAAAMC/4AAAAAAAAADA98MBAAAAAAAAAOXiAwAAAAAAAACqHQMAAAAAAAAA8NkBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(64, SpreadExplorationTopology.BankCrossing, "b4e3598518e85c082e9e5f533542916fa6572810da774a46c265431b540c66bc",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEA2AAAAAAA2AADYXgAAAAAAZQAA9LAAgA0AAH8AAPRxBkBtAABsAADc0dpHPgAANgAAAABwQ74AAAAAAAAAAADEAAAAAAAAAAAAXAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAID/AAAAAAAAAADAfgEAAAAAAAAAoNVhAAAAAAAAAMC/8AAAAAAAAADA99EBAAAAAAAAAOXgAwAAAAAAAACqEQMAAAAAAAAAANABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(65, SpreadExplorationTopology.OffsetLanes, "b491a7a40742840dc4eeb00d6a1bc8f13adaac6b21fe944d230ccad88a585628",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAQAIAAAAAAAAAAKgBAAAAAAA6AADmDQAAAAAAGgCAeRgAwAIAAH8AgHu4AKAOAABcAIBu7OWDHQAAOgAABzyxox0AAAAAAAAAv6IKAAAAAAAAAMCjDgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAGoAAAAAAAAAAAB/xAEAAAAAAAAA+8cGAAAAAAAAANrFHwAAAAAAAABQLgcAAAAAAAAA4OYeAAAAAAAAAAD9HgAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(65, SpreadExplorationTopology.BrokenEnclosures, "4c6c7993a335ab3353ac2fe388e163aac3769d774a3156046bf4f45618baff7b",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAQAIAAAAAAAAAAKgBAAAAAAA6AADmDQAAAAAAGgCAGRkA0AAAAEcAgBu7ALgIAABEAIAO7+GfGAAAAgAABz6htxgAAAAAAAA/o7IIAAAAAAAAvcG3CAAAAAAAAACA7wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB/AAAAAAAAAAAA++MGAAAAAAAAANrBHwAAAAAAAABQLgcAAAAAAAAA4OYeAAAAAAAAAAD9HgAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(65, SpreadExplorationTopology.BankCrossing, "669d2a2889f9f8f1cb0a9f5653066fa04b253b67d64cc73e24b7c0e01836dd25",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAQAIAAAAAAAAAAKgBAAAAAAA6AADmDQAAAAAAGgCA+REAwAIAAH8AgPuxAIAOAABcAIDu4eWHHQAAOgAABzCxhx0AAAAAAAAAv4IKAAAAAAAAAMCHDgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAGoAAAAAAAAAAAB/8AEAAAAAAAAA++EGAAAAAAAAANrRHwAAAAAAAABQIAcAAAAAAAAAAOAeAAAAAAAAAADwHgAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(1729, SpreadExplorationTopology.OffsetLanes, "ae0f4e73859814a457866811385640bd5f85ae9d956c7a9a26d9058de938310d",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAEoAAAAAAAAAAKAMAAAAAAA8AADIfQAAAAAAPwAAFEgAAB8AAH4AADjMB+B/AAA7AABs7C/C/wAAPgAAAADA4o8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMAOAAAAAAAAAADgXgEAAAAAAAAAwKtFAAAAAAAAAMAiwwAAAAAAAADAtccBAAAAAAAAgHPFAwAAAAAAAAD07gIAAAAAAAAA4KYBAAAAAAAAAIClAQAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(1729, SpreadExplorationTopology.BrokenEnclosures, "ee514f49f0b34b5102b8611a33f00f738f92bb34236bbb9aba4c08f1d958d68f",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAEoAAAAAAAAAAKAMAAAAAAA8AADIfQAAAAAAPwAAFEsAABgAAEYAABjMA/B4AAADAAAM7iPG+AAABgAAAK7D/ogAAAAAAAB2QX2IAAAAAAAA3sFxAAAAAAAAAAAAuAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADgXgAAAAAAAAAAwKthAAAAAAAAAMAiwwAAAAAAAADAteMBAAAAAAAAgHPhAwAAAAAAAAD07gIAAAAAAAAA4KYBAAAAAAAAAIClAQAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(1729, SpreadExplorationTopology.BankCrossing, "52f3ab779716dc0fec11f8ea6d88a82d9cea9f3e2748ab27c4169cc5879f6a31",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAEoAAAAAAAAAAKAMAAAAAAA8AADIfQAAAAAAPwAAFEEAAB8AAH4AALjAB8B/AAA7AABs4C/G/wAAPgAAAADAxo8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMAOAAAAAAAAAADgXgEAAAAAAAAAwKtxAAAAAAAAAMAiwQAAAAAAAADAteEBAAAAAAAAgHPxAwAAAAAAAAD04AIAAAAAAAAAAKABAAAAAAAAAACgAQAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(-2147483648, SpreadExplorationTopology.OffsetLanes, "522a12caccb8e91459ebbaa9c65682b34f5715bff0f9f6b32c95c7fdcf4c00f3",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgB4AAAAAAAAAAMgDAAAAAAAGAABUCwAAAAAAHwAAHjQA4AcAAH8AAHR0AOAeAAAvAAB+RKcjPgAAKgAAAvzS4iUAAAAAAADoq6I/AAAAAAAAPP3jGgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADdAQAAAAAAAACATwUAAAAAAAAAgOaHAAAAAAAAAIB9RgMAAAAAAAAA2YcDAAAAAAAAANZHDwAAAAAAAACMsw8AAAAAAAAAYNcEAAAAAAAAAADfBQAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(-2147483648, SpreadExplorationTopology.BrokenEnclosures, "6c1aa226d7d5145e7e9be47c50567244e3830453fe8ee5ac1e5b1801fe2de17f",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgB4AAAAAAAAAAMgDAAAAAAAGAABUCwAAAAAAHwAAHjUA4AAAAEcAABR1APgYAAAHAAAeRaM/OAAAAgAAAvzC/iAAAAAAAADro7o4AAAAAAAAP8H3GAAAAAAAAAAAlwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACATwAAAAAAAAAAgOajAAAAAAAAAIB9YgMAAAAAAAAA2aMDAAAAAAAAANZjDwAAAAAAAACMsw8AAAAAAAAAYNcEAAAAAAAAAADfBQAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(-2147483648, SpreadExplorationTopology.BankCrossing, "d1db1d9aae3bfbb4f0b1985b8f2d36177f4bbf130d1af561871bcea1c72710aa",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgB4AAAAAAAAAAMgDAAAAAAAGAABUCwAAAAAAHwAAnjEAwAcAAH8AAHRxAMAeAAAvAAD+QacHPgAAKgAAAvDSxiUAAAAAAIDhq4I/AAAAAAAAMf3HGgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADdAQAAAAAAAACATwEAAAAAAAAAgOaxAAAAAAAAAIB9cAMAAAAAAAAA2bEDAAAAAAAAANZhDwAAAAAAAACMsQ8AAAAAAAAAANAEAAAAAAAAAADQBQAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(2147483647, SpreadExplorationTopology.OffsetLanes, "958d1feb51017a7f107e0049edc83904cfdf9cccc18b2a4de80d967d3b6f798c",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgAkAAAAAAAAAAOQHAAAAAAAAAADPDQAAAAAANACAXBwAYAEAAHcAgCn0AOICAAB6AABhbN7jEwAAdQAAQvyXYxcAAD4AAAAw7+EfAAAAAAAA9O2gCgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACEAQAAAAAAAAAAyAMAAAAAAAAAAGYCAAAAAAAAAADfRgMAAAAAAAAA/0ILAAAAAAAAAH7HHwAAAAAAAADg/x0AAAAAAAAAIHQSAAAAAAAAAABlHwAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(2147483647, SpreadExplorationTopology.BrokenEnclosures, "92de439802f6f00dd525c78173306e9c16b8f369825339aa3fd9137a499211e1",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgAkAAAAAAAAAAOQHAAAAAAAAAADPDQAAAAAANACAHB8AcAAAAEcAgAn3APYAAABCAAABb8LvEAAARQAAAv6DZxAAAAYAAAAx4/0YAAAAAAAA98G8CAAAAAAAAACAjwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAASAAAAAAAAAAAAGYiAAAAAAAAAADfQgMAAAAAAAAA/2ILAAAAAAAAAH7DHwAAAAAAAADg/x0AAAAAAAAAIHQSAAAAAAAAAABlHwAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(2147483647, SpreadExplorationTopology.BankCrossing, "2543d201f45792c45c9fecafc72db0d061719016160d0825980ddedfed21d941",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAgAkAAAAAAAAAAOQHAAAAAAAAAADPDQAAAAAANACA3BEAQAEAAHcAgKnxAMYCAAB6AADhYd7HEwAAdQAAwvCXRxcAAD4AAIAx78UfAAAAAACA8e2ECgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACEAQAAAAAAAAAAyBEAAAAAAAAAAGYwAAAAAAAAAADfUAMAAAAAAAAA/3ALAAAAAAAAAH7RHwAAAAAAAADg8R0AAAAAAAAAAHASAAAAAAAAAABgHwAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(200893250, SpreadExplorationTopology.OffsetLanes, "d07a198d0cbbd1729a82d27948942fffff103beb071056346689bb4bd739fb7f",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAABgAAAAAAAAAAEB+AAAAAAAAAABwqQAAAAAAPAAAMMwBAD8AAGMAAHj0D0CjAAA1AABA6H4iLQAAfgAAQPz/4+EBAA4AAAB87qH4AQAAAAAA/PPjaAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAGD6AQAAAAAAAADgJQUAAAAAAAAAgHYGAAAAAAAAAKBbhgAAAAAAAADgt4UAAAAAAAAAgNTFAQAAAAAAAABoPQEAAAAAAAAAeO0BAAAAAAAAAAAsAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(200893250, SpreadExplorationTopology.BrokenEnclosures, "608740bda17e5e2443c132cd3003df4307b30d6f5b035a186ad00304795c6a0a",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAABgAAAAAAAAAAEB+AAAAAAAAAABwqQAAAAAAPAAAEM0BADgAAEMAABj2A0CgAAAFAAAA62I2KAAARgAAAP7j++ABAAYAAAB/4r34AQAAAAAA/MH/aAAAAAAAAAAAeCgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADgJQAAAAAAAAAAgHYiAAAAAAAAALBbggAAAAAAAADgt6EAAAAAAAAAgNThAQAAAAAAAABoPQEAAAAAAAAAeO0BAAAAAAAAAAAsAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
            new Original(200893250, SpreadExplorationTopology.BankCrossing, "de66f41592e321328cfbab5864115dfd207fb9d4b3f549af6df7fc960720c82b",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAABgAAAAAAAAAAEB+AAAAAAAAAABwqQAAAAAAPAAAsMEBAD8AAGMAAHjwD0CjAAA1AADA4X4GLQAAfgAAwPD/w+EBAA4AAIBx7oX4AQAAAAAA8PPHaAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAGD6AQAAAAAAAADgJQEAAAAAAAAAgHYwAAAAAAAAALBbkAAAAAAAAADgt7EAAAAAAAAAgNTxAQAAAAAAAABoMQEAAAAAAAAAAOABAAAAAAAAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        };
        static SpreadCompositionPlan Plan(Original b) => SpreadCompositionPlan.Create(East, b.Seed, Formation.None, b.Topology);
        static IEnumerable<(int x, int y)> Cells()
        { for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) yield return (x, y); }
        static IEnumerable<(int x, int y)> Neighbors((int x, int y) p)
        { yield return (p.x - 1, p.y); yield return (p.x + 1, p.y); yield return (p.x, p.y - 1); yield return (p.x, p.y + 1); }
        static string Hash(string value)
        {
            using (var h = SHA256.Create())
                return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
        static string FixedSignature(SpreadCompositionPlan p)
        {
            var text = new StringBuilder();
            foreach (var c in Cells()) {
                string bp = p.ObjectAt(c.x, c.y);
                text.Append(c.x).Append(',').Append(c.y).Append(':').Append(p.GroundAt(c.x, c.y)).Append('|')
                    .Append(p.IsApproach(c.x, c.y) ? '1' : '0').Append(p.IsLawn(c.x, c.y) ? '1' : '0')
                    .Append(p.IsWater(c.x, c.y) ? '1' : '0').Append('|').Append(bp == "CharmFlowers" ? "" : bp).Append('\n');
            }
            return text.ToString();
        }
        [TestCaseSource(nameof(Seeds))]
        public void OnlyOriginalFlowersMayBeSubtractedWhileGroundTreesBushesAndRoutesStayExact(int seed)
        {
            foreach (var b in Originals.Where(row => row.Seed == seed)) {
                var p = Plan(b);
                Assert.AreEqual(b.FixedHash, Hash(FixedSignature(p)), b.Topology + ": no nonflower geometry may shift.");
                var flowers = Cells().Where(c => p.ObjectAt(c.x, c.y) == "CharmFlowers").ToArray();
                Assert.True(flowers.All(b.Flowers.Contains), "A reduction must not move owners into new grass.");
                Assert.AreEqual(p.Signature(), Plan(b).Signature());
            }
        }
        [TestCaseSource(nameof(Seeds))]
        public void UnequalPopulatedDriftsGainSeveralConnectedClearingsRatherThanOnlySingleTileHoles(int seed)
        {
            foreach (var b in Originals.Where(row => row.Seed == seed)) {
                var p = Plan(b);
                var flowers = new HashSet<(int x, int y)>(Cells().Where(c => p.ObjectAt(c.x, c.y) == "CharmFlowers"));
                Assert.That((double)flowers.Count / b.Flowers.Count, Is.InRange(.55, .70),
                    b.Topology + ": preserve substantial meadow coverage while removing 30–45% of repeated bright marks.");
                Assert.GreaterOrEqual(flowers.Count(c => c.x <= 48 && c.y <= 11), 40, "Keep the large upper body.");
                Assert.GreaterOrEqual(flowers.Count(c => c.x >= 42 && c.y >= 14), 25, "Keep the distinct lower body.");
                Assert.GreaterOrEqual(flowers.Count(c => c.x >= 62 && c.y <= 9), 5, "Keep the small eastern echo.");
                Assert.GreaterOrEqual(flowers.Count(c => Neighbors(c).Any(flowers.Contains)) * 4, flowers.Count * 3,
                    "Most retained flowers must still belong to adjacent groups, not uniform sparse confetti.");
                // Count connected removed owners, not preexisting empty cells or mandatory routes.
                var remaining = new HashSet<(int x, int y)>(b.Flowers.Where(c => !flowers.Contains(c)));
                int groups = 0;
                while (remaining.Count > 0) {
                    var start = remaining.First(); var pending = new Queue<(int x, int y)>(); pending.Enqueue(start);
                    int size = 0; bool reachedInterior = false;
                    while (pending.Count > 0) {
                        var c = pending.Dequeue(); if (!remaining.Remove(c)) continue; size++;
                        reachedInterior |= Neighbors(c).Count(b.Flowers.Contains) >= 3;
                        foreach (var n in Neighbors(c)) if (remaining.Contains(n)) pending.Enqueue(n);
                    }
                    if (size >= 3 && reachedInterior) groups++;
                }
                Assert.GreaterOrEqual(groups, 3, "Several multi-cell openings must reach inside the old mass; one-cell holes or edge-only erosion do not qualify.");
                int longest = 0;
                for (int y = 0; y < Zone.Height; y++) {
                    int run = 0;
                    for (int x = 0; x < Zone.Width; x++) { run = flowers.Contains((x, y)) ? run + 1 : 0; longest = Math.Max(longest, run); }
                }
                Assert.LessOrEqual(longest, 8, "Bright flowers must not retain the old long filled horizontal ranks.");
            }
        }
        static bool OriginallyFlower(SpreadCompositionPlan p, int x, int y)
        {
            var query = typeof(SpreadCompositionPlan).GetMethod("HadOriginalOpeningFlower", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(query, "The cold pad needs the original flower exclusion after thinning; it cannot infer it from newly empty ground.");
            return (bool)query.Invoke(p, new object[] { x, y });
        }
        [Test]
        public void OriginalFlowerSubstrateQueryIsExactAndDoesNotClaimOtherPlansOrInvalidCells()
        {
            foreach (var b in Originals) {
                var p = Plan(b);
                foreach (var c in Cells()) Assert.AreEqual(b.Flowers.Contains(c), OriginallyFlower(p, c.x, c.y));
                Assert.False(OriginallyFlower(p, -1, 3)); Assert.False(OriginallyFlower(p, 80, 3));
                Assert.False(OriginallyFlower(p, 3, -1)); Assert.False(OriginallyFlower(p, 3, 25));
            }
            foreach (var p in new[] {
                SpreadCompositionPlan.Create(East, 64, Formation.FlowerMeadow, SpreadExplorationTopology.Legacy),
                SpreadCompositionPlan.Create(East, 64, Formation.OldRoad, SpreadExplorationTopology.BrokenEnclosures),
                SpreadCompositionPlan.Create("Overworld.11.11.0", 64, Formation.FlowerMeadow, SpreadExplorationTopology.BrokenEnclosures) })
                Assert.False(Cells().Any(c => OriginallyFlower(p, c.x, c.y)), "Exact address, formation and non-Legacy scope all matter.");
        }
        [Test]
        public void PreviouslyFlowerOccupiedPadCellsStayUnpavedButPlainGrassStillSupportsTheSameForge()
        {
            using (var scope = new HaulingContentScope()) {
                scope.Seed(64); var zone = new Zone(East);
                var terrain = new SpreadCompositionBuilder(64) { Topology = SpreadExplorationTopology.BrokenEnclosures };
                Assert.True(terrain.BuildZone(zone, scope.Factory, new Random(64)));
                var original = Originals.Single(b => b.Seed == 64 && b.Topology == SpreadExplorationTopology.BrokenEnclosures);
                // A controlled cold substrate: only above-ground dressing is cleared before
                // population capture. The original plan's exclusions must still govern the pad.
                foreach (var e in zone.GetReadOnlyEntities().Where(e => e.BlueprintName != "Grass").ToArray()) zone.RemoveEntity(e);
                zone.GenReservedCells.Clear();
                var population = new PopulationBuilder(new PopulationTable {
                    Name = "SpreadTier1", Entries = new List<PopulationEntry> {
                        new PopulationEntry { BlueprintName = "Viper", EncounterGroup = "SpreadTier1Encounter", MinCount = 1, MaxCount = 1 }
                    }
                }) { CaptureSourceReceipts = true, HabitatFilter = (bp, cell) => cell.X == 60 && cell.Y == 5 };
                Assert.True(population.BuildZone(zone, scope.Factory, new Random(17)));
                Assert.True(population.SourceReceipt.IsCurrent);
                var sourceActor = population.SourceReceipt.Owners.Single();
                Assert.AreEqual((60,5), zone.GetEntityPosition(sourceActor));
                var pad = new[] { (20,10), (21,10), (18,11), (19,11), (20,11), (21,11), (17,12), (18,12), (19,12), (18,13), (19,13), (20,13) };
                var allowed = new HashSet<(int x, int y)>(pad.Concat(new[] { (20,12), (22,13), (18,10), (19,10), (22,10), (22,11), (19,13), (60,5) }));
                foreach (var c in Cells()) if (!allowed.Contains(c)) zone.GenReservedCells.Add(c);
                Assert.True(SpreadExplorationWorksites.TryPlace(zone, scope.Factory, terrain, population, null,
                    SpreadExplorationFamily.TemperingShelter, () => true, out var owners, out var final));
                Assert.True(final());
                Assert.AreEqual((20,12), zone.GetEntityPosition(owners.Single(e => e.BlueprintName == "TinkersForge")));
                var actual = owners.Where(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) == "yard-ground").Select(zone.GetEntityPosition).ToArray();
                var expected = pad.Where(c => !original.Flowers.Contains(c)).ToArray();
                Assert.AreEqual(10, expected.Length, "Original two occupied cells plus ten plain-grass controls are nonvacuous.");
                CollectionAssert.AreEquivalent(expected, actual, "No new paving should appear merely because flower marks were removed.");
                Assert.False(population.SourceReceipt.IsCurrent, "The successful transaction consumes its one-use source receipt.");
                Assert.AreSame(sourceActor, zone.GetReadOnlyEntities().Single(e => e.HasTag("Creature")));
                Assert.AreEqual("Viper", sourceActor.BlueprintName); Assert.AreEqual((60,5), zone.GetEntityPosition(sourceActor));
            }
        }

        // Exact pre-change native full-generation receipts, including all nonflower
        // ground/actors/props, finite stock and loadouts. The raw observer accidentally
        // included allocated body-slot IDs; normalized pins preserve equipment values
        // and duplicate multiplicity, not anatomical slot identity across cold worlds.
        static readonly HashSet<string> StockParts = new HashSet<string> {
            "HarvestablePart", "FieldHarvestPart", "CropPart", "WaterskinPart", "FuelPart",
            "AmmoPart", "MissileWeaponPart", "TacticalSupplyPart", "FieldMedicinePart", "ContainerPart", "TraderPart"
        };
        static object Stock(Entity owner)
        {
            var parts = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach (var part in owner.Parts) {
                if (!StockParts.Contains(part.GetType().Name)) continue;
                var fields = new SortedDictionary<string, object>(StringComparer.Ordinal);
                foreach (var field in part.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) {
                    var type = field.FieldType;
                    if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)) fields[field.Name] = field.GetValue(part);
                }
                parts[part.GetType().Name] = fields;
            }
            return parts;
        }
        static string Item(Entity owner) => DiagnosticAdapterProbe.Serialize(new {
            blueprint = owner.BlueprintName, count = owner.GetPart<StackerPart>()?.StackCount ?? 1, stock = Stock(owner)
        });
        static string FullTuple(Zone zone, Entity owner)
        {
            var at = zone.GetEntityPosition(owner); var pack = owner.GetPart<InventoryPart>(); var container = owner.GetPart<ContainerPart>();
            return DiagnosticAdapterProbe.Serialize(new {
                blueprint = owner.BlueprintName, role = owner.GetProperty(SpreadExplorationWorksites.RoleKey), x = at.x, y = at.y,
                hp = owner.GetStatValue("Hitpoints"), stock = Stock(owner),
                gear = DensityLootTestScope.Gear(owner).Select(Item).OrderBy(s => s, StringComparer.Ordinal).ToArray(),
                inventory = pack == null ? new string[0] : pack.Objects.Select(Item).OrderBy(s => s, StringComparer.Ordinal).ToArray(),
                equipped = pack == null ? new string[0] : pack.EquippedItems
                    .Select(kv => (int.TryParse(kv.Key, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out _) ? "body" : kv.Key) + "=" + Item(kv.Value))
                    .OrderBy(s => s, StringComparer.Ordinal).ToArray(),
                contents = container == null ? new string[0] : container.Contents.Select(Item).OrderBy(s => s, StringComparer.Ordinal).ToArray()
            });
        }
        static string Coordinate((int x, int y) p) => p.x.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "," + p.y.ToString(System.Globalization.CultureInfo.InvariantCulture);
        // Derived only from original raw receipts; see east-meadow-normalized-generated-comparison.json.
        [TestCase(64, 2126, "833c063fca2cba2714e8c1b2e6b61a761e8de5b728fe50ab626308479bab399c",
            "5,18;5,19;5,20;6,18;6,19;6,20;7,17;7,18;7,20;8,17;8,18", 736, "b2fd4850db1ab453e49051af7e29699bf0a27766ad9b6d899e69c259bee79c56")]
        [TestCase(65, 2077, "3b4c30e64c5f2351582fec567302a44781e2bfcb6e41b93258cd75d8d78f9f98",
            "62,4;63,3;63,4;63,5;64,3;64,4;65,2;66,2;66,3", 564, "7d257d2f3158d97f0948a117366ecda17177a41452a2843f8d46b8315dee8bdd")]
        [TestCase(200893250, 2150, "43411b9ecfb0ffe8f8f6351267809d8ec4a171a00180d048a76e1762c896787f",
            "18,16;19,15;19,16;19,17;20,15;20,16;20,17;21,14;21,15;21,17;22,14;22,15", 663, "5f977b57513c87a43707e36f8437408184f3fb59583f7b04dbde38970214377c")]
        public void FullGeneratedNonflowerOwnersStockCourtAndReservationsMatchOriginalReceipt(
            int seed, int nonFlowerCount, string tupleHash, string roads, int reservedCount, string reservedHash)
        {
            // Repeat in the same domain: body-slot allocation continues between scopes.
            // A pristine first-run hash alone would hide the original observer mistake.
            for (int pass = 0; pass < 2; pass++)
            using (var scope = new HaulingContentScope()) {
                scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(East, int.MaxValue)));
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true); var zone = manager.GetZone(East);
                Assert.NotNull(zone); Assert.AreEqual(2, manager.Exploration.DispositionFor(East), "Preserve actual source acceptance, never force an optional site.");
                var tuples = zone.GetReadOnlyEntities().Where(e => e.BlueprintName != "CharmFlowers")
                    .Select(e => FullTuple(zone, e)).OrderBy(s => s, StringComparer.Ordinal).ToArray();
                Assert.AreEqual(nonFlowerCount, tuples.Length); Assert.AreEqual(tupleHash, Hash(string.Join("\n", tuples)), "Nonflower owners, positions or finite stock changed.");
                CollectionAssert.AreEquivalent(roads.Split(';'), zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "RoadStone")
                    .Select(e => Coordinate(zone.GetEntityPosition(e))).ToArray(), "The original working pad stays exact.");
                var reserved = zone.GenReservedCells.Select(Coordinate).OrderBy(s => s, StringComparer.Ordinal).ToArray();
                Assert.AreEqual(reservedCount, reserved.Length); Assert.AreEqual(reservedHash, Hash(string.Join("\n", reserved)));
            }
        }

        static string PlayedGraph(Zone zone) => string.Join(";", zone.GetReadOnlyEntities()
            .Select(e => e.ID + ":" + e.BlueprintName + "@" + Coordinate(zone.GetEntityPosition(e)))
            .OrderBy(s => s, StringComparer.Ordinal));
        [Test]
        public void PlayedFlowerRemovalSurvivesCachedReturnAndRealSaveWithoutColdLayoutReplay()
        {
            using (var scope = new HaulingContentScope()) {
                scope.Seed(unchecked(64 ^ FormationSelector.StableIndex(East, int.MaxValue)));
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true); var zone = manager.GetZone(East);
                var flowers = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "CharmFlowers").ToArray();
                Assert.Greater(flowers.Length, 40); var removed = flowers[0]; Assert.True(zone.RemoveEntity(removed));
                var player = scope.Factory.CreateEntity("Player"); Assert.True(zone.AddEntity(player, 40, 12)); manager.SetActiveZone(zone);
                string before = PlayedGraph(zone);
                manager.UnloadZone(East); Assert.AreSame(zone, manager.GetZone(East)); Assert.AreEqual(before, PlayedGraph(zone));
                var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("east-meadow", "flower-persistence", manager, null, player));
                var restored = loaded.ZoneManager.ActiveZone; Assert.AreEqual(before, PlayedGraph(restored));
                Assert.False(restored.GetReadOnlyEntities().Any(e => e.ID == removed.ID));
                Assert.AreEqual(flowers.Length - 1, restored.GetReadOnlyEntities().Count(e => e.BlueprintName == "CharmFlowers"));
                Assert.AreSame(restored, loaded.ZoneManager.GetZone(East)); Assert.AreEqual(before, PlayedGraph(restored));
                var cold = new SpreadCompositionBuilder(64) { Topology = SpreadExplorationTopology.BrokenEnclosures };
                Assert.False(cold.BuildZone(restored, scope.Factory, new Random(64))); Assert.AreEqual(before, PlayedGraph(restored));
            }
        }
    }
}
