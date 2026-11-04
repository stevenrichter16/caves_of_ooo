const palette={k:'#0b1310',K:'#55634f',y:'#afba9d',Y:'#e8e5c8',w:'#97885c',W:'#dfc37b',g:'#669455',G:'#a7c77a',r:'#ad6855',R:'#e18b68',b:'#547781',B:'#92b5be',c:'#66a29a',C:'#a5c9b2',m:'#947d99',M:'#c0a0b8'};
const atlases=new Map();
export const imagesReady=Promise.all(['base','cast','anchors','states','items','cellar','ground','motion'].map(name=>new Promise(resolve=>{const image=new Image();image.src=`art/opening-${name}.png`;image.onload=()=>{atlases.set(`opening-${name}`,image);resolve();};image.onerror=resolve;})).concat([new Promise(resolve=>{const image=new Image();image.src='art/sill-environment.png';image.onload=()=>{atlases.set('sill-environment',image);resolve();};image.onerror=resolve;})]));
export function color(code){return palette[(code||'&y').replace(/[^a-zA-Z]/g,'').slice(-1)]||palette.y;}
export class WorldMapView {
 constructor(canvas){this.canvas=canvas;this.ctx=canvas.getContext('2d');this.scale=window.innerWidth<650?1.35:1.7;this.cx=40;this.cy=12;this.follow=true;this.target=null;this.data=null;this.bounds=null;this.observer=new ResizeObserver(()=>this.draw());this.observer.observe(canvas);}
 set(data){const changed=this.data?.zone!==data.zone;this.data=data;if(this.follow||changed){this.cx=data.x;this.cy=data.y;this.follow=true;}this.draw();}
 center(){if(this.data){this.follow=true;this.cx=this.data.x;this.cy=this.data.y;this.draw();}}
 zoom(change){this.scale=Math.min(3,Math.max(.65,this.scale+change));this.draw();}
 tileAt(x,y){if(!this.bounds)return null;return{x:Math.floor((x-this.bounds.ox)/this.bounds.tw),y:Math.floor((y-this.bounds.oy)/this.bounds.th)};}
 pan(dx,dy){if(!this.bounds)return;this.follow=false;this.cx-=dx/this.bounds.tw;this.cy-=dy/this.bounds.th;this.cx=Math.min(79,Math.max(0,this.cx));this.cy=Math.min(24,Math.max(0,this.cy));this.draw();}
 draw(){
  if(!this.data)return;
  const rect=this.canvas.getBoundingClientRect();if(!rect.width||!rect.height)return;
  const dpr=Math.min(devicePixelRatio||1,2),W=rect.width,H=rect.height;
  if(this.canvas.width!==Math.round(W*dpr)||this.canvas.height!==Math.round(H*dpr)){this.canvas.width=Math.round(W*dpr);this.canvas.height=Math.round(H*dpr);}
  const c=this.ctx;c.setTransform(dpr,0,0,dpr,0,0);c.imageSmoothingEnabled=false;c.fillStyle='#0a120d';c.fillRect(0,0,W,H);
  const tw=Math.round(16*this.scale),th=Math.round(24*this.scale);
  let ox=Math.round(W/2-(this.cx+.5)*tw),oy=Math.round(H/2-(this.cy+.5)*th);
  if(W>=80*tw)ox=Math.floor((W-80*tw)/2);else ox=Math.min(0,Math.max(W-80*tw,ox));
  if(H>=25*th)oy=Math.floor((H-25*th)/2);else oy=Math.min(0,Math.max(H-25*th,oy));
  this.bounds={ox,oy,tw,th,W,H};
  const underground=this.data.zone?.split('.').at(-1)!=='0';
  for(const t of this.data.cells){
   const x=ox+t.x*tw,y=oy+t.y*th;if(x+tw<0||x>W||y+th<0||y>H)continue;
   c.globalAlpha=t.v?1:.32;
   const noise=((t.x*17+t.y*37)%7)/7;
   c.fillStyle=underground?`rgb(${22+noise*6},${26+noise*7},${23+noise*5})`:`rgb(${23+noise*6},${36+noise*7},${24+noise*5})`;
   if(t.bg&&/\^[bBcC]/.test(t.bg))c.fillStyle='#214440';
   c.fillRect(x,y,tw,th);
   const environment=atlases.get('sill-environment'),ground=atlases.get('opening-ground');
   const variation=(t.x*13+t.y*7)%4;
   if(environment)c.drawImage(environment,(underground?4+variation:variation)*16,0,16,24,x,y,tw,th);
   if(ground&&(t.ground==='RoadStone'||t.g==='='))c.drawImage(ground,(16+((t.x+t.y)&1))*16,0,16,24,x,y,tw,th);
   if(ground&&(t.wet||t.ground==='Water'))c.drawImage(ground,0,0,16,24,x,y,tw,th);
   if(t.v&&!t.solid&&!underground){c.fillStyle='#8b9f5b15';c.fillRect(x+((t.x*7+t.y)%13)/16*tw,y+((t.x+t.y*7)%20)/24*th,1,2);}
   let drawn=false;
   if(t.art){const [key,frame]=t.art.split(':'),image=atlases.get(key);if(image){c.drawImage(image,Number(frame)*16,0,16,24,x,y,tw,th);drawn=true;}}
   if(!drawn&&t.g&&t.g!=='.'&&t.g!==' '&&t.g!=='='&&t.g!==','){c.fillStyle=color(t.c);c.font=`${Math.max(12,Math.round(th*.68))}px ui-monospace,SFMono-Regular,Menlo,monospace`;c.textAlign='center';c.textBaseline='middle';c.fillText(t.g==='#'?'▓':t.g,x+tw/2,y+th*.53,tw);}
   if(t.hostile){c.fillStyle='#cc765a';c.fillRect(x+tw*.3,y+th-2,tw*.4,2);}
  }
  c.globalAlpha=1;
  const px=ox+this.data.x*tw,py=oy+this.data.y*th;
  c.strokeStyle='#e4cd8b';c.lineWidth=1;c.strokeRect(px+1,py+1,tw-2,th-2);
  if(this.target){const tx=ox+this.target.x*tw,ty=oy+this.target.y*th;c.strokeStyle='#f2d384';c.lineWidth=2;c.strokeRect(tx+1,ty+1,tw-2,th-2);c.fillStyle='#e7c47b33';c.fillRect(tx,ty,tw,th);}
 }
}
export async function drawOpening(canvas){try{const data=await(await fetch('opening-view.json')).json();await imagesReady;const map=new WorldMapView(canvas);map.scale=1.15;map.follow=false;map.cx=39;map.cy=12;map.data=data;map.draw();return map;}catch{return null;}}
