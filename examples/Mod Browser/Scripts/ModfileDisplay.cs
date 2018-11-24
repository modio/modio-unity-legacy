using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModfileDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public delegate void OnClickDelegate(ModfileDisplay display, int modfileId);
    public event OnClickDelegate onClick;

    [Header("Settings")]
    public GameObject textLoadingPrefab;

    [Header("UI Components")]
    public Text dateAddedDisplay;
    public Text fileNameDisplay;
    public Text fileSizeDisplay;
    public Text fileHashDisplay;
    public Text versionDisplay;
    public Text changelogDisplay;

    [Header("Display Data")]
    [SerializeField] private int m_modfileId = -1;
    private List<GameObject> m_loadingInstances = null;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        if(textLoadingPrefab != null)
        {
            m_loadingInstances = new List<GameObject>();

            if(dateAddedDisplay != null)
            {
                GameObject loadingGO = InstantiateTextLoadingPrefab(dateAddedDisplay.GetComponent<RectTransform>());
                loadingGO.SetActive(false);
                m_loadingInstances.Add(loadingGO);
            }
            if(fileNameDisplay != null)
            {
                GameObject loadingGO = InstantiateTextLoadingPrefab(fileNameDisplay.GetComponent<RectTransform>());
                loadingGO.SetActive(false);
                m_loadingInstances.Add(loadingGO);
            }
            if(fileSizeDisplay != null)
            {
                GameObject loadingGO = InstantiateTextLoadingPrefab(fileSizeDisplay.GetComponent<RectTransform>());
                loadingGO.SetActive(false);
                m_loadingInstances.Add(loadingGO);
            }
            if(fileHashDisplay != null)
            {
                GameObject loadingGO = InstantiateTextLoadingPrefab(fileHashDisplay.GetComponent<RectTransform>());
                loadingGO.SetActive(false);
                m_loadingInstances.Add(loadingGO);
            }
            if(versionDisplay != null)
            {
                GameObject loadingGO = InstantiateTextLoadingPrefab(versionDisplay.GetComponent<RectTransform>());
                loadingGO.SetActive(false);
                m_loadingInstances.Add(loadingGO);
            }
            if(changelogDisplay != null)
            {
                GameObject loadingGO = InstantiateTextLoadingPrefab(changelogDisplay.GetComponent<RectTransform>());
                loadingGO.SetActive(false);
                m_loadingInstances.Add(loadingGO);
            }
        }
    }

    private GameObject InstantiateTextLoadingPrefab(RectTransform displayObjectTransform)
    {
        RectTransform parentRT = displayObjectTransform.parent as RectTransform;
        GameObject loadingGO = GameObject.Instantiate(textLoadingPrefab,
                                                      new Vector3(),
                                                      Quaternion.identity,
                                                      parentRT);

        RectTransform loadingRT = loadingGO.transform as RectTransform;
        loadingRT.anchorMin = displayObjectTransform.anchorMin;
        loadingRT.anchorMax = displayObjectTransform.anchorMax;
        loadingRT.offsetMin = displayObjectTransform.offsetMin;
        loadingRT.offsetMax = displayObjectTransform.offsetMax;

        return loadingGO;
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayModfile(Modfile modfile)
    {
        Debug.Assert(modfile != null);

        m_modfileId = modfile.id;

        if(dateAddedDisplay)
        {
            dateAddedDisplay.text = ServerTimeStamp.ToLocalDateTime(modfile.dateAdded).ToString();
            dateAddedDisplay.enabled = true;
        }
        if(fileNameDisplay)
        {
            fileNameDisplay.text = modfile.fileName;
            fileNameDisplay.enabled = true;
        }
        if(fileSizeDisplay)
        {
            fileSizeDisplay.text = ModBrowser.ByteCountToDisplayString(modfile.fileSize);
            fileSizeDisplay.enabled = true;
        }
        if(fileHashDisplay)
        {
            fileHashDisplay.text = modfile.fileHash.md5;
            fileHashDisplay.enabled = true;
        }
        if(versionDisplay)
        {
            versionDisplay.text = modfile.version;
            versionDisplay.enabled = true;
        }
        if(changelogDisplay)
        {
            changelogDisplay.text = modfile.changelog;
            changelogDisplay.enabled = true;
        }

        if(m_loadingInstances != null)
        {
            foreach(GameObject loadingGO in m_loadingInstances)
            {
                loadingGO.SetActive(false);
            }
        }
    }

    public void DisplayLoading()
    {

        if(dateAddedDisplay)
        {
            dateAddedDisplay.enabled = false;
        }
        if(fileNameDisplay)
        {
            fileNameDisplay.enabled = false;
        }
        if(fileSizeDisplay)
        {
            fileSizeDisplay.enabled = false;
        }
        if(fileHashDisplay)
        {
            fileHashDisplay.enabled = false;
        }
        if(versionDisplay)
        {
            versionDisplay.enabled = false;
        }
        if(changelogDisplay)
        {
            changelogDisplay.enabled = false;
        }

        if(m_loadingInstances != null)
        {
            foreach(GameObject loadingGO in m_loadingInstances)
            {
                loadingGO.SetActive(true);
            }
        }
    }

    // ---------[ EVENT HANDLING ]---------
    public void NotifyClicked()
    {
        if(this.onClick != null)
        {
            this.onClick(this, m_modfileId);
        }
    }
}
