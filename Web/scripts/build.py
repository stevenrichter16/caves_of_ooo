#!/usr/bin/env python3
"""Publish the portable core and package an offline-capable static browser game."""
from pathlib import Path
import hashlib,json,shutil,subprocess
root=Path(__file__).resolve().parents[1]
subprocess.run(['dotnet','publish','Client/Client.csproj','-c','Release','-o','.build','-nologo','-v','q'],cwd=root,check=True)
dist=root/'dist'
if dist.exists():shutil.rmtree(dist)
shutil.copytree(root/'.build/wwwroot',dist,ignore=shutil.ignore_patterns('*.gz','*.br','*.pdb','*.map','web.config'))
files=sorted(p for p in dist.rglob('*') if p.is_file() and p.name!='sw.js')
release=hashlib.sha256(b''.join(hashlib.sha256(p.read_bytes()).digest() for p in files)).hexdigest()[:16]
assets=['./']+['./'+p.relative_to(dist).as_posix() for p in files]
sw='''// Generated from the exact release. An update waits for all game tabs to close.
const CACHE='ooo-static-__RELEASE__';
const ASSETS=__ASSETS__;
self.addEventListener('install',event=>event.waitUntil((async()=>{
 const cache=await caches.open(CACHE);
 // A failed download rejects the installation, preserving the previous complete release.
 await cache.addAll(ASSETS.map(url=>new Request(url,{cache:'reload'})));
})()));
self.addEventListener('activate',event=>event.waitUntil((async()=>{
 for(const key of await caches.keys())if(key.startsWith('ooo-static-')&&key!==CACHE)await caches.delete(key);
 await self.clients.claim();
})()));
self.addEventListener('fetch',event=>{
 const url=new URL(event.request.url);
 if(event.request.method!=='GET'||url.origin!==self.location.origin)return;
 const path=url.pathname.slice(new URL(self.registration.scope).pathname.length);
 if(event.request.mode!=='navigate'&&!ASSETS.includes('./'+path))return;
 event.respondWith((async()=>{
  const cache=await caches.open(CACHE);
  const saved=await cache.match(event.request,{ignoreSearch:true});
  return saved||(event.request.mode==='navigate'?await cache.match('./index.html'):null)||fetch(event.request);
 })());
});
'''.replace('__RELEASE__',release).replace('__ASSETS__',json.dumps(assets,separators=(',',':')))
(dist/'sw.js').write_text(sw)
print(json.dumps({'release':release,'files':len(assets),'uncompressedBytes':sum(p.stat().st_size for p in dist.rglob('*') if p.is_file())}))
