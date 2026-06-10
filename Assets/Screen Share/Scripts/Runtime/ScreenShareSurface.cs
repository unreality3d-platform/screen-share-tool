using UnityEngine;

namespace ScreenShareTool
{
    public class ScreenShareSurface : MonoBehaviour
    {
        [SerializeField] ScreenShareRoom room;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] string materialTextureProperty = "_BaseMap";
        [SerializeField] Texture2D placeholderTexture;

        int _glTexId;

        void Awake()
        {
            if (targetRenderer != null && targetRenderer.sharedMaterial == null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader != null)
                    targetRenderer.material = new Material(unlitShader);
                else
                    Debug.LogWarning("[ScreenShareSurface] URP/Unlit shader not found. Assign an Unlit material to the Renderer manually.", this);
            }
        }

        void Start()
        {
            if (room == null)
            {
                Debug.LogWarning("[ScreenShareSurface] No ScreenShareRoom assigned.", this);
                return;
            }
            if (targetRenderer == null)
                Debug.LogWarning("[ScreenShareSurface] No display Renderer assigned.", this);
        }

        void OnEnable()
        {
            if (room == null) return;
            room.OnPresenterVideoReceived.AddListener(HandleVideoReceived);
            room.OnPresenterVideoRemoved.AddListener(HandleVideoRemoved);

            if (room.CurrentPresenterTexture != null)
                HandleVideoReceived(room.CurrentPresenterTexture, room.CurrentPresenterIdentity);
        }

        void OnDisable()
        {
            if (room == null) return;
            room.OnPresenterVideoReceived.RemoveListener(HandleVideoReceived);
            room.OnPresenterVideoRemoved.RemoveListener(HandleVideoRemoved);
        }

        void Update()
        {
            // The SDK refreshes the video texture on the page's animation-frame loop,
            // which the browser pauses during an immersive WebXR session. While a headset
            // session is active we re-upload the current frame ourselves from Unity's
            // render loop (driven by the XR frame loop). Outside VR the SDK's own loop
            // handles it, so we skip.
            if (_glTexId == 0) return;
            if (!ScreenShareVideoPump.ImmersiveActive()) return;
            ScreenShareVideoPump.PumpFrame(_glTexId);
        }

        void HandleVideoReceived(Texture2D texture, string presenterIdentity)
        {
            if (texture == null) return;

            _glTexId = (int)texture.GetNativeTexturePtr();
            ApplyTexture(texture);
        }

        void HandleVideoRemoved(string presenterIdentity)
        {
            _glTexId = 0;

            Texture clear = placeholderTexture != null ? (Texture)placeholderTexture : Texture2D.blackTexture;
            ApplyTexture(clear);
        }

        void ApplyTexture(Texture texture)
        {
            if (targetRenderer != null)
                targetRenderer.material.SetTexture(materialTextureProperty, texture);
        }
    }
}