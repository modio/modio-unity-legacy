#if UNITY_EDITOR
using UnityEditor;

namespace ModIO
{
    public interface ISceneEditorView
    {
        string GetViewHeader();

        void OnEnable();
        void OnDisable();
        void OnGUI(EditorSceneData sceneData);
    }
}

#endif
