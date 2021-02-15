using UnityEngine;

namespace ModIO.UI
{
    public class RefreshButton : MonoBehaviour
    {
        // ---------[ Fields ]---------
        public RectTransformSpinner spinner = null;

        private bool m_isUpdating = false;

        // ---------[ Initialization ]---------
        private void OnEnable()
        {
            this.UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            this.GetComponent<UnityEngine.UI.Button>().interactable = !this.m_isUpdating;

            if(this.spinner != null)
            {
                this.spinner.transform.localRotation = Quaternion.identity;
                this.spinner.enabled = this.m_isUpdating;
            }
        }

        // ---------[ Events ]---------
        public void StartUpdate()
        {
            this.m_isUpdating = true;
            this.UpdateDisplay();

            this.StartCoroutine(ModBrowser.instance.UpdateSubscriptions(this.OnUpdateComplete));
        }

        private void OnUpdateComplete()
        {
            if(this != null)
            {
                this.m_isUpdating = false;
                this.UpdateDisplay();
            }
        }
    }
}
