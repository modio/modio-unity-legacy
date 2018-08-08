using UnityEngine;
using UnityEngine.UI;

using ModIO;

public class ModThumbnailContainer : MonoBehaviour
{
    // TODO(@jackson): rows/columns
    public int[] modIds;
    public Image[] modThumbnails;

    public event System.Action<int> thumbnailClicked;

    public void NotifyClick(int index)
    {
        if(thumbnailClicked != null)
        {
            thumbnailClicked(index);
        }
    }
}
