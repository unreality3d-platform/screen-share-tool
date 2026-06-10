(function () {
    function patchXR() {
        if (!navigator.xr || navigator.xr.__ssSessionPatched) return;
        navigator.xr.__ssSessionPatched = true;
        var orig = navigator.xr.requestSession.bind(navigator.xr);
        navigator.xr.requestSession = function (mode, options) {
            return orig(mode, options).then(function (session) {
                if (mode === 'immersive-vr' || mode === 'immersive-ar') {
                    window.__ssImmersive = true;
                    session.addEventListener('end', function () { window.__ssImmersive = false; });
                }
                return session;
            });
        };
    }
    if (typeof navigator !== 'undefined' && navigator.xr) patchXR();
})();