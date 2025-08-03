using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace ymm4_pdf
{
    internal class PdfShapeParameter : ShapeParameterBase
    {
        [Display(Name = "ファイル")]
        [PdfFileSelector]
        public string FilePath { get => filePath; set => Set(ref filePath, value); }
        private string filePath = "";

        [Display(Name = "ページ番号")]
        [TextBoxSlider("F0", "ページ", 1, 9999)]
        [DefaultValue(1)]
        public int Page { get => page; set => Set(ref page, value); }
        private int page = 1;

        [Display(Name = "拡大率")]
        [AnimationSlider("F1", "%", 0, 800)]
        public Animation Scale { get; } = new(100, 0, 800);

        public PdfShapeParameter() : this(null) { }
        public PdfShapeParameter(SharedDataStore? sharedData) : base(sharedData) { }

        public override IShapeSource CreateShapeSource(IGraphicsDevicesAndContext devices)
        {
            return new PdfShapeSource(devices, this);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() => new IAnimatable[] { Scale };

        public override IEnumerable<string> CreateMaskExoFilter(int keyFrameIndex, ExoOutputDescription desc, ShapeMaskExoOutputDescription shapeMaskDesc) => [];
        public override IEnumerable<string> CreateShapeItemExoFilter(int keyFrameIndex, ExoOutputDescription desc) => [];
        protected override void LoadSharedData(SharedDataStore store) { }
        protected override void SaveSharedData(SharedDataStore store) { }
    }
}