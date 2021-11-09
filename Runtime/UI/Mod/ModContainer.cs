using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays a collection of mods.</summary>
    public class ModContainer : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Template to duplicate for the purpose of displaying the mod views.</summary>
        public RectTransform containerTemplate = null;

        /// <summary>Should the template be disabled if empty?</summary>
        public bool hideIfEmpty = false;

        /// <summary>Limit of mod views that can be displayed in this container.</summary>
        [SerializeField]
        private int m_itemLimit = -1;

        /// <summary>Fill to item limit.</summary>
        [SerializeField]
        [Tooltip("If enabled, fills the container with hidden mod views to match the item limit.")]
        private bool m_fillToLimit = false;

        /// <summary>Event triggered when the item limit is changed.</summary>
        public event System.Action<int> onItemLimitChanged = null;

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
            get {
                return this.m_itemLimit;
            }
            set {
                if(this.m_itemLimit != value)
                {
                    this.m_itemLimit = value;

                    this.DisplayMods(this.m_modProfiles, this.m_modStatistics);

                    if(this.onItemLimitChanged != null)
                    {
                        this.onItemLimitChanged.Invoke(this.m_itemLimit);
                    }
                }
            }
        }

        /// <summary>Profiles currently being displayed.</summary>
        public ModProfile[] modProfiles
        {
            get {
                return this.m_modProfiles;
            }
        }

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            this.containerTemplate.gameObject.SetActive(false);
        }

        /// <summary>Initialize template.</summary>
        protected virtual void Start()
        {
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
            Transform templateParent = this.containerTemplate.parent;
            string templateInstance_name = this.containerTemplate.gameObject.name + " (Instance)";
            int templateInstance_index = this.containerTemplate.GetSiblingIndex() + 1;

            // NOTE(@jackson): The canvas group is required to hide the unused
            // ModViews in the case of this.m_fillToLimit
            this.m_itemTemplate = this.containerTemplate.GetComponentInChildren<ModView>(true);
            if(this.m_itemTemplate.gameObject.GetComponent<CanvasGroup>() == null)
            {
                this.m_itemTemplate.gameObject.AddComponent<CanvasGroup>();
            }

            // duplication protection
            bool isInstantiated =
                (templateParent.childCount > templateInstance_index
                 && templateParent.GetChild(templateInstance_index).gameObject.name
                        == templateInstance_name);
            if(isInstantiated)
            {
                this.m_templateClone = templateParent.GetChild(templateInstance_index).gameObject;
                ModView[] viewInstances =
                    this.m_templateClone.GetComponentsInChildren<ModView>(true);

                if(viewInstances == null || viewInstances.Length == 0)
                {
                    isInstantiated = false;
                    GameObject.Destroy(this.m_templateClone);
                }
                else
                {
                    this.m_container = (RectTransform)viewInstances[0].transform.parent;

                    foreach(ModView view in viewInstances) { GameObject.Destroy(view.gameObject); }
                }
            }

            if(!isInstantiated)
            {
                this.m_templateClone =
                    GameObject.Instantiate(this.containerTemplate.gameObject, templateParent);
                this.m_templateClone.transform.SetSiblingIndex(templateInstance_index);
                this.m_templateClone.name = templateInstance_name;

                ModView viewInstance = this.m_templateClone.GetComponentInChildren<ModView>(true);
                this.m_container = (RectTransform)viewInstance.transform.parent;
                GameObject.Destroy(viewInstance.gameObject);

                this.m_templateClone.SetActive(true);
            }

            this.DisplayMods(this.m_modProfiles, this.m_modStatistics);
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
            if(profiles != null && statistics != null && profiles.Count != statistics.Count)
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

            // copy arrays
            if(this.m_modProfiles != profiles)
            {
                this.m_modProfiles = new ModProfile[itemCount];
                for(int i = 0; i < itemCount; ++i) { this.m_modProfiles[i] = profiles[i]; }
            }
            if(this.m_modStatistics != statistics)
            {
                this.m_modStatistics = new ModStatistics[itemCount];
                for(int i = 0; i < itemCount; ++i) { this.m_modStatistics[i] = statistics[i]; }
            }

            // display
            if(this.m_itemTemplate != null)
            {
                Debug.Assert(this.m_container != null);

                // set instance count
                int viewCount = itemCount;
                if(this.m_fillToLimit && this.m_itemLimit >= 0)
                {
                    viewCount = this.m_itemLimit;

                    if(viewCount < itemCount)
                    {
                        itemCount = viewCount;
                    }
                }

                UIUtilities.SetInstanceCount(this.m_container, this.m_itemTemplate, "Mod View",
                                             viewCount, ref this.m_views, true);


                // -- set view visibility --
                if(this.m_fillToLimit && this.m_itemLimit >= 0)
                {
                    int visibleCount = itemCount;

                    for(int i = 0; i < visibleCount; ++i)
                    {
                        CanvasGroup c = this.m_views[i].GetComponent<CanvasGroup>();
                        c.alpha = 1f;
                        c.interactable = true;
                        c.blocksRaycasts = true;
                    }

                    for(int i = visibleCount; i < viewCount; ++i)
                    {
                        CanvasGroup c = this.m_views[i].GetComponent<CanvasGroup>();
                        c.alpha = 0f;
                        c.interactable = false;
                        c.blocksRaycasts = false;
                    }
                }

                // display data
                if(this.m_modProfiles != null)
                {
                    for(int i = 0; i < this.m_modProfiles.Length && i < viewCount; ++i)
                    {
                        this.m_views[i].profile = this.m_modProfiles[i];
                    }
                }
                if(this.m_modStatistics != null)
                {
                    for(int i = 0; i < this.m_modStatistics.Length && i < viewCount; ++i)
                    {
                        this.m_views[i].statistics = this.m_modStatistics[i];
                    }
                }

                // hide if necessary
                this.m_templateClone.SetActive(itemCount > 0 || !this.hideIfEmpty);
            }
        }

        // ---------[ UTILITY ]---------
        /// <summary>Returns the views currently being managed by the mod container.</summary>
        public List<ModView> GetModViews()
        {
            List<ModView> retVal = null;
            if(this.m_fillToLimit && this.m_views != null)
            {
                retVal = new List<ModView>();

                foreach(ModView view in this.m_views)
                {
                    if(view.gameObject.GetComponent<CanvasGroup>().alpha == 1f)
                    {
                        retVal.Add(view);
                    }
                }
            }
            else if(this.m_views != null)
            {
                retVal = new List<ModView>(this.m_views);
            }

            return retVal;
        }

        /// <summary>Checks a ModContainer's template structure.</summary>
        public static bool HasValidTemplate(ModContainer container, out string helpMessage)
        {
            helpMessage = null;
            bool isValid = true;

            ModView itemTemplate = null;

            // null check
            if(container.containerTemplate == null)
            {
                helpMessage = ("Invalid template:" + " The container template is unassigned.");
                isValid = false;
            }
            // containerTemplate is child of Component
            else if(!container.containerTemplate.IsChildOf(container.transform)
                    || container.containerTemplate == container.transform)
            {
                helpMessage = ("Invalid template:"
                               + " The container template must be a child of this object.");
                isValid = false;
            }
            // ModView is found under containerTemplate
            else if((itemTemplate =
                         container.containerTemplate.gameObject.GetComponentInChildren<ModView>())
                    == null)
            {
                helpMessage =
                    ("Invalid template:"
                     + " No ModView component found in the children of the container template.");
                isValid = false;
            }
            // ModView is on same gameObject as containerTemplate
            else if(itemTemplate.transform == container.containerTemplate)
            {
                helpMessage =
                    ("Invalid template:"
                     + " The ModView component cannot share a GameObject with the container template.");
                isValid = false;
            }

            return isValid;
        }
    }
}
