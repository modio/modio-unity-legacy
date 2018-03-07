#if UNITY_EDITOR

namespace ModIO
{
    public interface ISceneEditorHeader
    {
        void OnEnable();
        void OnDisable();
        void OnGUI();

        bool IsInteractionDisabled();
    }
}

#endif
