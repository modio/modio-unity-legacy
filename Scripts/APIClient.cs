#define LOG_QUERIES

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public delegate void ErrorCallback(string error);
    public delegate void ObjectCallback<T>(T requestedObject);
    public delegate void ObjectArrayCallback<T>(T[] objectArray);
    public delegate void DownloadCallback(byte[] data);

    public class APIClient : MonoBehaviour
    {
        // ---------[ CONSTANTS ]---------
        public const string VERSION = "v1";
        public const string URL = "https://api.mod.io/" + VERSION + "/";

        // ---------[ INTERNAL CLASSES ]---------
        [System.Serializable]
        private class JSONObjectArray<T>
        {
            public T[] data = null;
            public int cursor_id = -1;
            public int prev_id = -1;
            public int next_id = -1;
            public int result_count = 0;
        };

        public int gameID = 0;
        public string apiKey = "";
        // public string OAUTH_TOKEN = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImp0aSI6ImMxMDZhZTQ5OWE5ZjMwZGM2ZjJiYzE2Yjc5YjlmNDk5MDYzOWVlNjFkMGQxNzYyNWRlNjcwZDlhMGFjZjhhOWM2NzJiZDI5ZDM4ZjM3ODNiIn0.eyJhdWQiOiI4IiwianRpIjoiYzEwNmFlNDk5YTlmMzBkYzZmMmJjMTZiNzliOWY0OTkwNjM5ZWU2MWQwZDE3NjI1ZGU2NzBkOWEwYWNmOGE5YzY3MmJkMjlkMzhmMzc4M2IiLCJpYXQiOjE1MDgzMDg4MTMsIm5iZiI6MTUwODMwODgxMywiZXhwIjoxNTM5ODQ0ODEzLCJzdWIiOiIzMTM0NCIsInNjb3BlcyI6WyJyZWFkIiwid3JpdGUiXX0.HVy5AHZOsAZX1BSRtI6oqTqsV2XLjlMoJIQtJqG5fRTDi6K-miA9vR_0FZrPs9Y7qYjWUYEE7Dcg8li7dPho774T9OdNVn3MkPRiEigbpXa-ZMaMuxxSFRxmCsNpYleHAwSaAQD0x03M4t_PkFEvTlc-FPO5l5PXx_qtw1Uo8oa-2wtWOspxXDs4AbP0TNJ_3p7rdVs5Lt9bxEKAJcMpf3G7gRG13gHOOGeC44Jm3k1Yvj8_j3qy-Bb64hnDZuTaNYV5nq0YRQ3Qp-SpD1zVl67eUGidD8t1vBdltRDCHXtvu0HTJZPKRVo7nbEqb_ZcPQ_f7Yw5nO4wlgmkolZS6bhn-v8rkf2LP-mZGJLF6AQwQ7zj2xvmwB2yquYpYzTX9vUS5n-43vjr8M6qqZ6cSPM4zs0NzgmgJKGKsx_BzHnXgWMgYdfxOvgN-q-1NLhatFIS44qV285EVoLF66ZPJboaeT4hXP5Kyj4j4BrqLMEJQab_rxyIkv8mtXESSez9PLKPxnBXgkNpHtguAf42MYpHmHhO78zZ45MkeZ5KTBH7ku_w0vhHhAgePsSoX49YJOjhjXUcrnmmnjO7Rav3zatflKekbioOJ8oBXI4kKK_LX3PgSpLvBqaZDfEmus-AuazrGYFY3rbCQS5WzVtTtvv4CKjuQhgH0rCoPu-TA24";

        // "cursor_id":null,"prev_id":null,"next_id":120,"result_count":3

        public static void OnAPIRequestError(string errorMessage)
        {
            Debug.LogWarning(errorMessage);
        }

        // ---------[ CORE FUNCTIONS ]---------
        public static IEnumerator LogQuery(string query)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(URL + query);
            yield return webRequest.Send();
            
            if(webRequest.isNetworkError)
            {
                Debug.LogError("API QUERY FAILED"
                               + "\nQuery: " + URL + query
                               + "\nResponse: " + webRequest.error
                               + "\n");
            }
            else
            {
                Debug.Log("API QUERY SUCCEEDED"
                          + "\nQuery: " + URL + query
                          + "\nResponse: " + webRequest.downloadHandler.text
                          + "\n");
            }
        }

        public static IEnumerator RequestObject<T>(string query,
                                                   ObjectCallback<T> onSuccess,
                                                   ErrorCallback onError)
        {
            #if LOG_QUERIES
            Debug.Log("REQUESTING JSON OBJECT"
                      + "\nQuery: " + URL + query);
            #endif

            UnityWebRequest webRequest = UnityWebRequest.Get(URL + query);
            yield return webRequest.Send();
            
            if(webRequest.isNetworkError)
            {
                #if LOG_QUERIES
                Debug.LogError("API QUERY FAILED"
                               + "\nQuery: " + URL + query
                               + "\nResponse: " + webRequest.error
                               + "\n");
                #endif

                onError(webRequest.error);
            }
            else
            {
                #if LOG_QUERIES
                Debug.Log("JSON OBJECT RECEIVED"
                          + "\nQuery: " + URL + query
                          + "\nResponse: " + webRequest.downloadHandler.text
                          + "\n");
                #endif

                // string response_string = webRequest.downloadHandler.text;
                // int indexOfData = response_string.IndexOf("data:");
                // if(indexOfData > 0)
                // {
                //     string preData = response_string.Substring(0, response_string.Length - indexOfData);
                //     string postData = response_string.Substring(indexOfData + 5);

                //     response_string = preData + postData;
                // }
                // T response = JsonUtility.FromJson<T>(response_string);

                T response = JsonUtility.FromJson<T>(webRequest.downloadHandler.text);
                onSuccess(response);
            }
        }

        public static IEnumerator RequestObjectArray<T>(string query,
                                                        ObjectArrayCallback<T> onSuccess,
                                                        ErrorCallback onError)
        {
            #if LOG_QUERIES
            Debug.Log("REQUESTING JSON OBJECT"
                      + "\nQuery: " + URL + query);
            #endif

            UnityWebRequest webRequest = UnityWebRequest.Get(URL + query);
            yield return webRequest.Send();
            
            if(webRequest.isNetworkError)
            {
                #if LOG_QUERIES
                Debug.LogError("API QUERY FAILED"
                               + "\nQuery: " + URL + query
                               + "\nResponse: " + webRequest.error
                               + "\n");
                #endif

                onError(webRequest.error);
            }
            else
            {
                #if LOG_QUERIES
                Debug.Log("JSON OBJECT RECEIVED"
                          + "\nQuery: " + URL + query
                          + "\nResponse: " + webRequest.downloadHandler.text
                          + "\n");
                #endif

                // string response_string = webRequest.downloadHandler.text;
                // int indexOfData = response_string.IndexOf("data:");
                // if(indexOfData > 0)
                // {
                //     string preData = response_string.Substring(0, response_string.Length - indexOfData);
                //     string postData = response_string.Substring(indexOfData + 5);

                //     response_string = preData + postData;
                // }
                // T response = JsonUtility.FromJson<T>(response_string);

                JSONObjectArray<T> response = JsonUtility.FromJson<JSONObjectArray<T>>(webRequest.downloadHandler.text);
                onSuccess(response.data);
            }
        }

        public static IEnumerator RequestFileData(string url,
                                                  DownloadCallback onSuccess,
                                                  ErrorCallback onError)
        {
            #if LOG_QUERIES
            Debug.Log("REQUESTING FILE DOWNLOAD"
                      + "\nSourceURI: " + url);
            #endif

            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.Send();
            
            if(webRequest.isNetworkError)
            {
                onError(webRequest.error);
            }
            else
            {
                #if LOG_QUERIES
                Debug.Log("FILE DOWNLOADED SUCCESSFULLY"
                          + "\nSourceURI: " + url);
                #endif

                byte[] downloadedData = webRequest.downloadHandler.data;
                onSuccess(downloadedData);
            }
        }

        // ---------[ GET ENDPOINTS ]---------
        // View Game
        public void ViewGame(ObjectCallback<Game> callback)
        {
            string query = "games/" + gameID + "?api_key=" + apiKey;
            
            StartCoroutine(RequestObject<Game>(query,
                                               callback,
                                               OnAPIRequestError));
        }

        // Browse Mods
        public void BrowseMods(ModQueryFilter filter, ObjectArrayCallback<Mod> callback)
        {
            string query = "games/" + gameID + "/mods?";
            query += filter.GenerateQueryString();
            query += "&api_key=" + apiKey;

            StartCoroutine(APIClient.RequestObjectArray<Mod>(query,
                                                             callback,
                                                             OnAPIRequestError));
        }
        // View Mod
        public void ViewMod(int modID, ObjectCallback<Mod> callback)
        {
            string query = "games/" + gameID + "/mods/" + modID + "?api_key=" + apiKey;
            
            StartCoroutine(RequestObject<Mod>(query,
                                              callback,
                                              OnAPIRequestError));
        }

        // Browse Mod Activity
        public void BrowseModActivity(int modID, ModQueryFilter filter,
                                      ObjectArrayCallback<ModActivity> callback)
        {
            string query = "games/" + gameID + "/mods/" + modID + "/activity?";
            query += filter.GenerateQueryString();
            query += "&api_key=" + apiKey;

            StartCoroutine(APIClient.RequestObjectArray<ModActivity>(query,
                                                                     callback,
                                                                     OnAPIRequestError));
        }

        // Browse Mod Files
        public void BrowseModFiles(int modID, ModQueryFilter filter,
                                   ObjectArrayCallback<ModFile> callback)
        {
            string query = "games/" + gameID + "/mods/" + modID + "/files";
            query += filter.GenerateQueryString();
            query += "&api_key=" + apiKey;

            StartCoroutine(APIClient.RequestObjectArray<ModFile>(query,
                                                                 callback,
                                                                 OnAPIRequestError));
        }

        // Browse Mod Tags
        // public void BrowseModTags(ObjectArrayCallback<T)

        // Browse Mod Comments
        // View Mod Comment
        
        // Browse Mod Team Members


        // ---------[ PUSH ENDPOINTS ]---------
        // Add Mod
        // Edit Mod
        // Delete Mod
        // Add Mod Media
        // Delete Mod Media

        // Add Mod File
        // View Mod File
        // Edit Mod File

        // Add Mod Tag
        // Delete Mod Tag
        // Add Mod Rating
        // Delete Mod Comment
        // Add Mod Team Member
        // Update Mod Team Member
        // Delete Mod Team Member
    }
}
