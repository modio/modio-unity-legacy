using UnityEngine.Networking;

namespace ModIO
{
    /// <summary>Adds extensions to the UnityWebRequest class.</summary>
    public static class UnityWebRequestExtensions
    {
        /// <summary>Wraps the various error-types for earlier versions of Unity.</summary>
        public static bool IsError(this UnityWebRequest webRequest)
        {
#if UNITY_2020_1_OR_NEWER

            return (webRequest.result == UnityWebRequest.Result.ConnectionError
                    || webRequest.result == UnityWebRequest.Result.ProtocolError
                    || webRequest.result == UnityWebRequest.Result.DataProcessingError);

#elif UNITY_2017_1_OR_NEWER

            return (webRequest.isHttpError || webRequest.isNetworkError);

#else

            return webRequest.isError;

#endif // Unity Version Selector
        }
    }
}
