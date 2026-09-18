/* Reversible offline scene study. Pure mechanics are exported for Node verification. */
(function () {
  "use strict";
  const fail = (message) => {
    throw new Error(message);
  };
  const same = (a, b) => a[0] === b[0] && a[1] === b[1];
  const key = (cell) => cell.join(",");
  const pair = (value) =>
    Array.isArray(value) && value.length === 2 && value.every(Number.isFinite);
  const cell = (value) => pair(value) && value.every(Number.isInteger);
  const localPath = (value) =>
    typeof value === "string" &&
    value.length > 0 &&
    !/[?#%\\]/.test(value) &&
    !/^(?:[a-z][a-z\d+.-]*:|[/\\])/i.test(value) &&
    !value.split(/[/\\]/).includes("..");
  const sorted = (items) =>
    [...items].sort(
      (a, b) => a.depth - b.depth || (a.id < b.id ? -1 : a.id > b.id ? 1 : 0),
    );
  function validateScene(data) {
    if (
      !data ||
      data.schemaVersion !== 1 ||
      typeof data.id !== "string" ||
      typeof data.title !== "string"
    )
      fail("Unsupported scene manifest.");
    const c = data.canvas,
      n = data.navigation;
    if (
      !c ||
      ![c.width, c.height, c.pixelsPerCell].every(
        (v) => Number.isInteger(v) && v > 0,
      )
    )
      fail("Invalid canvas.");
    if (
      !n ||
      n.width * c.pixelsPerCell !== c.width ||
      n.height * c.pixelsPerCell !== c.height ||
      !Number.isInteger(n.width) ||
      !Number.isInteger(n.height)
    )
      fail("Navigation dimensions disagree with canvas.");
    const inside = (p) =>
      cell(p) && p[0] >= 0 && p[1] >= 0 && p[0] < n.width && p[1] < n.height;
    const asset = (a, label) => {
      if (!a || !localPath(a.path)) fail(`Invalid local asset: ${label}`);
    };
    for (const name of ["base", "baseline", "underlay", "inferenceMask"])
      asset(data[name], name);
    if (
      !Array.isArray(n.walkableCells) ||
      !n.walkableCells.every(inside) ||
      !inside(n.spawn)
    )
      fail("Invalid navigation cells or spawn.");
    if (new Set(n.walkableCells.map(key)).size !== n.walkableCells.length)
      fail("Duplicate navigation cells.");
    if (!Array.isArray(data.components)) fail("Missing components.");
    const ids = new Set();
    for (const item of data.components) {
      if (
        !item ||
        typeof item.id !== "string" ||
        !item.id ||
        ids.has(item.id) ||
        typeof item.name !== "string"
      )
        fail("Invalid or duplicate component id.");
      ids.add(item.id);
      for (const field of ["mutable", "defaultVisible", "blocksMovement"])
        if (typeof item[field] !== "boolean")
          fail(`${item.id}.${field} must be boolean.`);
      if (!Number.isFinite(item.depth) || !pair(item.foot))
        fail(`Invalid depth/foot: ${item.id}`);
      const b = item.bounds;
      if (
        !Array.isArray(b) ||
        b.length !== 4 ||
        !b.every(Number.isInteger) ||
        b[0] < 0 ||
        b[1] < 0 ||
        b[2] < 1 ||
        b[3] < 1 ||
        b[0] + b[2] > c.width ||
        b[1] + b[3] > c.height
      )
        fail(`Invalid component bounds: ${item.id}`);
      if (
        !Array.isArray(item.footprintCells) ||
        !item.footprintCells.every(inside)
      )
        fail(`Invalid footprint: ${item.id}`);
      asset(item.sprite, item.id);
      for (const field of ["contact", "mask"])
        if (item[field]) asset(item[field], `${item.id}.${field}`);
    }
    if (data.player) {
      asset(data.player, "player");
      const p = data.player;
      if (
        ![p.frameWidth, p.frameHeight, p.columns, p.frameCount].every(
          (v) => Number.isInteger(v) && v > 0,
        ) ||
        !Number.isFinite(p.scale) ||
        p.scale <= 0 ||
        !pair(p.pivot)
      )
        fail("Invalid player sheet.");
    }
    const state = createState(data);
    if (!isWalkable(data, state, n.spawn)) fail("Spawn is blocked.");
    return data;
  }
  function createState(data) {
    return {
      mode: "walk",
      view: "reconstructed",
      player: [...data.navigation.spawn],
      hidden: new Set(
        data.components.filter((c) => !c.defaultVisible).map((c) => c.id),
      ),
      selectedId: null,
      soloId: null,
      route: [],
      steps: 0,
      masks: false,
      inferred: false,
      exploded: false,
      actualSize: false,
      grid: false,
    };
  }
  function occupancy(data, state) {
    const blocked = new Set();
    for (const c of data.components)
      if (c.blocksMovement && !state.hidden.has(c.id))
        for (const p of c.footprintCells) blocked.add(key(p));
    return blocked;
  }
  function isWalkable(data, state, p) {
    if (!cell(p)) return false;
    return (
      data.navigation.walkableCells.some((c) => same(c, p)) &&
      !occupancy(data, state).has(key(p))
    );
  }
  function neighbors(data, state, p) {
    const result = [],
      allowed = new Set(data.navigation.walkableCells.map(key)),
      blocked = occupancy(data, state),
      open = (c) => allowed.has(key(c)) && !blocked.has(key(c));
    for (const [dx, dy] of [
      [0, -1],
      [1, 0],
      [0, 1],
      [-1, 0],
      [1, -1],
      [1, 1],
      [-1, 1],
      [-1, -1],
    ]) {
      const q = [p[0] + dx, p[1] + dy];
      if (
        open(q) &&
        (!dx || !dy || (open([p[0] + dx, p[1]]) && open([p[0], p[1] + dy])))
      )
        result.push(q);
    }
    return result;
  }
  function findPath(data, state, start, target) {
    if (!isWalkable(data, state, start) || !isWalkable(data, state, target))
      return null;
    if (same(start, target)) return [];
    const allowed = new Set(data.navigation.walkableCells.map(key)),
      blocked = occupancy(data, state),
      open = (c) => allowed.has(key(c)) && !blocked.has(key(c));
    const queue = [[...start]],
      parents = new Map([[key(start), null]]),
      byKey = new Map([[key(start), [...start]]]);
    for (let at = 0; at < queue.length; at++) {
      const p = queue[at];
      for (const [dx, dy] of [
        [0, -1],
        [1, 0],
        [0, 1],
        [-1, 0],
        [1, -1],
        [1, 1],
        [-1, 1],
        [-1, -1],
      ]) {
        const q = [p[0] + dx, p[1] + dy],
          k = key(q);
        if (
          parents.has(k) ||
          !open(q) ||
          (dx && dy && (!open([p[0] + dx, p[1]]) || !open([p[0], p[1] + dy])))
        )
          continue;
        parents.set(k, key(p));
        byKey.set(k, q);
        if (same(q, target)) {
          const route = [];
          for (
            let cursor = k;
            parents.get(cursor) !== null;
            cursor = parents.get(cursor)
          )
            route.unshift(byKey.get(cursor));
          return route;
        }
        queue.push(q);
      }
    }
    return null;
  }
  function reduceState(data, previous, action) {
    const next = {
      ...previous,
      hidden: new Set(previous.hidden),
      player: [...previous.player],
      route: previous.route.map((c) => [...c]),
    };
    const get = (id) =>
      data.components.find((c) => c.id === id) ||
      fail(`Unknown component: ${id}`);
    const movement = () => {
      if (
        next.mode !== "walk" ||
        next.view !== "reconstructed" ||
        next.soloId ||
        next.exploded
      )
        fail("Enter Walk mode to move.");
    };
    const change = (ids, remove) => {
      if (!Array.isArray(ids)) fail("Component ids must be an array.");
      const items = ids.map(get);
      for (const item of items) {
        if (!item.mutable) fail(`${item.name} is a fixed component.`);
        if (
          !remove &&
          item.blocksMovement &&
          item.footprintCells.some((p) => same(p, next.player))
        )
          fail(
            `${item.name} cannot be restored: its footprint is occupied by the traveler. Move away first.`,
          );
      }
      for (const item of items)
        if (remove) next.hidden.add(item.id);
        else next.hidden.delete(item.id);
      next.route = [];
      next.view = "reconstructed";
      next.soloId = null;
    };
    const move = (destination) => {
      movement();
      if (!neighbors(data, next, next.player).some((p) => same(p, destination)))
        fail("That step is blocked.");
      next.player = destination;
      next.steps++;
    };
    switch (action.type) {
      case "REMOVE":
        change(action.ids, true);
        break;
      case "RESTORE":
        change(action.ids, false);
        break;
      case "REMOVE_ALL":
        change(
          data.components.filter((c) => c.mutable).map((c) => c.id),
          true,
        );
        break;
      case "RESTORE_ALL":
        change(
          data.components
            .filter((c) => c.mutable && next.hidden.has(c.id))
            .map((c) => c.id),
          false,
        );
        break;
      case "SELECT":
        next.selectedId = get(action.id).id;
        break;
      case "MODE":
        if (!["walk", "inspect"].includes(action.mode)) fail("Invalid mode.");
        next.mode = action.mode;
        next.route = [];
        if (action.mode === "walk") {
          next.view = "reconstructed";
          next.soloId = null;
          next.exploded = false;
        }
        break;
      case "VIEW":
        if (!["reconstructed", "baseline", "underlay"].includes(action.view))
          fail("Invalid view.");
        next.view = action.view;
        next.route = [];
        next.soloId = null;
        next.exploded = false;
        if (action.view !== "reconstructed") next.mode = "inspect";
        break;
      case "SOLO":
        get(action.id);
        next.selectedId = action.id;
        next.soloId = next.soloId === action.id ? null : action.id;
        next.mode = "inspect";
        next.view = "reconstructed";
        next.route = [];
        next.exploded = false;
        break;
      case "OPTION":
        if (
          !["masks", "inferred", "exploded", "actualSize", "grid"].includes(
            action.key,
          ) ||
          typeof action.value !== "boolean"
        )
          fail("Invalid option.");
        next[action.key] = action.value;
        if (action.key === "exploded" && action.value) {
          next.mode = "inspect";
          next.route = [];
          next.view = "reconstructed";
          next.soloId = null;
          next.actualSize = false;
        }
        break;
      case "MOVE":
        if (
          !cell(action.delta) ||
          action.delta.some((v) => Math.abs(v) > 1) ||
          same(action.delta, [0, 0])
        )
          fail("Move must be a single adjacent step.");
        move([
          next.player[0] + action.delta[0],
          next.player[1] + action.delta[1],
        ]);
        next.route = [];
        break;
      case "ROUTE": {
        movement();
        const route = findPath(data, next, next.player, action.target);
        if (route === null) fail("No reachable path to that cell.");
        next.route = route;
        break;
      }
      case "TICK":
        if (next.route.length) {
          move(next.route[0]);
          next.route.shift();
        }
        break;
      case "CANCEL_ROUTE":
        next.route = [];
        break;
      case "RESET":
        return createState(data);
      default:
        fail(`Unknown action: ${action.type}`);
    }
    return next;
  }
  function hitTest(data, state, point, alphas) {
    return sorted(data.components)
      .reverse()
      .filter((c) => {
        if (state.soloId && state.soloId !== c.id) return false;
        if (state.view === "underlay" && c.mutable) return false;
        if (state.view === "baseline" && !c.defaultVisible) return false;
        if (
          state.view === "reconstructed" &&
          !state.soloId &&
          state.hidden.has(c.id)
        )
          return false;
        const [l, t, w, h] = c.bounds,
          x = Math.floor(point[0] - l),
          y = Math.floor(point[1] - t),
          alpha = alphas.get(c.id);
        return (
          x >= 0 && y >= 0 && x < w && y < h && alpha && alpha[y * w + x] > 0
        );
      });
  }
  function comparePixels(a, b) {
    if (a.length !== b.length || a.length % 4)
      fail("Pixel arrays must be matching RGBA buffers.");
    let changedPixels = 0,
      maxChannelDifference = 0;
    for (let i = 0; i < a.length; i += 4) {
      let changed = false;
      for (let k = 0; k < 4; k++) {
        const d = Math.abs(a[i + k] - b[i + k]);
        if (d) changed = true;
        maxChannelDifference = Math.max(maxChannelDifference, d);
      }
      if (changed) changedPixels++;
    }
    return { changedPixels, maxChannelDifference };
  }
  const vectors = { up: [0, -1], right: [1, 0], down: [0, 1], left: [-1, 0] };
  function createInput() {
    return { held: new Set(), taps: [] };
  }
  function pressInput(input, direction) {
    if (!vectors[direction]) fail("Unknown direction.");
    if (!input.held.has(direction)) {
      input.held.add(direction);
      const active = [...input.held];
      if (active.length > 1 && input.taps.length)
        input.taps[input.taps.length - 1] = active;
      else input.taps.push(active);
    }
  }
  function releaseInput(input, direction) {
    input.held.delete(direction);
  }
  function takeInput(input) {
    const dirs = input.taps.length ? input.taps.shift() : [...input.held];
    if (!dirs.length) return null;
    const sum = dirs.reduce(
      (v, d) => [v[0] + vectors[d][0], v[1] + vectors[d][1]],
      [0, 0],
    );
    return same(sum, [0, 0]) ? null : sum.map(Math.sign);
  }
  function clearInput(input) {
    input.held.clear();
    input.taps = [];
  }
  // Interpolation affects display only: collision remains on the committed cell.
  function interpolateStep(tween, currentCell, now, reducedMotion = false) {
    if (
      !tween ||
      reducedMotion ||
      !same(tween.to, currentCell) ||
      !Number.isFinite(tween.duration) ||
      tween.duration <= 0
    )
      return [...currentCell];
    const amount = Math.max(
      0,
      Math.min(1, (now - tween.start) / tween.duration),
    );
    return tween.from.map(
      (value, axis) => value + (tween.to[axis] - value) * amount,
    );
  }
  function isTypingTarget(target) {
    return Boolean(
      target?.isContentEditable ||
      ["TEXTAREA", "SELECT"].includes(target?.tagName) ||
      (target?.tagName === "INPUT" &&
        !["checkbox", "radio", "button", "submit", "reset"].includes(
          target.type,
        )),
    );
  }
  if (typeof module !== "undefined" && module.exports)
    module.exports = {
      validateScene,
      createState,
      reduceState,
      occupancy,
      isWalkable,
      neighbors,
      findPath,
      hitTest,
      comparePixels,
      createInput,
      pressInput,
      releaseInput,
      takeInput,
      clearInput,
      isTypingTarget,
      interpolateStep,
    };
  if (typeof document === "undefined") return;
  const el = (id) => document.getElementById(id),
    canvas = el("scene"),
    ctx = canvas.getContext("2d"),
    frame = el("scene-frame");
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  const manifestUrl = new URL("./build/scene.json", document.baseURI);
  const runtime = {
    data: null,
    state: null,
    images: new Map(),
    sprites: new Map(),
    contacts: new Map(),
    alphas: new Map(),
    masks: new Map(),
    exploded: [],
    input: createInput(),
    lastStep: 0,
    tween: null,
    loading: false,
    evidence: null,
    hover: null,
  };
  const announce = (message) => {
    el("announcement").textContent = message;
  };
  const showError = (message) => {
    el("error").textContent = message;
    el("error").hidden = !message;
  };
  const surface = (w, h) => {
    const c = document.createElement("canvas");
    c.width = w;
    c.height = h;
    return c;
  };
  const pixels = (image) => {
    const c = surface(
      image.naturalWidth || image.width,
      image.naturalHeight || image.height,
    );
    const x = c.getContext("2d", { willReadFrequently: true });
    x.drawImage(image, 0, 0);
    return x.getImageData(0, 0, c.width, c.height).data;
  };
  const alphaOf = (rgba) =>
    Uint8Array.from({ length: rgba.length / 4 }, (_, i) => rgba[i * 4 + 3]);
  function tinted(mask, width, height, rgb, opacity = 112) {
    const c = surface(width, height),
      x = c.getContext("2d"),
      rgba = x.createImageData(width, height);
    for (let i = 0; i < mask.length; i++) {
      rgba.data.set(rgb, i * 4);
      rgba.data[i * 4 + 3] = Math.round((mask[i] * opacity) / 255);
    }
    x.putImageData(rgba, 0, 0);
    return c;
  }
  async function fetchJson(url) {
    const response = await fetch(url, { cache: "no-store" });
    if (!response.ok) fail(`HTTP ${response.status}: ${new URL(url).pathname}`);
    return response.json();
  }
  function loadImage(path, dimensions) {
    if (!localPath(path))
      return Promise.reject(new Error(`Invalid local image: ${path}`));
    return new Promise((resolve, reject) => {
      const image = new Image();
      image.onload = () => {
        if (
          dimensions &&
          (image.naturalWidth !== dimensions[0] ||
            image.naturalHeight !== dimensions[1])
        )
          reject(
            new Error(
              `${path}: expected ${dimensions.join("×")}, got ${image.naturalWidth}×${image.naturalHeight}.`,
            ),
          );
        else resolve(image);
      };
      image.onerror = () => reject(new Error(`Could not load ${path}.`));
      const url = new URL(path, manifestUrl);
      url.searchParams.set("build", runtime.generation);
      image.src = url;
    });
  }
  function drawLayer(target, item, at = item.bounds) {
    const contact = runtime.contacts.get(item.id),
      sprite = runtime.sprites.get(item.id);
    if (contact) target.drawImage(contact, at[0], at[1]);
    if (sprite) target.drawImage(sprite, at[0], at[1]);
  }
  function composeIntact() {
    const { width, height } = runtime.data.canvas,
      c = surface(width, height),
      x = c.getContext("2d");
    x.imageSmoothingEnabled = false;
    x.drawImage(runtime.images.get("base"), 0, 0);
    for (const item of sorted(runtime.data.components))
      if (item.defaultVisible) drawLayer(x, item);
    return c;
  }
  async function loadBuild() {
    if (runtime.loading) return;
    runtime.loading = true;
    runtime.data = null;
    runtime.state = null;
    runtime.generation = Date.now();
    runtime.tween = null;
    clearInput(runtime.input);
    showError("");
    el("loading").hidden = false;
    el("loading").textContent = "Reading scene build…";
    el("refresh").disabled = true;
    el("proof-dot").className = "proof-dot";
    el("assembly-status").textContent =
      "Independent reconstruction check pending.";
    for (const map of [
      runtime.images,
      runtime.sprites,
      runtime.contacts,
      runtime.alphas,
      runtime.masks,
    ])
      map.clear();
    el("component-list").replaceChildren();
    el("asset-errors").replaceChildren();
    try {
      if (location.protocol === "file:")
        fail(
          "Serve the project over local HTTP. Run python3 -m http.server 8765 from the repository, then open /ArtSource/CatacombVillage/.",
        );
      const data = validateScene(await fetchJson(manifestUrl));
      runtime.data = data;
      runtime.state = createState(data);
      el("scene-title").textContent = data.title;
      document.title = `${data.title} · Caves of Ooo`;
      const errors = [],
        dimensions = [data.canvas.width, data.canvas.height];
      await Promise.all(
        ["base", "baseline", "underlay", "inferenceMask"].map(async (name) => {
          try {
            runtime.images.set(
              name,
              await loadImage(data[name].path, dimensions),
            );
          } catch (error) {
            errors.push(`${name}: ${error.message}`);
          }
        }),
      );
      let count = 0;
      await Promise.all(
        data.components.map(async (item) => {
          try {
            const image = await loadImage(
              item.sprite.path,
              item.bounds.slice(2),
            );
            runtime.sprites.set(item.id, image);
            const alpha = alphaOf(pixels(image));
            runtime.alphas.set(item.id, alpha);
            runtime.masks.set(
              item.id,
              tinted(
                alpha,
                item.bounds[2],
                item.bounds[3],
                [102, 215, 192],
                110,
              ),
            );
            if (item.contact)
              runtime.contacts.set(
                item.id,
                await loadImage(item.contact.path, item.bounds.slice(2)),
              );
          } catch (error) {
            errors.push(`${item.id}: ${error.message}`);
          }
          count++;
          el("loading").textContent =
            `Reading layered sprites… ${count} / ${data.components.length}`;
        }),
      );
      if (data.player) {
        try {
          runtime.images.set(
            "player",
            await loadImage(data.player.path, [
              data.player.frameWidth * data.player.columns,
              data.player.frameHeight *
                Math.ceil(data.player.frameCount / data.player.columns),
            ]),
          );
        } catch (error) {
          errors.push(`Traveler: ${error.message}`);
        }
      }
      if (errors.length) {
        for (const error of errors) {
          const li = document.createElement("li");
          li.textContent = error;
          el("asset-errors").append(li);
        }
        fail(
          `${errors.length} required scene asset(s) could not be loaded. Expand Build evidence for details. No verified scene is claimed.`,
        );
      }
      const inference = pixels(runtime.images.get("inferenceMask"));
      runtime.inference = tinted(
        Uint8Array.from(
          { length: inference.length / 4 },
          (_, i) => inference[i * 4],
        ),
        ...dimensions,
        [187, 141, 218],
        95,
      );
      runtime.evidence = comparePixels(
        pixels(composeIntact()),
        pixels(runtime.images.get("baseline")),
      );
      const ok = runtime.evidence.changedPixels === 0;
      el("proof-dot").className = `proof-dot ${ok ? "verified" : "failed"}`;
      el("assembly-status").textContent = ok
        ? `Exact intact reconstruction · ${(dimensions[0] * dimensions[1]).toLocaleString()} pixels compared in this browser.`
        : `Reconstruction mismatch · ${runtime.evidence.changedPixels.toLocaleString()} pixels differ (largest channel difference ${runtime.evidence.maxChannelDifference}).`;
      el("build-status").textContent =
        `${data.components.length} sprite layers loaded. ${data.components.filter((c) => c.mutable).length} props can be removed and restored. ${data.navigation.walkableCells.length} authored ground cells before component collision.`;
      el("build-evidence").textContent = JSON.stringify(
        {
          browserComparison: runtime.evidence,
          manifest: data.id,
          canvas: data.canvas,
          navigation: {
            width: data.navigation.width,
            height: data.navigation.height,
            spawn: data.navigation.spawn,
            destinations:
              data.navigation.destinations || data.navigation.waypoints || [],
          },
          provenance: data.provenance || data.sources || null,
        },
        null,
        2,
      );
      if (!ok)
        showError(
          "The browser reconstruction differs from the exported baseline. The scene is available for review; fidelity verification has not passed.",
        );
      renderInventory();
      updateUI();
      draw();
      announce(
        "Scene loaded. Walk with WASD, arrow keys, or click a reachable ground cell.",
      );
    } catch (error) {
      showError(error.message);
      runtime.data = null;
      runtime.state = null;
      el("view-status").textContent = "Build unavailable";
      el("assembly-status").textContent =
        "Not verified · required assets or manifest unavailable.";
      el("proof-dot").className = "proof-dot failed";
      ctx.clearRect(0, 0, canvas.width, canvas.height);
    } finally {
      runtime.loading = false;
      el("loading").hidden = true;
      el("refresh").disabled = false;
    }
  }
  function dispatch(action, { quiet = false } = {}) {
    if (!runtime.data) return false;
    try {
      const previousCell = runtime.state.player;
      const now = performance.now();
      const previousDisplay = interpolateStep(
        runtime.tween,
        previousCell,
        now,
        reducedMotion.matches,
      );
      runtime.state = reduceState(runtime.data, runtime.state, action);
      if (
        ["RESET", "MODE", "VIEW", "SOLO"].includes(action.type) ||
        runtime.state.exploded
      )
        runtime.tween = null;
      else if (!same(previousCell, runtime.state.player))
        runtime.tween = {
          from: previousDisplay,
          to: [...runtime.state.player],
          start: now,
          duration: 110,
        };
      if (!["MOVE", "TICK", "ROUTE", "SELECT", "OPTION"].includes(action.type))
        clearInput(runtime.input);
      if (!quiet) showError("");
      updateUI();
      draw();
      return true;
    } catch (error) {
      if (!quiet) {
        showError(error.message);
        announce(error.message);
      }
      return false;
    }
  }
  function renderInventory() {
    const query = el("component-search").value.toLowerCase(),
      data = runtime.data,
      state = runtime.state;
    el("component-list").replaceChildren();
    if (!data) return;
    const items = sorted(data.components)
      .reverse()
      .filter((item) =>
        `${item.name} ${item.id} ${item.kind}`.toLowerCase().includes(query),
      );
    for (const item of items) {
      const b = document.createElement("button");
      b.type = "button";
      b.className = `component-button${state.hidden.has(item.id) ? " hidden-prop" : ""}`;
      b.setAttribute("aria-pressed", String(state.selectedId === item.id));
      b.dataset.id = item.id;
      const image = document.createElement("img");
      image.className = "mini";
      image.alt = "";
      image.src = runtime.sprites.get(item.id).src;
      const label = document.createElement("span");
      label.className = "component-label";
      const title = document.createElement("strong");
      title.textContent = item.name;
      const detail = document.createElement("small");
      detail.textContent = `${item.mutable ? "Prop" : "Fixed"} · ${state.hidden.has(item.id) ? "removed" : item.kind || "layer"}`;
      label.append(title, detail);
      const dot = document.createElement("span");
      dot.className = "component-dot";
      b.append(image, label, dot);
      b.addEventListener("click", () => {
        dispatch({ type: "MODE", mode: "inspect" });
        dispatch({ type: "SELECT", id: item.id });
      });
      el("component-list").append(b);
    }
    if (!items.length) {
      const p = document.createElement("p");
      p.className = "subtle";
      p.textContent = "No matching components.";
      el("component-list").append(p);
    }
  }
  function updateUI() {
    const d = runtime.data,
      s = runtime.state;
    if (!d || !s) return;
    for (const mode of ["walk", "inspect"])
      el(`mode-${mode}`).setAttribute("aria-pressed", String(mode === s.mode));
    for (const view of ["reconstructed", "baseline", "underlay"])
      el(`view-${view}`).setAttribute("aria-pressed", String(view === s.view));
    for (const option of [
      "grid",
      "masks",
      "inferred",
      "exploded",
      "actualSize",
    ])
      el(option).checked = s[option];
    el("mode-help").textContent =
      s.mode === "walk"
        ? "WASD or arrows to move · click the ground to walk"
        : "Select artwork or an inventory item · remove and inspect props";
    el("component-count").textContent = d.components.length;
    el("hidden-count").textContent = `${s.hidden.size} removed`;
    el("player-status").textContent =
      `Traveler ${s.player.join(", ")} · ${s.steps} steps`;
    const descriptions = {
      reconstructed: "Layered scene",
      baseline: "Intact exported baseline",
      underlay: "Ground and fixed architecture · props removed",
    };
    el("view-status").textContent = s.soloId
      ? `Isolated · ${d.components.find((c) => c.id === s.soloId).name}`
      : s.exploded
        ? "Exploded component inventory"
        : `${descriptions[s.view]}${s.route.length ? ` · ${s.route.length} steps on route` : ""}`;
    canvas.classList.toggle("walk", s.mode === "walk");
    frame.classList.toggle("actual", s.actualSize && !s.exploded);
    frame.classList.toggle("exploded", s.exploded);
    for (const button of el("component-list").querySelectorAll(
      "button[data-id]",
    )) {
      const id = button.dataset.id,
        item = d.components.find((c) => c.id === id);
      button.setAttribute("aria-pressed", String(s.selectedId === id));
      button.classList.toggle("hidden-prop", s.hidden.has(id));
      button.querySelector("small").textContent =
        `${item.mutable ? "Prop" : "Fixed"} · ${s.hidden.has(id) ? "removed" : item.kind || "layer"}`;
    }
    const item = d.components.find((c) => c.id === s.selectedId);
    el("toggle-prop").disabled = !item || !item.mutable;
    el("solo-prop").disabled = !item;
    el("locate-prop").disabled = !item;
    el("selected-preview").hidden = !item;
    el("selected-details").replaceChildren();
    if (!item) {
      el("selected-name").textContent = "Choose a component";
      el("selected-status").textContent =
        "Enter Inspect, then select the artwork or an inventory item.";
      el("selected-notes").textContent = "";
      return;
    }
    el("selected-name").textContent = item.name;
    el("selected-status").textContent = item.mutable
      ? s.hidden.has(item.id)
        ? "Removed · backing exposed and footprint released"
        : "Visible prop · independently removable"
      : "Fixed architecture · available for isolated inspection";
    el("toggle-prop").textContent = s.hidden.has(item.id)
      ? "Restore"
      : "Remove";
    el("solo-prop").textContent =
      s.soloId === item.id ? "Exit isolate" : "Isolate";
    const preview = el("sprite-preview");
    preview.width = item.bounds[2];
    preview.height = item.bounds[3];
    preview.getContext("2d").drawImage(runtime.sprites.get(item.id), 0, 0);
    for (const [label, value] of [
      ["Pixels", `${item.bounds[2]} × ${item.bounds[3]}`],
      ["Position", `${item.bounds[0]}, ${item.bounds[1]}`],
      [
        "Collision",
        item.blocksMovement
          ? `${item.footprintCells.length} occupied cells`
          : "Walkable decoration",
      ],
      [
        "Backing",
        item.contact
          ? "Ground + removable contact layer"
          : "Complete ground beneath",
      ],
    ]) {
      const dt = document.createElement("dt"),
        dd = document.createElement("dd");
      dt.textContent = label;
      dd.textContent = value;
      el("selected-details").append(dt, dd);
    }
    el("selected-notes").textContent = (item.notes || []).join(" ");
  }
  function drawPlayer(target, displayedCell) {
    const d = runtime.data,
      s = runtime.state,
      p = d.player,
      image = runtime.images.get("player");
    if (!p || !image) return;
    const x = (displayedCell[0] + 0.5) * d.canvas.pixelsPerCell,
      y = (displayedCell[1] + 1) * d.canvas.pixelsPerCell,
      w = p.frameWidth * p.scale,
      h = p.frameHeight * p.scale;
    target.save();
    target.strokeStyle = "#d3e3a3b0";
    target.lineWidth = 1.5;
    target.beginPath();
    target.ellipse(x, y + 1, 10, 4, 0, 0, Math.PI * 2);
    target.stroke();
    target.drawImage(
      image,
      0,
      0,
      p.frameWidth,
      p.frameHeight,
      Math.round(x - w * p.pivot[0]),
      Math.round(y - h * p.pivot[1]),
      w,
      h,
    );
    target.restore();
  }
  function draw(now = performance.now()) {
    const d = runtime.data,
      s = runtime.state;
    if (!d || !s) return;
    if (s.exploded) {
      drawExploded();
      return;
    }
    if (canvas.width !== d.canvas.width || canvas.height !== d.canvas.height) {
      canvas.width = d.canvas.width;
      canvas.height = d.canvas.height;
    }
    const displayedCell = interpolateStep(
      runtime.tween,
      s.player,
      now,
      reducedMotion.matches,
    );
    ctx.imageSmoothingEnabled = false;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    if (s.soloId) {
      const item = d.components.find((c) => c.id === s.soloId);
      ctx.drawImage(
        runtime.sprites.get(item.id),
        item.bounds[0],
        item.bounds[1],
      );
    } else if (s.view !== "reconstructed")
      ctx.drawImage(runtime.images.get(s.view), 0, 0);
    else {
      ctx.drawImage(runtime.images.get("base"), 0, 0);
      const actor = s.mode === "walk",
        playerDepth = (displayedCell[1] + 1) * d.canvas.pixelsPerCell;
      let drawn = false;
      if (s.inferred) ctx.drawImage(runtime.inference, 0, 0);
      for (const item of sorted(d.components)) {
        if (s.hidden.has(item.id)) continue;
        if (actor && !drawn && item.depth > playerDepth) {
          drawPlayer(ctx, displayedCell);
          drawn = true;
        }
        drawLayer(ctx, item);
        if (s.masks)
          ctx.drawImage(
            runtime.masks.get(item.id),
            item.bounds[0],
            item.bounds[1],
          );
      }
      if (actor && !drawn) drawPlayer(ctx, displayedCell);
    }
    if (s.inferred && (s.view !== "reconstructed" || s.soloId))
      ctx.drawImage(runtime.inference, 0, 0);
    if (s.masks && (s.view !== "reconstructed" || s.soloId)) {
      for (const item of d.components) {
        if (s.soloId && s.soloId !== item.id) continue;
        ctx.drawImage(
          runtime.masks.get(item.id),
          item.bounds[0],
          item.bounds[1],
        );
      }
    }
    if (s.grid) drawGrid();
    if (s.route.length && s.mode === "walk") {
      ctx.save();
      ctx.strokeStyle = "#bdddb095";
      ctx.lineWidth = 2;
      ctx.setLineDash([3, 5]);
      ctx.beginPath();
      for (const [i, p] of [displayedCell, ...s.route].entries()) {
        const x = (p[0] + 0.5) * 32,
          y = (p[1] + 0.5) * 32;
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.stroke();
      ctx.restore();
    }
    if (s.selectedId && s.mode === "inspect" && !s.soloId) {
      const item = d.components.find((c) => c.id === s.selectedId);
      ctx.save();
      ctx.strokeStyle = s.hidden.has(item.id) ? "#dbb48c" : "#b6dec6";
      ctx.lineWidth = 2;
      ctx.setLineDash([5, 5]);
      ctx.strokeRect(...item.bounds);
      ctx.restore();
    }
  }
  function drawGrid() {
    const d = runtime.data,
      s = runtime.state,
      p = d.canvas.pixelsPerCell,
      allowed = new Set(d.navigation.walkableCells.map(key)),
      blocked = occupancy(d, s);
    ctx.save();
    for (let y = 0; y < d.navigation.height; y++)
      for (let x = 0; x < d.navigation.width; x++) {
        if (!allowed.has(`${x},${y}`)) ctx.fillStyle = "#10201970";
        else if (blocked.has(`${x},${y}`)) ctx.fillStyle = "#d178695e";
        else ctx.fillStyle = "#8dcaa51a";
        ctx.fillRect(x * p, y * p, p, p);
        ctx.strokeStyle = "#99c79b21";
        ctx.lineWidth = 0.5;
        ctx.strokeRect(x * p, y * p, p, p);
      }
    ctx.restore();
  }
  function drawExploded() {
    const d = runtime.data,
      s = runtime.state,
      cols = 6,
      tileW = 256,
      tileH = 200,
      items = sorted(d.components).reverse();
    canvas.width = 1536;
    canvas.height = Math.ceil(items.length / cols) * tileH;
    ctx.imageSmoothingEnabled = false;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    runtime.exploded = [];
    for (const [i, item] of items.entries()) {
      const x = (i % cols) * tileW,
        y = Math.floor(i / cols) * tileH;
      ctx.fillStyle = s.selectedId === item.id ? "#385041" : "#1b251f";
      ctx.fillRect(x + 5, y + 5, tileW - 10, tileH - 10);
      const image = runtime.sprites.get(item.id),
        scale = Math.min(
          2,
          (tileW - 28) / image.naturalWidth,
          (tileH - 55) / image.naturalHeight,
        ),
        w = Math.round(image.naturalWidth * scale),
        h = Math.round(image.naturalHeight * scale);
      ctx.globalAlpha = s.hidden.has(item.id) ? 0.4 : 1;
      ctx.drawImage(
        image,
        x + (tileW - w) / 2,
        y + 15 + (tileH - 55 - h) / 2,
        w,
        h,
      );
      ctx.globalAlpha = 1;
      ctx.fillStyle = "#d6e2c8";
      ctx.font = "12px system-ui";
      ctx.textAlign = "center";
      ctx.fillText(item.name.slice(0, 32), x + tileW / 2, y + tileH - 25);
      ctx.fillStyle = "#91a18b";
      ctx.font = "10px system-ui";
      ctx.fillText(
        `${item.mutable ? "Prop" : "Fixed"} · ${item.bounds[2]}×${item.bounds[3]}${s.hidden.has(item.id) ? " · removed" : ""}`,
        x + tileW / 2,
        y + tileH - 10,
      );
      runtime.exploded.push({ id: item.id, bounds: [x, y, tileW, tileH] });
    }
  }
  function locate() {
    const d = runtime.data,
      s = runtime.state,
      item = d.components.find((c) => c.id === s.selectedId);
    if (!item) return;
    dispatch({ type: "VIEW", view: "reconstructed" });
    dispatch({ type: "MODE", mode: "inspect" });
    if (s.actualSize) {
      frame.scrollTo({
        left: item.bounds[0] + item.bounds[2] / 2 - frame.clientWidth / 2,
        top: item.bounds[1] + item.bounds[3] / 2 - frame.clientHeight / 2,
        behavior: "smooth",
      });
    }
    canvas.focus({ preventScroll: true });
    announce(`Located ${item.name}.`);
  }
  for (const mode of ["walk", "inspect"])
    el(`mode-${mode}`).addEventListener("click", () => {
      dispatch({ type: "MODE", mode });
      canvas.focus({ preventScroll: true });
    });
  for (const view of ["reconstructed", "baseline", "underlay"])
    el(`view-${view}`).addEventListener("click", () =>
      dispatch({ type: "VIEW", view }),
    );
  for (const option of ["grid", "masks", "inferred", "exploded", "actualSize"])
    el(option).addEventListener("change", (event) =>
      dispatch({ type: "OPTION", key: option, value: event.target.checked }),
    );
  el("remove-all").addEventListener("click", () => {
    if (dispatch({ type: "REMOVE_ALL" }))
      announce(
        "All removable props lifted. Their ground and collision footprints are clear.",
      );
  });
  el("restore-all").addEventListener("click", () => {
    if (dispatch({ type: "RESTORE_ALL" })) announce("All props restored.");
  });
  el("reset").addEventListener("click", () => {
    dispatch({ type: "RESET" });
    announce("Scene reset. Traveler returned to the entrance.");
  });
  el("refresh").addEventListener("click", loadBuild);
  el("component-search").addEventListener("input", renderInventory);
  el("toggle-prop").addEventListener("click", () => {
    const s = runtime.state;
    if (s?.selectedId)
      dispatch({
        type: s.hidden.has(s.selectedId) ? "RESTORE" : "REMOVE",
        ids: [s.selectedId],
      });
  });
  el("solo-prop").addEventListener("click", () => {
    if (runtime.state?.selectedId)
      dispatch({ type: "SOLO", id: runtime.state.selectedId });
  });
  el("locate-prop").addEventListener("click", locate);
  canvas.addEventListener("click", (event) => {
    if (!runtime.data) return;
    canvas.focus({ preventScroll: true });
    const r = canvas.getBoundingClientRect(),
      p = [
        ((event.clientX - r.left) * canvas.width) / r.width,
        ((event.clientY - r.top) * canvas.height) / r.height,
      ],
      s = runtime.state;
    if (s.exploded) {
      const item = runtime.exploded.find(
        (e) =>
          p[0] >= e.bounds[0] &&
          p[0] < e.bounds[0] + e.bounds[2] &&
          p[1] >= e.bounds[1] &&
          p[1] < e.bounds[1] + e.bounds[3],
      );
      if (item) dispatch({ type: "SELECT", id: item.id });
      return;
    }
    if (s.mode === "walk") {
      dispatch({
        type: "ROUTE",
        target: p.map((v) => Math.floor(v / runtime.data.canvas.pixelsPerCell)),
      });
    } else {
      const hits = hitTest(runtime.data, s, p, runtime.alphas);
      if (hits.length) dispatch({ type: "SELECT", id: hits[0].id });
      else
        announce(
          "No sprite under that pixel. Use the inventory to select removed props.",
        );
    }
  });
  const keyDirections = {
    ArrowUp: "up",
    w: "up",
    W: "up",
    ArrowRight: "right",
    d: "right",
    D: "right",
    ArrowDown: "down",
    s: "down",
    S: "down",
    ArrowLeft: "left",
    a: "left",
    A: "left",
  };
  window.addEventListener("keydown", (event) => {
    if (isTypingTarget(event.target)) return;
    if (event.key === "Escape") {
      clearInput(runtime.input);
      dispatch({ type: "CANCEL_ROUTE" });
      return;
    }
    const direction = keyDirections[event.key];
    if (!direction || !runtime.state || runtime.state.mode !== "walk") return;
    event.preventDefault();
    if (event.repeat) return;
    pressInput(runtime.input, direction);
    runtime.state = reduceState(runtime.data, runtime.state, {
      type: "CANCEL_ROUTE",
    });
  });
  window.addEventListener("keyup", (event) => {
    const direction = keyDirections[event.key];
    if (direction) releaseInput(runtime.input, direction);
  });
  function clearMovement() {
    clearInput(runtime.input);
    if (runtime.data && runtime.state)
      dispatch({ type: "CANCEL_ROUTE" }, { quiet: true });
  }
  window.addEventListener("blur", clearMovement);
  document.addEventListener("visibilitychange", () => {
    if (document.hidden) clearMovement();
  });
  el("component-search").addEventListener("focus", clearMovement);
  function tick(now) {
    if (
      runtime.state &&
      runtime.state.mode === "walk" &&
      now - runtime.lastStep >= 115
    ) {
      runtime.lastStep = now;
      const delta = takeInput(runtime.input);
      if (delta) dispatch({ type: "MOVE", delta }, { quiet: true });
      else if (
        runtime.state.route.length &&
        !dispatch({ type: "TICK" }, { quiet: true })
      )
        dispatch({ type: "CANCEL_ROUTE" }, { quiet: true });
    }
    if (runtime.tween) {
      draw(now);
      if (
        reducedMotion.matches ||
        now >= runtime.tween.start + runtime.tween.duration
      )
        runtime.tween = null;
    }
    requestAnimationFrame(tick);
  }
  requestAnimationFrame(tick);
  loadBuild();
})();
