using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json;
using CavesOfOoo.Core;
namespace CavesOfOoo.Core {
 public class GameEvent { }
 public enum EntityVisualFacing { South, North }
 public class Part { public virtual string Name=>""; public virtual bool HandleEvent(GameEvent e)=>true; }
}
public sealed class W {
 readonly BinaryWriter b;
 public W(Stream stream) { b=new BinaryWriter(stream); }
 public void S(string value) { b.Write(value!=null); if(value!=null)b.Write(value); }
 public void I(int value)=>b.Write(value);
 public void B(bool value)=>b.Write(value);
 public void Raw(byte[] value)=>b.Write(value);
}
public class Program {
 static FieldInfo[] fields=typeof(RenderPart).GetFields(BindingFlags.Instance|BindingFlags.Public);
 static void Dynamic(Type t, object v, W w) {
  var nullable=Nullable.GetUnderlyingType(t); if(nullable!=null){w.B(v!=null);if(v!=null)Dynamic(nullable,v,w);return;}
  if(t==typeof(int))w.I((int)v);
  else if(t==typeof(long)||t==typeof(float)||t==typeof(double))throw new NotSupportedException();
  else if(t==typeof(bool))w.B((bool)v);
  else if(t==typeof(char))throw new NotSupportedException();
  else if(t==typeof(string))w.S((string)v);
  else if(t==typeof(Guid))w.S(((Guid)v).ToString("N"));
  else if(t.IsEnum)w.I(Convert.ToInt32(v));
  else throw new NotSupportedException(t.Name);
 }
 static Action<object,W> ValueWriter(FieldInfo f) {
  if(f.FieldType==typeof(string))return (o,w)=>w.S((string)f.GetValue(o));
  if(f.FieldType==typeof(int))return (o,w)=>w.I((int)f.GetValue(o));
  if(f.FieldType==typeof(bool))return (o,w)=>w.B((bool)f.GetValue(o));
  if(f.FieldType.IsEnum)return (o,w)=>w.I(Convert.ToInt32(f.GetValue(o)));
  throw new NotSupportedException();
 }
 static Action<object,W> CompiledWriter(FieldInfo f) {
  var o=Expression.Parameter(typeof(object));var w=Expression.Parameter(typeof(W));
  Expression access=Expression.Field(Expression.Convert(o,typeof(RenderPart)),f);
  string method=f.FieldType==typeof(string)?"S":f.FieldType==typeof(bool)?"B":"I";
  if(f.FieldType.IsEnum)access=Expression.Convert(access,typeof(int));
  return Expression.Lambda<Action<object,W>>(Expression.Call(w,typeof(W).GetMethod(method),access),o,w).Compile();
 }
 static byte[] Encoded(string s){using var m=new MemoryStream();new W(m).S(s);return m.ToArray();}
 static void Main() {
  const int count=45596;
  var rows=new List<object>();var cold=new Dictionary<string,double>();
  var sw=Stopwatch.StartNew();var writers=fields.Select(ValueWriter).ToArray();cold["descriptorBuildMs"]=sw.Elapsed.TotalMilliseconds;
  sw.Restart();var typed=fields.Select(CompiledWriter).ToArray();cold["expressionBuildMs"]=sw.Elapsed.TotalMilliseconds;
  sw.Restart();var names=fields.Select(f=>Encoded(f.Name)).ToArray();cold["nameEncodingMs"]=sw.Elapsed.TotalMilliseconds;
  var entities=Enumerable.Range(0,count).Select(i=>new RenderPart {DisplayName="floor "+(i%4),RenderString=i%2==0?".":"#",ColorString="&y",Tile=null,RenderLayer=i%4,Visible=i%3!=0}).ToArray();
  var modes=new[]{"baseline","descriptor","typed","typed_encoded"};
  byte[] expected=null;
  using var buffer=new MemoryStream(20_000_000);
  foreach(var mode in modes){
   for(int sample=-1;sample<5;sample++){
    buffer.Position=0;buffer.SetLength(0);var w=new W(buffer);long alloc=GC.GetAllocatedBytesForCurrentThread();sw.Restart();
    foreach(var entity in entities){w.I(fields.Length);for(int i=0;i<fields.Length;i++){
     if(mode=="typed_encoded")w.Raw(names[i]);else w.S(fields[i].Name);
     if(mode=="baseline")Dynamic(fields[i].FieldType,fields[i].GetValue(entity),w);
     else if(mode=="descriptor")writers[i](entity,w);
     else typed[i](entity,w);
    }}
    double ms=sw.Elapsed.TotalMilliseconds;long bytesAllocated=GC.GetAllocatedBytesForCurrentThread()-alloc;
    if(sample<0){var result=buffer.ToArray();if(expected==null)expected=result;else if(!expected.SequenceEqual(result))throw new Exception("wire mismatch "+mode);}
    else rows.Add(new{mode,sample,ms,allocatedBytes=bytesAllocated,wireBytes=buffer.Length});
   }
  }
  Console.WriteLine(JsonSerializer.Serialize(new{runtime=Environment.Version.ToString(),scope="Isolated .NET synthetic RenderPart field loop using production RenderPart source; not Unity, not full graph; 45,596 owners; pre-sized MemoryStream; all four arms byte-identical",cold,rows},new JsonSerializerOptions{WriteIndented=true}));
 }
}
