using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScreenShareTool.Editor
{
    public static class ScreenShareEditorTools
    {
        private const string MATERIALS_FOLDER = "Assets/Screen Share/Materials";
        private const string SURFACE_MATERIAL_PATH = "Assets/Screen Share/Materials/ScreenShareSurfaceMaterial.mat";
        private const string SETTINGS_ASSET_PATH = "Assets/Screen Share/ScreenShareSettings.asset";

        [MenuItem("Screen Share/Add Screen Share Setup")]
        public static void CreateScreenShareSetup()
        {
            // Placed at (0, 1.5, 0): centered, at eye height, at world origin.
            // Move the root on Z to position it in front of your camera.
            GameObject root = new GameObject("Screen Share Session");
            root.transform.position = new Vector3(0f, 1.5f, 0f);

            ScreenShareSettings settings = GetOrCreateSettings();

            ScreenShareRoom room = root.AddComponent<ScreenShareRoom>();
            var roomSO = new SerializedObject(room);
            roomSO.FindProperty("settings").objectReferenceValue = settings;
            // Unique per setup so two deployments can't accidentally share a room.
            roomSO.FindProperty("roomName").stringValue = GenerateRoomName();
            roomSO.ApplyModifiedProperties();

            ScreenShareControls controls = root.AddComponent<ScreenShareControls>();
            var controlsSO = new SerializedObject(controls);
            controlsSO.FindProperty("room").objectReferenceValue = room;
            controlsSO.ApplyModifiedProperties();

            GameObject screenObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenObj.name = "Screen";
            screenObj.transform.SetParent(root.transform, false);
            screenObj.transform.localScale = new Vector3(3.2f, 1.8f, 1f);

            MeshCollider meshCollider = screenObj.GetComponent<MeshCollider>();
            if (meshCollider != null)
                Object.DestroyImmediate(meshCollider);

            MeshRenderer screenRenderer = screenObj.GetComponent<MeshRenderer>();
            if (screenRenderer != null)
            {
                Material mat = GetOrCreateSurfaceMaterial();
                if (mat != null)
                    screenRenderer.sharedMaterial = mat;
            }

            ScreenShareSurface surface = screenObj.AddComponent<ScreenShareSurface>();
            var surfaceSO = new SerializedObject(surface);
            surfaceSO.FindProperty("room").objectReferenceValue = room;
            surfaceSO.FindProperty("targetRenderer").objectReferenceValue = screenRenderer;
            surfaceSO.ApplyModifiedProperties();

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            EditorUtility.SetDirty(root);
        }

        private static string GenerateRoomName()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(sceneName))
            {
                foreach (char c in sceneName)
                {
                    if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                        sb.Append(c);
                    if (sb.Length >= 32)
                        break;
                }
            }

            // Untitled scenes (empty name) fall back so the room is never just a bare suffix.
            string baseName = sb.Length > 0 ? sb.ToString() : "room";
            string suffix = System.Guid.NewGuid().ToString("N").Substring(0, 6);
            return baseName + "-" + suffix;
        }

        internal static Material GetOrCreateSurfaceMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(SURFACE_MATERIAL_PATH);
            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder(MATERIALS_FOLDER))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Screen Share"))
                    AssetDatabase.CreateFolder("Assets", "Screen Share");
                AssetDatabase.CreateFolder("Assets/Screen Share", "Materials");
            }

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null)
            {
                Debug.LogWarning("[ScreenShareEditorTools] Universal Render Pipeline/Unlit shader not found. The screen will keep the default Lit material. Assign an Unlit material manually to avoid lighting glare.");
                return null;
            }

            Material mat = new Material(unlitShader);
            mat.name = "ScreenShareSurfaceMaterial";
            AssetDatabase.CreateAsset(mat, SURFACE_MATERIAL_PATH);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static ScreenShareSettings GetOrCreateSettings()
        {
            ScreenShareSettings existing = AssetDatabase.LoadAssetAtPath<ScreenShareSettings>(SETTINGS_ASSET_PATH);
            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Screen Share"))
                AssetDatabase.CreateFolder("Assets", "Screen Share");

            ScreenShareSettings settings = ScriptableObject.CreateInstance<ScreenShareSettings>();
            AssetDatabase.CreateAsset(settings, SETTINGS_ASSET_PATH);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}