namespace PDF.Models
{
    public sealed class RenderResult : IDisposable
    {
        private byte[]? _buffer;

        public int Width { get; }
        public int Height { get; }
        public int ByteCount { get; }

        public ReadOnlySpan<byte> Data
        {
            get
            {
                ObjectDisposedException.ThrowIf(_buffer is null, this);
                return _buffer.AsSpan(0, ByteCount);
            }
        }

        internal byte[] RawBuffer
        {
            get
            {
                ObjectDisposedException.ThrowIf(_buffer is null, this);
                return _buffer!;
            }
        }

        internal byte[] TakeBuffer()
        {
            ObjectDisposedException.ThrowIf(_buffer is null, this);
            var taken = _buffer!;
            _buffer = null;
            return taken;
        }

        internal RenderResult(byte[] buffer, int width, int height, int byteCount)
        {
            _buffer = buffer;
            Width = width;
            Height = height;
            ByteCount = byteCount;
        }

        public void Dispose()
        {
            _buffer = null;
        }
    }
}
