#if UNITY_EDITOR
using UnityEditor;

namespace ModIO
{
    public interface IModProfileViewPart
    {
        bool isDisabled { get; }

        void OnEnable(SerializedObject serializedModEdits, ModProfile profile);
        void OnDisable();
        void OnGUI();
        void OnUpdate();
    }
}

#endif
