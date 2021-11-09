using UnityEngine;

#if UNITY_EDITOR
using Directory = System.IO.Directory;

using UnityEditor;
#endif


namespace ModIO.EditorCode
{
    [CreateAssetMenu(fileName = "New Mod Profile", menuName = "ModIO/Create Mod Profile")]
    public class ScriptableModProfile : ScriptableObject
    {
// ---------[ MENU ITEM ]---------
#if UNITY_EDITOR
        [MenuItem("Tools/mod.io/Create Mod Profile")]
        public static void CreateAssetInstance()
        {
            ScriptableModProfile asset = ScriptableObject.CreateInstance<ScriptableModProfile>();

            int profileCount = Directory
                                   .GetFiles(Application.dataPath, "NewModProfile*.asset",
                                             System.IO.SearchOption.TopDirectoryOnly)
                                   .Length;

            string fileNameAddition =
                (profileCount > 0 ? " (" + profileCount.ToString() + ")" : "");

            AssetDatabase.CreateAsset(asset, "Assets/NewModProfile" + fileNameAddition + ".asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
        }
#endif

        // ---------[ CONSTANTS ]---------
        public const int UNINITIALIZED_MOD_ID = -1;

        // ---------[ FIELDS ]---------
        public int modId = ScriptableModProfile.UNINITIALIZED_MOD_ID;
        public EditableModProfile editableModProfile = null;
    }
}
