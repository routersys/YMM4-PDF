using PdfiumViewer;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using Vortice.WIC;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace ymm4_pdf
{
    internal class PdfShapeSource : IShapeSource
    {
        private const int RenderDpi = 1200;

        private readonly IGraphicsDevicesAndContext devices;
        private readonly PdfShapeParameter parameter;
        private readonly DisposeCollector disposer = new();

        private readonly AffineTransform2D transformEffect;
        private readonly ID2D1Image outputImage;
        public ID2D1Image Output => outputImage;

        private ID2D1Bitmap? pdfBitmap;

        private string loadedFilePath = "";
        private int loadedPage = -1;
        private double currentScale = -1;

        public PdfShapeSource(IGraphicsDevicesAndContext devices, PdfShapeParameter parameter)
        {
            this.devices = devices;
            this.parameter = parameter;

            transformEffect = new AffineTransform2D(devices.DeviceContext);
            disposer.Collect(transformEffect);
            outputImage = transformEffect.Output;

            ClearAndSetEmpty();
        }

        public void Update(TimelineItemSourceDescription desc)
        {
            var scale = parameter.Scale.GetValue(desc.ItemPosition.Frame, desc.ItemDuration.Frame, desc.FPS) / 100.0;

            if (string.IsNullOrEmpty(parameter.FilePath) || !File.Exists(parameter.FilePath) || parameter.Page < 1)
            {
                if (loadedFilePath != "") ClearAndSetEmpty();
                return;
            }

            bool needsBitmapUpdate = loadedFilePath != parameter.FilePath || loadedPage != parameter.Page;
            if (!needsBitmapUpdate && currentScale == scale) return;

            try
            {
                if (needsBitmapUpdate)
                {
                    using var doc = PdfDocument.Load(parameter.FilePath);
                    using var gdiBitmap = doc.Render(parameter.Page - 1, RenderDpi, RenderDpi, true);

                    using var stream = new MemoryStream();
                    gdiBitmap.Save(stream, ImageFormat.Png);
                    stream.Position = 0;

                    using var factory = new IWICImagingFactory();
                    using var wicStream = factory.CreateStream(stream);
                    using var decoder = factory.CreateDecoderFromStream(wicStream);
                    using var decodedFrame = decoder.GetFrame(0);
                    using var converter = factory.CreateFormatConverter();
                    converter.Initialize(decodedFrame, Vortice.WIC.PixelFormat.Format32bppPBGRA, BitmapDitherType.None, null, 0, BitmapPaletteType.Custom);

                    disposer.RemoveAndDispose(ref pdfBitmap);
                    pdfBitmap = devices.DeviceContext.CreateBitmapFromWicBitmap(converter);
                    disposer.Collect(pdfBitmap);

                    transformEffect.SetInput(0, pdfBitmap, true);
                }

                if (pdfBitmap is null) return;

                var matrix =
                    Matrix3x2.CreateScale((float)scale) *
                    Matrix3x2.CreateTranslation(-pdfBitmap.Size.Width / 2f, -pdfBitmap.Size.Height / 2f);

                transformEffect.TransformMatrix = matrix;

                loadedFilePath = parameter.FilePath;
                loadedPage = parameter.Page;
                currentScale = scale;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ymm4-pdf] Error: {ex.Message}");
                ClearAndSetEmpty();
            }
        }

        private void ClearAndSetEmpty()
        {
            disposer.RemoveAndDispose(ref pdfBitmap);
            pdfBitmap = devices.DeviceContext.CreateEmptyBitmap();
            disposer.Collect(pdfBitmap);

            transformEffect.SetInput(0, pdfBitmap, true);
            transformEffect.TransformMatrix = Matrix3x2.Identity;

            loadedFilePath = "";
            loadedPage = -1;
            currentScale = -1;
        }

        public void Dispose()
        {
            transformEffect.SetInput(0, null, true);
            outputImage.Dispose();
            disposer.DisposeAndClear();
            GC.SuppressFinalize(this);
        }
    }
}