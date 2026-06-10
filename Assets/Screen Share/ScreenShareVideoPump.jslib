mergeInto(LibraryManager.library, {
    ScreenShare_ImmersiveActive: function () {
        return (typeof window !== 'undefined' && window.__ssImmersive) ? 1 : 0;
    },

    ScreenShare_PumpVideoFrame: function (texId) {
        var tex = GL.textures[texId];
        if (!tex) return;

        // The SDK appends the remote presenter's <video> to the page. A WebRTC stream
        // is the one playing element with a srcObject.
        var vids = document.getElementsByTagName('video');
        var video = null;
        for (var i = 0; i < vids.length; i++) {
            if (!vids[i].paused && vids[i].srcObject) { video = vids[i]; break; }
        }
        if (!video) return;

        GLctx.bindTexture(GLctx.TEXTURE_2D, tex);
        GLctx.pixelStorei(GLctx.UNPACK_FLIP_Y_WEBGL, true);
        GLctx.texImage2D(GLctx.TEXTURE_2D, 0, GLctx.RGBA, GLctx.RGBA, GLctx.UNSIGNED_BYTE, video);
        GLctx.pixelStorei(GLctx.UNPACK_FLIP_Y_WEBGL, false);

        GLctx.texParameteri(GLctx.TEXTURE_2D, GLctx.TEXTURE_MAG_FILTER, GLctx.LINEAR);
        GLctx.texParameteri(GLctx.TEXTURE_2D, GLctx.TEXTURE_MIN_FILTER, GLctx.LINEAR);
        GLctx.texParameteri(GLctx.TEXTURE_2D, GLctx.TEXTURE_WRAP_S, GLctx.CLAMP_TO_EDGE);
        GLctx.texParameteri(GLctx.TEXTURE_2D, GLctx.TEXTURE_WRAP_T, GLctx.CLAMP_TO_EDGE);
    }
});