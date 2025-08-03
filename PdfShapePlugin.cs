using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace ymm4_pdf
{
    public class PdfShapePlugin : IShapePlugin
    {
        public string Name => "PDF図形";
        public bool IsExoShapeSupported => false;
        public bool IsExoMaskSupported => false;

        public IShapeParameter CreateShapeParameter(SharedDataStore? sharedData)
        {
            return new PdfShapeParameter(sharedData);
        }
    }
}