using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Scene-level cold-generation contracts, not a pixel-quality acceptance test.</summary>
    public sealed class GladeVegetationCompositionTests
    {
        static readonly int[] Seeds = { 0, 1, 64, 65, 1729, int.MinValue, int.MaxValue };

        static IEnumerable<TestCaseData> Variants()
        {
            foreach (int seed in Seeds)
                foreach (bool fieldwork in new[] { false, true })
                    yield return new TestCaseData(seed, fieldwork);
        }

        // Literal receipts from native pre-change plans, captured in
        // Docs/Verification/Live16x24/glade-vegetation-original-layouts.json.gz.
        // Do not refresh these pins from the changed production implementation.
        static IEnumerable<TestCaseData> OriginalLayouts()
        {
            yield return new TestCaseData(0, false,
                "02eb699d8edd01a187226224ebf97728cb7c0af22639b5aab9282226978059a1",
                "ddef2c270831daf2796978b23b720305d4d17edfdb1fcd1ff96d518e969bf7c4",
                "BrokenColumn=23;Bush=192;Campfire=1;Chest=1;CropRow=17;FallenBeam=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=262;RipeCropRow=3;RoadStone=25;Rubble=68;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=73;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed0_fieldworkfalse");
            yield return new TestCaseData(0, true,
                "916d5cbdb2894c021d73950606e845b1fb2b794a6b2bf2a67d4c49160c65525f",
                "940542451809113fed4c58c59134cf3910d6a37d275b3d229a256122e278deac",
                "BrokenColumn=23;Bush=192;Campfire=1;Chest=1;CropRow=17;DrawgourdCrop=1;FallenBeam=1;GleanersBuckledWicket=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=262;RipeCropRow=3;RoadStone=25;Rubble=68;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=72;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed0_fieldworktrue");
            yield return new TestCaseData(1, false,
                "daf7712306551d00a84e79a766fcd44db28d152472bad2e1989bdc0cef4be0a1",
                "f33729d5e2b65578f7dbd535290ef341c028410c18bc3d60e4453c3c0c99a372",
                "BrokenColumn=27;Bush=193;Campfire=1;Chest=1;CropRow=17;FallenBeam=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=265;RipeCropRow=3;RoadStone=25;Rubble=65;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=73;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed1_fieldworkfalse");
            yield return new TestCaseData(1, true,
                "90418025b7fc7a3a4f8a326ec932e89f24bdaeaa8e7bbac96c071892cb754c1a",
                "7c326a4867197aa21de801c5ac9c5060722e9c7379c429528a3ac99f3a554f30",
                "BrokenColumn=27;Bush=193;Campfire=1;Chest=1;CropRow=17;DrawgourdCrop=1;FallenBeam=1;GleanersBuckledWicket=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=265;RipeCropRow=3;RoadStone=25;Rubble=65;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=72;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed1_fieldworktrue");
            yield return new TestCaseData(64, false,
                "743d07f2bb18e4e3f4c88a45091747bbc14d32eae76d17f249072d96b3647288",
                "b0560e7f55e4a97b9a59ae002b650bc59d0ff06fb8ef66ab3ca4e721c5353903",
                "BrokenColumn=22;Bush=195;Campfire=1;Chest=1;CropRow=17;FallenBeam=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=255;RipeCropRow=3;RoadStone=25;Rubble=66;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=73;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed64_fieldworkfalse");
            yield return new TestCaseData(64, true,
                "776c0fb0bca2e575f02fcfdf020e3045722f71ed65a19c5e4a4cfee6f7c990cd",
                "37e3e1e97c6496b324184f5ecaa366734d2cdc442c9c2208f937b936067f0b65",
                "BrokenColumn=22;Bush=195;Campfire=1;Chest=1;CropRow=17;DrawgourdCrop=1;FallenBeam=1;GleanersBuckledWicket=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=255;RipeCropRow=3;RoadStone=25;Rubble=65;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=72;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed64_fieldworktrue");
            yield return new TestCaseData(65, false,
                "8702af6049d590c1e00ff04bd9182b4694d98473f5e615317b19e0075dc0e263",
                "58fe48099d413dd8bf25d4e7dfd5d7410096cb493a7d89b054ac93113efa8e70",
                "BrokenColumn=24;Bush=194;Campfire=1;Chest=1;CropRow=17;FallenBeam=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=253;RipeCropRow=3;RoadStone=25;Rubble=64;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=73;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed65_fieldworkfalse");
            yield return new TestCaseData(65, true,
                "6717c420938c5341468d4398d854210a5ba796bcbaebca1b55bfd3e1944ee6b6",
                "5dd866ec563b7bb29f9bd62995db94123347f16baeaa5f41463e21394febd50b",
                "BrokenColumn=24;Bush=194;Campfire=1;Chest=1;CropRow=17;DrawgourdCrop=1;FallenBeam=1;GleanersBuckledWicket=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=253;RipeCropRow=3;RoadStone=25;Rubble=64;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=72;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed65_fieldworktrue");
            yield return new TestCaseData(1729, false,
                "8588b6423c390e660385b8f0adf0cb5d417b5632f8bc3225068f40f3dd192aab",
                "e894ff9fbfda2b56aababca2f462f7b66b67ca96e70ee822935210a5ae85a7bf",
                "BrokenColumn=26;Bush=192;Campfire=1;Chest=1;CropRow=17;FallenBeam=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=271;RipeCropRow=3;RoadStone=25;Rubble=69;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=73;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed1729_fieldworkfalse");
            yield return new TestCaseData(1729, true,
                "9b3aeb9b5179f6d1b018115ac2841d22c25d6489f06260d8e41f2112c09a7b1b",
                "ab2dd83a7f220f274a06926c56b778c262576ebfd26adff85f1fe39b0adc2ad2",
                "BrokenColumn=26;Bush=192;Campfire=1;Chest=1;CropRow=17;DrawgourdCrop=1;FallenBeam=1;GleanersBuckledWicket=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=271;RipeCropRow=3;RoadStone=25;Rubble=68;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=72;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed1729_fieldworktrue");
            yield return new TestCaseData(-2147483648, false,
                "7c82220c7995b40b30646257316fcf0427b83c17a7a66b3915dcf35f8c69cb64",
                "0683ccc97360cbad1162a115f0c6eb1a913bd73a746e3ee1ca2f064c16099567",
                "BrokenColumn=18;Bush=194;Campfire=1;Chest=1;CropRow=17;FallenBeam=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=259;RipeCropRow=3;RoadStone=25;Rubble=68;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=73;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed-2147483648_fieldworkfalse");
            yield return new TestCaseData(-2147483648, true,
                "dc14138cfd0399ec46e8c699c08f373d483300f31bdc0dcf679fad707802f071",
                "dbf73d53b1a359a42fcb4e5d2035fc9eeb8541b0a400a1ae34f99e3a562d6395",
                "BrokenColumn=18;Bush=194;Campfire=1;Chest=1;CropRow=17;DrawgourdCrop=1;FallenBeam=1;GleanersBuckledWicket=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=259;RipeCropRow=3;RoadStone=25;Rubble=68;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=72;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed-2147483648_fieldworktrue");
            yield return new TestCaseData(2147483647, false,
                "c12e1b25480f4a1d20a576984064dd9c1699660d60aea2fe2ff03125afca0efd",
                "d09372af1ec1f94868814a88824e150cc16290113acf301e9071bdb73a04f911",
                "BrokenColumn=32;Bush=172;Campfire=1;Chest=1;CropRow=17;FallenBeam=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=264;RipeCropRow=3;RoadStone=25;Rubble=65;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=73;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed2147483647_fieldworkfalse");
            yield return new TestCaseData(2147483647, true,
                "38e4ca8aee5e970e9c5e8b1813bd99867dc7aa1074e125df62fec0f24d4539e2",
                "3ea251dd17ad9bce57f01fa2d733838e0f2ad923ea9d4fb780359bd50f0aca6d",
                "BrokenColumn=32;Bush=172;Campfire=1;Chest=1;CropRow=17;DrawgourdCrop=1;FallenBeam=1;GleanersBuckledWicket=1;GlowQuartzVein=2;Grass=2000;MarlbackGleaner=1;MarlbackScrabbler=2;MushroomRing=2;PetDog=1;Reeds=264;RipeCropRow=3;RoadStone=25;Rubble=64;Signpost=2;SpreadDrawPoint=1;Torch=1;Villager=1;Wall=72;Warden=1;Waterskin=1;WoodenBarrel=2").SetName("OriginalLayout_seed2147483647_fieldworktrue");
        }

        // Scope is part of the design contract. This deliberately does not call
        // a new production relocation predicate or reproduce its affinity/ranking.
        static bool Protected(ReferenceGladePlan.Placement p)
            => (p.X >= 28 && p.X <= 39 && p.Y >= 1 && p.Y <= 10)
                || (p.X >= 41 && p.X <= 51 && p.Y >= 6 && p.Y <= 14)
                || (p.X >= 24 && p.X <= 29 && p.Y >= 14 && p.Y <= 21)
                || Math.Max(Math.Abs(p.X - 40), Math.Abs(p.Y - 12)) <= 2;

        static bool Movable(ReferenceGladePlan.Placement p)
            => !Protected(p)
                && ((p.Blueprint == "Bush" && p.VisualID == "reference-glade-green-grass")
                    || (p.Blueprint == "Reeds" && p.VisualID == "reference-glade-pale-reeds" && p.X >= 52));

        static string Hash(string text)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text)))
                    .Replace("-", "").ToLowerInvariant();
        }

        static string FixedSignature(IEnumerable<ReferenceGladePlan.Placement> placements)
            => string.Concat(placements.Where(p => !Movable(p))
                .Select(p => p.Blueprint + "|" + p.VisualID + "|" + p.X + "|" + p.Y + "\n"));

        [TestCaseSource(nameof(OriginalLayouts))]
        public void FunctionalFootprintsPlantBudgetsAndOwnerOrderStayAtTheOriginalReceipt(
            int seed, bool fieldwork, string fixedHash, string ownerOrderHash, string blueprintBudget)
        {
            var plan = ReferenceGladePlan.Create(seed, fieldwork);
            var placements = plan.Placements;
            Assert.AreEqual(fixedHash, Hash(FixedSignature(placements)),
                "Only the declared grass-Bush/eastern-Reeds dressing may move. Pond/west reeds, discoveries, rubble and terrain stay exact and ordered.");
            Assert.AreEqual(blueprintBudget, string.Join(";", placements.GroupBy(p => p.Blueprint)
                .OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => g.Key + "=" + g.Count())),
                "Visual quiet must not erase or multiply real plant/fuel and resource budgets.");
            Assert.AreEqual(ownerOrderHash, Hash(string.Concat(placements.Select(p => p.Blueprint + "|" + p.VisualID + "\n"))),
                "Relocation must preserve each blueprint/VisualID slot in the staged owner sequence.");
            Assert.AreEqual(2000, placements.Count(p => p.Blueprint == "Grass"));
            Assert.AreEqual(2000, placements.Where(p => p.Blueprint == "Grass").Select(p => (p.X, p.Y)).Distinct().Count());
            Assert.True(placements.All(p => p.X >= 0 && p.X < 80 && p.Y >= 0 && p.Y < 25));
            var aboveGround = placements.Where(p => p.Blueprint != "Grass").ToArray();
            Assert.AreEqual(aboveGround.Length, aboveGround.Select(p => (p.X, p.Y)).Distinct().Count(),
                "A prettier cluster cannot hide duplicate owners or place vegetation on functional scenery.");
            Assert.AreEqual(plan.Signature(), ReferenceGladePlan.Create(seed, fieldwork).Signature());
        }

        [TestCaseSource(nameof(Variants))]
        public void FieldAndRuinApproachesAreQuietWithoutErasingTheSurroundingVegetation(int seed, bool fieldwork)
        {
            var movable = ReferenceGladePlan.Create(seed, fieldwork).Placements.Where(Movable).ToArray();
            Assert.Greater(movable.Count(p => p.Blueprint == "Bush"), 150, "An empty composition cannot satisfy the quiet-apron test.");
            Assert.Greater(movable.Count(p => p.Blueprint == "Reeds"), 150);
            Assert.IsEmpty(movable.Where(p => p.X >= 52 && p.X <= 60 && p.Y >= 10 && p.Y <= 15),
                "The field's eastern apron must read as an open approach, not the old reed rectangle.");
            Assert.IsEmpty(movable.Where(p => p.X >= 32 && p.X <= 39 && p.Y >= 15 && p.Y <= 18),
                "The western central approach must leave the real ruin and actors legible.");
        }

        [TestCaseSource(nameof(Variants))]
        public void EasternReedsRetainPopulatedUpperMiddleAndLowerDrifts(int seed, bool fieldwork)
        {
            var reeds = ReferenceGladePlan.Create(seed, fieldwork).Placements
                .Where(p => Movable(p) && p.Blueprint == "Reeds").ToArray();
            // Broad visual regions are acceptance controls, not ellipse centers,
            // exact chosen cells or a copy of the production scoring algorithm.
            Assert.GreaterOrEqual(reeds.Count(p => p.Y <= 9), 12,
                "The upper drift must be a real plant group, not isolated remainder cells.");
            Assert.GreaterOrEqual(reeds.Count(p => p.X >= 61 && p.Y >= 10 && p.Y <= 15), 20,
                "Keep an eastern middle drift beside the quiet field apron.");
            Assert.GreaterOrEqual(reeds.Count(p => p.Y >= 17), 40,
                "A substantial lower drift must remain beyond the central cross.");
        }

        [TestCaseSource(nameof(Variants))]
        public void MostGrassBushesBelongToAdjacentClustersInsteadOfSingleTileSprinkles(int seed, bool fieldwork)
        {
            var bushes = new HashSet<(int x, int y)>(ReferenceGladePlan.Create(seed, fieldwork).Placements
                .Where(p => Movable(p) && p.Blueprint == "Bush").Select(p => (p.X, p.Y)));
            Assert.Greater(bushes.Count, 150, "Clustering cannot be achieved by deleting the native fuel owners.");
            int joined = bushes.Count(p => bushes.Contains((p.x - 1, p.y)) || bushes.Contains((p.x + 1, p.y))
                || bushes.Contains((p.x, p.y - 1)) || bushes.Contains((p.x, p.y + 1)));
            Assert.GreaterOrEqual(joined * 3, bushes.Count * 2,
                "At least two thirds of grass-Bush owners need a cardinal Bush neighbor; scattered singles must not dominate.");
        }

        [TestCaseSource(nameof(Variants))]
        public void EasternReedDriftsHaveSmallRealInteriorGapsInsteadOfSolidStamps(int seed, bool fieldwork)
        {
            var placements = ReferenceGladePlan.Create(seed, fieldwork).Placements;
            var reeds = new HashSet<(int x, int y)>(placements
                .Where(p => Movable(p) && p.Blueprint == "Reeds").Select(p => (p.X, p.Y)));
            var occupied = new HashSet<(int x, int y)>(placements
                .Where(p => p.Blueprint != "Grass").Select(p => (p.X, p.Y)));
            Assert.Greater(reeds.Count, 150, "An erased drift cannot satisfy an interior-gap contract.");
            int gaps = 0;
            for (int y = 1; y < 24; y++)
                for (int x = 53; x < 78; x++)
                {
                    // Existing public/quiet routes are deliberate large breaks,
                    // not the small gaps inside the plant mass sought here.
                    if (ReferenceGladePlan.IsApproach(x, y) || occupied.Contains((x, y))
                        || (x <= 60 && y >= 10 && y <= 15)) continue;
                    int neighbors = (reeds.Contains((x - 1, y)) ? 1 : 0)
                        + (reeds.Contains((x + 1, y)) ? 1 : 0)
                        + (reeds.Contains((x, y - 1)) ? 1 : 0)
                        + (reeds.Contains((x, y + 1)) ? 1 : 0);
                    if (neighbors >= 3) gaps++;
                }
            Assert.GreaterOrEqual(gaps, 6,
                "Retain several genuinely empty cells enclosed by the reeds; a different prop or a mandatory crossing is not an interior gap.");
        }

        [TestCaseSource(nameof(Variants))]
        public void EasternWildReedsDoNotFormLongFullyFilledHorizontalRanks(int seed, bool fieldwork)
        {
            var reeds = new HashSet<(int x, int y)>(ReferenceGladePlan.Create(seed, fieldwork).Placements
                .Where(p => Movable(p) && p.Blueprint == "Reeds").Select(p => (p.X, p.Y)));
            Assert.Greater(reeds.Count, 150, "The same substantial plant budget must survive the shape refinement.");
            int longest = 0;
            for (int y = 0; y < 25; y++)
            {
                int run = 0;
                for (int x = 52; x < 78; x++)
                {
                    run = reeds.Contains((x, y)) ? run + 1 : 0;
                    longest = Math.Max(longest, run);
                }
            }
            Assert.LessOrEqual(longest, 12,
                "Wild drifts need interruptions before a full native row becomes a ruler-straight planted strip. Keep the separate regional-population controls.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(64)] [TestCase(65)] [TestCase(1729)]
        [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
        public void ActualOwnersKeepFourExitsAndExistingServiceApproachesAcrossSeeds(int seed)
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(seed);
                foreach (bool fieldwork in new[] { false, true })
                {
                    var zone = new Zone(ReferenceGladePlan.ZoneID);
                    var random = new Random(19);
                    Assert.True(new ReferenceGladeBuilder(seed, fieldwork).BuildZone(zone, content.Factory, random));
                    Assert.AreEqual(new Random(19).Next(), random.Next(), "Composition must not consume the caller's random stream.");
                    var reached = DensityReferenceGladeTests.Reach(zone, 40, 12);
                    Assert.True(reached.Any(p => p.Item1 == 0));
                    Assert.True(reached.Any(p => p.Item1 == 79));
                    Assert.True(reached.Any(p => p.Item2 == 0));
                    Assert.True(reached.Any(p => p.Item2 == 24));
                    string[] services = { "Chest", "SpreadDrawPoint", "FallenBeam", "Campfire", "RipeCropRow", "GlowQuartzVein" };
                    var owners = zone.GetReadOnlyEntities().Where(e => services.Contains(e.BlueprintName)).ToArray();
                    CollectionAssert.AreEquivalent(services, owners.Select(e => e.BlueprintName).Distinct());
                    foreach (var owner in owners)
                    {
                        var cell = zone.GetEntityCell(owner);
                        Assert.True(reached.Any(p => Math.Max(Math.Abs(p.Item1 - cell.X), Math.Abs(p.Item2 - cell.Y)) <= 1),
                            seed + " fieldwork=" + fieldwork + ": no reachable approach to " + owner.BlueprintName);
                    }
                }
            }
        }
    }
}
