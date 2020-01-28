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
        public IBrowserView currentFocus { get { return this.m_focusedView; } }

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

            IBrowserView initFocus = null;

            if(this.explorerView != null
               && this.explorerView.isActiveAndEnabled)
            {
                this.m_currentMainView = this.explorerView;
                initFocus = this.explorerView;
            }

            if(this.subscriptionsView != null
               && this.subscriptionsView.isActiveAndEnabled)
            {
                if(initFocus == this.explorerView as IBrowserView)
                {
                    this.explorerView.gameObject.SetActive(false);
                }

                this.m_currentMainView = this.subscriptionsView;
                initFocus = this.subscriptionsView;
            }

            if(this.m_currentMainView == null)
            {
                if(this.explorerView != null)
                {
                    this.m_currentMainView = this.explorerView;
                    initFocus = this.explorerView;

                    this.explorerView.gameObject.SetActive(true);
                }
                else if(this.subscriptionsView != null)
                {
                    this.subscriptionsView.gameObject.SetActive(true);
                    initFocus = this.subscriptionsView;

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
                initFocus = this.inspectorView;
            }

            if(this.loginDialog != null
               && this.loginDialog.isActiveAndEnabled)
            {
                if(initFocus == this.inspectorView as IBrowserView)
                {
                    initFocus.gameObject.SetActive(false);
                }

                initFocus = this.loginDialog;
            }

            this.StartCoroutine(DelayedViewFocusOnStart(initFocus));
        }

        /// <summary>Sends events at the end of the frame.</summary>
        private System.Collections.IEnumerator DelayedViewFocusOnStart(IBrowserView view)
        {
            yield return null;

            if(this != null && view != null)
            {
                this.m_focusedView = view;
                this.onAfterFocusView.Invoke(view);
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

            if(this.m_focusedView == (IBrowserView)this.m_inspectorView) { return; }

            this.HideAndDefocusView(this.m_loginDialog);
            this.ShowAndFocusView(this.m_inspectorView);
        }

        public void ActivateExplorerView()
        {
            #if DEBUG
                if(this.m_explorerView == null)
                {
                    Debug.Log("[mod.io] Explorer View not found.");
                }
            #endif

            this.HideAndDefocusView(this.m_subscriptionsView);
            this.ShowAndFocusView(this.m_explorerView);
        }

        public void ActivateSubscriptionsView()
        {
            #if DEBUG
                if(this.m_subscriptionsView == null)
                {
                    Debug.Log("[mod.io] Subscriptions View not found.");
                }
            #endif


            this.HideAndDefocusView(this.m_explorerView);
            this.ShowAndFocusView(this.m_subscriptionsView);
        }

        public void ShowLoginDialog()
        {
            #if DEBUG
                if(this.m_loginDialog == null)
                {
                    Debug.Log("[mod.io] Login Dialog not found.");
                }
            #endif

            if(this.m_loginDialog == null
               || this.m_focusedView == (IBrowserView)this.m_loginDialog)
            {
                return;
            }

            this.HideAndDefocusView(this.m_inspectorView);
            this.ShowAndFocusView(this.m_loginDialog);
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
    }
}
