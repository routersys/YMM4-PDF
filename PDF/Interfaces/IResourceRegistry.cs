namespace PDF.Interfaces
{
    public interface IResourceRegistry : IDisposable
    {
        T Track<T>(T resource) where T : IDisposable;
        void ReleaseAndNull<T>(ref T? resource) where T : class, IDisposable;
        void ReleaseAll();
    }
}
