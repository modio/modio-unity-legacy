#if UNITY_EDITOR
using UnityEditor;

namespace ModIO
{
    public interface IModProfileViewPart
    {
        void OnEnable(SerializedProperty serializedEditableModProfile, ModProfile profile);
        void OnDisable();
        void OnGUI();
        void OnUpdate();
    }
}

#endif
