using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;
using Ymm4Pdf.UI;

namespace Ymm4Pdf.Shape
{
    public enum RenderMode
    {
        [Display(Name = nameof(Translate.Vector), ResourceType = typeof(Translate))]
        Vector,
        [Display(Name = nameof(Translate.Raster), ResourceType = typeof(Translate))]
        Raster
    }

    internal class PdfShapeParameter : ShapeParameterBase
    {
        [Display(Name = nameof(Translate.File), ResourceType = typeof(Translate))]
        [PdfFileSelector]
        public string FilePath { get => filePath; set => Set(ref filePath, value); }
        private string filePath = "";

        [Display(Name = nameof(Translate.PageNumber), ResourceType = typeof(Translate))]
        [AnimationSlider("F0", nameof(Translate.Page), 1, 100, ResourceType = typeof(Translate))]
        public Animation Page { get; } = new(1, 1, 9999);

        [Display(Name = nameof(Translate.RenderMode), ResourceType = typeof(Translate))]
        [EnumComboBox]
        public RenderMode RenderMode { get => renderMode; set => Set(ref renderMode, value); }
        private RenderMode renderMode = RenderMode.Vector;

        [Display(Name = nameof(Translate.Size), GroupName = nameof(Translate.VectorSettings), ResourceType = typeof(Translate))]
        [AnimationSlider("F2", "%", 1, 800)]
        public Animation VectorSize { get; } = new(100, 0, 8000);

        [Display(Name = nameof(Translate.Quality), GroupName = nameof(Translate.RasterSettings), ResourceType = typeof(Translate))]
        [AnimationSlider("F0", "DPI", 72, 2400)]
        public Animation RasterDpi { get; } = new(300, 72, 9600);

        public PdfShapeParameter() : this(null) { }
        public PdfShapeParameter(SharedDataStore? sharedData) : base(sharedData) { }

        public override IShapeSource CreateShapeSource(IGraphicsDevicesAndContext devices)
        {
            return new PdfShapeSource(devices, this);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables()
        {
            return new IAnimatable[] { this.Page, this.VectorSize, this.RasterDpi };
        }

        public override IEnumerable<string> CreateMaskExoFilter(int keyFrameIndex, ExoOutputDescription desc, ShapeMaskExoOutputDescription shapeMaskDesc) => [];
        public override IEnumerable<string> CreateShapeItemExoFilter(int keyFrameIndex, ExoOutputDescription desc) => [];
        protected override void LoadSharedData(SharedDataStore store) { }
        protected override void SaveSharedData(SharedDataStore store) { }
    }
}