using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Component responsible for management of the various views.</summary>
    public class ViewManager : MonoBehaviour
    {
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
            if(this.m_inspectorView == null)
            {
                Debug.Log("[mod.io] Inspector View not found.");
            }
            else
            {
                this.m_inspectorView.modId = modId;
                this.m_inspectorView.gameObject.SetActive(true);
            }
        }

        public void ActivateExplorerView()
        {
            if(this.m_explorerView == null)
            {
                Debug.Log("[mod.io] Explorer View not found.");
            }
            else
            {
                this.m_explorerView.gameObject.SetActive(true);

                if(this.m_subscriptionsView.gameObject != null)
                {
                    this.m_subscriptionsView.gameObject.SetActive(false);
                }
            }
        }

        public void ActivateSubscriptionsView()
        {
            if(this.m_subscriptionsView == null)
            {
                Debug.Log("[mod.io] Subscriptions View not found.");
            }
            else
            {
                this.m_subscriptionsView.gameObject.SetActive(true);

                if(this.m_explorerView.gameObject != null)
                {
                    this.m_explorerView.gameObject.SetActive(false);
                }
            }
        }

        public void ShowLoginDialog()
        {
            if(this.m_loginDialog == null)
            {
                Debug.Log("[mod.io] Login Dialog not found.");
            }
            else
            {
                if(this.m_inspectorView != null)
                {
                    this.m_inspectorView.gameObject.SetActive(false);
                }

                this.m_loginDialog.gameObject.SetActive(true);
            }
        }
    }
}
