"""Build review-only native audit drafts from the captured baseline, never Assets."""
from pathlib import Path
import difflib

HERE = Path(__file__).resolve().parent
source = (HERE / 'StarterSpell3DNativeAudit.cs.baseline').read_text()

def splice(old, new):
    global source
    assert source.count(old) == 1, (old[:100], source.count(old))
    source = source.replace(old, new)

splice('ZoneTileState fixtureGroundBaseline; (int x,int y) lane;',
       'ZoneTileState fixtureGroundBaseline; (int x,int y) lane; int directionX=1,directionY;')
splice('yield return CommandCase(0,"south-showcase",true);yield return ProfilePair();RemoveFixtures();',
       'yield return CommandCase(0,"south-showcase",true);yield return ProfilePair();yield return DirectionalMatrix();RemoveFixtures();')
splice('var source=input.CurrentZone.GetEntityCell(input.PlayerEntity);var selected=input.CurrentZone.GetCell(source.X+1,source.Y);\n            bool consumed=input.PlayerEntity.GetPart<SkillsPart>().TryRouteSkillCommand(ability.Command,input.CurrentZone,rng,1,0,source,selected,ability.Range,out bool blocks);',
'''var source=input.CurrentZone.GetEntityCell(input.PlayerEntity);var selected=input.CurrentZone.GetCell(source.X+directionX,source.Y+directionY);
            var intended=fixtureZone.GetEntityPosition(dummy);bool sourceVisible=source.IsVisible&&source.Explored;
            bool targetVisible=fixtureZone.GetCell(intended.x,intended.y).IsVisible&&fixtureZone.GetCell(intended.x,intended.y).Explored;
            if(mode.StartsWith("direction-",StringComparison.Ordinal))
            {
                Require(sourceVisible&&targetVisible,"Directional source and actual recipient must both be in ordinary live FOV.");
                Require(ProjectionRoundTrips(source.X,source.Y)&&ProjectionRoundTrips(intended.x,intended.y),"Directional simulation cells must agree with the native XZ projection.");
            }
            bool consumed=input.PlayerEntity.GetPart<SkillsPart>().TryRouteSkillCommand(ability.Command,input.CurrentZone,rng,directionX,directionY,source,selected,ability.Range,out bool blocks);''')
splice('sourceX=sequence.Source.X,sourceY=sequence.Source.Y,\n                targets=',
'''sourceX=sequence.Source.X,sourceY=sequence.Source.Y,
                directionX=directionX,directionY=directionY,intendedX=intended.x,intendedY=intended.y,
                finalX=fixtureZone.GetEntityPosition(dummy).x,finalY=fixtureZone.GetEntityPosition(dummy).y,sourceVisible=sourceVisible,targetVisible=targetVisible,
                targets=''')
splice('if(mode=="showcase"||mode=="south-showcase")',
       'if(mode=="showcase"||mode=="south-showcase"||mode.StartsWith("direction-",StringComparison.Ordinal))')
splice('            bool observeConditional=(index==4||index==5)&&mode!="profile";',
'''            if(mode.StartsWith("direction-",StringComparison.Ordinal))
            {
                Require(sequence.Targets.Count==1&&sequence.Targets[0].TargetId==dummy.ID,"Directional corridor may hit only its owned recipient, including the Jet cone's side cells.");
                var contact=sequence.Targets[0];int push=index==2||index==3?1:0;
                Require(contact.Cell.X==intended.x&&contact.Cell.Y==intended.y
                    &&contact.FinalCell.X==intended.x+directionX*push&&contact.FinalCell.Y==intended.y+directionY*push,
                    "Actual copied contact and displacement must follow the requested native direction.");
                Require(ProjectionRoundTrips(contact.FinalCell.X,contact.FinalCell.Y),"Final copied contact must remain in the native cell projection.");
                int pathLength=index==2?2:index==3?4:3;
                Require(row.path.SequenceEqual(Enumerable.Range(1,pathLength).Select(n=>(source.X+n*directionX)+","+(source.Y+n*directionY))),"Directional path must contain exactly the real ray, without an east-only fallback.");
                if(index==2)Require(new HashSet<string>(row.affected).SetEquals(JetCells(source.X,source.Y,directionX,directionY).Concat(new[]{contact.FinalCell.X+","+contact.FinalCell.Y})),"Directional Jet must retain its four-cell cone plus the actual pushed wet-ground cell.");
            }
            bool observeConditional=(index==4||index==5)&&mode!="profile";''')
splice('Require(fixtureZone.AddEntity(dummy,lane.x+3,lane.y),"Fixture recipient placement must be real.");',
       'Require(fixtureZone.AddEntity(dummy,lane.x+3*directionX,lane.y+3*directionY),"Fixture recipient placement must be real.");')
splice('Require(fixtureZone.AddEntity(crop,lane.x-1,lane.y+1),"Fixture crop placement must be real.");',
       'Require(fixtureZone.AddEntity(crop,lane.x-directionX-directionY,lane.y-directionY+directionX),"Fixture crop placement must be real.");')
splice('Require(fixtureZone.AddEntity(dummy,lane.x+(spell==1?1:spell==2?2:3),lane.y),"Reset target must occupy its exact actual cell.");',
       'int distance=spell==1?1:spell==2?2:3;Require(fixtureZone.AddEntity(dummy,lane.x+distance*directionX,lane.y+distance*directionY),"Reset target must occupy its exact actual cell.");')
splice('Require(fixtureZone.AddEntity(crop,lane.x-1,lane.y+1),"Restore owned crop membership.");',
       'Require(fixtureZone.AddEntity(crop,lane.x-directionX-directionY,lane.y-directionY+directionX),"Restore owned crop membership.");')
splice('''            // Only the bounded command-fixture lane returns to its original writing.
            // Other cells, terrain sources and current spell writes remain authoritative.
            // Steady profile phases deliberately retain ordinary accumulating reactions.
            for(int y=lane.y-3;y<=lane.y+3;y++)for(int x=lane.x-3;x<=lane.x+5;x++)
            {
                var original=fixtureGroundBaseline.Get(x,y);var current=fixtureZone.TileState.Get(x,y);
                if(JsonUtility.ToJson(original)==JsonUtility.ToJson(current))continue;
                fixtureZone.TileState.Clear(x,y);
                if(original==null)continue;
                foreach(var layer in original.Coatings)fixtureZone.TileState.WriteCoating(x,y,layer.Id,layer.Turns);
                foreach(var layer in original.Residues)fixtureZone.TileState.WriteResidue(x,y,layer.Id,layer.Turns);
                fixtureZone.TileState.AddHeat(x,y,original.Heat);fixtureZone.TileState.AddCold(x,y,original.Cold);fixtureZone.TileState.AddCharge(x,y,original.Charge);
                fixtureZone.TileState.WriteCloud(x,y,original.Cloud,original.CloudTurns);
                Require(JsonUtility.ToJson(original)==JsonUtility.ToJson(fixtureZone.TileState.Get(x,y)),"Restore the original fixture-lane ground exactly.");
            }''',
'''            RestoreFixtureGround(fixtureZone,fixtureGroundBaseline,FixtureGroundBounds(lane.x,lane.y,directionX,directionY));''')

insert = r'''
        // These methods belong only to the explicit acceptance scenario. Ordinary
        // gameplay never selects, clears, relocates or acquires these fixtures.
        IEnumerator DirectionalMatrix()
        {
            // Keep all four original profile phases and their east-facing workloads
            // before this matrix. Capturing images is never part of profile timing.
            foreach(var direction in new[]{(name:"north",dx:0,dy:-1),(name:"south",dx:0,dy:1),(name:"northeast",dx:1,dy:-1)})
            {
                yield return WaitForFx();yield return WaitForIdleGesture();
                // Restore while the previous lane and snapshot still belong together.
                // Only owned dummy/crop membership is removed; never erase scenery.
                ResetFixtureGround();RemoveFixtures();
                directionX=direction.dx;directionY=direction.dy;SpellFxSettings.Mode=SpellFxMode.Full;
                lane=FindDirectionalLane(input.CurrentZone,directionX,directionY);
                FixturePlace(input.CurrentZone,lane);yield return WaitForNative();
                PrepareFixtures();input.ZoneRenderer.RenderZone();yield return null;
                foreach(int index in new[]{0,2,3,4,5})yield return CommandCase(index,"direction-"+direction.name,true);
            }
            yield return WaitForFx();yield return WaitForIdleGesture();ResetFixtureGround();RemoveFixtures();
            directionX=1;directionY=0;
            bool pass=HasDirectionalEvidence(casts.ToArray());
            Check("native_direction_matrix",pass,"15 actual command casts: Ember, Jet, Surge, Rime, Calm × north/south/northeast, with live FOV, contact, displacement, geometry, gesture and bounded cleanup.");
            Require(pass,"Directional native matrix is incomplete or inconsistent with actual copied outcomes.");
        }
        (int x,int y) FindDirectionalLane(Zone zone,int dx,int dy)
        {
            for(int distance=0;distance<Zone.Width+Zone.Height;distance++)for(int y=3;y<Zone.Height-3;y++)for(int x=3;x<Zone.Width-3;x++)
                if(Math.Abs(x-40)+Math.Abs(y-12)==distance&&CanUseDirectionalLane(zone,input.PlayerEntity,x,y,dx,dy))return(x,y);
            throw new InvalidOperationException("No clear native directional corridor for ("+dx+","+dy+"); audit refuses to erase authored scenery or use a clipped lane.");
        }
        static bool CanUseDirectionalLane(Zone zone,Entity actor,int x,int y,int dx,int dy)
        {
            if(zone==null||actor==null||Math.Abs(dx)>1||Math.Abs(dy)>1||(dx==0&&dy==0))return false;
            var bounds=FixtureGroundBounds(x,y,dx,dy);
            if(!zone.InBounds(bounds.xMin,bounds.yMin)||!zone.InBounds(bounds.xMax-1,bounds.yMax-1))return false;
            // The full one-cell margin contains Jet's real diagonal side cells as
            // well as the center ray, target and both push destinations. Occupants
            // includes off-anchor footprint contacts, not just canonical entities.
            for(int step=-1;step<=5;step++)for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)
            {
                var cell=zone.GetCell(x+step*dx+ox,y+step*dy+oy);
                if(cell==null||cell.IsSolid()||cell.BlocksMovement(actor)
                    ||cell.Occupants.Any(e=>e!=actor&&AbilityTargeting.IsElementalTarget(e,actor)))return false;
            }
            for(int cy=y-3;cy<=y+3;cy++)for(int cx=x-3;cx<=x+3;cx++)
                if(zone.GetCell(cx,cy).Occupants.Any(e=>e.HasPart<CropPart>()))return false;
            return true;
        }
        static RectInt FixtureGroundBounds(int x,int y,int dx,int dy)
        {
            int minX=x+Math.Min(-3,5*dx),maxX=x+Math.Max(3,5*dx);
            int minY=y+Math.Min(-3,5*dy),maxY=y+Math.Max(3,5*dy);
            return new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
        }
        static void RestoreFixtureGround(Zone zone,ZoneTileState baseline,RectInt bounds)
        {
            Require(zone!=null&&baseline!=null&&zone.InBounds(bounds.xMin,bounds.yMin)&&zone.InBounds(bounds.xMax-1,bounds.yMax-1),"Only an owned in-bounds fixture snapshot can be restored.");
            // Preserve every entity and all writing outside the old lane. Original
            // east bounds remain exactly x[-3,+5], y[-3,+3]. Profiles still accumulate.
            for(int y=bounds.yMin;y<bounds.yMax;y++)for(int x=bounds.xMin;x<bounds.xMax;x++)
            {
                var original=baseline.Get(x,y);var current=zone.TileState.Get(x,y);
                if(JsonUtility.ToJson(original)==JsonUtility.ToJson(current))continue;
                zone.TileState.Clear(x,y);
                if(original!=null)
                {
                    foreach(var layer in original.Coatings)zone.TileState.WriteCoating(x,y,layer.Id,layer.Turns);
                    foreach(var layer in original.Residues)zone.TileState.WriteResidue(x,y,layer.Id,layer.Turns);
                    zone.TileState.AddHeat(x,y,original.Heat);zone.TileState.AddCold(x,y,original.Cold);zone.TileState.AddCharge(x,y,original.Charge);
                    zone.TileState.WriteCloud(x,y,original.Cloud,original.CloudTurns);
                }
                Require(JsonUtility.ToJson(original)==JsonUtility.ToJson(zone.TileState.Get(x,y)),"Restore the original fixture-lane ground exactly.");
            }
        }
        static bool ProjectionRoundTrips(int x,int y)
        {
            if(x<0||x>=Zone.Width||y<0||y>=Zone.Height)return false;
            return Village3DProjection.TryWorldToCell(Village3DProjection.CellCentre(x,y),out int actualX,out int actualY)&&x==actualX&&y==actualY;
        }
        static IEnumerable<string> JetCells(int x,int y,int dx,int dy)
        {
            yield return (x+dx)+","+(y+dy);
            for(int off=-1;off<=1;off++)yield return (x+2*dx-dy*off)+","+(y+2*dy+dx*off);
        }
        /// <summary>Audit-only metadata gate shared by the native launcher. It
        /// rejects relabelled east casts, missing directions and neutral-only art;
        /// screenshots and hashes still pass the launcher's separate file gate.</summary>
        public static bool HasDirectionalEvidence(CastRow[] rows)
        {
            if(rows==null)return false;
            var directional=rows.Where(r=>r!=null&&r.mode!=null&&r.mode.StartsWith("direction-",StringComparison.Ordinal)).ToArray();
            if(directional.Length!=15)return false;
            foreach(var direction in new[]{(name:"north",dx:0,dy:-1),(name:"south",dx:0,dy:1),(name:"northeast",dx:1,dy:-1)})
                foreach(int index in new[]{0,2,3,4,5})
                {
                    var matches=directional.Where(r=>r.mode=="direction-"+direction.name&&r.spell==SpellIds[index]).ToArray();
                    if(matches.Length!=1)return false;var r=matches[0];int distance=index==2?2:3,push=index==2||index==3?1:0;
                    int tx=r.sourceX+distance*direction.dx,ty=r.sourceY+distance*direction.dy;
                    int fx=tx+push*direction.dx,fy=ty+push*direction.dy;
                    if(r.zoneId!="Overworld.3.7.0"||r.fxMode!="Full"||r.directionX!=direction.dx||r.directionY!=direction.dy
                        ||!r.sourceVisible||!r.targetVisible||!ProjectionRoundTrips(r.sourceX,r.sourceY)||!ProjectionRoundTrips(tx,ty)||!ProjectionRoundTrips(fx,fy)
                        ||r.intendedX!=tx||r.intendedY!=ty||r.finalX!=fx||r.finalY!=fy||r.targets!=1
                        ||r.targetCells==null||r.targetCells.Length!=1||r.targetCells[0]!=tx+","+ty+"->"+fx+","+fy
                        ||r.copiedTargetIds==null||r.copiedTargetIds.Length!=1||string.IsNullOrEmpty(r.copiedTargetIds[0])
                        ||r.nativeEntry!=r.spell||!r.pass||!r.resultStable||r.cooldown<=0||r.peakMeshes<=0||!r.groundFixtureReset
                        ||!r.sawGesture||!r.gestureRestored||r.gestureSamples<2||float.IsNaN(r.gesturePeakDegrees)||float.IsInfinity(r.gesturePeakDegrees)||r.gesturePeakDegrees<=3
                        ||double.IsNaN(r.seconds)||double.IsInfinity(r.seconds)||r.seconds<1.2||r.seconds>=4||r.frames==null||r.frames.Count<4)return false;
                    int length=index==2?2:index==3?4:3;
                    if(r.path==null||!r.path.SequenceEqual(Enumerable.Range(1,length).Select(n=>(r.sourceX+n*direction.dx)+","+(r.sourceY+n*direction.dy))))return false;
                    if(index!=5&&r.damage<=0||index==5&&r.damage!=0)return false;
                    if(index==2&&(r.applied==null||!r.applied.Contains(nameof(WetEffect))||r.affected==null||!new HashSet<string>(r.affected).SetEquals(JetCells(r.sourceX,r.sourceY,direction.dx,direction.dy).Concat(new[]{fx+","+fy}))))return false;
                    if(index==4||index==5)
                    {
                        string status=index==4?nameof(FrozenEffect):"Pacified";
                        if(r.applied==null||!r.applied.Contains(status)||r.observedConditionalMeshes==null||r.observedConditionalMeshes.Length==0)return false;
                    }
                }
            return true;
        }
'''
splice('        void FixturePlace(Zone zone,(int x,int y) at)',insert+'\n        void FixturePlace(Zone zone,(int x,int y) at)')
splice('public int sourceX,sourceY,targets,damage,cooldown,rngCalls,peakMeshes,gestureSamples;',
       'public int sourceX,sourceY,directionX,directionY,intendedX,intendedY,finalX,finalY,targets,damage,cooldown,rngCalls,peakMeshes,gestureSamples;')
splice('public bool rejectedFrozen,rejectedPacified,resultStable,pass,sawGesture,gestureRestored,groundFixtureReset;',
       'public bool rejectedFrozen,rejectedPacified,resultStable,pass,sawGesture,gestureRestored,groundFixtureReset,sourceVisible,targetVisible;')
splice('World simulation does not advance during command-fixture playback.',
       'After all profile phases,15 additional Full-mode command fixtures capture Ember/Jet/Surge/Rime/Calm facing north,south,northeast in clear native south-chunk corridors. Source/recipient live FOV, actual direction/contact/push and XZ cell projection are asserted; no authored scenery is erased. World simulation does not advance during command-fixture playback.')
(HERE / 'StarterSpell3DNativeAudit.cs').write_text(source)

batch=(HERE/'StarterSpell3DNativeAuditBatch.cs.baseline').read_text()
old='if(f.casts==null||f.casts.Length<40||f.casts.Any(c=>c==null||!c.pass||!c.resultStable||c.cooldown<=0))return false;'
assert batch.count(old)==1
batch=batch.replace(old,old+'\n                if(!f.checks.Any(c=>c.name=="native_direction_matrix"&&c.pass)||!StarterSpell3DNativeAudit.HasDirectionalEvidence(f.casts))return false;')
(HERE/'StarterSpell3DNativeAuditBatch.cs').write_text(batch)
patch=[]
for name,target in [('StarterSpell3DNativeAudit.cs','Assets/Scripts/Scenarios/Custom/'),('StarterSpell3DNativeAuditBatch.cs','Assets/Editor/Scenarios/')]:
    patch.extend(difflib.unified_diff((HERE/(name+'.baseline')).read_text().splitlines(True),(HERE/name).read_text().splitlines(True),fromfile='a/'+target+name,tofile='b/'+target+name))
(HERE/'directional-audit.patch').write_text(''.join(patch))
