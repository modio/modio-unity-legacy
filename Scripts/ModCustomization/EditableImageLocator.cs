namespace ModIO
{
    [System.Serializable]
    public struct ImageLocatorData
    {
        public string fileName;
        public string source;

        public static ImageLocatorData CreateFromImageLocator(IImageLocator locator)
        {
            ImageLocatorData retVal = new ImageLocatorData()
            {
                fileName = locator.fileName,
                source = locator.source,
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
        public string source
        {
            get { return this.value.source; }
            set { this.value.source = value;}
        }
    }

    [System.Serializable]
    public class EditableImageLocatorArrayField : EditableArrayField<ImageLocatorData>{}
}