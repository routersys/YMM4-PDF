using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;
using Ymm4Pdf.Shape;

namespace Ymm4Pdf
{

    public class PdfShapePlugin : IShapePlugin
    {

        public string Name => Translate.PluginName;

        public bool IsExoShapeSupported => false;

        public bool IsExoMaskSupported => false;

        public IShapeParameter CreateShapeParameter(SharedDataStore? sharedData)
        {
            return new PdfShapeParameter(sharedData);
        }
    }
}