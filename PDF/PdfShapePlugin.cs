using PDF.Localization;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace PDF
{
    public sealed class PdfShapePlugin : IShapePlugin
    {
        public string Name => Texts.PluginName;

        public bool IsExoShapeSupported => false;

        public bool IsExoMaskSupported => false;

        public IShapeParameter CreateShapeParameter(SharedDataStore? sharedData)
            => new PdfShapeParameter(sharedData);
    }
}
