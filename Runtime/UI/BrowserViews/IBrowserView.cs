using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Provides the interface for a browser view to implement.</summary>
    public interface IBrowserView
    {
        /// <summary>GameObject for the view.</summary>
        GameObject gameObject { get; }

        /// <summary>CanvasGroup attached to the view.</summary>
        CanvasGroup canvasGroup { get; }

        /// <summary>Should the selection be reset when the view is hidden?</summary>
        bool resetSelectionOnHide { get; }

        /// <summary>Is the view a root view or window view?</summary>
        bool isRootView { get; }

        /// <summary>The priority to focus the selectables.</summary>
        List<Selectable> onFocusPriority { get; }
    }
}
