# Screen Share Tool

Live screen sharing for Unity WebGL and WebXR experiences, viewable right in the browser.

A drop-in package for Unity WebGL/WebXR projects. Add it to a scene, point it at your own server, and visitors can share a desktop screen into the space and watch it together on a surface you place. Nothing is installed to view or share; it all runs in the browser.

Works in Unreality3D and any other Unity WebGL or WebXR host.

## What works where

- Watching a shared screen: desktop, mobile, and WebXR VR browsers.
- Sharing your screen: desktop browsers.
- Browser-based, so it does not run in standalone (native) builds.

## Requirements

- Unity 6
- The LiveKit WebGL SDK (`client-sdk-unity-web`). The importer offers to install it for you. You can also add it through Package Manager with the git URL `https://github.com/livekit/client-sdk-unity-web.git#v2.0.0`.
- Your own token server (a small serverless function). The setup guide walks through standing one up.

## Install

1. Download the latest `ScreenShareTool.unitypackage` from [Releases](https://github.com/unreality3d-platform/screen-share-tool/releases/latest).
2. In Unity: **Assets > Import Package > Custom Package**, choose the file, and import.
3. When prompted, click **Install LiveKit SDK** (or add it through the git URL above).
4. In your scene, use the menu **Screen Share > Add Screen Share Setup**.
5. Open the **ScreenShareSettings** asset and fill in your Server URL and Token Endpoint URL.

Full setup, including standing up your token server, is on the [project page](https://unreality3d-platform.github.io/screen-share-tool/).

## Repository layout

- `Assets/Screen Share/`: the tool itself (runtime scripts, the editor setup tool, and the WebGL plugins).
- `token-server/`: an example serverless token function and its `package.json`.
- `index.html`: the project page and setup guide, served through GitHub Pages.

## License

MIT. See [LICENSE](LICENSE).
