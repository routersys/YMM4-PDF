using PDF.Interfaces;
using PDF.Models;
using PDF.Services;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;

namespace PDF
{
    internal sealed class PdfShapeSource : IShapeSource
    {
        private static readonly Lazy<PropertyInfo?> ZoomProperty = new(
            () => typeof(ShapeParameterBase).GetProperty(
                "Zoom",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            LazyThreadSafetyMode.PublicationOnly);

        private readonly IGraphicsDevicesAndContext _devices;
        private readonly PdfShapeParameter _parameter;
        private readonly IPdfRenderService _renderService;
        private readonly ResourceScope _scope = new();

        private ID2D1CommandList? _commandList;
        private LocalPageBuffer? _pageBuffer;

        private string _cachedFilePath = string.Empty;
        private int _cachedPage = -1;
        private RenderMode _cachedRenderMode = (RenderMode)(-1);
        private double _cachedZoom = double.NaN;
        private double _cachedVectorSize = double.NaN;
        private int _cachedRasterDpi = -1;

        public ID2D1Image Output => _commandList
            ?? throw new InvalidOperationException(
                $"{nameof(Update)} must be called before accessing {nameof(Output)}.");

        public PdfShapeSource(
            IGraphicsDevicesAndContext devices,
            PdfShapeParameter parameter,
            IPdfRenderService renderService)
        {
            _devices = devices;
            _parameter = parameter;
            _renderService = renderService;
        }

        public void Update(TimelineItemSourceDescription desc)
        {
            if (string.IsNullOrEmpty(_parameter.FilePath) || !File.Exists(_parameter.FilePath))
            {
                ReleaseBuffer();
                BuildEmptyCommandList();
                return;
            }

            var frame = desc.ItemPosition.Frame;
            var duration = desc.ItemDuration.Frame;
            var fps = desc.FPS;

            var pageNum = Math.Max((int)_parameter.Page.GetValue(frame, duration, fps), 1);
            var renderMode = _parameter.RenderMode;

            var modeChanged = _cachedRenderMode != renderMode;
            if (modeChanged)
                ReleaseBuffer();

            var needsUpdate =
                _cachedFilePath != _parameter.FilePath ||
                _cachedPage != pageNum ||
                modeChanged;

            var scale = ComputeScale(frame, duration, fps, renderMode, ref needsUpdate);
            scale = Math.Clamp(scale, 0.1f, 10.0f);

            if (!needsUpdate && _commandList is not null) return;

            try
            {
                RenderPage(_parameter.FilePath, pageNum, scale, renderMode);
                _cachedFilePath = _parameter.FilePath;
                _cachedPage = pageNum;
                _cachedRenderMode = renderMode;
            }
            catch
            {
                ReleaseBuffer();
                BuildEmptyCommandList();
            }
        }

        private void ReleaseBuffer()
        {
            _scope.ReleaseAndNull(ref _commandList);
            _scope.ReleaseAndNull(ref _pageBuffer);

            _cachedFilePath = string.Empty;
            _cachedPage = -1;
            _cachedVectorSize = double.NaN;
            _cachedRasterDpi = -1;
            _cachedZoom = double.NaN;
        }

        private float ComputeScale(int frame, int duration, int fps, RenderMode mode, ref bool dirty)
        {
            var zoomAnim = ZoomProperty.Value?.GetValue(_parameter) as Animation;
            var zoom = (zoomAnim?.GetValue(frame, duration, fps) ?? 100.0) / 100.0;

            if (mode == RenderMode.Vector)
            {
                var vectorSize = _parameter.VectorSize.GetValue(frame, duration, fps) / 100.0;

                if (Math.Abs(_cachedZoom - zoom) > 0.0001 || Math.Abs(_cachedVectorSize - vectorSize) > 0.0001)
                {
                    dirty = true;
                    _cachedZoom = zoom;
                    _cachedVectorSize = vectorSize;
                }

                return (float)(zoom * vectorSize);
            }
            else
            {
                var dpi = Math.Clamp((int)_parameter.RasterDpi.GetValue(frame, duration, fps), 72, 9600);

                if (_cachedRasterDpi != dpi || Math.Abs(_cachedZoom - zoom) > 0.0001)
                {
                    dirty = true;
                    _cachedRasterDpi = dpi;
                    _cachedZoom = zoom;
                }

                return (float)(dpi / 72.0 * zoom);
            }
        }

        private void RenderPage(string filePath, int pageNum, float scale, RenderMode renderMode)
        {
            if (_pageBuffer is not null && _pageBuffer.Matches(filePath, pageNum - 1, scale, renderMode))
            {
                BuildCommandListFromBuffer(_pageBuffer);
                return;
            }

            _scope.ReleaseAndNull(ref _pageBuffer);

            using var renderResult = _renderService.RenderPage(filePath, pageNum - 1, scale);

            if (renderResult.Width <= 0 || renderResult.Height <= 0)
            {
                BuildEmptyCommandList();
                return;
            }

            _pageBuffer = _scope.Track(new LocalPageBuffer(
                filePath,
                pageNum - 1,
                scale,
                renderMode,
                renderResult.TakeBuffer(),
                renderResult.Width,
                renderResult.Height,
                renderResult.ByteCount));

            BuildCommandListFromBuffer(_pageBuffer);
        }

        private void BuildCommandListFromBuffer(LocalPageBuffer buffer)
        {
            _scope.ReleaseAndNull(ref _commandList);
            _commandList = _scope.Track(_devices.DeviceContext.CreateCommandList());

            var dc = _devices.DeviceContext;
            dc.Target = _commandList;
            dc.BeginDraw();
            dc.Clear(null);

            var handle = GCHandle.Alloc(buffer.RawPixels, GCHandleType.Pinned);
            try
            {
                var bitmapProps = new BitmapProperties(new Vortice.DCommon.PixelFormat(
                    Vortice.DXGI.Format.B8G8R8A8_UNorm,
                    Vortice.DCommon.AlphaMode.Premultiplied));
                using var bitmap = dc.CreateBitmap(
                    new Vortice.Mathematics.SizeI(buffer.Width, buffer.Height),
                    handle.AddrOfPinnedObject(),
                    buffer.Width * 4,
                    bitmapProps);
                dc.DrawImage(bitmap, new Vector2(-buffer.Width / 2f, -buffer.Height / 2f));
            }
            finally
            {
                handle.Free();
            }

            dc.EndDraw();
            dc.Target = null;
            _commandList.Close();
        }

        private void BuildEmptyCommandList()
        {
            _scope.ReleaseAndNull(ref _commandList);
            _commandList = _scope.Track(_devices.DeviceContext.CreateCommandList());

            var dc = _devices.DeviceContext;
            dc.Target = _commandList;
            dc.BeginDraw();
            dc.Clear(null);
            dc.EndDraw();
            dc.Target = null;
            _commandList.Close();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
