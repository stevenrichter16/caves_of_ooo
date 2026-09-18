using System;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>The source-art Felling circle at its existing world location.
    /// Scene geometry, seven positions and component owners share one definition.</summary>
    public sealed class FellingSiteBuilder : IZoneBuilder
    {
        public string Name=>"FellingSite";
        public int Priority=>1000;
        public const int WorldX=3,WorldY=5;
        public const string SiteName="the Felling-Site";
        public const string ZoneID="Overworld.3.5.0";
        public const int SeventhX=40,SeventhY=8;
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
            =>FellingSceneRuntime.Install(zone,factory);
    }
}
