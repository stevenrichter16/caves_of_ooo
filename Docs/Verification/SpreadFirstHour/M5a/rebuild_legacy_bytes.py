"""Build private fixture from two genuine baseline wire corpora; never publish Assets."""
from pathlib import Path
import base64,gzip,json,hashlib
r=Path('/tmp/coo-first-hour-e2');native=r/'native-legacy';portable=r/'legacy'
files=sorted(portable.glob('*.bin'));assert len(files)==9
text='''// Synthetic pre-repair baseline saves, never user saves. Standalone adapters
// have a different wire shape; each runtime loads its own executed baseline corpus.
using System; using System.IO; using System.IO.Compression; using CavesOfOoo.Core;
namespace CavesOfOoo.Tests { internal static class FirstHourAmbushLegacyBytes {
internal static GameSessionState Load(string name) { string data; switch(name) {\n'''
receipt=[]
for a in files:
 b=native/a.name;assert b.exists(),b
 encode=lambda p:base64.b64encode(gzip.compress(p.read_bytes(),mtime=0)).decode()
 text+='case "'+a.stem+'":\n#if UNITY_5_3_OR_NEWER\n data="'+encode(b)+'";\n#else\n data="'+encode(a)+'";\n#endif\n break;\n'
 receipt.append({'file':a.name,'nativeBytes':b.stat().st_size,'nativeSHA256':hashlib.sha256(b.read_bytes()).hexdigest(),'standaloneBytes':a.stat().st_size,'standaloneSHA256':hashlib.sha256(a.read_bytes()).hexdigest()})
text+='''default: throw new ArgumentException(name); } using(var packed=new MemoryStream(Convert.FromBase64String(data))) using(var unzip=new GZipStream(packed,CompressionMode.Decompress)) using(var stream=new MemoryStream()) { unzip.CopyTo(stream);stream.Position=0;var factory=new CavesOfOoo.Data.EntityFactory();factory.LoadBlueprints(UnityEngine.Resources.Load<UnityEngine.TextAsset>("Content/Blueprints/Objects").text);return GameSessionState.Load(new SaveReader(stream,factory)); } } } }\n'''
out=r/'candidate/Assets/Tests/EditMode/Gameplay/AI/FirstHourAmbushLegacyBytes.cs';out.write_text(text)
(r/'dual-runtime-legacy-manifest.json').write_text(json.dumps(receipt,indent=2)+'\n')
print(out)
print('sha256',hashlib.sha256(out.read_bytes()).hexdigest())
