using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace FlyHigh.PhoneController.Editor
{
    public static class PrototypeBuild
    {
        [InitializeOnLoadMethod]
        private static void EnsureTextResources()
        {
            EditorApplication.delayCall += () =>
            {
                PrepareCarVisual();
                if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) Prepare();
            };
        }
        public static void Prepare()
        {
            PrepareCarVisual();
            TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
        }
        private static void PrepareCarVisual()
        {
            const string folder = "Assets/PhoneController/Resources";
            const string path = folder + "/FlyHighCarVisual.asset";
            if (AssetDatabase.LoadAssetAtPath<CarVisualSettings>(path) != null) return;
            var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Vehicles/Car_13.prefab");
            if (prefab == null) return;
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/PhoneController", "Resources");
            var settings = UnityEngine.ScriptableObject.CreateInstance<CarVisualSettings>();
            settings.prefab = prefab;
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Fly High/Build PC prototype")]
        public static void BuildPC() => Build(BuildTarget.StandaloneWindows64, "Builds/PC/FlyHigh.exe");
        [MenuItem("Fly High/Build PC city drive")]
        public static void BuildCity() => Build(BuildTarget.StandaloneWindows64, "Builds/City/FlyHighCity.exe", "Assets/Scenes/CityDrive.unity");
        [MenuItem("Fly High/Build Android controller")]
        public static void BuildAndroid() => Build(BuildTarget.Android, "Builds/Android/FlyHighController.apk");
        private static void Build(BuildTarget target, string path, string scene = "Assets/Scenes/GameScene.unity")
        {
            Prepare();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var oldOrientation = PlayerSettings.defaultInterfaceOrientation;
            var oldInternet = PlayerSettings.Android.forceInternetPermission;
            var oldId = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
            try
            {
                if (target == BuildTarget.Android)
                {
                    PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
                    PlayerSettings.Android.forceInternetPermission = true;
                    PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.flyhigh.phonecontroller");
                }
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { scene }, locationPathName = path,
                    target = target, options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new System.Exception("Prototype build failed: " + report.summary.result);
            }
            finally
            {
                PlayerSettings.defaultInterfaceOrientation = oldOrientation;
                PlayerSettings.Android.forceInternetPermission = oldInternet;
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, oldId);
            }
        }
    }
}
