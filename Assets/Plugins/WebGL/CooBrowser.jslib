mergeInto(LibraryManager.library, {
  CooPhoneView: function (jsonPtr) {
    window.dispatchEvent(new CustomEvent('coo-view', { detail: JSON.parse(UTF8ToString(jsonPtr)) }));
  },
  CooStorageInitialize: function (receiverPtr, rootPtr) {
    var receiver = UTF8ToString(receiverPtr), root = UTF8ToString(rootPtr);
    var reply = function (error) {
      SendMessage(receiver, 'StorageReply', JSON.stringify({ operation: 0, error: error || '' }));
    };
    try {
      if (!window.CooBrowserSaveStore) throw new Error('Browser storage module did not load. Reload this page.');
      window.cooResumeAudio = function () {
        if (typeof WEBAudio !== "undefined" && WEBAudio.audioContext && WEBAudio.audioContext.state !== "running")
          WEBAudio.audioContext.resume().catch(function () {});
      };
      var store = new window.CooBrowserSaveStore();
      store.onUnavailable = function (message) {
        SendMessage(receiver, "StorageUnavailable", message);
        window.dispatchEvent(new CustomEvent("coo-storage", { detail: { state: "error", message: message } }));
      };
      window.cooSaveStore = store;
      window.cooSaveRoot = root;
      store.hydrate().then(function (records) {
        FS.mkdirTree(root);
        records.forEach(function (record) {
          var dir = root + '/' + record.gameId;
          FS.mkdirTree(dir);
          FS.writeFile(dir + '/' + record.slot + '.sav.gz', new Uint8Array(record.payload));
          FS.writeFile(dir + '/' + record.slot + '.json', record.metadata);
        });
        window.dispatchEvent(new CustomEvent('coo-storage', { detail: {
          state: 'ready', recovered: records.some(function (r) { return r.recovered; })
        }}));
        reply(null);
      }).catch(function (error) {
        store.ready = false;
        window.dispatchEvent(new CustomEvent('coo-storage', { detail: { state: 'error', message: error.message } }));
        reply(error.message);
      });
    } catch (error) {
      window.dispatchEvent(new CustomEvent('coo-storage', { detail: { state: 'error', message: error.message } }));
      reply(error.message);
    }
  },
  CooStorageCommit: function (receiverPtr, operation, gamePtr, slotPtr, payloadPtr, length, metadataPtr) {
    var receiver = UTF8ToString(receiverPtr), gameId = UTF8ToString(gamePtr), slot = UTF8ToString(slotPtr);
    var metadata = UTF8ToString(metadataPtr), payload = HEAPU8.slice(payloadPtr, payloadPtr + length);
    var reply = function (error) {
      SendMessage(receiver, 'StorageReply', JSON.stringify({ operation: operation, error: error || '', unavailable: !window.cooSaveStore.ready }));
    };
    var store = window.cooSaveStore;
    window.dispatchEvent(new CustomEvent('coo-storage', { detail: { state: 'saving' } }));
    store.commit(gameId, slot, payload, metadata).then(function (record) {
      try {
        var dir = window.cooSaveRoot + '/' + record.gameId;
        FS.mkdirTree(dir);
        FS.writeFile(dir + '/' + record.slot + '.sav.gz', new Uint8Array(record.payload));
        FS.writeFile(dir + '/' + record.slot + '.json', record.metadata);
      } catch (error) {
        store.ready = false;
        throw new Error('Checkpoint reached browser storage, but could not be reopened. Reload to recover it. ' + error.message);
      }
      window.dispatchEvent(new CustomEvent('coo-storage', { detail: { state: 'saved' } }));
      reply(null);
    }).catch(function (error) {
      window.dispatchEvent(new CustomEvent('coo-storage', { detail: { state: 'error', message: error.message } }));
      reply(error.message);
    });
  }
});
