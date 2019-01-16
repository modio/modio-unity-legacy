using System;

using Texture2D = UnityEngine.Texture2D;

namespace ModIO
{
    public class ImageRequest
    {
        public event Action<ImageRequest> succeeded;
        public event Action<ImageRequest> failed;

        public Texture2D imageTexture;
        public string filePath;
        public bool isDone;

        public WebRequestError error;

        internal void NotifySucceeded()
        {
            if(succeeded != null)
            {
                succeeded(this);
            }
        }

        internal void NotifyFailed()
        {
            if(failed != null)
            {
                failed(this);
            }
        }
    }
}
