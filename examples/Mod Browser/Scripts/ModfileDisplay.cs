using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class ModfileDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    public delegate void OnClickDelegate(ModfileDisplay display, int modfileId);
    public event OnClickDelegate onClick;

    [Header("UI Components")]
    public Text dateAddedDisplay;
    public Text fileNameDisplay;
    public Text fileSizeDisplay;
    public Text fileHashDisplay;
    public Text versionDisplay;
    public Text changelogDisplay;

    [Header("Display Data")]
    [SerializeField] private int m_modfileId = -1;

    // --- RUNTIME DATA ---
    private delegate string GetDisplayString(Modfile modfile);

    private Dictionary<Text, GetDisplayString> m_displayMapping = null;
    private List<LoadingDisplay> m_loadingDisplays = null;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {
        m_displayMapping = new Dictionary<Text, GetDisplayString>();

        if(dateAddedDisplay != null)
        {
            m_displayMapping.Add(dateAddedDisplay, (m) => ServerTimeStamp.ToLocalDateTime(m.dateAdded).ToString());
        }
        if(fileNameDisplay != null)
        {
            m_displayMapping.Add(fileNameDisplay, (m) => m.fileName);
        }
        if(fileSizeDisplay != null)
        {
            m_displayMapping.Add(fileSizeDisplay, (m) => ModBrowser.ByteCountToDisplayString(m.fileSize));
        }
        if(fileHashDisplay != null)
        {
            m_displayMapping.Add(fileHashDisplay, (m) => m.fileHash.md5);
        }
        if(versionDisplay != null)
        {
            m_displayMapping.Add(versionDisplay, (m) => m.version);
        }
        if(changelogDisplay != null)
        {
            m_displayMapping.Add(changelogDisplay, (m) => m.changelog);
        }

        m_loadingDisplays = new List<LoadingDisplay>();
        foreach(Text textDisplay in m_displayMapping.Keys)
        {
            m_loadingDisplays.AddRange(textDisplay.gameObject.GetComponentsInChildren<LoadingDisplay>(true));
        }
    }

    // ---------[ UI FUNCTIONALITY ]---------
    public void DisplayModfile(Modfile modfile)
    {
        Debug.Assert(modfile != null);

        m_modfileId = modfile.id;

        foreach(LoadingDisplay loadingDisplay in m_loadingDisplays)
        {
            loadingDisplay.gameObject.SetActive(false);
        }
        foreach(var kvp in m_displayMapping)
        {
            kvp.Key.text = kvp.Value(modfile);
            kvp.Key.enabled = true;
        }
    }

    public void DisplayLoading(int modfileId = -1)
    {
        m_modfileId = modfileId;

        foreach(LoadingDisplay loadingDisplay in m_loadingDisplays)
        {
            loadingDisplay.gameObject.SetActive(true);
        }
        foreach(Text textComponent in m_displayMapping.Keys)
        {
            textComponent.enabled = false;
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
