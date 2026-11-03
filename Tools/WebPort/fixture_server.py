#!/usr/bin/env python3
"""Shell-only browser fixture. Unity and WebGL are explicitly mocked in tests."""
import json
from pathlib import Path
import re
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

TEMPLATE = Path(__file__).resolve().parents[2] / 'Assets/WebGLTemplates/CavesPhone'
LOADER = b'''window.fixture = { calls: [], current: { revision: 1, mode: 'World', title: 'Normal', hp: 'HP 40', rows: [], messages: ['The expedition begins.'] },
 view(value) { this.current = { ...this.current, ...value }; window.dispatchEvent(new CustomEvent('coo-view', { detail: this.current })); } };
window.createUnityInstance = async function(canvas, config, progress) {
 progress(1);
 window.dispatchEvent(new CustomEvent('coo-storage', { detail: {state:'ready'} }));
 window.fixture.view({});
 return { SendMessage(receiver, method, value) { window.fixture.calls.push({receiver,method,value}); } };
};'''

class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(TEMPLATE), **kwargs)
    def log_message(self, format, *args):
        pass
    def do_GET(self):
        if self.path.startswith('/offline/'):
            name = self.path.removeprefix('/offline/')
            if name in ('', 'index.html'):
                data, kind = b'<title>Offline fixture</title><p>Offline fixture</p>', 'text/html'
            elif name == 'service-worker.js':
                data = (TEMPLATE / name).read_text().replace('__COO_BUILD_ID__', 'first-session-fixture').encode()
                kind = 'application/javascript'
            elif name == 'cache-manifest.json':
                data, kind = b'{"assets":["Build/fixture.loader.js","Build/fixture.data","Build/fixture.wasm"]}', 'application/json'
            elif name == 'build-info.json':
                data, kind = b'{"source":"first-session-fixture"}', 'application/json'
            elif name.startswith('Build/fixture.'):
                data, kind = ('First-session asset: ' + name).encode(), 'application/octet-stream'
            elif name in ('phone.css', 'phone.js', 'web-save-store.js', 'icon.svg', 'manifest.webmanifest'):
                data, kind = (TEMPLATE / name).read_bytes(), self.guess_type(name)
            else:
                self.send_error(404); return
        elif self.path in ('/', '/index.html'):
            html = (TEMPLATE / 'index.html').read_text()
            html = re.sub(r'window.cooBuildConfig = \{.*?\n    \};',
                          'window.cooBuildConfig = {loaderUrl:"test-loader.js"};', html, flags=re.S)
            data, kind = html.encode(), 'text/html'
        elif self.path == '/test-loader.js':
            data, kind = LOADER, 'application/javascript'
        elif self.path == '/build-info.json':
            data, kind = json.dumps({'source': 'shell-test-build'}).encode(), 'application/json'
        elif self.path == '/service-worker.js':
            self.send_error(404); return  # No service worker can mask a changed test fixture.
        else:
            return super().do_GET()
        self.send_response(200); self.send_header('Content-Type', kind)
        self.send_header('Content-Length', str(len(data))); self.end_headers(); self.wfile.write(data)

if __name__ == '__main__':
    ThreadingHTTPServer(('127.0.0.1', 8097), Handler).serve_forever()
