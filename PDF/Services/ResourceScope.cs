using PDF.Interfaces;

namespace PDF.Services
{
    public sealed class ResourceScope : IResourceRegistry
    {
        private readonly List<IDisposable> _resources = [];
        private readonly Lock _lock = new();
        private bool _disposed;

        public T Track<T>(T resource) where T : IDisposable
        {
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _resources.Add(resource);
            }
            return resource;
        }

        public void ReleaseAndNull<T>(ref T? resource) where T : class, IDisposable
        {
            if (resource is null) return;
            var captured = resource;
            resource = null;
            lock (_lock)
                _resources.Remove(captured);
            captured.Dispose();
        }

        public void ReleaseAll()
        {
            IDisposable[] snapshot;
            lock (_lock)
            {
                snapshot = [.. _resources];
                _resources.Clear();
            }
            foreach (var r in snapshot)
            {
                try { r.Dispose(); }
                catch (Exception) { }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
            }
            ReleaseAll();
        }
    }
}
