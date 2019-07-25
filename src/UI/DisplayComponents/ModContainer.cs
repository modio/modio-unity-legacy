using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays a collection of mods.</summary>
    public class ModContainer : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Template to duplicate for the purpose of displaying the mod views.</summary>
        public RectTransform template = null;

        /// <summary>Should the template be disabled if empty?</summary>
        public bool hideIfEmpty = true;

        /// <summary>Limit of mod views that can be displayed in this container.</summary>
        public int itemLimit = -1;

        // --- Run-Time Data ---
        /// <summary>Instance of the template clone.</summary>
        private GameObject m_templateClone = null;

        /// <summary>Container for the display objects.</summary>
        private RectTransform m_container = null;

        /// <summary>Mod View object template.</summary>
        private ModView m_itemTemplate = null;

        /// <summary>Profiles currently being displayed.</summary>
        private ModProfile[] m_modProfiles = new ModProfile[0];

        /// <summary>Statistics current being displayed.</summary>
        private ModStatistics[] m_modStatistics = new ModStatistics[0];

        /// <summary>Display objects.</summary>
        private ModView[] m_views = new ModView[0];

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initialize template.</summary>
        protected virtual void Awake()
        {
            // duplication protection
            if(this.m_itemTemplate != null) { return; }

            // initialize
            this.template.gameObject.SetActive(false);
            this.m_itemTemplate = this.template.GetComponentInChildren<ModView>(true);

            if(this.m_itemTemplate != null
               && this.template.gameObject != this.m_itemTemplate.gameObject)
            {
                this.m_templateClone = GameObject.Instantiate(this.template.gameObject, this.template.parent);
                this.m_templateClone.SetActive(true);
                this.m_templateClone.transform.SetSiblingIndex(this.template.GetSiblingIndex() + 1);

                this.m_views = new ModView[1];
                this.m_views[0] = this.m_templateClone.GetComponentInChildren<ModView>(true);
                this.m_views[0].gameObject.name = "Mod View [00]";

                this.m_container = (RectTransform)this.m_views[0].transform.parent;
            }
            else
            {
                Debug.LogError("[mod.io] This ModContainer has an invalid template"
                               + " hierarchy. The Template must contain a child with a"
                               + " ModView component to use as the item template.",
                               this);
            }
        }

        /// <summary>Ensure the display is current.</summary>
        protected virtual void OnEnable()
        {
            this.DisplayMods(this.m_modProfiles, this.m_modStatistics);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays a set of mods.</summary>
        public virtual void DisplayMods(IList<ModProfile> profiles, IList<ModStatistics> statistics)
        {
            // assert validity
            if(profiles != null
               && statistics != null
               && profiles.Count != statistics.Count)
            {
                Debug.LogWarning("[mod.io] Cannot display a collection of profiles"
                                 + " and statistics where the counts are not equal."
                                 + "\n profiles.Count = " + profiles.Count.ToString()
                                 + "\n statistics.Count = " + statistics.Count.ToString(),
                                 this);

                statistics = null;
            }

            int itemCount = 0;
            if(profiles != null)
            {
                itemCount = profiles.Count;
            }
            else if(statistics != null)
            {
                itemCount = statistics.Count;
            }

            if(this.itemLimit >= 0 && itemCount > this.itemLimit)
            {
                Debug.LogWarning("[mod.io] Attempting to display more mods than accepted by this"
                                 + " Mod Container."
                                 + "\n Item Limit = " + this.itemLimit.ToString()
                                 + "\n Item Count = " + itemCount.ToString(),
                                 this);
            }


            // copy arrays
            if(this.m_modProfiles != profiles)
            {
                this.m_modProfiles = new ModProfile[itemCount];
                for(int i = 0; i < itemCount; ++i)
                {
                    this.m_modProfiles[i] = profiles[i];
                }
            }
            if(this.m_modStatistics != statistics)
            {
                this.m_modStatistics = new ModStatistics[itemCount];
                for(int i = 0; i < itemCount; ++i)
                {
                    this.m_modStatistics[i] = statistics[i];
                }
            }

            // display
            if(this.isActiveAndEnabled)
            {
                this.SetViewCount(itemCount);

                // display data
                if(this.m_modProfiles != null)
                {
                    for(int i = 0; i < this.m_modProfiles.Length; ++i)
                    {
                        this.m_views[i].profile = this.m_modProfiles[i];
                    }

                }
                if(this.m_modStatistics != null)
                {
                    for(int i = 0; i < this.m_modStatistics.Length; ++i)
                    {
                        this.m_views[i].statistics = this.m_modStatistics[i];
                    }
                }

                // hide if necessary
                this.m_templateClone.SetActive(itemCount > 0 || !this.hideIfEmpty);
            }
        }

        /// <summary>Creates/Destroys views to match the given value.</summary>
        protected virtual void SetViewCount(int newCount)
        {
            int difference = newCount - this.m_views.Length;

            if(difference > 0)
            {
                ModView[] newViewArray = new ModView[newCount];

                for(int i = 0;
                    i < this.m_views.Length;
                    ++i)
                {
                    newViewArray[i] = this.m_views[i];
                }

                for(int i = this.m_views.Length;
                    i < newViewArray.Length;
                    ++i)
                {
                    GameObject displayGO = GameObject.Instantiate(this.m_itemTemplate.gameObject);
                    displayGO.name = "Mod View [" + i.ToString("00") + "]";
                    displayGO.transform.SetParent(this.m_container, false);

                    newViewArray[i] = displayGO.GetComponent<ModView>();
                }

                this.m_views = newViewArray;
            }
            else if(difference < 0)
            {
                ModView[] newViewArray = new ModView[newCount];

                for(int i = 0;
                    i < newViewArray.Length;
                    ++i)
                {
                    newViewArray[i] = this.m_views[i];
                }

                for(int i = newViewArray.Length;
                    i < this.m_views.Length;
                    ++i)
                {
                    GameObject.Destroy(this.m_views[i].gameObject);
                }

                this.m_views = newViewArray;
            }
        }
    }
}
