using System;
using System.Collections.Generic;
namespace CavesOfOoo.Rendering
{
 /// <summary>Pure exact source preflight shared by scoped adoption and runtime
 /// identity. It performs no asset, world, RNG or Unity operations.</summary>
 [Serializable] public sealed class SpreadBiomeHumanoidSource
 {
  [Serializable]public sealed class Role{public string blueprint,id,glyph,headwear,garment;public int body,skin,accent,hair;public float stature;}
  public int schemaVersion;public string sourceRig;public string[] preserveNativeModelIds,palette;public Role[] roles;
  private static readonly Dictionary<string,string> Ids=new Dictionary<string,string>(StringComparer.Ordinal)
  {
   {"Elder","spread-person-elder"},
   {"Villager","spread-person-villager"},
   {"Weaponsmith","spread-person-weaponsmith"},
   {"Armorer","spread-person-armorer"},
   {"Apothecary","spread-person-apothecary"},
   {"Arcanist","spread-person-arcanist"},
   {"Provisioner","spread-person-provisioner"},
   {"CaveHermit","spread-person-cave-hermit"},
   {"DesertHermit","spread-person-desert-hermit"},
   {"JungleHermit","spread-person-jungle-hermit"},
   {"RuinsHermit","spread-person-ruins-hermit"},
   {"Marceline","spread-person-marceline"},
   {"VillageChild","spread-person-village-child"},
   {"Tinker","spread-person-tinker"},
   {"Merchant","spread-person-merchant"},
   {"Quartermaster","spread-person-quartermaster"},
   {"PalimpsestEcho","spread-person-palimpsest-echo"},
   {"SaccharineEnvoy","spread-person-saccharine-envoy"},
   {"ConcordFactor","spread-person-concord-factor"},
   {"PaleCurator","spread-person-pale-curator"},
   {"GlassblownDrifter","spread-person-glassblown-drifter"},
   {"DesertBandit","spread-person-desert-bandit"},
   {"RuinScavenger","spread-person-ruin-scavenger"},
   {"Mogu","spread-person-mogu"},
   {"Grib","spread-person-grib"},
   {"Nam","spread-person-nam"},
   {"Sien","spread-person-sien"},
   {"Sopp","spread-person-sopp"},
   {"Warden","spread-person-warden"},
   {"WellKeeper","spread-person-well-keeper"},
   {"Farmer","spread-person-farmer"},
   {"Innkeeper","spread-person-innkeeper"},
   {"Undertaker","spread-person-undertaker"},
   {"Scribe","spread-person-scribe"},
   {"AmbushBandit","spread-person-ambush-bandit"},
   {"RuneCultist","spread-person-rune-cultist"},
   {"TentRightHost","spread-person-tent-right-host"},
   {"SaltMaster","spread-person-salt-master"},
   {"RecensionScribe","spread-person-recension-scribe"},
   {"StillleafSearcher","spread-person-stillleaf-searcher"},
   {"CurationSorter","spread-person-curation-sorter"},
   {"StillleafIndexer","spread-person-stillleaf-indexer"},
   {"PeatCutter","spread-person-peat-cutter"},
   {"FilerClerk","spread-person-filer-clerk"},
   {"EncasedElder","spread-person-encased-elder"},
   {"CatacombWarden","spread-person-catacomb-warden"},
   {"PlaqueTender","spread-person-plaque-tender"},
   {"FoundingListener","spread-person-founding-listener"},
   {"FoundingPlaqueTender","spread-person-founding-plaque-tender"},
   {"GantryRegistrar","spread-person-gantry-registrar"},
   {"SootGremlin","spread-person-soot-gremlin"},
   {"DirtGnome","spread-person-dirt-gnome"},
   {"MarlbackCindercaller","spread-person-marlback-cindercaller"},
   {"MarlbackSoursprayer","spread-person-marlback-soursprayer"},
  };
  private static readonly Dictionary<string,string> Glyphs=new Dictionary<string,string>(StringComparer.Ordinal)
  {
   {"Elder","@"},
   {"Villager","@"},
   {"Weaponsmith","@"},
   {"Armorer","@"},
   {"Apothecary","@"},
   {"Arcanist","@"},
   {"Provisioner","@"},
   {"CaveHermit","@"},
   {"DesertHermit","@"},
   {"JungleHermit","@"},
   {"RuinsHermit","@"},
   {"Marceline","M"},
   {"VillageChild","c"},
   {"Tinker","@"},
   {"Merchant","@"},
   {"Quartermaster","@"},
   {"PalimpsestEcho","@"},
   {"SaccharineEnvoy","@"},
   {"ConcordFactor","@"},
   {"PaleCurator","@"},
   {"GlassblownDrifter","@"},
   {"DesertBandit","h"},
   {"RuinScavenger","r"},
   {"Mogu","M"},
   {"Grib","G"},
   {"Nam","N"},
   {"Sien","S"},
   {"Sopp","s"},
   {"Warden","@"},
   {"WellKeeper","@"},
   {"Farmer","@"},
   {"Innkeeper","@"},
   {"Undertaker","U"},
   {"Scribe","@"},
   {"AmbushBandit","b"},
   {"RuneCultist","c"},
   {"TentRightHost","@"},
   {"SaltMaster","@"},
   {"RecensionScribe","@"},
   {"StillleafSearcher","@"},
   {"CurationSorter","@"},
   {"StillleafIndexer","@"},
   {"PeatCutter","@"},
   {"FilerClerk","@"},
   {"EncasedElder","@"},
   {"CatacombWarden","@"},
   {"PlaqueTender","@"},
   {"FoundingListener","@"},
   {"FoundingPlaqueTender","@"},
   {"GantryRegistrar","@"},
   {"SootGremlin","s"},
   {"DirtGnome","g"},
   {"MarlbackCindercaller","g"},
   {"MarlbackSoursprayer","g"},
  };

  public static bool IsCaster(string blueprint)=>blueprint=="MarlbackCindercaller"||blueprint=="MarlbackSoursprayer";
  public static string ModelId(string blueprint)=>blueprint!=null&&Ids.TryGetValue(blueprint,out var id)?id:null;
  public static string CanonicalGlyph(string blueprint)=>blueprint!=null&&Glyphs.TryGetValue(blueprint,out var glyph)?glyph:null;
  public static void ValidateRole(Role role)
  {
   if(role==null||string.IsNullOrEmpty(role.blueprint)||string.IsNullOrEmpty(role.id)||ModelId(role.blueprint)==null||role.id!=ModelId(role.blueprint)||role.glyph!=CanonicalGlyph(role.blueprint)
    ||float.IsNaN(role.stature)||float.IsInfinity(role.stature)||role.stature<.65f||role.stature>1
    ||role.body<0||role.body>=24||role.skin<0||role.skin>=24||role.accent<0||role.accent>=24||role.hair<0||role.hair>=24
    ||Array.IndexOf(new[]{"hair","cap","hood","tall-cap","long-hair","goggles","straw-hat","ears"},role.headwear)<0
    ||Array.IndexOf(new[]{"belt","sash","apron","pouches","cloak","coat","book-pouch"},role.garment)<0)
    throw new ArgumentException("Invalid explicit humanoid source role.");
  }
 }
}
