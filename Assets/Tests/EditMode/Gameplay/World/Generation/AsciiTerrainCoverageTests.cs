using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class AsciiTerrainCoverageTests
    {
        private const string TestBlueprints = @"{
          ""Objects"": [
            {
              ""Name"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""RenderString"", ""Value"": ""?"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&y"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [] }
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""Terrain"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderLayer"", ""Value"": ""0"" }] }
              ],
              ""Stats"": [],
              ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }]
            },
            {
              ""Name"": ""Floor"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""floor"" },
                  { ""Key"": ""RenderString"", ""Value"": ""."" },
                  { ""Key"": ""ColorString"", ""Value"": ""&K"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""Rubble"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""rubble"" },
                  { ""Key"": ""RenderString"", ""Value"": "","" },
                  { ""Key"": ""ColorString"", ""Value"": ""&y"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""Grass"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""grass"" },
                  { ""Key"": ""RenderString"", ""Value"": ""."" },
                  { ""Key"": ""ColorString"", ""Value"": ""&g"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""Wall"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""wall"" },
                  { ""Key"": ""RenderString"", ""Value"": ""#"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&w"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""0"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] }
              ],
              ""Stats"": [],
              ""Tags"": [
                { ""Key"": ""Solid"", ""Value"": """" },
                { ""Key"": ""Wall"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""VineWall"",
              ""Inherits"": ""Wall"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""vine wall"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&G"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""Tree"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""tree"" },
                  { ""Key"": ""RenderString"", ""Value"": ""T"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&G"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""1"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] }
              ],
              ""Stats"": [],
              ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" }]
            },
            {
              ""Name"": ""Bush"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""bush"" },
                  { ""Key"": ""RenderString"", ""Value"": "";"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&G"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""1"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            }
          ]
        }";

        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(TestBlueprints);
        }

        [Test]
        public void CaveBuilder_PassableCellsAlwaysContainTerrain()
        {
            var zone = new Zone("test");
            var builder = new CaveBuilder();

            builder.BuildZone(zone, _factory, new System.Random(42));

            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsPassable())
                {
                    Assert.IsTrue(
                        cell.HasObjectWithTag("Terrain"),
                        $"Passable cave cell ({x},{y}) is missing explicit terrain.");
                }
            });
        }

        [Test]
        public void JungleBuilder_PassableCellsAlwaysContainTerrain()
        {
            var zone = new Zone("test");
            var builder = new JungleBuilder();

            builder.BuildZone(zone, _factory, new System.Random(42));

            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsPassable())
                {
                    Assert.IsTrue(
                        cell.HasObjectWithTag("Terrain"),
                        $"Passable jungle cell ({x},{y}) is missing explicit terrain.");
                }
            });
        }
    }
}
