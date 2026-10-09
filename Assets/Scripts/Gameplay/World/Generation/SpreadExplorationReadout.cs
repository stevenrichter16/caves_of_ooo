namespace CavesOfOoo.Core
{
 /// <summary>Local live-state descriptions only. No remote lookup, prediction or stock repair.</summary>
 public static class SpreadExplorationReadout
 {
  public static string Describe(Entity owner)
  {
   var zone=owner?.SpatialZone;
   if(!SpreadActorContext.Ground(owner,zone))return null;
   if(owner.BlueprintName=="SpreadDrawPoint")
   {
    var pool=owner.GetPart<LiquidPoolPart>();var physics=owner.GetPart<PhysicsPart>();
    if(pool==null||pool.ParentEntity!=owner||physics.Takeable||pool.LiquidId!="water"||pool.Volume<0)return null;
    return pool.Volume==0?"The draw point is empty. The surrounding wet ground supplies no further drinks.":"The draw point holds "+pool.Volume+" drink"+(pool.Volume==1?"":"s")+" of water. Drawing it reduces that supply.";
   }
   if(!SpreadActorContext.Actor(owner,zone,out _))return null;
   var hunt=owner.GetPart<SpreadPredatorPart>()?.DescribeState();
   if(!string.IsNullOrEmpty(hunt))return hunt;
   var grazer=owner.GetPart<SpreadGrazerPart>();
   if(grazer?.ParentEntity==owner&&grazer.Configured&&grazer.ZoneID==zone.ZoneID)
   {
    // Spent allowance is saved history, not a promise that its old row remains.
    if(grazer.Fed)return "It has cropped its fill and leaves the remaining wheat alone.";
    if(grazer.Food!=grazer.ReservedRow&&RipeAt(grazer.Food,zone,grazer.FoodX,grazer.FoodY)
      &&RipeAt(grazer.ReservedRow,zone,grazer.ReservedX,grazer.ReservedY))
     return "It noses toward a particular ripe patch and shies from close approach.";
   }
   var territory=owner.GetPart<SpreadTerritoryPart>();
   if(territory?.ParentEntity==owner&&territory.Configured&&territory.ZoneID==zone.ZoneID
     &&territory.GraceTurns>=1&&territory.GraceTurns<=6&&SpreadActorContext.Ground(territory.Post,zone)&&!territory.Post.HasTag("Creature"))
   {
    var post=zone.GetEntityCell(territory.Post);
    if(post.X==territory.PostX&&post.Y==territory.PostY&&post.X>=territory.Left&&post.X<=territory.Right
      &&post.Y>=territory.Top&&post.Y<=territory.Bottom)
    {
     var box=territory.Post.GetPart<ContainerPart>();
     if(box?.ParentEntity==territory.Post&&(territory.Post.BlueprintName=="Crate"||territory.Post.BlueprintName=="Sack"))
      return "It guards the ground around that "+(territory.Post.BlueprintName=="Crate"?"crate":"sack")+". Heed its warning and withdraw to avoid a fight; an attack is remembered.";
     return "It keeps to the ground around its work post. Heed its warning and withdraw to avoid a fight; an attack is remembered.";
    }
   }
   return null;
  }
  static bool RipeAt(Entity row,Zone zone,int x,int y)
  {
   if(!SpreadActorContext.Ground(row,zone)||row.BlueprintName!="RipeCropRow")return false;
   var field=row.GetPart<FieldHarvestPart>();var cell=zone.GetEntityCell(row);
   return cell.X==x&&cell.Y==y&&field!=null&&field.ParentEntity==row&&!field.Harvested
    &&row.GetPart<RenderPart>()?.ParentEntity==row&&row.GetPart<ExaminablePart>()?.ParentEntity==row;
  }
 }
}
