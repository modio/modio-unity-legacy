using UnityEngine;

namespace ModIO.UI
{
    /// <summary>A component designed to place itself behind the foremost open window.</summary>
    [RequireComponent(typeof(Canvas))]
    public class WindowBacking : MonoBehaviour
    {
        // --- Accessors ---
        /// <summary>The attached Canvas component.</summary>
        public Canvas canvas
        {
            get {
                return this.gameObject.GetComponent<Canvas>();
            }
        }

        // ---------[ UI Functionality ]---------
        /// <summary>Ensures the canvas properties are correct.</summary>
        private void Start()
        {
            this.canvas.overrideSorting = true;
        }

        /// <summary>Sets the sorting order of the attached canvas to be directly behind the open
        /// window.</summary>
        public void UpdateSortingOrder(IBrowserView view)
        {
            bool validView = false;

            if(view != null && !view.isRootView && view.gameObject != null)
            {
                Canvas viewCanvas = view.gameObject.GetComponent<Canvas>();

                if(viewCanvas != null)
                {
                    int targetSortOrder = viewCanvas.sortingOrder - 1;
                    this.canvas.sortingOrder = targetSortOrder;

                    validView = true;
                }
            }

            this.gameObject.SetActive(validView);
        }
    }
}
