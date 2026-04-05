using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace PDF.Models
{
    public sealed class PdfFileItem : INotifyPropertyChanged, IEquatable<PdfFileItem>
    {
        private BitmapSource? _thumbnail;
        private bool _isThumbnailLoading;

        public string FilePath { get; }
        public string FileName { get; }

        public BitmapSource? Thumbnail
        {
            get => _thumbnail;
            internal set => SetField(ref _thumbnail, value);
        }

        public bool IsThumbnailLoading
        {
            get => _isThumbnailLoading;
            internal set => SetField(ref _isThumbnailLoading, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public PdfFileItem(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
        }

        public bool Equals(PdfFileItem? other)
            => other is not null && string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => Equals(obj as PdfFileItem);

        public override int GetHashCode()
            => StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath);

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
