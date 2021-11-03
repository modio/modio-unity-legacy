#if UNITY_EDITOR

using UnityEditor;

namespace ModIO.UI.EditorCode
{
    [CustomEditor(typeof(Touchable))]
    public class TouchableEditor : Editor
    {
        public override void OnInspectorGUI() {}
    }
}

#endif
