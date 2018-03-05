#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModIO
{
    // TODO(@jackson): Needs beauty-pass
    // TODO(@jackson): Force repaint on Callbacks
    // TODO(@jackson): Implement client-side error-checking in submission
    // TODO(@jackson): Check if undos are necessary
    // TODO(@jackson): Check for scene change between callbacks
    public abstract class ModSceneEditorWindow : EditorWindow
    {
        // ------[ WINDOW FIELDS ]---------
        private Scene currentScene;
        private EditorSceneData sceneData;
        private bool wasPlaying;
        private int activeTabbedViewIndex;
        private Vector2 scrollPos;

        private void OnEnable()
        {
            ModManager.Initialize();

            currentScene = new Scene();
            wasPlaying = Application.isPlaying;
            sceneData = null;
            activeTabbedViewIndex = 0;

            GetEditorHeader().OnEnable();
        }

        private void OnDisable()
        {
            GetEditorHeader().OnDisable();
        }

        protected virtual void OnSceneChange()
        {
            // - Initialize Scene Variables -
            currentScene = SceneManager.GetActiveScene();
            sceneData = Object.FindObjectOfType<EditorSceneData>();
            
            activeTabbedViewIndex = 0;
            scrollPos = Vector2.zero;

            if(sceneData == null)
            {
                GetUninitializedSceneView().OnEnable();
            }
        }

        protected abstract ISceneEditorHeader GetEditorHeader();
        protected abstract UninitializedSceneView GetUninitializedSceneView();
        protected abstract ISceneEditorView[] GetTabbedViews();

        // ---------[ GUI DISPLAY ]---------
        protected virtual void OnGUI()
        {
            bool isPlaying = Application.isPlaying;

            // - Update Data -
            if(currentScene != SceneManager.GetActiveScene()
               || (isPlaying != wasPlaying))
            {
                OnSceneChange();
            }

            // ---[ Display ]---
            GetEditorHeader().OnGUI();

            EditorGUILayout.Space();

            // ---[ Main Panel ]---
            if(sceneData == null)
            {
                sceneData = Object.FindObjectOfType<EditorSceneData>();
            }

            if(sceneData == null)
            {
                this.GetUninitializedSceneView().OnGUI();
            }
            else
            {
                int prevViewIndex = activeTabbedViewIndex;
                ISceneEditorView[] tabbedViews = this.GetTabbedViews();

                EditorGUILayout.BeginHorizontal();
                    for(int i = 0;
                        i < tabbedViews.Length;
                        ++i)
                    {
                        if(GUILayout.Button(tabbedViews[i].GetViewHeader()))
                        {
                            activeTabbedViewIndex = i;
                        }
                    }
                EditorGUILayout.EndHorizontal();

                if(prevViewIndex != activeTabbedViewIndex)
                {
                    scrollPos = Vector2.zero;

                    tabbedViews[prevViewIndex].OnDisable();
                    tabbedViews[activeTabbedViewIndex].OnEnable();
                }

                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                    tabbedViews[activeTabbedViewIndex].OnGUI(sceneData);
                
                    EditorGUILayout.EndScrollView();
                }
            }

            wasPlaying = isPlaying;
        }
    }
}

#endif