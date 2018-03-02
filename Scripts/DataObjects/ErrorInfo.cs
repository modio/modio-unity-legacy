using System;

namespace ModIO
{
    [Serializable]
    public class ErrorInfo : IEquatable<ErrorInfo>
    {
        // - Constructors -
        public static ErrorInfo GenerateFromWebRequest(UnityEngine.Networking.UnityWebRequest webRequest)
        {
            UnityEngine.Debug.Assert(webRequest.isNetworkError || webRequest.isHttpError);
            
            ErrorInfo retVal = new ErrorInfo();
            retVal.url = webRequest.url;

            if(webRequest.isNetworkError
               || webRequest.responseCode == 404)
            {
                retVal.httpStatusCode = (int)webRequest.responseCode;
                retVal.message = webRequest.error;
            }
            else // if(webRequest.isHttpError)
            {
                API.ErrorObject error = UnityEngine.JsonUtility.FromJson<API.ErrorObject>(webRequest.downloadHandler.text);
                retVal.httpStatusCode = error.error.code;
                retVal.message = error.error.message;
            }

            return retVal;
        }

        // - Fields -
        public string url;
        public int httpStatusCode;
        public string message;

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this.url.GetHashCode() ^ this.httpStatusCode;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ErrorInfo);
        }

        public bool Equals(ErrorInfo other)
        {
            if(Object.ReferenceEquals(other, null))
            {
                return false;
            }
            
            return(Object.ReferenceEquals(this, other)
                   || (this.url == other.url
                       && this.httpStatusCode == other.httpStatusCode
                       && this.message == other.message));
        }
    }
}
