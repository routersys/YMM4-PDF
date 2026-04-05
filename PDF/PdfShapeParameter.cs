using PDF.Attributes;
using PDF.Interfaces;
using PDF.Localization;
using PDF.Models;
using PDF.Services;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace PDF
{
    [PluginDetails(AuthorName = "routersys")]

    public sealed class PdfShapeParameter : ShapeParameterBase
    {
        [Display(Name = nameof(Texts.File), ResourceType = typeof(Texts))]
        [PdfFileSelector]
        public string FilePath
        {
            get => _filePath;
            set => Set(ref _filePath, value);
        }
        private string _filePath = string.Empty;

        [Display(Name = nameof(Texts.PageNumber), ResourceType = typeof(Texts))]
        [AnimationSlider("F0", nameof(Texts.Page), 1, 100, ResourceType = typeof(Texts))]
        public Animation Page { get; } = new(1, 1, 9999);

        [Display(Name = nameof(Texts.RenderMode), ResourceType = typeof(Texts))]
        [EnumComboBox]
        public RenderMode RenderMode
        {
            get => _renderMode;
            set => Set(ref _renderMode, value);
        }
        private RenderMode _renderMode = RenderMode.Vector;

        [Display(Name = "")]
        [PdfShapeEditor]
        public object? EditorPlaceholder => null;

        public Animation VectorSize { get; } = new(100, 0, 1200);

        public Animation RasterDpi { get; } = new(300, 72, 800);

        public PdfShapeParameter() : this(null) { }

        public PdfShapeParameter(SharedDataStore? sharedData) : base(sharedData) { }

        public override IShapeSource CreateShapeSource(IGraphicsDevicesAndContext devices)
            => new PdfShapeSource(
                devices,
                this,
                ServiceLocator.Instance.Resolve<IPdfRenderService>());

        protected override IEnumerable<IAnimatable> GetAnimatables()
            => [Page, VectorSize, RasterDpi];

        public override IEnumerable<string> CreateMaskExoFilter(
            int keyFrameIndex,
            ExoOutputDescription desc,
            ShapeMaskExoOutputDescription shapeMaskDesc) => [];

        public override IEnumerable<string> CreateShapeItemExoFilter(
            int keyFrameIndex,
            ExoOutputDescription desc) => [];

        protected override void LoadSharedData(SharedDataStore store) { }

        protected override void SaveSharedData(SharedDataStore store) { }

        public sealed class VectorParameters
        {
            private readonly PdfShapeParameter _parent;
            public VectorParameters(PdfShapeParameter parent) => _parent = parent;

            [Display(Name = nameof(Texts.Size), GroupName = nameof(Texts.VectorSettings), ResourceType = typeof(Texts))]
            [AnimationSlider("F2", "%", 1, 1200)]
            public Animation VectorSize => _parent.VectorSize;
        }

        public sealed class RasterParameters
        {
            private readonly PdfShapeParameter _parent;
            public RasterParameters(PdfShapeParameter parent) => _parent = parent;

            [Display(Name = nameof(Texts.Quality), GroupName = nameof(Texts.RasterSettings), ResourceType = typeof(Texts))]
            [AnimationSlider("F0", "DPI", 72, 800)]
            public Animation RasterDpi => _parent.RasterDpi;
        }
    }
}
