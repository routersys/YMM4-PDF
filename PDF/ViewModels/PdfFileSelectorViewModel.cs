using PDF.Interfaces;
using PDF.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PDF.ViewModels
{
    public sealed class PdfFileSelectorViewModel : INotifyPropertyChanged, IDisposable
    {
        private sealed class ThumbnailCacheEntry
        {
            public WeakReference<BitmapSource> Thumbnail { get; }
            public DateTime LastAccess { get; private set; }

            public ThumbnailCacheEntry(BitmapSource bitmap)
            {
                Thumbnail = new WeakReference<BitmapSource>(bitmap);
                LastAccess = DateTime.UtcNow;
            }

            public BitmapSource? TryGet()
            {
                if (!Thumbnail.TryGetTarget(out var target)) return null;
                LastAccess = DateTime.UtcNow;
                return target;
            }

            public bool IsExpired(TimeSpan ttl) => (DateTime.UtcNow - LastAccess) > ttl;
        }

        private static readonly TimeSpan ThumbnailTtl = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromSeconds(20);

        private readonly IPdfRenderService _renderService;
        private readonly IPdfFileRepository _fileRepository;
        private readonly Dictionary<string, PdfFileItem> _itemCache =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ThumbnailCacheEntry> _thumbnailCache =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _thumbnailGate = new(4, 4);
        private readonly Timer _cleanupTimer;
        private readonly object _thumbnailCacheLock = new();

        private string _currentFilePath = string.Empty;
        private PdfFileItem? _selectedItem;
        private bool _disposed;

        public ObservableCollection<PdfFileItem> PdfFiles { get; } = [];

        public PdfFileItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetField(ref _selectedItem, value) && value is not null)
                    FileSelected?.Invoke(this, value.FilePath);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<string>? FileSelected;

        public PdfFileSelectorViewModel(
            IPdfRenderService renderService,
            IPdfFileRepository fileRepository)
        {
            _renderService = renderService;
            _fileRepository = fileRepository;
            _cleanupTimer = new Timer(OnCleanupTick, null, CleanupInterval, CleanupInterval);
        }

        public void SyncExternalPath(string filePath)
        {
            if (string.Equals(_currentFilePath, filePath, StringComparison.OrdinalIgnoreCase))
                return;

            _currentFilePath = filePath;
            RefreshFileList(selectCurrentFile: true);
        }

        public void RefreshFileList(bool selectCurrentFile = false)
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
            {
                PdfFiles.Clear();
                return;
            }

            var directory = Path.GetDirectoryName(_currentFilePath);
            if (string.IsNullOrEmpty(directory))
            {
                PdfFiles.Clear();
                return;
            }

            var diskPaths = _fileRepository.GetPdfFilesInDirectory(directory);
            var diskSet = new HashSet<string>(diskPaths, StringComparer.OrdinalIgnoreCase);

            for (var i = PdfFiles.Count - 1; i >= 0; i--)
            {
                if (!diskSet.Contains(PdfFiles[i].FilePath))
                    PdfFiles.RemoveAt(i);
            }

            var existingSet = new HashSet<string>(
                PdfFiles.Select(f => f.FilePath), StringComparer.OrdinalIgnoreCase);

            foreach (var path in diskPaths)
            {
                if (existingSet.Contains(path)) continue;

                if (!_itemCache.TryGetValue(path, out var item))
                {
                    item = new PdfFileItem(path);
                    _itemCache[path] = item;
                }
                PdfFiles.Add(item);
            }

            if (selectCurrentFile || _selectedItem is null)
            {
                var target = PdfFiles.FirstOrDefault(f =>
                    string.Equals(f.FilePath, _currentFilePath, StringComparison.OrdinalIgnoreCase));
                if (!ReferenceEquals(_selectedItem, target))
                {
                    _selectedItem = target;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
                }
            }

            foreach (var item in PdfFiles)
                _ = LoadThumbnailAsync(item, CancellationToken.None);
        }

        private async Task LoadThumbnailAsync(PdfFileItem item, CancellationToken ct)
        {
            lock (_thumbnailCacheLock)
            {
                if (_thumbnailCache.TryGetValue(item.FilePath, out var cached))
                {
                    var existing = cached.TryGet();
                    if (existing is not null)
                    {
                        if (!ReferenceEquals(item.Thumbnail, existing))
                            item.Thumbnail = existing;
                        return;
                    }
                    _thumbnailCache.Remove(item.FilePath);
                }
            }

            if (item.Thumbnail is not null || item.IsThumbnailLoading) return;

            item.IsThumbnailLoading = true;
            await _thumbnailGate.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                var bitmap = await Task.Run(() => CreateThumbnail(item.FilePath), ct)
                    .ConfigureAwait(false);

                if (bitmap is null) return;

                lock (_thumbnailCacheLock)
                    _thumbnailCache[item.FilePath] = new ThumbnailCacheEntry(bitmap);

                await Application.Current.Dispatcher
                    .InvokeAsync(() => item.Thumbnail = bitmap)
                    .Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
            finally
            {
                _thumbnailGate.Release();
                item.IsThumbnailLoading = false;
            }
        }

        private BitmapSource? CreateThumbnail(string filePath)
        {
            try
            {
                using var result = _renderService.RenderThumbnail(filePath);
                if (result.Width <= 0 || result.Height <= 0) return null;

                var handle = GCHandle.Alloc(result.RawBuffer, GCHandleType.Pinned);
                try
                {
                    var bitmap = BitmapSource.Create(
                        result.Width, result.Height,
                        96, 96,
                        PixelFormats.Bgra32,
                        null,
                        handle.AddrOfPinnedObject(),
                        result.ByteCount,
                        result.Width * 4);
                    bitmap.Freeze();
                    return bitmap;
                }
                finally
                {
                    handle.Free();
                }
            }
            catch
            {
                return null;
            }
        }

        private void OnCleanupTick(object? state)
        {
            lock (_thumbnailCacheLock)
            {
                var expired = _thumbnailCache
                    .Where(kvp => kvp.Value.IsExpired(ThumbnailTtl) || !kvp.Value.Thumbnail.TryGetTarget(out _))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expired)
                    _thumbnailCache.Remove(key);
            }

            var activePaths = new HashSet<string>(
                PdfFiles.Select(f => f.FilePath), StringComparer.OrdinalIgnoreCase);

            var staleItems = _itemCache.Keys
                .Where(k => !activePaths.Contains(k))
                .ToList();

            foreach (var key in staleItems)
                _itemCache.Remove(key);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cleanupTimer.Dispose();
            _thumbnailGate.Dispose();

            lock (_thumbnailCacheLock)
                _thumbnailCache.Clear();
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }
    }
}
