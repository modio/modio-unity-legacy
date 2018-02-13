#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace ModIO
{
    public class EditorSceneData : MonoBehaviour
    {
        public EditableModInfo modInfo = new EditableModInfo();
    }
}
#endif
