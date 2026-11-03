(function () {
  'use strict';
  const $ = id => document.getElementById(id);
  let view = null, engine = null, starting = false, storageReady = false, engineReady = false;
  let pending = false, buildId = null, pressedRevision = null;
  document.addEventListener("pointerdown", () => { pressedRevision = view?.revision ?? null; }, true);
  document.addEventListener("click", event => {
    if (view && event.detail > 0 && pressedRevision !== view.revision && event.target.closest("button"))
    { event.preventDefault(); event.stopImmediatePropagation(); }
  }, true);
  const note = text => { $('save-status').textContent = text; };
  const message = error => error?.message || String(error);
  const fatal = error => { $('error-message').textContent = message(error); $('error-panel').hidden = false; };
  const send = intent => {
    if (!engine || !view || pending || document.hidden) return false;
    pending = true;
    engine.SendMessage('CooBrowser', 'PhoneIntent', JSON.stringify({ ...intent, revision: view.revision }));
    return true;
  };
  const key = value => send({ kind: 'key', value });
  function visibleGame() { if (storageReady && engineReady) $('launch').hidden = true; }
  async function identity() {
    if (!buildId) {
      const response = await fetch('build-info.json', { cache: 'no-store' });
      if (!response.ok) throw new Error('The game build information is missing.');
      buildId = (await response.json()).source;
    }
    return buildId;
  }
  async function start() {
    if (starting) return;
    starting = true; $('play').disabled = true; $('import-save').disabled = true;
    $('loading').hidden = false; $('launch-status').textContent = 'Opening the world…';
    try {
      await identity();
      if (!window.WebAssembly || !document.createElement('canvas').getContext('webgl2'))
        throw new Error('This browser needs WebAssembly and WebGL 2 to open the game.');
      const script = document.createElement('script'); script.src = window.cooBuildConfig.loaderUrl;
      await new Promise((resolve, reject) => { script.onload = resolve; script.onerror = () => reject(new Error('The game download failed. Check your connection and reload.')); document.body.append(script); });
      engine = await createUnityInstance($('unity-canvas'), { ...window.cooBuildConfig,
        showBanner: (text, type) => { if (type === 'error') fatal(text); }
      }, progress => { $('loading').value = progress; });
      window.cooUnity = engine; engineReady = true;
      visibleGame();
      if ('serviceWorker' in navigator) navigator.serviceWorker.register('service-worker.js').catch(() => {});
    } catch (error) { fatal(error); }
  }
  $('play').addEventListener('click', start);
  $('reload').addEventListener('click', () => location.reload());
  document.addEventListener('click', () => window.cooResumeAudio?.(), { capture: true });
  document.querySelectorAll('[data-step]').forEach(button => button.addEventListener('click', () => {
    const [x, y] = button.dataset.step.split(',').map(Number); send({ kind: 'step', x, y });
  }));
  document.querySelectorAll('[data-command]').forEach(button => button.addEventListener('click', () => {
    send({ kind: 'command', value: button.dataset.command }); $('more-actions').hidden = true;
  }));
  document.querySelectorAll('[data-key]').forEach(button => button.addEventListener('click', () => key(button.dataset.key)));
  $('wait').addEventListener('click', () => view?.mode === 'Targeting' ? key('Return') : send({ kind: 'wait' }));
  $('reader-back').addEventListener('click', () => key('Escape'));
  $('more-toggle').addEventListener('click', () => {
    $('more-actions').hidden = !$('more-actions').hidden;
    if (!$('more-actions').hidden) { pending = true; engine?.SendMessage('CooBrowser', 'Overlay', 'more'); }
    $('more-toggle').setAttribute('aria-expanded', String(!$('more-actions').hidden));
  });
  $('journal-toggle').addEventListener('click', () => { $('journal').hidden = false; pending = true; engine?.SendMessage('CooBrowser', 'Overlay', 'journal'); });
  $('journal-close').addEventListener('click', () => { $('journal').hidden = true; pending = true; engine?.SendMessage('CooBrowser', 'Overlay', 'closed'); });
  let pointer = null;
  $('map-touch').addEventListener('pointerdown', event => { event.preventDefault(); pointer = { x: event.clientX, y: event.clientY, id: event.pointerId }; });
  $('map-touch').addEventListener('pointerup', event => {
    event.preventDefault();
    const first = pointer; pointer = null;
    if (!first || first.id !== event.pointerId || Math.hypot(first.x - event.clientX, first.y - event.clientY) > 10 || view?.mode !== 'World') return;
    const rect = $('map-touch').getBoundingClientRect();
    send({ kind: 'inspect', screenX: (event.clientX - rect.left) / rect.width, screenY: (event.clientY - rect.top) / rect.height });
  });
  $('map-touch').addEventListener('pointercancel', () => { pointer = null; });
  function quarantine(hidden) {
    pointer = null; pending = true; pressedRevision = null;
    engine?.SendMessage('CooBrowser', 'Visibility', hidden ? 'hidden' : 'visible');
  }
  document.addEventListener('visibilitychange', () => quarantine(document.hidden));
  window.addEventListener('pagehide', () => quarantine(true));
  window.addEventListener('pageshow', () => { if (engine) quarantine(false); });
  window.addEventListener('blur', () => quarantine(true));
  window.addEventListener('focus', () => { if (engine) quarantine(false); });
  window.addEventListener('orientationchange', () => quarantine(false));
  $('name-form').addEventListener('submit', event => { event.preventDefault(); send({ kind: 'text', value: $('loadout-name').value }); $('loadout-name').blur(); });

  const shortcuts = text => {
    const found = new Map();
    const allowed = value => /^[A-Za-z0-9]$|^(Esc|Enter|Tab|Space|PageUp|PageDown|Home|End|F1|F2|F5|F6)$/.test(value);
    for (const match of text.matchAll(/\[([A-Za-z0-9,+.=\/-]+)\]([^\[\]\n]*)/g))
      for (const value of match[1].split('/')) if (allowed(value)) {
        const description = match[2].trim().split(/ {2,}/)[0].slice(0, 30);
        found.set(value, description ? description + ' · ' + value : value);
      }
    for (const match of text.matchAll(/(?:^|\s)([A-Za-z0-9/]+):\s*([^:\n]*?)(?=\s+[A-Za-z0-9/]+:|$)/gm))
      for (const value of match[1].split('/')) if (allowed(value))
        found.set(value, match[2].trim().slice(0, 30) + ' · ' + value);
    return found;
  };
  window.addEventListener('coo-view', event => {
    const next = event.detail; const contextChanged = !view || view.mode !== next.mode || view.title !== next.title || view.modal !== next.modal;
    view = next; pending = false;
    $('vitals').textContent = view.hp || '';
    document.querySelector('[data-command=ActivateAbility]').textContent = view.ability || 'Use ability';
    const menu = view.mode === 'Menu' || view.mode === 'Skills';
    $('reader').hidden = !menu;
    document.body.classList.toggle('targeting', view.mode === 'Targeting');
    document.body.classList.toggle('menu-open', menu);
    $('wait').disabled = view.mode === 'Targeting';
    $('wait').setAttribute('aria-label', view.mode === 'Targeting' ? 'Choose a direction, then confirm below' : 'Wait one turn');
    $('target-actions').hidden = view.mode !== 'Targeting';
    $('target-reader').hidden = view.mode !== 'Targeting' || !view.rows?.length;
    $('target-reader').replaceChildren(...(view.rows || []).map(row => { const p = document.createElement('p'); p.textContent = row.text; return p; }));
    $('controls').hidden = menu;
    const title = view.boot ? 'Continue your expedition' : view.dead ? 'Your expedition has ended'
      : view.naming ? 'Name your loadout' : (view.title || '').replace(/([a-z])([A-Z])/g, '$1 $2').replace(/ Open$/, '');
    $('reader-title').textContent = title;
    const scroll = contextChanged ? 0 : $('reader-content').scrollTop;
    $('reader-content').replaceChildren(...(view.rows || []).map(row => {
      const p = document.createElement('p'); p.textContent = row.text;
      if (/^#[0-9a-f]{6}$/i.test(row.color || "")) {
        const rgb = [1, 3, 5].map(i => parseInt(row.color.slice(i, i + 2), 16));
        if (rgb[0] * .2126 + rgb[1] * .7152 + rgb[2] * .0722 > 110) p.style.color = row.color;
      }
      return p;
    }));
    $('reader-content').scrollTop = scroll;
    $('name-form').hidden = !view.naming;
    if (view.naming && document.activeElement !== $('loadout-name')) $('loadout-name').value = view.name || '';
    const keys = shortcuts((view.rows || []).map(row => row.text).join('\n'));
    if (view.title === 'InventoryOpen' && (!view.modal || view.modal.startsWith('inventory:base:'))) keys.set('I', 'Actions');
    // Navigation already has dedicated controls; leave the reader room to breathe.
    for (const duplicate of ['Enter', 'Esc', 'Tab', 'PageUp', 'PageDown']) keys.delete(duplicate);
    if (view.boot) { keys.set('C', 'Continue'); keys.set('N', 'New expedition'); }
    if (view.dead) { keys.set('L', 'Load checkpoint'); keys.set('R', 'Restart'); }
    $('menu-shortcuts').hidden = keys.size === 0;
    $('menu-shortcuts').replaceChildren(...Array.from(keys, ([value, label]) => {
      const button = document.createElement('button'); button.textContent = label; button.dataset.key = value;
      button.addEventListener('click', () => key(value)); return button;
    }));
    const messages = view.messages || [];
    $('latest-message').textContent = messages[messages.length - 1] || '';
    $('journal-content').replaceChildren(...messages.map(text => { const p = document.createElement('p'); p.textContent = text; return p; }));
  });
  window.addEventListener('coo-storage', event => {
    const state = event.detail;
    if (state.state === 'ready') { storageReady = true; note(state.recovered ? 'Recovered the previous checkpoint' : 'Ready'); visibleGame(); }
    if (state.state === 'saving') note('Saving…');
    if (state.state === 'saved') note('Saved on this device');
    if (state.state === 'error') {
      note('Save unavailable: ' + state.message);
      if (!storageReady) fatal(state.message);
    }
  });
  $('recovery-export').addEventListener('click', async () => {
    try {
      const store = window.cooSaveStore || new CooBrowserSaveStore();
      const bundle = await store.exportRecovery(await identity());
      const url = URL.createObjectURL(new Blob([JSON.stringify(bundle)], { type: 'application/json' }));
      const link = document.createElement('a'); link.href = url; link.download = 'caves-of-ooo-recovery.json'; link.click();
      setTimeout(() => URL.revokeObjectURL(url), 60000);
    } catch (error) { $('error-message').textContent = message(error); }
  });
  $('storage-persist').addEventListener('click', async () => {
    const granted = await navigator.storage?.persist?.();
    note(granted ? 'Persistent storage granted. Keep an exported backup too.' : 'This browser manages storage automatically. Export a backup to keep it safe.');
  });
  $('export-save').addEventListener('click', async () => {
    try {
      if (!window.cooSaveStore || window.cooSaveStore.busy) throw new Error('Wait for the current save to finish, then export.');
      const bundle = await window.cooSaveStore.exportBackup(await identity());
      const url = URL.createObjectURL(new Blob([JSON.stringify(bundle)], { type: 'application/json' }));
      const link = document.createElement('a'); link.href = url; link.download = 'caves-of-ooo-backup.json'; link.click();
      setTimeout(() => URL.revokeObjectURL(url), 60000); note('Backup exported');
    } catch (error) { note(message(error)); }
  });
  $('import-save').addEventListener('change', async event => {
    const file = event.target.files?.[0]; if (!file || starting) return;
    $('play').disabled = true;
    try {
      if (file.size > 128 * 1024 * 1024) throw new Error('This backup is too large.');
      const store = new CooBrowserSaveStore(); await store.hydrate();
      await store.importBackup(JSON.parse(await file.text()), await identity());
      $('launch-status').textContent = 'Backup imported. Begin to continue your expedition.';
    } catch (error) { $('launch-status').textContent = message(error); }
    finally { $('play').disabled = false; event.target.value = ''; }
  });
})();
