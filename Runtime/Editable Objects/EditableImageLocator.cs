namespace ModIO
{
    [System.Serializable]
    public struct ImageLocatorData
    {
        public string fileName;
        public string url;

        public static ImageLocatorData CreateFromImageLocator(IImageLocator locator)
        {
            ImageLocatorData retVal = new ImageLocatorData() {
                fileName = locator.GetFileName(),
                url = locator.GetURL(),
            };
            return retVal;
        }
    }

    [System.Serializable]
    public class EditableImageLocatorField : EditableField<ImageLocatorData>, IImageLocator
    {
        // ---------[ IIMAGELOCATOR INTERFACE ]---------
        public string GetFileName()
        {
            return this.value.fileName;
        }
        public string GetURL()
        {
            return this.value.url;
        }
    }

    [System.Serializable]
    public class EditableImageLocatorArrayField : EditableArrayField<ImageLocatorData>
    {
    }
}
