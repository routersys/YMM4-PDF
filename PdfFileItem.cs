using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Docnet.Core;
using Docnet.Core.Models;

namespace Ymm4Pdf.Shape
{
    public class PdfFileItem : INotifyPropertyChanged
    {
        public string FilePath { get; }
        public string FileName { get; }

        private BitmapSource? _thumbnail;
        public BitmapSource? Thumbnail
        {
            get => _thumbnail;
            private set
            {
                _thumbnail = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public PdfFileItem(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
        }

        public async Task LoadThumbnailAsync()
        {
            if (Thumbnail != null) return;

            try
            {
                await Task.Run(() =>
                {
                    const double renderScale = 0.15;

                    using var docReader = DocLib.Instance.GetDocReader(FilePath, new PageDimensions(renderScale));
                    if (docReader == null || docReader.GetPageCount() == 0) return;

                    using var pageReader = docReader.GetPageReader(0);
                    var width = pageReader.GetPageWidth();
                    var height = pageReader.GetPageHeight();
                    var bgraBytes = pageReader.GetImage();

                    if (bgraBytes == null || bgraBytes.Length == 0) return;

                    var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bgraBytes, width * 4);
                    bitmap.Freeze();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Thumbnail = bitmap;
                    });
                });
            }
            catch
            {
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}