using UnityEngine;

namespace ModIO.UI
{
    [System.Serializable]
    public struct ImageDisplayData
    {
        public enum MediaType
        {
            None,
            ModLogo,
            ModGalleryImage,
            YouTubeThumbnail,
            UserAvatar,
        };

        public int ownerId;
        public MediaType mediaType;
        public string imageId;
        public Texture2D texture;

        public int modId        { get { return ownerId; } set { ownerId = value; } }
        public int userId       { get { return ownerId; } set { ownerId = value; } }

        public string fileName  { get { return imageId; } set { imageId = value; } }
        public string youTubeId { get { return imageId; } set { imageId = value; } }
    }
}
