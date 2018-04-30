namespace ModIO
{
    [System.Serializable]
    public struct ImageLocatorData
    {
        public string fileName;
        public string url;

        public static ImageLocatorData CreateFromImageLocator(IImageLocator locator)
        {
            ImageLocatorData retVal = new ImageLocatorData()
            {
                fileName = locator.fileName,
                url = locator.url,
            };
            return retVal;
        }
    }

    [System.Serializable]
    public class EditableImageLocatorField : EditableField<ImageLocatorData>, IImageLocator
    {
        // ---------[ IIMAGELOCATOR INTERFACE ]---------
        public string fileName
        {
            get { return this.value.fileName; }
            set { this.value.fileName = value;}
        }
        public string url
        {
            get { return this.value.url; }
            set { this.value.url = value;}
        }
    }

    [System.Serializable]
    public class EditableImageLocatorArrayField : EditableArrayField<ImageLocatorData>{}
}