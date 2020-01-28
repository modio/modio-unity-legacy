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

        /// <summary>Event callback for when a view is hidden.</summary>
        public ViewChangeEvent onBeforeHideView = new ViewChangeEvent();

        /// <summary>Event callback for when a view is shown.</summary>
        public ViewChangeEvent onBeforeShowView = new ViewChangeEvent();

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

        // ---------[ INITIALIZATION ]---------
        private void Start()
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

            this.FindViews();
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

            if(this.m_inspectorView == null) { return; }

            this.m_inspectorView.modId = modId;
            this.ShowView(this.m_inspectorView);
        }

        public void ActivateExplorerView()
        {
            #if DEBUG
                if(this.m_explorerView == null)
                {
                    Debug.Log("[mod.io] Explorer View not found.");
                }
            #endif

            this.HideView(this.m_subscriptionsView);
            this.ShowView(this.m_explorerView);
        }

        public void ActivateSubscriptionsView()
        {
            #if DEBUG
                if(this.m_subscriptionsView == null)
                {
                    Debug.Log("[mod.io] Subscriptions View not found.");
                }
            #endif


            this.HideView(this.m_explorerView);
            this.ShowView(this.m_subscriptionsView);
        }

        public void ShowLoginDialog()
        {
            #if DEBUG
            if(this.m_loginDialog == null)
            {
                Debug.Log("[mod.io] Login Dialog not found.");
            }
            #endif
            else

            this.HideView(this.m_inspectorView);
            this.ShowView(this.m_loginDialog);
        }

        /// <summary>Hides a given view.</summary>
        public void HideView(IBrowserView view)
        {
            if(view == null) { return; }

            this.onBeforeHideView.Invoke(view);

            view.gameObject.SetActive(false);
        }

        /// <summary>Shows a given view.</summary>
        public void ShowView(IBrowserView view)
        {
            if(view == null) { return; }

            this.onBeforeShowView.Invoke(view);

            view.gameObject.SetActive(true);
        }
    }
}
