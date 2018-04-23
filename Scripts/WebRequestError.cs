using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    [System.Serializable]
    public class WebRequestError
    {
        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private string _method;
        [SerializeField] private string _url;
        [SerializeField] private string _message;
        [SerializeField] private int _responseCode;

        // ---------[ FIELDS ]---------
        public string method    { get { return this._method; } }
        public string url       { get { return this._url; } }
        public string message   { get { return this._message; } }
        public int responseCode { get { return this._responseCode; } }

        // ---------[ INITIALIZATION ]---------
        public static WebRequestError GenerateFromWebRequest(UnityEngine.Networking.UnityWebRequest webRequest)
        {
            UnityEngine.Debug.Assert(webRequest.isNetworkError || webRequest.isHttpError);
            
            var retVal = new WebRequestError();
            retVal._method = webRequest.method.ToUpper();
            retVal._url = webRequest.url;

            if(webRequest.isNetworkError
               || webRequest.responseCode == 404)
            {
                retVal._responseCode = (int)webRequest.responseCode;
                retVal._message = webRequest.error;
            }
            else // if(webRequest.isHttpError)
            {
                API.ErrorObject error;
                if(Utility.TryParseJsonString(webRequest.downloadHandler.text,
                                              out error))
                {
                    retVal._responseCode = error.error.code;
                    retVal._message = error.error.message;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Failed to parse error from reponse:\n"
                                                 + "[" + webRequest.responseCode + "] "
                                                 + webRequest.downloadHandler.text);
                }
            }

            return retVal;
        }
    }
}
