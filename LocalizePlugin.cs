using System.Globalization;
using YukkuriMovieMaker.Plugin;

namespace Ymm4Pdf
{
    internal class LocalizePlugin : ILocalizePlugin
    {
        public string Name => Translate.PluginName;

        public void SetCulture(CultureInfo cultureInfo)
        {
            Translate.Culture = cultureInfo;
        }
    }
}