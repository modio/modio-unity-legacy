using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>Component responsible for managing the navigation of the UI.</summary>
    public class NavigationManager : MonoBehaviour
    {
        // ---------[ Singleton ]---------
        /// <summary>Singleton instance.</summary>
        private static NavigationManager _instance = null;

        /// <summary>Singleton instance.</summary>
        public static NavigationManager instance
        {
            get
            {
                if(NavigationManager._instance == null)
                {
                    NavigationManager._instance = UIUtilities.FindComponentInAllScenes<NavigationManager>(true);

                    if(NavigationManager._instance == null)
                    {
                        GameObject go = new GameObject("Navigation Manager");
                        NavigationManager._instance = go.AddComponent<NavigationManager>();
                    }
                }

                return NavigationManager._instance;
            }
        }

        // ---------[ Fields ]---------
        /// <summary>The menu bar that is always shown.</summary>
        public CanvasGroup menuBar = null;

        // ---------[ Initialization ]---------
        /// <summary>Sets singleton instance.</summary>
        private void Awake()
        {
            if(NavigationManager._instance == null)
            {
                NavigationManager._instance = this;
            }
            #if DEBUG
            else if(NavigationManager._instance != this)
            {
                Debug.LogWarning("[mod.io] Second instance of a NavigationManager"
                                 + " component enabled simultaneously."
                                 + " Only one instance of a NavigationManager"
                                 + " component should be active at a time.");
                this.enabled = false;
            }
            #endif
        }

        /// <summary>Links with View Manager.</summary>
        private void Start()
        {
            ViewManager.instance.onBeforeDefocusView.AddListener(this.OnDefocusView);
            ViewManager.instance.onAfterFocusView.AddListener(this.OnFocusView);
        }

        // ---------[ Event Handlers ]---------
        /// <summary>Stores the selection and makes the view uninteractable.</summary>
        public void OnDefocusView(IBrowserView view)
        {
            view.canvasGroup.interactable = false;
        }

        /// <summary>Set the selection for a change in focus.</summary>
        public void OnFocusView(IBrowserView view)
        {
            view.canvasGroup.interactable = true;
            EventSystem.current.SetSelectedGameObject(view.primarySelection);

            this.menuBar.interactable = (view is ExplorerView || view is SubscriptionsView);
        }
    }
}
