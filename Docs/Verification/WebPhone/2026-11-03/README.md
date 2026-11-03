# Experimental phone Web adapter receipts

These receipts qualify isolated source contracts and the browser shell. They do
**not** demonstrate a running Unity Web player, Safari on a physical iPhone, or
desktop native regression acceptance. Both configured Unity Editor connections
returned HTTP 404; no licensed local Editor was available.

- `headless-main.xml`: 111 passing existing save/menu cases against frozen main
  `8c4573ae51a58916884e23efa282859d0669d645`.
- `headless-current.xml`: the same 111 cases plus 16 browser save/input contract
  cases, all passing under .NET 8.0.427 and the opt-in Unity stubs. Full Unity
  presentation and lifecycle code is excluded from this runner.
- `baseline-results.json`: initial broader baseline and focused comparisons.
  Six failures reproduced on frozen main, including in isolation. Their causes
  are unconfirmed; they are not included in the passing focused totals.
- `storage-tests.txt`: 24 passing production JavaScript storage/backup cases
  using fake IndexedDB for transaction fault injection.
- `project-export-tests.txt`: 13 passing Python cases for committed-source
  export, source preservation, dependencies, and missing required assets.
- `project-dry-run.json`: the actual committed project at `51fe0038`, with
  25,615 tracked project files and the configured gameplay scene. This checks
  export completeness without downloading all assets or invoking Unity.
- `browser-tests.json`: 42 passing cases across Chromium 156 and Linux WebKit
  27.2, with phone-sized portrait and landscape viewports. Real IndexedDB,
  compression and service workers; Unity/WebGL capability and build binaries
  are explicitly mocked. This is not iOS Safari qualification.
- `source-sha256.json`: exact changed/new implementation and test file hashes
  relative to the frozen main source. Files outside the tested subsets are
  recorded for reproducibility, not claimed as compiled or executed.
- `shell-*.png`: visually inspected browser-shell fixtures. The menu text is
  injected test data; no image shows actual gameplay or a Unity render.

The root reviewed source and fixture screenshots after independent storage,
input, build/export and UI reviews. Source review does not replace execution.
Unity `.meta` files were generated with unique GUIDs; their import is untested.

Reproduction commands: [Tools/WebPort/README.md](../../../../Tools/WebPort/README.md).
Implementation and outstanding gates:
[WEB-IPHONE-IMPLEMENTATION.md](../../../WEB-IPHONE-IMPLEMENTATION.md).

The cloud browser dependencies were extracted into a local library directory
without root access. `PLAYWRIGHT_SKIP_VALIDATE_HOST_REQUIREMENTS=1` bypassed
Playwright's system-wide package detection; the actual shared libraries were
present and both browsers executed. That environment-specific workaround is
not required on a normally provisioned Playwright host.
