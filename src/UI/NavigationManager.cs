using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModIO.UI
{
    /// <summary>Component responsible for managing the navigation of the UI.</summary>
    /// <remarks>This component needs to be Updated **before** the EventSystem/InputModule has
    /// Updated to ensure that the selection storage functions as expected.</remarks>
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

        /// <summary>Is the player currently navigation the UI with the mouse?</summary>
        public bool isMouseMode = true;

        /// <summary>Selections to remember for when a view is refocused.</summary>
        private Dictionary<IBrowserView, GameObject> m_lastViewSelection = new Dictionary<IBrowserView, GameObject>();

        /// <summary>Currently hovered selectable.</summary>
        private Selectable m_currentHoverSelectable = null;

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

        // ---------[ Updates ]---------
        /// <summary>Checks whether the input method needs to be switched.</summary>
        private void Update()
        {
            IBrowserView currentView = ViewManager.instance.currentFocus;
            if(currentView == null) { return; }

            bool controllerInput = (Input.GetAxis("Horizontal") != 0f
                                    || Input.GetAxis("Vertical") != 0f
                                    || Input.GetButton("Submit")
                                    || Input.GetButton("Cancel"));

            bool mouseInput = (Input.GetAxis("Mouse X") != 0
                               || Input.GetAxis("Mouse Y") != 0
                               || Input.GetMouseButton(0)
                               || Input.GetMouseButton(1)
                               || Input.GetMouseButton(2));

            // controllerMode needs to be set
            if(controllerInput && this.isMouseMode)
            {
                this.isMouseMode = false;
                EventSystem.current.SetSelectedGameObject(this.ReacquireSelectionForView(currentView));

                if(this.m_currentHoverSelectable != null)
                {
                    ExecuteEvents.Execute(this.m_currentHoverSelectable.gameObject,
                                          new PointerEventData(EventSystem.current),
                                          ExecuteEvents.pointerExitHandler);

                    this.m_currentHoverSelectable = null;
                }
            }
            // mouseMode needs to be set
            else if(!this.isMouseMode && mouseInput && !controllerInput)
            {
                this.isMouseMode = true;
                EventSystem.current.SetSelectedGameObject(null);

                this.m_currentHoverSelectable = NavigationManager.GetHoveredSelectable();
                if(this.m_currentHoverSelectable != null)
                {
                    ExecuteEvents.Execute(this.m_currentHoverSelectable.gameObject,
                                          new PointerEventData(EventSystem.current),
                                          ExecuteEvents.pointerEnterHandler);
                }
            }
        }

        /// <summary>Handles any necessary selection corrections and storage.</summary>
        private void LateUpdate()
        {
            IBrowserView currentView = ViewManager.instance.currentFocus;
            if(currentView == null) { return; }

            GameObject currentSelection = EventSystem.current.currentSelectedGameObject;

            // mouse mode
            if(this.isMouseMode)
            {
                this.m_currentHoverSelectable = NavigationManager.GetHoveredSelectable();

                if(this.m_currentHoverSelectable != null
                   && NavigationManager.IsValidSelection(this.m_currentHoverSelectable.gameObject)
                   && this.m_currentHoverSelectable.navigation.mode != Navigation.Mode.None)
                {
                    currentSelection = this.m_currentHoverSelectable.gameObject;
                }
            }
            // controller/keyboard mode
            else
            {
                // on controller/keyboard input reset selection
                if(!NavigationManager.IsValidSelection(currentSelection))
                {
                    currentSelection = this.ReacquireSelectionForView(currentView);
                    EventSystem.current.SetSelectedGameObject(currentSelection);
                }
            }

            // store
            if(currentSelection != null)
            {
                this.m_lastViewSelection[ViewManager.instance.currentFocus] = currentSelection;
            }
        }

        // ---------[ Event Handlers ]---------
        /// <summary>Makes the view uninteractable and deselects/dehighlights objects.</summary>
        public void OnDefocusView(IBrowserView view)
        {
            if(EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if(this.isMouseMode && this.m_currentHoverSelectable != null)
            {
                ExecuteEvents.Execute(this.m_currentHoverSelectable.gameObject,
                                      new PointerEventData(EventSystem.current),
                                      ExecuteEvents.pointerExitHandler);
            }

            view.canvasGroup.interactable = false;
        }

        /// <summary>Set the selection for a change in focus.</summary>
        public void OnFocusView(IBrowserView view)
        {
            view.canvasGroup.interactable = true;
            this.menuBar.interactable = view.isRootView;

            GameObject newSelection = EventSystem.current.currentSelectedGameObject;

            if(this.isMouseMode)
            {
                this.m_currentHoverSelectable = NavigationManager.GetHoveredSelectable();
                if(this.m_currentHoverSelectable != null)
                {
                    ExecuteEvents.Execute(this.m_currentHoverSelectable.gameObject,
                                          new PointerEventData(EventSystem.current),
                                          ExecuteEvents.pointerEnterHandler);
                }

                newSelection = null;
            }
            else
            {
                if(!NavigationManager.IsValidSelection(newSelection))
                {
                    newSelection = this.ReacquireSelectionForView(view);
                }
            }

            if(newSelection != EventSystem.current.currentSelectedGameObject)
            {
                EventSystem.current.SetSelectedGameObject(newSelection);
            }
        }

        /// <summary>Gets the primary selection element for a given view.</summary>
        public GameObject ReacquireSelectionForView(IBrowserView view)
        {
            GameObject selection = null;

            if(this.m_lastViewSelection.TryGetValue(view, out selection)
               && NavigationManager.IsValidSelection(selection))
            {
                return selection;
            }

            int primaryPriority = -1;
            GameObject primarySelection = null;

            foreach(var selectionPriority in view.gameObject.GetComponentsInChildren<SelectionFocusPriority>())
            {
                if(selectionPriority.gameObject.activeSelf
                   && selectionPriority.priority > primaryPriority)
                {
                    primarySelection = selectionPriority.gameObject;
                    primaryPriority = selectionPriority.priority;
                }
            }

            if(primarySelection != null)
            {
                return primarySelection;
            }

            foreach(var sel in view.gameObject.GetComponentsInChildren<Selectable>())
            {
                if(sel.IsActive() && sel.interactable)
                {
                    return sel.gameObject;
                }
            }

            return null;
        }

        // ---------[ Utility ]---------
        /// <summary>Returns the object that mouse pointer is currently hovering over.</summary>
        public static Selectable GetHoveredSelectable()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                pointerId = 0,
            };
            pointerData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            GameObject hoveredObject = null;

            foreach(var candidate in results)
            {
                if(candidate.gameObject != null)
                {
                    hoveredObject = candidate.gameObject;
                    break;
                }
            }

            if(hoveredObject != null)
            {
                Transform t = hoveredObject.transform;
                while(t != null)
                {
                    Selectable s = t.GetComponent<Selectable>();

                    if(s != null
                       && s.IsActive())
                    {
                        return s;
                    }

                    t = t.parent;
                }
            }

            return null;
        }

        /// <summary>Checks if a selection object is valid.</summary>
        private static bool IsValidSelection(GameObject selectionObject)
        {
            if(selectionObject == null) { return false; }

            Selectable sel = selectionObject.GetComponent<Selectable>();
            return (selectionObject.activeInHierarchy
                    && sel != null
                    && sel.interactable
                    && sel.IsActive());
        }
    }
}
