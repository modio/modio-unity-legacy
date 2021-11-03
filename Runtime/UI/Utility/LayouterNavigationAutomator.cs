using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace ModIO.UI
{
    /// <summary>Automates the navigation network for the selectable elements of a
    /// layouter.</summary>
    [RequireComponent(typeof(LayoutGroup))]
    public class LayouterNavigationAutomator : UnityEngine.EventSystems.UIBehaviour
    {
        // ---------[ Fields ]---------
        /// <summary>Horizontal navigation style.</summary>
        public EdgeCellNavigationMode horizontalNavigation = EdgeCellNavigationMode.DontWrap;

        /// <summary>Vertical navigation style.</summary>
        public EdgeCellNavigationMode verticalNavigation = EdgeCellNavigationMode.DontWrap;

        /// <summary>How many children deep are the selectables found?</summary>
        public int selectableDepth = 1;

        // ---------[ Functionality ]---------
        /// <summary>Initializes the nav data.</summary>
        protected override void Start()
        {
            base.Start();

            this.UpdateNavigationForChildren();
        }

        /// <summary>Catches the OnTransformChildrenChanged event to reapply settings.</summary>
        private void OnTransformChildrenChanged()
        {
            this.UpdateNavigationForChildren();
        }

        /// <summary>Applies the navigation settings to the child objects.</summary>
        public void UpdateNavigationForChildren()
        {
            LayoutGroup lg = this.GetComponent<LayoutGroup>();
            if(lg == null)
            {
                return;
            }

            int columnCount = 1;
            List<Selectable> selectables = null;

            if(this.selectableDepth < 0)
            {
                selectables =
                    new List<Selectable>(this.gameObject.GetComponentsInChildren<Selectable>());
            }
            else
            {
                selectables = new List<Selectable>();

                Action<Transform, int> appendChildSelectables = null;
                appendChildSelectables = (t, depth) =>
                {
                    foreach(var ignorer in t.gameObject.GetComponents<ILayoutIgnorer>())
                    {
                        if(ignorer.ignoreLayout)
                        {
                            return;
                        }
                    }

                    if(!t.gameObject.activeSelf)
                    {
                        return;
                    }

                    if(depth == selectableDepth)
                    {
                        Selectable s = t.gameObject.GetComponent<Selectable>();
                        if(s != null)
                        {
                            selectables.Add(s);
                        }
                    }
                    else
                    {
                        foreach(Transform child in t) { appendChildSelectables(child, depth + 1); }
                    }
                };

                appendChildSelectables(this.transform, 0);
            }

            if(lg is HorizontalLayoutGroup)
            {
                columnCount = selectables.Count;
            }
            else if(lg is GridLayoutGroup)
            {
                columnCount = UIUtilities.CalculateGridColumnCount((GridLayoutGroup)lg);
            }

            UIUtilities.SetExplicitGridNavigation(
                selectables, columnCount, this.horizontalNavigation, this.verticalNavigation);
        }
    }
}
