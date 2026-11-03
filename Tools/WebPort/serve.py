#!/usr/bin/env python3
"""Local browser qualification server; publish the build directory over HTTPS for iPhone."""
import argparse
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from functools import partial

class Handler(SimpleHTTPRequestHandler):
    extensions_map = {**SimpleHTTPRequestHandler.extensions_map, '.wasm': 'application/wasm',
                      '.webmanifest': 'application/manifest+json', '.unityweb': 'application/octet-stream'}
    def end_headers(self):
        self.send_header('Cache-Control', 'no-cache')
        self.send_header('X-Content-Type-Options', 'nosniff')
        super().end_headers()

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory'); parser.add_argument('--port', type=int, default=8080)
    args = parser.parse_args()
    ThreadingHTTPServer(('0.0.0.0', args.port), partial(Handler, directory=args.directory)).serve_forever()
