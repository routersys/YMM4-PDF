using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;
using Ymm4Pdf.Shape;

namespace Ymm4Pdf
{
    /// <summary>
    /// プラグインのエントリーポイントです。
    /// </summary>
    public class PdfShapePlugin : IShapePlugin
    {
        /// <summary>
        /// プラグインの表示名
        /// </summary>
        public string Name => "PDF図形 (ベクター)";

        /// <summary>
        /// AviUtlのexoファイル出力に対応しているか（図形アイテムとして）
        /// </summary>
        public bool IsExoShapeSupported => false;

        /// <summary>
        /// AviUtlのexoファイル出力に対応しているか（マスクとして）
        /// </summary>
        public bool IsExoMaskSupported => false;

        /// <summary>
        /// タイムラインアイテムのパラメータを生成します。
        /// </summary>
        /// <param name="sharedData">プロジェクト内での設定共有用データストア</param>
        /// <returns>PDF図形アイテムのパラメータ</returns>
        public IShapeParameter CreateShapeParameter(SharedDataStore? sharedData)
        {
            return new PdfShapeParameter(sharedData);
        }
    }
}
