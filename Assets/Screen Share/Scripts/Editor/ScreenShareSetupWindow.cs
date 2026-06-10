using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ScreenShareTool.Editor
{
    [InitializeOnLoad]
    public static class ScreenShareDependencyChecker
    {
        private const string LIVEKIT_IDENTIFIER = "client-sdk-unity-web";
        private const string SESSION_KEY = "ScreenShareTool_LiveKitPrompted";

        static ScreenShareDependencyChecker()
        {
            if (!IsLiveKitInstalled() && !SessionState.GetBool(SESSION_KEY, false))
            {
                SessionState.SetBool(SESSION_KEY, true);
                EditorApplication.delayCall += ScreenShareSetupWindow.Open;
            }
        }

        private static bool IsLiveKitInstalled()
        {
            string manifestPath = System.IO.Path.Combine(
                Application.dataPath, "..", "Packages", "manifest.json");

            if (!System.IO.File.Exists(manifestPath))
                return false;

            return System.IO.File.ReadAllText(manifestPath).Contains(LIVEKIT_IDENTIFIER);
        }
    }

    public class ScreenShareSetupWindow : EditorWindow
    {
        private const string LIVEKIT_GIT_URL =
            "https://github.com/livekit/client-sdk-unity-web.git#v2.0.0";

        private UnityEditor.PackageManager.Requests.AddRequest _addRequest;

        public static void Open()
        {
            var window = GetWindow<ScreenShareSetupWindow>(true, "Screen Share Setup", true);
            window.minSize = new Vector2(440, 170);
            window.maxSize = new Vector2(440, 170);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Screen Share Tool — Required Dependency",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "The Screen Share Tool requires the LiveKit Unity WebGL SDK, " +
                "which is not yet installed in this project.",
                MessageType.Warning);
            EditorGUILayout.Space(6);

            bool installing = _addRequest != null && !_addRequest.IsCompleted;
            bool done = _addRequest != null && _addRequest.IsCompleted;

            if (installing)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Installing LiveKit SDK…");
                EditorGUI.EndDisabledGroup();
            }
            else if (done && _addRequest.Status == StatusCode.Success)
            {
                EditorGUILayout.HelpBox(
                    "LiveKit SDK installed successfully. You're ready to build.",
                    MessageType.Info);
                if (GUILayout.Button("Close"))
                    Close();
            }
            else if (done)
            {
                EditorGUILayout.HelpBox(
                    "Install failed: " + (_addRequest.Error?.message ?? "unknown error"),
                    MessageType.Error);
                if (GUILayout.Button("Retry"))
                    StartInstall();
            }
            else
            {
                if (GUILayout.Button("Install LiveKit SDK"))
                    StartInstall();
            }
        }

        private void StartInstall()
        {
            _addRequest = Client.Add(LIVEKIT_GIT_URL);
        }

        private void Update()
        {
            if (_addRequest != null)
                Repaint();
        }
    }
}