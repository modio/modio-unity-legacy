namespace ModIO
{
    public interface IImageLocator
    {
        string GetFileName();
        string GetURL();
    }

    public interface IMultiSizeImageLocator<E> : IImageLocator
    {
        string GetSizeURL(E size);
        string[] GetAllURLs();
    }
}
