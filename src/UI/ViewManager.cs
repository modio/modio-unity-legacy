using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace ModIO.UI
{
    /// <summary>Component responsible for management of the various views.</summary>
    public class ViewManager : MonoBehaviour
    {
        // ---------[ Nested Data-Types ]---------
        /// <summary>Event for views changing.</summary>
        public class ViewChangeEvent : UnityEvent<IBrowserView> {}

        // ---------[ SINGLETON ]---------
        private static ViewManager _instance = null;
        public static ViewManager instance
        {
            get
            {
                if(ViewManager._instance == null)
                {
                    ViewManager._instance = UIUtilities.FindComponentInAllScenes<ViewManager>(true);

                    if(ViewManager._instance == null)
                    {
                        GameObject go = new GameObject("View Manager");
                        ViewManager._instance = go.AddComponent<ViewManager>();
                    }

                    ViewManager._instance.FindViews();
                }

                return ViewManager._instance;
            }
        }

        // ---------[ FIELDS ]---------
        private ExplorerView m_explorerView = null;
        private SubscriptionsView m_subscriptionsView = null;
        private InspectorView m_inspectorView = null;
        private LoginDialog m_loginDialog = null;
        private bool m_viewsFound = false;

        /// <summary>Main view currently open.</summary>
        private IBrowserView m_currentMainView = null;

        /// <summary>The currently focused view.</summary>
        private IBrowserView m_focusedView = null;

        /// <summary>View stack for all the currently open views.</summary>
        private List<IBrowserView> m_viewStack = new List<IBrowserView>();

        /// <summary>Event callback for when a view is hidden.</summary>
        public ViewChangeEvent onBeforeHideView = new ViewChangeEvent();

        /// <summary>Event callback for when a view is shown.</summary>
        public ViewChangeEvent onBeforeShowView = new ViewChangeEvent();

        /// <summary>Event callback for when a view is defocused.</summary>
        public ViewChangeEvent onBeforeDefocusView = new ViewChangeEvent();

        /// <summary>Event callback for when a view is focused.</summary>
        public ViewChangeEvent onAfterFocusView = new ViewChangeEvent();

        // --- Accessors ---
        /// <summary>Explorer View in the UI.</summary>
        public ExplorerView explorerView
        {
            get { return this.m_explorerView; }
        }
        /// <summary>Subscriptions View in the UI.</summary>
        public SubscriptionsView subscriptionsView
        {
            get { return this.m_subscriptionsView; }
        }
        /// <summary>Inspector View in the UI.</summary>
        public InspectorView inspectorView
        {
            get { return this.m_inspectorView; }
        }
        /// <summary>Login View in the UI</summary>
        public LoginDialog loginDialog
        {
            get { return this.m_loginDialog; }
        }

        /// <summary>Currently focused view.</summary>
        public IBrowserView currentFocus
        {
            get { return this.m_viewStack[this.m_viewStack.Count-1]; }
        }

        // ---------[ INITIALIZATION ]---------
        /// <summary>Sets singleton instance.</summary>
        private void Awake()
        {
            if(ViewManager._instance == null)
            {
                ViewManager._instance = this;
            }
            #if DEBUG
            else if(ViewManager._instance != this)
            {
                Debug.LogWarning("[mod.io] Second instance of a ViewManager"
                                 + " component enabled simultaneously."
                                 + " Only one instance of a ViewManager"
                                 + " component should be active at a time.");
                this.enabled = false;
            }
            #endif
        }

        /// <summary>Gathers the views in the scene.</summary>
        private void Start()
        {
            this.FindViews();

            this.m_focusedView = null;

            List<IBrowserView> initViewStack = new List<IBrowserView>();

            if(this.explorerView != null
               && this.explorerView.isActiveAndEnabled)
            {
                initViewStack.Add(this.explorerView);

                this.m_currentMainView = this.explorerView;
            }

            if(this.subscriptionsView != null
               && this.subscriptionsView.isActiveAndEnabled)
            {
                if(initViewStack.Count == 1)
                {
                    initViewStack[0] = this.subscriptionsView;
                    this.explorerView.gameObject.SetActive(false);
                }
                else
                {
                    initViewStack.Add(this.subscriptionsView);
                }

                this.m_currentMainView = this.subscriptionsView;
            }

            if(initViewStack.Count == 0)
            {
                if(this.explorerView != null)
                {
                    initViewStack.Add(this.explorerView);

                    this.m_currentMainView = this.explorerView;

                    this.explorerView.gameObject.SetActive(true);
                }
                else if(this.subscriptionsView != null)
                {
                    initViewStack.Add(this.subscriptionsView);

                    this.subscriptionsView.gameObject.SetActive(true);

                    this.m_currentMainView = this.subscriptionsView;
                }
                #if DEBUG
                    else
                    {
                        Debug.Log("[mod.io] No main view found in the scene."
                                  + " Please consider adding either an ExplorerView or"
                                  + " a SubscriptionsView to the scene.", this);
                    }
                #endif
            }

            if(this.inspectorView != null
               && this.inspectorView.isActiveAndEnabled)
            {
                initViewStack.Add(this.inspectorView);
            }

            if(this.loginDialog != null
               && this.loginDialog.isActiveAndEnabled)
            {
                initViewStack.Add(this.loginDialog);
            }

            this.StartCoroutine(DelayedViewFocusOnStart(initViewStack));
        }

        /// <summary>Sends events at the end of the frame.</summary>
        private System.Collections.IEnumerator DelayedViewFocusOnStart(List<IBrowserView> viewStack)
        {
            yield return null;

            if(this != null && viewStack != null && viewStack.Count > 0)
            {
                this.m_viewStack = viewStack;

                for(int i = 0; i < viewStack.Count-1; ++i)
                {
                    this.onBeforeDefocusView.Invoke(viewStack[i]);
                }

                this.m_currentMainView = viewStack[0];
                this.m_focusedView = viewStack[viewStack.Count-1];
                this.onAfterFocusView.Invoke(viewStack[viewStack.Count-1]);
            }
        }

        private void FindViews()
        {
            if(this.m_viewsFound) { return; }

            this.m_explorerView = GetComponentInChildren<ExplorerView>(true);
            this.m_subscriptionsView = GetComponentInChildren<SubscriptionsView>(true);
            this.m_inspectorView = GetComponentInChildren<InspectorView>(true);
            this.m_loginDialog = GetComponentInChildren<LoginDialog>(true);
            this.m_viewsFound = true;
        }

        // ---------[ VIEW MANAGEMENT ]---------
        public void InspectMod(int modId)
        {
            #if DEBUG
                if(this.m_inspectorView == null)
                {
                    Debug.Log("[mod.io] Inspector View not found.");
                }
            #endif

            if(this.m_inspectorView == null)
            {
                return;
            }

            this.m_inspectorView.modId = modId;

            this.FocusStackedView(this.m_inspectorView);
        }

        public void ActivateExplorerView()
        {
            #if DEBUG
                if(this.m_explorerView == null)
                {
                    Debug.Log("[mod.io] Explorer View not found.");
                }
            #endif

            if(this.m_explorerView == null) { return; }

            this.HideAndDefocusView(this.m_subscriptionsView);
            this.ShowAndFocusView(this.m_explorerView);

            this.m_currentMainView = this.m_explorerView;
        }

        public void ActivateSubscriptionsView()
        {
            #if DEBUG
                if(this.m_subscriptionsView == null)
                {
                    Debug.Log("[mod.io] Subscriptions View not found.");
                }
            #endif

            if(this.m_subscriptionsView == null) { return; }

            this.HideAndDefocusView(this.m_explorerView);
            this.ShowAndFocusView(this.m_subscriptionsView);

            this.m_currentMainView = this.m_subscriptionsView;
        }

        public void ShowLoginDialog()
        {
            #if DEBUG
                if(this.m_loginDialog == null)
                {
                    Debug.Log("[mod.io] Login Dialog not found.");
                }
            #endif

            this.FocusStackedView(this.m_loginDialog);
        }

        /// <summary>Hides a given view and refocusses the current main view.</summary>
        public void HideViewAndFocusMain(IBrowserView view)
        {
            if(view == null || view == this.m_currentMainView) { return; }

            if(this.m_currentMainView == null)
            {
                Debug.LogError("[mod.io] Cannot focus main view as it is currently unassigned.",
                               this);

                return;
            }

            this.HideAndDefocusView(view);

            // Focus main view
            this.m_focusedView = this.m_currentMainView;
            this.onAfterFocusView.Invoke(this.m_currentMainView);
        }

        /// <summary>Shows a given view and sets it as the focus.</summary>
        public void ShowAndFocusView(IBrowserView view)
        {
            if(view == null || view == this.m_focusedView) { return; }

            if(this.m_focusedView != null)
            {
                this.onBeforeDefocusView.Invoke(this.m_focusedView);
            }

            if(!view.gameObject.activeSelf)
            {
                this.onBeforeShowView.Invoke(view);
                view.gameObject.SetActive(true);
            }

            this.m_focusedView = view;
            this.onAfterFocusView.Invoke(view);
        }

        /// <summary>Executes the functionality for hiding a view.</summary>
        private void HideAndDefocusView(IBrowserView view)
        {
            if(view == null) { return; }

            if(this.m_focusedView == view)
            {
                this.onBeforeDefocusView.Invoke(view);
                this.m_focusedView = null;
            }

            if(view.gameObject.activeSelf)
            {
                this.onBeforeHideView.Invoke(view);
                view.gameObject.SetActive(false);
            }
        }

        /// <summary>Either adds the view to the stack, or removes any views above it on the stack.</summary>
        private void FocusStackedView(IBrowserView view)
        {
            Debug.Assert(this.m_viewStack.Count > 0,
                         "[mod.io] Can only focus a stacked view if there is an existing view on the stack.");

            if(view == null || view == this.currentFocus) { return; }

            if(this.m_viewStack.Contains(view))
            {
                while(this.currentFocus != view)
                {
                    this.onBeforeDefocusView.Invoke(this.currentFocus);

                    this.onBeforeHideView.Invoke(this.currentFocus);
                    this.currentFocus.gameObject.SetActive(false);

                    this.m_viewStack.RemoveAt(this.m_viewStack.Count-1);
                }

                this.onAfterFocusView.Invoke(view);
            }
            else
            {
                this.onBeforeDefocusView.Invoke(this.currentFocus);

                this.onBeforeShowView.Invoke(view);
                view.gameObject.SetActive(true);

                this.m_viewStack.Add(view);
                this.onAfterFocusView.Invoke(view);
            }
        }
    }
}
