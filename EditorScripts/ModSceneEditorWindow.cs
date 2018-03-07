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
        private Vector2 scrollPos;

        protected ISceneEditorView activeView;
        
        private bool isRepaintRequired;
        private bool wasActiveViewDisabled;
        private bool wasHeaderDisabled;

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
            scrollPos = Vector2.zero;

            if(sceneData == null)
            {
                activeView = GetUninitializedSceneView();
            }
            else
            {
                activeView = GetTabbedViews()[0];
            }
            isRepaintRequired = false;
            wasActiveViewDisabled = false;
            wasHeaderDisabled = false;

            // - Call Enables on Views -
            GetEditorHeader().OnEnable();
            activeView.OnEnable();
        }

        protected virtual void OnDisable()
        {
            GetEditorHeader().OnDisable();
            activeView.OnDisable();
        }

        protected virtual void OnSceneChange()
        {
            // - Initialize Scene Variables -
            currentScene = SceneManager.GetActiveScene();
            sceneData = Object.FindObjectOfType<EditorSceneData>();
            scrollPos = Vector2.zero;

            if(sceneData == null)
            {
                SetActiveView(GetUninitializedSceneView());
            }
            else
            {
                SetActiveView(GetTabbedViews()[0]);
            }
        }

        protected void SetActiveView(ISceneEditorView newActiveView)
        {
            activeView.OnDisable();

            activeView = newActiveView;
            activeView.OnEnable();

            scrollPos = Vector2.zero;
            wasActiveViewDisabled = false;
            isRepaintRequired = true;
        }

        // ---------[ UPDATES ]---------
        protected virtual void OnInspectorUpdate()
        {
            if(isRepaintRequired
               || wasActiveViewDisabled != activeView.IsViewDisabled()
               || wasHeaderDisabled != GetEditorHeader().IsInteractionDisabled())
            {
                Repaint();
                isRepaintRequired = false;
            }
            wasActiveViewDisabled = activeView.IsViewDisabled();
            wasHeaderDisabled = GetEditorHeader().IsInteractionDisabled();
        }

        protected virtual void OnGUI()
        {
            bool isPlaying = Application.isPlaying;
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
                   SetActiveView(tabbedViews[0]);
                }
            }

            // ---[ Header ]---
            GetEditorHeader().OnGUI();

            EditorGUILayout.Space();

            // ---[ Tabs ]---
            int prevViewIndex = -1;
            int newViewIndex = -1;
            using (new EditorGUI.DisabledScope(sceneData == null))
            {
                EditorGUILayout.BeginHorizontal();
                    for(int i = 0;
                        i < tabbedViews.Length;
                        ++i)
                    {
                        ISceneEditorView tabView = tabbedViews[i];
                        if(tabView == activeView)
                        {
                            prevViewIndex = i;
                        }
                        if(GUILayout.Button(tabbedViews[i].GetViewHeader()))
                        {
                            newViewIndex = i;
                        }
                    }
                EditorGUILayout.EndHorizontal();
            }

            if(newViewIndex >= 0
               && prevViewIndex != newViewIndex)
            {
                SetActiveView(tabbedViews[newViewIndex]);
            }

            // ---[ Main Panel ]---
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                    activeView.OnGUI(sceneData);
                EditorGUILayout.EndScrollView();
            }

            wasPlaying = isPlaying;
        }
    }
}

#endif