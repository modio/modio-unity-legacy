#if UNITY_EDITOR

using UnityEditor;

namespace ModIO.UI.Editor
{
    [CustomEditor(typeof(Touchable))]
    public class TouchableEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI(){}
    }
}

#endif
