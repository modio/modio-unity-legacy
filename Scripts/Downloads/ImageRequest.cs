using System;

using Texture2D = UnityEngine.Texture2D;

namespace ModIO
{
    public class ImageRequest
    {
        public event Action<Texture2D> succeeded;
        public event Action<WebRequestError> failed;

        internal void NotifySucceeded(Texture2D texture)
        {
            if(succeeded != null)
            {
                succeeded(texture);
            }
        }

        internal void NotifyFailed(WebRequestError error)
        {
            #if DEBUG
                if(GlobalSettings.LOG_ALL_WEBREQUESTS
                   && failed != APIClient.LogError)
                {
                    APIClient.LogError(error);
                }
            #endif

            if(failed != null)
            {
                failed(error);
            }
        }
    }
}
