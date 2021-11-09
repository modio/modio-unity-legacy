using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays a collection of modfiles.</summary>
    public class ModfileContainer : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Template to duplicate for the purpose of displaying the gallery
        /// images.</summary>
        public RectTransform containerTemplate = null;

        /// <summary>Should the template be disabled if empty?</summary>
        public bool hideIfEmpty = false;

        /// <summary>Limit of mod views that can be displayed in this container.</summary>
        [SerializeField]
        private int m_itemLimit = -1;

        /// <summary>Event triggered when the item limit is changed.</summary>
        public event System.Action<int> onItemLimitChanged = null;

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

                    this.DisplayModfiles(this.m_modfiles);

                    if(this.onItemLimitChanged != null)
                    {
                        this.onItemLimitChanged.Invoke(this.m_itemLimit);
                    }
                }
            }
        }

        /// <summary>Profiles currently being displayed.</summary>
        public Modfile[] modfiles
        {
            get {
                return this.m_modfiles;
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
            if(!ModfileContainer.HasValidTemplate(this, out message))
            {
                Debug.LogError("[mod.io] " + message, this);
                return;
            }
#endif

            // get template vars
            Transform templateParent = this.containerTemplate.parent;
            string templateInstance_name = this.containerTemplate.gameObject.name + " (Instance)";
            int templateInstance_index = this.containerTemplate.GetSiblingIndex() + 1;
            this.m_itemTemplate = this.containerTemplate.GetComponentInChildren<ModfileView>(true);

            // duplication protection
            bool isInstantiated =
                (templateParent.childCount > templateInstance_index
                 && templateParent.GetChild(templateInstance_index).gameObject.name
                        == templateInstance_name);
            if(isInstantiated)
            {
                this.m_templateClone = templateParent.GetChild(templateInstance_index).gameObject;
                ModfileView[] viewInstances =
                    this.m_templateClone.GetComponentsInChildren<ModfileView>(true);

                if(viewInstances == null || viewInstances.Length == 0)
                {
                    isInstantiated = false;
                    GameObject.Destroy(this.m_templateClone);
                }
                else
                {
                    this.m_container = (RectTransform)viewInstances[0].transform.parent;

                    foreach(ModfileView view in viewInstances)
                    {
                        GameObject.Destroy(view.gameObject);
                    }
                }
            }

            if(!isInstantiated)
            {
                this.m_templateClone =
                    GameObject.Instantiate(this.containerTemplate.gameObject, templateParent);
                this.m_templateClone.transform.SetSiblingIndex(templateInstance_index);
                this.m_templateClone.name = templateInstance_name;

                ModfileView viewInstance =
                    this.m_templateClone.GetComponentInChildren<ModfileView>(true);
                this.m_container = (RectTransform)viewInstance.transform.parent;
                GameObject.Destroy(viewInstance.gameObject);

                this.m_templateClone.SetActive(true);
            }

            this.DisplayModfiles(this.m_modfiles);
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
                for(int i = 0; i < modfileCount; ++i) { this.m_modfiles[i] = modfiles[i]; }
            }

            // display
            if(this.m_itemTemplate != null)
            {
                // set instance count
                int itemCount = this.m_modfiles.Length;
                ;
                if(this.m_itemLimit >= 0 && this.m_itemLimit < itemCount)
                {
                    itemCount = this.m_itemLimit;
                }

                // set view count
                UIUtilities.SetInstanceCount(this.m_container, this.m_itemTemplate, "Modfile View",
                                             itemCount, ref this.m_views);

                // display data
                for(int i = 0; i < itemCount; ++i) { this.m_views[i].modfile = this.m_modfiles[i]; }

                // hide if necessary
                this.m_templateClone.SetActive(itemCount > 0 || !this.hideIfEmpty);
            }
        }

        // ---------[ UTILITY ]---------
        /// <summary>Checks a ModfileContainer's template structure.</summary>
        public static bool HasValidTemplate(ModfileContainer container, out string helpMessage)
        {
            helpMessage = null;
            bool isValid = true;

            ModfileView itemTemplate = null;

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
            // ModfileView is found under containerTemplate
            else if((itemTemplate = container.containerTemplate.gameObject
                                        .GetComponentInChildren<ModfileView>())
                    == null)
            {
                helpMessage =
                    ("Invalid template:"
                     + " No ModfileView component found in the children of the container template.");
                isValid = false;
            }
            // ModfileView is on same gameObject as containerTemplate
            else if(itemTemplate.transform == container.containerTemplate)
            {
                helpMessage =
                    ("Invalid template:"
                     + " The ModfileView component cannot share a GameObject with the container template.");
                isValid = false;
            }

            return isValid;
        }
    }
}
