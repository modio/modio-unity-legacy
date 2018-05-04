namespace ModIO
{
    public interface IImageLocator
    {
        string GetFileName();
        string GetURL();
    }

    public interface IMultiVersionImageLocator<E> : IImageLocator
    {
        string GetVersionURL(E version);
    }
}