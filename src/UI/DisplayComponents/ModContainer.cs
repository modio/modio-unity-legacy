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
        [SerializeField]
        private int m_itemLimit = -1;

        /// <summary>Fill to item limit.</summary>
        [Tooltip("If enabled, fills the container with hidden mod views to match the item limit.")]
        public bool fillToLimit = false;

        // --- Run-Time Data ---
        /// <summary>Instance of the template clone.</summary>
        private GameObject m_templateClone = null;

        /// <summary>Container for the display objects.</summary>
        private RectTransform m_container = null;

        /// <summary>Mod View object template.</summary>
        private ModView m_itemTemplate = null;

        /// <summary>Display objects.</summary>
        private ModView[] m_views = new ModView[0];

        /// <summary>Profiles currently being displayed.</summary>
        private ModProfile[] m_modProfiles = new ModProfile[0];

        /// <summary>Statistics current being displayed.</summary>
        private ModStatistics[] m_modStatistics = new ModStatistics[0];

        // --- Accessors ---
        /// <summary>Limit of mod views that can be displayed in this container.</summary>
        public int itemLimit
        {
            get { return this.m_itemLimit; }
            set { this.m_itemLimit = value; }
        }

        /// <summary>Profiles currently being displayed.</summary>
        public ModProfile[] modProfiles
        {
            get { return this.m_modProfiles; }
        }

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initialize template.</summary>
        protected virtual void Awake()
        {
            this.template.gameObject.SetActive(false);

            // check template
            #if DEBUG
            string message;
            if(!ModContainer.HasValidTemplate(this, out message))
            {
                Debug.LogError("[mod.io] " + message, this);
                return;
            }
            #endif

            // get template vars
            Transform templateParent = this.template.parent;
            string templateInstance_name = this.template.gameObject.name + " (Instance)";
            int templateInstance_index = this.template.GetSiblingIndex() + 1;

            // NOTE(@jackson): The canvas group is required to hide the unused
            // ModViews in the case of this.fillToLimit
            this.m_itemTemplate = this.template.GetComponentInChildren<ModView>(true);
            if(this.m_itemTemplate.gameObject.GetComponent<CanvasGroup>() == null)
            {
                this.m_itemTemplate.gameObject.AddComponent<CanvasGroup>();
            }

            // duplication protection
            bool isInstantiated = (templateParent.childCount > templateInstance_index
                                   && templateParent.GetChild(templateInstance_index).gameObject.name == templateInstance_name);
            if(isInstantiated)
            {
                this.m_templateClone = templateParent.GetChild(templateInstance_index).gameObject;
                ModView[] viewInstances = this.m_templateClone.GetComponentsInChildren<ModView>(true);

                if(viewInstances == null
                   || viewInstances.Length == 0)
                {
                    isInstantiated = false;
                    GameObject.Destroy(this.m_templateClone);
                }
                else
                {
                    this.m_container = (RectTransform)viewInstances[0].transform.parent;

                    foreach(ModView view in viewInstances)
                    {
                        GameObject.Destroy(view.gameObject);
                    }
                }
            }

            if(!isInstantiated)
            {
                this.m_templateClone = GameObject.Instantiate(this.template.gameObject, templateParent);
                this.m_templateClone.SetActive(true);
                this.m_templateClone.transform.SetSiblingIndex(templateInstance_index);
                this.m_templateClone.name = templateInstance_name;

                ModView viewInstance = this.m_templateClone.GetComponentInChildren<ModView>(true);
                this.m_container = (RectTransform)viewInstance.transform.parent;

                GameObject.Destroy(viewInstance.gameObject);
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

            if(this.m_itemLimit >= 0 && itemCount > this.m_itemLimit)
            {
                Debug.LogWarning("[mod.io] Attempting to display more mods than accepted by this"
                                 + " Mod Container."
                                 + "\n Item Limit = " + this.m_itemLimit.ToString()
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
            if(this.m_itemTemplate != null)
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
            Debug.Assert(this.m_itemLimit < 0 || newCount <= this.m_itemLimit);

            // checks for filling container
            int visibleCount = newCount;
            int viewCount = newCount;
            if(this.fillToLimit && this.m_itemLimit >= 0)
            {
                viewCount = this.m_itemLimit;
            }

            // -- create/destroy as necessary --
            int difference = viewCount - this.m_views.Length;

            if(difference > 0)
            {
                ModView[] newViewArray = new ModView[viewCount];

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
                ModView[] newViewArray = new ModView[viewCount];

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

            // -- set view visibility --
            for(int i = 0;
                i < visibleCount;
                ++i)
            {
                CanvasGroup c = this.m_views[i].GetComponent<CanvasGroup>();
                c.alpha = 1f;
                c.interactable = true;
                c.blocksRaycasts = true;
            }

            for(int i = visibleCount;
                i < viewCount;
                ++i)
            {
                CanvasGroup c = this.m_views[i].GetComponent<CanvasGroup>();
                c.alpha = 0f;
                c.interactable = false;
                c.blocksRaycasts = false;
            }
        }

        // ---------[ UTILITY ]---------
        /// <summary>Checks a ModContainer's template structure.</summary>
        public static bool HasValidTemplate(ModContainer container, out string helpMessage)
        {
            helpMessage = null;
            bool isValid = true;

            if(container.template.gameObject == container.gameObject
               || container.transform.IsChildOf(container.template))
            {
                helpMessage = ("This Mod Container has an invalid template."
                               + "\nThe container template cannot share the same GameObject"
                               + " as this Mod Container component, and cannot be a parent of"
                               + " this object.");
                isValid = false;
            }

            ModView itemTemplate = container.template.GetComponentInChildren<ModView>(true);
            if(itemTemplate == null
               || container.template.gameObject == itemTemplate.gameObject)
            {
                helpMessage = ("This Mod Container has an invalid template."
                               + "\nThe container template needs a child with the ModView"
                               + " component attached to use as the item template.");
                isValid = false;
            }

            return isValid;
        }
    }
}
