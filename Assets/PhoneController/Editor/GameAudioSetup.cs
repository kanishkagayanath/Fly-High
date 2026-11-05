using UnityEditor;
using UnityEngine;

namespace FlyHigh.PhoneController.Editor
{
    public static class GameAudioSetup
    {
        public const string Path="Assets/PhoneController/Resources/FlyHighAudio.asset";
        [InitializeOnLoadMethod]
        private static void Ready(){EditorApplication.delayCall+=Ensure;}
        private static void Ensure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||AssetDatabase.LoadAssetAtPath<GameAudioSettings>(Path)!=null)return;
            var settings=ScriptableObject.CreateInstance<GameAudioSettings>();ReadBounds(settings);
            AssetDatabase.CreateAsset(settings,Path);AssetDatabase.SaveAssets();
        }
        [MenuItem("Fly High/Audio/Open audio settings")]
        public static void Open()
        {
            Ensure();var settings=AssetDatabase.LoadAssetAtPath<GameAudioSettings>(Path);
            Selection.activeObject=settings;EditorGUIUtility.PingObject(settings);
        }
        [MenuItem("Fly High/Audio/Read island bounds")]
        public static void RefreshBounds()
        {
            if(EditorApplication.isPlaying)return;Ensure();var settings=AssetDatabase.LoadAssetAtPath<GameAudioSettings>(Path);if(settings==null)return;
            Undo.RecordObject(settings,"Update audio island bounds");
            if(ReadBounds(settings)){EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();}
            Selection.activeObject=settings;
        }
        private static bool ReadBounds(GameAudioSettings settings)
        {
            foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                foreach(var material in renderer.sharedMaterials)
                    if(material!=null&&material.HasProperty("_IslandRect"))
                    {
                        var rect=material.GetVector("_IslandRect");if(rect.z<=0||rect.w<=0)continue;
                        settings.islandCentre=new Vector2(rect.x,rect.y);settings.islandHalfSize=new Vector2(rect.z,rect.w);return true;
                    }
            return false;
        }
    }
    [CustomEditor(typeof(GameAudioSettings))]
    public sealed class GameAudioSettingsInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Drag your imported MP3 / WAV clips into the slots below. Empty slots are silent. This asset is used automatically by the PC and Android builds. Assign outside Play mode, then rebuild to include the clips on your phone.",MessageType.Info);
            DrawDefaultInspector();
        }
    }
}
