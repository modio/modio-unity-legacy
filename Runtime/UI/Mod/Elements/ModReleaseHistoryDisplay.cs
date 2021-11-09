using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Displays the release history of a mod.</summary>
    [RequireComponent(typeof(ModfileContainer))]
    public class ModReleaseHistoryDisplay : MonoBehaviour, IModViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>Display in reverse chronological order?</summary>
        [Tooltip("Enabling this will display the modfiles in reverse chronological order.")]
        public bool reverseChronological = true;

        /// <summary>Parent ModView.</summary>
        private ModView m_view = null;

        /// <summary>Id of the mod release history being displayed.</summary>
        private int m_modId = ModProfile.NULL_ID;

        /// <summary>Id of the mod release history currently being requested.</summary>
        private int m_requestedModId = ModProfile.NULL_ID;

        // --- Accessors ---
        /// <summary>Accessor for the modfile container.</summary>
        public ModfileContainer container
        {
            get {
                return this.gameObject.GetComponent<ModfileContainer>();
            }
        }

        // ---------[ INITIALIZATION ]---------
        protected virtual void OnEnable()
        {
            this.RequestReleaseHistory(this.m_modId);
        }

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
                this.m_view.onProfileChanged.RemoveListener(RequestReleaseHistory);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onProfileChanged.AddListener(RequestReleaseHistory);
                this.RequestReleaseHistory(this.m_view.profile);
            }
            else
            {
                this.RequestReleaseHistory(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Requests the release history for a mod.</summary>
        public void RequestReleaseHistory(ModProfile modProfile)
        {
            int modId = ModProfile.NULL_ID;

            if(modProfile != null)
            {
                modId = modProfile.id;
            }

            this.RequestReleaseHistory(modId);
        }

        /// <summary>Requests the release history for a mod.</summary>
        public void RequestReleaseHistory(int modId)
        {
            this.m_modId = modId;

            if(this.isActiveAndEnabled && modId != this.m_requestedModId)
            {
                this.m_requestedModId = modId;

                // set item count
                int itemCount = this.container.itemLimit;
                if(itemCount < 0)
                {
                    itemCount = APIPaginationParameters.LIMIT_MAX;
                }

                container.DisplayModfiles(null);

                // pagination
                var pagination = new APIPaginationParameters() {
                    offset = 0,
                    limit = itemCount,
                };

                // filter
                RequestFilter filter = new RequestFilter() {
                    sortFieldName = ModIO.API.GetAllModfilesFilterFields.dateAdded,
                    isSortAscending = !this.reverseChronological,
                };

                // fetch
                APIClient.GetAllModfiles(modId, filter, pagination, (r) => {
                    if(this != null && modId == this.m_modId)
                    {
                        this.container.DisplayModfiles(r.items);
                    }
                }, null);
            }
        }
    }
}
