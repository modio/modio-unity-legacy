using UnityEngine;

namespace ModIO.UI
{
    public class WebLinkFollower : MonoBehaviour
    {
        public void OpenBrowserAt(string url)
        {
            Application.OpenURL(url);
        }
    }
}
