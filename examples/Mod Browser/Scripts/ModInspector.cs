using System;

using UnityEngine;
using UnityEngine.UI;

using ModIO;

public class ModInspector : MonoBehaviour
{
    public event Action downloadClicked;
    public event Action installClicked;
    public event Action subscribeClicked;

    public Text title;
    public Text author;
    public Image logo;
    public Button downloadButton;
    public Text downloadButtonText;
    public Button installButton;
    public Text installButtonText;
    public Button subscribeButton;
    public Text subscribeButtonText;

    public void NotifyDownloadClicked()
    {
        if(this.downloadClicked != null)
        {
            this.downloadClicked();
        }
    }

    public void NotifyInstallClicked()
    {
        if(this.installClicked != null)
        {
            this.installClicked();
        }
    }

    public void NotifySubscribeClicked()
    {
        if(this.subscribeClicked != null)
        {
            this.subscribeClicked();
        }
    }
}
