using UnityEngine;

namespace ModIO.UI
{
    public class WebLinkFollower : MonoBehaviour
    {
        public void OpenBrowserAt(string url)
        {
#if STEAM_VR
            Valve.VR.OpenVR.Overlay.ShowDashboard("valve.steam.desktop");      
#endif
            Application.OpenURL(url);
        }
    }
}
