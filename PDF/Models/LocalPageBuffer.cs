namespace PDF.Models
{
    internal sealed class LocalPageBuffer : IDisposable
    {
        private byte[]? _pixels;
        private bool _disposed;

        public string FilePath { get; }
        public int PageIndex { get; }
        public float Scale { get; }
        public RenderMode Mode { get; }
        public int Width { get; }
        public int Height { get; }
        public int ByteCount { get; }

        internal byte[] RawPixels
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _pixels!;
            }
        }

        public LocalPageBuffer(
            string filePath,
            int pageIndex,
            float scale,
            RenderMode mode,
            byte[] pixels,
            int width,
            int height,
            int byteCount)
        {
            FilePath = filePath;
            PageIndex = pageIndex;
            Scale = scale;
            Mode = mode;
            Width = width;
            Height = height;
            ByteCount = byteCount;
            _pixels = pixels;
        }

        public bool Matches(string filePath, int pageIndex, float scale, RenderMode mode)
            => Mode == mode &&
               PageIndex == pageIndex &&
               Math.Abs(Scale - scale) < 0.001f &&
               string.Equals(FilePath, filePath, StringComparison.OrdinalIgnoreCase);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_pixels is not null)
            {
                Array.Clear(_pixels, 0, _pixels.Length);
                _pixels = null;
            }
        }
    }
}
