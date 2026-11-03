const CACHE = 'caves-phone-__COO_BUILD_ID__';
const SHELL = ['./', 'phone.css', 'phone.js', 'web-save-store.js', 'icon.svg', 'manifest.webmanifest', 'build-info.json'];
self.addEventListener('install', event => event.waitUntil((async () => {
  const cache = await caches.open(CACHE);
  await cache.addAll(SHELL);
  const response = await fetch('cache-manifest.json', { cache: 'no-store' });
  if (!response.ok) throw new Error('Offline asset manifest is missing.');
  const manifest = await response.json();
  // Install succeeds only after this build's actual loader/data/WASM assets are cached.
  for (const asset of manifest.assets) await cache.add(asset);
})()));
// A waiting update activates only after old game tabs close. Never swap a live runtime.
self.addEventListener('activate', event => event.waitUntil(caches.keys().then(keys => Promise.all(
  keys.filter(key => key.startsWith('caves-phone-') && key !== CACHE).map(key => caches.delete(key)))).then(() => self.clients.claim())));
self.addEventListener('fetch', event => {
  if (event.request.method !== 'GET' || new URL(event.request.url).origin !== self.location.origin) return;
  event.respondWith(caches.open(CACHE).then(async cache => {
    const cached = await cache.match(event.request); if (cached) return cached;
    const response = await fetch(event.request);
    if (response.ok) cache.put(event.request, response.clone()).catch(() => {});
    return response;
  }));
});
