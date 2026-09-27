using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
namespace CavesOfOoo.Rendering
{
    /// <summary>Exact current portable identity. Ownership and receiving-biome
    /// authority belong to the renderer; lookup never creates or moves items.</summary>
    public static class SpreadPortableRecipes
    {
        private static readonly HashSet<string> Concrete = new HashSet<string>(StringComparer.Ordinal)
        {
            "Bone",
            "GoldCoin",
            "InkVial",
            "Dagger",
            "Starapple",
            "HealingTonic",
            "PoisonTonic",
            "FireTonic",
            "ForgedWeapon",
            "SteelBladeComponent",
            "IronSpikeComponent",
            "OakHaftComponent",
            "WillowHaftComponent",
            "LeatherBindingComponent",
            "SerratedEdgeComponent",
            "FireMoss",
            "LampOil",
            "EmberFruit",
            "FrostLichen",
            "GlacierSalt",
            "GlimmerBrine",
            "SparkRoot",
            "VenomGland",
            "BogSap",
            "CandyHeartRoot",
            "MendleafSprig",
            "StoneburrSeed",
            "BlastcapSpore",
            "CandyCarrotSeed",
            "EmberwheatSeed",
            "CandyCarrot",
            "WateringGrimoire",
            "Emberwheat",
            "BrewedTonic",
            "InertSludge",
            "AcidTonic",
            "LightningTonic",
            "FrostTonic",
            "WaterTonic",
            "BleedTonic",
            "CharredTonic",
            "Antidote",
            "BurnSalve",
            "Panacea",
            "LeatherArmor",
            "ChainMail",
            "LongSword",
            "Battleaxe",
            "Greatsword",
            "ShortSword",
            "Mace",
            "Spear",
            "Hatchet",
            "Claymore",
            "Cudgel",
            "Buckler",
            "IronHelmet",
            "LeatherBoots",
            "LeatherGloves",
            "LeatherCap",
            "IronshodBoots",
            "WardedCloak",
            "IronBuckler",
            "PlateArmor",
            "Cloak",
            "SpeedTonic",
            "StrengthTonic",
            "StoneskinTonic",
            "Mushroom",
            "DriedMeat",
            "LoanerDagger",
            "LoanerSpear",
            "LoanerLongsword",
            "Warhammer",
            "ChoirSpine",
            "OldWorldPipe",
            "Sporeblade",
            "FlamingSword",
            "IceSword",
            "CryoLance",
            "EmberSpear",
            "AcidicDagger",
            "VenomDagger",
            "ThunderHammer",
            "EchoKnife",
            "TemporalShard",
            "SeveranceEdge",
            "GlassblownStiletto",
            "DissolutionMaul",
            "FirstRootGlaive",
            "PalimpsestBlade",
            "LanternOil",
            "RawMeat",
            "CookedMeat",
            "RoastedStarapple",
            "Torch",
            "SilverSand",
            "WellMaintenanceManual",
            "PurifyWaterGrimoire",
            "GrimoireCopy",
            "RiteGrimoire",
            "MendingRiteGrimoire",
            "OvenBuildersGuide",
            "FireClay",
            "KindleRiteGrimoire",
            "KindleGrimoire",
            "QuenchGrimoire",
            "ConflagrationGrimoire",
            "IceLanceGrimoire",
            "AcidSprayGrimoire",
            "ArcBoltGrimoire",
            "RimeNovaGrimoire",
            "ThunderclapGrimoire",
            "EmberVeinGrimoire",
            "KindleFlameGrimoire",
            "DryingBreezeGrimoire",
            "HearthwarmGrimoire",
            "ConjureWaterGrimoire",
            "ChillDraftGrimoire",
            "WardGleamGrimoire",
            "SchematicHonedEdge",
            "SchematicReinforcedPlating",
            "SchematicDuelistCut",
            "LanternOilRecipe",
            "WardOil",
            "PaleSalt",
            "ChoirIron",
            "WildBerries",
            "Honeycomb",
            "GlowQuartz",
            "PoisonGasGrenade",
            "SleepGasGrenade",
            "StunGasGrenade",
            "StormAnvilGrimoire",
            "HangingBoltGrimoire",
            "RenderedSteamGrimoire",
            "ScaldingVeilGrimoire",
            "FulminationGrimoire",
            "ShatteredRimeGrimoire",
            "StillHeartGrimoire",
            "VerdigrisBloomGrimoire",
            "HollowCoinGrimoire",
            "SunderingWordGrimoire",
            "BloodletterLedgerGrimoire",
            "SaltbriarSprig",
            "WovenDoll",
            "GroveRed",
            "ShamblerSporeSac",
            "Tepuibone",
            "CreatureCorpse",
            "MarlbackCorpse",
            "PruningWrit",
            "BreacherCleaver",
            "IronKey",
            "StillleafKey",
            "StillleafRegister",
            "FrogOil",
            "SealedBogTakenBody",
            "MemoryMarble",
            "MuteStone",
            "ChoirCutting",
            "Waterskin",
            "RoastedMushroom",
            "TemperedLongSword",
            "CounterweightMaul",
            "FineRingMail",
            "RivetedPlate",
            "LiquidFlask",
            "Codex01",
            "Codex02",
            "Codex03",
            "Codex04",
            "Codex05",
            "Codex06",
            "Codex07",
            "Codex08",
            "Codex09",
            "Codex10",
            "Codex11",
            "Codex12",
            "Codex13",
        };
        private static readonly Dictionary<string, string> CorpseFamilies = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "MarlbackScrabbler", "marlback" },
            { "SpreadHurdleCutter", "marlback" },
            { "SpreadDitchMate", "marlback" },
            { "Player", "humanoid" },
            { "MarlbackGleaner", "marlback" },
            { "MarlbackTunnelguard", "marlback" },
            { "Elder", "humanoid" },
            { "Villager", "humanoid" },
            { "Weaponsmith", "humanoid" },
            { "Armorer", "humanoid" },
            { "Apothecary", "humanoid" },
            { "Arcanist", "humanoid" },
            { "Provisioner", "humanoid" },
            { "CaveHermit", "humanoid" },
            { "DesertHermit", "humanoid" },
            { "JungleHermit", "humanoid" },
            { "RuinsHermit", "humanoid" },
            { "Marceline", "humanoid" },
            { "Magpie", "avian" },
            { "PetDog", "quadruped" },
            { "VillageChild", "humanoid" },
            { "Tinker", "humanoid" },
            { "Merchant", "humanoid" },
            { "Quartermaster", "humanoid" },
            { "ChoirTendril", "plant" },
            { "PalimpsestEcho", "humanoid" },
            { "SaccharineEnvoy", "humanoid" },
            { "ConcordFactor", "humanoid" },
            { "PaleCurator", "humanoid" },
            { "GlassblownDrifter", "humanoid" },
            { "CaveBat", "bat" },
            { "CaveSlime", "slime" },
            { "CaveBear", "quadruped" },
            { "Glowmaw", "ambush-maw" },
            { "Scorpion", "scorpion" },
            { "DesertBandit", "humanoid" },
            { "SandWurm", "serpent" },
            { "GiantSpider", "spider" },
            { "Viper", "serpent" },
            { "SpreadLatchcoil", "serpent" },
            { "JungleApe", "ape" },
            { "RuinScavenger", "humanoid" },
            { "SkeletalSentry", "skeleton" },
            { "StoneGolem", "construct" },
            { "PaleStalker", "stalker" },
            { "ObsidianBrute", "construct" },
            { "MarlbackWallkeeper", "marlback" },
            { "MarlbackBreacher", "marlback" },
            { "Mosshulk", "fungal" },
            { "DesertProwler", "quadruped" },
            { "DuneLurker", "ambush-maw" },
            { "BrittleHound", "quadruped" },
            { "JungleStalker", "quadruped" },
            { "Rotling", "fungal" },
            { "CanopyStrangler", "plant" },
            { "AncientGuardian", "construct" },
            { "VaultSentinel", "construct" },
            { "BrassHusk", "construct" },
            { "GlassScorpion", "scorpion" },
            { "SporeShambler", "fungal" },
            { "Mogu", "humanoid" },
            { "Grib", "humanoid" },
            { "Nam", "humanoid" },
            { "Sien", "humanoid" },
            { "Sopp", "humanoid" },
            { "IceWight", "skeleton" },
            { "CharredHusk", "charred-humanoid" },
            { "Warden", "humanoid" },
            { "WellKeeper", "humanoid" },
            { "Farmer", "humanoid" },
            { "Innkeeper", "humanoid" },
            { "Undertaker", "humanoid" },
            { "Scribe", "humanoid" },
            { "SleepingTroll", "troll" },
            { "MimicChest", "mimic" },
            { "AmbushBandit", "humanoid" },
            { "RuneCultist", "humanoid" },
            { "SunStriker", "lizard" },
            { "TentRightHost", "humanoid" },
            { "SaltMaster", "humanoid" },
            { "Reedfrog", "frog" },
            { "Bandfrog", "frog" },
            { "MawToad", "frog" },
            { "GinFrog", "frog" },
            { "RecensionScribe", "humanoid" },
            { "StillleafSearcher", "humanoid" },
            { "CurationSorter", "humanoid" },
            { "StillleafIndexer", "humanoid" },
            { "PeatCutter", "humanoid" },
            { "FilerClerk", "humanoid" },
            { "EncasedElder", "humanoid" },
            { "CatacombWarden", "humanoid" },
            { "PlaqueTender", "humanoid" },
            { "SariSnake", "serpent" },
            { "Wardline", "serpent" },
            { "CascadeFather", "frog" },
            { "GlasspaneFrog", "frog" },
            { "YellowfootWayfarer", "tortoise" },
            { "Shambler", "fungal" },
            { "GroveLanternMoth", "moth" },
            { "SummitSinger", "frog" },
            { "BrocchiniaSentinel", "lizard" },
            { "SkySari", "avian" },
            { "PrickleBrowGecko", "lizard" },
            { "HelmwoodFrog", "frog" },
            { "FoundingListener", "humanoid" },
            { "FoundingPlaqueTender", "humanoid" },
            { "GantryRegistrar", "humanoid" },
            { "SootGremlin", "humanoid" },
            { "DirtGnome", "humanoid" },
        };
        private static bool LiquidColor(string value) => value == "&B" || value == "&C" || value == "&G"
            || value == "&K" || value == "&R" || value == "&W" || value == "&Y" || value == "&c"
            || value == "&g" || value == "&m" || value == "&w" || value == "&y";
        public static bool HandlesBlueprint(string blueprint)
            => blueprint != null && (Concrete.Contains(blueprint) || blueprint == "SeveredLimb"
                || blueprint == "DetectiveNotebook" || blueprint == "CrunchyLocket");
        public static bool TryRecipe(Entity owner, out string modelId)
        {
            modelId = null;
            if (owner == null || !HandlesBlueprint(owner.BlueprintName)
                || owner.HasTag("Natural") || owner.HasTag("Creature")) return false;
            var physics = owner.GetPart<PhysicsPart>();
            var render = owner.GetPart<RenderPart>();
            if (physics == null || !ReferenceEquals(physics.ParentEntity, owner) || !physics.Takeable
                || render == null || !ReferenceEquals(render.ParentEntity, owner) || !render.Visible
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants)) return false;
            if (owner.BlueprintName == "CreatureCorpse" || owner.BlueprintName == "MarlbackCorpse")
            {
                if (!owner.Properties.TryGetValue("SourceBlueprint", out string source)
                    || !owner.Properties.TryGetValue("SourceID", out string sourceId) || string.IsNullOrEmpty(sourceId)
                    || sourceId == owner.ID || source == null || !CorpseFamilies.TryGetValue(source, out string family)
                    || (family == "marlback") != (owner.BlueprintName == "MarlbackCorpse")) return false;
                modelId = family == "marlback" ? "spread-portable-marlbackcorpse" : "spread-portable-corpse-" + family;
                return true;
            }
            if (owner.BlueprintName == "SeveredLimb")
            {
                var limb = owner.GetPart<SeveredLimbPart>();
                if (limb == null || !ReferenceEquals(limb.ParentEntity, owner) || limb.Category < 1 || limb.Category > 21
                    || (limb.PartType != "Head" && limb.PartType != "Arm" && limb.PartType != "Hand"
                        && limb.PartType != "Feet" && limb.PartType != "Tail" && limb.PartType != "Body"
                        && limb.PartType != "Wing" && limb.PartType != "Tendril")) return false;
                modelId = "spread-portable-limb-" + limb.Category + "-" + limb.PartType.ToLowerInvariant();
                return true;
            }
            if (owner.BlueprintName == "BrewedTonic")
            {
                var brew = owner.GetPart<BrewItemPart>();
                if (brew != null && !ReferenceEquals(brew.ParentEntity, owner)) return false;
                if (brew != null && brew.Form != "Tonic")
                {
                    if (brew.Form != "Coating" && brew.Form != "Throwable" && brew.Form != "Food") return false;
                    modelId = "spread-portable-brew-" + brew.Form.ToLowerInvariant();
                    return true;
                }
            }
            if (owner.BlueprintName == "ForgedWeapon")
            {
                var assembly = owner.GetPart<WeaponAssemblyPart>();
                if (assembly != null)
                {
                    if (!ReferenceEquals(assembly.ParentEntity, owner)) return false;
                    string blade = assembly.BladeBlueprint == "SteelBladeComponent" ? "blade" : assembly.BladeBlueprint == "IronSpikeComponent" ? "spike" : null;
                    string haft = assembly.HaftBlueprint == "OakHaftComponent" ? "oak" : assembly.HaftBlueprint == "WillowHaftComponent" ? "willow" : null;
                    string binding = assembly.BindingBlueprint == "LeatherBindingComponent" ? "leather" : assembly.BindingBlueprint == "SerratedEdgeComponent" ? "serrated" : null;
                    if (blade == null || haft == null || binding == null) return false;
                    modelId = "spread-portable-forged-" + blade + "-" + haft + "-" + binding;
                    return true;
                }
            }
            if (owner.BlueprintName == "Waterskin")
            {
                var skin = owner.GetPart<WaterskinPart>();
                if (skin == null || !ReferenceEquals(skin.ParentEntity, owner) || skin.Capacity <= 0
                    || skin.Charges < 0 || skin.Charges > skin.Capacity) return false;
            }
            if (owner.BlueprintName == "LiquidFlask")
            {
                var flask = owner.GetPart<LiquidVesselPart>();
                if (flask == null || !ReferenceEquals(flask.ParentEntity, owner) || flask.Capacity <= 0
                    || flask.Volume < 0 || flask.Volume > flask.Capacity
                    || (flask.Volume == 0) != string.IsNullOrEmpty(flask.LiquidId)) return false;
                if (flask.Volume > 0)
                {
                    var liquid = LiquidRegistry.IsInitialized ? LiquidRegistry.Get(flask.LiquidId) : null;
                    if (liquid == null || liquid.Id != flask.LiquidId || !LiquidColor(liquid.Color)) return false;
                    modelId = "spread-portable-flask-" + ((int)liquid.Color[1]).ToString("x2");
                    return true;
                }
            }
            modelId = "spread-portable-" + owner.BlueprintName.ToLowerInvariant();
            return true;
        }
    }
}
