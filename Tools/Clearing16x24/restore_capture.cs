if(SessionState.GetBool("ClearingCapture.Active",false)){
 var type=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView",true);
 var view=EditorWindow.GetWindow(type);
 int added=SessionState.GetInt("ClearingCapture.AddedIndex",-1);
 if(added>=0){
  var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
  var asm=typeof(UnityEditor.Editor).Assembly;var sizesType=asm.GetType("UnityEditor.GameViewSizes",true);var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
  var sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);var groupType=asm.GetType("UnityEditor.GameViewSizeGroupType",true);
  var group=sizesType.GetMethod("GetGroup",flags).Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});group.GetType().GetMethod("RemoveCustomSize",flags).Invoke(group,new object[]{added});
 }
 type.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,SessionState.GetInt("ClearingCapture.OldIndex",0));
 view.Repaint();SessionState.SetBool("ClearingCapture.Active",false);
}
return "Game view selection restored.";
