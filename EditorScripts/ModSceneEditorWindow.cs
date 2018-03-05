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

        // ------[ ABSTRACT FUNCTIONS ]------
        protected abstract ISceneEditorHeader GetEditorHeader();
        protected abstract UninitializedSceneView GetUninitializedSceneView();
        protected abstract ISceneEditorView[] GetTabbedViews();

        // ------[ INITIALIZATION ]------
        protected virtual void OnEnable()
        {
            ModManager.Initialize();

            wasPlaying = Application.isPlaying;

            // - Initialize Scene Variables -
            currentScene = SceneManager.GetActiveScene();
            sceneData = Object.FindObjectOfType<EditorSceneData>();
            activeTabbedViewIndex = (sceneData == null ? -1 : 0);
            scrollPos = Vector2.zero;

            // - Call Enables on Views -
            GetEditorHeader().OnEnable();
            
            if(sceneData == null)
            {
                GetUninitializedSceneView().OnEnable();
            }
            else
            {
                GetTabbedViews()[activeTabbedViewIndex].OnEnable();
            }
        }

        protected virtual void OnDisable()
        {
            GetEditorHeader().OnDisable();

            if(sceneData == null)
            {
                GetUninitializedSceneView().OnDisable();
            }
            else
            {
                GetTabbedViews()[activeTabbedViewIndex].OnDisable();
            }
        }

        protected virtual void OnSceneChange()
        {
            if(sceneData == null)
            {
                GetUninitializedSceneView().OnDisable();
            }
            else
            {
                GetTabbedViews()[activeTabbedViewIndex].OnDisable();
            }

            // - Initialize Scene Variables -
            currentScene = SceneManager.GetActiveScene();
            sceneData = Object.FindObjectOfType<EditorSceneData>();
            activeTabbedViewIndex = (sceneData == null ? -1 : 0);
            scrollPos = Vector2.zero;

            if(sceneData == null)
            {
                GetUninitializedSceneView().OnEnable();
            }
            else
            {
                GetTabbedViews()[activeTabbedViewIndex].OnEnable();
            }
        }

        // ---------[ GUI DISPLAY ]---------
        protected virtual void OnGUI()
        {
            bool isPlaying = Application.isPlaying;
            int prevViewIndex = activeTabbedViewIndex;
            ISceneEditorView[] tabbedViews = this.GetTabbedViews();
            
            // - Update Data -
            if(currentScene != SceneManager.GetActiveScene()
               || (isPlaying != wasPlaying))
            {
                OnSceneChange();
            }

            if(sceneData == null)
            {
                sceneData = Object.FindObjectOfType<EditorSceneData>();
                if(sceneData != null)
                {
                    activeTabbedViewIndex = 0;
                }
            }

            // ---[ Header ]---
            GetEditorHeader().OnGUI();

            EditorGUILayout.Space();

            // ---[ Tabs ]---
            using (new EditorGUI.DisabledScope(sceneData == null))
            {
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
            }

            // ---[ Main Panel ]---
            if(prevViewIndex != activeTabbedViewIndex)
            {
                scrollPos = Vector2.zero;

                if(prevViewIndex == -1)
                {
                    this.GetUninitializedSceneView().OnDisable();
                }
                else
                {
                    tabbedViews[prevViewIndex].OnDisable();
                }

                if(activeTabbedViewIndex == -1)
                {
                    this.GetUninitializedSceneView().OnEnable();
                }
                else
                {
                    tabbedViews[activeTabbedViewIndex].OnEnable();
                }
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if(activeTabbedViewIndex == -1)
                {
                    this.GetUninitializedSceneView().OnGUI();
                }
                else
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