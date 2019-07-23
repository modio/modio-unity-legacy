using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays a collection of mods.</summary>
    public class ModContainer : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Template to duplicate for the purpose of displaying the gallery images.</summary>
        public RectTransform template = null;

        /// <summary>Should the template be disabled if empty?</summary>
        public bool hideIfEmpty = true;

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
        /// <summary>Displays a set of gallery images.</summary>
        public virtual void DisplayMods(IList<ModProfile> profiles, IList<ModStatistics> statistics)
        {
            // assert validity
            if(profiles != null
               && statistics != null
               && profiles.Count != statistics.Count)
            {
                Debug.LogWarning("[mod.io] Cannot display the a collection of profiles"
                                 + " and statistics where the counts are not equal."
                                 + "\n profiles.Count = " + profiles.Count.ToString()
                                 + "\n statistics.Count = " + statistics.Count.ToString(),
                                 this);

                statistics = null;
            }

            // copy arrays
            if(this.m_modProfiles != profiles)
            {
                int profileCount = 0;
                if(profiles != null)
                {
                    profileCount = profiles.Count;
                }

                this.m_modProfiles = new ModProfile[profileCount];
                for(int i = 0; i < profileCount; ++i)
                {
                    this.m_modProfiles[i] = profiles[i];
                }
            }

            if(this.m_modStatistics != profiles)
            {
                int statisticsCount = 0;
                if(statistics != null)
                {
                    statisticsCount = statistics.Count;
                }

                this.m_modStatistics = new ModStatistics[statisticsCount];
                for(int i = 0; i < statisticsCount; ++i)
                {
                    this.m_modStatistics[i] = statistics[i];
                }
            }

            // display
            if(this.isActiveAndEnabled)
            {
                // set view count
                int itemCount = this.m_modProfiles.Length;
                if(itemCount == 0)
                {
                    itemCount = this.m_modStatistics.Length;
                }

                this.SetViewCount(itemCount);

                // Assert we good
                Debug.Assert(this.m_views.Length >= this.m_modProfiles.Length);
                Debug.Assert(this.m_views.Length >= this.m_modStatistics.Length);

                // display data
                for(int i = 0; i < this.m_modProfiles.Length; ++i)
                {
                    this.m_views[i].profile = this.m_modProfiles[i];
                }
                for(int i = 0; i < this.m_modStatistics.Length; ++i)
                {
                    this.m_views[i].statistics = this.m_modStatistics[i];
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
