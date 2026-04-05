using Docnet.Core;
using Docnet.Core.Models;
using PDF.Interfaces;
using PDF.Models;

namespace PDF.Services
{
    public sealed class PdfRenderService : IPdfRenderService
    {
        public RenderResult RenderPage(string filePath, int pageIndex, float scale)
            => Render(filePath, pageIndex, (double)scale);

        public RenderResult RenderThumbnail(string filePath, float scale = 0.15f)
            => Render(filePath, 0, (double)scale);

        private static RenderResult Render(string filePath, int pageIndex, double scale)
        {
            var clampedScale = Math.Clamp(scale, 0.01, 100.0);

            using var docReader = DocLib.Instance.GetDocReader(
                filePath,
                new PageDimensions(clampedScale));

            using var pageReader = docReader.GetPageReader(pageIndex);

            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            if (width <= 0 || height <= 0)
                return new RenderResult([], 0, 0, 0);

            var byteCount = width * height * 4;
            var rawImage = pageReader.GetImage();

            for (int i = 0; i < byteCount; i += 4)
            {
                var a = rawImage[i + 3];
                if (a < 255)
                {
                    if (a == 0)
                    {
                        rawImage[i] = 0;
                        rawImage[i + 1] = 0;
                        rawImage[i + 2] = 0;
                    }
                    else
                    {
                        rawImage[i] = (byte)(rawImage[i] * a / 255);
                        rawImage[i + 1] = (byte)(rawImage[i + 1] * a / 255);
                        rawImage[i + 2] = (byte)(rawImage[i + 2] * a / 255);
                    }
                }
            }

            return new RenderResult(rawImage, width, height, byteCount);
        }
    }
}
