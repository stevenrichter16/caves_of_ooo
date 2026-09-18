/* Source-aligned, reversible art workbench. This is not the native Unity game. */
(function () {
  "use strict";
  const fail = (m) => {
      throw new Error(m);
    },
    pair = (v) =>
      Array.isArray(v) && v.length === 2 && v.every(Number.isFinite),
    cell = (v) => pair(v) && v.every(Number.isInteger),
    key = (p) => p.join(","),
    same = (a, b) => a[0] === b[0] && a[1] === b[1];
  const localPath = (p) =>
    typeof p === "string" &&
    p.length > 0 &&
    !/[?#%\\]/.test(p) &&
    !/^(?:[a-z][a-z\d+.-]*:|\/)/i.test(p) &&
    !p.split("/").includes("..");
  const caches = new WeakMap();
  function index(d) {
    if (!caches.has(d))
      caches.set(d, {
        owners: new Map(d.components.map((c) => [c.id, c])),
        rooms: new Map(d.rooms.map((r) => [r.id, r])),
        allowed: new Set(d.navigation.walkableCells.map(key)),
        layers: [...d.layers].sort(
          (a, b) => a.z - b.z || a.id.localeCompare(b.id),
        ),
      });
    return caches.get(d);
  }
  function validateScene(d) {
    if (
      !d ||
      d.schemaVersion !== 1 ||
      d.id !== "morrowfast" ||
      typeof d.title !== "string"
    )
      fail("Unsupported Morrowfast manifest.");
    const c = d.canvas,
      n = d.navigation;
    if (
      !c ||
      ![c.width, c.height].every((v) => Number.isInteger(v) && v > 0) ||
      !n ||
      ![n.width, n.height, n.cellSize].every(
        (v) => Number.isInteger(v) && v > 0,
      ) ||
      n.width * n.cellSize !== c.width ||
      n.height * n.cellSize !== c.height
    )
      fail("Canvas and navigation dimensions disagree.");
    const inside = (p) =>
      cell(p) && p[0] >= 0 && p[1] >= 0 && p[0] < n.width && p[1] < n.height;
    const bounds = (b) =>
      Array.isArray(b) &&
      b.length === 4 &&
      b.every(Number.isInteger) &&
      b[0] >= 0 &&
      b[1] >= 0 &&
      b[2] > 0 &&
      b[3] > 0 &&
      b[0] + b[2] <= c.width &&
      b[1] + b[3] <= c.height;
    if (!localPath(d.base?.path) || !localPath(d.baseline?.path))
      fail("Assets must use safe local relative paths.");
    if (
      !Array.isArray(n.walkableCells) ||
      !n.walkableCells.every(inside) ||
      new Set(n.walkableCells.map(key)).size !== n.walkableCells.length ||
      !inside(n.spawn) ||
      !inside(n.northExit)
    )
      fail("Invalid navigation cells or arrivals.");
    if (
      !Array.isArray(d.components) ||
      !Array.isArray(d.layers) ||
      !Array.isArray(d.rooms)
    )
      fail("Scene inventory is missing.");
    const ids = new Set(),
      layers = new Set(),
      rooms = new Set();
    for (const o of d.components) {
      if (
        !o ||
        typeof o.id !== "string" ||
        !o.id ||
        ids.has(o.id) ||
        typeof o.name !== "string" ||
        typeof o.kind !== "string" ||
        typeof o.description !== "string" ||
        !Array.isArray(o.actions) ||
        !o.actions.every((a) => typeof a === "string") ||
        typeof o.mutable !== "boolean" ||
        typeof o.blocksMovement !== "boolean" ||
        !pair(o.foot) ||
        !bounds(o.bounds) ||
        !Array.isArray(o.footprintCells) ||
        !o.footprintCells.every(inside) ||
        !Array.isArray(o.layerIds)
      )
        fail("Invalid component: " + (o?.id || "unnamed"));
      ids.add(o.id);
    }
    for (const l of d.layers) {
      if (
        !l ||
        !l.id ||
        layers.has(l.id) ||
        !ids.has(l.ownerId) ||
        !localPath(l.path) ||
        !bounds(l.bounds) ||
        !Number.isFinite(l.z) ||
        typeof l.role !== "string"
      )
        fail("Invalid or orphaned layer: " + (l?.id || "unnamed"));
      layers.add(l.id);
    }
    for (const r of d.rooms) {
      if (
        !r ||
        !r.id ||
        rooms.has(r.id) ||
        !ids.has(r.roofId) ||
        !ids.has(r.doorId) ||
        !pair(r.entry) ||
        ![r.polygon, r.interiorPolygon].every(
          (p) => Array.isArray(p) && p.length >= 3 && p.every(pair),
        )
      )
        fail("Invalid room: " + (r?.id || "unnamed"));
      rooms.add(r.id);
    }
    for (const o of d.components) {
      if (o.roomId != null && !rooms.has(o.roomId))
        fail("Unknown room on " + o.id);
      if (
        !o.layerIds.every(
          (id) =>
            layers.has(id) &&
            d.layers.find((l) => l.id === id).ownerId === o.id,
        )
      )
        fail("Layer ownership mismatch on " + o.id);
    }
    for (const l of d.layers)
      if (!d.components.find((o) => o.id === l.ownerId).layerIds.includes(l.id))
        fail("Unclaimed layer: " + l.id);
    if (n.crossings !== undefined && !Array.isArray(n.crossings))
      fail("Invalid crossing supports.");
    const crossingOwners = new Set(),
      crossingCells = new Set();
    const allowedCells = new Set(n.walkableCells.map(key));
    for (const crossing of n.crossings || []) {
      if (
        !crossing ||
        !ids.has(crossing.ownerId) ||
        crossingOwners.has(crossing.ownerId) ||
        !Array.isArray(crossing.cells) ||
        !crossing.cells.length ||
        !crossing.cells.every((p) => inside(p) && allowedCells.has(key(p)))
      )
        fail("Invalid crossing owner or supported ground.");
      crossingOwners.add(crossing.ownerId);
      for (const p of crossing.cells) {
        if (crossingCells.has(key(p))) fail("Duplicate crossing support cell.");
        crossingCells.add(key(p));
      }
    }
    caches.delete(d);
    const s = createState(d);
    if (!isWalkable(d, s, n.spawn)) fail("Surveyor arrival is blocked.");
    return d;
  }
  function createState(d) {
    return {
      mode: "walk",
      view: "reconstruction",
      player: [...d.navigation.spawn],
      hidden: new Set(),
      selectedId: null,
      route: [],
      steps: 0,
      masks: false,
      grid: false,
    };
  }
  function ownerVisible(d, s, id) {
    const o = index(d).owners.get(id);
    if (!o || s.hidden.has(id)) return false;
    if (o.visibleWhen === "room-open") {
      const r = index(d).rooms.get(o.roomId);
      return !!r && s.hidden.has(r.roofId);
    }
    return true;
  }
  function layerVisible(d, s, l) {
    return ownerVisible(d, s, l.ownerId);
  }
  function occupancy(d, s) {
    const out = new Set();
    for (const o of d.components)
      if (o.blocksMovement && !s.hidden.has(o.id))
        for (const p of o.footprintCells) out.add(key(p));
    // Removed furniture contributes no obstacle. Removed bridges instead
    // withdraw support from water cells that were walkable while intact.
    for (const crossing of d.navigation.crossings || [])
      if (s.hidden.has(crossing.ownerId))
        for (const p of crossing.cells) out.add(key(p));
    return out;
  }
  function isWalkable(d, s, p) {
    return (
      cell(p) && index(d).allowed.has(key(p)) && !occupancy(d, s).has(key(p))
    );
  }
  function setHidden(d, s, id, hidden) {
    const o = index(d).owners.get(id);
    if (!o) return { ok: false, reason: "Unknown object." };
    if (typeof hidden !== "boolean")
      return { ok: false, reason: "Invalid visibility state." };
    if (!o.mutable)
      return {
        ok: false,
        reason:
          "This structure stays in place; inspect its separate movable components.",
      };
    if (
      !hidden &&
      o.blocksMovement &&
      o.footprintCells.some((p) => same(p, s.player))
    )
      return {
        ok: false,
        reason:
          "Move the surveyor clear before restoring or closing this object.",
      };
    if (hidden) {
      const crossing = (d.navigation.crossings || []).find(
        (c) => c.ownerId === id,
      );
      if (crossing) {
        const support = new Set(crossing.cells.map(key));
        if (support.has(key(s.player)))
          return {
            ok: false,
            reason: "Move the surveyor off the bridge before lifting it.",
          };
        if (s.route.some((p) => support.has(key(p))))
          return {
            ok: false,
            reason:
              "Finish or cancel the queued crossing before lifting the bridge.",
          };
      }
      s.hidden.add(id);
    } else s.hidden.delete(id);
    return { ok: true };
  }
  function primaryActionLabel(d, s, id) {
    const o = index(d).owners.get(id);
    if (!o || !o.mutable) return null;
    const room = d.rooms.find((r) => r.roofId === id || r.doorId === id);
    const hidden = s.hidden.has(id);
    if (room?.roofId === id) return hidden ? "Replace roof" : "Lift roof";
    if (room?.doorId === id || o.kind === "door")
      return hidden ? "Close door" : "Open door";
    return hidden
      ? "Restore component"
      : o.actions.find((a) => /harvest|take/i.test(a)) || "Lift component";
  }
  function hideOutdoor(d, s) {
    const result = { changed: 0, refused: [] };
    for (const o of d.components)
      if (
        o.mutable &&
        !o.roomId &&
        !["roof", "door", "gate", "building-shell"].includes(o.kind)
      ) {
        const change = setHidden(d, s, o.id, true);
        if (change.ok) result.changed++;
        else result.refused.push(o.id);
      }
    return result;
  }
  function openAllRooms(d, s) {
    for (const r of d.rooms) setHidden(d, s, r.roofId, true);
  }
  function findPath(d, s, start, target) {
    const allowed = index(d).allowed,
      blocked = occupancy(d, s),
      open = (p) => cell(p) && allowed.has(key(p)) && !blocked.has(key(p));
    if (!open(start) || !open(target)) return null;
    if (same(start, target)) return [];
    const q = [[...start]],
      prev = new Map([[key(start), null]]),
      points = new Map([[key(start), start]]);
    for (let at = 0; at < q.length; at++) {
      const p = q[at];
      for (const [dx, dy] of [
        [0, -1],
        [1, 0],
        [0, 1],
        [-1, 0],
      ]) {
        const t = [p[0] + dx, p[1] + dy],
          k = key(t);
        if (prev.has(k) || !open(t)) continue;
        prev.set(k, key(p));
        points.set(k, t);
        if (same(t, target)) {
          const route = [];
          for (let k2 = k; prev.get(k2) !== null; k2 = prev.get(k2))
            route.push(points.get(k2));
          return route.reverse();
        }
        q.push(t);
      }
    }
    return null;
  }
  function moveOne(d, s, target) {
    if (s.view !== "reconstruction" || s.mode !== "walk")
      return {
        ok: false,
        reason: "Movement is available in Walk / Reconstruction.",
      };
    if (
      !cell(target) ||
      Math.abs(target[0] - s.player[0]) + Math.abs(target[1] - s.player[1]) !==
        1 ||
      !isWalkable(d, s, target)
    )
      return { ok: false, reason: "The way is blocked." };
    s.player = [...target];
    s.steps++;
    return { ok: true };
  }
  function serializeState(d, s) {
    return {
      schemaVersion: 1,
      id: d.id,
      hidden: [...s.hidden].sort(),
      player: [...s.player],
      steps: s.steps,
    };
  }
  function loadState(d, raw) {
    let v;
    try {
      v = typeof raw === "string" ? JSON.parse(raw) : raw;
    } catch {
      fail("The saved workbench state is not valid JSON.");
    }
    if (
      !v ||
      v.schemaVersion !== 1 ||
      v.id !== d.id ||
      !Array.isArray(v.hidden) ||
      new Set(v.hidden).size !== v.hidden.length ||
      !v.hidden.every((id) => index(d).owners.get(id)?.mutable) ||
      !cell(v.player) ||
      !Number.isInteger(v.steps) ||
      v.steps < 0
    )
      fail("The saved state does not match this scene.");
    const s = createState(d);
    s.hidden = new Set(v.hidden);
    s.player = [...v.player];
    s.steps = v.steps;
    if (!isWalkable(d, s, s.player))
      fail("The saved surveyor is inside a closed or blocked surface.");
    return s;
  }
  const api = {
    validateScene,
    createState,
    ownerVisible,
    layerVisible,
    occupancy,
    isWalkable,
    setHidden,
    hideOutdoor,
    primaryActionLabel,
    openAllRooms,
    findPath,
    moveOne,
    serializeState,
    loadState,
  };
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
    return;
  }
  window.MorrowfastPreview = api;
  if (typeof document === "undefined") return;
  const $ = (id) => document.getElementById(id);
  let data,
    state,
    images = new Map(),
    layerPixels = new Map(),
    hoverId = null,
    timer = null,
    saveAvailable = true;
  const canvas = $("scene"),
    ctx = canvas.getContext("2d", { alpha: false }),
    storageKey = "coo-morrowfast-preview-v1";
  const announce = (t) => {
    $("status-message").textContent = t;
    $("announcement").textContent = t;
  };
  function save() {
    try {
      localStorage.setItem(
        storageKey,
        JSON.stringify(serializeState(data, state)),
      );
    } catch {
      saveAvailable = false;
    }
  }
  function message(text, kind = "") {
    const el = $("error");
    el.textContent = text;
    el.hidden = !text;
    el.dataset.kind = kind;
  }
  async function loadImage(path) {
    if (images.has(path)) return images.get(path);
    const img = new Image();
    img.decoding = "async";
    await new Promise((resolve, reject) => {
      img.onload = resolve;
      img.onerror = () => reject(new Error("Unable to load " + path));
      img.src = path;
    });
    images.set(path, img);
    return img;
  }
  function component(id = state?.selectedId) {
    return index(data).owners.get(id);
  }
  function roomFor(o) {
    return o && data.rooms.find((r) => r.roofId === o.id || r.doorId === o.id);
  }
  function visibleLayers() {
    return index(data).layers.filter((l) => layerVisible(data, state, l));
  }
  function draw() {
    if (!data) return;
    ctx.imageSmoothingEnabled = false;
    ctx.fillStyle = "#10120f";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    if (state.view === "original")
      ctx.drawImage(
        images.get(data.baseline.path),
        0,
        0,
        canvas.width,
        canvas.height,
      );
    else {
      ctx.drawImage(
        images.get(data.base.path),
        0,
        0,
        canvas.width,
        canvas.height,
      );
      if (state.view === "reconstruction")
        for (const l of visibleLayers())
          ctx.drawImage(images.get(l.path), ...l.bounds);
    }
    if (state.view === "reconstruction") {
      if (state.grid) {
        const cs = data.navigation.cellSize,
          blocked = occupancy(data, state);
        ctx.fillStyle = "rgba(71,177,157,.12)";
        for (const p of data.navigation.walkableCells)
          if (!blocked.has(key(p)))
            ctx.fillRect(p[0] * cs, p[1] * cs, cs - 1, cs - 1);
      }
      if (state.route.length) {
        ctx.strokeStyle = "rgba(232,211,147,.55)";
        ctx.lineWidth = 2;
        ctx.setLineDash([3, 6]);
        ctx.beginPath();
        ctx.moveTo(...pointFor(state.player));
        for (const p of state.route) ctx.lineTo(...pointFor(p));
        ctx.stroke();
        ctx.setLineDash([]);
      }
      const id = hoverId || state.selectedId,
        o = component(id);
      if (o && ownerVisible(data, state, id)) {
        ctx.save();
        ctx.strokeStyle = "#efd6a1";
        ctx.lineWidth = 2;
        ctx.setLineDash([7, 5]);
        ctx.strokeRect(
          o.bounds[0] - 2,
          o.bounds[1] - 2,
          o.bounds[2] + 4,
          o.bounds[3] + 4,
        );
        ctx.setLineDash([]);
        ctx.restore();
      }
      if (state.masks) {
        for (const l of visibleLayers()) {
          if (state.selectedId && l.ownerId !== state.selectedId) continue;
          const mask = layerPixels.get(l.id)?.tint;
          if (mask) ctx.drawImage(mask, ...l.bounds);
        }
      }
      const [x, y] = pointFor(state.player);
      ctx.save();
      ctx.shadowColor = "#000";
      ctx.shadowBlur = 6;
      ctx.fillStyle = "#e8d5a7";
      ctx.strokeStyle = "#171a13";
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(x, y - 7);
      ctx.lineTo(x + 5, y);
      ctx.lineTo(x, y + 7);
      ctx.lineTo(x - 5, y);
      ctx.closePath();
      ctx.fill();
      ctx.stroke();
      ctx.restore();
    }
    $("surveyor-position").textContent =
      `Surveyor ${state.player[0]}, ${state.player[1]} · ${state.steps} steps`;
  }
  function pointFor(p) {
    const c = data.navigation.cellSize;
    return [(p[0] + 0.5) * c, (p[1] + 0.5) * c];
  }
  function stopRoute() {
    clearInterval(timer);
    timer = null;
    if (state) state.route = [];
  }
  function walkTo(p) {
    stopRoute();
    const route = findPath(data, state, state.player, p);
    if (!route) {
      announce("No clear route. Open doors or remove the blocking component.");
      return;
    }
    state.route = route;
    draw();
    timer = setInterval(() => {
      const next = state.route.shift();
      if (!next) {
        stopRoute();
        save();
        draw();
        return;
      }
      const result = moveOne(data, state, next);
      if (!result.ok) {
        stopRoute();
        announce(result.reason);
      }
      draw();
    }, 45);
  }
  function pick(p, forWalking = false) {
    const layers = visibleLayers();
    for (let i = layers.length - 1; i >= 0; i--) {
      const l = layers[i];
      if (forWalking && ["interior", "base"].includes(l.role)) continue;
      const b = l.bounds,
        x = Math.floor(p[0] - b[0]),
        y = Math.floor(p[1] - b[1]);
      if (x < 0 || y < 0 || x >= b[2] || y >= b[3]) continue;
      const raster = layerPixels.get(l.id);
      if (raster && raster.alpha[(y * b[2] + x) * 4 + 3] > 20) return l.ownerId;
    }
    return null;
  }
  function eventPoint(e) {
    const r = canvas.getBoundingClientRect();
    return [
      ((e.clientX - r.left) * canvas.width) / r.width,
      ((e.clientY - r.top) * canvas.height) / r.height,
    ];
  }
  function update() {
    if (!data) return;
    for (const mode of ["walk", "inspect"])
      $("mode-" + mode).setAttribute("aria-pressed", state.mode === mode);
    for (const view of ["reconstruction", "original", "base"])
      $("view-" + view).setAttribute("aria-pressed", state.view === view);
    $("mode-help").textContent =
      state.view !== "reconstruction"
        ? "Comparison view · choose Reconstruction to interact"
        : state.mode === "walk"
          ? "WASD / arrows · click ground to walk · click an object to inspect"
          : "Click the artwork or inventory to select its actual component";
    $("component-count").textContent = data.components.length;
    $("hidden-count").textContent = state.hidden.size + " lifted";
    $("masks").checked = state.masks;
    $("grid").checked = state.grid;
    renderList();
    renderSelection();
    draw();
  }
  function renderList() {
    const q = $("search").value.trim().toLowerCase(),
      list = $("component-list");
    list.replaceChildren();
    for (const o of data.components) {
      if (
        q &&
        !`${o.name} ${o.kind} ${o.description}`.toLowerCase().includes(q)
      )
        continue;
      const b = document.createElement("button");
      b.className = "object-row";
      b.type = "button";
      b.setAttribute("aria-pressed", state.selectedId === o.id);
      const label = document.createElement("span");
      label.textContent = o.name;
      const meta = document.createElement("small");
      meta.textContent = state.hidden.has(o.id)
        ? "lifted"
        : !ownerVisible(data, state, o.id)
          ? "under roof"
          : o.kind.replaceAll("-", " ");
      b.append(label, meta);
      b.onclick = () => select(o.id);
      list.append(b);
    }
    if (!list.childElementCount) {
      const p = document.createElement("p");
      p.className = "empty";
      p.textContent = "No components match your search.";
      list.append(p);
    }
  }
  function select(id) {
    state.selectedId = id;
    hoverId = null;
    $("conversation").hidden = true;
    update();
  }
  function button(label, handler) {
    const b = document.createElement("button");
    b.textContent = label;
    b.type = "button";
    b.onclick = handler;
    return b;
  }
  function renderSelection() {
    const o = component(),
      actions = $("actions");
    actions.replaceChildren();
    $("selected-name").textContent = o?.name || "Choose a component";
    $("description").textContent =
      o?.description ||
      "Inspect the architecture, meet the watch, or lift a roof to reveal the room beneath.";
    $("selected-state").textContent = o
      ? state.hidden.has(o.id)
        ? "Lifted from the reconstruction"
        : !ownerVisible(data, state, o.id)
          ? "Inside a closed roof"
          : "Visible · " + o.kind.replaceAll("-", " ")
      : "Select the artwork or a name above.";
    const preview = $("object-preview"),
      pc = preview.getContext("2d");
    if (!o) {
      preview.hidden = true;
      $("component-meta").textContent = "";
      return;
    }
    preview.hidden = false;
    preview.width = o.bounds[2];
    preview.height = o.bounds[3];
    pc.clearRect(0, 0, preview.width, preview.height);
    pc.imageSmoothingEnabled = false;
    for (const l of index(data).layers.filter((l) => l.ownerId === o.id))
      pc.drawImage(
        images.get(l.path),
        l.bounds[0] - o.bounds[0],
        l.bounds[1] - o.bounds[1],
        l.bounds[2],
        l.bounds[3],
      );
    const room = roomFor(o),
      isDoor = room?.doorId === o.id || o.kind === "door";
    if (o.mutable) {
      const label = primaryActionLabel(data, state, o.id);
      actions.append(
        button(label, () => {
          const r = setHidden(data, state, o.id, !state.hidden.has(o.id));
          if (r.ok) stopRoute();
          announce(
            r.ok
              ? `${o.name}: ${state.hidden.has(o.id) ? (isDoor ? "opened" : "lifted") : "restored"}. Backing and collision updated.`
              : r.reason,
          );
          if (r.ok) save();
          update();
        }),
      );
    }
    if (o.visibleWhen === "room-open" && !ownerVisible(data, state, o.id)) {
      const r = index(data).rooms.get(o.roomId);
      actions.append(
        button("Lift room roof", () => {
          setHidden(data, state, r.roofId, true);
          save();
          update();
        }),
      );
    }
    if (
      o.actions.some((a) => /talk|chat|trade|shop/i.test(a)) ||
      /npc|resident|guard|merchant|cook/i.test(o.kind)
    )
      actions.append(button("Meet / browse", () => showCard(o)));
    actions.append(
      button("Locate", () => {
        hoverId = o.id;
        draw();
        canvas.focus({ preventScroll: true });
      }),
    );
    $("component-meta").textContent =
      `${o.id} · ${o.layerIds.length} layer${o.layerIds.length === 1 ? "" : "s"} · ${o.blocksMovement ? o.footprintCells.length + " collision cells" : "non-blocking"}`;
  }
  function showCard(o) {
    const panel = $("conversation");
    panel.hidden = false;
    $("card-title").textContent = o.name;
    $("card-text").textContent = Array.isArray(o.dialogue)
      ? o.dialogue.join("\n\n")
      : typeof o.dialogue === "string"
        ? o.dialogue
        : o.description;
    const stock = $("card-stock");
    stock.replaceChildren();
    if (Array.isArray(o.stock))
      for (const item of o.stock) {
        const p = document.createElement("p");
        p.textContent =
          typeof item === "string"
            ? item
            : `${item.name}${item.quantity != null ? " × " + item.quantity : ""}${item.price != null ? " · " + item.price + " drams" : ""}${item.status ? " · " + item.status : ""}`;
        stock.append(p);
      }
    else if (o.actions.some((a) => /trade|shop/i.test(a))) {
      const p = document.createElement("p");
      p.textContent =
        "Stock and dialogue design are represented here. Buying, currency and inventory will use the native Unity trade system during integration.";
      stock.append(p);
    }
    announce("Opened an authored character / shop preview.");
  }
  async function boot() {
    try {
      const response = await fetch("build/manifest.json", {
        cache: "no-store",
      });
      if (!response.ok)
        throw new Error(
          "The scene export is not ready (" + response.status + ").",
        );
      data = validateScene(await response.json());
      canvas.width = data.canvas.width;
      canvas.height = data.canvas.height;
      await Promise.all(
        [
          ...new Set([
            data.base.path,
            data.baseline.path,
            ...data.layers.map((l) => l.path),
          ]),
        ].map(loadImage),
      );
      for (const l of data.layers) {
        const img = images.get(l.path);
        if (
          img.naturalWidth !== l.bounds[2] ||
          img.naturalHeight !== l.bounds[3]
        )
          throw new Error("Export dimensions disagree for " + l.id);
        const tile = document.createElement("canvas");
        tile.width = l.bounds[2];
        tile.height = l.bounds[3];
        const tc = tile.getContext("2d", { willReadFrequently: true });
        tc.drawImage(img, 0, 0);
        const alpha = tc.getImageData(0, 0, tile.width, tile.height).data;
        tc.globalCompositeOperation = "source-in";
        tc.fillStyle = "rgba(103,214,186,.36)";
        tc.fillRect(0, 0, tile.width, tile.height);
        layerPixels.set(l.id, { alpha, tint: tile });
      }
      state = createState(data);
      try {
        const old = localStorage.getItem(storageKey);
        if (old) state = loadState(data, old);
      } catch (e) {
        announce("Previous workbench state was reset: " + e.message);
      }
      $("scene-title").textContent = data.title;
      $("loading").hidden = true;
      $("build-summary").textContent =
        `${data.components.length} semantic components · ${data.layers.length} individual visual layers · ${data.rooms.length} furnished interiors`;
      $("source-link").href = data.baseline.path;
      update();
      window.morrowfastWorkbench = {
        get data() {
          return data;
        },
        get state() {
          return state;
        },
        select,
        draw,
        reset: () => {
          $("reset").click();
        },
        report: () => ({
          scene: data.id,
          components: data.components.length,
          layers: data.layers.length,
          rooms: data.rooms.length,
          loaded: images.size,
          hidden: [...state.hidden],
          player: [...state.player],
          saveAvailable,
        }),
      };
      announce(
        "Morrowfast is ready. Lift a roof to inspect its reconstructed interior.",
      );
    } catch (e) {
      $("loading").hidden = true;
      message(e.message);
      console.error(e);
    }
  }
  canvas.addEventListener("pointermove", (e) => {
    if (!state || state.view !== "reconstruction") return;
    const id = pick(eventPoint(e));
    if (id !== hoverId) {
      hoverId = id;
      canvas.style.cursor = id
        ? "pointer"
        : state.mode === "walk"
          ? "crosshair"
          : "default";
      draw();
    }
  });
  canvas.addEventListener("pointerleave", () => {
    hoverId = null;
    draw();
  });
  canvas.addEventListener("click", (e) => {
    if (!state || state.view !== "reconstruction") return;
    const p = eventPoint(e),
      id = pick(p, state.mode === "walk");
    if (id) {
      select(id);
      return;
    }
    if (state.mode === "walk")
      walkTo(p.map((v) => Math.floor(v / data.navigation.cellSize)));
    else select(null);
    canvas.focus({ preventScroll: true });
  });
  document.addEventListener("keydown", (e) => {
    if (
      !state ||
      e.ctrlKey ||
      e.metaKey ||
      e.altKey ||
      /INPUT|TEXTAREA|SELECT|BUTTON/.test(document.activeElement?.tagName)
    )
      return;
    const direction = {
      ArrowUp: [0, -1],
      w: [0, -1],
      W: [0, -1],
      ArrowDown: [0, 1],
      s: [0, 1],
      S: [0, 1],
      ArrowLeft: [-1, 0],
      a: [-1, 0],
      A: [-1, 0],
      ArrowRight: [1, 0],
      d: [1, 0],
      D: [1, 0],
    }[e.key];
    if (!direction) return;
    e.preventDefault();
    stopRoute();
    const result = moveOne(data, state, [
      state.player[0] + direction[0],
      state.player[1] + direction[1],
    ]);
    if (!result.ok) announce(result.reason);
    else save();
    draw();
  });
  for (const mode of ["walk", "inspect"])
    $("mode-" + mode).onclick = () => {
      if (!state) return;
      stopRoute();
      state.mode = mode;
      update();
      canvas.focus({ preventScroll: true });
    };
  for (const view of ["reconstruction", "original", "base"])
    $("view-" + view).onclick = () => {
      if (!state) return;
      stopRoute();
      state.view = view;
      update();
      canvas.focus({ preventScroll: true });
    };
  for (const option of ["masks", "grid"])
    $(option).onchange = () => {
      if (state) {
        state[option] = $(option).checked;
        draw();
      }
    };
  $("search").oninput = () => {
    if (data) renderList();
  };
  $("reset").onclick = () => {
    if (!data) return;
    stopRoute();
    state = createState(data);
    hoverId = null;
    $("conversation").hidden = true;
    save();
    update();
    announce(
      "All components restored. Surveyor returned to the southern arrival.",
    );
  };
  $("lift-outdoors").onclick = () => {
    if (!data) return;
    const result = hideOutdoor(data, state);
    stopRoute();
    save();
    update();
    announce(
      result.refused.length
        ? "Outdoor props lifted; a bridge supporting the surveyor or queued route stayed in place."
        : "Outdoor movable components lifted. Fixed structures and interiors preserved.",
    );
  };
  $("open-interiors").onclick = () => {
    if (!data) return;
    stopRoute();
    openAllRooms(data, state);
    save();
    update();
    announce("All roofs lifted; door collision remains unchanged.");
  };
  $("restore-all").onclick = () => {
    if (!data) return;
    stopRoute();
    let refused = 0;
    for (const id of [...state.hidden])
      if (!setHidden(data, state, id, false).ok) refused++;
    save();
    update();
    announce(
      refused
        ? "Some blockers remain lifted until the surveyor moves clear."
        : "All lifted components restored.",
    );
  };
  $("close-card").onclick = () => {
    $("conversation").hidden = true;
  };
  $("export-state").onclick = () => {
    if (!state) return;
    const a = document.createElement("a"),
      url = URL.createObjectURL(
        new Blob([JSON.stringify(serializeState(data, state), null, 2)], {
          type: "application/json",
        }),
      );
    a.href = url;
    a.download = "morrowfast-workbench-state.json";
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  };
  $("import-state").onchange = async (e) => {
    const f = e.target.files[0];
    if (!f || !data) return;
    try {
      const loaded = loadState(data, await f.text());
      stopRoute();
      state = loaded;
      save();
      update();
      announce("Workbench state restored from file.");
    } catch (err) {
      announce("Import refused: " + err.message);
    }
    e.target.value = "";
  };
  boot();
})();
