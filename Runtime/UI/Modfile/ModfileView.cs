using UnityEngine;

namespace ModIO.UI
{
    /// <summary>A view that provides information to child IModfileViewElements.</summary>
    [DisallowMultipleComponent]
    public class ModfileView : MonoBehaviour
    {
        // ---------[ NESTED DATA-TYPES ]---------
        /// <summary>Event for notifying listeners of a change to the modfile.</summary>
        [System.Serializable]
        public class ModfileChangedEvent : UnityEngine.Events.UnityEvent<Modfile>
        {
        }

        // ---------[ FIELDS ]---------
        /// <summary>Currently displayed modfile.</summary>
        [SerializeField]
        private Modfile m_modfile = null;

        /// <summary>Text to use in place of an empty changelog.</summary>
        public string emptyChangelogText = string.Empty;

        /// <summary>Event fired when the modfile changes.</summary>
        public ModfileChangedEvent onModfileChanged = null;

        // --- Accessors ---
        /// <summary>Currently displayed modfile.</summary>
        public Modfile modfile
        {
            get {
                return this.m_modfile;
            }
            set {
                if(this.m_modfile != value)
                {
                    this.m_modfile = value;

                    if(this.m_modfile != null && string.IsNullOrEmpty(this.m_modfile.changelog))
                    {
                        this.m_modfile.changelog = this.emptyChangelogText;
                    }

                    if(this.onModfileChanged != null)
                    {
                        this.onModfileChanged.Invoke(this.m_modfile);
                    }
                }
            }
        }

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
#if DEBUG
            ModfileView nested = this.gameObject.GetComponentInChildren<ModfileView>(true);
            if(nested != null && nested != this)
            {
                Debug.LogError(
                    "[mod.io] Nesting ModfileViews is currently not supported due to the"
                        + " way IModfileViewElement component parenting works."
                        + "\nThe nested ModfileViews must be removed to allow ModfileView functionality."
                        + "\nthis=" + this.gameObject.name + "\nnested=" + nested.gameObject.name,
                    this);
                return;
            }
#endif

            // assign modfile view elements to this
            var modfileViewElements =
                this.gameObject.GetComponentsInChildren<IModfileViewElement>(true);
            foreach(IModfileViewElement viewElement in modfileViewElements)
            {
                viewElement.SetModfileView(this);
            }
        }
    }
}
