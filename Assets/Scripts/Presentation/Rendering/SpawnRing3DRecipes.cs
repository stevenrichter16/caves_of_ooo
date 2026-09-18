using System;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>A read-only instruction for the current native object. Batched
    /// scenery still retains its entity alias; native mutations invalidate its
    /// ground patch. A failed recipe requests the ordinary native fallback.</summary>
    public readonly struct SpawnRing3DRecipe
    {
        public readonly Entity Owner;
        public readonly string ModelId, ComponentId, Failure;
        public readonly Vector3 Position;
        public readonly bool Transient, Batched;
        public readonly int QuarterTurns;
        public SpawnRing3DRecipe(Entity owner, string modelId, string componentId, Vector3 position, bool transient, bool batched, string failure = null, int quarterTurns = 0)
        { Owner = owner; ModelId = modelId; ComponentId = componentId; Position = position; Transient = transient; Batched = batched; Failure = failure; QuarterTurns = quarterTurns; }
    }

    /// <summary>Maps current native refs to explicit ring art without spawning,
    /// repairing, assigning IDs, consuming RNG or changing gameplay state.</summary>
    public static class SpawnRing3DRecipes
    {
        public static SpawnRing3DRecipe Resolve(Zone zone, Entity entity, SpawnRing3DCatalog catalog, MultiCellPilot3DCatalog pilot = null)
        {
            if (zone == null || catalog == null || !catalog.SupportsZone(zone.ZoneID)) return Refused(entity, "outside-ring");
            if (!AreaCompositionScope.Allows(zone)) return Refused(entity,"runtime-place-authority");
            if (zone.ZoneID == FellingSiteBuilder.ZoneID && !FellingSceneRuntime.IsActive(zone)) return Refused(entity, "missing-native-felling-contract");
            if (entity == null) return Refused(null, "missing-native-entity");
            var cell = zone.GetEntityCell(entity);
            if (cell == null || !cell.Objects.Contains(entity)) return Refused(entity, "not-current-zone-member");
            var render = entity.GetPart<RenderPart>();
            if (render == null || !render.Visible) return Refused(entity, "native-render-hidden");
            if (entity.HasPart<MultiCellPilotPropPart>()) return ResolvePilot(zone, entity, pilot);
            var component = entity.GetPart<FellingScenePropPart>();
            string componentId = null, modelId;
            bool transient = false, batched;
            if (component != null)
            {
                componentId = component.ComponentId;
                var owner = catalog.FindFellingOwner(componentId);
                if (zone.ZoneID != FellingSiteBuilder.ZoneID || owner == null
                    || !ReferenceEquals(FellingSceneRuntime.FindOwner(zone, componentId), entity))
                    return Refused(entity, "not-current-felling-owner");
                modelId = owner.modelId; batched = false;
            }
            else
            {
                bool cinderhold=CinderholdCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool sumphold=SumpholdCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool drownedLedger=DrownedLedgerCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool marrowstye=MarrowstyeCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool witnessIntake=drownedLedger||marrowstye;
                if(witnessIntake)
                {
                    bool ledgerArt=drownedLedger;
                    string family=ledgerArt?DrownedLedgerVoxelKitLibrary.Family(entity.BlueprintName):MarrowstyeVoxelKitLibrary.Family(entity.BlueprintName);
                    // Cargo and inhabitants retain native identity on either end
                    // of the courier route. Architecture remains local.
                    if(family==null)
                    {
                        string shared=ledgerArt?MarrowstyeVoxelKitLibrary.Family(entity.BlueprintName):DrownedLedgerVoxelKitLibrary.Family(entity.BlueprintName);
                        if(shared!=null&&shared!="ground"&&shared!="wall"){family=shared;ledgerArt=!ledgerArt;}
                    }
                    if(family!=null)
                    {
                        bool actor=entity.HasTag("Creature");
                        if(actor&&!MatchesNativeActorGlyph(entity.BlueprintName,render.RenderString))return Refused(entity,"reskinned-native-actor");
                        bool moving=actor||entity.GetPart<PhysicsPart>()?.Takeable==true;
                        // Massive cargo stays batched but moving it must not
                        // select a different shape from its new cell coordinate.
                        bool stable=moving||entity.BlueprintName=="StoneCoffer"||entity.BlueprintName=="SaltCuredBody";
                        int variant=Variant(stable?null:zone.ZoneID,entity.BlueprintName,entity.ID,stable?0:cell.X,stable?0:cell.Y,4);
                        string id=ledgerArt?DrownedLedgerVoxelKitLibrary.ModelId(family,variant):MarrowstyeVoxelKitLibrary.ModelId(family,variant);
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),moving,!moving);
                    }
                    // Quiet low shelter walls keep people legible beside the
                    // reading tent; native pool surfaces use the shared teal kit.
                    if(drownedLedger&&(entity.BlueprintName=="WaterPuddle"||entity.BlueprintName=="SandstoneWall"))
                        return new SpawnRing3DRecipe(entity,SumpholdVoxelKitLibrary.ModelId(entity.BlueprintName=="WaterPuddle"?"water":"wall",Variant(zone.ZoneID,entity.BlueprintName,entity.ID,cell.X,cell.Y,4)),
                            null,Village3DProjection.CellCentre(cell.X,cell.Y),false,true);
                }
                bool firstTent=FirstTentCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool lastCounter=LastCounterCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool thresholdCamp=firstTent||lastCounter;
                if(thresholdCamp)
                {
                    bool firstArt=firstTent;
                    string family=firstArt?FirstTentVoxelKitLibrary.Family(entity.BlueprintName):LastCounterVoxelKitLibrary.Family(entity.BlueprintName);
                    // Named people retain their body when travelling between
                    // these places. Ground and buildings use only local aliases.
                    if(family==null&&entity.HasTag("Creature"))
                    {firstArt=!firstArt;family=firstArt?FirstTentVoxelKitLibrary.Family(entity.BlueprintName):LastCounterVoxelKitLibrary.Family(entity.BlueprintName);}
                    if(family!=null)
                    {
                        bool actor=entity.HasTag("Creature");
                        if(actor&&!MatchesNativeActorGlyph(entity.BlueprintName,render.RenderString))return Refused(entity,"reskinned-native-actor");
                        bool moving=actor||entity.GetPart<PhysicsPart>()?.Takeable==true;
                        int variant=Variant(moving?null:zone.ZoneID,entity.BlueprintName,entity.ID,moving?0:cell.X,moving?0:cell.Y,4);
                        // The native gameplay camera views from negative Z;
                        // show the sign's authored +Z disclaimer face to it.
                        int turn=family=="sign"?2:0;
                        if(firstArt&&family=="tent")
                        {
                            int neighbors=TentNeighbors(zone,cell);
                            switch(neighbors)
                            {
                                case 3:family="corner";turn=0;break;
                                case 6:family="corner";turn=1;break;
                                case 12:family="corner";turn=2;break;
                                case 9:family="corner";turn=3;break;
                                default:turn=(neighbors&5)!=0&&(neighbors&10)==0?1:0;break;
                            }
                        }
                        string id=firstArt?FirstTentVoxelKitLibrary.ModelId(family,variant):LastCounterVoxelKitLibrary.ModelId(family,variant);
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),moving,!moving,quarterTurns:turn);
                    }
                }
                bool gantry=GantryCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool tine=TineCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool quillhold=QuillholdCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool tally=TallyCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool civicQuartet=gantry||tine||quillhold||tally;
                if(civicQuartet)
                {
                    int kit=gantry?0:tine?1:quillhold?2:3;
                    string family=gantry?GantryVoxelKitLibrary.Family(entity.BlueprintName):tine?TineVoxelKitLibrary.Family(entity.BlueprintName):quillhold?QuillholdVoxelKitLibrary.Family(entity.BlueprintName):TallyVoxelKitLibrary.Family(entity.BlueprintName);
                    // The registrar has one native body wherever they travel.
                    if(family==null&&entity.BlueprintName=="GantryRegistrar"){kit=0;family="registrar";}
                    // A travelling scribe keeps the same body as in Quillhold;
                    // local ground aliases must not recolor an existing person.
                    if(entity.BlueprintName=="Scribe"){kit=2;family="scribe";}
                    if(family!=null)
                    {
                        bool actor=entity.HasTag("Creature");
                        if(actor&&!MatchesNativeActorGlyph(entity.BlueprintName,render.RenderString))return Refused(entity,"reskinned-native-actor");
                        bool moving=actor||entity.GetPart<PhysicsPart>()?.Takeable==true;
                        int v=Variant(moving?null:zone.ZoneID,entity.BlueprintName,entity.ID,moving?0:cell.X,moving?0:cell.Y,4);
                        string id=kit==0?GantryVoxelKitLibrary.ModelId(family,v):kit==1?TineVoxelKitLibrary.ModelId(family,v):kit==2?QuillholdVoxelKitLibrary.ModelId(family,v):TallyVoxelKitLibrary.ModelId(family,v);
                        int turn=0;
                        if(gantry&&family=="wall")
                        {int n=NativeWallNeighbors(zone,cell,"GantryTimberWall");turn=(n&5)!=0&&(n&10)==0?1:0;}
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),moving,!moving,quarterTurns:turn);
                    }
                    if(tine&&entity.BlueprintName=="Reeds")
                        return new SpawnRing3DRecipe(entity,SpreadVoxelLibrary.ModelId("reeds",Variant(zone.ZoneID,entity.BlueprintName,entity.ID,cell.X,cell.Y,4)),null,Village3DProjection.CellCentre(cell.X,cell.Y),false,true);
                    if(tine&&entity.BlueprintName=="BoatFrame")
                        return new SpawnRing3DRecipe(entity,SumpholdVoxelKitLibrary.ModelId("hull",Variant(zone.ZoneID,entity.BlueprintName,entity.ID,cell.X,cell.Y,4)),null,Village3DProjection.CellCentre(cell.X,cell.Y),false,true);
                    if(entity.BlueprintName=="TentRightHost"||entity.BlueprintName=="GuestClothPole")
                    {
                        bool actor=entity.HasTag("Creature");if(actor&&!MatchesNativeActorGlyph(entity.BlueprintName,render.RenderString))return Refused(entity,"reskinned-native-actor");
                        return new SpawnRing3DRecipe(entity,FirstTentVoxelKitLibrary.ModelId(actor?"host":"cloth",Variant(actor?null:zone.ZoneID,entity.BlueprintName,entity.ID,actor?0:cell.X,actor?0:cell.Y,4)),null,Village3DProjection.CellCentre(cell.X,cell.Y),actor,!actor);
                    }
                }
                bool workingTown=cinderhold||sumphold||witnessIntake||thresholdCamp||civicQuartet;
                if(workingTown)
                {
                    if(entity.BlueprintName=="CrunchyLocket"&&render.RenderString=="*")
                    {
                        var objective=entity.GetPart<CavesOfOoo.Storylets.CompleteObjectiveOnTaken>();
                        if(objective?.Quest!="CrunchyLocket"||objective.Objective!="find_locket"||entity.GetPart<PhysicsPart>()?.Takeable!=true)
                            return Refused(entity,"unrecognized-native-quest-token");
                        return new SpawnRing3DRecipe(entity,CinderholdVoxelKitLibrary.ModelId("token",Variant(null,entity.BlueprintName,entity.ID,0,0,4)),
                            null,Village3DProjection.CellCentre(cell.X,cell.Y),true,false);
                    }
                    string ownFamily=cinderhold?CinderholdVoxelKitLibrary.Family(entity.BlueprintName):null;
                    bool post=cinderhold;
                    // Ground and architecture stay local; portable named actors
                    // retain their exact native identity between these two towns.
                    if(sumphold)ownFamily=SumpholdVoxelKitLibrary.Family(entity.BlueprintName);
                    if(ownFamily==null&&entity.BlueprintName=="MarketStall")
                    {post=true;ownFamily="stall";}
                    if(ownFamily==null&&(cinderhold||sumphold)&&entity.HasTag("Creature"))
                    {post=!post;ownFamily=post?CinderholdVoxelKitLibrary.Family(entity.BlueprintName):SumpholdVoxelKitLibrary.Family(entity.BlueprintName);}
                    if(ownFamily!=null)
                    {
                        bool moving=entity.HasTag("Creature")||entity.GetPart<PhysicsPart>()?.Takeable==true;
                        if(entity.HasTag("Creature")&&!MatchesNativeActorGlyph(entity.BlueprintName,render.RenderString))return Refused(entity,"reskinned-native-actor");
                        int v=Variant(moving?null:zone.ZoneID,entity.BlueprintName,entity.ID,moving?0:cell.X,moving?0:cell.Y,4);
                        string id=post?CinderholdVoxelKitLibrary.ModelId(ownFamily,v):SumpholdVoxelKitLibrary.ModelId(ownFamily,v);
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),moving,!moving);
                    }
                }
                bool olderdeep=OlderdeepCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool wellmeet=WellmeetCompositionPlan.IsSupportedZone(zone.ZoneID)||workingTown;
                if(olderdeep||wellmeet)
                {
                    // The canonical Warren now lives at Wellmeet. Retain the
                    // same authored quest-body exception as the other towns,
                    // requiring its actual kill-fact contract, not just a glyph.
                    if(wellmeet&&entity.BlueprintName=="Snapjaw"&&render.RenderString=="g")
                    {
                        var fact=entity.GetPart<CavesOfOoo.Storylets.AddFactWhenSlain>();
                        if(fact?.Fact!="warren_gnomes_routed"||fact.Amount!=1)return Refused(entity,"reskinned-native-actor");
                        return new SpawnRing3DRecipe(entity,WellmeetVoxelLibrary.ModelId("child",2),null,Village3DProjection.CellCentre(cell.X,cell.Y),true,false);
                    }
                    if(olderdeep&&zone.ZoneID=="Overworld.4.6.2"&&entity.BlueprintName=="SandstoneWall")
                        return new SpawnRing3DRecipe(entity,OlderdeepVoxelLibrary.ModelId("wall",Variant(zone.ZoneID,entity.BlueprintName,entity.ID,cell.X,cell.Y,4)),
                            null,Village3DProjection.CellCentre(cell.X,cell.Y),false,true);
                    if(wellmeet&&entity.BlueprintName=="HiddenShrineMarker"&&render.RenderString=="+"
                        &&entity.GetPart<CavesOfOoo.Storylets.QuestMarkerTriggerPart>()?.Fact=="shrine_reached")
                        return new SpawnRing3DRecipe(entity,WellmeetVoxelLibrary.ModelId("shrine",0),null,Village3DProjection.CellCentre(cell.X,cell.Y),false,true);
                    bool founding=olderdeep;
                    string family=founding?OlderdeepVoxelLibrary.Family(entity.BlueprintName):WellmeetVoxelLibrary.Family(entity.BlueprintName);
                    // Exact owner identities remain recognizable across the two
                    // supported areas; art never replays a layout to manufacture them.
                    if(family==null)
                    {founding=!founding;family=founding?OlderdeepVoxelLibrary.Family(entity.BlueprintName):WellmeetVoxelLibrary.Family(entity.BlueprintName);}
                    if(family!=null)
                    {
                        bool actor=entity.HasTag("Creature");
                        bool panicked=entity.BlueprintName=="Villager"&&render.RenderString=="p"
                            &&(entity.GetPart<CavesOfOoo.Storylets.QuestBeaconPart>()?.Quest=="StrongestInOoo"
                                ||entity.GetPart<CavesOfOoo.Storylets.QuestBeaconPart>()?.Quest=="HiddenShrine");
                        if(actor&&!panicked&&!(wellmeet&&MatchesWorkingTownQuestAppearance(entity,render.RenderString))&&!MatchesNativeActorGlyph(entity.BlueprintName,render.RenderString))return Refused(entity,"reskinned-native-actor");
                        bool movable=actor||entity.GetPart<PhysicsPart>()?.Takeable==true;
                        int variant=Variant(movable?null:zone.ZoneID,entity.BlueprintName,entity.ID,movable?0:cell.X,movable?0:cell.Y,4);
                        if(!founding)
                        {
                            if(family=="well")variant=SiteVariant(entity.GetPart<WellSitePart>()?.VisualStage);
                            else if(family=="oven")variant=SiteVariant(entity.GetPart<OvenSitePart>()?.VisualStage);
                            else if(family=="lantern")variant=SiteVariant(entity.GetPart<LanternSitePart>()?.VisualStage);
                            else if(family=="adult")variant=AdultVariant(entity.BlueprintName);
                        }
                        int turn=family=="niche"?NicheOpening(zone,cell):0;
                        if(!founding&&family=="tent")
                        {
                            int neighbors=TentNeighbors(zone,cell);
                            switch(neighbors)
                            {
                                case 3:family="corner";turn=0;break;
                                case 6:family="corner";turn=1;break;
                                case 12:family="corner";turn=2;break;
                                case 9:family="corner";turn=3;break;
                                default:turn=(neighbors&5)!=0&&(neighbors&10)==0?1:0;break;
                            }
                        }
                        string id=founding?OlderdeepVoxelLibrary.ModelId(family,variant):WellmeetVoxelLibrary.ModelId(family,variant);
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),movable,!movable,quarterTurns:turn);
                    }
                }
                bool cathedral=CathedralCompositionPlan.IsSupportedZone(zone.ZoneID);
                bool stillleaf=StillleafCompositionPlan.IsSupportedZone(zone.ZoneID);
                if(cathedral||stillleaf||olderdeep)
                {
                    bool cathedralModel=cathedral;
                    string family=cathedral?CathedralVoxelLibrary.Family(entity.BlueprintName):StillleafVoxelLibrary.Family(entity.BlueprintName);
                    // Portable inhabitants and supplies can cross between the
                    // two stacks without losing their exact native body.
                    if(family==null&&cathedral)
                    {
                        string shared=StillleafVoxelLibrary.Family(entity.BlueprintName);
                        if(shared=="bear"||shared=="slime"||shared=="boots"||shared=="spring")
                        {family=shared;cathedralModel=false;}
                    }
                    else if(family==null&&(stillleaf||olderdeep))
                    {
                        string shared=CathedralVoxelLibrary.Family(entity.BlueprintName);
                        if(shared=="elder"||shared=="tendril"){family=shared;cathedralModel=true;}
                    }
                    if(family!=null)
                    {
                        bool actor=family=="elder"||family=="tendril"||family=="bear"||family=="slime";
                        if(actor&&!MatchesNativeActorGlyph(entity.BlueprintName,render.RenderString))return Refused(entity,"reskinned-native-actor");
                        if(family=="door"&&entity.GetPart<SealedLibraryBarrierPart>()?.IsClosed==false
                            &&entity.GetPart<PhysicsPart>()?.Solid==false)family="open-door";
                        bool movable=actor||entity.GetPart<PhysicsPart>()?.Takeable==true;
                        int variant=Variant(movable?null:zone.ZoneID,entity.BlueprintName,entity.ID,movable?0:cell.X,movable?0:cell.Y,4);
                        string id=cathedralModel?CathedralVoxelLibrary.ModelId(family,variant):StillleafVoxelLibrary.ModelId(family,variant);
                        int facing=family=="door"||family=="open-door"?1:family=="elder"&&cell.Y<12?2:0;
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),movable,!movable,quarterTurns:facing);
                    }
                }
                bool overwrit=OverwritCompositionPlan.IsWildernessZone(zone.ZoneID);
                // These exact native cave identities share their established art
                // in all composed stacks; no area plan is replayed by rendering.
                bool caveArt=GinmereCompositionPlan.IsSupportedZone(zone.ZoneID)||cathedral||stillleaf||olderdeep;
                if(overwrit||caveArt||wellmeet)
                {
                    string family=overwrit?OverwritVoxelLibrary.Family(entity.BlueprintName):GinmereVoxelLibrary.Family(entity.BlueprintName);
                    if(family!=null)
                    {
                        bool actor=family=="frog"||family=="gecko";
                        if(actor&&!MatchesNativeActorGlyph(entity.BlueprintName,render.RenderString))
                            return Refused(entity,"reskinned-native-actor");
                        bool movable=actor||entity.GetPart<PhysicsPart>()?.Takeable==true;
                        int variant=caveArt&&(family=="cliff"||family=="rim")
                            ?NativeHeightVariant(zone,cell,entity.BlueprintName)
                            // Portable bodies keep their shape when carried
                            // between Ginmere levels as well as within a cell grid.
                            :Variant(movable?null:zone.ZoneID,entity.BlueprintName,entity.ID,movable?0:cell.X,movable?0:cell.Y,4);
                        string id=overwrit?OverwritVoxelLibrary.ModelId(family,variant):GinmereVoxelLibrary.ModelId(family,variant);
                        int facing=overwrit&&(family=="bench"||family=="waymarker")?OverwritCompositionPlan.InwardQuarterTurns(zone.ZoneID):0;
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),movable,!movable,quarterTurns:facing);
                    }
                    // Share only the art of current native wreckage. No region
                    // builder is consulted to restore a destroyed object.
                    if(entity.BlueprintName=="Rubble"||(caveArt||witnessIntake||thresholdCamp||civicQuartet)&&entity.BlueprintName=="Bones")
                    {
                        string wreckage=BeatingVoxelLibrary.Family(entity.BlueprintName);
                        string id=BeatingVoxelLibrary.ModelId(wreckage,Variant(zone.ZoneID,entity.BlueprintName,entity.ID,cell.X,cell.Y,4));
                        bool movable=entity.GetPart<PhysicsPart>()?.Takeable==true;
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),movable,!movable);
                    }
                }
                if(StumpCompositionPlan.IsWildernessZone(zone.ZoneID)||stillleaf||olderdeep)
                {
                    // Destruction leaves the same native Rubble identity as
                    // sandstone country, so share its already-coarse body.
                    if(entity.BlueprintName=="Rubble")
                    {
                        string id=BeatingVoxelLibrary.ModelId("rubble",Variant(zone.ZoneID,entity.BlueprintName,entity.ID,cell.X,cell.Y,4));
                        bool movable=entity.GetPart<PhysicsPart>()?.Takeable==true;
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),movable,!movable);
                    }
                    string family=StumpVoxelLibrary.Family(entity.BlueprintName);
                    if(family!=null)
                    {
                        bool actor=family=="singer"||family=="sentinel";
                        if(actor && !MatchesNativeActorGlyph(entity.BlueprintName,render.RenderString))
                            return Refused(entity,"reskinned-native-actor");
                        bool movable=actor||entity.GetPart<PhysicsPart>()?.Takeable==true;
                        bool stableBody=(stillleaf||olderdeep)&&movable;
                        int variant=family=="grain"||family=="dome"
                            ? NativeHeightVariant(zone,cell,entity.BlueprintName)
                            : Variant(stableBody?null:zone.ZoneID,entity.BlueprintName,entity.ID,stableBody?0:cell.X,stableBody?0:cell.Y,4);
                        string id=StumpVoxelLibrary.ModelId(family,variant);
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),movable,!movable);
                    }
                }
                if(BeatingCompositionPlan.IsWildernessZone(zone.ZoneID))
                {
                    string family=BeatingVoxelLibrary.Family(entity.BlueprintName);
                    // Canonical formation is address-based. Only native sand in
                    // a salt pan borrows its pale floor; no terrain is recreated.
                    if(entity.BlueprintName=="Sand" && FormationSelector.For(BiomeType.Beating,zone.ZoneID)==Formation.SaltPan)
                        family="pan";
                    if(family!=null)
                    {
                        int variant=family=="dune"?NativeHeightVariant(zone,cell,"DuneCrest"):Variant(zone.ZoneID,entity.BlueprintName,entity.ID,cell.X,cell.Y,4);
                        string id=BeatingVoxelLibrary.ModelId(family,variant);
                        bool movable=entity.GetPart<PhysicsPart>()?.Takeable==true;
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),movable,!movable);
                    }
                }
                if(SoddenCompositionPlan.IsWildernessZone(zone.ZoneID)||sumphold||drownedLedger)
                {
                    string family=SoddenVoxelLibrary.Family(entity.BlueprintName);
                    if(family!=null)
                    {
                        string id=SoddenVoxelLibrary.ModelId(family,Variant(zone.ZoneID,entity.BlueprintName,entity.ID,cell.X,cell.Y,4));
                        bool movable=entity.GetPart<PhysicsPart>()?.Takeable==true;
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),movable,!movable);
                    }
                }
                if(SpreadCompositionPlan.IsWildernessZone(zone.ZoneID)
                    || entity.BlueprintName=="Emberwheat"
                    || ((SoddenCompositionPlan.IsWildernessZone(zone.ZoneID)||sumphold||drownedLedger) && entity.BlueprintName=="Reeds"))
                {
                    string family=SpreadVoxelLibrary.Family(entity.BlueprintName);
                    if(entity.BlueprintName=="RipeCropRow"&&entity.GetPart<FieldHarvestPart>()?.Harvested==true)
                        family="stubble";
                    if(family!=null)
                    {
                        string id=SpreadVoxelLibrary.ModelId(family,Variant(zone.ZoneID,entity.BlueprintName,entity.ID,cell.X,cell.Y,4));
                        bool movable=entity.GetPart<PhysicsPart>()?.Takeable==true;
                        return new SpawnRing3DRecipe(entity,id,null,Village3DProjection.CellCentre(cell.X,cell.Y),movable,!movable);
                    }
                }
                var binding = catalog.FindBlueprint(entity.HasTag("Player") ? "Player" : entity.BlueprintName);
                // Native cave snapjaw roles share the species' body while their
                // real brains, equipment and glyph-reskin guard remain intact.
                if(binding==null&&caveArt&&(entity.BlueprintName=="SnapjawScavenger"||entity.BlueprintName=="SnapjawHunter"))
                    binding=catalog.FindBlueprint("Snapjaw");
                // Existing green harvestable uses the shipped green plant family;
                // only its appearance is shared, never the Bush entity or mechanics.
                if (binding == null && entity.BlueprintName == "MendleafPlant") binding = catalog.FindBlueprint("Bush");
                // Supported native camps keep their real locked container owner;
                // its existing chest body is only a visual alias.
                if (binding == null && entity.BlueprintName == "LockedChest"
                    && (StumpCompositionPlan.IsWildernessZone(zone.ZoneID)||stillleaf||olderdeep)) binding = catalog.FindBlueprint("Chest");
                if (binding == null && SpreadCompositionPlan.IsWildernessZone(zone.ZoneID))
                    binding = catalog.FindBlueprint(SpreadArtFamily(entity.BlueprintName));
                if (binding == null || binding.models == null || binding.models.Length == 0) return Refused(entity, "unmodeled-native-blueprint");
                if (binding.role == "actor" && !MatchesNativeActorGlyph(binding.blueprint, render.RenderString))
                    return Refused(entity, "reskinned-native-actor");
                // The art kit reserves clean variant zero for the sacred scar;
                // its native stone must not acquire decorative grass or flowers.
                bool cleanScar = zone.ZoneID == FellingSiteBuilder.ZoneID && binding.blueprint == "TepuiStone"
                    && cell.X >= 31 && cell.X <= 48 && cell.Y >= 6 && cell.Y <= 18;
                modelId = binding.models[cleanScar ? 0 : Variant(zone.ZoneID, entity.BlueprintName, entity.ID, cell.X, cell.Y, binding.models.Length)];
                transient = binding.role == "actor" || entity.GetPart<PhysicsPart>()?.Takeable == true;
                batched = !transient;
            }
            return new SpawnRing3DRecipe(entity, modelId, componentId, Village3DProjection.CellCentre(cell.X, cell.Y), transient, batched);
        }
        /// <summary>Portable pilot actors/objects retain their authored body in
        /// another supported presenter. Stationary scenery remains site-bound;
        /// resolving art never installs the pilot layout or changes native state.</summary>
        internal static SpawnRing3DRecipe ResolvePilot(Zone zone, Entity entity, MultiCellPilot3DCatalog pilot)
        {
            var prop = entity?.GetPart<MultiCellPilotPropPart>();
            if (zone == null || prop == null || pilot == null) return Refused(entity, "missing-pilot-contract");
            if (prop.IsStationary && !MultiCellPilotRuntime.IsActive(zone)) return Refused(entity, "outside-active-pilot");
            var cell = zone.GetEntityCell(entity);
            if (cell == null || !cell.Objects.Contains(entity)) return Refused(entity, "not-current-zone-member");
            var render = entity.GetPart<RenderPart>();
            if (render == null || !render.Visible) return Refused(entity, "native-render-hidden");
            var model = pilot.FindModel(prop.ModelId);
            if (model == null || model.blueprint != entity.BlueprintName || model.role != prop.Role
                || entity.GetPart<SpatialFootprintPart>()?.CellsRaw != model.cellsRaw)
                return Refused(entity, "pilot-model-contract-mismatch");
            if (model.role == "actor" && !MatchesNativeActorGlyph(entity.BlueprintName, render.RenderString))
                return Refused(entity, "reskinned-native-actor");
            return new SpawnRing3DRecipe(entity, model.id, prop.OwnerId,
                Village3DProjection.CellCentre(cell.X, cell.Y),
                model.role == "actor" || entity.GetPart<PhysicsPart>()?.Takeable == true, false);
        }
        // Four constant-size native queries give broad landforms a low flank and
        // high core. Current membership/visibility drives the shape; removing a
        // neighbour cannot be undone by replaying a procedural seed.
        private static int NativeHeightVariant(Zone zone,Cell cell,string blueprint)
        {
            int neighbours=0;
            for(int d=0;d<4;d++)
            {
                var adjacent=zone.GetCell(cell.X+(d==0?1:d==1?-1:0),cell.Y+(d==2?1:d==3?-1:0));
                if(adjacent==null)continue;
                foreach(var entity in adjacent.Objects)
                    if(entity.BlueprintName==blueprint&&entity.GetPart<RenderPart>()?.Visible==true)
                    {neighbours++;break;}
            }
            return Math.Max(0,neighbours-1);
        }
        // Packed road uses the established plain ground family.
        private static string SpreadArtFamily(string blueprint)
        {
            switch (blueprint)
            {
                case "RoadStone": return "Floor";
                default: return null;
            }
        }
        // Native applied stage is per owner, so a preview/loaded graph cannot
        // borrow the currently active settlement's state. No stage is invented.
        private static int SiteVariant(RepairStage? stage)
        {
            switch(stage)
            {
                case RepairStage.Fouled:return 0;
                case RepairStage.TemporarilyPurified:return 1;
                case RepairStage.ImprovedWithCaretaker:return 3;
                default:return 2;
            }
        }
        private static int AdultVariant(string blueprint)
        {
            switch(blueprint)
            {
                case "Merchant":case "Quartermaster":case "Innkeeper":return 0;
                case "Scribe":return 1;
                case "Tinker":return 2;
                default:return 3;
            }
        }
        // N/E/S/W mask from live wall owners. Destroyed or hidden neighbours
        // never survive as authored corners, and each cell keeps one owner.
        private static int NativeWallNeighbors(Zone zone,Cell cell,string blueprint)
        {
            int mask=0;
            var n=zone.GetCell(cell.X,cell.Y-1);if(n!=null)foreach(var e in n.Objects)if(e.BlueprintName==blueprint){mask|=1;break;}
            n=zone.GetCell(cell.X+1,cell.Y);if(n!=null)foreach(var e in n.Objects)if(e.BlueprintName==blueprint){mask|=2;break;}
            n=zone.GetCell(cell.X,cell.Y+1);if(n!=null)foreach(var e in n.Objects)if(e.BlueprintName==blueprint){mask|=4;break;}
            n=zone.GetCell(cell.X-1,cell.Y);if(n!=null)foreach(var e in n.Objects)if(e.BlueprintName==blueprint){mask|=8;break;}
            return mask;
        }

        private static int TentNeighbors(Zone zone,Cell cell)
        {
            int mask=0;
            for(int d=0;d<4;d++)
            {
                var at=zone.GetCell(cell.X+(d==1?1:d==3?-1:0),cell.Y+(d==0?-1:d==2?1:0));
                if(at==null)continue;
                foreach(var e in at.Objects)if(e.BlueprintName=="TentWall"&&e.GetPart<RenderPart>()?.Visible==true)
                {mask|=1<<d;break;}
            }
            return mask;
        }
        // Four native neighbour queries orient an open niche away from backing
        // rock. A destroyed wall changes the pose without reconstructing a room.
        private static int NicheOpening(Zone zone,Cell cell)
        {
            for(int d=0;d<4;d++)
            {
                var adjacent=zone.GetCell(cell.X+(d==2?-1:d==3?1:0),cell.Y+(d==0?-1:d==1?1:0));
                if(adjacent==null)continue;
                foreach(var owner in adjacent.Objects)
                    if(owner.HasTag("Wall")&&owner.GetPart<RenderPart>()?.Visible==true)
                        return d==0?2:d==1?0:d==2?1:3;
            }
            return 0;
        }
        // Native village quests deliberately reskin generic Villager owners.
        // Both conversation and quest identity distinguish those from unrelated
        // content that should keep its ordinary render fallback.
        private static bool MatchesWorkingTownQuestAppearance(Entity owner,string glyph)
        {
            if(owner.BlueprintName!="Villager")return false;
            string conversation=owner.GetPart<ConversationPart>()?.ConversationID;
            string quest=owner.GetPart<CavesOfOoo.Storylets.QuestBeaconPart>()?.Quest;
            switch(glyph)
            {
                case "b":return conversation=="Baker_Quest"&&quest=="MessageForHermit";
                case "h":return conversation=="Hermit_Quest";
                case "f":return conversation=="Warren_Quest"&&quest=="ClearTheWarren";
                case "P":return conversation=="CandyTax_Quest"&&quest=="TheCandyTax";
                case "c":return conversation=="CandyCitizen"||conversation=="Crunchy_Quest"&&quest=="CrunchyLocket";
                default:return false;
            }
        }
        private static bool MatchesNativeActorGlyph(string blueprint, string glyph)
        {
            char canonical = EnvironmentSpriteRenderer.NamedActorCanonicalGlyph(blueprint);
            if (canonical == '\0')
            {
                // These four have no named sprite row. Exact shipped RenderString
                // values are pinned by the all-species recipe positive controls.
                switch (blueprint)
                {
                    case "Player": case "EncasedElder":
                    case "ConcordFactor":case "Weaponsmith":case "PeatCutter":
                    case "RecensionScribe":case "CurationSorter":case "FilerClerk":case "GantryRegistrar":
                    case "FoundingListener":case "FoundingPlaqueTender":
                    case "TentRightHost":case "SaltMaster":case "Elder":case "Innkeeper":
                    case "Merchant":case "Quartermaster":case "Scribe":case "Tinker":
                    case "Villager":case "Warden":case "Farmer":case "WellKeeper": canonical = '@'; break;
                    case "VillageChild":canonical='c';break;
                    case "Snapjaw": canonical = 's'; break;
                    case "Shambler": canonical = 'z'; break;
                    case "MawToad": canonical = 't'; break;
                }
            }
            return canonical != '\0' && !string.IsNullOrEmpty(glyph) && glyph[0] == canonical;
        }
        private static SpawnRing3DRecipe Refused(Entity entity, string reason)
            => new SpawnRing3DRecipe(entity, null, null, default, false, false, reason);

        /// <summary>Permanent water is represented independently of pool objects.
        /// A removed saved coating stays absent; temporary effects keep their native
        /// overlay. This method never reconstructs a seed or writes a coating.</summary>
        public static bool HasPermanentWater(Zone zone, int x, int y)
        {
            var state = zone?.TileState.Get(x, y);
            if (state == null) return false;
            foreach (var layer in state.Coatings)
                if (layer.Id == "water" && layer.Turns == ZoneTileState.Permanent) return true;
            return false;
        }
        private static int Variant(string zone, string blueprint, string id, int x, int y, int count)
        {
            // Explicit FNV-1a, not randomized string.GetHashCode or simulation RNG.
            unchecked
            {
                uint value = 2166136261;
                Hash(ref value, zone); Hash(ref value, blueprint); Hash(ref value, id);
                value = (value ^ (uint)x) * 16777619; value = (value ^ (uint)y) * 16777619;
                return (int)(value % (uint)count);
            }
        }
        private static void Hash(ref uint value, string text)
        {
            unchecked
            {
                if (text != null) foreach (char c in text) value = (value ^ c) * 16777619;
                value = (value ^ 0xff) * 16777619;
            }
        }
    }
}
