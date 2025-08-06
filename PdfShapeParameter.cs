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
        [Description("ベクター (高品質)")]
        Vector,
        [Description("ラスター (高速)")]
        Raster
    }

    internal class PdfShapeParameter : ShapeParameterBase
    {
        [Display(Name = "ファイル")]
        [PdfFileSelector]
        public string FilePath { get => filePath; set => Set(ref filePath, value); }
        private string filePath = "";

        [Display(Name = "ページ番号")]
        [AnimationSlider("F0", "ページ", 1, 100)]
        public Animation Page { get; } = new(1, 1, 9999);

        [Display(Name = "描画モード")]
        [EnumComboBox]
        public RenderMode RenderMode { get => renderMode; set => Set(ref renderMode, value); }
        private RenderMode renderMode = RenderMode.Vector;

        [Display(Name = "サイズ", GroupName = "ベクター設定")]
        [AnimationSlider("F2", "%", 1, 800)]
        public Animation VectorSize { get; } = new(100, 0, 8000);

        [Display(Name = "品質(DPI)", GroupName = "ラスター設定")]
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