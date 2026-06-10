// LiveKit's LKBridge.DynCall calls the legacy Emscripten `dynCall` global,
// which Unity 6 no longer exports. This restores it using getWasmTableEntry,
// which is Unity 6's replacement. Safe to include alongside De-Panther WebXR
// Export — the typeof guard prevents double-definition.
Module['preRun'].push(function () {
  if (typeof getWasmTableEntry !== "undefined" && typeof dynCall === "undefined") {
    window.dynCall = function(sig, ptr, args) {
      return getWasmTableEntry(ptr).apply(null, args || []);
    };
  }
});