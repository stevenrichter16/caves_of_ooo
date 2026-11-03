# Phone Web build and verification

This is the browser adapter for the existing Unity game. The Unity player has
**not yet been built or qualified on iPhone**. See
[`Docs/WEB-IPHONE-IMPLEMENTATION.md`](../../Docs/WEB-IPHONE-IMPLEMENTATION.md)
for the current evidence and remaining gates.

## Verify without Unity

Requires Python 3.9+, Node 20+, .NET SDK 8, and installed Playwright browsers.

```sh
bash Tools/WebPort/verify_core.sh
cd Tools/WebPort
npm ci
npm test
python3 -m unittest discover -s tests -p 'test_*.py'
npx playwright install --with-deps chromium webkit
npm run test:browser
```

The C# runner uses Unity stubs, deterministic seed substitutions, and opt-in
pure save/menu controllers. It does not compile the complete presentation
assembly, execute native lifecycle callbacks, or prove IL2CPP compatibility.
The opt-in expected-error adapter checks expected and unexpected error logs;
default runner logging remains unchanged. The browser fixture explicitly mocks
Unity and WebGL capability. It tests the real HTML/JS/CSS, IndexedDB, gzip,
and service-worker code. It is not a playable game or a Unity performance test.

## Prepare a clean project

Commit the source first, then export that exact revision to a **new directory
outside the source checkout**:

```sh
python3 Tools/WebPort/prepare_project.py --ref HEAD --output /tmp/coo-phone-project --dry-run
python3 Tools/WebPort/prepare_project.py --ref HEAD --output /tmp/coo-phone-project
```

All tracked Assets, Packages and ProjectSettings come from that commit, including
assets absent from a sparse checkout. Git may download substantial missing
assets. Uncommitted changes are deliberately not included. Existing output
directories and symlink assets are rejected.

Only the export is adjusted: remove the Mac-local MCP package and its five
Editor adapters, explicitly pin Newtonsoft 3.2.2, and generate conservative
reflection preservation for `CavesOfOoo`. The source checkout and desktop
package files are unchanged. `.coo-web-project.json` records provenance.

## Build the real game

Requires licensed **Unity 6000.3.4f1 with Web Build Support**. Run its executable:

```sh
Unity -batchmode -quit -projectPath /tmp/coo-phone-project \
  -buildTarget WebGL -executeMethod CavesOfOoo.Editor.WebPhoneBuild.Build \
  -logFile /tmp/coo-phone-build.log
```

The output is `/tmp/coo-phone-project/Builds/Phone`. Set `COO_WEB_OUTPUT` to
choose another empty directory. The build entry refuses a different Unity
version, an unsupported target, a nonempty output, or a project without the
export marker. It uses SampleScene, IL2CPP, WebGL 2, no threads, hashed filenames,
and Gzip with Unity's decompression fallback. Heap settings reproduce the
existing project baseline and still need device measurement.

Build output includes `build-info.json`, `build-provenance.json`, and an offline
asset manifest generated from the actual Build/StreamingAssets files. A waiting
service-worker update does not replace an active game. The first successful
worker installation caches this build's assets before claiming offline readiness.
Storage quota can prevent installation; the online game remains usable.

## Serve and qualify

```sh
python3 Tools/WebPort/serve.py /tmp/coo-phone-project/Builds/Phone --port 8080
```

Localhost is suitable for desktop checks. Use a stable HTTPS origin for an
iPhone; plain HTTP on a LAN address does not provide the required secure context.
Upload the entire output directory, retaining the relative paths. `.unityweb`
files use Unity's fallback decompression; do not invent `Content-Encoding`
headers for them. Cross-origin isolation is not required by this single-threaded
build. Keep an older release available for same-build backup recovery.

Run the physical-device matrix and gameplay/lifecycle checklist in the plan
before publishing a supported release or merging runtime changes. Include
twenty-zone and larger real saves, interrupted writes, page termination,
orientation/software keyboard, audio unlock, late-game reflection restoration,
and current desktop/controller regressions.
