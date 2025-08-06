using Docnet.Core;
using Docnet.Core.Models;
using System;
using System.Numerics;
using System.Reflection;
using Vortice;
using Vortice.Direct2D1;
using Vortice.Mathematics;
using Vortice.WIC;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;

namespace Ymm4Pdf.Shape
{
    internal class PdfShapeSource : IShapeSource
    {
        private readonly IGraphicsDevicesAndContext devices;
        private readonly PdfShapeParameter parameter;
        private readonly DisposeCollector disposer = new();
        private readonly IWICImagingFactory wicFactory;

        public ID2D1Image Output => commandList ?? throw new InvalidOperationException("Update must be called before accessing Output");

        private ID2D1CommandList? commandList;
        private ID2D1Bitmap? pageBitmap;

        // キャッシュ用のフィールド
        private string cachedFilePath = "";
        private int cachedPage = -1;
        private RenderMode cachedRenderMode = (RenderMode)(-1);
        private double cachedZoom = -1;
        private double cachedVectorSize = -1;
        private int cachedRasterDpi = -1;

        // リフレクション用の静的フィールド
        private static PropertyInfo? zoomPropertyInfo;
        private static bool isZoomPropertySearched = false;

        public PdfShapeSource(IGraphicsDevicesAndContext devices, PdfShapeParameter parameter)
        {
            this.devices = devices;
            this.parameter = parameter;

            wicFactory = new IWICImagingFactory();
            disposer.Collect(wicFactory);
        }

        private Animation? GetZoomAnimation()
        {
            if (!isZoomPropertySearched)
            {
                zoomPropertyInfo = typeof(ShapeParameterBase).GetProperty("Zoom", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                isZoomPropertySearched = true;
            }
            return zoomPropertyInfo?.GetValue(this.parameter) as Animation;
        }

        public void Update(TimelineItemSourceDescription desc)
        {
            // ファイルの存在確認
            if (string.IsNullOrEmpty(parameter.FilePath) || !System.IO.File.Exists(parameter.FilePath))
            {
                if (commandList == null || !string.IsNullOrEmpty(cachedFilePath))
                {
                    CreateEmptyCommandList();
                }
                return;
            }

            var pageNum = Math.Max((int)parameter.Page.GetValue(desc.ItemPosition.Frame, desc.ItemDuration.Frame, desc.FPS), 1);
            var renderMode = parameter.RenderMode;

            // パラメータ変更の検出
            bool needsUpdate = cachedFilePath != parameter.FilePath ||
                              cachedPage != pageNum ||
                              cachedRenderMode != renderMode;

            float scale = CalculateScale(desc, renderMode, ref needsUpdate);

            // スケール範囲の制限
            scale = Math.Clamp(scale, 0.1f, 10.0f);

            if (!needsUpdate && commandList != null) return;

            // PDF描画処理
            try
            {
                RenderPdfPage(parameter.FilePath, pageNum, scale);
                UpdateCache(parameter.FilePath, pageNum, renderMode);
            }
            catch
            {
                CreateEmptyCommandList();
            }
        }

        private float CalculateScale(TimelineItemSourceDescription desc, RenderMode renderMode, ref bool needsUpdate)
        {
            var frame = desc.ItemPosition.Frame;
            var length = desc.ItemDuration.Frame;
            var fps = desc.FPS;

            double zoomValue = GetZoomAnimation()?.GetValue(frame, length, fps) ?? 100.0;
            double currentZoom = zoomValue / 100.0;

            if (renderMode == RenderMode.Vector)
            {
                var vectorSizeValue = parameter.VectorSize.GetValue(frame, length, fps);
                double currentVectorSize = vectorSizeValue / 100.0;

                if (cachedZoom != currentZoom || cachedVectorSize != currentVectorSize)
                {
                    needsUpdate = true;
                    cachedZoom = currentZoom;
                    cachedVectorSize = currentVectorSize;
                }

                return (float)(currentZoom * currentVectorSize);
            }
            else
            {
                var dpi = Math.Clamp((int)parameter.RasterDpi.GetValue(frame, length, fps), 72, 9600);

                if (cachedRasterDpi != dpi || cachedZoom != currentZoom)
                {
                    needsUpdate = true;
                    cachedRasterDpi = dpi;
                    cachedZoom = currentZoom;
                }

                return (float)((dpi / 72f) * currentZoom);
            }
        }

        private void UpdateCache(string filePath, int pageNum, RenderMode renderMode)
        {
            cachedFilePath = filePath;
            cachedPage = pageNum;
            cachedRenderMode = renderMode;
        }

        private void RenderPdfPage(string filePath, int pageNum, float scale)
        {
            disposer.RemoveAndDispose(ref pageBitmap);

            using var docReader = DocLib.Instance.GetDocReader(filePath, new PageDimensions(scale));
            if (docReader == null || docReader.GetPageCount() < pageNum)
            {
                CreateEmptyCommandList();
                return;
            }

            using var pageReader = docReader.GetPageReader(pageNum - 1);
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();
            var bgraBytes = pageReader.GetImage();

            if (bgraBytes == null || bgraBytes.Length == 0)
            {
                CreateEmptyCommandList();
                return;
            }

            using var wicBitmap = wicFactory.CreateBitmapFromMemory(width, height, Vortice.WIC.PixelFormat.Format32bppBGRA, bgraBytes);
            using var converter = wicFactory.CreateFormatConverter();

            converter.Initialize(wicBitmap, Vortice.WIC.PixelFormat.Format32bppPBGRA, BitmapDitherType.None, null, 0, BitmapPaletteType.MedianCut);
            pageBitmap = devices.DeviceContext.CreateBitmapFromWicBitmap(converter);
            disposer.Collect(pageBitmap);

            CreateCommandListWithBitmap();
        }

        private void CreateCommandListWithBitmap()
        {
            disposer.RemoveAndDispose(ref commandList);
            commandList = devices.DeviceContext.CreateCommandList();
            disposer.Collect(commandList);

            var dc = devices.DeviceContext;
            dc.Target = commandList;
            dc.BeginDraw();
            dc.Clear(null);

            if (pageBitmap != null)
            {
                var bitmapSize = pageBitmap.Size;
                var offset = new Vector2(-bitmapSize.Width / 2f, -bitmapSize.Height / 2f);
                dc.DrawImage(pageBitmap, offset);
            }

            dc.EndDraw();
            dc.Target = null;
            commandList.Close();
        }

        private void CreateEmptyCommandList()
        {
            disposer.RemoveAndDispose(ref pageBitmap);
            disposer.RemoveAndDispose(ref commandList);

            commandList = devices.DeviceContext.CreateCommandList();
            disposer.Collect(commandList);

            var dc = devices.DeviceContext;
            dc.Target = commandList;
            dc.BeginDraw();
            dc.Clear(null);
            dc.EndDraw();
            dc.Target = null;
            commandList.Close();

            cachedFilePath = "";
        }

        public void Dispose()
        {
            disposer.DisposeAndClear();
        }
    }
}