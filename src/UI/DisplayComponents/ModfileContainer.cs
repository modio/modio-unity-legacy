using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays a collection of modfiles.</summary>
    public class ModfileContainer : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Template to duplicate for the purpose of displaying the gallery images.</summary>
        public RectTransform template = null;

        /// <summary>Should the template be disabled if empty?</summary>
        public bool hideIfEmpty = false;

        // --- Run-Time Data ---
        /// <summary>Instance of the template clone.</summary>
        private GameObject m_templateClone = null;

        /// <summary>Container for the display objects.</summary>
        private RectTransform m_container = null;

        /// <summary>Modfile View item template.</summary>
        private ModfileView m_itemTemplate = null;

        /// <summary>Modfiles currently being displayed.</summary>
        private Modfile[] m_modfiles = new Modfile[0];

        /// <summary>Display objects.</summary>
        private ModfileView[] m_views = new ModfileView[0];

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initialize template.</summary>
        protected virtual void Awake()
        {
            // duplication protection
            if(this.m_itemTemplate != null) { return; }

            // initialize
            this.template.gameObject.SetActive(false);
            this.m_itemTemplate = this.template.GetComponentInChildren<ModfileView>(true);

            if(this.m_itemTemplate != null
               && this.template.gameObject != this.m_itemTemplate.gameObject)
            {
                this.m_templateClone = GameObject.Instantiate(this.template.gameObject, this.template.parent);
                this.m_templateClone.SetActive(true);
                this.m_templateClone.transform.SetSiblingIndex(this.template.GetSiblingIndex() + 1);

                this.m_views = new ModfileView[1];
                this.m_views[0] = this.m_templateClone.GetComponentInChildren<ModfileView>(true);
                this.m_views[0].gameObject.name = "Modfile View [00]";

                this.m_container = (RectTransform)this.m_views[0].transform.parent;
            }
            else
            {
                Debug.LogError("[mod.io] This ModfileContainer has an invalid template"
                               + " hierarchy. The Template must contain a child with a"
                               + " ModfileView component to use as the item template.",
                               this);
            }
        }

        /// <summary>Ensure the display is current.</summary>
        protected virtual void OnEnable()
        {
            this.DisplayModfiles(this.m_modfiles);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays a set of modfiles.</summary>
        public virtual void DisplayModfiles(IList<Modfile> modfiles)
        {
            // copy data
            if(this.m_modfiles != modfiles)
            {
                int modfileCount = 0;
                if(modfiles != null)
                {
                    modfileCount = modfiles.Count;
                }

                this.m_modfiles = new Modfile[modfileCount];
                for(int i = 0; i < modfileCount; ++i)
                {
                    this.m_modfiles[i] = modfiles[i];
                }
            }

            // display
            if(this.isActiveAndEnabled)
            {
                // set view count
                int itemCount = this.m_modfiles.Length;
                UIUtilities.SetInstanceCount(this.m_container, this.m_itemTemplate,
                                             "Mod View", itemCount,
                                             ref this.m_views);

                // display data
                for(int i = 0; i < itemCount; ++i)
                {
                    this.m_views[i].modfile = this.m_modfiles[i];
                }

                // hide if necessary
                this.m_templateClone.SetActive(itemCount > 0 || !this.hideIfEmpty);
            }
        }
    }
}
