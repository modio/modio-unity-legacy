// PROCESS:
// 1) Player clicks subscribe
// 2) ModBrowser gets event
// 3) ModBrowser add the id to the subscription ids.
// 4) ModBrowser initiates the binary download.
// ?)
// X) DownloadProgressDisplay starts Coroutine
//
// Problems:
//  - Coroutines cannot Start if component is disabled
//  - How to detect which components need to receive the message?
//  - Sending through the SubView seems superfluous? Why not directly from ModB?
//  - What about through mod pages? Through ModProfileDisplays?

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ModIO;

public class DownloadProgressDisplay : MonoBehaviour
{
    // ---------[ FIELDS ]---------
    [Header("UI Elements")]
    // TODO(@jackson): You know what to do...
    public Text percentageText;
    public Text byteCountText;
    public RectTransform progressBar;
    public GameObject container;

    // ---------[ INITIALIZATION ]---------
    public void Initialize()
    {

    }

    private void OnEnable()
    {
        // if(m_uwr != null)
        // {
        //     // Start Coroutine
        // }
    }
    private void OnDisable()
    {
        // stop coroutine
    }

    // ---------[ UI FUNCTIONALITY ]---------
    // public void DisplayModfile(int modId, int modfileId)
    // {
    //     ModBinaryRequest request = ModManager.GetDownloadInProgress(modfileId);
    //     DisplayDownload(request);

    //     if(request == null)
    //     {
    //         this.m_modId = modId;
    //         this.m_modfileId = modfileId;
    //     }
    // }

    // public void DisplayDownload(ModBinaryRequest request)
    // {
    //     if(request == null
    //        || request.isDone)
    //     {
    //         m_modId = -1;
    //         m_modfileId = -1;
    //         m_uwr = null;

    //         if(container != null)
    //         {
    //             container.gameObject.SetActive(false);
    //         }
    //     }
    //     else
    //     {
    //         m_modId = request.

    //         if(container != null)
    //         {
    //             container.gameObject.SetActive(true);
    //         }
    //         StartCoroutine(UpdateDownloadProgressElements(request));
    //     }
    // }

    public IEnumerator UpdateDisplayForRequestCoroutine(ModBinaryRequest request)
    {
        if(container != null)
        {
            container.gameObject.SetActive(true);
        }

        RectTransform barTransform = null;
        if(progressBar != null)
        {
            barTransform = progressBar.GetChild(0) as RectTransform;
            barTransform.sizeDelta = new Vector2(0f, 0f);
        }
        if(byteCountText != null)
        {
            byteCountText.text = ModBrowser.ByteCountToDisplayString(0);
        }
        if(percentageText != null)
        {
            percentageText.text = "0%";
        }

        while(!request.isDone)
        {
            if(request.webRequest != null)
            {
                float percentComplete = request.webRequest.downloadProgress;

                if(progressBar != null)
                {
                    float barWidth = percentComplete * progressBar.rect.width;
                    barTransform.sizeDelta = new Vector2(barWidth, 0f);
                }

                if(byteCountText != null)
                {
                    byteCountText.text = ModBrowser.ByteCountToDisplayString((Int64)request.webRequest.downloadedBytes);
                }

                if(percentageText != null)
                {
                    percentageText.text = (percentComplete * 100f).ToString("0.0") + "%";
                }
            }

            yield return null;
        }

        if(progressBar != null)
        {
            barTransform.sizeDelta = new Vector2(progressBar.rect.width, 0f);
        }

        if(byteCountText != null)
        {

            try
            {
                var info = new System.IO.FileInfo(request.binaryFilePath);
                string byteCountString = ModBrowser.ByteCountToDisplayString(info.Length);
                byteCountText.text = byteCountString;
            }
            catch(Exception e)
            {
                Debug.LogError(Utility.GenerateExceptionDebugString(e));
            }
        }

        if(percentageText != null)
        {
            percentageText.text = "100%";
        }

        yield return new WaitForSeconds(4f);

        if(container != null)
        {
            container.gameObject.SetActive(false);
        }
    }
}
