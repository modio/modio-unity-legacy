using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Automates the navigation network for the selectable elements of a layouter.</summary>
    [RequireComponent(typeof(LayoutGroup))]
    public class LayouterExplicitNavigationAutomator : UnityEngine.EventSystems.UIBehaviour
    {
        // ---------[ Fields ]---------
        /// <summary>Should the navigation wrap vertically?</summary>
        public bool wrapVertically = false;

        /// <summary>Should the navigation wrap horizontally?</summary>
        public bool wrapHorizontally = false;

        // ---------[ Functionality ]---------
        /// <summary>Initializes the nav data.</summary>
        protected override void Start()
        {
            base.Start();

            this.SetNavigationForChildren();
        }

        /// <summary>Catches the OnTransformChildrenChanged event to reapply settings.</summary>
        private void OnTransformChildrenChanged()
        {
            this.SetNavigationForChildren();
        }

        /// <summary>Applies the navigation settings to the child objects.</summary>
        public void SetNavigationForChildren()
        {
            LayoutGroup lg = this.GetComponent<LayoutGroup>();
            if(lg == null) { return; }

            Selectable[] selectables = this.gameObject.GetComponentsInChildren<Selectable>();
            int columnCount = 1;

            if(lg is HorizontalLayoutGroup)
            {
                columnCount = selectables.Length;
            }
            else if(lg is GridLayoutGroup)
            {
                columnCount = UIUtilities.CalculateGridColumnCount((GridLayoutGroup)lg);
            }

            UIUtilities.SetExplicitGridNavigation(selectables, columnCount,
                                                  this.wrapVertically,
                                                  this.wrapHorizontally);
        }
    }
}
