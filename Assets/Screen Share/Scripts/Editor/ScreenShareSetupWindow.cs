using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ScreenShareTool.Editor
{
    public static class ScreenShareLiveKitDetection
    {
        private const string MANIFEST_IDENTIFIER = "client-sdk-unity-web";
        private const string PACKAGE_IDENTIFIER = "livekit";

        public static string ProjectRoot =>
            Path.GetDirectoryName(Application.dataPath);

        public static bool IsInstalled()
        {
            return InManifest() || InPackagesFolder() || InAssetsFolder();
        }

        private static bool InManifest()
        {
            string manifestPath = Path.Combine(ProjectRoot, "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
                return false;

            try
            {
                return File.ReadAllText(manifestPath).Contains(MANIFEST_IDENTIFIER);
            }
            catch
            {
                return false;
            }
        }

        private static bool InPackagesFolder()
        {
            string packagesPath = Path.Combine(ProjectRoot, "Packages");
            if (!Directory.Exists(packagesPath))
                return false;

            try
            {
                foreach (string directory in Directory.GetDirectories(packagesPath))
                {
                    string packageJson = Path.Combine(directory, "package.json");
                    if (!File.Exists(packageJson))
                        continue;

                    if (File.ReadAllText(packageJson).ToLowerInvariant().Contains(PACKAGE_IDENTIFIER))
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool InAssetsFolder()
        {
            try
            {
                foreach (string packageJson in Directory.EnumerateFiles(
                    Application.dataPath, "package.json", SearchOption.AllDirectories))
                {
                    if (File.ReadAllText(packageJson).ToLowerInvariant().Contains(PACKAGE_IDENTIFIER))
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }

    [InitializeOnLoad]
    public static class ScreenShareDependencyChecker
    {
        private const string SESSION_KEY = "ScreenShareTool_LiveKitPrompted";

        static ScreenShareDependencyChecker()
        {
            if (Application.isBatchMode)
                return;

            if (!ScreenShareLiveKitDetection.IsInstalled() && !SessionState.GetBool(SESSION_KEY, false))
            {
                SessionState.SetBool(SESSION_KEY, true);
                EditorApplication.delayCall += ScreenShareSetupWindow.Open;
            }
        }
    }

    public class ScreenShareBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            if (ScreenShareLiveKitDetection.IsInstalled())
                return;

            throw new BuildFailedException(
                "Screen Share Tool: the LiveKit Unity WebGL SDK is missing from this project, " +
                "so the build cannot compile.\n\n" +
                "Fix it one of two ways:\n" +
                "1. Window > Package Manager > + > Add package from git URL:\n" +
                "   https://github.com/livekit/client-sdk-unity-web.git#v2.0.0\n" +
                "   (requires Git installed on this machine)\n" +
                "2. Download the v2.0.0 source ZIP, rename the extracted folder to " +
                "'client-sdk-unity-web', and place it in this project's Packages folder " +
                "(the one beside Assets, not inside it).\n\n" +
                "Tools > Screen Share > Install LiveKit SDK reopens the setup window with both options.");
        }
    }

    public class ScreenShareSetupWindow : EditorWindow
    {
        private const string LIVEKIT_GIT_URL =
            "https://github.com/livekit/client-sdk-unity-web.git#v2.0.0";
        private const string LIVEKIT_ZIP_URL =
            "https://github.com/livekit/client-sdk-unity-web/archive/refs/tags/v2.0.0.zip";

        private UnityEditor.PackageManager.Requests.AddRequest _addRequest;
        private Vector2 _scroll;
        private bool _showManualSteps;

        [MenuItem("Tools/Screen Share/Install LiveKit SDK")]
        public static void Open()
        {
            var window = GetWindow<ScreenShareSetupWindow>(true, "Screen Share Setup", true);
            window.minSize = new Vector2(480, 300);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Screen Share Tool — Required Dependency",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            if (ScreenShareLiveKitDetection.IsInstalled())
            {
                EditorGUILayout.HelpBox(
                    "The LiveKit Unity WebGL SDK is installed. You're ready to build.",
                    MessageType.Info);

                if (GUILayout.Button("Close"))
                    Close();

                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.HelpBox(
                "The Screen Share Tool requires the LiveKit Unity WebGL SDK, " +
                "which is not yet installed in this project. Builds will fail until it is.",
                MessageType.Warning);
            EditorGUILayout.Space(6);

            bool installing = _addRequest != null && !_addRequest.IsCompleted;
            bool failed = _addRequest != null && _addRequest.IsCompleted &&
                          _addRequest.Status != StatusCode.Success;

            if (installing)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Installing LiveKit SDK…");
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.LabelField("Option 1 — automatic (requires Git on this machine)",
                    EditorStyles.boldLabel);

                if (GUILayout.Button(failed ? "Retry Install" : "Install LiveKit SDK"))
                    StartInstall();
            }

            if (failed)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Install failed. Unity's message is below — copy it if you need it. " +
                    "This usually means Git is missing or misconfigured on this machine. " +
                    "Use Option 2 instead; it needs no Git.",
                    MessageType.Error);

                EditorGUILayout.SelectableLabel(
                    _addRequest.Error?.message ?? "unknown error",
                    EditorStyles.textArea,
                    GUILayout.MinHeight(50));

                _showManualSteps = true;
            }

            EditorGUILayout.Space(10);
            _showManualSteps = EditorGUILayout.Foldout(_showManualSteps,
                "Option 2 — manual install (no Git needed)", true);

            if (_showManualSteps)
            {
                EditorGUILayout.HelpBox(
                    "1. Download the v2.0.0 ZIP using the button below.\n" +
                    "2. Extract it. Find the folder that has package.json directly inside it.\n" +
                    "3. Rename that folder to: client-sdk-unity-web\n" +
                    "4. Move it into this project's Packages folder (beside Assets, not inside it).\n" +
                    "5. Click back into Unity, then press Re-check below.",
                    MessageType.None);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Download v2.0.0 ZIP"))
                    Application.OpenURL(LIVEKIT_ZIP_URL);

                if (GUILayout.Button("Open Packages Folder"))
                    EditorUtility.RevealInFinder(
                        Path.Combine(ScreenShareLiveKitDetection.ProjectRoot, "Packages") +
                        Path.DirectorySeparatorChar);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Re-check"))
                Repaint();

            EditorGUILayout.EndScrollView();
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
