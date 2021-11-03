using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Component used to display a field of a FileDownloadInfo in text.</summary>
    public class DownloadFieldDisplay : MonoBehaviour, IDownloadViewElement
    {
        // ---------[ FIELDS ]---------
        /// <summary>DownloadInfo field to display.</summary>
        [MemberReference.DropdownDisplay(typeof(FileDownloadInfo), displayEnumerables = false,
                                         displayNested = true,
                                         membersToIgnore = new string[] { "error.webRequest" })]
        public MemberReference reference = new MemberReference("bytesPerSecond");

        /// <summary>Formatting to apply to the object value.</summary>
        public ValueFormatting formatting = new ValueFormatting();

        /// <summary>Wrapper for the text component.</summary>
        private GenericTextComponent m_textComponent = new GenericTextComponent();

        /// <summary>Parent DownloadView.</summary>
        private DownloadView m_view = null;

        /// <summary>Currently displayed DownloadInfo object.</summary>
        private FileDownloadInfo m_downloadInfo = null;

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            Component textDisplayComponent =
                GenericTextComponent.FindCompatibleTextComponent(this.gameObject);
            this.m_textComponent.SetTextDisplayComponent(textDisplayComponent);

#if DEBUG
            if(textDisplayComponent == null)
            {
                Debug.LogWarning("[mod.io] No compatible text components were found on this "
                                     + "GameObject to set text for."
                                     + "\nCompatible with any component that exposes a"
                                     + " publicly settable \'.text\' property.",
                                 this);
            }
#endif
        }

        protected virtual void OnEnable()
        {
            this.DisplayDownload(this.m_downloadInfo);
        }

        /// <summary>IDownloadViewElement interface.</summary>
        public void SetDownloadView(DownloadView view)
        {
            // early out
            if(this.m_view == view)
            {
                return;
            }

            // unhook
            if(this.m_view != null)
            {
                this.m_view.onDownloadInfoUpdated.RemoveListener(DisplayDownload);
            }

            // assign
            this.m_view = view;

            // hook
            if(this.m_view != null)
            {
                this.m_view.onDownloadInfoUpdated.AddListener(DisplayDownload);
                this.DisplayDownload(this.m_view.downloadInfo);
            }
            else
            {
                this.DisplayDownload(null);
            }
        }

        // ---------[ UI FUNCTIONALITY ]---------
        /// <summary>Displays the appropriate field of a given download.</summary>
        public void DisplayDownload(FileDownloadInfo downloadInfo)
        {
            this.m_downloadInfo = downloadInfo;

            // display
            object fieldValue = this.reference.GetValue(this.m_downloadInfo);
            string displayString = ValueFormatting.FormatValue(fieldValue, this.formatting.method,
                                                               this.formatting.toStringParameter);

            this.m_textComponent.text = displayString;
        }
    }
}
