using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace ModIO.UI
{
    /// <summary>Component responsible for management of the various views.</summary>
    public class ViewManager : MonoBehaviour
    {
        // ---------[ Constants ]---------
        /// <summary>The difference between the sorting order for each view on the view
        /// stack.</summary>
        public const int SORTORDER_SPACING = 2;

        // ---------[ Nested Data-Types ]---------
        /// <summary>Event for views changing.</summary>
        [System.Serializable]
        public class ViewChangeEvent : UnityEvent<IBrowserView>
        {
        }

        // ---------[ SINGLETON ]---------
        private static ViewManager _instance = null;
        public static ViewManager instance
        {
            get {
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
        private MessageDialog m_messageDialog = null;
        private ReportDialog m_reportDialog = null;
        private bool m_viewsFound = false;

        /// <summary>Event callback for when a view is hidden.</summary>
        public ViewChangeEvent onBeforeHideView = new ViewChangeEvent();

        /// <summary>Event callback for when a view is shown.</summary>
        public ViewChangeEvent onBeforeShowView = new ViewChangeEvent();

        /// <summary>Event callback for when a view is defocused.</summary>
        public ViewChangeEvent onBeforeDefocusView = new ViewChangeEvent();

        /// <summary>Event callback for when a view is focused.</summary>
        public ViewChangeEvent onAfterFocusView = new ViewChangeEvent();

        /// <summary>View stack for all the currently open views.</summary>
        private List<IBrowserView> m_viewStack = new List<IBrowserView>();

        /// <summary>All found IBrowserView components.</summary>
        private IBrowserView[] m_views = null;

        /// <summary>Sorting order for the root view.</summary>
        private int m_rootViewSortOrder = 0;

        // --- Accessors ---
        /// <summary>Explorer View in the UI.</summary>
        public ExplorerView explorerView
        {
            get {
                return this.m_explorerView;
            }
        }
        /// <summary>Subscriptions View in the UI.</summary>
        public SubscriptionsView subscriptionsView
        {
            get {
                return this.m_subscriptionsView;
            }
        }
        /// <summary>Inspector View in the UI.</summary>
        public InspectorView inspectorView
        {
            get {
                return this.m_inspectorView;
            }
        }
        /// <summary>Login View in the UI</summary>
        public LoginDialog loginDialog
        {
            get {
                return this.m_loginDialog;
            }
        }
        /// <summary>Message View in the UI</summary>
        public MessageDialog messageDialog
        {
            get {
                return this.m_messageDialog;
            }
        }
        /// <summary>Report Mod View in the UI</summary>
        public ReportDialog reportDialog
        {
            get {
                return this.m_reportDialog;
            }
        }

        /// <summary>Currently focused view.</summary>
        public IBrowserView currentFocus
        {
            get {
                if(this.m_viewStack == null || this.m_viewStack.Count == 0)
                {
                    return null;
                }
                else
                {
                    return this.m_viewStack[this.m_viewStack.Count - 1];
                }
            }
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

            // - get the parent canvas for the views -
            IBrowserView firstView = this.m_views[0];
            Debug.Assert(
                firstView != null,
                "[mod.io] No views found in the scene."
                    + " Please ensure the scene contains at least one IBrowserView component"
                    + " before using the ViewManager.",
                this);

            Transform firstViewParent = firstView.gameObject.transform.parent;
            Debug.Assert(
                firstViewParent != null,
                "[mod.io] The first found view in the scene appears to be a root object."
                    + " ViewManager expects the views to be contained under a canvas object to"
                    + " function correctly.",
                firstView.gameObject);

            Canvas parentCanvas = firstViewParent.GetComponentInParent<Canvas>();
            Debug.Assert(
                parentCanvas != null,
                "[mod.io] The first found view in the scene has no parent canvas component."
                    + " ViewManager expects the views to be contained under a canvas object to"
                    + " function correctly.",
                firstView.gameObject);

#if UNITY_EDITOR
            if(this.m_views.Length > 1)
            {
                foreach(IBrowserView view in this.m_views)
                {
                    Transform parentTransform = view.gameObject.transform.parent;
                    if(parentTransform == null
                       || parentCanvas != parentTransform.GetComponentInParent<Canvas>())
                    {
                        Debug.LogError("[mod.io] All the views must have the same parent canvas"
                                           + " in order for the ViewManager to function correctly.",
                                       this);

                        this.enabled = false;
                        return;
                    }
                }
            }
#endif

            // set the sorting order base
            this.m_rootViewSortOrder = parentCanvas.sortingOrder + ViewManager.SORTORDER_SPACING;

            // add canvas + raycaster components to views
            foreach(IBrowserView view in this.m_views)
            {
                Canvas viewCanvas = view.gameObject.GetComponent<Canvas>();

                if(viewCanvas == null)
                {
                    viewCanvas = view.gameObject.AddComponent<Canvas>();
                    viewCanvas.overridePixelPerfect = false;
                    viewCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
                }

                GraphicRaycaster raycaster = view.gameObject.GetComponent<GraphicRaycaster>();

                if(raycaster == null)
                {
                    raycaster = view.gameObject.AddComponent<GraphicRaycaster>();
                    raycaster.ignoreReversedGraphics = true;
                    raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
                }
            }

            // - create the initial view stack -
            List<IBrowserView> initViewStack = new List<IBrowserView>();

            if(this.explorerView != null && this.explorerView.isActiveAndEnabled)
            {
                initViewStack.Add(this.explorerView);
            }

            if(this.subscriptionsView != null && this.subscriptionsView.isActiveAndEnabled)
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
            }

            if(initViewStack.Count == 0)
            {
                if(this.explorerView != null)
                {
                    initViewStack.Add(this.explorerView);

                    this.explorerView.gameObject.SetActive(true);
                }
                else if(this.subscriptionsView != null)
                {
                    initViewStack.Add(this.subscriptionsView);

                    this.subscriptionsView.gameObject.SetActive(true);
                }
#if DEBUG
                else
                {
                    Debug.Log("[mod.io] No main view found in the scene."
                                  + " Please consider adding either an ExplorerView or"
                                  + " a SubscriptionsView to the scene.",
                              this);
                }
#endif
            }

            if(this.inspectorView != null && this.inspectorView.isActiveAndEnabled)
            {
                initViewStack.Add(this.inspectorView);
            }

            if(this.loginDialog != null && this.loginDialog.isActiveAndEnabled)
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
                IBrowserView view = null;
                this.m_viewStack = viewStack;

                for(int i = 0; i < viewStack.Count - 1; ++i)
                {
                    view = viewStack[i];

                    this.SetSortOrder(view, i);
                    this.onBeforeDefocusView.Invoke(view);
                }

                view = viewStack[viewStack.Count - 1];

                this.SetSortOrder(view, viewStack.Count - 1);
                this.onAfterFocusView.Invoke(view);
            }
        }

        private void FindViews()
        {
            if(this.m_viewsFound)
            {
                return;
            }

            this.m_explorerView = GetComponentInChildren<ExplorerView>(true);
            this.m_subscriptionsView = GetComponentInChildren<SubscriptionsView>(true);
            this.m_inspectorView = GetComponentInChildren<InspectorView>(true);
            this.m_loginDialog = GetComponentInChildren<LoginDialog>(true);
            this.m_messageDialog = GetComponentInChildren<MessageDialog>(true);
            this.m_reportDialog = GetComponentInChildren<ReportDialog>(true);
            this.m_viewsFound = true;

            this.m_views = this.gameObject.GetComponentsInChildren<IBrowserView>(true);
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

            this.FocusView(this.m_inspectorView);
        }

        public void ReportMod(int modId)
        {
#if DEBUG
            if(this.m_reportDialog == null)
            {
                Debug.Log("[mod.io] Report Dialog not found.");
            }
#endif

            if(this.m_reportDialog == null)
            {
                return;
            }

            this.m_reportDialog.SetModId(modId);
            this.FocusView(this.m_reportDialog);
        }

        public void ActivateExplorerView()
        {
#if DEBUG
            if(this.m_explorerView == null)
            {
                Debug.Log("[mod.io] Explorer View not found.");
            }
#endif

            if(this.m_explorerView == null)
            {
                return;
            }

            this.FocusView(this.m_explorerView);
        }

        public void ActivateSubscriptionsView()
        {
#if DEBUG
            if(this.m_subscriptionsView == null)
            {
                Debug.Log("[mod.io] Subscriptions View not found.");
            }
#endif

            if(this.m_subscriptionsView == null)
            {
                return;
            }

            this.FocusView(this.m_subscriptionsView);
        }

        public void ShowLoginDialog()
        {
#if DEBUG
            if(this.m_loginDialog == null)
            {
                Debug.Log("[mod.io] Login Dialog not found.");
            }
#endif

            this.FocusView(this.m_loginDialog);
        }

        /// <summary>Shows the message dialog using the given settings.</summary>
        public void ShowMessageDialog(MessageDialog.Data messageData)
        {
            if(this.m_messageDialog == null)
            {
                return;
            }

            this.m_messageDialog.ApplyData(messageData);

            this.FocusView(this.m_messageDialog);
        }

        /// <summary>Shows the report mod dialog using the given data.</summary>
        public void ShowReportDialog(int modId)
        {
            if(this.m_reportDialog == null)
            {
                return;
            }

            this.FocusView(this.m_reportDialog);
        }

        /// <summary>Focuses the given view, showing and hiding views as necessary.</summary>
        public void FocusView(IBrowserView view)
        {
            if(view == null || view == this.currentFocus)
            {
                return;
            }

            if(this.currentFocus != null)
            {
                this.onBeforeDefocusView.Invoke(this.currentFocus);
            }

            // knock off views above the desired one
            if(view.isRootView || this.m_viewStack.Contains(view))
            {
                while(this.m_viewStack.Count > 0 && this.currentFocus != view)
                {
                    IBrowserView closingView = this.currentFocus;

                    this.onBeforeHideView.Invoke(closingView);

                    // NOTE(@jackson): The order here is important. Some views may check the view
                    // stack OnDisable
                    this.m_viewStack.RemoveAt(this.m_viewStack.Count - 1);
                    closingView.gameObject.SetActive(false);
                }
            }

            // push the view if necessary
            if(this.currentFocus != view)
            {
                this.onBeforeShowView.Invoke(view);

                // NOTE(@jackson): The order here is important. Some views may check the view stack
                // OnEnable, and the gameObject must be active for the SetSortOrder to function
                // correctly
                this.m_viewStack.Add(view);
                view.gameObject.SetActive(true);

                this.SetSortOrder(view, this.m_viewStack.Count - 1);
            }

            this.onAfterFocusView.Invoke(view);
        }

        /// <summary>Closes and hides a view.</summary>
        public void CloseWindowedView(IBrowserView view)
        {
            if(view == null || !view.gameObject.activeSelf)
            {
                return;
            }

            int viewIndex = this.m_viewStack.IndexOf(view);

            if(viewIndex >= 0)
            {
                if(this.currentFocus == view)
                {
                    this.PopView();
                }
                else
                {
                    this.onBeforeHideView.Invoke(view);

                    // NOTE(@jackson): The order here is important. Some views may check the view
                    // stack OnDisable.
                    this.m_viewStack.RemoveAt(viewIndex);
                    view.gameObject.SetActive(false);

                    for(int i = viewIndex; i < this.m_viewStack.Count; ++i)
                    {
                        this.SetSortOrder(this.m_viewStack[i], i);
                    }
                }
            }
        }

        /// <summary>Pushes a view to the stack and fires the necessary events.</summary>
        public void PushView(IBrowserView view)
        {
            Debug.Assert(view != null);
            Debug.Assert(view.gameObject.GetComponent<Canvas>() != null);

            if(this.m_viewStack.Contains(view))
            {
                return;
            }

            if(this.currentFocus != null)
            {
                this.onBeforeDefocusView.Invoke(this.currentFocus);
            }

            this.onBeforeShowView.Invoke(view);

            // NOTE(@jackson): The order here is important. Some views may check the view stack
            // OnEnable, and the gameObject must be active for the SetSortOrder to function
            // correctly
            this.m_viewStack.Add(view);
            view.gameObject.SetActive(true);

            this.SetSortOrder(view, this.m_viewStack.Count - 1);
            this.onAfterFocusView.Invoke(view);
        }

        /// <summary>Pops a view from the stack and fires the necessary events.</summary>
        public void PopView()
        {
            Debug.Assert(this.m_viewStack.Count > 0);

            IBrowserView view = this.currentFocus;

            this.onBeforeDefocusView.Invoke(view);
            this.onBeforeHideView.Invoke(view);

            // NOTE(@jackson): The order here is important. Some views may check the view stack
            // OnDisable
            this.m_viewStack.RemoveAt(this.m_viewStack.Count - 1);
            view.gameObject.SetActive(false);

            if(this.currentFocus != null)
            {
                this.onAfterFocusView.Invoke(this.currentFocus);
            }
        }

        // ---------[ Utility ]---------
        /// <summary>Sets the sorting order for the view.</summary>
        private void SetSortOrder(IBrowserView view, int stackIndex)
        {
            /**
             * NOTE(@jackson): overrideSorting MUST be set before the sortingOrder, otherwise the
             * sortingOrder value won't update until the next time the object is disabled->enabled
             **/
            Canvas viewCanvas = view.gameObject.GetComponent<Canvas>();
            viewCanvas.overrideSorting = true;
            viewCanvas.sortingOrder =
                this.m_rootViewSortOrder + stackIndex * ViewManager.SORTORDER_SPACING;
        }
    }
}
