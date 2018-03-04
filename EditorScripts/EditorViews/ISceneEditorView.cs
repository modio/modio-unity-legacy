#if UNITY_EDITOR

namespace ModIO
{
    public interface ISceneEditorView
    {
        string GetViewHeader();

        void OnEnable();
        void OnDisable();
        void OnGUI();
    }
}

#endif
