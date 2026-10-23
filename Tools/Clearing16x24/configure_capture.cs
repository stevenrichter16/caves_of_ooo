// Presentation-only capture setup. Run restore_capture.cs after all captures.
var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
var asm=typeof(UnityEditor.Editor).Assembly;
var viewType=asm.GetType("UnityEditor.GameView",true);var view=EditorWindow.GetWindow(viewType);
var selected=viewType.GetProperty("selectedSizeIndex",flags);
if(!SessionState.GetBool("ClearingCapture.Active",false))SessionState.SetInt("ClearingCapture.OldIndex",(int)selected.GetValue(view));
var sizesType=asm.GetType("UnityEditor.GameViewSizes",true);
var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
var sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
var groupType=asm.GetType("UnityEditor.GameViewSizeGroupType",true);
var group=sizesType.GetMethod("GetGroup",flags).Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});
int built=(int)group.GetType().GetMethod("GetBuiltinCount",flags).Invoke(group,null),custom=(int)group.GetType().GetMethod("GetCustomCount",flags).Invoke(group,null),chosen=-1;
for(int i=0;i<built+custom;i++){
 var size=group.GetType().GetMethod("GetGameViewSize",flags).Invoke(group,new object[]{i});
 if((int)size.GetType().GetProperty("width",flags).GetValue(size)==1280&&(int)size.GetType().GetProperty("height",flags).GetValue(size)==800){chosen=i;break;}
}
if(!SessionState.GetBool("ClearingCapture.Active",false))SessionState.SetInt("ClearingCapture.AddedIndex",-1);
if(chosen<0){
 var sizeType=asm.GetType("UnityEditor.GameViewSize",true);var enumType=asm.GetType("UnityEditor.GameViewSizeType",true);
 var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.Parse(enumType,"FixedResolution"),1280,800,"Clearing art capture"},null);
 group.GetType().GetMethod("AddCustomSize",flags).Invoke(group,new[]{size});chosen=built+custom;SessionState.SetInt("ClearingCapture.AddedIndex",custom);
}
SessionState.SetBool("ClearingCapture.Active",true);selected.SetValue(view,chosen);view.Show();view.Focus();view.Repaint();EditorApplication.QueuePlayerLoopUpdate();
return new{sizeIndex=chosen,width=1280,height=800,oldIndex=SessionState.GetInt("ClearingCapture.OldIndex",0)};
