using System.Runtime.InteropServices;

namespace ScreenShareTool
{
    internal static class ScreenShareVideoPump
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int ScreenShare_ImmersiveActive();

        [DllImport("__Internal")]
        private static extern void ScreenShare_PumpVideoFrame(int texId);

        public static bool ImmersiveActive() => ScreenShare_ImmersiveActive() == 1;
        public static void PumpFrame(int texId) => ScreenShare_PumpVideoFrame(texId);
#else
        public static bool ImmersiveActive() => false;
        public static void PumpFrame(int texId) { }
#endif
    }
}