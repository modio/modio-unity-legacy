using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Provides the interface for a browser view to implement.</summary>
    public interface IBrowserView
    {
        /// <summary>GameObject for the view.</summary>
        GameObject gameObject { get; }

        /// <summary>CanvasGroup attached to the view.</summary>
        CanvasGroup canvasGroup { get; }

        /// <summary>Primary selectable object for the view.</summary>
        GameObject primarySelection { get; }

        /// <summary>Should the selection be reset when the view is hidden?</summary>
        bool resetSelectionOnHide { get; }

        /// <summary>Is the view a root view or window view?</summary>
        bool isRootView { get; }
    }
}
