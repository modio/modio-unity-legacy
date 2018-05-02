using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ModIO
{
    [CreateAssetMenu(fileName = "New Mod Profile", menuName = "ModIO/Create Mod Profile")]
    public class ScriptableModProfile : ScriptableObject
    {
        public const int UNINITIALIZED_MOD_ID = -1;

        #if UNITY_EDITOR
        [MenuItem("mod.io/Create Mod Profile")]
        public static void CreateAssetInstance()
        {
            ScriptableModProfile asset = ScriptableObject.CreateInstance<ScriptableModProfile>();

            AssetDatabase.CreateAsset(asset, "Assets/NewModProfile.asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
        }
        #endif

        public int modId = ScriptableModProfile.UNINITIALIZED_MOD_ID;
        public EditableModProfile editableModProfile = null;
    }
}
