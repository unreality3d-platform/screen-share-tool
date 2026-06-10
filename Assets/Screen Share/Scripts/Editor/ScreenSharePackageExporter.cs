using System.IO;
using UnityEditor;
using UnityEngine;

namespace ScreenShareTool.Editor
{
    public static class ScreenSharePackageExporter
    {
        private const string EXPORT_FOLDER = "Assets/Screen Share";
        private const string OUTPUT_DIRECTORY = "Build";
        private const string PACKAGE_NAME = "ScreenShareTool.unitypackage";

        [MenuItem("Screen Share/Export .unitypackage")]
        public static void ExportPackage()
        {
            string outputPath = Export();
            EditorUtility.RevealInFinder(outputPath);
        }

        public static void ExportPackageCommandLine()
        {
            try
            {
                string outputPath = Export();
                Debug.Log("[ScreenSharePackageExporter] Export succeeded: " + outputPath);
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ScreenSharePackageExporter] Export failed: " + e);
                EditorApplication.Exit(1);
            }
        }

        private static string Export()
        {
            if (!AssetDatabase.IsValidFolder(EXPORT_FOLDER))
                throw new DirectoryNotFoundException("Export folder not found: " + EXPORT_FOLDER);

            Directory.CreateDirectory(OUTPUT_DIRECTORY);
            string outputPath = Path.Combine(OUTPUT_DIRECTORY, PACKAGE_NAME);

            AssetDatabase.ExportPackage(EXPORT_FOLDER, outputPath, ExportPackageOptions.Recurse);

            return outputPath;
        }
    }
}