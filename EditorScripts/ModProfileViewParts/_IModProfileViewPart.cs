#if UNITY_EDITOR
using UnityEditor;

namespace ModIO
{
    public interface IModProfileViewPart
    {
        bool isDisabled { get; }

        void OnEnable(SerializedProperty serializedEditableModProfile, ModProfile profile);
        void OnDisable();
        void OnGUI();
        void OnUpdate();
    }
}

#endif
