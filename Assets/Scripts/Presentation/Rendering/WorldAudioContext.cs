using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>Known place and visible danger only. Never persists or derives hidden encounter information.</summary>
    public readonly struct WorldAudioContext
    {
        public readonly string MusicId, AmbienceId;
        public readonly bool Danger;
        WorldAudioContext(string music,string ambience,bool danger){MusicId=music;AmbienceId=ambience;Danger=danger;}
        public WorldAudioContext WithAmbience(string id)=>new WorldAudioContext(MusicId,id,Danger);
        public static WorldAudioContext Select(BiomeType biome,int depth,bool danger,bool inhabitedStop=false)
        {
            string music,ambience;
            if(depth>0||biome==BiomeType.Cave||biome==BiomeType.Ruins||biome==BiomeType.Stump)
            {music="underground";ambience="cave";}
            else if(biome==BiomeType.Desert||biome==BiomeType.Beating||biome==BiomeType.Overwrit)
            {music="dry";ambience=null;}
            else {music=biome==BiomeType.Sodden?"sodden":biome==BiomeType.Grovelands?"grovelands":"organic";ambience=biome==BiomeType.Sodden?"water":"field";}
            if(inhabitedStop&&depth==0&&music!="underground")music="village";
            return new WorldAudioContext(danger?"danger":music,ambience,danger);
        }
    }
}
