using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Unit tests for HouseDramaLoader.LoadFromJson. Covers:
    ///   - Valid single-drama JSON registers the drama
    ///   - Valid multi-drama JSON registers all dramas
    ///   - Drama missing an ID is skipped with a warning (no throw)
    ///   - Duplicate ID overwrites the earlier entry
    ///   - null/empty JSON is silently ignored
    ///   - Malformed JSON does not throw (requires try/catch in LoadFromJson)
    ///   - Register() and Reset() behave correctly
    /// </summary>
    public class HouseDramaLoaderTests
    {
        [SetUp]
        public void Setup()
        {
            HouseDramaLoader.Reset();
        }

        // ── Single drama ──────────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_ValidSingleDrama_RegistersDrama()
        {
            string json = @"{""Dramas"":[{""ID"":""Alpha"",""Name"":""Alpha Drama""}]}";

            HouseDramaLoader.LoadFromJson(json, "test");

            var data = HouseDramaLoader.Get("Alpha");
            Assert.IsNotNull(data);
            Assert.AreEqual("Alpha", data.ID);
        }

        // ── Multiple dramas ───────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_MultiDramaJson_RegistersAll()
        {
            string json = @"{""Dramas"":[
                {""ID"":""Alpha"",""Name"":""Alpha Drama""},
                {""ID"":""Beta"",""Name"":""Beta Drama""}
            ]}";

            HouseDramaLoader.LoadFromJson(json, "test");

            Assert.IsNotNull(HouseDramaLoader.Get("Alpha"));
            Assert.IsNotNull(HouseDramaLoader.Get("Beta"));
        }

        // ── Missing ID skipped ────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_DramaMissingId_SkipsWithoutThrow()
        {
            string json = @"{""Dramas"":[{""Name"":""No ID""}]}";

            Assert.DoesNotThrow(() => HouseDramaLoader.LoadFromJson(json, "test"));

            Assert.AreEqual(0, HouseDramaLoader.GetAll().Count);
        }

        // ── Duplicate ID overwrites ───────────────────────────────────────────

        [Test]
        public void LoadFromJson_DuplicateId_OverwritesEarlierEntry()
        {
            string json1 = @"{""Dramas"":[{""ID"":""Alpha"",""Name"":""First""}]}";
            string json2 = @"{""Dramas"":[{""ID"":""Alpha"",""Name"":""Second""}]}";

            HouseDramaLoader.LoadFromJson(json1, "file1");
            HouseDramaLoader.LoadFromJson(json2, "file2");

            var data = HouseDramaLoader.Get("Alpha");
            Assert.AreEqual("Second", data.Name);
        }

        // ── Null / empty JSON ─────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_NullJson_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => HouseDramaLoader.LoadFromJson(null, "test"));
        }

        [Test]
        public void LoadFromJson_EmptyJson_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => HouseDramaLoader.LoadFromJson("", "test"));
        }

        // ── Malformed JSON ────────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_MalformedJson_DoesNotThrow()
        {
            // RED: HouseDramaLoader.LoadFromJson currently has no try/catch around
            // JsonUtility.FromJson — this will throw until the fix is applied.
            Assert.DoesNotThrow(() =>
                HouseDramaLoader.LoadFromJson("{ not valid json }", "bad_file"));
        }

        // ── Register ──────────────────────────────────────────────────────────

        [Test]
        public void Register_AddsToLookup()
        {
            var drama = new HouseDramaData { ID = "Manual", Name = "Manually Registered" };
            HouseDramaLoader.Register(drama);

            Assert.IsNotNull(HouseDramaLoader.Get("Manual"));
        }

        [Test]
        public void Register_NullData_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => HouseDramaLoader.Register(null));
        }

        // ── GetAll ────────────────────────────────────────────────────────────

        [Test]
        public void GetAll_ReturnsAllRegisteredDramas()
        {
            HouseDramaLoader.Register(new HouseDramaData { ID = "A" });
            HouseDramaLoader.Register(new HouseDramaData { ID = "B" });
            HouseDramaLoader.Register(new HouseDramaData { ID = "C" });

            var all = HouseDramaLoader.GetAll();

            Assert.AreEqual(3, all.Count);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllRegisteredDramas()
        {
            HouseDramaLoader.Register(new HouseDramaData { ID = "Alpha" });
            HouseDramaLoader.Reset();

            Assert.IsNull(HouseDramaLoader.Get("Alpha"));
            Assert.AreEqual(0, HouseDramaLoader.GetAll().Count);
        }

        // ── Get unknown ───────────────────────────────────────────────────────

        [Test]
        public void Get_UnknownId_ReturnsNull()
        {
            var result = HouseDramaLoader.Get("nonexistent_drama");

            Assert.IsNull(result);
        }
    }
}
