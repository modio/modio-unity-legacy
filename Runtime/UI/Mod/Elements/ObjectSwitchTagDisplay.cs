using System.Collections.Generic;

using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Enables objects based on matched tags.</summary>
    public class ObjectSwitchTagDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Data structure for pairing tags with GameObjects.</summary>
        [System.Serializable]
        public struct TagObjectPair
        {
            public string tagName;
            public GameObject gameObject;
        }

        // ---------[ FIELDS ]---------
        /// <summary>Tag-GameObject pairings.</summary>
        public List<TagObjectPair> tagObjectPairs = new List<TagObjectPair>();

        /// <summary>Parent Mod View.</summary>
        private ModView m_view = null;

        // ---------[ INTIALIZATION ]---------
        /// <summary>IModViewElement interface.</summary>
        public void SetModView(ModView view)
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

            // hook
            if(view != null)
            {
                view.onProfileChanged.AddListener(DisplayProfile);
            }

            // finalize
            this.m_view = view;
            this.DisplayProfile(this.m_view.profile);
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays tags of a profile.</summary>
        public void DisplayProfile(ModProfile profile)
        {
            if(profile == null)
            {
                this.HideAll();
            }
            else
            {
                List<string> tagNames = new List<string>(profile.tagNames);
                this.DisplayTags(tagNames);
            }
        }

        /// <summary>Sets object visibility based on the tags.</summary>
        public void DisplayTags(IList<string> tagNames)
        {
            if(tagNames == null)
            {
                this.HideAll();
            }
            else
            {
                foreach(TagObjectPair pair in this.tagObjectPairs)
                {
                    if(pair.gameObject != null)
                    {
                        bool isFound = tagNames.Contains(pair.tagName);
                        pair.gameObject.SetActive(isFound);
                    }
                }
            }
        }

        /// <summary>Hides all pair objects.</summary>
        public void HideAll()
        {
            if(this.tagObjectPairs.Count > 0)
            {
                foreach(TagObjectPair pair in this.tagObjectPairs)
                {
                    if(pair.gameObject != null)
                    {
                        pair.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
