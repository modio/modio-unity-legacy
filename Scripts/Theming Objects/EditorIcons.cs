using UnityEngine;

namespace ModIO
{
    [CreateAssetMenu(fileName = "New Editor Icon Set", menuName = "ModIO/Theming/Editor Icon Set")]
    public class EditorIcons : ScriptableObject
    {
        // ------[ SINGLETON ]------
        static EditorIcons _instance = null;
        public static EditorIcons Instance
        {
            get
            {
                if (!_instance)
                {
                    if(Resources.LoadAll<EditorIcons>("").Length > 0)
                    {
                        _instance = Resources.FindObjectsOfTypeAll<EditorIcons>()[0];
                    }
                    else
                    {
                        Debug.LogWarning("[mod.io] Unable to locate the mod.io EditorIcons. Creating run-time instance.");
                        _instance = ScriptableObject.CreateInstance<EditorIcons>();
                    }
                }
                return _instance;
            }
        }

        // ------[ FIELDS ]------
        public Texture2D undoButton;
        public static Texture2D UndoButton { get { return Instance.undoButton; } }

        public Texture2D clearButton;
        public static Texture2D ClearButton { get { return Instance.clearButton; } }
    }
}
