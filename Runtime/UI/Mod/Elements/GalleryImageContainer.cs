using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the mod gallery images for a given mod.</summary>
    public class GalleryImageContainer : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Template to duplicate for the purpose of displaying the gallery
        /// images.</summary>
        public RectTransform containerTemplate = null;

        /// <summary>Should the template be disabled if empty?</summary>
        public bool hideIfEmpty = false;

        // --- Run-Time Data ---
        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Instance of the template clone.</summary>
        private GameObject m_templateClone = null;

        /// <summary>Container for the display objects.</summary>
        private RectTransform m_container = null;

        /// <summary>Gallery image display object template.</summary>
        private GalleryImageDisplay m_itemTemplate = null;

        /// <summary>ModId for the currently displayed images.</summary>
        private int m_modId = ModProfile.NULL_ID;

        /// <summary>Gallery Image Locators to display.</summary>
        private GalleryImageLocator[] m_locators = new GalleryImageLocator[0];

        /// <summary>Display objects.</summary>
        private GalleryImageDisplay[] m_displays = new GalleryImageDisplay[0];

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
            if(!GalleryImageContainer.HasValidTemplate(this, out message))
            {
                Debug.LogError("[mod.io] " + message, this);
                return;
            }
#endif

            // get template vars
            Transform templateParent = this.containerTemplate.parent;
            string templateInstance_name = this.containerTemplate.gameObject.name + " (Instance)";
            int templateInstance_index = this.containerTemplate.GetSiblingIndex() + 1;
            this.m_itemTemplate =
                this.containerTemplate.GetComponentInChildren<GalleryImageDisplay>(true);

            // duplication protection
            bool isInstantiated =
                (templateParent.childCount > templateInstance_index
                 && templateParent.GetChild(templateInstance_index).gameObject.name
                        == templateInstance_name);
            if(isInstantiated)
            {
                this.m_templateClone = templateParent.GetChild(templateInstance_index).gameObject;
                GalleryImageDisplay[] itemInstances =
                    this.m_templateClone.GetComponentsInChildren<GalleryImageDisplay>(true);

                if(itemInstances == null || itemInstances.Length == 0)
                {
                    isInstantiated = false;
                    GameObject.Destroy(this.m_templateClone);
                }
                else
                {
                    this.m_container = (RectTransform)itemInstances[0].transform.parent;

                    foreach(GalleryImageDisplay item in itemInstances)
                    {
                        GameObject.Destroy(item.gameObject);
                    }
                }
            }

            if(!isInstantiated)
            {
                this.m_templateClone =
                    GameObject.Instantiate(this.containerTemplate.gameObject, templateParent);
                this.m_templateClone.transform.SetSiblingIndex(templateInstance_index);
                this.m_templateClone.name = templateInstance_name;

                GalleryImageDisplay itemInstance =
                    this.m_templateClone.GetComponentInChildren<GalleryImageDisplay>(true);
                this.m_container = (RectTransform)itemInstance.transform.parent;
                GameObject.Destroy(itemInstance.gameObject);

                this.m_templateClone.SetActive(true);
            }

            this.DisplayImages(this.m_modId, this.m_locators);
        }

        /// <summary>Ensure the displays are accurate.</summary>
        protected virtual void OnEnable()
        {
            this.DisplayImages(this.m_modId, this.m_locators);
        }

        /// <summary>IModViewElement interface.</summary>
        public virtual void SetModView(ModView view)
        {
            // early out
            if(this.m_view == view)
            {
                return;
            }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.RemoveListener(DisplayProfile);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(DisplayProfile);
                this.DisplayProfile(this.m_view.profile);
            }
            else
            {
                this.DisplayProfile(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays gallery images of a profile.</summary>
        public virtual void DisplayProfile(ModProfile profile)
        {
            int modId = ModProfile.NULL_ID;
            GalleryImageLocator[] locators = null;

            if(profile != null && profile.media != null)
            {
                modId = profile.id;
                locators = profile.media.galleryImageLocators;
            }

            this.DisplayImages(modId, locators);
        }

        /// <summary>Displays a set of gallery images.</summary>
        public virtual void DisplayImages(int modId, IList<GalleryImageLocator> locators)
        {
            this.m_modId = modId;

            // copy locators
            if(this.m_locators != locators)
            {
                int imageCount = 0;
                if(locators != null)
                {
                    imageCount = locators.Count;
                }

                this.m_locators = new GalleryImageLocator[imageCount];
                for(int i = 0; i < imageCount; ++i) { this.m_locators[i] = locators[i]; }
            }

            // display
            if(this.m_itemTemplate != null)
            {
                // set view count
                UIUtilities.SetInstanceCount(this.m_container, this.m_itemTemplate, "Gallery Image",
                                             this.m_locators.Length, ref this.m_displays);

                // display data
                for(int i = 0; i < this.m_locators.Length; ++i)
                {
                    this.m_displays[i].DisplayGalleryImage(modId, this.m_locators[i]);
                }

                // hide if necessary
                this.m_templateClone.SetActive(this.m_locators.Length > 0 || !this.hideIfEmpty);
            }
        }

        // ---------[ UTILITY ]---------
        /// <summary>Checks a GalleryImageContainer's template structure.</summary>
        public static bool HasValidTemplate(GalleryImageContainer container, out string helpMessage)
        {
            helpMessage = null;
            bool isValid = true;

            GalleryImageDisplay itemTemplate = null;

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
            // GalleryImageDisplay is found under containerTemplate
            else if((itemTemplate = container.containerTemplate.gameObject
                                        .GetComponentInChildren<GalleryImageDisplay>())
                    == null)
            {
                helpMessage =
                    ("Invalid template:"
                     + " No GalleryImageDisplay component found in the children of the container template.");
                isValid = false;
            }
            // GalleryImageDisplay is on same gameObject as containerTemplate
            else if(itemTemplate.transform == container.containerTemplate)
            {
                helpMessage =
                    ("Invalid template:"
                     + " The GalleryImageDisplay component cannot share a GameObject with the container template.");
                isValid = false;
            }

            return isValid;
        }
    }
}
